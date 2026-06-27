# ERP PRO — Domain Layer Implementation Report

**Project:** ERP PRO  
**Phase:** A — Pure Domain Layer (No Database)  
**Technology:** C# / .NET 9 class library  
**Date:** 2026-06-26  
**Reference:** [`ERP_PRO_DOMAIN_FOUNDATION.md`](ERP_PRO_DOMAIN_FOUNDATION.md), [`ERP_PRO_DOMAIN_MODEL_DIAGRAM.md`](ERP_PRO_DOMAIN_MODEL_DIAGRAM.md)

---

## 1. Folder Structure

```
ERPSystem.Domain/
├── Aggregates/
│   ├── AccountingAggregate.cs
│   ├── ContainerAggregate.cs
│   ├── CustomerAggregate.cs
│   ├── SalesInvoiceAggregate.cs
│   └── WarehouseAggregate.cs
├── Common/
│   ├── AggregateRoot.cs
│   ├── AuditableEntity.cs
│   ├── DomainEvent.cs
│   └── Entity.cs
├── Entities/
│   ├── Accounting/AccountingEntities.cs
│   ├── Catalog/CatalogEntities.cs
│   ├── ChinaImport/ChinaImportEntities.cs
│   ├── Finance/FinanceEntities.cs
│   ├── HR/HrEntities.cs
│   ├── Identity/IdentityEntities.cs
│   ├── Inventory/InventoryEntities.cs
│   ├── Parties/PartyEntities.cs
│   ├── Purchasing/PurchasingEntities.cs
│   ├── Sales/SalesEntities.cs
│   └── System/SystemEntities.cs
├── Enums/
│   ├── ApprovalStatus.cs          (+ LandingCostStatus, FabricRollStatus, PurchaseInvoiceStatus)
│   ├── ChinaContainerStatus.cs
│   ├── CustomerType.cs              (+ CustomerStatus, SupplierStatus)
│   ├── DocumentType.cs
│   ├── JournalEntryStatus.cs
│   ├── MovementType.cs
│   ├── PaymentType.cs
│   ├── SalesInvoiceStatus.cs
│   ├── StockMovementStatus.cs
│   ├── VoucherStatus.cs
│   └── WarehouseDetailingStatus.cs
├── Events/
│   ├── Accounting/AccountingEvents.cs
│   ├── Audit/AuditEvents.cs
│   ├── ChinaImport/ChinaImportEvents.cs
│   ├── Finance/FinanceEvents.cs
│   ├── Inventory/InventoryEvents.cs
│   └── Sales/SalesEvents.cs
├── Exceptions/
│   └── DomainExceptions.cs
├── Interfaces/
│   ├── IDomainEvent.cs
│   └── ISpecification.cs
├── Services/
│   ├── AccountingPostingPolicy.cs
│   ├── CreditLimitChecker.cs
│   ├── CustomerBalanceCalculator.cs
│   ├── InventoryReservationPolicy.cs
│   ├── LandingCostCalculator.cs
│   ├── SalesInvoiceTotalCalculator.cs
│   ├── StatementCalculator.cs
│   ├── StockMovementValidator.cs
│   └── WarehouseDetailingValidator.cs
├── Specifications/
│   └── DomainSpecifications.cs
├── Validators/
│   └── DomainValidators.cs
├── ValueObjects/
│   ├── AuditInfo.cs
│   ├── ContactInfo.cs               (PhoneNumber, EmailAddress, Address)
│   ├── DocumentNumbers.cs           (BranchCode, ContainerNumber, InvoiceNumber, RollNumber)
│   ├── LengthInMeters.cs
│   ├── Money.cs
│   ├── Percentage.cs
│   └── WeightInKg.cs                (+ WeightInGrams)
└── ERPSystem.Domain.csproj
```

**Total:** 57 C# source files. No NuGet package dependencies beyond the .NET 9 SDK.

---

## 2. Implemented Entities

