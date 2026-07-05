# Module PostgreSQL Connection Status

**Previous audit:** 2026-07-03  
**This audit:** 2026-07-05 (stabilization pass — opening balances, inventory foundation, suppliers/purchases)  
**Scope:** All submodules in `SubmoduleRegistry` (+ Dashboard).  
**Method:** Source-code review of view factories, controls, UI services, engines, and handlers. Documentation conflicts resolved in favor of source.

## Status legend

| Symbol | Meaning |
|--------|---------|
| ✅ Full | Main list/read **and** create/edit/post (or equivalent write path) connected to PostgreSQL |
| 🟡 Partial | Some real DB reads or writes work; key sub-operations missing or read-only |
| ❌ None | Empty state, placeholder, or UI shell with no PostgreSQL connection |

## Summary

| Status | Previous (2026-07-03) | **Current (2026-07-05)** |
|--------|----------------------:|-------------------------:|
| ✅ Full | 28 | **42** |
| 🟡 Partial | 35 | **30** |
| ❌ None | 28 | **22** |
| **Total submodules** | 91 | **94** |

*Total increased by 3: Accounting `OpeningBalances` submodule + Inventory dashboard route keys already in registry.*

---

## Stabilization changes (this pass)

| Area | Previous | New | Key files |
|------|----------|-----|-----------|
| Unified opening balances | 🟡 dual path | ✅ single engine path | `OpeningBalanceEngine.cs`, `OpeningBalanceUiService.cs`, `CustomerOpeningBalanceControl.cs`, `SupplierOpeningBalanceControl.cs` |
| Customer opening balance | ❌ None | ✅ Full | `CustomerOpeningBalanceControl.cs` → `OpeningBalanceUiService.PostPartyOpeningBalanceAsync` |
| Supplier list/form/statement | ❌ None | ✅ Full | `SupplierListPageControl.cs`, `SupplierFormControl.cs`, `SupplierAccountStatementControl.cs` |
| Supplier opening balance | ❌ None | ✅ Full | `SupplierOpeningBalanceControl.cs` |
| Purchase invoices | ❌ None | ✅ Full | `PurchaseInvoiceListPageControl.cs`, `PurchaseInvoiceFormControl.cs` → `PurchaseUiService` |
| Inventory opening stock | ❌ None | ✅ Full | `InventoryOpeningStockPageControl.cs`, `InventoryOpeningStockFormControl.cs` → `IInventoryEngine.PostOpeningStockAsync` |
| Inventory transfers | ❌ None | ✅ Full | `InventoryTransferListPageControl.cs`, `InventoryTransferWizardControl.cs` → `CompleteTransferAsync` |
| Inventory stocktake | ❌ None | ✅ Full | `InventoryStocktakeListPageControl.cs`, `InventoryStocktakeWizardControl.cs` → `PostStocktakeAsync` |
| Inventory warehouses | 🟡 Partial | ✅ Full | `InventoryWarehouseListPageControl.cs`, `InventoryWarehouseFormControl.cs`, `InventoryPopupService.cs` |
| Expense dashboard route | 🟡 wrong route | ✅ Full | `ExpenseViews.cs` → `ExpenseDashboardControl` |
| Capital dashboard route | 🟡 wrong route | ✅ Full | `CapitalViews.cs` → `CapitalDashboardControl` |
| Legacy OB accounting APIs | active | obsolete | `IIntegratedAccountingService.PostCustomer/SupplierOpeningBalanceAsync` |

---

## Connection map (stabilization-focused modules)

