# ERP PRO — Application Layer Report

**Project:** ERP PRO  
**Phase:** B — Application Layer (No Database)  
**Technology:** C# / .NET 9 class library  
**Date:** 2026-06-26  
**References:** [`ERP_PRO_DOMAIN_FOUNDATION.md`](ERP_PRO_DOMAIN_FOUNDATION.md), [`ERP_PRO_DOMAIN_IMPLEMENTATION_REPORT.md`](ERP_PRO_DOMAIN_IMPLEMENTATION_REPORT.md)

---

## 1. Project Structure

```
ERPSystem.Application/
├── Abstractions/
│   ├── IUseCaseHandler.cs
│   ├── IUnitOfWork.cs
│   ├── Repositories/          (14 repository interfaces)
│   └── Services/              (7 service abstractions)
├── Commands/
│   ├── Accounting/JournalEntryCommands.cs
│   ├── Containers/ContainerCommands.cs
│   ├── Customers/CustomerCommands.cs
│   ├── Finance/FinanceCommands.cs
│   └── Sales/SalesInvoiceCommands.cs
├── Queries/
│   ├── Containers/ContainerQueries.cs
│   ├── Customers/CustomerQueries.cs
│   ├── Dashboard/DashboardQueries.cs
│   ├── Reports/ReportQueries.cs
│   ├── Sales/SalesQueries.cs
│   └── Warehouses/WarehouseQueries.cs
├── DTOs/
│   ├── Containers/ContainerDtos.cs
│   ├── Customers/CustomerDtos.cs
│   ├── Dashboard/DashboardDtos.cs
│   ├── Finance/FinanceDtos.cs
│   ├── Sales/SalesDtos.cs
│   └── Warehouses/WarehouseDtos.cs
├── UseCases/
│   ├── Accounting/JournalEntryHandlers.cs
│   ├── Containers/ContainerHandlers.cs
│   ├── Customers/CustomerHandlers.cs
│   ├── Finance/FinanceHandlers.cs
│   ├── Sales/SalesInvoiceHandlers.cs
│   └── Queries/
│       ├── CustomerQueryHandlers.cs
│       └── OperationsQueryHandlers.cs
├── Results/ApplicationResult.cs
├── Common/
│   ├── ApplicationExceptionMapper.cs
│   └── PagedResult.cs
├── Mapping/DomainMappers.cs
├── Validation/ApplicationValidators.cs
├── Notifications/ApplicationNotifications.cs
├── Documents/DocumentRequests.cs
├── Services/ApplicationServiceRegistration.cs
└── ERPSystem.Application.csproj
```

**Total:** 44 C# source files. References `ERPSystem.Domain` only.

---

## 2. Repository Interfaces

| Interface | Aggregate / Entity | Key Methods |
|-----------|-------------------|-------------|
| `ICustomerRepository` | CustomerAggregate | GetById, GetList, Add, Update |
| `ISupplierRepository` | SupplierAggregate | GetById, GetList, Add, Update |
| `IChinaContainerRepository` | ContainerAggregate | GetById, GetByNumber, GetList, Add, Update |
| `IFabricCatalogRepository` | FabricItem, FabricColor, FabricCategory | GetItemById, GetItems, GetCategories |
| `IWarehouseRepository` | WarehouseAggregate | GetById, GetList, Add, Update |
| `ISalesInvoiceRepository` | SalesInvoiceAggregate | GetById, GetList, GetDetailingQueue, Add, Update |
| `IPurchaseInvoiceRepository` | PurchaseInvoice | GetById, GetList, Add, Update |
| `IReceiptVoucherRepository` | ReceiptVoucher | GetById, GetList, Add, Update |
| `IPaymentVoucherRepository` | PaymentVoucher | GetById, GetList, Add, Update |
| `ICashboxRepository` | Cashbox | GetById, GetList, Add, Update |
| `IJournalEntryRepository` | AccountingAggregate | GetById, GetByNumber, GetList, Add, Update |
| `IAuditLogRepository` | AuditLog | Add, GetByEntity |
| `IUserRepository` | User, Role, Permission | GetById, GetByUsername, GetRoles, HasPermission |
| `IUnitOfWork` | — | SaveChanges, Begin/Commit/RollbackTransaction |

No implementations — contracts only.

---

## 3. Service Abstractions

