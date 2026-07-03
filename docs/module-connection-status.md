# Module PostgreSQL Connection Status

**Audit date:** 2026-07-03  
**Scope:** All top-level modules and submodules registered in `SubmoduleRegistry` (+ Dashboard).  
**Method:** Read-only review of view factories, UI controls, and `*UiService` / MediatR handler wiring. No code changes.

## Status legend

| Symbol | Meaning |
|--------|---------|
| ✅ Full | Main list/read **and** create/edit/post (or equivalent write path) connected to PostgreSQL |
| 🟡 Partial | Some real DB reads or writes work; key sub-operations missing, context-only, or read-only |
| ❌ None | Empty state, placeholder, or UI shell with no PostgreSQL connection |

## Summary

| Status | Count |
|--------|------:|
| ✅ Full | 28 |
| 🟡 Partial | 35 |
| ❌ None | 28 |
| **Total submodules** | **91** |

---

## Connection map

| Module | Submodule | Status | Notes |
|--------|-----------|--------|-------|
| **Dashboard** | Overview | 🟡 Partial | KPI cards via `GetDashboardSummaryHandler`; debt customers + containers chart + detailing queue from DB; activity feed empty; insight cards hidden; trend placeholders `—` |
| **China Import** | Containers | ✅ Full | `ContainerListPageControl` → `ContainerUiService.GetListAsync`; OC + workflow navigation |
| **China Import** | New Import | ✅ Full | `NewChinaImportControl` → parse/create via `ContainerUiService` (invoice, packing, DPL Excel) |
| **China Import** | File Analysis | 🟡 Partial | `PackingListAnalysisControl` uses parsed session + `RefreshMultiFileSessionAsync`; needs upload context from import step |
| **China Import** | Distribution | 🟡 Partial | `ContainerWorkflowSummaryControl` reads container items from DB; customer/reservation allocation not implemented |
| **China Import** | Stocktake | 🟡 Partial | Shows system roll/meter totals from DB; physical count entry not implemented |
| **China Import** | Landing Cost | 🟡 Partial | `ChinaImportLandingCostReviewControl` → `ApproveContainerAsync`; requires active container in `ChinaImportNavigationContext` |
| **China Import** | Reports | ✅ Full | `ModuleReportsHub` → `ModuleReportRepository` (`cn.containers`, `cn.landing_cost`, `cn.inventory`, `cn.sale_ready`) |
| **Inventory** | Warehouses | 🟡 Partial | `InventoryWarehouseListPageControl` → `SalesUiService.GetWarehousesAsync` (read); add/edit → `ShowComingSoon` |
| **Inventory** | Categories | 🟡 Partial | `InventoryFabricCategoriesPageControl` → `IFabricCatalogRepository` (read-only grid); no category CRUD UI |
| **Inventory** | Import Excel | ❌ None | Upload UI shell only; preview/actions → `ShowComingSoon` |
| **Inventory** | Opening Stock | ❌ None | Blank form; save/approve → `ShowComingSoon` |
| **Inventory** | Stocktake | ❌ None | Empty state «لا توجد جلسات جرد» |
| **Inventory** | Transfers | ❌ None | Empty state «لا توجد مناقلات» |
| **Inventory** | Settings | ❌ None | `PlaceholderUi.DevelopmentPhase` only |
| **Inventory** | Reports | 🟡 Partial | Report hub wired to DB; `inv.stocktake` report key returns «قيد التطوير» |
| **Sales** | New Invoice | ✅ Full | `NewSalesInvoiceControl` → `SalesUiService` (draft, lines, send, approve); stock from `GetWarehouseStockAsync` |
| **Sales** | Invoice List | ✅ Full | `SalesInvoiceListPageControl` → `SalesUiService.GetListAsync` |
| **Sales** | Invoice View | ❌ None | Empty state «يرجى اختيار فاتورة»; real view via list → Operations Center |
| **Sales** | New Return | ❌ None | `PlaceholderUi.DevelopmentPhase` |
| **Sales** | Returns List | ❌ None | `PlaceholderUi.DevelopmentPhase` |
| **Sales** | Detailing | ✅ Full | `WarehouseDetailingPageControl` → `GetDetailingQueueAsync` + `CompleteDetailingAsync` |
| **Sales** | Delivery | ❌ None | `PlaceholderUi.DatabasePhase` — delivery cards not built |
| **Sales** | Reports | ✅ Full | Hub reports `sal.invoices`, `sal.by_customer`, `sal.detailing`, `sal.returns`, `sal.delivery` query PostgreSQL |
| **Purchases** | Invoices | ❌ None | Empty list (`BindData([])`); no purchase invoice handlers wired |
| **Purchases** | Orders | ❌ None | Blank form shell only |
| **Purchases** | Returns | ❌ None | Blank form shell only |
| **Purchases** | Reports | 🟡 Partial | `pur.invoices` / `pur.by_supplier` query DB if purchase data exists; no purchase UI to create records |
| **Customers** | List | ✅ Full | `CustomerListPageControl` → `CustomerUiService.GetListAsync`; OC on double-click |
| **Customers** | Form | ✅ Full | `CustomerFormControl` → `CreateAsync` / `UpdateAsync` / `GetDetailsAsync` |
| **Customers** | Opening Balances | ❌ None | Empty state only |
| **Customers** | Statement | 🟡 Partial | `CustomerAccountStatementControl` → `GetCustomerStatementHandler` when customer selected; empty guard from sidebar |
| **Customers** | Invoices | ❌ None | Redirect message / «اختر عميلاً»; no filtered invoice list by customer |
| **Customers** | Reports | ✅ Full | `cus.balances`, `cus.statements`, `cus.invoices` via `ModuleReportRepository` |
| **Suppliers** | List | ❌ None | Empty list; seeded supplier exists in DB but no list handler wired |
| **Suppliers** | Form | ❌ None | Blank form shell; no save handler |
| **Suppliers** | Statement | ❌ None | «اختر مورداً لعرض كشف حسابه» |
| **Suppliers** | Invoices | ❌ None | «اختر مورداً لعرض فواتيره» |
| **Suppliers** | Reports | 🟡 Partial | `sup.balances`, `sup.statements`, `sup.invoices` query DB; supplier UI not connected |
| **Accounting** | Chart of Accounts | ✅ Full | `ChartOfAccountsListPageControl` + `AccountFormControl` → `AccountingUiService` CRUD |
| **Accounting** | Journal | ✅ Full | `JournalEntryListPageControl` + `JournalEntryFormControl` → create/post/approve/reverse |
| **Accounting** | Journal Books | 🟡 Partial | `JournalBookListPageControl` reads `GetJournalBooksAsync`; cards marked read-only, no book CRUD UI |
| **Accounting** | Account Ledger | ✅ Full | `AccountLedgerReportControl` → `GetAccountLedgerAsync` |
| **Accounting** | Receipts | ✅ Full | `ReceiptVoucherPageControl` → `FinanceUiService.CreateReceiptVoucherAsync` + `PostReceiptVoucherAsync` |
| **Accounting** | Payments | ✅ Full | `PaymentVoucherPageControl` → `FinanceUiService.CreatePaymentVoucherAsync` + `PostPaymentVoucherAsync` |
| **Accounting** | Cashboxes | 🟡 Partial | `CashboxListPageControl` → `FinanceUiService.GetCashboxesAsync` (read); add → `ShowComingSoon` |
| **Accounting** | Transfers | ❌ None | `FinanceViews` → `PlaceholderUi.DevelopmentPhase` |
| **Accounting** | Receivables | ❌ None | `FinanceViews` → `PlaceholderUi.DevelopmentPhase` (report `acc.receivables` works separately) |
| **Accounting** | Payables | ❌ None | `FinanceViews` → `PlaceholderUi.DevelopmentPhase` (report `acc.payables` works separately) |
| **Accounting** | Trial Balance | ✅ Full | `TrialBalanceReportControl` → `AccountingUiService.GetTrialBalanceAsync` |
| **Accounting** | Reports | ✅ Full | Hub + custom views for trial balance/ledger; `acc.journal`, `acc.receipts`, `acc.payments`, receivables/payables reports query DB |
| **Expenses** | List | ✅ Full | `ExpenseListPageControl` → definitions CRUD via `ExpenseUiService` + popups |
| **Expenses** | Entries | ✅ Full | `ExpenseEntryListPageControl` → `GetEntriesAsync` |
| **Expenses** | New Entry | ✅ Full | `ExpenseEntryFormControl` → `RecordPaymentAsync` |
| **Expenses** | Form | ✅ Full | `ExpenseFormControl` → `CreateDefinitionAsync` / `UpdateDefinitionAsync` |
| **Expenses** | Dashboard | 🟡 Partial | `ExpenseDashboardControl` + `GetDashboardAsync` exist but `ExpenseViews` has no `Dashboard` route — sidebar opens List instead |
| **Expenses** | Reports | ✅ Full | Custom `ExpenseReportsControl` + hub slices (`exp.outstanding`, `exp.upcoming`, `exp.recurring`) |
| **Expenses** | Workspace | 🟡 Partial | `ExpenseOperationsCenterControl` → `GetOperationsCenterAsync` when opened with expense context; sidebar opens without ID |
| **Expenses** | Categories | 🟡 Partial | `ExpenseCategoryAdminControl` reads seeded categories via `GetCategoriesAsync`; no admin CRUD |
| **Capital Partners** | List | ✅ Full | `CapitalPartnerListPageControl` → `GetListAsync` + create popup |
| **Capital Partners** | Transactions | ✅ Full | `CapitalTransactionListPageControl` → `GetTransactionsAsync` |
| **Capital Partners** | Investment | ✅ Full | `CapitalInvestmentFormControl` → `RecordTransactionAsync` |
| **Capital Partners** | Form | ✅ Full | `CapitalPartnerFormControl` → create/update + ownership |
| **Capital Partners** | Dashboard | 🟡 Partial | `CapitalDashboardControl` + `GetDashboardAsync` exist but `CapitalViews` has no `Dashboard` route — sidebar opens List |
| **Capital Partners** | Distributions | 🟡 Partial | `CapitalDistributionsControl` → `GetDistributionsAsync` (read-only list); no create/post UI |
| **Capital Partners** | Reports | ✅ Full | Custom capital report views + `ModuleReportRepository` |
| **Capital Partners** | Workspace | 🟡 Partial | `CapitalOperationsCenterControl` when partner ID in navigation context; sidebar opens without ID |
| **Reports** | BI Dashboard | ❌ None | `ReportViews.BuildExecutiveDashboard` — static `—` KPIs, no DB query |
| **Reports** | Executive Reports | 🟡 Partial | Report hub present; `exec.dashboard` / `exec.sales_vs_purch` return «قيد التطوير» from repository |
| **HR** | Employees | ❌ None | Empty list; add → `ShowComingSoon` |
| **HR** | Departments | ❌ None | `PlaceholderUi.DatabasePhase` |
| **HR** | Attendance | ❌ None | `PlaceholderUi.DatabasePhase` |
| **HR** | Leaves | ❌ None | `PlaceholderUi.DatabasePhase` |
| **HR** | Shifts | ❌ None | `PlaceholderUi.DatabasePhase` |
| **HR** | Contracts | ❌ None | `PlaceholderUi.DatabasePhase` |
| **HR** | Payroll | ❌ None | `PlaceholderUi.DatabasePhase` |
| **HR** | Advances | ❌ None | `PlaceholderUi.DatabasePhase` |
| **HR** | Reports | 🟡 Partial | `hr.employees` queries DB; `hr.attendance` / `hr.payroll` return «قيد التطوير» |
| **Settings** | Company | ❌ None | Empty form inputs; save not wired |
| **Settings** | Branches | ❌ None | Empty form inputs; save not wired |
| **Settings** | Users | ❌ None | Empty form inputs; save not wired |
| **Settings** | Locale | ❌ None | Empty form inputs; save not wired |
| **Settings** | Currencies | ❌ None | Empty form inputs; save not wired |
| **Settings** | Finance | ❌ None | Empty form inputs; save not wired |
| **Settings** | Taxes | ❌ None | Empty form inputs; save not wired |
| **Settings** | Numbering | ❌ None | Empty form inputs; save not wired |
| **Settings** | Print | ❌ None | Empty form inputs; save not wired |
| **Settings** | Inventory | ❌ None | Empty form inputs; save not wired |
| **Settings** | Sales | ❌ None | Empty form inputs; save not wired |
| **Settings** | Backup | ❌ None | Empty form inputs; save not wired |
| **Settings** | Audit | ❌ None | Empty form inputs; save not wired |