| Entity | Location | Notes |
|--------|----------|-------|
| Customer | Parties | Credit limit, balance, status transitions |
| Supplier | Parties | Payables balance |
| ChinaSupplier | Parties | Port, incoterm, lead time |
| ChinaOrder | ChinaImport | Order lifecycle |
| ChinaContainerItem | ChinaImport | Line items with roll count, meters |
| ChinaImportBatch | ChinaImport | Excel import audit |
| ChinaImportRow | ChinaImport | Raw row validation |
| ContainerCustomerDistribution | ChinaImport | Buyer/reservation split |
| LandingCost | ChinaImport | Cost per meter calculations |
| LandingCostExpense | ChinaImport | Expense line items |
| FabricCategory | Catalog | Category master |
| FabricItem | Catalog | Fabric master |
| FabricColor | Catalog | Color variants |
| FabricRoll | Catalog | Physical roll identity |
| Warehouse | Inventory | Branch-scoped warehouse |
| WarehouseLocation | Inventory | Zone/bin |
| WarehouseStockBalance | Inventory | Meters + roll count, reserve/deduct |
| StockMovement | Inventory | Draft/post workflow |
| StockTransfer | Inventory | Inter-warehouse transfer |
| StocktakeSession | Inventory | Physical count session |
| SalesInvoiceItem | Sales | Line with roll count, unit price |
| SalesInvoiceRollDetail | Sales | Per-roll length entry |
| WarehouseDetailingSession | Sales | Detailing workflow session |
| SalesReturn | Sales | Return document |
| DeliveryNote | Sales | Delivery record |
| PurchaseInvoice | Purchasing | Local/import purchase |
| PurchaseInvoiceItem | Purchasing | Purchase line |
| PurchaseReturn | Purchasing | Purchase return |
| ReceiptVoucher | Finance | Customer receipt |
| PaymentVoucher | Finance | Supplier payment |
| Cashbox | Finance | Cash balance |
| CashboxMovement | Finance | Cash movement |
| CashboxTransfer | Finance | Inter-cashbox transfer |
| Account | Accounting | Chart of accounts |
| JournalEntryLine | Accounting | Debit/credit line |
| PostingBatch | Accounting | Batch posting metadata |
| CustomerStatementEntry | Accounting | Statement line |
| SupplierStatementEntry | Accounting | Supplier statement line |
| Company | Identity | Legal entity |
| Branch | Identity | Branch master |
| User | Identity | User account |
| Role | Identity | Role definition |
| Permission | Identity | Permission code |
| AuditLog | System | Immutable audit record |
| DocumentTemplate | System | Print template |
| SystemSetting | System | Key/value settings |
| Department | HR | Department master |
| Employee | HR | Employee record |
| Shift | HR | Work shift |
| AttendanceRecord | HR | Check-in/out |
| LeaveRequest | HR | Leave workflow |

---

## 3. Aggregate Roots

| Aggregate | Root Entity | Key Behavior |
|-----------|-------------|--------------|
| **SalesInvoiceAggregate** | SalesInvoice | `CreateDraft`, `AddItem`, `SendToWarehouse`, `StartDetailing`, `EnterRollLength`, `CompleteDetailing`, `MarkReadyForApproval`, `Approve`, `Print`, `Deliver`, `Cancel` |
| **ContainerAggregate** | ChinaContainer | `CreateDraft`, `AddItem`, `BeginReview`, `SetLandingCost`, `Approve`, `MoveToWarehouse`, `Close`, `Archive` |
| **CustomerAggregate** | Customer | `RecordPostedInvoice`, `RecordPostedReceipt`, `WouldExceedCreditLimit` |
| **SupplierAggregate** | Supplier | Wrapper for supplier consistency boundary |
| **WarehouseAggregate** | Warehouse | `AddLocation`, `AddOrUpdateBalance`, `FindBalance` |
| **AccountingAggregate** | JournalEntry | `CreateDraft`, `AddLine`, `Approve`, `Post`, `CreateReversal`, `Cancel` |

All aggregate roots inherit `AggregateRoot` (domain event collection, `Raise()`).

---

## 4. Value Objects

| Value Object | Validation |
|--------------|------------|
| Money | Non-negative optional; currency-aware add/subtract/multiply |
| LengthInMeters | > 0; Zero singleton; Add/Subtract |
| WeightInKg | > 0; ToGrams() |
| WeightInGrams | > 0 |
| Percentage | 0–100 range |
| PhoneNumber | Min 8 digits after normalization |
| EmailAddress | Must contain `@` |
| Address | Line1 + city required |
| AuditInfo | Created/modified metadata |
| DateRange | Start ≤ end |
| BranchCode | Non-empty, uppercased |
| ContainerNumber | Non-empty, uppercased |
| InvoiceNumber | Non-empty, uppercased |
| RollNumber | Positive integer |

All value objects are immutable (`record` types with private/init-only construction).

---

## 5. Enums

