# ERP PRO — Vertical Slice #1: Customers Module Report

**Phase:** D — End-to-end Customers vertical slice  
**Date:** 2026-06-26  
**Status:** Complete  
**Build:** WPF + Application + Infrastructure + Domain — **0 errors, 0 warnings**

---

## 1. Screens Migrated

| Screen | Control / View | Data Source |
|--------|----------------|-------------|
| Customer List | `CustomerListPageControl` | `GetCustomerListHandler` → PostgreSQL |
| Add / Edit Customer | `CustomerFormControl` | `CreateCustomerHandler`, `UpdateCustomerHandler`, `GetCustomerDetailsHandler` |
| Customer Operations Center | `CustomerOperationsCenterControl` | `GetCustomerOperationsCenterHandler` |
| Customer Account Statement | `CustomerAccountStatementControl` | `GetCustomerStatementHandler` |
| Dashboard — top debtors widget | `DashboardModule.LoadDebtCustomersAsync` | `GetCustomerListHandler` |
| Right-click menu actions | `RowContextMenuService` + `CustomerActionRouter` | Application commands / workspace |
| Quick actions (ops center) | `MockQuickActionRouter` + `CustomerActionRouter` | Application commands |

---

## 2. Mock Components Removed (Customers)

| Removed / Replaced | Replacement |
|--------------------|-------------|
| `PartyViews.CustomerList()` + `CustomerSampleData.Generate(40)` | `CustomerListPageControl` |
| `PartyViews.PartyForm` for customers | `CustomerFormControl` |
| `OperationsCenterFactory.BuildCustomer` mock shell | `CustomerOperationsCenterControl` |
| `CustomerAccountStatementControl.SeedSampleData()` | PostgreSQL ledger via `GetCustomerStatementHandler` |
| `MockInteractionService.OpenCustomerStatement/Center` default mock fallback | Requires `CustomerListRow` with real `Guid` |
| Dashboard debt widget `CustomerSampleData` | Live customer list ordered by balance |

**Retained (non-customer mock):** `CustomerModels.cs` / `CustomerSampleData` still exist for legacy UI enums and unused mock paths in other modules — **no customer screen loads mock data**.

---

## 3. Queries Implemented / Used

| Query | Handler | Purpose |
|-------|---------|---------|
| `GetCustomerListQuery` | `GetCustomerListHandler` | Paginated list + DB search |
| `GetCustomerDetailsQuery` | `GetCustomerDetailsHandler` | Edit form load |
| `GetCustomerOperationsCenterQuery` | `GetCustomerOperationsCenterHandler` | Ops center header/KPIs |
| `GetCustomerStatementQuery` | `GetCustomerStatementHandler` | Account statement lines |

---

## 4. Commands Implemented / Used

| Command | Handler | UI Trigger |
|---------|---------|--------------|
| `CreateCustomerCommand` | `CreateCustomerHandler` | Add customer form |
| `UpdateCustomerCommand` | `UpdateCustomerHandler` | Edit customer form |
| `DeactivateCustomerCommand` | `DeactivateCustomerHandler` | Right-click / ops center quick action |

All commands enforce permissions via `IPermissionService` and publish notifications via `INotificationService`.

---

## 5. Repository Usage

| Interface | Implementation | Operations |
|-----------|----------------|------------|
| `ICustomerRepository` | `CustomerRepository` | `GetById`, `GetList`, **`GetPagedAsync`**, `Add`, `Update` |
| `ISalesInvoiceRepository` | `SalesInvoiceRepository` | Statement + ops center invoice counts |
| `IReceiptVoucherRepository` | `ReceiptVoucherRepository` | Statement credit lines |
| `IUserRepository` | `UserRepository` | Permission checks (`WpfPermissionService`) |
| `IUnitOfWork` | `EfUnitOfWork` | Transaction commit on commands |

**Paging:** `GetPagedAsync` applies `AsNoTracking`, company filter, active-only (`IsActive`), ILIKE-style `Contains` search on code/name, DB-level `Skip/Take`.

---