---

## Modules ranked by connectivity

| Module | Full | Partial | None | Overall |
|--------|-----:|--------:|-----:|---------|
| Sales | 4 | 0 | 4 | Strong — core invoice + detailing flow complete |
| Accounting | 6 | 2 | 3 | Strong — GL, vouchers, trial balance connected |
| Expenses | 5 | 3 | 0 | Strong — full expense lifecycle |
| Capital Partners | 5 | 3 | 0 | Strong — partners + transactions connected |
| Customers | 3 | 1 | 2 | Good — CRM core done; opening balances & invoice sub-view missing |
| China Import | 3 | 4 | 0 | Good — end-to-end import workflow; distribution/stocktake incomplete |
| Inventory | 0 | 2 | 5 | Weak — read-only warehouse/catalog; no stock operations UI |
| Reports (global) | 0 | 1 | 1 | Weak — per-module report hubs are stronger |
| Dashboard | 0 | 1 | 0 | Mixed — operational widgets real; analytics/activity not |
| Purchases | 0 | 1 | 3 | Minimal — reports may show DB data; no purchase UI |
| Suppliers | 0 | 1 | 4 | Minimal — reports only |
| HR | 0 | 1 | 8 | Not started (UI) |
| Settings | 0 | 0 | 13 | Not started |

