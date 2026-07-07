# Performance Root Cause Report

- Timestamp UTC: `2026-07-07T20:27:02.5555733+00:00`
- Test database: `erp_pro_perf`
- Seed time: `26.17s`
- Database size: `10.97 MB` -> `250.28 MB`

## Record Counts
- `fabric_rolls`: `200,000`
- `containers`: `500`
- `container_items`: `200,000`
- `sales_invoices`: `10,000`
- `sales_invoice_items`: `50,000`
- `customers`: `500`
- `purchase_orders`: `2,000`
- `stock_movements`: `50,000`
- `stock_movement_lines`: `50,000`
- `journal_entries`: `15,000`
- `audit_logs`: `100,000`
- `total`: `678,000`

## Measured Bottlenecks
- `desktop_warehouse_list_current_n_plus_one_shape`: `2,639.99 ms`, rows `15`, payload `0.00 MB`, managed memory delta `0.20 MB`
- `db_inventory_all_rolls_for_warehouse`: `1,234.36 ms`, rows `80,000`, payload `35.89 MB`, managed memory delta `3.89 MB`
- `desktop_inventory_list_repository_shape`: `1,128.19 ms`, rows `80,000`, payload `29.29 MB`, managed memory delta `195.11 MB`
- `desktop_operations_center_stock_balance_shape`: `439.01 ms`, rows `20,000`, payload `13.34 MB`, managed memory delta `90.65 MB`
- `db_inventory_filter_main_complete`: `186.09 ms`, rows `60,000`, payload `5.45 MB`, managed memory delta `2.46 MB`
- `desktop_navigation_inventory_to_sales_back`: `153.05 ms`, rows `4`, payload `0.00 MB`, managed memory delta `0.04 MB`
- `db_inventory_filter_main_complete_paged_50`: `99.04 ms`, rows `50`, payload `0.00 MB`, managed memory delta `0.03 MB`
- `db_fabric_selection_by_stock`: `90.96 ms`, rows `4`, payload `0.00 MB`, managed memory delta `0.02 MB`
- `db_sales_invoices_all_active`: `62.12 ms`, rows `8,000`, payload `2.70 MB`, managed memory delta `2.56 MB`
- `db_month_sales_report_sql_aggregate`: `18.82 ms`, rows `30`, payload `0.00 MB`, managed memory delta `0.02 MB`

## Web Measurements
- `Local WiFi` `GET /api/v1/inventory/warehouses`: backend `1.50 ms`, payload `0.00 MB`, estimated E2E `11.67 ms`
- `Local WiFi` `GET /api/v1/inventory/dashboard`: backend `233.87 ms`, payload `0.00 MB`, estimated E2E `243.96 ms`
- `Local WiFi` `GET /api/v1/inventory/alerts`: backend `2.39 ms`, payload `0.00 MB`, estimated E2E `12.39 ms`
- `Local WiFi` `Inventory mobile page aggregate (3 parallel React Query calls)`: backend `237.75 ms`, payload `0.00 MB`, estimated E2E `268.02 ms`
- `Mobile 4G` `GET /api/v1/inventory/warehouses`: backend `0.57 ms`, payload `0.00 MB`, estimated E2E `102.26 ms`
- `Mobile 4G` `GET /api/v1/inventory/dashboard`: backend `220.74 ms`, payload `0.00 MB`, estimated E2E `321.68 ms`
- `Mobile 4G` `GET /api/v1/inventory/alerts`: backend `0.55 ms`, payload `0.00 MB`, estimated E2E `100.55 ms`
- `Mobile 4G` `Inventory mobile page aggregate (3 parallel React Query calls)`: backend `221.86 ms`, payload `0.00 MB`, estimated E2E `524.49 ms`
- `Mobile 3G` `GET /api/v1/inventory/warehouses`: backend `0.49 ms`, payload `0.00 MB`, estimated E2E `217.35 ms`
- `Mobile 3G` `GET /api/v1/inventory/dashboard`: backend `228.35 ms`, payload `0.00 MB`, estimated E2E `437.74 ms`
- `Mobile 3G` `GET /api/v1/inventory/alerts`: backend `0.50 ms`, payload `0.00 MB`, estimated E2E `200.50 ms`
- `Mobile 3G` `Inventory mobile page aggregate (3 parallel React Query calls)`: backend `229.33 ms`, payload `0.00 MB`, estimated E2E `855.57 ms`

