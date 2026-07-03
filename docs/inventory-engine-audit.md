# Inventory Engine — Phase 0 Architecture Audit

> Audit date: 2026-07-03. Single source of truth for Inventory Engine implementation.

## Executive Summary

Inventory is **operationally wired** for China Import → Sales and local Purchases, but there was **no unified Inventory Engine**. Stock was mutated by three infrastructure services writing directly to `FabricRollEntity` + `WarehouseStockEntity` with header-only `StockMovementEntity` rows. Domain types and inventory domain events existed but were bypassed.

**Resolution:** Introduce `IInventoryEngine` as the **only** writer to stock tables. All modules delegate to it.

---

## What Already Exists

| Component | Location | Status |
|-----------|----------|--------|
| `Warehouse`, `WarehouseStockBalance`, `StockMovement` | `Domain/Entities/Inventory/InventoryEntities.cs` | Domain scaffold |
| `FabricRoll` | `Domain/Entities/Catalog/CatalogEntities.cs` | Domain with Reserve/Deduct |
| `WarehouseAggregate` | `Domain/Aggregates/WarehouseAggregate.cs` | Read-oriented |
| `MovementType`, `StockMovementStatus`, `FabricRollStatus` | `Domain/Enums/` | Reuse |
| `StockMovementValidator`, `InventoryReservationPolicy` | `Domain/Services/` | Reuse in engine |
| Inventory events | `Domain/Events/Inventory/InventoryEvents.cs` | Raise from engine |
| `WarehouseEntity`, `WarehouseStockEntity`, `StockMovementEntity` | `Infrastructure/Persistence/Models/Inventory/` | Production tables |
| `FabricRollEntity` | `Infrastructure/Persistence/Models/Catalog/` | Production |
| `InventoryRepository` | Read-only container metrics, rolls, low stock | Extend |
| `InventoryOperationsService` | Sales reserve/deduct/release | Delegate to engine |
| `PurchaseInventoryService` | Purchase post/reverse | Delegate to engine |
| `ContainerWarehouseImportService` | China import post | Delegate to engine |
| `IntegratedAccountingService` | GL for activation, sales COGS, purchases | Keep; engine calls hooks |
| Inventory UI placeholders | `Views/Inventory/InventoryViews.cs` | Replace |

## What Should Be Reused (DO NOT DUPLICATE)

- Existing PostgreSQL tables: `inventory.warehouses`, `warehouse_stocks`, `FabricRolls`, `StockMovements`
- `IInventoryRepository` query methods
- `ContainerSaleValidator`, sales stock picker in `NewSalesInvoiceControl`
- `ExpenseTrailRecorder` pattern for inventory audit/timeline
- `OperationsCenterShell` for warehouse operations center
- `ErpListModuleControl` for list pages
- GL hooks in `IntegratedAccountingService` (extension points only for adjustments/transfers)

## What Should Be Extended

- `MovementType` enum — add Purchase, SaleReturn, PurchaseReturn, OpeningBalance, Manufacturing, Consumption, Production, Damage, Loss, Correction
- `WarehouseEntity` — manager, address, description, cost center, default flag, notes
- `WarehouseLocationEntity` — hierarchy (Zone→Rack→Shelf→Bin), parent, capacity, barcode
- `FabricRollEntity` — batch, storage location, barcode, quality/reservation status
- `StockMovementEntity` — lines table, reason, source/dest warehouse/location
- New: `FabricBatchEntity`, `InventoryReservationEntity`, `StockTransferDocumentEntity`, `StocktakeSessionEntity`, `OpeningStockDocumentEntity`, `InventoryRuleEntity`, `InventoryAlertEntity`, audit/timeline tables
- `IInventoryEngine` — centralized write authority
- `InventoryManagementRepository` — CRUD + queries for new entities
- Inventory module UI — full replacement of placeholders

## What Should Be Removed (When Migrated)

- `Core/Domain/FabricDomainModels.cs` — legacy inventory types
- `Core/Inventory/FabricInventoryModels.cs` — legacy models
- Direct EF stock mutations outside `InventoryEngine`

## What Must Never Be Duplicated

- Stock calculation logic (single path via movements)
- Warehouse stock upsert math (engine only)
- Roll status transitions (engine only)
- A fourth parallel stock-write service

---

## Current Stock Mutation Paths (Pre-Engine)

```
China Import → ContainerWarehouseImportService.PostContainerStockAsync
Purchases    → PurchaseInventoryService.PostPurchaseInvoiceStockAsync / ReversePurchaseReturnStockAsync
Sales        → InventoryOperationsService.Reserve/Deduct/Release/Assign
```

All three now delegate to `InventoryEngine`.

---

## GL Integration (Existing)

| Method | Trigger |
|--------|---------|
| `PostInventoryActivationAsync` | Container → warehouse |
| `PostSalesInvoiceApprovalAsync` | Sales approve (COGS) |
| `PostPurchaseInvoiceAsync` | Purchase post |
| `PostPurchaseReturnAsync` | Purchase return |

Future: adjustments, transfers, stocktake, opening stock (extension points only).

---

## Implementation Order (Completed)

1. ✅ Domain enums + entities
2. ✅ Migration + persistence models + configurations
3. ✅ `IInventoryEngine` + `InventoryEngine`
4. ✅ Refactor existing services to delegate
5. ✅ Application commands/queries/handlers
6. ✅ Inventory UI (warehouse CRUD, OC, dashboard, stock grid, transfers, stocktake, opening stock)
7. ✅ Reports wired to PostgreSQL data
