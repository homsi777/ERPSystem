# ERP PRO — Project Knowledge Base

> **Document purpose:** Permanent technical reference for senior architects.  
> **Source of truth:** Repository source code and existing `docs/` audit files as of 2026-07-05.  
> **Rule:** Nothing below is invented. Unknown items are marked explicitly.

---

## Table of Contents

1. [Project Overview](#1-project-overview)
2. [Technology Stack](#2-technology-stack)
3. [Solution Structure](#3-solution-structure)
4. [Folder Structure](#4-folder-structure)
5. [ERP Modules](#5-erp-modules)
6. [Navigation Map](#6-navigation-map)
7. [Screen Catalog](#7-screen-catalog)
8. [Business Engines](#8-business-engines)
9. [Services](#9-services)
10. [Workflows](#10-workflows)
11. [Database](#11-database)
12. [Security](#12-security)
13. [UI Architecture](#13-ui-architecture)
14. [Current Progress](#14-current-progress)
15. [Technical Debt](#15-technical-debt)
16. [Future Roadmap](#16-future-roadmap)
17. [Architecture Rules](#17-architecture-rules)
18. [Coding Standards](#18-coding-standards)
19. [Glossary](#19-glossary)
20. [Overall Project Health](#20-overall-project-health)

---

## 1. Project Overview

### Project name
**ERP PRO** (assembly / window title: `ERP PRO`; repository folder: `ERPSystem`)

### Purpose
Desktop ERP for a **fabric / textile trading business** with China import, roll-based inventory, sales detailing, integrated accounting, expenses, capital partners, and operational reporting. UI is **Arabic-first (RTL, `ar-SA`)**.

### ERP vision
Single integrated system covering:
- Import containers from China → landing cost → warehouse activation
- Roll/meter inventory with reservations, transfers, stocktake, opening stock
- Sales with warehouse detailing workflow
- Purchases, customers, suppliers, GL, vouchers, cashboxes
- Expenses, capital partners, HR (partial), settings, executive reports

Architecture intent: **Clean Architecture** (Domain → Application → Infrastructure) with **CQRS-style handlers**, **domain aggregates**, and **centralized engines** for cross-cutting business rules (inventory, accounting, opening balances).

### Business domains
| Domain | AppModule |
|--------|-----------|
| Executive dashboard | `Dashboard` |
| China fabric import | `ChinaImport` |
| Sales & returns | `Sales` |
| Purchases | `Purchases` |
| Inventory & warehouses | `Inventory` |
| Customers (AR) | `Customers` |
| Suppliers (AP) | `Suppliers` |
| Accounting & finance | `Accounting` |
| Expenses | `Expenses` |
| Capital / partners | `CapitalPartners` |
| Cross-module reports | `Reports` |
| Human resources | `HR` |
| System settings | `Settings` |

### Current development stage
**Active mid-production.** Core vertical slices (customers, suppliers, sales, purchases, inventory engine, expenses, capital, accounting GL, unified opening balances module) have real PostgreSQL persistence and handlers. Many submodules remain **placeholders** or **partial** (see `docs/module-connection-status.md` and Section 14).

Build policy: **`TreatWarningsAsErrors` = true** on main projects; last verified build target is **0 errors / 0 warnings**.

### Overall architecture philosophy
1. **Domain-centric:** Business rules and state machines live in `ERPSystem.Domain` (aggregates, entities, enums).
2. **Application orchestration:** Commands/queries + handlers in `ERPSystem.Application`; no EF or WPF references.
3. **Infrastructure implementation:** EF Core + PostgreSQL, repositories, engines, numbering, seeding.
4. **WPF presentation shell:** Code-behind `UserControl` screens + `*UiService` facades that dispatch to scoped handlers via `IServiceScopeFactory`.
5. **Engines for shared posting logic:** `IInventoryEngine`, `IIntegratedAccountingService`, `IOpeningBalanceEngine` — intended single paths for inventory movements, journals, and opening balances.
6. **Operations Center pattern:** Modal/tabbed workspace per document (invoice, expense, partner, opening balance, etc.) built on `OperationsCenterShell`.

---

## 2. Technology Stack

| Category | Technology |
|----------|------------|
| Language | C# |
| Runtime / SDK | **.NET 9** (`net9.0`, `net9.0-windows` for WPF) |
| UI framework | **WPF** (`UseWPF=true`) |
| Database | **PostgreSQL** (`Host=localhost;Database=erp_pro` in `appsettings.json`) |
| ORM | **Entity Framework Core 9.0.6** |
| PostgreSQL provider | **Npgsql.EntityFrameworkCore.PostgreSQL 9.0.4** |
| Architecture pattern | Clean Architecture + CQRS handlers (not MediatR package — custom `ICommandHandler` / `IQueryHandler`) |
| Dependency injection | **Microsoft.Extensions.DependencyInjection 9.0.6** |
| Configuration | `appsettings.json`, environment variables, `IConfiguration` |
| Logging | `Microsoft.Extensions.Logging` + Console provider |
| Excel (import/export) | **ClosedXML 0.104.2**, **NPOI 2.7.2** (Application layer) |
| PDF (WPF app) | **QuestPDF 2024.12.3** (main `ERPSystem.csproj`) |
| HTML document engine | **ERPSystem.DocumentEngine** (standalone; embedded CSS; HTML/print/PDF via `IPdfConverter` — **not referenced by WPF project**) |
| Serialization | System.Text.Json (implicit); EF value converters in Infrastructure |
| Localization | `LocalizationManager`, `ar-SA` default in `App.xaml.cs` |
| Migrations | EF Core migrations in `ERPSystem.Infrastructure/Migrations/` |
| Document numbering | `PostgreSqlNumberingService` (`INumberingService`) |
| Third-party (WPF) | QuestPDF, EF Core Relational (transitive in WPF for tooling) |
| Third-party (Infrastructure) | EF Core Design/Tools, Npgsql |
| Third-party (Application) | ClosedXML, NPOI |
| Solution file | **Unable to determine from current source** — no `.sln` found in repo root; projects are referenced via `ProjectReference` in `.csproj` files |

---

## 3. Solution Structure

### 3.1 `ERPSystem` (WPF host — presentation)
| Attribute | Detail |
|-----------|--------|
| **Responsibility** | WPF shell: `MainWindow`, modules, controls, views, UI services, themes, dialogs |
| **Target** | `net9.0-windows` WinExe |
| **Dependencies** | `ERPSystem.Application`, `ERPSystem.Infrastructure` |
| **Why it exists** | Desktop client and composition root for UI + DI bootstrap (`App.xaml.cs`) |
| **Main namespaces** | `ERPSystem`, `ERPSystem.Modules`, `ERPSystem.Views.*`, `ERPSystem.Controls.*`, `ERPSystem.Services.*`, `ERPSystem.Core`, `ERPSystem.Shell`, `ERPSystem.Helpers`, `ERPSystem.Dialogs` |

**Note:** `ERPSystem.DocumentEngine/**` is explicitly **excluded** from WPF compile (`ERPSystem.csproj` `Compile Remove`).

### 3.2 `ERPSystem.Domain`
| Attribute | Detail |
|-----------|--------|
| **Responsibility** | Entities, aggregates, value objects, domain enums, domain exceptions |
| **Dependencies** | None (pure .NET class library) |
| **Why it exists** | Core business model independent of UI and persistence |
| **Main namespaces** | `ERPSystem.Domain.Entities.*`, `ERPSystem.Domain.Aggregates`, `ERPSystem.Domain.Enums`, `ERPSystem.Domain.Exceptions`, `ERPSystem.Domain.ValueObjects` |

### 3.3 `ERPSystem.Application`
| Attribute | Detail |
|-----------|--------|
| **Responsibility** | Commands, queries, DTOs, handler interfaces, use-case handlers, mapping, validation orchestration |
| **Dependencies** | `ERPSystem.Domain`; ClosedXML, NPOI |
| **Why it exists** | Application layer / use cases without infrastructure details |
| **Main namespaces** | `ERPSystem.Application.Commands.*`, `ERPSystem.Application.Queries.*`, `ERPSystem.Application.DTOs.*`, `ERPSystem.Application.UseCases.*`, `ERPSystem.Application.Abstractions.*`, `ERPSystem.Application.Mapping`, `ERPSystem.Application.Results` |

### 3.4 `ERPSystem.Infrastructure`
| Attribute | Detail |
|-----------|--------|
| **Responsibility** | EF Core `ErpDbContext`, persistence models, repositories, engines, migrations, seeding, numbering |
| **Dependencies** | `ERPSystem.Application`, `ERPSystem.Domain`; EF Core, Npgsql |
| **Why it exists** | Technical implementation of persistence and external services |
| **Main namespaces** | `ERPSystem.Infrastructure.Persistence`, `ERPSystem.Infrastructure.Repositories`, `ERPSystem.Infrastructure.Services`, `ERPSystem.Infrastructure.Configurations`, `ERPSystem.Infrastructure.Migrations`, `ERPSystem.Infrastructure.Seed`, `ERPSystem.Infrastructure.Numbering` |

### 3.5 `ERPSystem.DocumentEngine`
| Attribute | Detail |
|-----------|--------|
| **Responsibility** | Standalone HTML/CSS document rendering (templates, components, renderers) |
| **Dependencies** | None on WPF/EF/Application (per project comments) |
| **Why it exists** | Host-agnostic printable documents (invoices, statements, vouchers, reports) |
| **Main namespaces** | `ERPSystem.DocumentEngine.Models`, `ERPSystem.DocumentEngine.Templates.*`, `ERPSystem.DocumentEngine.Renderer`, `ERPSystem.DocumentEngine.Services`, `ERPSystem.DocumentEngine.Components` |
| **WPF integration** | **Not implemented yet** — not referenced by main WPF project |

### 3.6 `tools/*` (utility projects)
| Project | Purpose |
|---------|---------|
| `ImportCatalogCleanup` | Development DB cleanup utility |
| `ChinaImportCatalogTest` | China import catalog testing |
| `PackingListParseDiag` | Packing list parse diagnostics |

---

## 4. Folder Structure

### WPF project (`ERPSystem/`)

| Folder | Purpose |
|--------|---------|
| `App.xaml` / `App.xaml.cs` | Application entry, DI bootstrap, migration+seed on startup |
| `MainWindow.xaml(.cs)` | Host window; swaps module `UserControl`s in `WorkspaceHost` |
| `Modules/` | Top-level module shells (`*Module.xaml`) — one per `AppModule` |
| `Views/` | Static view factories (`*Views.cs`, `SubmoduleViewFactory`) — map route key → control |
| `Controls/` | Screen implementations (lists, forms, dashboards, operations centers) |
| `Controls/OperationsCenter/` | Reusable OC shell (`OperationsCenterShell`, models) |
| `Services/` | WPF-side `*UiService`, popup services, action routers, context menus |
| `Core/` | `AppModule`, navigation (`NavigationStateManager`, `SubmoduleRegistry`), actions (`EntityActionRegistry`), workspace |
| `Core/LocalizationManager.cs` | RTL/LTR flow direction and language change events |
| `Shell/` | Top navigation bars (`TopNavBar`, `TopContextBar`) |
| `Helpers/` | `ErpUiFactory`, `ErpUxFactory`, design tokens, placeholders |
| `Dialogs/` | Modal windows (`ErpModalWindow`, confirmation dialogs) |
| `ViewModels/Base/` | `ViewModelBase`, `RelayCommand` — **minimal usage** (see Section 13) |
| `Resources/Themes/` | `EnterpriseTheme.xaml` — global brushes, button styles |
| `Documentation/` | Project knowledge base (this file) |
| `docs/` | Historical audits and implementation reports |

### Application layer folders

| Folder | Purpose |
|--------|---------|
| `Abstractions/Repositories/` | Repository interfaces |
| `Abstractions/Services/` | Engine and cross-cutting service interfaces |
| `Commands/` | Write model per bounded context |
| `Queries/` | Read model per bounded context |
| `DTOs/` | Data transfer objects for UI/API |
| `UseCases/` | Handler implementations (`*Handlers.cs`) |
| `Mapping/` | Domain ↔ DTO mappers |
| `Results/` | `ApplicationResult`, validation errors |
| `DependencyInjection/` | `ApplicationServiceCollectionExtensions` |

### Domain layer folders

| Folder | Purpose |
|--------|---------|
| `Entities/` | Entities grouped by bounded context (Finance, Inventory, Parties, etc.) |
| `Aggregates/` | Aggregate roots (e.g. `SalesInvoiceAggregate`, `ContainerAggregate`) |
| `Enums/` | Status enums, document types, movement types |
| `ValueObjects/` | Typed values (amounts, numbers) |
| `Exceptions/` | Domain/inventory exceptions |

### Infrastructure layer folders

| Folder | Purpose |
|--------|---------|
| `Persistence/Models/` | EF entity classes (persistence models, not domain) |
| `Persistence/Mapping/` | Domain ↔ persistence mappers |
| `Configurations/` | Fluent EF configurations |
| `Repositories/` | Repository implementations |
| `Services/` | Engine implementations (`InventoryEngine`, `OpeningBalanceEngine`, etc.) |
| `Migrations/` | EF migrations + snapshot |
| `Seed/` | `DatabaseSeeder`, module seeders |
| `Numbering/` | `PostgreSqlNumberingService` |
| `DependencyInjection/` | `InfrastructureServiceCollectionExtensions` |

### DocumentEngine folders

| Folder | Purpose |
|--------|---------|
| `Assets/css/` | Design system stylesheets |
| `Templates/` | Document templates per type |
| `Renderer/` | HTML, print, PDF renderers |
| `Components/` | HTML building blocks |
| `Models/` | `DocumentModel`, branding, labels |

---

## 5. ERP Modules

Status legend: **✅ Completed** | **🟡 In Progress / Partial** | **⬜ Planned / Placeholder**

Detailed submodule DB connectivity audit: `docs/module-connection-status.md` (2026-07-03).

---

### 5.1 Dashboard (`AppModule.Dashboard`)

| Attribute | Detail |
|-----------|--------|
| **Purpose** | Executive overview: KPIs, operational tables, quick navigation |
| **Status** | 🟡 Partial — KPIs via `GetDashboardSummaryHandler`; some placeholders (`—` trends) |
| **Screens** | `Modules/DashboardModule.xaml` |
| **Services** | Direct handler dispatch from module; `MockInteractionService` for navigation |
| **Engines** | None directly |
| **Workflow** | Read-only aggregation |
| **Dependencies** | Application queries, multiple repositories |

---

### 5.2 China Import (`AppModule.ChinaImport`)

| Attribute | Detail |
|-----------|--------|
| **Purpose** | Import fabric containers: Excel parse, landing cost, warehouse move, sale readiness |
| **Status** | 🟡 Partial — core import/list/approve strong; distribution/stocktake partial |
| **Screens** | See [China Import screens](#72-china-import) |
| **Services** | `ContainerUiService`, `ChinaContainerQuickActionRouter` |
| **Engines** | `IInventoryEngine.PostContainerImportAsync`, `IIntegratedAccountingService` (container approval, inventory activation) |
| **Workflow** | `ChinaContainerStatus` lifecycle (domain) |
| **Dependencies** | Inventory, accounting, fabric catalog |

---

### 5.3 Sales (`AppModule.Sales`)

| Attribute | Detail |
|-----------|--------|
| **Purpose** | Sales invoices, returns, warehouse detailing, delivery |
| **Status** | 🟡 Partial — invoice CRUD/approve/post connected; some views placeholder |
| **Screens** | See [Sales screens](#73-sales) |
| **Services** | `SalesUiService`, `SalesReturnUiService`, `SalesPopupService`, `SalesDocumentService` |
| **Engines** | `IInventoryEngine` (reserve, issue, detailing, returns), `IIntegratedAccountingService` |
| **Workflow** | `SalesInvoiceStatus`: Draft → AwaitingDetailing → Detailed → ReadyForApproval → Approved → Printed → Delivered (+ returns/cancel) |
| **Dependencies** | Inventory, customers, accounting, China containers (optional) |

---

### 5.4 Purchases (`AppModule.Purchases`)

| Attribute | Detail |
|-----------|--------|
| **Purpose** | Purchase invoices, orders, returns |
| **Status** | ✅ Mostly complete per audit |
| **Screens** | Invoice/order/return list + forms, purchase OC |
| **Services** | `PurchaseUiService`, `PurchaseDocumentService` |
| **Engines** | `IPurchaseInventoryService` → `IInventoryEngine`, `IIntegratedAccountingService` |
| **Workflow** | `PurchaseInvoiceStatus`, `PurchaseOrderStatus`, `PurchaseReturnStatus` |
| **Dependencies** | Suppliers, inventory, accounting |

---

### 5.5 Inventory (`AppModule.Inventory`)

| Attribute | Detail |
|-----------|--------|
| **Purpose** | Warehouses, fabric stock, transfers, stocktake, opening stock, catalog |
| **Status** | ✅ Full — warehouses CRUD, opening stock, transfers, stocktake, fabric balances; settings/import Excel remain placeholder |
| **Screens** | See [Inventory screens](#74-inventory) |
| **Services** | `InventoryUiService`, `InventoryCatalogUiService`, `InventoryPopupService` |
| **Engines** | **`IInventoryEngine`** (central), `IInventoryOperationsService` |
| **Workflow** | `InventoryDocumentStatus` for transfers, stocktake, opening stock |
| **Dependencies** | Fabric catalog, accounting (valuation snapshots) |

---

### 5.6 Customers (`AppModule.Customers`)

| Attribute | Detail |
|-----------|--------|
| **Purpose** | Customer master, AR opening balance, statements, invoice drill-down |
| **Status** | ✅ Full — list/CRUD/statement/opening balance connected via unified `IOpeningBalanceEngine` |
| **Screens** | List, form, opening balance, statement, invoices |
| **Services** | `CustomerUiService`, `OpeningBalanceUiService` (party opening balance post), `CustomerActionRouter` |
| **Engines** | `IOpeningBalanceEngine.PostPartyOpeningBalanceAsync` → `OpeningBalanceDocument` |
| **Workflow** | `CustomerStatus`; `OpeningBalancePosted` flag on customer |
| **Dependencies** | Accounting, sales, opening balances |

---

### 5.7 Suppliers (`AppModule.Suppliers`)

| Attribute | Detail |
|-----------|--------|
| **Purpose** | Supplier master, AP opening balance, statements, purchase invoices |
| **Status** | ✅ Full — list/CRUD/statement/opening balance/invoices connected via unified engine |
| **Services** | `SupplierUiService`, `OpeningBalanceUiService` (party opening balance post), `SupplierActionRouter` |
| **Engines** | `IOpeningBalanceEngine.PostPartyOpeningBalanceAsync` |
| **Dependencies** | Purchases, accounting |

---

### 5.8 Accounting (`AppModule.Accounting`)

| Attribute | Detail |
|-----------|--------|
| **Purpose** | Chart of accounts, journals, books, vouchers, aging, trial balance, **unified opening balances** |
| **Status** | 🟡 Partial — GL core strong; some finance sub-routes use `FinanceViews` placeholders |
| **Screens** | Accounting + finance controls (see catalogs) |
| **Services** | `AccountingUiService`, `FinanceUiService`, `OpeningBalanceUiService` |
| **Engines** | `IIntegratedAccountingService`, **`IOpeningBalanceEngine`** |
| **Workflow** | `JournalEntryStatus`, `VoucherStatus`, opening balance workflow (Section 10) |
| **Dependencies** | All posting modules |

---

### 5.9 Expenses (`AppModule.Expenses`)

| Attribute | Detail |
|-----------|--------|
| **Purpose** | Expense definitions, entries, payments, installments, categories |
| **Status** | ✅ Mostly complete |
| **Services** | `ExpenseUiService`, `ExpensePopupService`, `ExpenseQuickActionRouter`, `ExpenseContextMenuService` |
| **Engines** | `IIntegratedAccountingService.PostExpensePaymentAsync` |
| **Workflow** | `ExpenseStatus` (Section 10) |
| **Dependencies** | Cashboxes, accounting |

---

### 5.10 Capital Partners (`AppModule.CapitalPartners`)

| Attribute | Detail |
|-----------|--------|
| **Purpose** | Partners, investments, withdrawals, profit distributions |
| **Status** | ✅ Mostly complete |
| **Services** | `CapitalPartnerUiService`, `CapitalPartnerPopupService`, `CapitalPartnerQuickActionRouter` |
| **Engines** | Accounting integration for capital movements |
| **Workflow** | `PartnerStatus`, `CapitalApprovalStatus` |
| **Dependencies** | Accounting, cashboxes |

---

### 5.11 Reports (`AppModule.Reports`)

| Attribute | Detail |
|-----------|--------|
| **Purpose** | Executive BI dashboard + per-module report hubs |
| **Status** | 🟡 Partial — module report hubs connected; executive KPIs partially placeholder |
| **Services** | `ModuleReportUiService` |
| **Engines** | None — `IModuleReportRepository` |
| **Dependencies** | All modules (read models) |

---

### 5.12 HR (`AppModule.HR`)

| Attribute | Detail |
|-----------|--------|
| **Purpose** | Employees, departments, attendance, payroll, etc. |
| **Status** | 🟡 Partial — **Employees + Departments implemented**; other submodules are `DevelopmentTab` placeholders |
| **Services** | `HrUiService` |
| **Engines** | Not implemented yet |
| **Dependencies** | `IHrRepository` / department & employee repositories |

---

### 5.13 Settings (`AppModule.Settings`)

| Attribute | Detail |
|-----------|--------|
| **Purpose** | Company identity, branches, users, locale, finance settings, numbering, etc. |
| **Status** | 🟡 Partial — **Company, Finance, Numbering, Branches** have real `SettingsUiService` persistence; most other sections show `PlaceholderUi.DevelopmentPhase` |
| **Services** | `SettingsUiService`, `CurrencyCatalog` |
| **Engines** | None |
| **Dependencies** | `ISystemSettingsRepository`, `IBranchRepository` |

---

## 6. Navigation Map

### 6.1 Top-level navigation
`MainWindow` hosts one `UserControl` module per `AppModule` (see `MainWindow.xaml.cs`).  
`NavigationStateManager` maintains module + subpage + back stack.  
`Shell/TopNavBar` + `TopContextBar` provide chrome.

### 6.2 Submodule resolution
`SubmoduleRegistry.Get(module)` → list of `SubmoduleDef(Key, LabelAr, IconGlyph)`  
`ModuleShellControl` renders active submodule via `SubmoduleViewFactory.Create(module, key)`.

### 6.3 Full menu map

| Main Module | Submodule Key | Label (AR) | Primary View Factory |
|-------------|---------------|------------|----------------------|
| Dashboard | — | لوحة التحكم | `DashboardModule` |
| ChinaImport | Containers | قائمة الحاويات | `ChinaViews` |
| ChinaImport | NewImport | استيراد حاوية جديدة | `ChinaViews` |
| ChinaImport | FileAnalysis | تحليل الملف | `ChinaViews` |
| ChinaImport | Distribution | توزيع الكميات | `ChinaViews` |
| ChinaImport | Stocktake | جرد الحاوية | `ChinaViews` |
| ChinaImport | LandingCost | ملخص تكلفة الاستيراد | `ChinaViews` |
| ChinaImport | Reports | التقارير | `ModuleReportsViews` |
| Inventory | Dashboard | لوحة المخزون | `InventoryViews` |
| Inventory | Warehouses | المستودعات | `InventoryViews` |
| Inventory | Categories | التصنيفات | `InventoryViews` |
| Inventory | ImportExcel | استيراد Excel | `InventoryViews` (placeholder) |
| Inventory | OpeningStock | مواد أول مدة | `InventoryViews` |
| Inventory | Stocktake | الجرد | `InventoryViews` |
| Inventory | Transfers | المناقلات | `InventoryViews` |
| Inventory | Settings | إعدادات المخزون | `InventoryViews` (placeholder) |
| Inventory | Reports | التقارير | `ModuleReportsViews` |
| Sales | NewInvoice | فاتورة بيع جديدة | `SalesViews` |
| Sales | Invoices | قائمة فواتير البيع | `SalesViews` |
| Sales | InvoiceView | عرض فاتورة بيع | `SalesViews` (placeholder without context) |
| Sales | NewReturn | مرتجع بيع جديد | `SalesViews` |
| Sales | Returns | قائمة مرتجعات البيع | `SalesViews` |
| Sales | Detailing | تفصيل الأطوال | `SalesViews` |
| Sales | Delivery | التسليم | `SalesViews` |
| Sales | Reports | التقارير | `ModuleReportsViews` |
| Customers | List | سجل العملاء | `PartyViews` |
| Customers | Form | إضافة / تعديل عميل | `PartyViews` |
| Customers | Opening | أرصدة افتتاحية | `PartyViews` |
| Customers | Statement | كشف حساب عميل | `PartyViews` |
| Customers | Invoices | كشف فواتير عميل | `PartyViews` |
| Customers | Reports | التقارير | `ModuleReportsViews` |
| Suppliers | List | سجل الموردين | `PartyViews` |
| Suppliers | Form | إضافة / تعديل مورد | `PartyViews` |
| Suppliers | Opening | أرصدة افتتاحية | `PartyViews` |
| Suppliers | Invoices | كشف فواتير مورد | `PartyViews` |
| Suppliers | Statement | كشف حساب مورد | `PartyViews` |
| Suppliers | Reports | التقارير | `ModuleReportsViews` |
| Accounting | Chart | شجرة الحسابات | `AccountingViews` |
| Accounting | Journal | دفتر اليومية | `AccountingViews` |
| Accounting | JournalBooks | دفاتر اليومية | `AccountingViews` |
| Accounting | AccountLedger | كشف حساب | `AccountingViews` |
| Accounting | Receipts | سند قبض | `AccountingViews` |
| Accounting | Payments | سند صرف | `AccountingViews` |
| Accounting | Cashboxes | الصناديق | `FinanceViews` |
| Accounting | Transfers | تحويل بين الصناديق | `FinanceViews` |
| Accounting | Receivables | الذمم المدينة | `FinanceViews` |
| Accounting | Payables | الذمم الدائنة | `FinanceViews` |
| Accounting | TrialBalance | ميزان مراجعة | `AccountingViews` / `FinanceViews` (route-dependent) |
| Accounting | OpeningBalances | أرصدة افتتاحية | `FinanceViews` |
| Accounting | Reports | التقارير | `ModuleReportsViews` |
| Expenses | List | المصاريف | `ExpenseViews` |
| Expenses | Entries | سجل القيود | `ExpenseViews` |
| Expenses | Entry | قيد مصروف جديد | `ExpenseViews` |
| Expenses | Form | تعريف مصروف جديد | `ExpenseViews` |
| Expenses | Dashboard | لوحة المصاريف | `ExpenseViews` |
| Expenses | Workspace | مركز عمل المصروف | `ExpenseViews` |
| Expenses | Categories | فئات المصاريف | `ExpenseViews` |
| Expenses | Reports | تقارير المصاريف | `ModuleReportsViews` |
| CapitalPartners | List | سجل الشركاء | `CapitalViews` |
| CapitalPartners | Transactions | حركات رأس المال | `CapitalViews` |
| CapitalPartners | Investment | حركة رأس مال | `CapitalViews` |
| CapitalPartners | Form | شريك جديد | `CapitalViews` |
| CapitalPartners | Dashboard | لوحة رأس المال | `CapitalViews` |
| CapitalPartners | Distributions | توزيع الأرباح | `CapitalViews` |
| CapitalPartners | Workspace | مركز عمل الشريك | `CapitalViews` |
| CapitalPartners | Reports | تقارير رأس المال | `ModuleReportsViews` |
| Purchases | Invoices | فواتير الشراء | `PurchasesViews` |
| Purchases | Form | فاتورة شراء جديدة | `PurchasesViews` |
| Purchases | Orders | أمر شراء | `PurchasesViews` |
| Purchases | Returns | مرتجع شراء | `PurchasesViews` |
| Purchases | Reports | التقارير | `ModuleReportsViews` |
| Reports | BI | لوحة الإدارة | `ReportViews` |
| Reports | Reports | التقارير التنفيذية | `ModuleReportsViews` |
| HR | Employees | الموظفون | `HrViews` (default) |
| HR | Departments | الأقسام | `HrViews` |
| HR | Attendance–Reports | (7 submodules) | `HrViews` placeholders |
| Settings | Company–Audit | (13 submodules) | `SettingsViews` |

### 6.4 Hidden / programmatic routes
| Route | Purpose |
|-------|---------|
| `OpeningBalanceWorkspace` | Operations center for opening balance document |
| `OpeningBalanceDashboard` | Opening balances KPI dashboard |
| `WarehouseOperationsCenter` | Inventory warehouse OC |
| `TransferForm`, `StocktakeForm`, `OpeningStockForm` | Inventory wizard launchers |
| China routes: `CostEntry`, `SalePrice`, `MoveToWarehouse`, `ReadyForSale` | Workflow steps (not all in sidebar registry) |

### 6.5 Operations Centers (modal or workspace)
| Entity | Control |
|--------|---------|
| Customer | `CustomerOperationsCenterControl` |
| Supplier | `SupplierOperationsCenterControl` |
| Sales invoice | `SalesInvoiceOperationsCenterControl` |
| Purchase invoice | `PurchaseInvoiceOperationsCenterControl` |
| China container | `ChinaContainerOperationsCenterControl` |
| Expense | `ExpenseOperationsCenterControl` |
| Capital partner | `CapitalOperationsCenterControl` |
| Cashbox | `CashboxOperationsCenterControl` |
| Inventory warehouse | `InventoryOperationsCenterControl` |
| Opening balance | `OpeningBalanceOperationsCenterControl` |

### 6.6 Dialogs / popups
| Service | Role |
|---------|------|
| `ErpModalWindow` | Generic modal host |
| `ExpensePopupService` | Expense actions |
| `CapitalPartnerPopupService` | Partner actions |
| `InventoryPopupService` | Transfer/stocktake/opening stock wizards |
| `SalesPopupService` | Sales popups |
| `AccountingPopupService` | GL popups |
| `CashboxPopupService` | Cashbox OC |
| `OpeningBalancePopupService` | Opening balance OC |
| `ConfirmationDialogService` | Dangerous action confirmation |

---

## 7. Screen Catalog

**ViewModel column:** The project does **not** use MVVM for most screens. `ViewModels/` contains only base classes. Screens are **code-behind `UserControl`** classes. ViewModel is listed as **Not used** unless otherwise noted.

**Status:** ✅ Live DB | 🟡 Partial | ⬜ Placeholder

### 7.1 Dashboard

| Screen | Purpose | Control | Services | Engine | Navigation | Status |
|--------|---------|---------|----------|--------|------------|--------|
| Dashboard | KPIs, quick actions, operational tables | `DashboardModule` | Handler via `AppServices` | — | `AppModule.Dashboard` | 🟡 |

### 7.2 China Import

| Screen | Purpose | Control | Services | Engine | Navigation | Status |
|--------|---------|---------|----------|--------|------------|--------|
| Container list | List/search containers | `ContainerListPageControl` | `ContainerUiService` | — | ChinaImport/Containers | ✅ |
| New import | Create container + Excel | `NewChinaImportControl` | `ContainerUiService` | — | ChinaImport/NewImport | ✅ |
| File analysis | Packing list review | `PackingListAnalysisControl` | `ContainerUiService` | — | ChinaImport/FileAnalysis | 🟡 |
| Cost entry | Landing cost lines | `ChinaImportCostEntryControl` | `ContainerUiService` | — | (workflow route) | 🟡 |
| Landing cost review | Approve container costs | `ChinaImportLandingCostReviewControl` | `ContainerUiService` | Accounting | ChinaImport/LandingCost | 🟡 |
| Sale price | Set sale prices | `ChinaImportSalePriceControl` | `ContainerUiService` | — | (workflow) | 🟡 |
| Move to warehouse | Activate stock | `ChinaImportWarehouseTransferControl` | `ContainerUiService` | `IInventoryEngine` | (workflow) | 🟡 |
| Ready for sale | Mark sale-ready | `ChinaImportReadyForSaleControl` | `ContainerUiService` | — | (workflow) | 🟡 |
| Distribution | Customer allocation summary | `ContainerWorkflowSummaryControl` | `ContainerUiService` | — | ChinaImport/Distribution | 🟡 |
| Container stocktake | Container count summary | `ContainerWorkflowSummaryControl` | `ContainerUiService` | — | ChinaImport/Stocktake | 🟡 |
| Container OC | Full container workspace | `ChinaContainerOperationsCenterControl` | `ContainerUiService` | Multiple | Modal | ✅ |
| Module reports | China reports hub | `ModuleReportsHubControl` | `ModuleReportUiService` | — | ChinaImport/Reports | ✅ |

### 7.3 Sales

| Screen | Purpose | Control | Services | Engine | Navigation | Status |
|--------|---------|---------|----------|--------|------------|--------|
| Invoice list | Sales invoices grid | `SalesInvoiceListPageControl` | `SalesUiService` | — | Sales/Invoices | ✅ |
| New invoice | Create sales invoice | `NewSalesInvoiceControl` | `SalesUiService` | Inventory+Accounting on post | Sales/NewInvoice | ✅ |
| Invoice view | View single invoice | Placeholder in `SalesViews` | — | — | Sales/InvoiceView | ⬜ |
| Returns list | Sales returns | `SalesReturnListPageControl` | `SalesReturnUiService` | `IInventoryEngine` | Sales/Returns | ✅ |
| Detailing queue | Warehouse detailing | `WarehouseDetailingPageControl` | `SalesUiService` | `IInventoryEngine` | Sales/Detailing | ✅ |
| Delivery list | Delivery confirmation | `SalesDeliveryListPageControl` | `SalesUiService` | — | Sales/Delivery | 🟡 |
| Sales invoice OC | Invoice workspace | `SalesInvoiceOperationsCenterControl` | `SalesUiService` | Multiple | Modal | ✅ |
| Module reports | Sales reports | `ModuleReportsHubControl` | `ModuleReportUiService` | — | Sales/Reports | ✅ |

### 7.4 Inventory

| Screen | Purpose | Control | Services | Engine | Navigation | Status |
|--------|---------|---------|----------|--------|------------|--------|
| Inventory dashboard | KPIs | `InventoryDashboardControl` | `InventoryUiService` | — | Inventory/Dashboard | 🟡 |
| Warehouses hub | Warehouse list + fabric stock tabs | `InventoryViews` composite | `InventoryUiService` | — | Inventory/Warehouses | 🟡 |
| Warehouse list | Warehouse CRUD list | `InventoryWarehouseListPageControl` | `InventoryUiService` | — | (tab) | 🟡 |
| Fabric stock | Stock balances | `InventoryFabricStockPageControl` | `InventoryUiService` | — | (tab) | 🟡 |
| Warehouse form | Add/edit warehouse | `InventoryWarehouseFormControl` | `InventoryUiService` | — | WarehouseForm | 🟡 |
| Warehouse OC | Warehouse workspace | `InventoryOperationsCenterControl` | `InventoryUiService` | — | Modal | 🟡 |
| Categories | Fabric categories/items | `InventoryFabricCategoriesPageControl` | `InventoryCatalogUiService` | — | Inventory/Categories | 🟡 |
| Opening stock list | Opening stock documents | `InventoryOpeningStockPageControl` | `InventoryUiService` | `IInventoryEngine.PostOpeningStockAsync` | Inventory/OpeningStock | ✅ |
| Opening stock form | Create/post opening stock | `InventoryOpeningStockFormControl` | `InventoryUiService` | `IInventoryEngine` | OpeningStockForm | ✅ |
| Transfers list | Stock transfers | `InventoryTransferListPageControl` | `InventoryUiService` | `IInventoryEngine.CompleteTransferAsync` | Inventory/Transfers | ✅ |
| Transfer wizard | Create transfer | `InventoryTransferWizardControl` | `InventoryPopupService` | `IInventoryEngine` | Modal | ✅ |
| Stocktake list | Stocktake sessions | `InventoryStocktakeListPageControl` | `InventoryUiService` | `IInventoryEngine.PostStocktakeAsync` | Inventory/Stocktake | ✅ |
| Stocktake form | Stocktake wizard | `InventoryStocktakeFormControl` | `InventoryPopupService` | `IInventoryEngine` | Modal | ✅ |
| Import Excel | Inventory Excel import | Placeholder in `InventoryViews` | — | — | Inventory/ImportExcel | ⬜ |
| Inventory settings | Rules placeholder | Placeholder in `InventoryViews` | — | — | Inventory/Settings | ⬜ |
| Module reports | Inventory reports | `ModuleReportsHubControl` | `ModuleReportUiService` | — | Inventory/Reports | ✅ |

### 7.5 Customers & Suppliers

| Screen | Purpose | Control | Services | Engine | Navigation | Status |
|--------|---------|---------|----------|--------|------------|--------|
| Customer list | Customers grid | `CustomerListPageControl` | `CustomerUiService` | — | Customers/List | ✅ |
| Customer form | Add/edit customer | `CustomerFormControl` | `CustomerUiService` | — | Customers/Form | ✅ |
| Customer opening balance | Legacy AR opening | `CustomerOpeningBalanceControl` | `CustomerUiService` | `IOpeningBalanceEngine` (intended) / legacy handler | Customers/Opening | 🟡 |
| Customer statement | Account statement | `CustomerAccountStatementControl` | `CustomerUiService` | — | Customers/Statement | ✅ |
| Customer invoices | Scoped invoice list | `SalesInvoiceListPageControl` | `SalesUiService` | — | Customers/Invoices | ✅ |
| Customer OC | Customer workspace | `CustomerOperationsCenterControl` | `CustomerUiService` | — | Modal | ✅ |
| Supplier list | Suppliers grid | `SupplierListPageControl` | `SupplierUiService` | — | Suppliers/List | ✅ |
| Supplier form | Add/edit supplier | `SupplierFormControl` | `SupplierUiService` | — | Suppliers/Form | ✅ |
| Supplier opening balance | Legacy AP opening | `SupplierOpeningBalanceControl` | `SupplierUiService` | Legacy + engine path | Suppliers/Opening | 🟡 |
| Supplier statement | Account statement | `SupplierAccountStatementControl` | `SupplierUiService` | — | Suppliers/Statement | ✅ |
| Supplier invoices | Purchase invoices | `SupplierInvoiceListControl` | `PurchaseUiService` | — | Suppliers/Invoices | ✅ |
| Supplier OC | Supplier workspace | `SupplierOperationsCenterControl` | `SupplierUiService` | — | Modal | ✅ |

### 7.6 Accounting & Finance

| Screen | Purpose | Control | Services | Engine | Navigation | Status |
|--------|---------|---------|----------|--------|------------|--------|
| Chart of accounts | Account tree/list | `ChartOfAccountsListPageControl` | `AccountingUiService` | — | Accounting/Chart | ✅ |
| Journal entries | JE list | `JournalEntryListPageControl` | `AccountingUiService` | — | Accounting/Journal | ✅ |
| Journal books | Book list | `JournalBookListPageControl` | `AccountingUiService` | — | Accounting/JournalBooks | ✅ |
| Trial balance | TB report | `TrialBalanceReportControl` | `AccountingUiService` | — | Accounting/TrialBalance | ✅ |
| Account ledger | Ledger report | `AccountLedgerReportControl` | `AccountingUiService` | — | Accounting/AccountLedger | ✅ |
| Receipt vouchers | AR receipts | `ReceiptVoucherPageControl` | `FinanceUiService` | `IIntegratedAccountingService` | Accounting/Receipts | ✅ |
| Payment vouchers | AP payments | `PaymentVoucherPageControl` | `FinanceUiService` | `IIntegratedAccountingService` | Accounting/Payments | ✅ |
| Account form | Add/edit account | `AccountFormControl` | `AccountingUiService` | — | AccountForm | ✅ |
| Journal form | Manual JE | `JournalEntryFormControl` | `AccountingUiService` | — | JournalForm | ✅ |
| Cashbox list | Cashboxes | `CashboxListPageControl` | `FinanceUiService` | — | Accounting/Cashboxes | ✅ |
| Cashbox transfers | Inter-cashbox transfers | `CashboxTransferListPageControl` | `FinanceUiService` | `IIntegratedAccountingService` | Accounting/Transfers | ✅ |
| Receivables aging | AR aging | `ReceivablesAgingControl` | `FinanceUiService` | — | Accounting/Receivables | 🟡 |
| Payables aging | AP aging | `PayablesAgingControl` | `FinanceUiService` | — | Accounting/Payables | 🟡 |
| Opening balance list | Unified OB documents | `OpeningBalanceListPageControl` | `OpeningBalanceUiService` | `IOpeningBalanceEngine` | Accounting/OpeningBalances | ✅ |
| Opening balance form | Manual/import OB | `OpeningBalanceFormControl` | `OpeningBalanceUiService` | `IOpeningBalanceEngine` | Modal | ✅ |
| Opening balance dashboard | OB KPIs | `OpeningBalanceDashboardControl` | `OpeningBalanceUiService` | — | OpeningBalanceDashboard | ✅ |
| Opening balance OC | OB workspace | `OpeningBalanceOperationsCenterControl` | `OpeningBalanceUiService` | `IOpeningBalanceEngine` | Modal/Workspace | ✅ |
| Cashbox OC | Cashbox workspace | `CashboxOperationsCenterControl` | `FinanceUiService` | — | Modal | 🟡 |
| Module reports | Accounting reports | `ModuleReportsHubControl` | `ModuleReportUiService` | — | Accounting/Reports | ✅ |

### 7.7 Purchases

| Screen | Purpose | Control | Services | Engine | Navigation | Status |
|--------|---------|---------|----------|--------|------------|--------|
| Invoice list | Purchase invoices | `PurchaseInvoiceListPageControl` | `PurchaseUiService` | `IPurchaseInventoryService` | Purchases/Invoices | ✅ |
| Invoice form | New/edit PI | `PurchaseInvoiceFormControl` | `PurchaseUiService` | Inventory+Accounting | Purchases/Form | ✅ |
| Order list | Purchase orders | `PurchaseOrderListPageControl` | `PurchaseUiService` | — | Purchases/Orders | 🟡 |
| Order form | PO form | `PurchaseOrderFormControl` | `PurchaseUiService` | — | OrderForm | 🟡 |
| Return list | Purchase returns | `PurchaseReturnListPageControl` | `PurchaseUiService` | `IInventoryEngine` | Purchases/Returns | ✅ |
| Return form | PR form | `PurchaseReturnFormControl` | `PurchaseUiService` | — | ReturnForm | ✅ |
| Purchase OC | Invoice workspace | `PurchaseInvoiceOperationsCenterControl` | `PurchaseUiService` | Multiple | Modal | ✅ |

### 7.8 Expenses

| Screen | Purpose | Control | Services | Engine | Navigation | Status |
|--------|---------|---------|----------|--------|------------|--------|
| Expense list | Expense definitions | `ExpenseListPageControl` | `ExpenseUiService` | — | Expenses/List | ✅ |
| Expense form | Define expense | `ExpenseFormControl` | `ExpenseUiService` | — | Expenses/Form | ✅ |
| Expense entries | Payment entries | `ExpenseEntryListPageControl` | `ExpenseUiService` | Accounting on pay | Expenses/Entries | ✅ |
| Expense entry form | New payment | `ExpenseEntryFormControl` | `ExpenseUiService` | — | Expenses/Entry | ✅ |
| Expense dashboard | KPIs | `ExpenseDashboardControl` | `ExpenseUiService` | — | Expenses/Dashboard | ✅ |
| Expense categories | Category admin | `ExpenseCategoryAdminControl` | `ExpenseUiService` | — | Expenses/Categories | ✅ |
| Expense OC | Expense workspace | `ExpenseOperationsCenterControl` | `ExpenseUiService` | — | Expenses/Workspace | ✅ |

### 7.9 Capital Partners

| Screen | Purpose | Control | Services | Engine | Navigation | Status |
|--------|---------|---------|----------|--------|------------|--------|
| Partner list | Partners grid | `CapitalPartnerListPageControl` | `CapitalPartnerUiService` | — | CapitalPartners/List | ✅ |
| Partner form | Add/edit partner | `CapitalPartnerFormControl` | `CapitalPartnerUiService` | — | CapitalPartners/Form | ✅ |
| Transactions | Capital movements | `CapitalTransactionListPageControl` | `CapitalPartnerUiService` | Accounting | CapitalPartners/Transactions | ✅ |
| Investment form | Invest/withdraw | `CapitalInvestmentFormControl` | `CapitalPartnerUiService` | — | CapitalPartners/Investment | ✅ |
| Dashboard | Capital KPIs | `CapitalDashboardControl` | `CapitalPartnerUiService` | — | CapitalPartners/Dashboard | ✅ |
| Distributions | Profit distribution | `CapitalDistributionsControl` | `CapitalPartnerUiService` | — | CapitalPartners/Distributions | 🟡 |
| Partner OC | Partner workspace | `CapitalOperationsCenterControl` | `CapitalPartnerUiService` | — | CapitalPartners/Workspace | ✅ |

### 7.10 HR

| Screen | Purpose | Control | Services | Engine | Navigation | Status |
|--------|---------|---------|----------|--------|------------|--------|
| Employee list | Employees | `EmployeeListPageControl` | `HrUiService` | — | HR/Employees | ✅ |
| Employee form | Add/edit employee | `EmployeeFormControl` | `HrUiService` | — | (modal) | ✅ |
| Department list | Departments | `DepartmentListPageControl` | `HrUiService` | — | HR/Departments | ✅ |
| Department form | Add/edit department | `DepartmentFormControl` | `HrUiService` | — | (modal) | ✅ |
| Attendance–Reports | HR submodules | `PlaceholderUi.DatabasePhase` | — | — | HR/* | ⬜ |

### 7.11 Settings & Reports

| Screen | Purpose | Control | Services | Navigation | Status |
|--------|---------|---------|----------|------------|--------|
| Settings hub | Settings cards | `SettingsViews.BuildHub` | `SettingsUiService` | Settings | 🟡 |
| Company / Finance / Numbering / Branches | Editable settings | `SettingsViews` | `SettingsUiService` | Settings/{key} | 🟡 |
| Other settings sections | Placeholder | `PlaceholderUi.DevelopmentPhase` | — | Settings/{key} | ⬜ |
| Executive BI | High-level dashboard | `ReportViews` | — | Reports/BI | 🟡 |
| Module report hub | Per-module reports | `ModuleReportsHubControl` | `ModuleReportUiService` | */Reports | ✅ |

---

## 8. Business Engines

### 8.1 `IOpeningBalanceEngine` → `OpeningBalanceEngine`
| Attribute | Detail |
|-----------|--------|
| **Purpose** | Single source of truth for all opening balance types before go-live |
| **Location** | Interface: `Application/Abstractions/Services/IOpeningBalanceEngine.cs`; Impl: `Infrastructure/Services/OpeningBalanceEngine.cs` |
| **Responsibilities** | Validate, create/update, submit/approve/reject, post (GL), archive, duplicate, Excel import, party quick-post, audit trail, party marking (`OpeningBalancePosted`), duplicate line detection |
| **Public API** | `ValidateAsync`, `CreateAsync`, `UpdateAsync`, `SubmitAsync`, `ApproveAsync`, `RejectAsync`, `PostAsync`, `ArchiveAsync`, `DuplicateAsync`, `ImportExcelAsync`, `BuildImportTemplate`, `PostPartyOpeningBalanceAsync` |
| **Consumers** | `OpeningBalanceHandlers`, `OpeningBalanceUiService`; intended consumer for customer/supplier legacy screens |
| **Dependencies** | `IOpeningBalanceRepository`, `IOpeningBalanceLookupService`, `IIntegratedAccountingService`, `INumberingService`, `IUnitOfWork`, `ICurrentUserService`, `ICurrentBranchService`, customer/supplier repositories, `ErpDbContext` |
| **Status** | ✅ Implemented. Opening stock inventory posting via `IInventoryEngine.PostFinanceOpeningBalanceStockAsync` declared on interface — **wiring to engine PostAsync: verify at integration time** |
| **Types supported** | `OpeningStock`, `CustomerReceivable`, `SupplierPayable`, `Cash`, `Bank`, `Capital`, `GeneralLedger` (+ future enum values 7–11 in domain, not fully wired) |

### 8.2 `IInventoryEngine` → `InventoryEngine`
| Attribute | Detail |
|-----------|--------|
| **Purpose** | All stock movements: rolls, reservations, COGS, transfers, stocktake, opening stock |
| **Responsibilities** | Container import posting, purchase/sales issue/receive, returns, reservations, detailing roll assignment, opening stock, transfers, stocktake, valuation snapshots |
| **Public API** | `PostContainerImportAsync`, `PostPurchaseInvoiceAsync`, `ReversePurchase*`, `ReserveForInvoiceAsync`, `IssueForInvoiceAsync`, `ReleaseForInvoiceAsync`, `AssignFabricRollsOnDetailingAsync`, `ReceiveSalesReturnAsync`, `PostOpeningStockAsync`, `PostFinanceOpeningBalanceStockAsync`, `CompleteTransferAsync`, `PostStocktakeAsync`, `RecordValuationSnapshotAsync` |
| **Consumers** | Inventory handlers, purchase inventory service, container warehouse import, sales handlers |
| **Dependencies** | `ErpDbContext`, `IIntegratedAccountingService` |
| **Status** | ✅ Core engine implemented |

### 8.3 `IIntegratedAccountingService` → `IntegratedAccountingService`
| Attribute | Detail |
|-----------|--------|
| **Purpose** | Central GL posting for operational documents |
| **Responsibilities** | Idempotent journal creation (`PostIfNotExistsAsync`) per `DocumentType` + source id |
| **Key APIs** | Container approval/activation, sales invoice, receipts/payments, purchases/returns, expense payments, cashbox transfers, customer/supplier opening balance (legacy), **unified** `PostOpeningBalanceDocumentAsync` |
| **Consumers** | All modules that post journals |
| **Status** | ✅ Implemented |

### 8.4 `IPurchaseInventoryService` → `PurchaseInventoryService`
| Attribute | Detail |
|-----------|--------|
| **Purpose** | Bridge purchase posting to inventory engine |
| **Dependencies** | `IInventoryEngine` |
| **Status** | ✅ Thin wrapper |

### 8.5 `IContainerWarehouseImportService` → `ContainerWarehouseImportService`
| Attribute | Detail |
|-----------|--------|
| **Purpose** | Bridge China container warehouse activation to inventory engine |
| **Status** | ✅ Thin wrapper |

### 8.6 `IInventoryOperationsService` → `InventoryOperationsService`
| Attribute | Detail |
|-----------|--------|
| **Purpose** | Higher-level inventory operations (Unable to determine full surface without reading entire file) |
| **Status** | ✅ Registered in DI |

### 8.7 `INumberingService` → `PostgreSqlNumberingService`
| Attribute | Detail |
|-----------|--------|
| **Purpose** | Document number sequences (invoices, OBL, opening stock, etc.) |
| **Status** | ✅ Implemented |

### 8.8 `IGlobalSearchService` → `GlobalSearchService`
| Attribute | Detail |
|-----------|--------|
| **Purpose** | Cross-module search |
| **Consumers** | `GlobalSearchUiService` |
| **Status** | ✅ Implemented |

### 8.9 `ERPSystem.DocumentEngine` / `DocumentEngineService`
| Attribute | Detail |
|-----------|--------|
| **Purpose** | HTML/CSS document generation (host-agnostic) |
| **Templates** | Sales invoice, purchase invoice, vouchers, statements, inventory reports, stocktake/opening stock, executive reports, quotation, container report, expense voucher |
| **Consumers** | **Not implemented yet** in WPF host (WPF uses QuestPDF in places) |
| **Status** | ✅ Library built; ❌ not integrated into main app |

---

## 9. Services

### 9.1 Registration model
| Layer | Registration |
|-------|--------------|
| Infrastructure | `InfrastructureServiceCollectionExtensions.AddInfrastructure` — repositories, engines, EF, numbering |
| Application | `ApplicationServiceCollectionExtensions.AddApplication` — all command/query handlers (scoped) |
| WPF (`App.xaml.cs`) | Singleton `*UiService` per module + `WpfCurrentUserService`, `WpfCurrentBranchService`, `WpfPermissionService` (scoped), `WpfNotificationService` |

### 9.2 WPF UI Services (`Services/`)

| Service | Purpose | Backend access |
|---------|---------|----------------|
| `CustomerUiService` | Customer CRUD, statement, opening balance | Scoped handlers via `IServiceScopeFactory` |
| `SupplierUiService` | Supplier CRUD, statement, opening balance | Scoped handlers |
| `SalesUiService` | Sales invoice lifecycle, detailing, delivery | Scoped handlers |
| `SalesReturnUiService` | Sales returns | Scoped handlers |
| `PurchaseUiService` | Purchase invoices/orders/returns | Scoped handlers |
| `ContainerUiService` | China import workflow | Scoped handlers |
| `InventoryUiService` | Warehouses, movements, transfers, stocktake, opening stock | Scoped handlers |
| `InventoryCatalogUiService` | Fabric categories/items/colors | Scoped handlers |
| `ExpenseUiService` | Expenses, payments, approvals | Scoped handlers |
| `CapitalPartnerUiService` | Partners, transactions, distributions | Scoped handlers |
| `AccountingUiService` | Chart, journals, reports | Scoped handlers |
| `FinanceUiService` | Vouchers, cashboxes, aging | Scoped handlers |
| `OpeningBalanceUiService` | Unified opening balances CRUD/workflow | `IOpeningBalanceEngine` via handlers |
| `ModuleReportUiService` | Module report execution | `IModuleReportRepository` |
| `SettingsUiService` | System settings, branches | `ISystemSettingsRepository`, `IBranchRepository` |
| `GlobalSearchUiService` | Global search UI | `IGlobalSearchService` |
| `HrUiService` | HR employees/departments | HR handlers |

### 9.2b WPF presentation & document services (`Services/`)

| Service | Purpose | Registration |
|---------|---------|--------------|
| `AppServices` | Static `IServiceProvider` accessor for controls | `AppServices.Initialize` in `App.xaml.cs` |
| `WpfCurrentUserService` | `ICurrentUserService` — current user context | Singleton in `App.xaml.cs` |
| `WpfCurrentBranchService` | `ICurrentBranchService` — company/branch context | Singleton in `App.xaml.cs` |
| `WpfPermissionService` | `IPermissionService` — permission checks from UI | Scoped in `App.xaml.cs` |
| `WpfNotificationService` | `INotificationService` — replaces infra in-memory notifier | Singleton in `App.xaml.cs` |
| `ApplicationResultPresenter` | Maps `ApplicationResult` to UI toasts/errors | Static helper |
| `ConfirmationDialogService` | Yes/No dangerous-action confirmation | Static helper |
| `ErpDataRefreshHub` | Cross-screen refresh events (`DataChanged`) | Static event hub |
| `ListExportService` | Excel export for list/report rows | Static helper (ClosedXML path via handlers) |
| `StatementDocumentService` | Customer/supplier statement PDF/export | Uses QuestPDF / report data |
| `PurchaseDocumentService` | Purchase invoice preview/print | Document generation |
| `SalesDocumentService` | Sales invoice preview/print | Document generation |
| `InventoryExportService` | Inventory list export | Static helper |
| `CurrencyCatalog` | Cached currency list for UI | Refreshed on startup |

### 9.2c Popup services

| Service | Entity / module |
|---------|-----------------|
| `OpeningBalancePopupService` | Opening balance operations center modal |
| `ExpensePopupService` | Expense edit, OC, actions |
| `CapitalPartnerPopupService` | Partner edit, investment, OC |
| `InventoryPopupService` | Transfer/stocktake/opening stock wizards |
| `InventoryCatalogPopupService` | Fabric catalog modals |
| `SalesPopupService` | Sales invoice popups |
| `AccountingPopupService` | GL account/journal popups |
| `CashboxPopupService` | Cashbox OC and forms |

### 9.2d Context menu services

| Service | Entity |
|---------|--------|
| `RowContextMenuService` | Generic DataGrid attached behavior |
| `ExpenseContextMenuService` | Expense list rows |
| `CapitalPartnerContextMenuService` | Capital partner rows |
| `SalesContextMenuService` | Sales invoice rows |
| `AccountingContextMenuService` | Accounting entities |
| `CashboxContextMenuService` | Cashbox rows |
| `OpeningBalanceContextMenuService` | Opening balance list rows (open, submit, approve, post, archive, export) |

### 9.3 Domain services (`ERPSystem.Domain/Services/`)

Pure domain logic (no DI registration as services — invoked from aggregates/handlers):

| Class | Purpose |
|-------|---------|
| `CustomerBalanceCalculator` | Customer balance computation rules |
| `StatementCalculator` | Statement running balance |
| `CreditLimitChecker` | Credit limit validation |
| `SalesInvoiceTotalCalculator` | Invoice line totals |
| `StockMovementValidator` | Stock movement validation |
| `WarehouseDetailingValidator` | Detailing workflow validation |
| `InventoryReservationPolicy` | Reservation rules |
| `LandingCostCalculator` | China landing cost allocation |
| `ChinaImportFinancials` | Import financial calculations |
| `AccountingPostingPolicy` | GL posting policy helpers |
| `ExpenseLifecycle` | Expense status transitions |
| `CapitalServices` | Capital partner domain helpers |

### 9.4 Infrastructure abstractions (selected)

| Interface | Implementation |
|-----------|----------------|
| `ICustomerRepository` | `CustomerRepository` |
| `ISupplierRepository` | `SupplierRepository` |
| `ISalesInvoiceRepository` | Sales invoice repository |
| `IInventoryManagementRepository` | `InventoryManagementRepository` |
| `IOpeningBalanceRepository` | `OpeningBalanceRepository` |
| `IJournalEntryRepository` | Journal repository |
| `IAccountRepository` | Account repository |
| `IExpenseRepository` | Expense repository |
| `ICapitalPartnerRepository` | Capital partner repository |
| `IAuditLogRepository` | Audit log repository |
| `IPermissionService` | `WpfPermissionService` (UI), checks DB permissions |
| `INotificationService` | `WpfNotificationService` (UI), `InMemoryNotificationService` (infra default overridden) |
| `IDocumentPreviewService` | `NullDocumentPreviewService` (**not implemented yet**) |
| `IOpeningBalanceLookupService` | `OpeningBalanceLookupService` |

### 9.5 Infrastructure utilities

| Component | Purpose |
|-----------|---------|
| `AccountingHealth` | Accounting integrity checks (`Infrastructure/Services/AccountingHealth.cs`) |
| `UtcDateTimeSaveChangesInterceptor` | Normalizes UTC on EF save |
| `AuditSaveChangesInterceptor` | Writes audit rows on EF save |

### 9.6 Action routers & context menus
| Component | Role |
|-----------|------|
| `MockQuickActionRouter` | Fallback quick actions for operations centers |
| `OpeningBalanceQuickActionRouter` | OB approve/post/archive/export (operations center) |
| `OpeningBalanceContextMenuService` | OB list right-click menu (same actions + open) |
| `ExpenseQuickActionRouter` | Expense OC actions |
| `CapitalPartnerQuickActionRouter` | Partner OC actions |
| `CustomerActionRouter` / `SupplierActionRouter` | Party context actions |
| `RowContextMenuService` | Attached property for DataGrid right-click menus |
| `ExpenseContextMenuService` | Expense-specific context menu |
| `EntityActionRegistry` | Declarative action definitions per `EntityType` |

### 9.7 Mock / stub services
| Service | Role |
|---------|------|
| `MockInteractionService` | Toasts, navigation helpers, "coming soon" |
| `NullDocumentPreviewService` | No-op document preview |
| `PlaceholderUi` | Development/empty state UI blocks |

---

## 10. Workflows

### 10.1 Unified Opening Balance (`OpeningBalanceStatus`)
**Aggregate:** `OpeningBalanceDocument`  
**Engine:** `IOpeningBalanceEngine`

| Status | Value | Meaning |
|--------|-------|---------|
| Draft | 0 | Editable |
| PendingApproval | 1 | Submitted |
| Approved | 2 | Ready to post |
| Posted | 3 | Journal created |
| Locked | 4 | Posted + locked |
| Archived | 5 | Archived (non-posted or after lock rules) |
| Rejected | 6 | Returned to editing |

**Transitions (domain rules):**
- `SubmitForApproval`: Draft or Rejected → PendingApproval (requires lines)
- `Approve`: PendingApproval or Draft → Approved
- `Reject`: PendingApproval → Rejected
- `MarkPosted`: Approved → Posted (requires approved state)
- `Lock`: Posted → Locked
- `Archive`: blocked if Posted without lock path; sets Archived
- **Edit guard:** `IsEditable` only in Draft or Rejected

**Posting:** `PostAsync` builds journal lines per `OpeningBalanceType`, calls `IIntegratedAccountingService.PostOpeningBalanceDocumentAsync` (idempotent per document id), marks parties for AR/AP, records `OpeningBalanceEvent` audit entries.

**Types:** OpeningStock, CustomerReceivable, SupplierPayable, Cash, Bank, Capital, GeneralLedger (+ future types in enum).

### 10.2 Sales Invoice (`SalesInvoiceStatus`)
Draft → AwaitingDetailing → Detailed → ReadyForApproval → Approved → Printed → Delivered (+ Cancelled, PartiallyReturned, Returned).  
Inventory: reserve on send-to-warehouse, issue on approval, detailing assigns rolls.

### 10.3 Expense (`ExpenseStatus`)
Draft → PendingApproval → Approved → Scheduled → PartiallyPaid → Paid → Closed (+ Cancelled, Archived).

### 10.4 Purchase Invoice (`PurchaseInvoiceStatus`)
Domain enum exists; handlers implement post to inventory + GL. (Full transition diagram: **Unable to determine from current source** without reading entire aggregate.)

### 10.5 Inventory documents (`InventoryDocumentStatus`)
Used for opening stock, transfers, stocktake sessions (Draft → Posted/Completed/Cancelled variants).

### 10.6 Journal Entry (`JournalEntryStatus`)
Used in accounting module for manual journals.

### 10.7 China Container (`ChinaContainerStatus`)
Container lifecycle from import through approval and warehouse activation. (Detailed states: see `ERPSystem.Domain.Enums.ChinaContainerStatus`.)

### 10.8 Party opening balance (unified path)
Customer and supplier submodule screens call `OpeningBalanceUiService.PostPartyOpeningBalanceAsync`, which delegates to `IOpeningBalanceEngine` (create → submit → approve → post `OpeningBalanceDocument`). Legacy `IIntegratedAccountingService.PostCustomerOpeningBalanceAsync` / `PostSupplierOpeningBalanceAsync` are **obsolete** and unused by UI. Thin command handlers (`PostCustomerOpeningBalanceHandler` / `PostSupplierOpeningBalanceHandler`) remain for backward compatibility only.

---

## 11. Database

### Provider & connection
- **PostgreSQL** via Npgsql
- Connection string: `appsettings.json` → `ConnectionStrings:DefaultConnection`
- Startup: `provider.MigrateAndSeedAsync()` in `App.xaml.cs`

### ORM & context
- **EF Core 9** — `ErpDbContext` with `ApplyConfigurationsFromAssembly`
- Interceptors: `UtcDateTimeSaveChangesInterceptor`, `AuditSaveChangesInterceptor`
- Global query filters: e.g. sales invoices `IsActive && !IsArchived`

### Schema organization
Persistence models grouped by area under `Infrastructure/Persistence/Models/`:
`Accounting`, `Audit`, `Catalog`, `ChinaImport`, `Company`, `Capital`, `Expenses`, `Finance`, `Hr`, `Identity`, `Inventory`, `Parties`, `Purchasing`, `Sales`, `Settings`, `Documents`

Opening balance finance tables (migration `20260714120000_AddOpeningBalancesModule`): schema **`finance`** — `opening_balance_documents`, `opening_balance_lines`, `opening_balance_events`.

### Key entity groups (DbSets)
| Area | Entities |
|------|----------|
| Identity | Users, Roles, Permissions, UserRoles, RolePermissions |
| Parties | Customers, Suppliers, ChinaSuppliers |
| Catalog | FabricCategories, FabricItems, FabricColors, FabricRolls |
| China | Containers, ContainerItems, LandingCosts, Distributions, ImportBatches, … |
| Inventory | Warehouses, Stocks, Movements, Transfers, Stocktake, OpeningStock, Reservations, Alerts, Audit/Timeline, ValuationSnapshots |
| Sales | SalesInvoices, Returns, DetailingSessions, ReceiptPayments |
| Purchasing | PurchaseInvoices, Orders, Returns, Payments |
| Finance | Receipt/Payment vouchers, Cashboxes, Transfers, CostCenters, **OpeningBalance*** |
| Accounting | Accounts, JournalEntries/Lines, JournalBooks |
| Expenses | Expenses, Payments, Installments, Categories, Audit/Timeline |
| Capital | Partners, Participations, Transactions, ProfitDistributions |
| HR | Departments, Employees |
| Settings | SystemSettings, DocumentTemplates, DocumentCounters |
| Audit | AuditLogs |

### Migrations strategy
- Code-first EF migrations in `ERPSystem.Infrastructure/Migrations/`
- Named with timestamp prefix (e.g. `20260714120000_AddOpeningBalancesModule`)
- `ErpDbContextModelSnapshot.cs` maintained
- Initial create: `20260626235435_InitialCreate`

### Repositories
Repository-per-aggregate pattern in `Infrastructure/Repositories/`; interfaces in `Application/Abstractions/Repositories/`.  
Unit of work: `IUnitOfWork` → `EfUnitOfWork`.

### Entity relationships
**Unable to determine full ER diagram from current source** without exhaustive configuration review. Relationships are defined in `Infrastructure/Configurations/*.cs`. See `docs/ERP_PRO_DOMAIN_MODEL_DIAGRAM.md` for a historical domain diagram (may be partially outdated).

---

## 12. Security

### Authentication
**Unable to determine from current source** if full login UI exists. Seeder creates default admin user (`DatabaseSeeder.AdminUserId`). `WpfCurrentUserService` provides current user context.

### Authorization
- **Permission-based:** `IPermissionService.CanAsync(permissionCode)`
- Permissions stored in `Permissions` table; linked to roles via `RolePermissions`
- Module-specific permission seeds in `DatabaseSeeder` (accounting, customers, suppliers, sales, finance, openingbalances, containers, warehouses, purchases, HR, expenses, capital)
- Example opening balance permissions: `openingbalances.view`, `.create`, `.edit`, `.import`, `.approve`, `.post`, `.archive`, `.export`, `.print`
- WPF: `WpfPermissionService` (scoped per operation)

### Roles
- Default role seeded: **Admin** (`AdminRoleId`) with all permissions attached in seeder

### Audit trail
| Mechanism | Scope |
|-----------|--------|
| `AuditLogEntity` / `IAuditLogRepository` | Global audit log |
| `AuditSaveChangesInterceptor` | EF save changes |
| `OpeningBalanceEvent` | Per opening balance document |
| `ExpenseAuditLogEntity`, `ExpenseTimelineEventEntity` | Expenses |
| `PartnerAuditLogEntity`, `PartnerTimelineEventEntity` | Capital |
| `InventoryAuditEntryEntity`, `InventoryTimelineEventEntity` | Inventory |
| Opening balance trail recorder | `OpeningBalanceTrailRecorder` |

### Soft delete / archive
- Widespread `IsArchived` + `IsActive` flags on persistence entities
- EF global query filters on some entities (e.g. sales invoices)
- Domain-level `Archive()` methods on several aggregates
- **Hard delete:** **Unable to determine global policy from current source**

### Approval system
- Opening balances: explicit approval workflow (Section 10.1)
- Expenses: `PendingApproval` / `Approved` / `Reject` actions
- Capital: `CapitalApprovalStatus`
- Sales: approval before print/delivery
- Journal entries: `JournalApprove` actions in `EntityActionRegistry`

---

## 13. UI Architecture

### MVVM implementation
**Not implemented as primary pattern.**  
- Screens are `UserControl` with **code-behind** (C#) and direct control tree construction (many screens build UI in C#; some use XAML).  
- `ViewModels/Base/ViewModelBase.cs` and `RelayCommand.cs` exist but are **not wired to most screens**.  
- State and commands live in code-behind + `*UiService` calls.

### Navigation
1. `NavigationStateManager` — module + subpage state, back stack  
2. `ModuleShellControl` — submodule tabs + `SubmoduleViewFactory`  
3. `MockInteractionService.Navigate(AppModule, subPage)` — programmatic navigation  
4. `*NavigationContext` static classes — pass IDs between routes (e.g. `OpeningBalanceNavigationContext`, `ExpenseNavigationContext`)

### Dialogs
- `ErpModalWindow.Show(...)` — modal overlay  
- `*PopupService` classes — entity-specific modal workflows  
- `ConfirmationDialogService.ConfirmDangerous` — destructive confirmations

### Commands
- No universal `ICommand` binding pattern  
- Button `Click` handlers and `RelayCommand` only in unused ViewModel infrastructure  
- Application layer uses **CQRS command handlers**, not WPF commands

### Templates & styles
- `Resources/Themes/EnterpriseTheme.xaml` — brushes (`PrimaryBrush`, `AppBgBrush`, …), button styles (`PrimaryButtonStyle`, `SecondaryButtonStyle`, `GhostButtonStyle`)
- `ErpUiFactory` / `ErpUxFactory` — programmatic UI composition  
- `ErpDesignTokens` — spacing, radii, typography constants

### Operation Center pattern
`OperationsCenterShell.Build(OperationsCenterSpec)` renders:
- Breadcrumb, header, KPI row, workflow stepper  
- Tabbed content (Overview, Accounting, Audit, Timeline, Journal, Reports, …)  
- Quick action bar → routers (`OpeningBalanceQuickActionRouter`, etc.)

### Dashboard system
- Module dashboards: `ExpenseDashboardControl`, `CapitalDashboardControl`, `InventoryDashboardControl`, `OpeningBalanceDashboardControl`  
- Executive: `DashboardModule`, `ReportViews.BuildExecutiveDashboard`  
- KPI cards: `MetricCardControl`, `ErpUiFactory.SetSummaryCards`

### Quick Actions
`OperationsCenterQuickAction` with `ActionKey` strings (`ob:post`, `expense:approve`, `tab:Journal`, `nav:Accounting:OpeningBalances`, …) routed through module-specific routers and `MockQuickActionRouter`.

### Localization
- `LocalizationManager` — language change events, flow direction (RTL)  
- Default culture: `ar-SA`  
- UI strings predominantly Arabic inline in code

### List module pattern
`ErpListModuleControl` — reusable list page shell (header, filters, DataGrid, primary action button, context menu via `RowContextMenuService`).

---

## 14. Current Progress

### Completed (high confidence)
- Clean Architecture solution structure (Domain, Application, Infrastructure, WPF)
- PostgreSQL + EF migrations + seeding
- China import core (list, import, parse, approve, warehouse activation path)
- Sales invoices with inventory detailing integration
- Purchases (invoices, returns) with inventory + GL
- Inventory engine (movements, transfers, stocktake, opening stock)
- Customers & suppliers (master data, statements)
- Accounting core (COA, journals, books, trial balance, ledger)
- Finance vouchers, cashboxes, transfers
- Expenses module (production completion per migration name)
- Capital partners module
- Unified opening balances module (engine, persistence, UI list/form/OC)
- HR employees & departments
- Module report hubs (per `docs/module-connection-status.md` — 28 full submodules)

### In progress
- Opening balances: party screen unification, context menu on list, inventory posting integration for opening stock type
- HR: attendance, payroll, leaves, etc.
- Settings: most sections still placeholder
- Inventory: warehouse CRUD gaps, import Excel, settings rules UI
- China: distribution and physical stocktake entry
- DocumentEngine → WPF integration

### Recently completed work (from codebase state)
- `OpeningBalances` submodule in accounting navigation
- `OpeningBalanceEngine`, repositories, migrations (`20260714120000`)
- `OpeningBalanceUiService`, list/dashboard/form/operations center
- `OpeningBalanceQuickActionRouter` wired to operations center shell
- Cashbox account id migration, customer opening balance flag migration, HR employee extensions

### Known limitations
- Dual opening balance paths (unified engine vs legacy party handlers)
- `ERPSystem.DocumentEngine` not referenced by WPF; PDF via QuestPDF selectively
- Many `ShowComingSoon` / `PlaceholderUi` screens
- Executive dashboard KPIs partially show `—`
- `IDocumentPreviewService` is null implementation
- Batch/async import for large Excel files: synchronous only
- Opening balance list context menu (`OpeningBalanceContextMenuService`) wired to list page

### Pending work (evident from placeholders and audits)
- Full settings module
- Full HR module
- Inventory Excel import pipeline
- China distribution allocation
- Complete DocumentEngine adoption
- Consolidate all opening balance entry through `IOpeningBalanceEngine`
- Formal reversal/cancel flows for posted opening balances (**Unable to determine if implemented**)

---

## 15. Technical Debt

| Item | Description |
|------|-------------|
| **Non-MVVM UI** | Large code-behind controls; `ViewModels/` barely used — mismatch with stated MVVM-only rules in some docs |
| **MockInteractionService** | Used for navigation, toasts, and "coming soon" instead of structured feature flags |
| **MockQuickActionRouter** | Catch-all OC action router |
| **NullDocumentPreviewService** | Document preview not implemented |
| **Legacy opening balance accounting APIs** | `PostCustomerOpeningBalanceAsync` / `PostSupplierOpeningBalanceAsync` on `IIntegratedAccountingService` marked obsolete; UI uses unified engine |
| **Duplicate finance/accounting routes** | `AccountingViews` vs `FinanceViews` for overlapping submodules (TrialBalance placeholder in FinanceViews) |
| **DocumentEngine isolation** | Full HTML engine built but excluded from WPF compile |
| **Static navigation contexts** | `OpeningBalanceNavigationContext`, etc. — global static state for navigation params |
| **Placeholder screens** | HR (7 submodules), Settings (most), Sales InvoiceView, Inventory ImportExcel/Settings |
| **Permission checks inconsistent** | Some UI actions check permissions; others rely on handler-level checks only |
| **ErpDbContextModelSnapshot** | May lag behind manual migrations — verify before strict EF tooling |

### Known risks
- Legacy opening balance accounting methods still exist in codebase (obsolete) — must not be re-wired to UI
- `TreatWarningsAsErrors` — any warning fails CI/local build
- Credentials in `appsettings.json` (postgres password) — environment-specific secret handling **Unable to determine production strategy**

---

## 16. Future Roadmap

> **Note:** No formal product roadmap file found in repository. Below is inferred **only** from domain enums, placeholders, and incomplete integrations. Prioritization is architectural recommendation, not a committed plan.

### Phase 1 — Consolidation & correctness
- Route all opening balances (party screens + inventory stock) through `IOpeningBalanceEngine` + `IInventoryEngine` only
- Idempotent posting verification across GL and inventory
- Opening balance list context menu + permission-gated actions
- Wire `ERPSystem.DocumentEngine` into WPF or deprecate QuestPDF paths
- Complete settings: Users, Taxes, Print templates

### Phase 2 — Operational completeness
- China distribution & container stocktake data entry
- Inventory import Excel + rules engine UI
- HR attendance, payroll, contracts
- Executive dashboard live KPIs
- Global search UI polish

### Phase 3 — Enterprise hardening
- Opening balance types 7–11 (FixedAsset, Loan, EmployeeAdvance, PettyCash, BranchOpening)
- Formal reversal documents for posted balances
- Background/batch import (100k+ rows)
- Multi-branch permission scoping
- Authentication UI / session management (if not present)
- API layer (if desired) — **Not implemented yet**

---

## 17. Architecture Rules

Rules **observed in codebase** (not aspirational only):

| Rule | Evidence |
|------|----------|
| Clean Architecture layering | Separate Domain, Application, Infrastructure projects; WPF references Application+Infrastructure only |
| Domain has no infrastructure references | `ERPSystem.Domain.csproj` has no package references |
| CQRS-style handlers | `ICommandHandler<TCommand,TResult>`, `IQueryHandler<TQuery,TResult>` per use case |
| Business rules in domain aggregates | `OpeningBalanceDocument`, `SalesInvoiceAggregate`, etc. enforce transitions |
| Engines own cross-cutting posting | `IInventoryEngine`, `IOpeningBalanceEngine`, `IIntegratedAccountingService` |
| No EF in Application layer | DbContext only in Infrastructure |
| UI calls Application via UiService + scoped handlers | `OpeningBalanceUiService` pattern |
| Idempotent GL posting | `PostIfNotExistsAsync` keyed by `DocumentType` + source id |
| Permission checks in handlers | e.g. `OpeningBalanceHandlers` call `IPermissionService` |
| Arabic-first RTL UI | `ar-SA`, `LocalizationManager`, RTL flow |
| Treat warnings as errors | All main `.csproj` files |
| DocumentEngine isolation | No WPF/EF references in DocumentEngine project |

Rules **documented elsewhere but NOT fully followed:**
| Rule | Reality |
|------|---------|
| MVVM only | **Not followed** — code-behind dominates |
| Business logic never in Views | **Partially violated** — some validation/UI logic in controls |
| Single opening balance path | **Violated** — legacy party handlers remain |

---

## 18. Coding Standards

### Naming
- PascalCase for types, methods, properties
- Interfaces prefixed with `I`
- Commands: `VerbNounCommand` (e.g. `CreateOpeningBalanceCommand`)
- Handlers: `VerbNounHandler`
- UI services: `ModuleUiService`
- Persistence entities suffixed `Entity` in Infrastructure models
- Domain entities without suffix

### Folder conventions
- Bounded context folders: `Commands/Customers`, `UseCases/Finance`, `Controls/Inventory`
- View factories: `Views/{Module}/{Module}Views.cs`
- One handler class per command common pattern in `*Handlers.cs` files

### Interfaces
- Application abstractions in `ERPSystem.Application.Abstractions`
- Implementations `internal sealed` in Infrastructure where appropriate

### Async usage
- `async Task` / `async Task<T>` throughout handlers and UI event handlers
- `CancellationToken` parameters on application services

### Exceptions
- Domain: `InventoryException`, `ValidationException` (inventory engine)
- Application: `ApplicationResult` discriminated outcomes (Success, ValidationFailed, NotFound, PermissionDenied)
- UI: `ApplicationResultPresenter.Present(result)` pattern

### Logging
- `Microsoft.Extensions.Logging` registered; EF SQL logging at Warning level
- **Unable to determine** structured logging conventions beyond console

### Comments
- XML doc comments on key aggregates and engines (Arabic + English mixed in UI strings)
- Architecture notes in DocumentEngine `.csproj`

### Dependency injection
- Constructor injection in handlers and engines
- WPF singleton UiServices with `IServiceScopeFactory` for scoped handler resolution
- `AppServices.Initialize(provider)` static service locator for controls (**tight coupling to UI**)

---

## 19. Glossary

| Term (AR / EN) | Meaning in this ERP |
|----------------|---------------------|
| **حاوية / Container** | China import shipment unit containing fabric rolls |
| **تفصيل الأطوال / Detailing** | Warehouse process assigning specific fabric rolls/meters to sales lines |
| **مواد أول مدة / Opening Stock** | Inventory opening balance (rolls/meters) — inventory module path |
| **أرصدة افتتاحية / Opening Balance** | Unified finance opening balances (all types) via `IOpeningBalanceEngine` |
| **رصيد افتتاحي عملاء / Customer AR Opening** | Customer receivable balance before go-live |
| **رصيد افتتاحي موردين / Supplier AP Opening** | Supplier payable balance before go-live |
| **سند قبض / Receipt Voucher** | Cash in from customer (AR reduction) |
| **سند صرف / Payment Voucher** | Cash out to supplier (AP reduction) |
| **ميزان مراجعة / Trial Balance** | GL trial balance report |
| **دفتر اليومية / Journal** | General ledger journal entries |
| **ذمم مدينة / Receivables (AR)** | Customer balances owed to company |
| **ذمم دائنة / Payables (AP)** | Amounts owed to suppliers |
| **صندوق / Cashbox** | Physical/virtual cash account linked to GL |
| **مناقلة / Transfer** | Stock or cash movement between warehouses/cashboxes |
| **جرد / Stocktake** | Physical inventory count vs system |
| **تكلفة الوصول / Landing Cost** | China import shared costs allocated to fabric |
| **مركز العمليات / Operations Center** | Document-level workspace (tabs + quick actions) |
| **شريك / Capital Partner** | Equity partner; investments and profit shares |
| **مصروف / Expense** | Operating expense with optional installments |
| **فاتورة بيع / Sales Invoice** | Fabric sales document driving inventory + revenue |
| **فاتورة شراء / Purchase Invoice** | Purchase driving inventory + payables |
| **رول / Roll** | Physical fabric roll traceability unit |
| **متر / Meter** | Quantity unit for fabric |
| **ERP PRO** | Product name used in UI branding |

---

## 20. Overall Project Health

### Strengths
- **Clear layered architecture** with real separation of Domain, Application, and Infrastructure
- **Mature inventory + accounting integration** through dedicated engines
- **Rich domain model** for textile trading (rolls, containers, detailing, landing cost)
- **Consistent CQRS handler pattern** scalable for new modules
- **Operations Center UX pattern** gives enterprise feel for document lifecycle
- **PostgreSQL + EF migrations** with extensive seed data and permissions model
- **Standalone DocumentEngine** ready for future web/mobile reporting
- **Strict build quality gate** (`TreatWarningsAsErrors`)

### Weaknesses
- **UI layer not MVVM** — large code-behind files hinder testability
- **Static service locator (`AppServices`)** couples controls to DI container
- **Placeholder surface area** still large (~28 submodules with no DB per audit)
- **Duplicate business paths** (opening balances, finance vs accounting views)
- **DocumentEngine not integrated** — dual PDF/report strategy
- **Mock/placeholder services** mask incomplete features in production UI

### Architecture quality
**Good foundation, uneven presentation layer.** Domain and application layers follow Clean Architecture and DDD patterns reasonably well. WPF layer evolved as a rapid application shell with programmatic UI — architecturally acceptable for speed but creates long-term maintenance cost.

### Maintainability
**Moderate.** Handler-per-use-case is easy to navigate. Finding UI entry points requires following `SubmoduleRegistry` → `*Views` → `Controls`. Lack of ViewModels and heavy code-behind increases change risk.

### Scalability
- **Database:** PostgreSQL suitable for multi-user ERP; branch/company ids present on key entities
- **Application layer:** Stateless handlers — could host behind API with additional project
- **UI:** Desktop WPF — single-client scale; **multi-user concurrency rules Unable to determine from current source**
- **Batch import:** Synchronous Excel parsing — may not scale to 100k+ rows without background processing

### Performance considerations
- EF `AsNoTracking()` used in read paths in engines
- Inventory roll creation loops in `PostOpeningStockAsync` — potential bottleneck for large opening stock
- Global search hits multiple tables — index strategy **Unable to determine from current source**
- WPF DataGrid binding to in-memory lists (pagination varies by screen)

### Recommendations (architect-level)
1. **Complete opening balance unification** — single engine path, inventory integration, context menus, idempotency tests.
2. **Decide DocumentEngine vs QuestPDF** — integrate or document permanent dual stack.
3. **Introduce ViewModels incrementally** for new screens OR adopt explicit MVP with UiServices (document the chosen pattern).
4. **Replace `AppServices` static locator** with constructor injection into controls via factories.
5. **Close placeholder submodules** or remove from navigation until ready (reduce UX debt).
6. **Add architecture decision records (ADRs)** in `Documentation/` for major choices.
7. **Automated integration tests** for posting engines (GL balance, movement idempotency).

---

## Appendix A — Related internal documents

| File | Topic |
|------|-------|
| `docs/module-connection-status.md` | Submodule PostgreSQL connectivity audit |
| `docs/inventory-engine-audit.md` | Inventory engine design |
| `docs/ERP_PRO_DOMAIN_FOUNDATION.md` | Domain foundation |
| `docs/ERP_PRO_APPLICATION_LAYER_REPORT.md` | Application layer |
| `docs/ui-audit-report.md` | UI audit |
| `docs/mock-data-cleanup-audit.md` | Mock data cleanup |

---

## Appendix B — Document revision

| Field | Value |
|-------|-------|
| Generated | 2026-07-05 |
| Generator | Automated codebase analysis |
| Maintainer | Development team |
| Next review | After major module completion or architecture change |

---

*End of Project Knowledge Base*