## Root Causes
- **Critical / Database + Application + UI / Inventory and warehouse lists**: Repository methods load all matching FabricRoll rows into memory, then aggregate/project in C#. Current 200K test confirms full-result operations dominate versus LIMIT 50. Solution: Server-side pagination and DTO projection, composite indexes on WarehouseId/Status/RemainingLengthMeters/RollNumber, and WPF DataGrid virtualization.
- **Critical / Application / Operations center / stock balances**: GetFabricStockBalancesAsync loads warehouse_stocks and all rolls, then repeatedly filters rolls per stock row in memory. Solution: Move aggregation to SQL GROUP BY or materialized inventory summary, fetch only top/page rows for the current tab.
- **High / Network + API / Web mobile inventory**: The mobile page issues warehouses/dashboard/alerts calls. Backend aggregation is the main cost now; on 3G payload and latency add visible delay. Solution: Keep calls parallel, add response compression, cache dashboard snapshots, and paginate any future roll list endpoint.

## Answers
- `desktop_rows_displayed`: Current repository shape returns all rows for the selected warehouse; no server page contract exists for GetFabricRollsAsync.
- `wpf_virtualization`: Must be verified visually in XAML/runtime; code-level performance risk remains because ItemsSource can receive tens of thousands of rows.
- `filter_location`: Inventory filtering in tested repository methods is server-side only for simple WHERE clauses, but aggregation and many projections run client-side in C#.
- `ef_loading`: No lazy-loading proxy was found; slowness is from explicit ToListAsync full loads and follow-up dictionary/lookups.
- `sql_executed`: EXPLAIN ANALYZE output is included in the JSON report.
- `web_api_calls_inventory_page`: 3 calls in web-client Inventory.tsx: warehouses, dashboard, alerts.
- `web_parallel_or_sequential`: React Query calls are declared independently and can run in parallel.
- `web_pagination`: Current inventory web page does not request fabric-roll pages; WPF/repository roll APIs are unpaged.
- `compression`: API Program.cs does not configure response compression.
- `json_response_size`: Measured in payloadBytes per web measurement in the JSON report.
- `fabric_roll_indexes`: Existing model defines indexes only on Barcode and FabricBatchId; no WarehouseId/Status composite index.
- `partial_index_complete`: No partial index for available/complete rolls exists in the current model.
- `columns_returned`: Several paths return full entities first, then map to DTOs.
- `includes`: No large Include chain was found in the inventory repository; related data is loaded with separate lookup queries.
- `n_plus_one`: Warehouse list uses per-warehouse stock/roll queries; operations center uses in-memory repeated filtering across loaded collections.
- `lookup_cache`: Catalog lookups are queried per operation; no cross-request cache is configured.

## EXPLAIN Evidence
### inventory_filter
```text
Limit  (cost=9046.23..9052.06 rows=50 width=37) (actual time=85.610..92.749 rows=50 loops=1)
  Buffers: shared hit=2997 read=3327
  ->  Gather Merge  (cost=9046.23..11419.63 rows=20342 width=37) (actual time=85.609..92.742 rows=50 loops=1)
        Workers Planned: 2
        Workers Launched: 2
        Buffers: shared hit=2997 read=3327
        ->  Sort  (cost=8046.21..8071.63 rows=10171 width=37) (actual time=48.680..48.683 rows=33 loops=3)
              Sort Key: "RollNumber"
              Sort Method: top-N heapsort  Memory: 28kB
              Buffers: shared hit=2997 read=3327
              Worker 0:  Sort Method: top-N heapsort  Memory: 28kB
              Worker 1:  Sort Method: quicksort  Memory: 25kB
              ->  Parallel Seq Scan on "FabricRolls" r  (cost=0.00..7708.33 rows=10171 width=37) (actual time=8.470..44.819 rows=20000 loops=3)
                    Filter: (("RemainingLengthMeters" > '0'::numeric) AND ("WarehouseId" = '06f5501d-6523-42ef-95e4-c6c5d6df3b3c'::uuid) AND ("Status" = 0))
                    Rows Removed by Filter: 46667
                    Buffers: shared hit=2923 read=3327
Planning Time: 0.079 ms
Execution Time: 92.773 ms
```
### warehouse_stock
```text
Sort  (cost=3157.76..3208.08 rows=20127 width=148) (actual time=36.547..38.023 rows=20000 loops=1)
  Sort Key: "TotalMeters" DESC
  Sort Method: quicksort  Memory: 3528kB
  Buffers: shared hit=966
  ->  Seq Scan on warehouse_stocks  (cost=0.00..1719.00 rows=20127 width=148) (actual time=0.011..23.697 rows=20000 loops=1)
        Filter: (("TotalMeters" > '0'::numeric) AND ("WarehouseId" = '06f5501d-6523-42ef-95e4-c6c5d6df3b3c'::uuid))
        Rows Removed by Filter: 30200
        Buffers: shared hit=966
Planning Time: 0.094 ms
Execution Time: 39.734 ms
```
