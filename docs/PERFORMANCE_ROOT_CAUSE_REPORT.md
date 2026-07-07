# Performance Root Cause Report

- Timestamp UTC: `2026-07-07T21:09:42.2444156+00:00`
- Test database: `erp_pro_perf`
- Seed time: `34.69s`
- Database size: `11.01 MB` -> `284.16 MB`

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
- `db_inventory_all_rolls_for_warehouse`: `1,196.74 ms`, rows `80,000`, payload `35.89 MB`, managed memory delta `4.17 MB`
- `desktop_inventory_list_repository_shape`: `1,119.45 ms`, rows `80,000`, payload `29.29 MB`, managed memory delta `195.12 MB`
- `desktop_operations_center_stock_balance_optimized_shape`: `559.18 ms`, rows `20,000`, payload `11.80 MB`, managed memory delta `51.19 MB`
- `desktop_operations_center_stock_balance_shape`: `391.94 ms`, rows `20,000`, payload `13.32 MB`, managed memory delta `90.58 MB`
- `desktop_warehouse_list_current_n_plus_one_shape`: `291.91 ms`, rows `15`, payload `0.00 MB`, managed memory delta `0.20 MB`
- `desktop_warehouse_list_optimized_grouped_shape`: `215.78 ms`, rows `15`, payload `0.00 MB`, managed memory delta `0.08 MB`
- `db_inventory_filter_main_complete`: `201.27 ms`, rows `60,000`, payload `5.45 MB`, managed memory delta `2.46 MB`
- `desktop_navigation_inventory_to_sales_back`: `148.20 ms`, rows `4`, payload `0.00 MB`, managed memory delta `0.04 MB`
- `desktop_inventory_page_optimized_shape`: `139.32 ms`, rows `200`, payload `0.07 MB`, managed memory delta `0.65 MB`
- `db_sales_invoices_all_active`: `52.27 ms`, rows `8,000`, payload `2.70 MB`, managed memory delta `2.56 MB`
- `db_month_sales_report_sql_aggregate`: `27.78 ms`, rows `30`, payload `0.00 MB`, managed memory delta `0.02 MB`
- `db_fabric_selection_by_stock`: `1.20 ms`, rows `4`, payload `0.00 MB`, managed memory delta `0.02 MB`
- `db_inventory_filter_main_complete_paged_50`: `0.99 ms`, rows `50`, payload `0.00 MB`, managed memory delta `0.03 MB`

## Web Measurements
- `Local WiFi` `GET /api/v1/inventory/warehouses`: backend `1.77 ms`, payload `0.00 MB`, estimated E2E `11.94 ms`
- `Local WiFi` `GET /api/v1/inventory/dashboard`: backend `219.06 ms`, payload `0.00 MB`, estimated E2E `229.15 ms`
- `Local WiFi` `GET /api/v1/inventory/alerts`: backend `1.96 ms`, payload `0.00 MB`, estimated E2E `11.96 ms`
- `Local WiFi` `Inventory mobile page aggregate (3 parallel React Query calls)`: backend `222.79 ms`, payload `0.00 MB`, estimated E2E `253.05 ms`
- `Mobile 4G` `GET /api/v1/inventory/warehouses`: backend `0.49 ms`, payload `0.00 MB`, estimated E2E `102.18 ms`
- `Mobile 4G` `GET /api/v1/inventory/dashboard`: backend `219.89 ms`, payload `0.00 MB`, estimated E2E `320.83 ms`
- `Mobile 4G` `GET /api/v1/inventory/alerts`: backend `1.07 ms`, payload `0.00 MB`, estimated E2E `101.07 ms`
- `Mobile 4G` `Inventory mobile page aggregate (3 parallel React Query calls)`: backend `221.45 ms`, payload `0.00 MB`, estimated E2E `524.08 ms`
- `Mobile 3G` `GET /api/v1/inventory/warehouses`: backend `0.59 ms`, payload `0.00 MB`, estimated E2E `217.45 ms`
- `Mobile 3G` `GET /api/v1/inventory/dashboard`: backend `215.41 ms`, payload `0.00 MB`, estimated E2E `424.80 ms`
- `Mobile 3G` `GET /api/v1/inventory/alerts`: backend `1.48 ms`, payload `0.00 MB`, estimated E2E `201.48 ms`
- `Mobile 3G` `Inventory mobile page aggregate (3 parallel React Query calls)`: backend `217.48 ms`, payload `0.00 MB`, estimated E2E `843.73 ms`