| Interface | Purpose |
|-----------|---------|
| `ICurrentUserService` | Authenticated user context |
| `ICurrentBranchService` | Active company/branch context |
| `IPermissionService` | Authorization checks |
| `INotificationService` | Publish application notifications |
| `IDocumentPreviewService` | Future document engine preview |
| `INumberingService` | Document number generation |
| `IDateTimeProvider` | Testable date/time |

---

## 4. Commands

### Customers
- `CreateCustomerCommand`
- `UpdateCustomerCommand`
- `DeactivateCustomerCommand`

### Containers
- `CreateChinaContainerCommand`
- `ImportContainerExcelCommand` (+ `ImportContainerLineCommand`)
- `CalculateLandingCostCommand`
- `ApproveContainerCommand`
- `MoveContainerToWarehouseCommand`

### Sales
- `CreateSalesInvoiceDraftCommand` (+ `SalesInvoiceLineCommand`)
- `SendSalesInvoiceToWarehouseCommand`
- `CompleteWarehouseDetailingCommand` (+ `RollLengthEntryCommand`)
- `ApproveSalesInvoiceCommand`
- `CancelSalesInvoiceCommand`

### Finance
- `CreateReceiptVoucherCommand`, `ApproveReceiptVoucherCommand`, `PostReceiptVoucherCommand`
- `CreatePaymentVoucherCommand`, `ApprovePaymentVoucherCommand`, `PostPaymentVoucherCommand`

### Accounting
- `CreateJournalEntryCommand` (+ `JournalEntryLineCommand`)
- `ApproveJournalEntryCommand`, `PostJournalEntryCommand`, `ReverseJournalEntryCommand`

---

## 5. Queries

| Query | Returns |
|-------|---------|
| `GetDashboardSummaryQuery` | `DashboardSummaryDto` |
| `GetCustomerListQuery` | `PagedResult<CustomerListDto>` |
| `GetCustomerOperationsCenterQuery` | `CustomerOperationsCenterDto` |
| `GetCustomerStatementQuery` | `CustomerStatementDto` |
| `GetChinaContainerListQuery` | `PagedResult<ContainerListDto>` |
| `GetContainerOperationsCenterQuery` | `ContainerOperationsCenterDto` |
| `GetWarehouseListQuery` | `IReadOnlyList<WarehouseListDto>` |
| `GetWarehouseOperationsCenterQuery` | `WarehouseOperationsCenterDto` |
| `GetSalesInvoiceListQuery` | `PagedResult<SalesInvoiceDto>` |
| `GetSalesInvoiceOperationsCenterQuery` | `SalesInvoiceOperationsCenterDto` |
| `GetWarehouseDetailingQueueQuery` | `IReadOnlyList<WarehouseDetailingDto>` |
| `GetReportPreviewQuery` | `Dictionary<string, object>` (preview stub) |

---

## 6. DTOs

| DTO Group | Types |
|-----------|-------|
| Customers | `CustomerListDto`, `CustomerDetailsDto`, `CustomerStatementDto`, `CustomerStatementLineDto`, `CustomerOperationsCenterDto` |
| Containers | `ContainerListDto`, `ContainerDetailsDto`, `ContainerItemDto`, `LandingCostDto`, `ContainerOperationsCenterDto` |
| Warehouses | `WarehouseListDto`, `WarehouseStockDto`, `FabricItemDto`, `WarehouseOperationsCenterDto` |
| Sales | `SalesInvoiceDto`, `SalesInvoiceLineDto`, `WarehouseDetailingDto`, `WarehouseDetailingRollDto`, `SalesInvoiceOperationsCenterDto` |
| Finance | `ReceiptVoucherDto`, `PaymentVoucherDto`, `JournalEntryDto`, `JournalEntryLineDto` |
| Dashboard | `DashboardSummaryDto` |

Domain entities are never exposed to the UI layer — all output goes through DTOs and mappers in `Mapping/DomainMappers.cs`.

---

## 7. Use Case Handlers

### Command Handlers