## 6. PostgreSQL Tables Used

Schema: **`parties`** (and related finance/sales for statement)

| Table | Usage |
|-------|--------|
| `parties.customers` | CRUD, list, search, soft delete |
| `documents.document_counters` | Auto customer code (`NextCustomerCodeAsync`) |
| `sales.sales_invoices` | Statement debits, open invoice count |
| `finance.receipt_vouchers` | Statement credits |
| `identity.users` / role permissions | `customers.create`, `customers.deactivate` |

---

## 7. Performance Notes

- List queries use **DB pagination** and **`AsNoTracking`** projections to DTOs via `CustomerMapper`.
- Search runs in PostgreSQL (`Contains` on code / `NameAr` / `NameEn`), not in-memory.
- Statement handler loads invoices + receipts in two queries (not N+1 per line).
- Ops center loads customer + invoice list once per open.
- Local type/status filters on the list apply only to the current page (acceptable for slice; can move to query later).

---

## 8. Remaining Mock Areas (Outside Customers Slice)

| Module | Mock Still Used |
|--------|-----------------|
| Suppliers | `SupplierSampleData` |
| Sales | `SalesSampleData` |
| China Import | `ChinaImportSampleData` |
| Inventory / HR / Purchases / Accounting | Sample data in respective views |
| Customer submodule tabs (Invoices, Opening balances standalone pages) | Placeholder grids |

---

## 9. Lessons Learned for Next Slices

1. **Wire DI first** — `App.xaml.cs` + `AddInfrastructure` + `AddApplication` + session services (`ICurrentUserService`, `ICurrentBranchService`, `IPermissionService`, `INotificationService`).
2. **UI facade per module** — `CustomerUiService` wraps handlers; WPF never references repositories.
3. **Row model bridge** — `CustomerListRow` wraps `CustomerListDto` for grids, context menus, and workspace navigation.
4. **Namespace collision** — `ERPSystem.Application` conflicts with `Application.Current`; use `System.Windows.Application.Current` in WPF after referencing Application project.
5. **Exclude nested projects from WPF compile** — monorepo layout requires `<Compile Remove="ERPSystem.Application/**" />` etc.
6. **Server-side search flag** — `ErpListModuleControl.EnableServerSideSearch()` avoids double-filtering.
7. **Soft delete** — filter `IsActive` in repository list; never hard-delete.

---

## 10. Recommendation for Warehouse Slice (Next)

1. Copy the Customers pattern: `WarehouseListRow`, `WarehouseUiService`, `WarehouseListPageControl`.
2. Reuse existing `GetWarehouseListHandler`, `GetWarehouseOperationsCenterQuery`, warehouse commands when added.
3. Wire inventory submodule list first, then operations center, then stock movement commands.
4. Keep mock for fabric catalog until catalog slice is scheduled.
5. Add integration tests for repository paging and handler permission checks (Customers handlers are the template).

---

## Architecture Diagram

```
WPF (CustomerListPageControl / Form / Statement / Ops Center)
        ↓
CustomerUiService
        ↓
Application Handlers (Commands / Queries)
        ↓
ICustomerRepository (+ invoice/receipt repos for statement)
        ↓
CustomerRepository (EF Core)
        ↓
PostgreSQL (parties.customers, …)
```

---

## Acceptance Criteria Checklist

| # | Criterion | Status |
|---|-----------|--------|
| 1 | Customers module uses PostgreSQL only | ✅ |
| 2 | No mock customer data in customer screens | ✅ |
| 3 | UI communicates only with Application layer | ✅ |
| 4 | Operations Center uses real data | ✅ |
| 5 | Statement uses real data | ✅ |
| 6 | CRUD fully operational | ✅ |
| 7 | Soft delete works | ✅ |
| 8 | Search/filter use PostgreSQL (search) | ✅ |
| 9 | Permissions enforced | ✅ |
| 10 | Notifications work | ✅ |
| 11 | Build 0 errors / 0 warnings | ✅ |
| 12 | Reference architecture for remaining modules | ✅ |