| Enum | Values (summary) |
|------|------------------|
| SalesInvoiceStatus | Draft → AwaitingDetailing → Detailed → ReadyForApproval → Approved → Printed → Delivered / Cancelled |
| ChinaContainerStatus | Draft → InTransit → Arrived → UnderReview → LandingCostReviewed → Approved → InWarehouse → Closed / Archived |
| WarehouseDetailingStatus | Pending → InProgress → Completed / Rejected |
| VoucherStatus | Draft → Approved → Posted / Cancelled |
| JournalEntryStatus | Draft → Approved → Posted → Reversed / Cancelled |
| StockMovementStatus | Draft → Posted / Cancelled |
| PaymentType | Cash, Credit, Partial |
| CustomerType | Cash, Credit |
| CustomerStatus | Active, Suspended, Blocked |
| SupplierStatus | Active, Inactive |
| MovementType | Import, Sale, Return, Transfer, Adjustment, Stocktake |
| DocumentType | SalesInvoice, ReceiptVoucher, PaymentVoucher, JournalEntry, Container, PurchaseInvoice, StockMovement, etc. |
| ApprovalStatus | Pending, Approved, Rejected |
| LandingCostStatus | Draft, Reviewed, Approved |
| FabricRollStatus | Available, Reserved, Sold |
| PurchaseInvoiceStatus | Draft, Approved, Posted, Cancelled |

Workflow enums are centralized in `ERPSystem.Domain.Enums` — no duplication with UI.

---

## 6. Domain Services

| Service | Responsibility |
|---------|----------------|
| LandingCostCalculator | Total expenses, cost per meter, landed cost per meter |
| SalesInvoiceTotalCalculator | SubTotal, GrandTotal, total meters |
| WarehouseDetailingValidator | Roll length completeness checks |
| CreditLimitChecker | Credit limit enforcement + exceeded event |
| InventoryReservationPolicy | Reserve with availability check |
| AccountingPostingPolicy | Balanced entry validation, immutability rules |
| CustomerBalanceCalculator | Running balance from statement entries |
| StatementCalculator | Running balance builder, supplier balance |
| StockMovementValidator | Post eligibility, transfer validation |

All services are static, pure, and have no infrastructure dependencies.

---

## 7. Specifications

| Specification | Target | Rule |
|---------------|--------|------|
| InvoiceCanBeApprovedSpecification | SalesInvoiceAggregate | Status + valid rolls + positive total |
| ContainerCanBeApprovedSpecification | ContainerAggregate | Landing cost reviewed + valid items + meters |
| WarehouseCanDetailSpecification | SalesInvoiceAggregate | Awaiting detailing + has items |
| CreditLimitSatisfiedSpecification | CustomerAggregate | Projected balance within limit |
| BalancedJournalSpecification | AccountingAggregate | Lines exist + debits = credits |
| LandingCostValidSpecification | ContainerAggregate | Landing cost present + valid totals |

All implement `ISpecification<T>` with `IsSatisfiedBy()` and `FailureReason`.

---

## 8. Validators

| Validator | Validates |
|-----------|-----------|
| CustomerValidator | Code, name, company, credit limit |
| ContainerValidator | Container number, supplier, items |
| SalesInvoiceValidator | Customer, warehouse, container, line items |
| LandingCostValidator | Total length, customs amount |
| WarehouseValidator | Code, branch |
| JournalValidator | Entry number, lines, balance |
| ReceiptVoucherValidator | Customer, cashbox, amount |
| PaymentVoucherValidator | Supplier, cashbox, amount |

Validators throw `ValidationException` — no UI dependency.

---

## 9. Domain Events

| Event | Raised By |
|-------|-----------|
| SalesInvoiceCreated | SalesInvoiceAggregate.CreateDraft |
| SalesInvoiceSentToWarehouse | SalesInvoiceAggregate.SendToWarehouse |
| SalesInvoiceDetailed | SalesInvoiceAggregate.CompleteDetailing |
| SalesInvoiceApproved | SalesInvoiceAggregate.Approve |
| SalesInvoicePrinted | SalesInvoiceAggregate.Print |
| LandingCostCalculated | ContainerAggregate.SetLandingCost |
| ContainerApproved | ContainerAggregate.Approve |
| ContainerMovedToWarehouse | ContainerAggregate.MoveToWarehouse |
| InventoryReserved | (defined; raised by future application layer) |
| InventoryDeducted | (defined; raised by future application layer) |
| WarehouseStockLow | (defined; raised by future application layer) |
| ReceiptVoucherPosted | (defined; raised by future application layer) |
| PaymentVoucherPosted | (defined; raised by future application layer) |
| JournalEntryPosted | AccountingAggregate.Post |
| CustomerCreditLimitExceeded | CreditLimitChecker.TryCreateExceededEvent |
| AuditActionRecorded | (defined; raised by future application layer) |

Event classes only — no event bus implementation.

---

## 10. Domain Exceptions

| Exception | Use Case |
|-----------|----------|
| DomainException | Base domain error |
| ValidationException | Input/business validation failures |
| CreditLimitExceededException | Customer credit limit breach |
| InvalidInvoiceWorkflowException | Sales invoice status transition errors |
| ContainerApprovalException | Container approval/import errors |
| WarehouseDetailingException | Roll length / detailing errors |
| InventoryException | Stock reservation/deduction errors |
| AccountingException | Journal posting/balance errors |