| Handler | Command | Domain Operations |
|---------|---------|-------------------|
| `CreateCustomerHandler` | CreateCustomerCommand | Customer.Create → CustomerValidator → repository |
| `DeactivateCustomerHandler` | DeactivateCustomerCommand | Customer.Deactivate |
| `CreateChinaContainerHandler` | CreateChinaContainerCommand | ContainerAggregate.CreateDraft |
| `CalculateLandingCostHandler` | CalculateLandingCostCommand | LandingCost.Create → SetLandingCost |
| `ApproveContainerHandler` | ApproveContainerCommand | ContainerCanBeApprovedSpecification → Approve |
| `MoveContainerToWarehouseHandler` | MoveContainerToWarehouseCommand | MoveToWarehouse |
| `CreateSalesInvoiceDraftHandler` | CreateSalesInvoiceDraftCommand | SalesInvoiceAggregate.CreateDraft + AddItem |
| `SendSalesInvoiceToWarehouseHandler` | SendSalesInvoiceToWarehouseCommand | SendToWarehouse |
| `CompleteWarehouseDetailingHandler` | CompleteWarehouseDetailingCommand | EnterRollLength → CompleteDetailing |
| `ApproveSalesInvoiceHandler` | ApproveSalesInvoiceCommand | CreditLimitChecker → Approve → customer balance |
| `CancelSalesInvoiceHandler` | CancelSalesInvoiceCommand | Cancel |
| `CreateReceiptVoucherHandler` | CreateReceiptVoucherCommand | ReceiptVoucher.CreateDraft |
| `PostReceiptVoucherHandler` | PostReceiptVoucherCommand | Post → customer receipt → cashbox |
| `CreatePaymentVoucherHandler` | CreatePaymentVoucherCommand | PaymentVoucher.CreateDraft |
| `PostPaymentVoucherHandler` | PostPaymentVoucherCommand | Post → cashbox payment |
| `CreateJournalEntryHandler` | CreateJournalEntryCommand | AccountingAggregate.CreateDraft + AddLine |
| `PostJournalEntryHandler` | PostJournalEntryCommand | BalancedJournalSpecification → Post |
| `ReverseJournalEntryHandler` | ReverseJournalEntryCommand | CreateReversal |

### Query Handlers

| Handler | Query |
|---------|-------|
| `GetDashboardSummaryHandler` | GetDashboardSummaryQuery |
| `GetCustomerListHandler` | GetCustomerListQuery |
| `GetCustomerOperationsCenterHandler` | GetCustomerOperationsCenterQuery |
| `GetCustomerStatementHandler` | GetCustomerStatementQuery |
| `GetChinaContainerListHandler` | GetChinaContainerListQuery |
| `GetContainerOperationsCenterHandler` | GetContainerOperationsCenterQuery |
| `GetWarehouseListHandler` | GetWarehouseListQuery |
| `GetWarehouseOperationsCenterHandler` | GetWarehouseOperationsCenterQuery |
| `GetSalesInvoiceListHandler` | GetSalesInvoiceListQuery |
| `GetSalesInvoiceOperationsCenterHandler` | GetSalesInvoiceOperationsCenterQuery |
| `GetWarehouseDetailingQueueHandler` | GetWarehouseDetailingQueueQuery |
| `GetReportPreviewHandler` | GetReportPreviewQuery |

All handlers use repository interfaces only — no database access.

---

## 8. Result Model

```csharp
ApplicationResultStatus: Success | Failure | ValidationFailed | NotFound | Conflict | PermissionDenied

ApplicationResult          — non-generic (commands without return value)
ApplicationResult<T>       — generic (commands/queries with payload)
ValidationError            — Field + Message
OperationMessage           — Code + Text + IsWarning
```

Factory methods: `Success()`, `Failure()`, `ValidationFailed()`, `NotFound()`, `Conflict()`, `PermissionDenied()`.

Domain exceptions are mapped via `ApplicationExceptionMapper.ToFailureResult()`.

---

## 9. Validation Layer

**Application validation** (`Validation/ApplicationValidators.cs`) — input shape:
- Required IDs (CustomerId, WarehouseId, ContainerId)
- Required strings (code, name, container number)
- Positive amounts and lengths
- Non-empty line collections

**Domain validation** — business rules (called inside handlers):
- `CustomerValidator`, `ContainerValidator`, `SalesInvoiceValidator`
- `LandingCostValidator`, `JournalValidator`
- Domain specifications (`InvoiceCanBeApprovedSpecification`, etc.)
- Domain services (`CreditLimitChecker`, `AccountingPostingPolicy`)

---

## 10. Notifications & Documents