## Root Causes
- **Critical / Database + Application + UI / Inventory and warehouse lists**: Baseline repository shape loaded all matching FabricRoll rows into memory, then aggregated/projected in C#. The optimized page shape fetches only the requested rows. Solution: Implemented server-side pagination and DTO projection, composite/partial FabricRoll indexes, and grouped warehouse aggregates.
- **Critical / Application / Operations center / stock balances**: Baseline stock balance shape loaded warehouse_stocks and all rolls, then repeatedly filtered rolls per stock row in memory. Solution: Implemented SQL GROUP BY roll-cost aggregation and joined projected stock balances.
- **High / Network + API / Web mobile inventory**: The mobile page issues warehouses/dashboard/alerts calls. Backend aggregation is the main cost now; on 3G payload and latency add visible delay. Solution: Response compression is enabled and a paged rolls API contract is available for future roll-list screens.

## Answers
- `desktop_rows_displayed`: The legacy GetFabricRollsAsync now returns a capped first page for compatibility; GetFabricRollsPageAsync exposes the explicit server page contract.
- `wpf_virtualization`: WPF design was not changed; the UI service now has a paged method so screens can bind pages without loading tens of thousands of rows.
- `filter_location`: Inventory filtering in tested repository methods is server-side only for simple WHERE clauses, but aggregation and many projections run client-side in C#.
- `ef_loading`: No lazy-loading proxy was found; slowness is from explicit ToListAsync full loads and follow-up dictionary/lookups.
- `sql_executed`: EXPLAIN ANALYZE output is included in the JSON report.
- `web_api_calls_inventory_page`: 3 calls in web-client Inventory.tsx: warehouses, dashboard, alerts.
- `web_parallel_or_sequential`: React Query calls are declared independently and can run in parallel.
- `web_pagination`: A paged web API endpoint is available at GET /api/v1/inventory/warehouses/{warehouseId}/rolls with pageNumber/pageSize/status/search.
- `compression`: API Program.cs configures response compression.
- `json_response_size`: Measured in payloadBytes per web measurement in the JSON report.
- `fabric_roll_indexes`: The model now defines WarehouseId/Status, WarehouseId/FabricColorId, ContainerId/Status, Status, and available-roll partial indexes.
- `partial_index_complete`: The partial index uses the actual enum value Status = 0 for available rolls and RemainingLengthMeters > 0.
- `columns_returned`: The paged roll path projects DTO fields instead of materializing full FabricRoll entities.
- `includes`: No large Include chain was found in the inventory repository; related data is loaded with separate lookup queries.
- `n_plus_one`: Warehouse list now uses grouped stock and roll-value queries instead of per-warehouse stock/roll queries.
- `lookup_cache`: Account and fabric catalog lookups now use in-memory cache with prefix invalidation on writes.

## EXPLAIN Evidence
### inventory_filter
```text
Limit  (cost=0.41..43.86 rows=50 width=37) (actual time=0.019..0.038 rows=50 loops=1)
  Buffers: shared hit=5
  ->  Index Scan using idx_fabric_rolls_available_partial on "FabricRolls" r  (cost=0.41..21153.55 rows=24343 width=37) (actual time=0.018..0.034 rows=50 loops=1)
        Index Cond: ("WarehouseId" = 'd9d2cb51-2c70-4b94-a9f0-350b1b21e24c'::uuid)
        Buffers: shared hit=5
Planning Time: 0.183 ms
Execution Time: 0.049 ms
```
### warehouse_stock
```text
Sort  (cost=3142.35..3192.17 rows=19931 width=148) (actual time=29.484..31.114 rows=20000 loops=1)
  Sort Key: "TotalMeters" DESC
  Sort Method: quicksort  Memory: 3528kB
  Buffers: shared hit=966
  ->  Seq Scan on warehouse_stocks  (cost=0.00..1719.00 rows=19931 width=148) (actual time=0.013..20.489 rows=20000 loops=1)
        Filter: (("TotalMeters" > '0'::numeric) AND ("WarehouseId" = 'd9d2cb51-2c70-4b94-a9f0-350b1b21e24c'::uuid))
        Rows Removed by Filter: 30200
        Buffers: shared hit=966
Planning Time: 0.082 ms
Execution Time: 32.147 ms
```