| Module | Submodule | Status | Notes |
|--------|-----------|--------|-------|
| **Inventory** | Dashboard | 🟡 Partial | `InventoryDashboardControl` — KPIs from DB; some widgets placeholder |
| **Inventory** | Warehouses | ✅ Full | List + create/edit popup + OC; `InventoryUiService` / warehouse handlers |
| **Inventory** | Categories | 🟡 Partial | Imported fabric classifications (read + rename); no manual category CRUD |
| **Inventory** | Import Excel | ❌ None | Redirect banner to China Import / Opening Stock |
| **Inventory** | Opening Stock | ✅ Full | List + form; draft save + post via `InventoryUiService` → `IInventoryEngine` |
| **Inventory** | Stocktake | ✅ Full | Session list + wizard; system qty load, count, variance, post |
| **Inventory** | Transfers | ✅ Full | List + 5-step wizard; `CompleteTransferAsync`; same-warehouse blocked in engine |
| **Inventory** | Settings | ❌ None | Static info card only |
| **Inventory** | Reports | 🟡 Partial | Hub wired; `inv.stocktake` report key may return «قيد التطوير» |
| **Customers** | List | ✅ Full | `CustomerListPageControl` → `CustomerUiService` |
| **Customers** | Form | ✅ Full | `CustomerFormControl` CRUD |
| **Customers** | Opening Balances | ✅ Full | `CustomerOpeningBalanceControl` → `OpeningBalanceUiService.PostPartyOpeningBalanceAsync` |
| **Customers** | Statement | 🟡 Partial | Works when customer selected via OC/context; empty from sidebar alone |
| **Customers** | Invoices | ✅ Full | Scoped `SalesInvoiceListPageControl` when customer in navigation context |
| **Customers** | Reports | ✅ Full | Module report hub |
| **Suppliers** | List | ✅ Full | `SupplierListPageControl` → `SupplierUiService.GetListAsync` |
| **Suppliers** | Form | ✅ Full | `SupplierFormControl` → create/update |
| **Suppliers** | Opening Balances | ✅ Full | `SupplierOpeningBalanceControl` → unified engine |
| **Suppliers** | Statement | 🟡 Partial | `SupplierAccountStatementControl` when supplier in context |
| **Suppliers** | Invoices | 🟡 Partial | `SupplierInvoiceListControl` when supplier in context |
| **Suppliers** | Reports | ✅ Full | Module report hub |
| **Purchases** | Invoices | ✅ Full | List + form + post; `IPurchaseInventoryService` + `IIntegratedAccountingService` |
| **Purchases** | Orders | 🟡 Partial | List + form; posting workflow **Unable to determine full post path from UI alone** |
| **Purchases** | Returns | 🟡 Partial | List + form; post via handler when implemented |
| **Purchases** | Reports | 🟡 Partial | DB reports if purchase data exists |
| **Accounting** | OpeningBalances | ✅ Full | List, form, dashboard, OC, context menu; `IOpeningBalanceEngine` workflow |
| **Expenses** | Dashboard | ✅ Full | `ExpenseViews` `"Dashboard"` → `ExpenseDashboardControl` |
| **Capital Partners** | Dashboard | ✅ Full | `CapitalViews` `"Dashboard"` → `CapitalDashboardControl` |

*Other modules unchanged from prior audit unless noted in full map below.*

---

## Full connection map (all submodules)

| Module | Submodule | Status | Notes |
|--------|-----------|--------|-------|
| **Dashboard** | Overview | 🟡 Partial | KPIs from DB; activity feed empty |
| **China Import** | Containers | ✅ Full | |
| **China Import** | New Import | ✅ Full | |
| **China Import** | File Analysis | 🟡 Partial | Needs upload context |
| **China Import** | Distribution | 🟡 Partial | Allocation not implemented |
| **China Import** | Stocktake | 🟡 Partial | Physical count not implemented |
| **China Import** | Landing Cost | 🟡 Partial | Requires container context |
| **China Import** | Reports | ✅ Full | |
| **Inventory** | *(see stabilization table above)* | | |
| **Sales** | New Invoice | ✅ Full | |
| **Sales** | Invoice List | ✅ Full | |
| **Sales** | Invoice View | ❌ None | Use list → OC |
| **Sales** | New Return | ❌ None | Placeholder |
| **Sales** | Returns List | ❌ None | Placeholder |
| **Sales** | Detailing | ✅ Full | |
| **Sales** | Delivery | ❌ None | Placeholder |
| **Sales** | Reports | ✅ Full | |
| **Purchases** | *(see stabilization table above)* | | |
| **Customers** | *(see stabilization table above)* | | |
| **Suppliers** | *(see stabilization table above)* | | |
| **Accounting** | Chart of Accounts | ✅ Full | |
| **Accounting** | Journal | ✅ Full | |
| **Accounting** | Journal Books | 🟡 Partial | Read-only |
| **Accounting** | Account Ledger | ✅ Full | |
| **Accounting** | Receipts | ✅ Full | |
| **Accounting** | Payments | ✅ Full | |
| **Accounting** | Cashboxes | 🟡 Partial | List read; add may be popup-limited |
| **Accounting** | Transfers | ❌ None | Placeholder |
| **Accounting** | Receivables | ❌ None | Placeholder (reports work) |
| **Accounting** | Payables | ❌ None | Placeholder (reports work) |
| **Accounting** | Trial Balance | ✅ Full | |
| **Accounting** | OpeningBalances | ✅ Full | Unified engine |
| **Accounting** | Reports | ✅ Full | |
| **Expenses** | List | ✅ Full | |
| **Expenses** | Entries | ✅ Full | |
| **Expenses** | New Entry | ✅ Full | |
| **Expenses** | Form | ✅ Full | |
| **Expenses** | Dashboard | ✅ Full | Route fixed |
| **Expenses** | Reports | ✅ Full | |
| **Expenses** | Workspace | 🟡 Partial | Needs expense ID context |
| **Expenses** | Categories | 🟡 Partial | Read-only admin |
| **Capital Partners** | List | ✅ Full | |
| **Capital Partners** | Transactions | ✅ Full | |
| **Capital Partners** | Investment | ✅ Full | |
| **Capital Partners** | Form | ✅ Full | |
| **Capital Partners** | Dashboard | ✅ Full | Route fixed |
| **Capital Partners** | Distributions | 🟡 Partial | Read-only list |
| **Capital Partners** | Reports | ✅ Full | |
| **Capital Partners** | Workspace | 🟡 Partial | Needs partner ID context |
| **Reports** | BI Dashboard | ❌ None | Static KPIs |
| **Reports** | Executive Reports | 🟡 Partial | Some keys «قيد التطوير» |
| **HR** | Employees | 🟡 Partial | Handlers exist; UI connectivity **varies — verify in HR pass** |
| **HR** | Departments | ❌ None | Placeholder |
| **HR** | Attendance–Advances | ❌ None | Placeholders |
| **HR** | Reports | 🟡 Partial | |
| **Settings** | All 13 submodules | ❌ None | Forms not wired |