### Notifications (models only)
- `SalesInvoiceDetailedNotification`
- `SalesInvoiceApprovedNotification`
- `ContainerApprovedNotification`
- `CustomerCreditLimitExceededNotification`
- `JournalEntryPostedNotification`
- `ReceiptVoucherPostedNotification`
- `WarehouseStockLowNotification`

Published via `INotificationService` from handlers — no real notification system yet.

### Document Requests (future Document Engine)
- `PrintSalesInvoiceRequest`
- `PrintCustomerStatementRequest`
- `PrintContainerLandingCostRequest`
- `PrintReceiptVoucherRequest`
- `ExportReportRequest`

No PDF generation in this phase.

---

## 11. Dependency Verification

```
ERPSystem.Domain          ← Pure domain (Phase A) ✓
ERPSystem.Application     ← References Domain only ✓
  ├── No WPF              ✓
  ├── No EF Core          ✓
  ├── No PostgreSQL       ✓
  └── TreatWarningsAsErrors ✓

Domain → Application      ✗ Forbidden (verified)
Application → WPF       ✗ Forbidden (verified)
Application → EF Core     ✗ Forbidden (verified)
```

**Build results:**

```
dotnet build ERPSystem.Domain.csproj       → 0 errors, 0 warnings
dotnet build ERPSystem.Application.csproj  → 0 errors, 0 warnings
```

---

## 12. Ready for Infrastructure Phase

The following contracts are ready for EF Core / PostgreSQL implementation:

| Contract | Infrastructure Implementation |
|----------|------------------------------|
| `ICustomerRepository` | EF Core repository + mapping |
| `IChinaContainerRepository` | EF Core repository + mapping |
| `ISalesInvoiceRepository` | EF Core repository + mapping |
| `IWarehouseRepository` | EF Core repository + mapping |
| `IJournalEntryRepository` | EF Core repository + mapping |
| `IReceiptVoucherRepository` | EF Core repository + mapping |
| `IPaymentVoucherRepository` | EF Core repository + mapping |
| `ICashboxRepository` | EF Core repository + mapping |
| `IUnitOfWork` | DbContext wrapper |
| `INumberingService` | DB sequence / counter table |
| `ICurrentUserService` | WPF session / auth context |
| `ICurrentBranchService` | WPF branch selector |
| `IPermissionService` | Role/permission lookup |
| `INotificationService` | In-process or SignalR dispatcher |
| `IAuditLogRepository` | Append-only audit table |

Recommended next project: `ERPSystem.Infrastructure` referencing Application + Domain.

---

## 13. Remaining Before UI Wiring

| # | Item | Priority |
|---|------|----------|
| 1 | Create `ERPSystem.Infrastructure` with EF Core repositories | High |
| 2 | PostgreSQL migrations from domain model | High |
| 3 | DI composition root in WPF (`App.xaml.cs` or `ServiceCollection`) | High |
| 4 | Replace mock sample data with handler calls | High |
| 5 | Migrate UI enums to domain enums | High |
| 6 | Implement `INumberingService` with DB counters | Medium |
| 7 | Implement `IPermissionService` with user session | Medium |
| 8 | Wire `INotificationService` to UI toast/dialog | Medium |
| 9 | Add `UpdateCustomerHandler` implementation | Medium |
| 10 | Add `ImportContainerExcelHandler` implementation | Medium |
| 11 | Add `ApproveJournalEntryHandler` implementation | Medium |
| 12 | Customer statement query with real ledger entries | Medium |
| 13 | Unit test project for handlers | Medium |
| 14 | Document engine (PDF) | Low |

---

## Acceptance Criteria Checklist

| # | Criterion | Status |
|---|-----------|--------|
| 1 | ERPSystem.Application project created | ✅ |
| 2 | Application references Domain only | ✅ |
| 3 | Domain remains independent | ✅ |
| 4 | No EF Core added | ✅ |
| 5 | No PostgreSQL added | ✅ |
| 6 | Main workflow commands exist | ✅ |
| 7 | Main screen queries exist | ✅ |
| 8 | DTOs created for UI output | ✅ |
| 9 | Use case handlers use repository interfaces only | ✅ |
| 10 | Unified ApplicationResult exists | ✅ |
| 11 | Solution builds with 0 errors and 0 warnings | ✅ |

---

*End of Application Layer Report — Phase B complete.*