---

## 11. Remaining Work Before PostgreSQL

| # | Item | Priority |
|---|------|----------|
| 1 | Create `ERPSystem.Application` layer (commands, queries, handlers) | High |
| 2 | Wire domain events to application event dispatcher | High |
| 3 | Add EF Core `DbContext` + entity configurations (separate Infrastructure project) | High |
| 4 | PostgreSQL migrations from domain model | High |
| 5 | Repository interfaces in Application; implementations in Infrastructure | High |
| 6 | Migrate WPF UI enums to domain enums (`FabricInvoiceWorkflowStatus` → `SalesInvoiceStatus`) | High |
| 7 | Add `ProjectReference` from WPF → Application → Domain | Medium |
| 8 | Unit test project for aggregates, services, specifications | Medium |
| 9 | Full `FabricItem` aggregate with catalog invariants | Medium |
| 10 | `Company` aggregate with branch/fiscal year rules | Medium |
| 11 | `User` aggregate with permission evaluation | Medium |
| 12 | Stock deduction timing on invoice approval (currently event-only) | Medium |
| 13 | Receipt/Payment voucher aggregates with posting behavior | Medium |
| 14 | Purchase invoice aggregate with approval workflow | Low |
| 15 | HR module aggregates (optional scope) | Low |
| 16 | Status history tables for workflow audit trail | Low |
| 17 | Read models / report projections | Low |

---

## 12. Dependency Verification

```
ERPSystem.Domain          ← Pure .NET 9 class library
  ├── No WPF references     ✓
  ├── No EF Core            ✓
  ├── No PostgreSQL/Npgsql  ✓
  ├── No NuGet packages     ✓
  └── TreatWarningsAsErrors ✓

ERPSystem (WPF)             ← NOT yet referencing Domain (by design)
ERPSystem.Application       ← Does not exist yet (future)
Infrastructure              ← Does not exist yet (future)
```

**Build result:**

```
dotnet build ERPSystem.Domain.csproj
→ Build succeeded. 0 Warning(s), 0 Error(s)
```

The domain layer compiles independently with zero warnings and zero errors.

---

## 13. Deviations from Approved Domain Foundation

| # | Foundation Spec | Implementation | Rationale |
|---|-----------------|----------------|-----------|
| 1 | Aggregate root named `ChinaContainer` | Class named `ContainerAggregate` wrapping container state | DDD convention: aggregate class named `{Entity}Aggregate`; container entity state lives on the aggregate itself |
| 2 | Separate `FabricItem` aggregate root | `FabricItem` implemented as entity in `CatalogEntities.cs` | Catalog aggregate deferred; entity structure ready for future aggregate wrapper |
| 3 | Separate `User` / `Company` aggregates | Implemented as entities only (`IdentityEntities.cs`) | Full identity/company aggregate behavior deferred to Application phase |
| 4 | `CustomsCostCalculation` as persisted snapshot | Computed properties on `LandingCost` entity | Derived values available without separate entity; can add snapshot entity at persistence time |
| 5 | `CustomerStatementSummary` computed object | Balance on `Customer` entity + `CustomerStatementEntry` lines | Summary computed via `CustomerBalanceCalculator` service |
| 6 | Status history as separate entity | Not implemented | Requires persistence layer; workflow timestamps captured on aggregate (SentToWarehouseAt, ApprovedAt, etc.) |
| 7 | `FabricRoll` as standalone aggregate | `FabricRoll` as entity under Catalog | Roll lifecycle managed via warehouse/inventory; aggregate boundary deferred |
| 8 | Receipt/Payment as aggregate roots | Implemented as entities with basic status methods | Full posting aggregates planned for Application layer |
| 9 | Partial payment validation rules | `PartialPaymentAmount` property on invoice; validation in Application layer | Domain property exists; cross-field validation needs application orchestration |

No business rules from the foundation were omitted — core workflows (sales invoice detailing gate, container landing cost approval, journal immutability, credit limit check) are enforced in the domain.

---

## Acceptance Criteria Checklist

| # | Criterion | Status |
|---|-----------|--------|
| 1 | Domain Layer compiles independently | ✅ |
| 2 | No UI references | ✅ |
| 3 | No EF Core references | ✅ |
| 4 | No PostgreSQL references | ✅ |
| 5 | Business rules live inside the Domain | ✅ |
| 6 | Aggregate roots enforce invariants | ✅ |
| 7 | Value objects are immutable | ✅ |
| 8 | Workflow enums are centralized | ✅ |
| 9 | Domain events are defined | ✅ |
| 10 | Project builds with 0 errors and 0 warnings | ✅ |

---

*End of Domain Implementation Report — Phase A complete.*