---

## Modules ranked by connectivity (current)

| Module | Full | Partial | None | Overall |
|--------|-----:|--------:|-----:|---------|
| Accounting | 8 | 2 | 3 | Strong — GL, vouchers, unified opening balances |
| Expenses | 6 | 2 | 0 | Strong |
| Capital Partners | 6 | 2 | 0 | Strong |
| Sales | 4 | 0 | 4 | Core invoice flow complete |
| Inventory | 4 | 2 | 2 | **Improved** — operational stock workflows connected |
| Customers | 4 | 1 | 0 | **Improved** — opening balance unified |
| Suppliers | 3 | 2 | 0 | **Improved** — core CRM parity with customers |
| Purchases | 1 | 3 | 0 | **Improved** — invoice posting connected |
| China Import | 3 | 4 | 0 | Good |
| Dashboard | 0 | 1 | 0 | Mixed |
| Reports (global) | 0 | 1 | 1 | Weak |
| HR | 0 | 1 | 8 | Not started (UI) |
| Settings | 0 | 0 | 13 | Not started |

---

## Remaining gaps (stabilization scope)

| Gap | Status |
|-----|--------|
| Manual fabric category CRUD | Not implemented yet (import-driven catalog only) |
| Inventory Settings submodule | Not implemented yet |
| Inventory Import Excel standalone | Not implemented yet |
| Accounting Transfers / Receivables / Payables screens | Not implemented yet |
| Purchase order post-to-invoice workflow | Unable to determine from current source |
| HR module screens | Not implemented yet |
| Settings module | Not implemented yet |
| DocumentEngine → WPF print/PDF | Not implemented yet |

---

## Primary engines (stabilization)

| Workflow | Engine / service |
|----------|------------------|
| Opening balance document | `IOpeningBalanceEngine` → `IIntegratedAccountingService.PostOpeningBalanceDocumentAsync` |
| Party opening balance (customer/supplier UI) | `OpeningBalanceUiService.PostPartyOpeningBalanceAsync` |
| Opening stock | `IInventoryEngine.PostOpeningStockAsync` (+ finance OB stock via `PostFinanceOpeningBalanceStockAsync` when type is `OpeningStock`) |
| Transfer complete | `IInventoryEngine.CompleteTransferAsync` |
| Stocktake post | `IInventoryEngine.PostStocktakeAsync` |
| Purchase invoice post | `IPurchaseInventoryService` + `IIntegratedAccountingService` |

---

*Re-run this audit after wiring new handlers or view factories.*
