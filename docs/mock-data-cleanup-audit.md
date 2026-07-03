# Mock / Sample / Hardcoded Data Audit

**Project:** ERP PRO (C# WPF + PostgreSQL)  
**Date:** 2026-07-03  
**Scope:** Full solution scan — no code changes in this pass  
**Method:** Static analysis of `*SampleData*`, `Mock*`, `PlaceholderUi`, inline `new[] { ... }` grids, hardcoded defaults, and mixed DB/mock screens.

---

## Executive Summary

| Category | Count (approx.) | Risk |
|----------|-----------------|------|
| 🔴 **CRITICAL** | 14 | Mock data inside modules already wired to PostgreSQL — users may see fake numbers beside real data |
| 🟡 **ACTIVE MOCK** | 52 | Daily-visible screens or navigation paths still fed by generators / inline arrays |
| ⚪ **DORMANT** | 18 | Placeholder-only screens, unused generators, dead helpers, stub services |

**Modules with strong PostgreSQL coverage (primary lists/forms):** Customers, Sales (list + draft save), Warehouse Detailing (queue + save), China Import (containers + workflow), Accounting (chart, journal, vouchers, GL reports), Expenses, Capital Partners.

**Modules still almost entirely mock:** Inventory UI, Suppliers UI, Purchases UI, HR UI, Settings UI, Finance fallback views (Cashboxes / AR / AP lists).

---

## Legend

| Column | Meaning |
|--------|---------|
| **Breaks screen?** | **Yes** = mock is the only data source. **Partial** = DB path exists but mock still appears in some flows. **No** = explicit placeholder / empty-state only. |
| **Replacement** | **(a)** Connect to existing handler · **(b)** Empty state Arabic message · **(c)** Placeholder UI «قريباً» |

---

## 🔴 CRITICAL — Mock inside PostgreSQL-connected modules

These are the highest priority: they sit on code paths that already call real handlers (`CustomerUiService`, `SalesUiService`, `ContainerUiService`, `GetDashboardSummaryHandler`, etc.).

### Sales

| File | Location | What it provides | Breaks screen? | Suggested replacement |
|------|----------|------------------|----------------|----------------------|
| `Controls/Sales/NewSalesInvoiceControl.xaml.cs` | `SalesInvoiceLineRow` fields L42–48; `EnsureDefaultLine` L142–147; `BtnAddLine_Click` L568–570 | Default line: goods type «قماش قطن», bolt `FAB-001`, color «أبيض», **unit price `12.5m`** | **Partial** — DB saves real `UnitPrice` but UI pre-fills fake catalog/price on every new line | **(a)** Load fabric types/colors/prices from container/catalog via `GetChinaContainerListHandler` + fabric catalog query; default price from container sale price or `0` |
| `Controls/Sales/NewSalesInvoiceControl.xaml.cs` | `LoadInvoiceAsync` L272–288 | When editing DB invoice, rehydrates lines with hardcoded `GoodsType`/`BoltCode`/`Color` instead of fabric IDs/names | **Partial** — amounts/rolls from DB; display fields wrong | **(a)** Map `invoice.Lines` through `SalesUiService` DTO fabric names (`FabricItemId` / `FabricColorId`) |
| `Controls/Sales/WarehouseDetailingPageControl.cs` | `LoadQueueAsync` L113–116 | Fallback **`unitPrice = 12.5m`** if ops center load fails | **Partial** — queue from `GetWarehouseDetailingQueueHandler`; price may be wrong | **(a)** Use `dto` line price from queue DTO or require ops load; **(b)** show «—» if missing |
| `Controls/Workspace/WarehouseDetailingWorkspaceControl.cs` | `LoadFromDatabase` L196–197 | DB roll rows shown with hardcoded `FabricCode = "FAB-001"`, `Color = "أبيض"` | **Partial** — lengths from DB; fabric labels fake | **(a)** Extend `WarehouseDetailingRollDto` with fabric/color display fields |
| `Controls/Workspace/WarehouseDetailingWorkspaceControl.cs` | `LoadInvoice` L154–176 (legacy) | Generates rolls from hardcoded `COL-01`/`TRK-05` arrays; default **`pricePerMeter = 45m`** | **Yes** if called without DB | **(b)** Remove legacy path or guard — detailing page now uses `LoadFromDatabase` only |
| `Services/MockInteractionService.cs` | `OpenInvoiceOperationsCenter` L67–84 | Builds invoice OC from **`SalesSampleData.Generate(20)`** when opening by invoice number (e.g. dashboard double-click) | **Yes** for that navigation path — ignores real `SalesInvoiceListPageControl` rows | **(a)** `SalesUiService.GetOperationsCenterAsync` by invoice number; pass real `FabricSalesInvoiceRow` |
| `Views/OperationsCenters/OperationsCenterFactory.cs` | `BuildSalesInvoice` L310–390 | Invoice operations center: mock overview grid, workflow flags, KPIs «—», container **`CN-2026-001`** | **Partial** — used when entity row is `FabricSalesInvoiceRow` from **sample** path; real list uses different entry | **(a)** Route all invoice OC opens through `SalesUiService.GetOperationsCenterAsync` + dedicated control (like customer/container) |
| `Views/OperationsCenters/OperationsCenterFactory.cs` | `BuildDetailingPanel` L594–608 | Defaults `inv = "INV-000"`, **`cont = "CN-2026-001"`**, `rolls = 5` | **Partial** — only when opened without `SalesInvoice`/`FabricSalesInvoiceRow` | **(a)** Require invoice ID; **(b)** Arabic empty state |

### Customers

| File | Location | What it provides | Breaks screen? | Suggested replacement |
|------|----------|------------------|----------------|----------------------|
| `Controls/Customers/CustomerOperationsCenterControl.cs` | Tabs L103–104 | «فواتير» / «سندات القبض» tabs = `PlaceholderUi.DatabasePhase` | **No** — header/KPIs/statement use DB | **(a)** `GetSalesInvoiceListHandler` filtered by customer; `FinanceUiService` receipts list |
| `Controls/Customers/CustomerOperationsCenterControl.cs` | `OverviewTab` L128–134 | `MockGrid` with KPI rows — **values from DB DTO** but static grid layout | **No** | Keep or replace with structured KPI cards (not mock data issue) |
| `Views/Parties/PartyViews.cs` | `StatementPage` L83–96; `InvoicesPage` L105–112 | Customer submodule «كشف حساب» / «كشف فواتير» — hardcoded ledger rows | **Yes** on those submenu keys (not the OC statement tab) | **(a)** Reuse `CustomerAccountStatementControl` + sales invoice query |

### Dashboard (cross-module, daily use)

| File | Location | What it provides | Breaks screen? | Suggested replacement |
|------|----------|------------------|----------------|----------------------|
| `Modules/DashboardModule.xaml.cs` | `LoadPendingWarehouseTasks` L382–399 | **Hardcoded 3 invoices** (`INV-2026-0088`, `CN-2026-001`, etc.) in «فواتير بانتظار المتابعة» grid | **Yes** — only source for that table (KPI card uses DB) | **(a)** `GetWarehouseDetailingQueueHandler` / `SalesUiService.GetDetailingQueueAsync` |
| `Modules/DashboardModule.xaml.cs` | `LoadSalesInsightCards` L162–187; `DashboardInsightSnapshot.CreateMock` L479–483 | Top/least customer & fabric cards — **fully mock USD insights** | **Yes** for those 4 cards | **(a)** New analytics query on sales invoice lines; **(c)** hide row until handler exists |
| `Modules/DashboardModule.xaml.cs` | `ApplyOperationalDashboard` L76–103 | KPI **trend strings** hardcoded (`↑ 12.5%`, `CN-2026-001`, `2,847`, `32,100 ر.س`) — some overwritten by `LoadKpiCardsAsync` | **Partial** — trends/payables/customers stay fake after DB load | **(a)** Extend `GetDashboardSummaryHandler`; remove static trends |
| `Modules/DashboardModule.xaml` | Activity feed L122–197+ | **Static activity list** (invoice #1045, PO-0088, etc.) | **Yes** | **(a)** Audit log / recent documents query; **(b)** «لا يوجد نشاط حديث» |

### China Import

| File | Location | What it provides | Breaks screen? | Suggested replacement |
|------|----------|------------------|----------------|----------------------|
| `Views/OperationsCenters/OperationsCenterFactory.cs` | `BuildFabric` L217, L221–226 | Fabric OC header **container `CN-2026-001`**; KPIs «محجوز 45 م», «تكلفة/م 42.50» | **Partial** — only if user opens fabric OC from **inventory mock list** | **(a)** `IInventoryRepository` fabric roll metrics |

*Note: China Import primary screens (`ContainerListPageControl`, `ChinaContainerOperationsCenterControl`, import workflow controls) load from `ContainerUiService` — no mock generators found in `Controls/China/*` except DB-backed summary grids.*

---

## 🟡 ACTIVE MOCK — Visible screens / daily navigation

### Dashboard & Shell

| File | Location | Module | What it provides | Breaks screen? | Suggested replacement |
|------|----------|--------|------------------|----------------|----------------------|
| `MainWindow.xaml.cs` | `UpdateStatusBar` L125 | Shell | Status text **«وضع تجريبي — PostgreSQL لاحقاً»** (misleading — DB is active) | **No** | **(b)** «متصل بـ PostgreSQL» from connection health |
| `Shell/TopContextBar.xaml.cs` | Search L35–36 | Shell | Hardcoded search results (`SINV-1026`, `CN-2026-001`) | **Yes** | **(a)** Global search handler; **(c)** «قريباً» |
| `Shell/TopContextBar.xaml.cs` | Notifications L110–111 | Shell | Hardcoded notification bullet list | **Yes** | **(a)** `INotificationService` / dashboard alerts |

### Inventory

| File | Location | What it provides | Breaks screen? | Suggested replacement |
|------|----------|------------------|----------------|----------------------|
| `Views/Inventory/InventoryViews.cs` | `BuildWarehouseEntities` L38–56 | Inline **3 warehouses** (`WH-01`…); list grid | **Yes** | **(a)** `GetWarehouseListHandler` |
| `Views/Inventory/InventoryViews.cs` | `BuildFabricStock` L61–107 | **`FabricInventorySampleData.Generate(40)`** → stock grid | **Yes** | **(a)** `IInventoryRepository` / warehouse stocks query |
| `Views/Inventory/InventoryViews.cs` | `BuildCategories` L111–116 | Hardcoded category grid (قطن/COL-01…) | **Yes** | **(a)** `IFabricCatalogRepository` |
| `Views/Inventory/InventoryViews.cs` | `BuildImportExcel` L118–122 | Sample Excel preview row | **Yes** | **(c)** Upload-only UI until parser wired |
| `Views/Inventory/InventoryViews.cs` | `BuildOpeningStock` L124–133 | Form defaults (`OPN-2026-001`, خالد الشمري…) | **Yes** | **(b)** Empty form + validation |
| `Views/Inventory/InventoryViews.cs` | `BuildStocktake` L135–140 | **2 stocktake sessions** | **Yes** | **(a)** Stocktake handler (new) |
| `Views/Inventory/InventoryViews.cs` | `BuildTransfers` L142–146 | **1 transfer** `TRF-008` | **Yes** | **(a)** Stock movement query |
| `Views/Inventory/InventoryViews.cs` | `BuildSettings` L148 | Text «إعدادات تجريبية» | **No** | **(c)** |
| `Views/Inventory/InventoryViews.cs` | Primary actions L49, L80 | `OpenMockForm` for add warehouse/item | **No** | **(a)** Real forms |

### Sales (secondary screens)

| File | Location | What it provides | Breaks screen? | Suggested replacement |
|------|----------|------------------|----------------|----------------------|
| `Views/Sales/SalesViews.cs` | `InvoiceForm` / `BuildInvoiceView` L67–114 | Full mock invoice form (customers combo, **CN-2026-001** containers, line grid) | **Yes** on «عرض فاتورة بيع» submodule | **(a)** Open `NewSalesInvoiceControl` in read-only or OC |
| `Views/Sales/SalesViews.cs` | `FabricSalesInvoiceRow` L27–28 | Default `Container = "CN-2026-001"` | **Partial** | Remove defaults |
| `Views/Sales/SalesViews.cs` | `BuildReturn` / `BuildReturnsList` L134–145 | Return grid `RET-001` | **Yes** | **(c)** or sales return handler |
| `Views/Sales/SalesViews.cs` | `BuildDelivery` L119–127 | `PlaceholderUi.DatabasePhase` | **No** | **(c)** (already placeholder) |

### Customers & Suppliers (`PartyViews`)

| File | Location | What it provides | Breaks screen? | Suggested replacement |
|------|----------|------------------|----------------|----------------------|
| `Views/Parties/PartyViews.cs` | `SupplierList` L39–52 | **`SupplierSampleData.Generate(20)`** | **Yes** | **(a)** `ISupplierRepository` list handler |
| `Views/Parties/PartyViews.cs` | `PartyForm` L65–78 | Supplier form prefilled «مورد قوانغتشو» / phone | **Yes** | **(a)** Real supplier form control |
| `Views/Parties/PartyViews.cs` | `OpeningBalances` L117–124 | Opening balance grid | **Yes** | **(a)** Opening balance handler |
| `Controls/Customers/CustomerListPageControl.cs` | — | **PostgreSQL via `CustomerUiService`** | — | Reference implementation ✓ |
| `Controls/Customers/CustomerAccountStatementControl.xaml.cs` | `ReloadAsync` | **PostgreSQL via `GetCustomerStatementHandler`** | — | Reference implementation ✓ |

### Accounting (Finance fallback views)

Routed via `SubmoduleViewFactory` L37: `(AppModule.Accounting, _) => FinanceViews.Create(key)` for **Cashboxes, Transfers, Receivables, Payables** (and any unknown key → mock journal list).

| File | Location | What it provides | Breaks screen? | Suggested replacement |
|------|----------|------------------|----------------|----------------------|
| `Views/Finance/FinanceViews.cs` | `JournalList` L30–46 | **`AccountingSampleData.Generate(25)`** | **Yes** if user hits fallback route | Already superseded by `JournalEntryListPageControl` — remove fallback |
| `Views/Finance/FinanceViews.cs` | `BuildCashboxList` L82–103 | **3 hardcoded cashboxes** | **Yes** | **(a)** `FinanceUiService.GetCashboxesAsync` + list page |
| `Views/Finance/FinanceViews.cs` | `VoucherPage` L49–79 | Receipt/payment mock form (`RCP-001`, أحمد الحمصي…) | **Yes** on Finance route | **(a)** Already have `ReceiptVoucherPageControl` / `PaymentVoucherPageControl` — wire submodule keys |
| `Views/Finance/FinanceViews.cs` | `SimpleList` receivables/payables/trial L24–26 | Single-row hardcoded AR/AP/trial grids | **Yes** | **(a)** `ModuleReportRepository` / customer & supplier balances |
| `Views/Finance/FinanceViews.cs` | `Transfers` L23 | One `CashboxTransfer` | **Yes** | **(c)** |

*Connected accounting screens (not mock): `ChartOfAccountsListPageControl`, `JournalEntryListPageControl`, `JournalBookListPageControl`, `TrialBalanceReportControl`, `AccountLedgerReportControl`, `ReceiptVoucherPageControl`, `PaymentVoucherPageControl`, `ModuleReportsViews` hub.*

### Purchases

| File | Location | What it provides | Breaks screen? | Suggested replacement |
|------|----------|------------------|----------------|----------------------|
| `Views/Purchases/PurchasesViews.cs` | `InvoiceList` L26–39 | **`PurchaseSampleData.Generate(30)`** | **Yes** | **(a)** `IPurchaseInvoiceRepository` |
| `Views/Purchases/PurchasesViews.cs` | `FormPage` L43–53 | PO/return forms with hardcoded supplier combo | **Yes** | **(c)** |

### Operations Centers (mock entity fallbacks & tabs)

| File | Location | Entity | What it provides | Breaks screen? | Suggested replacement |
|------|----------|--------|------------------|----------------|----------------------|
| `OperationsCenterFactory.cs` | `BuildSupplier` L117–174 | Supplier | **`SupplierSampleData` fallback**; mock invoice/payment/container tabs; KPI «95,000» | **Partial** | **(a)** Supplier OC handler |
| `OperationsCenterFactory.cs` | `BuildFabric` L197–252 | Fabric | Mock movements grid; fake KPIs | **Yes** when opened | **(a)** Inventory OC |
| `OperationsCenterFactory.cs` | `BuildWarehouse` L255–307 | Warehouse | **`new WarehouseEntity { WH-01…}`** fallback; mock tabs | **Yes** | **(a)** `GetWarehouseListHandler` + stock |
| `OperationsCenterFactory.cs` | `BuildPurchaseInvoice` L393–434 | Purchase | **`PurchaseSampleData` fallback**; mock line grid | **Yes** | **(a)** Purchase invoice handler |
| `OperationsCenterFactory.cs` | `BuildEmployee` L437–481 | HR | **`HRSampleData` fallback**; mock attendance | **Yes** | **(c)** |
| `OperationsCenterFactory.cs` | `BuildJournal` L484–519 | Journal | **`AccountingSampleData` fallback**; mock journal lines | **Partial** | **(a)** `GetJournalEntry` details handler |
| `OperationsCenterFactory.cs` | `BuildCashbox` L544–576 | Cashbox | **`new Cashbox { CB-01, 125000 }`**; mock movements | **Yes** | **(a)** Cashbox ledger from GL |
| `OperationsCenterFactory.cs` | `TimelineMock` L655–658 | All | Fake timeline rows | **Yes** | **(a)** `IAuditLogRepository` |
| `OperationsCenterFactory.cs` | `OverviewSupplier` L628–631 | Supplier | Static overview grid | **Yes** | **(a)** Supplier analytics |
| `Controls/OperationsCenter/OperationsCenterShell.cs` | Quick actions L279 | All | `ShowComingSoon` for unmapped actions | **No** | Wire `actionKey` routes |

### Workspace / Generic actions

| File | Location | What it provides | Breaks screen? | Suggested replacement |
|------|----------|------------------|----------------|----------------------|
| `Controls/Workspace/ActionWorkspaceView.xaml.cs` | `BuildMovementGrid` L231–238 | Fabric movement **2 hardcoded rows** (`CN-2026-001`) | **Yes** when action = `FabricMovement` | **(a)** Stock movement query |
| `Controls/Workspace/ActionWorkspaceView.xaml.cs` | `GetTableData` L263–267 | Generic **2-line** grid | **Yes** for generic actions | **(b)** or entity-specific handlers |
| `Controls/Workspace/ActionWorkspaceView.xaml.cs` | `BuildInfoFields` default L188 | «بيانات تجريبية» | **Partial** | Entity-specific DTOs |

### Reports (global module)

| File | Location | What it provides | Breaks screen? | Suggested replacement |
|------|----------|------------------|----------------|----------------------|
| `Views/Reports/ReportViews.cs` | `BuildExecutiveDashboard` L32–38 | KPI cards all **«—»** | **No** | **(a)** Executive summary handler |

*Per-module report hubs (`ModuleReportsViews` + `ModuleReportRepository`) use **real PostgreSQL queries** — not counted as mock.*

### HR

| File | Location | What it provides | Breaks screen? | Suggested replacement |
|------|----------|------------------|----------------|----------------------|
| `Views/Hr/HrViews.cs` | `EmployeeList` L32–46 | **`HRSampleData.Generate(20)`** | **Yes** | **(a)** Employee repository |
| `Views/Hr/HrViews.cs` | `Placeholder` tabs L18–25 | Departments, attendance, payroll… inline arrays | **Yes** | **(c)** |
| `Views/Hr/HrViews.cs` | `Reports` | Routes to `ModuleReportsViews` (DB-backed `hr.employees` report) | **Partial** | OK for list report |

### Settings

| File | Location | What it provides | Breaks screen? | Suggested replacement |
|------|----------|------------------|----------------|----------------------|
| `Views/Settings/SettingsViews.cs` | `BuildSettingsForm` L169–172 | All 13 sections: **«قيمة تجريبية»** fields | **Yes** | **(a)** Settings aggregate from DB seed/config |
| `Views/Settings/SettingsViews.cs` | `BuildHub` | Navigation only — no business data | **No** | — |

### Mock interaction infrastructure (UX stubs, not business lists)

| File | Role | Suggested replacement |
|------|------|----------------------|
| `Services/MockInteractionService.cs` | Navigation, dialogs, **`OpenMockForm`**, document preview window | Keep navigation; replace `OpenMockForm` with real forms; preview → real PDF service |
| `Services/MockQuickActionRouter.cs` | Fake success / `ShowComingSoon` / `OpenMockForm` for OC quick actions | Map each `actionKey` to real command |
| `Dialogs/DocumentPreviewWindow.xaml.cs` | Print/PDF/Excel preview shell — no document body | **(a)** `IDocumentPreviewService` implementation |
| `Dialogs/MockFeedbackDialog.xaml.cs` | Toast/dialog UI | Keep (not data) |
| `Helpers/PlaceholderUi.cs` | `DatabasePhase`, `MockGrid`, `TabContent` helpers | Keep for intentional placeholders |

### Expenses (minor)

| File | Location | What it provides | Breaks screen? | Suggested replacement |
|------|----------|------------------|----------------|----------------------|
| `Controls/Expenses/ExpenseEntryFormControl.cs` | `_exchangeRate` default L59 | Form default **`15000`** (likely wrong for USD base) | **Partial** | Default from company settings |
| `Controls/Expenses/ExpenseOperationsCenterControl.cs` | Attachments L199 | `DatabasePhase` when no attachment | **No** | **(b)** «لم يُرفع مرفق» |

---

## ⚪ DORMANT — Placeholder / unused / dead code

| File | Location | Notes | Suggested replacement |
|------|----------|-------|----------------------|
| `Core/Customers/CustomerModels.cs` | `CustomerSampleData` L316+ | **Generator defined, never called** | Delete when cleaning or keep for demos |
| `Core/Sales/SalesModels.cs` | `SalesSampleData` L203+ | Used only by `MockInteractionService.OpenInvoiceOperationsCenter` | Remove after OC fix |
| `Core/Inventory/FabricInventoryModels.cs` | `FabricInventorySampleData` L27+ | Only `InventoryViews.BuildFabricStock` | Remove after inventory list wired |
| `Core/Suppliers/SupplierModels.cs` | `SupplierSampleData` | `PartyViews` + OC fallback | Remove after supplier API |
| `Core/Purchases/PurchaseModels.cs` | `PurchaseSampleData` | Purchases + OC fallback | Remove after purchase API |
| `Core/Accounting/AccountingModels.cs` | `AccountingSampleData` | FinanceViews + OC journal fallback | Remove after routing fix |
| `Core/HR/HRModels.cs` | `HRSampleData` | HR views + OC fallback | Remove when HR planned |
| `Views/OperationsCenters/OperationsCenterFactory.cs` | `OverviewCustomer` L617–625 | **Dead code** — customer OC uses `CustomerOperationsCenterControl` | Delete |
| `ERPSystem.Application/.../OperationsQueryHandlers.cs` | `GetReportPreviewHandler` L224–229 | Returns `PreviewNotImplemented` for unknown codes | Backend stub — UI does not call from WPF yet |
| `ERPSystem.Infrastructure/.../InfrastructureServices.cs` | `NullDocumentPreviewService` | No-op preview service | Implement when printing pipeline ready |
| `Core/Actions/EntityActionRegistry.cs` | `ContainerDelete` «حذف تجريبي» L69 | Label only | Rename when real delete exists |
| `Controls/Sales/NewSalesInvoiceControl.xaml` | «تسليم — قريباً» L242 | Disabled UI hint | **(c)** |
| `Resources/Themes/EnterpriseTheme.xaml` | Comment L36 | «mockup navy» — theme only | N/A |
| `Modules/DashboardModule.xaml` | Comment L84 | Documents mock sales insights | Remove comment after wiring |
| `Helpers/ErpListNavigation.cs` | Uses `MockInteractionService.Navigate` | Navigation helper | N/A |
| `Services/ApplicationResultPresenter.cs` | Uses `MockFeedbackDialog` | Error presentation | N/A |
| `Views/Purchases/PurchasesViews.cs` | `Orders`/`Returns` `FormPage` | Empty forms, no grid data | **(c)** |
| `Views/Sales/SalesViews.cs` | `BuildDelivery` | Already explicit placeholder | **(c)** |

---

## Sample Data Generators — Quick Reference

| Class | File | Consumed by |
|-------|------|-------------|
| `CustomerSampleData` | `Core/Customers/CustomerModels.cs` | *Unused* |
| `SalesSampleData` | `Core/Sales/SalesModels.cs` | `MockInteractionService.OpenInvoiceOperationsCenter` |
| `FabricInventorySampleData` | `Core/Inventory/FabricInventoryModels.cs` | `InventoryViews.BuildFabricStock` |
| `SupplierSampleData` | `Core/Suppliers/SupplierModels.cs` | `PartyViews.SupplierList`, `OperationsCenterFactory.BuildSupplier` |
| `PurchaseSampleData` | `Core/Purchases/PurchaseModels.cs` | `PurchasesViews`, `OperationsCenterFactory.BuildPurchaseInvoice` |
| `AccountingSampleData` | `Core/Accounting/AccountingModels.cs` | `FinanceViews.JournalList`, `OperationsCenterFactory.BuildJournal` |
| `HRSampleData` | `Core/HR/HRModels.cs` | `HrViews.EmployeeList`, `OperationsCenterFactory.BuildEmployee` |

---

## Recommended Cleanup Order

1. **🔴 Sales invoice line defaults & detailing display** — `12.5` / `FAB-001` / fake fabric labels (user-visible on every new invoice).
2. **🔴 Dashboard pending detailing table** — replace hardcoded grid with `GetDetailingQueueAsync`.
3. **🔴 `OpenInvoiceOperationsCenter`** — stop using `SalesSampleData`; load from DB.
4. **🔴 Dashboard sales insight cards** — remove `CreateMock()` or back with analytics query.
5. **🟡 Route Accounting Cashboxes/AR/AP** to real controls (stop `FinanceViews` fallback).
6. **🟡 Inventory module** — warehouses + fabric stock lists from `IInventoryRepository`.
7. **🟡 Suppliers & Purchases lists** — repository handlers.
8. **🟡 Operations Center factories** — replace `*SampleData` fallbacks with «اختر سجل من القائمة» empty states.
9. **⚪ Delete unused `CustomerSampleData` and dead `OverviewCustomer`.**

---

## Modules — Connection Status Matrix

| Module | Primary UI data source | Mock still present? |
|--------|------------------------|---------------------|
| **Customers** | PostgreSQL (`CustomerUiService`) | Submodule statement/invoices pages; OC invoice tab placeholder |
| **Sales** | PostgreSQL (list, draft, detailing save) | Default line fields; invoice view submodule; OC via sample; dashboard table |
| **China Import** | PostgreSQL (`ContainerUiService`) | Fabric OC if opened from mock inventory |
| **Detailing** | PostgreSQL (queue + `CompleteWarehouseDetailing`) | Fabric label display; price fallback `12.5` |
| **Accounting** | PostgreSQL (chart, journal, vouchers, reports) | Cashboxes/AR/AP/Transfers via `FinanceViews` |
| **Expenses** | PostgreSQL | Attachment empty state only |
| **Capital** | PostgreSQL | — |
| **Inventory** | **Mock generators** | Entire submodule except Reports hub (DB) |
| **Suppliers** | **Mock** | List + forms + OC |
| **Purchases** | **Mock** | List + forms |
| **HR** | **Mock** | All screens |
| **Settings** | **Mock** | All forms |
| **Dashboard** | **Mixed** | KPI partial DB; insights + activity + pending table mock |
| **Reports (global)** | Placeholder executive KPIs | Per-module hubs use DB |

---

## Verification Notes

- **No code was modified** in this audit pass — build status unchanged.
- **Excluded from mock audit:** `DatabaseSeeder` seed values (intentional dev data), EF migrations, unit test fixtures (none found in scan), and form control empty-string defaults (`FormField("")`).
- **Not mock:** `MockInteractionService` navigation/confirm/warning calls that only show UX feedback without fabricating business numbers.

---

*Generated by static codebase audit. Re-run after each vertical slice migration to shrink 🔴 CRITICAL section.*