---

## Notable routing gaps (not separate submodules)

These China Import workflow screens are **not** sidebar submodules but are part of the connected import pipeline when a container is in context:

| Screen | Control | Status | Notes |
|--------|---------|--------|-------|
| Cost Entry | `ChinaImportCostEntryControl` | ✅ Full | `SubmitCostEntryAsync` |
| Sale Price | `ChinaImportSalePriceControl` | ✅ Full | `SetTypeSalePricesAsync` |
| Move to Warehouse | `ChinaImportWarehouseTransferControl` | ✅ Full | `MoveToWarehouseAsync` |
| Ready for Sale | `ChinaImportReadyForSaleControl` | ✅ Full | Reads approved/in-warehouse container from DB |

---

## Primary services / handlers by connected module

| Module | Main UI service / infrastructure |
|--------|----------------------------------|
| Customers | `CustomerUiService`, `GetCustomerStatementHandler` |
| Sales | `SalesUiService`, `GetDetailingQueueAsync`, `GetSalesWarehouseStockHandler` |
| China Import | `ContainerUiService`, container use-case handlers |
| Accounting | `AccountingUiService`, `FinanceUiService` |
| Expenses | `ExpenseUiService`, `IExpenseRepository` |
| Capital Partners | `CapitalPartnerUiService` |
| Inventory (partial) | `SalesUiService.GetWarehousesAsync`, `IFabricCatalogRepository` |
| Reports (all modules) | `ModuleReportUiService` → `GetModuleReportHandler` → `ModuleReportRepository` |
| Dashboard | `GetDashboardSummaryHandler`, `CustomerUiService`, `ContainerUiService`, `SalesUiService` |

---

*Generated by read-only codebase audit. Re-run after wiring new handlers or view factories.*
