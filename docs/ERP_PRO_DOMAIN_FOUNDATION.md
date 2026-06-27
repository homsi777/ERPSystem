# ERP PRO — Domain Foundation

**Project:** ERP PRO  
**Technology:** C# / WPF (.NET 9) → PostgreSQL + EF Core (next phase)  
**Business domain:** China fabric import + wholesale fabric distribution  
**Date:** 2026-06-26  
**Status:** Domain design — **no database implementation in this document**

**Companion diagrams:** [`ERP_PRO_DOMAIN_MODEL_DIAGRAM.md`](ERP_PRO_DOMAIN_MODEL_DIAGRAM.md)

---

## 1. Executive Summary

ERP PRO is a **workflow-driven fabric ERP**, not a generic CRUD system. The business spine is:

**China Import → Container → Landing Cost → Warehouse Stock → Sales Invoice → Warehouse Detailing → Invoice Approval → Customer Balance → Accounting Posting**

This document defines the **domain model, aggregate roots, business rules, statuses, workflows, domain events, validation rules, and future PostgreSQL/EF Core boundaries** before any database work begins.

The current WPF UI already reflects this spine (operations centers, detailing workspace, landing cost screens, customer statements). The domain foundation **formalizes** what the UI implies and what PostgreSQL must enforce.

**Alignment note:** Mock UI enums (`FabricInvoiceWorkflowStatus`, `ContainerStatus`, `ContainerLandingCost`) are preserved or extended here — not replaced arbitrarily.

---

## 2. Domain Overview

### 2.1 Domain areas

| # | Area | Arabic | Primary responsibility |
|---|------|--------|------------------------|
| 1 | Identity & Security | الهوية والصلاحيات | Users, roles, permissions, audit actor |
| 2 | Company & Branches | الشركة والفروع | Legal entity, branches, fiscal context |
| 3 | Customers & Suppliers | العملاء والموردون | Party master data, credit, statements |
| 4 | China Import | استيراد الصين | Orders, suppliers, import batches |
| 5 | Containers | الحاويات | Container lifecycle, Excel import, approval |
| 6 | Fabric Catalog | كatalog الأقمشة | Categories, items, colors, codes |
| 7 | Fabric Rolls / Bolts | الأثواب / التوب | Physical roll identity, lengths, weights |
| 8 | Warehouses | المستودعات | Locations, balances, capacity |
| 9 | Inventory Movements | حركات المخزون | Import, sale, return, transfer, adjustment, stocktake |
| 10 | Sales | المبيعات | Invoices, detailing, returns, delivery |
| 11 | Warehouse Detailing | تفصيل المستودع | Per-roll length entry after invoice draft |
| 12 | Purchases | المشتريات | Local/import purchase invoices, returns |
| 13 | Receivables & Payables | الذمم | Customer/supplier balances, aging |
| 14 | Treasury / Cashboxes | الخزينة / الصناديق | Cashboxes, movements, transfers |
| 15 | Accounting | المحاسبة | Chart of accounts, journals, posting |
| 16 | Reports | التقارير | Operational & financial read models |
| 17 | Settings | الإعدادات | System, company, document templates |
| 18 | Audit | التدقيق | Immutable action log |
| 19 | HR (optional) | الموارد البشرية | Employees, attendance, leaves |

### 2.2 Core principles

1. **Sell by meter, not by weight** — weight validates import; meters drive sales and costing.
2. **Detailing gate** — invoice totals and final print require warehouse roll lengths.
3. **Container traceability** — imported fabric sales must reference source container.
4. **Posting immutability** — posted financial and stock documents are reversed, never silently deleted.
5. **Audit everything sensitive** — approvals, postings, cancellations, credit overrides.

### 2.3 Ubiquitous language (key terms)

| Term | Arabic | Meaning |
|------|--------|---------|
| Bolt / Roll | توب / ثوب | One physical fabric roll with its own length |
| Detailing | تفصيل | Warehouse entry of actual length per roll on an invoice |
| Landing Cost | تكلفة الوصول | Import cost allocation per meter for a container |
| Container | حاوية | Shipment unit from China with many fabric lines |
| Statement | كشف حساب | Chronological party ledger (customer/supplier) |
| Posting | ترحيل | Final accounting recognition (immutable) |
| Voucher | سند | Receipt or payment document |

---

## 3. Aggregate Roots

An **aggregate root** is the consistency boundary. External references use root IDs only.

### 3.1 Identity aggregate

**Root:** `User`  
**Contains:** role assignments, branch access, session metadata (future)  
**Rules:** inactive users cannot post; permissions evaluated at command level

### 3.2 Company aggregate

**Root:** `Company`  
**Contains:** `Branch` collection, default currency, fiscal year settings  
**Rules:** all documents scoped to `CompanyId` + `BranchId`

### 3.3 Customer aggregate

**Root:** `Customer`  
**Contains:** contacts, credit limit, payment terms, address, `CustomerStatementSummary` (computed)  
**Rules:** balance updated only from posted sales/receipts/returns; credit limit checked at approval

### 3.4 Supplier aggregate

**Root:** `Supplier`  
**Contains:** contacts, payment terms, `SupplierStatementSummary` (computed)  
**Rules:** payables updated only from posted purchases/payments

### 3.5 China Container aggregate

**Root:** `ChinaContainer`  
**Contains:**
- `ChinaContainerItem` (lines from Excel / manual)
- `ChinaImportBatch` + `ChinaImportRow` (raw import audit)
- `LandingCost` + `LandingCostExpense`
- `ContainerCustomerDistribution` (buyer/reservation split)
- `CustomsCostCalculation` (derived, persisted snapshot)

**Rules:**
- Landing cost uses **total China invoice length** as primary divisor
- Container cannot move to warehouse until approved
- Weight used for validation (`AvgGramPerMeter`), not selling unit

### 3.6 Fabric Catalog aggregate

**Root:** `FabricItem`  
**Contains:** `FabricCategory`, `FabricColor` variants, default unit = meter  
**Rules:** code + color unique per company; inactive items not sold

### 3.7 Warehouse aggregate

**Root:** `Warehouse`  
**Contains:**
- `WarehouseLocation`
- `WarehouseStockBalance` (by fabric + color + container lot + location)
- active `StocktakeSession`, `StockTransfer` (as child workflows)

**Rules:** stock quantity tracked in **meters** and **roll count**; deduction timing governed by sales workflow

### 3.8 Fabric Roll aggregate (child of Warehouse + Container)

**Root:** `FabricRoll` (aka `FabricBolt`)  
**Identity:** roll number within container + fabric code + color  
**Rules:** one roll → one current warehouse location; length updated only via import, detailing consumption, adjustment, stocktake

### 3.9 Sales Invoice aggregate

**Root:** `SalesInvoice`  
**Contains:**
- `SalesInvoiceItem` (fabric line: code, color, roll count, unit price)
- `SalesInvoiceRollDetail` (one row per roll with **required length**)
- optional `DeliveryNote` link
- workflow status history

**Rules:**
- Draft: no stock deduction, no customer balance impact
- Cannot approve until detailing complete and all roll lengths > 0
- Total = Σ(length × unit price) after detailing
- Must reference `CustomerId`, `WarehouseId`, `ChinaContainerId`

### 3.10 Warehouse Detailing aggregate (process bound to SalesInvoice)

**Root:** `WarehouseDetailingSession` (1:1 with invoice in AwaitingDetailing+)  
**Contains:** roll length entries, validator state, officer assignment  
**Rules:** completing detailing transitions invoice to `Detailed` / `ReadyForApproval`

### 3.11 Purchase Invoice aggregate

**Root:** `PurchaseInvoice`  
**Contains:** `PurchaseInvoiceItem`, optional `PurchaseReturn`  
**Rules:** posts to payables and inventory (local purchases) on approval

### 3.12 Treasury aggregates

**Receipt voucher root:** `ReceiptVoucher` — links customer, optional invoice allocations  
**Payment voucher root:** `PaymentVoucher` — links supplier, optional purchase allocations  
**Cashbox root:** `Cashbox` — contains `CashboxMovement`, `CashboxTransfer`

### 3.13 Accounting aggregate

**Root:** `JournalEntry`  
**Contains:** `JournalEntryLine` (debit/credit), `PostingBatch` reference  
**Rules:** Σ debit = Σ credit; posted entries immutable; reversal creates new entry

### 3.14 Audit aggregate

**Root:** `AuditLog` (append-only)  
**Rules:** no update/delete; retention policy configurable

### 3.15 HR aggregate (optional phase)

**Root:** `Employee` — contains department, attendance, leave requests

---

## 4. Entity Catalog

For each entity: **Arabic meaning**, purpose, fields, relations, lifecycle, rules, indexes, EF notes.

Legend: **R** = required, **O** = optional, **PK/FK/UQ/IX** = future DB constraints

---

### 4.1 Identity & Security

#### Company — الشركة

| | |
|---|---|
| **Purpose** | Legal operating entity owning all ERP data |
| **Aggregate** | Root of Company aggregate |

| Field | R/O | Type | Notes |
|-------|-----|------|-------|
| Id | R | uuid | PK |
| Code | R | varchar(20) | UQ |
| NameAr, NameEn | R | varchar | |
| TaxNumber | O | varchar | |
| DefaultCurrency | R | char(3) | e.g. SAR |
| IsActive | R | bool | soft deactivate |
| CreatedAt, UpdatedAt | R | timestamptz | |

**Relations:** 1→N `Branch`, N→N `User` (via branch access)  
**Indexes:** `IX_company_code (Code)` UQ  
**EF:** root entity; soft delete via `IsActive`

#### Branch — الفرع

| Field | R/O | Notes |
|-------|-----|-------|
| Id, CompanyId | R | FK |
| Code, NameAr, NameEn | R | UQ (CompanyId, Code) |
| City, Address | O | |
| IsActive | R | |

**Rules:** every operational document stores `BranchId`

#### User — المستخدم

| Field | R/O | Notes |
|-------|-----|-------|
| Id, Username | R | UQ |
| PasswordHash | R | never plain text |
| FullNameAr, FullNameEn | R | |
| Email, Phone | O | |
| IsActive | R | |
| LastLoginAt | O | |

**Relations:** N→N `Role`, N→N `Branch`  
**Indexes:** `UQ_user_username`, `IX_user_active (IsActive)`

#### Role — الدور

| Field | R/O | Notes |
|-------|-----|-------|
| Id, Name | R | e.g. Accountant, WarehouseOfficer |
| Description | O | |
| IsSystem | R | prevent delete of built-in roles |

#### Permission — الصلاحية

| Field | R/O | Notes |
|-------|-----|-------|
| Id, Code | R | e.g. `sales.invoice.approve` |
| Module, Action | R | |
| Description | O | |

**Relations:** N→N `Role` via `RolePermission`

---

### 4.2 Parties

#### Customer — العميل

| **Arabic** | عميل الجملة — مشتري الأقمshة |
| **Purpose** | Wholesale buyer; credit and statement tracking |

| Field | R/O | Notes |
|-------|-----|-------|
| Id, Code | R | UQ |
| NameAr, NameEn | R | |
| Type | R | Cash / Credit |
| CreditLimit | O | required if Credit |
| PaymentTermsDays | O | |
| Phone, Email, Address, City | O | |
| SalesRepId | O | FK User |
| Balance | R | computed / denormalized snapshot |
| Status | R | Active, Suspended, Blocked |
| IsActive | R | soft delete |

**Rules:** `Balance` increases on posted approved credit invoices; decreases on posted receipts  
**Indexes:** `IX_customer_code`, `IX_customer_status`, `IX_customer_balance` (collections)  
**EF:** aggregate root; `Balance` updated via domain service on posting events

#### Supplier — المورد

Same pattern as Customer without credit sales; used for local and China suppliers.

#### ChinaSupplier — مورد الصين

| **Purpose** | Import-specific supplier metadata |
| Fields | SupplierId (FK), Port, DefaultIncoterm, LeadTimeDays, BankDetails |

---

### 4.3 Fabric Catalog

#### FabricCategory — تصنيف القماش

| Field | Notes |
|-------|-------|
| Id, Code, NameAr, NameEn, ParentId | hierarchical optional |
| IsActive | |

#### FabricItem — صنف القمash

| **Arabic** | صنف القماش (كود + نوع) |
| **Purpose** | Sellable fabric master |

| Field | R/O | Notes |
|-------|-----|-------|
| Id, Code | R | UQ — maps to Excel fabric code |
| CategoryId | R | FK |
| NameAr, NameEn | R | e.g. كتان F12 |
| DefaultUnit | R | always `meter` |
| StandardWidthCm | O | |
| IsActive | R | |

#### FabricColor — لون الصنف

| Field | Notes |
|-------|-------|
| Id, FabricItemId, ColorCode, NameAr, NameEn | UQ (FabricItemId, ColorCode) |
| IsActive | |

---

### 4.4 China Import & Containers

#### ChinaOrder — طلب استيراد

| Field | Notes |
|-------|-------|
| Id, OrderNumber, ChinaSupplierId, OrderDate, ExpectedShipDate | |
| Status | Draft, Confirmed, Shipped, Closed, Cancelled |

#### ChinaContainer — حاوية

| **Arabic** | حاوية الشحن — وحدة الاستيراد الأساسية |
| **Aggregate root** | Yes |

| Field | R/O | Notes |
|-------|-----|-------|
| Id, ContainerNumber | R | UQ |
| ChinaOrderId | O | FK |
| SupplierId | R | FK |
| BranchId | R | FK |
| ShipmentDate, ExpectedArrival, ArrivalDate | O | |
| Port | O | |
| TotalRolls, TotalMeters, TotalWeightKg | R | from approved import |
| Status | R | see §7 |
| ImportCost | O | commercial invoice value |
| Notes | O | |
| ApprovedAt, ApprovedByUserId | O | |
| IsArchived | R | default false |

**Relations:** 1→N `ChinaContainerItem`, 1→1 `LandingCost`, 1→N `ContainerCustomerDistribution`  
**Indexes:** `UQ_container_number`, `IX_container_status`, `IX_container_arrival`

#### ChinaContainerItem — بند الحاوية

| Field | Notes |
|-------|-------|
| Id, ContainerId, LineNumber | |
| FabricItemId, FabricColorId | FK (resolved from code/color) |
| RollCount, LengthMeters, WeightKg | per line or per roll expansion |
| BuyerCustomerId | O — reservation/distribution |
| RowStatus | Valid, Error, Corrected |

#### ChinaImportBatch — دفعة استيراد Excel

| Field | Notes |
|-------|-------|
| Id, BatchNumber, ContainerId, FileName, ImportedAt, ImportedByUserId | |
| ValidRowCount, ErrorRowCount | |

#### ChinaImportRow — سطر خام من Excel

| Field | Notes |
|-------|-------|
| Id, BatchId, RowNumber, RawJson or typed columns | audit trail |
| ValidationErrors | |
| IsAccepted | |

#### ContainerCustomerDistribution — توزيع على العملاء

| Field | Notes |
|-------|-------|
| Id, ContainerId, CustomerId, FabricItemId, FabricColorId | |
| RollCount, Meters | reserved/sold allocation |

---

### 4.5 Landing Cost

#### LandingCost — تكلفة الوصول (aggregate part of Container)

| Field | R/O | Notes |
|-------|-----|-------|
| Id, ContainerId | R | 1:1 |
| TotalLengthFromInvoice | R | **primary cost base** (meters) |
| ContainerWeightKg | R | validation only |
| CustomsAmountPaid | R | |
| CustomsCostPerMeter | R | computed persisted |
| AvgGramPerMeter | R | `WeightGrams / TotalLength` |
| Shipping, Clearance, OtherExpenses | R | |
| TotalImportExpenses | R | |
| ExpenseCostPerMeter | R | |
| CalculatedAt, CalculatedByUserId | R | snapshot |
| Status | R | Draft, Reviewed, Approved |

**Business rules (formulas):**
```
CustomsCostPerMeter = CustomsAmountPaid / TotalLengthFromInvoice
AvgGramPerMeter = (ContainerWeightKg × 1000) / TotalLengthFromInvoice
ExpenseCostPerMeter = TotalImportExpenses / TotalLengthFromInvoice
```
Aligns with `ContainerLandingCost` in `Core/Domain/FabricDomainModels.cs`.

#### LandingCostExpense — بند مصروف

| Field | Notes |
|-------|-------|
| Id, LandingCostId, ExpenseType, Amount, Notes | Shipping, Clearance, Other |

#### CustomsCostCalculation — snapshot

Persisted denormalized calculation for audit reproducibility.

---

### 4.6 Inventory & Warehouses

#### Warehouse — المستودع

| Field | Notes |
|-------|-------|
| Id, Code, NameAr, BranchId, City, IsActive | |
| CapacityRolls | O |

#### WarehouseLocation — موقع التخزين

| Field | Notes |
|-------|-------|
| Id, WarehouseId, Zone, Aisle, BinCode | UQ (WarehouseId, BinCode) |

#### FabricRoll / FabricBolt — التوب / الثوب

| **Arabic** | لفة القمash الفعلية |
| Field | Notes |
| Id, ContainerId, ContainerItemId, RollNumber | identity |
| FabricItemId, FabricColorId | |
| LengthMeters, WeightKg | from import; length may adjust on stocktake |
| WarehouseId, LocationId | current |
| Status | Available, Reserved, Sold, Wasted, InTransit |

#### WarehouseStockBalance — رصيد المخزون

| Field | Notes |
|-------|-------|
| Id, WarehouseId, FabricItemId, FabricColorId, ContainerId | balance key |
| RollCount, TotalMeters | |
| ReservedMeters, AvailableMeters | |

**Indexes:** `IX_stock_wh_fabric_container` unique composite

#### StockMovement — حركة مخزون

| Field | Notes |
|-------|-------|
| Id, MovementNumber, MovementDate, Type | Import, Sale, Return, Transfer, Adjustment, Waste, Stocktake |
| WarehouseId, ReferenceType, ReferenceId | polymorphic link |
| Status | see §7 |
| PostedAt | |

#### StockTransfer — مناقلة

Header + lines referencing rolls/meters between warehouses.

#### StocktakeSession — جرد

Header + counted lines → generates adjustment movements on post.

---

### 4.7 Sales

#### SalesInvoice — فاتورة البيع

| **Aggregate root** | Yes |
| **Arabic** | فاتورة بيع الأقمshة بالجملة |

| Field | R/O | Notes |
|-------|-----|-------|
| Id, InvoiceNumber | R | UQ per branch/fiscal year |
| BranchId, CompanyId | R | |
| CustomerId | R | FK |
| WarehouseId | R | FK — **required before save** |
| ChinaContainerId | R | FK — **required for imported fabric** |
| InvoiceDate | R | |
| PaymentType | R | Cash, Credit |
| PartialPaymentAmount | O | credit invoices |
| Status | R | see §7 |
| SubTotal, DiscountTotal, TaxTotal, GrandTotal | R | GrandTotal final **after detailing** |
| CreatedByUserId, ApprovedByUserId | R/O | |
| SentToWarehouseAt, DetailedAt, ApprovedAt, PrintedAt, DeliveredAt | O | workflow timestamps |
| CancelledAt, CancelReason, ReversedByJournalEntryId | O | no hard delete |
| IsArchived | R | |

**Relations:** 1→N `SalesInvoiceItem`, 1→N `SalesInvoiceRollDetail`, 0→1 `DeliveryNote`, 0→1 `JournalEntry`

#### SalesInvoiceItem — بند الفاتورة

| Field | Notes |
|-------|-------|
| Id, SalesInvoiceId, LineNumber | |
| FabricItemId, FabricColorId | |
| RollCount | accountant enters count, **not lengths** |
| UnitPrice | per meter |
| Unit | `meter` |
| LineTotal | computed after detailing |

#### SalesInvoiceRollDetail — تفصيل الأثواب

| **Arabic** | طول كل ثوب — يُدخله أمين المستودع |
| Field | Notes |
| Id, SalesInvoiceItemId, RollSequence | 1..RollCount |
| FabricRollId | O — link if specific roll picked |
| LengthMeters | **R after detailing, must be > 0** |
| Unit | meter |
| EnteredByUserId, EnteredAt | |

**Rule:** one detail row per roll; empty/zero length invalid.

#### SalesReturn — مرتجع بيع

Links to original invoice; reverses stock and balance on post.

#### DeliveryNote — إذن تسليم

| Field | Notes |
|-------|-------|
| Id, DeliveryNumber, SalesInvoiceId, DeliveredAt, ReceivedByName | |

---

### 4.8 Purchases

#### PurchaseInvoice — فاتورة شراء

Local or import-related purchases; items in meters or rolls per agreement.

#### PurchaseInvoiceItem — بند الشراء

#### PurchaseReturn — مرتجع شراء

---

### 4.9 Finance (Treasury)

#### ReceiptVoucher — سند قبض

| Field | Notes |
|-------|-------|
| Id, VoucherNumber, CustomerId, Amount, CashboxId | |
| Allocations[] → SalesInvoiceId + Amount | optional |
| Status | Draft, Approved, Posted, Cancelled, Reversed |

#### PaymentVoucher — سند دفع

Same pattern for `SupplierId`.

#### Cashbox — الصندوق

| Field | Notes |
|-------|-------|
| Id, Code, Name, BranchId, Currency, CurrentBalance | |

#### CashboxMovement — حركة صندوق

Posted ledger of cash in/out per voucher.

#### CashboxTransfer — تحويل بين الصناديق

---

### 4.10 Accounting

#### Account — الحساب

Chart of accounts: Asset, Liability, Equity, Revenue, Expense; hierarchical.

#### JournalEntry — قيد يومية

| Field | Notes |
|-------|-------|
| Id, EntryNumber, EntryDate, Description | |
| Status | Draft, Approved, Posted, Reversed, Cancelled |
| SourceType, SourceId | SalesInvoice, ReceiptVoucher, etc. |
| PostedAt, PostedByUserId | |
| ReversalOfEntryId | O |

#### JournalEntryLine — سطر القيد

| Field | Notes |
|-------|-------|
| Id, JournalEntryId, AccountId, Debit, Credit, Narrative | |
| PartyId | O — customer/supplier sub-ledger |

#### PostingBatch — دفعة ترحيل

Groups periodic posting runs (optional).

#### CustomerStatementEntry / SupplierStatementEntry

| Field | Notes |
|-------|-------|
| Id, PartyId, EntryDate, DocumentType, DocumentId | |
| Debit, Credit, RunningBalance | materialized or view |

---

### 4.11 Documents, Settings, Audit, HR

#### DocumentTemplate — قالب مستند

Print/PDF layouts per document type (future Document Engine).

#### SystemSetting — إعداد النظام

Key/value per company/branch.

#### AuditLog — سجل التدقيق

| Field | Notes |
|-------|-------|
| Id, OccurredAt, UserId, Action, EntityType, EntityId | |
| OldValuesJson, NewValuesJson, IpAddress, BranchId | append-only |

#### Employee, Department, AttendanceRecord, LeaveRequest, Shift

Standard HR — optional module; isolated schema `hr`.

---

## 5. Business Rules

### 5.1 Fabric & selling rules

| # | Rule |
|---|------|
| BR-01 | Fabric selling unit is **meter**, not kg |
| BR-02 | Sales of imported fabric must reference a **ChinaContainer** |
| BR-03 | Sales must specify **Warehouse** before leaving draft |
| BR-04 | Accountant creates invoice with fabric + **roll count** only |
| BR-05 | Warehouse officer enters **one length per roll** from physical labels |
| BR-06 | Invoice **cannot finalize** before warehouse detailing completes |
| BR-07 | Invoice **cannot print as final** before detailing |
| BR-08 | **Grand total** = Σ(length × unit price) after detailing |
| BR-09 | Every sold roll has exactly one valid length > 0 |
| BR-10 | Zero or empty roll length is invalid |

### 5.2 Inventory timing rules

| # | Rule |
|---|------|
| BR-11 | **No inventory deduction** at draft creation |
| BR-12 | Deduction at **approval** (or configurable: reserve at send-to-warehouse, deduct at approve) |
| BR-13 | Returns add stock back only on posted return document |
| BR-14 | Transfers move stock between warehouses on post |
| BR-15 | Stocktake variances post adjustment movements |

**Recommended default:**  
- `SendToWarehouse` → **reserve** meters/rolls  
- `Approve` → **deduct** reserved stock (release reservation)

### 5.3 Financial rules

| # | Rule |
|---|------|
| BR-16 | Customer balance updates **only after invoice approval/posting** |
| BR-17 | Receipts reduce balance on **post**, not draft |
| BR-18 | Posted journal entries **cannot be deleted** |
| BR-19 | Cancellation creates **reversal entry** + audit trail |
| BR-20 | Journal must balance: Σ Debit = Σ Credit |

### 5.4 Landing cost rules

| # | Rule |
|---|------|
| BR-21 | Landing cost primary base = **total length from China invoice** |
| BR-22 | Customs per meter = customs paid ÷ total length |
| BR-23 | Container weight validates plausibility; **not** selling basis |
| BR-24 | Avg gram/meter = container weight (g) ÷ total length |
| BR-25 | Landing cost snapshot frozen on container approval |

### 5.5 Security & audit

| # | Rule |
|---|------|
| BR-26 | Every sensitive action produces **AuditLog** |
| BR-27 | Role separation: warehouse officer cannot approve own detailing invoice (configurable) |
| BR-28 | Credit limit check at approval; override requires permission + audit |

---

## 6. Workflow Definitions

### 6.1 China container import workflow

| Step | Actor | Action | Status after |
|------|-------|--------|--------------|
| 1 | Import officer | Create container draft | Draft |
| 2 | Import officer | Record shipment / in transit | InTransit |
| 3 | Warehouse / import | Mark arrived | Arrived |
| 4 | Import officer | Upload & parse Excel | UnderReview |
| 5 | Import officer | Validate fabric codes/colors, fix errors | UnderReview |
| 6 | Accountant | Calculate landing cost | LandingCostReviewed |
| 7 | Manager | Approve container | Approved |
| 8 | Warehouse | Transfer rolls to stock (stock movement) | InWarehouse |
| 9 | System | Rolls available for sales | InWarehouse |
| 10 | Admin | Close / archive when depleted | Closed → Archived |

**Outputs:** `ChinaContainerItem` rows, `LandingCost` snapshot, `StockMovement` (Import), `FabricRoll` records

### 6.2 Sales invoice workflow

| Step | Actor | Action | Status after |
|------|-------|--------|--------------|
| 1 | Accountant | Create invoice: customer, warehouse, container, payment type | Draft |
| 2 | Accountant | Add fabric lines (item, color, roll count, unit price) | Draft |
| 3 | Accountant | Save draft | Draft |
| 4 | Accountant | Send to warehouse detailing | AwaitingDetailing |
| 5 | Warehouse officer | Enter length per roll | InProgress detailing |
| 6 | Warehouse officer | Complete detailing (all lengths valid) | Detailed |
| 7 | System | Recalculate line totals and grand total | ReadyForApproval |
| 8 | Accountant | Review totals, notify if needed | ReadyForApproval |
| 9 | Accountant / manager | Approve invoice | Approved |
| 10 | Accountant | Print final invoice | Printed |
| 11 | Warehouse | Deliver goods | Delivered |
| 12 | Accountant | Post to accounting (may be auto on approve) | Approved + Posted JE |

**UI alignment:** matches `NewSalesInvoiceControl` + `WarehouseDetailingWorkspaceControl` + `FabricInvoiceWorkflowStatus`

### 6.3 Warehouse detailing workflow

| Step | Action |
|------|--------|
| 1 | Invoice appears in detailing queue (status = AwaitingDetailing) |
| 2 | Officer opens detailing session |
| 3 | For each roll 1..N: enter length from label |
| 4 | Validate: all lengths > 0, unit = meter |
| 5 | Complete → invoice → Detailed, notify accountant |
| 6 | Reject → return to accountant with reason (optional path) |

### 6.4 Customer payment workflow

| Step | Action |
|------|--------|
| 1 | Create receipt voucher (draft) |
| 2 | Select customer, cashbox, amount |
| 3 | Optional: allocate to open invoices |
| 4 | Approve |
| 5 | Post → update cashbox + customer balance + statement entry |
| 6 | Optional: generate journal entry |

### 6.5 Supplier payment workflow

Same as 6.4 for `PaymentVoucher` → supplier payables.

### 6.6 Inventory movement workflow

| Type | Trigger | Post effect |
|------|---------|-------------|
| Import | Container approved → warehouse | +stock |
| Sale | Sales invoice approved | −stock |
| Return | Sales return posted | +stock |
| Transfer | Transfer approved | −source, +destination |
| Adjustment | Manual approval | ±stock |
| Waste | Approved waste doc | −stock |
| Stocktake | Session posted | variance adjustment |

### 6.7 Accounting posting workflow

| Step | Action |
|------|--------|
| 1 | Create draft journal (manual or system-generated) |
| 2 | Validate balanced lines |
| 3 | Approve (optional separation of duties) |
| 4 | Post → lock entry, update account balances |
| 5 | Reversal if needed → new reversing entry, link `ReversalOfEntryId` |

---

## 7. Status Definitions

### 7.1 SalesInvoiceStatus

| Status | Arabic | Meaning |
|--------|--------|---------|
| Draft | مسودة | Editable; no stock/financial impact |
| AwaitingDetailing | بانتظار التفصيل | Sent to warehouse; lengths pending |
| Detailed | مفصلة | All roll lengths entered |
| ReadyForApproval | جاهزة للاعتماد | Totals calculated; accountant review |
| Approved | معتمدة | Approved; stock/balance rules applied |
| Printed | مطبوعة | Final document printed |
| Delivered | مسلمة | Goods delivered |
| Cancelled | ملغاة | Cancelled with reason; reversal if needed |

**Maps from UI:** `FabricInvoiceWorkflowStatus` (+ `ReadyForApproval` added for clarity)

### 7.2 ChinaContainerStatus

| Status | Arabic |
|--------|--------|
| Draft | مسودة |
| InTransit | بالطريق |
| Arrived | واصلة |
| UnderReview | قيد المراجعة |
| LandingCostReviewed | مراجعة Landing Cost |
| Approved | معتمدة |
| InWarehouse | في المخزون |
| Closed | مغلقة |
| Archived | مؤرشفة |
| Cancelled | ملغاة |

**Maps from UI:** extends `ContainerStatus` with Draft, UnderReview, LandingCostReviewed, InWarehouse

### 7.3 WarehouseDetailingStatus

| Status | Arabic |
|--------|--------|
| Pending | معلق |
| InProgress | قيد التفصيل |
| Completed | مكتمل |
| Rejected | مرفوض |

### 7.4 VoucherStatus (Receipt / Payment)

| Status | Arabic |
|--------|--------|
| Draft | مسودة |
| Approved | معتمد |
| Posted | مرحّل |
| Cancelled | ملغي |
| Reversed | معكوس |

### 7.5 JournalEntryStatus

| Status | Arabic |
|--------|--------|
| Draft | مسودة |
| Approved | معتمد |
| Posted | مرحّل |
| Reversed | معكوس |
| Cancelled | ملغي |

**Maps from UI:** extends `JournalStatus` with Approved, Reversed

### 7.6 StockMovementStatus

| Status | Arabic |
|--------|--------|
| Draft | مسودة |
| Posted | مرحّل |
| Cancelled | ملغي |
| Reversed | معكوس |

---

## 8. Domain Events

Events for future messaging, notifications, and audit enrichment.

| Event | Trigger | Entity | Notification | Audit |
|-------|---------|--------|--------------|-------|
| ContainerCreated | Save new container | ChinaContainer | — | Yes |
| ContainerExcelImported | Batch accepted | ChinaImportBatch | Import officer | Yes |
| ContainerLandingCostCalculated | LC saved | LandingCost | Accountant | Yes |
| ContainerApproved | Approve action | ChinaContainer | Warehouse | Yes |
| ContainerMovedToWarehouse | Stock movement posted | StockMovement | Warehouse | Yes |
| SalesInvoiceCreated | Save draft | SalesInvoice | — | Yes |
| SalesInvoiceSentToWarehouse | Send to detailing | SalesInvoice | **Warehouse officer** | Yes |
| SalesInvoiceDetailed | Detailing completed | SalesInvoice | **Accountant** | Yes |
| SalesInvoiceApproved | Approve | SalesInvoice | Customer rep | Yes |
| SalesInvoicePrinted | Print final | SalesInvoice | — | Yes |
| SalesInvoiceCancelled | Cancel | SalesInvoice | Accountant | Yes |
| InventoryReserved | Send to warehouse | WarehouseStockBalance | — | Yes |
| InventoryDeducted | Approve invoice | StockMovement | — | Yes |
| ReceiptVoucherPosted | Post receipt | ReceiptVoucher | Accountant | Yes |
| PaymentVoucherPosted | Post payment | PaymentVoucher | Accountant | Yes |
| JournalEntryPosted | Post journal | JournalEntry | — | Yes |
| CustomerCreditLimitExceeded | Approve blocked | Customer | **Manager** | Yes |
| WarehouseStockLow | Below threshold | WarehouseStockBalance | Warehouse | Yes |
| AuditActionRecorded | Any sensitive action | AuditLog | — | Self |

---

## 9. Validation Rules

### 9.1 Customer

- Code unique per company
- Credit limit ≥ 0 if credit customer
- Cannot approve credit invoice if balance + invoice > limit (unless override)
- Blocked customers cannot receive new invoices

### 9.2 Supplier

- Code unique; required for payment vouchers

### 9.3 FabricItem

- Code unique; default unit must be meter
- Must have at least one active color to sell

### 9.4 ChinaContainer

- ContainerNumber unique
- Cannot approve with unresolved import row errors
- TotalMeters > 0 before landing cost
- TotalWeightKg > 0 for gram/meter validation

### 9.5 LandingCost

- TotalLengthFromInvoice > 0
- All expense amounts ≥ 0
- Recalculation required if container items change before approval

### 9.6 WarehouseStock

- AvailableMeters ≥ 0
- Cannot deduct more than available (respecting reservations)

### 9.7 SalesInvoice

- CustomerId, WarehouseId, ChinaContainerId required (non-draft promotion)
- At least one line item
- RollCount ≥ 1 per line
- UnitPrice > 0
- Cannot approve unless status ≥ Detailed and all roll details valid
- GrandTotal > 0 after detailing

### 9.8 WarehouseDetailing

- Count of roll details = Σ RollCount on items
- Each LengthMeters > 0
- Officer role required

### 9.9 ReceiptVoucher / PaymentVoucher

- Amount > 0
- Cashbox active
- Posted requires approval (if approval enabled)

### 9.10 JournalEntry

- At least 2 lines
- Σ Debit = Σ Credit (± 0.01 tolerance configurable)
- Posted entries immutable
- Accounts must be postable (leaf accounts)

---

## 10. Future PostgreSQL Schema Plan

**No database created in this phase.** Planned schema groups:

### 10.1 `identity`

| Table | Notes |
|-------|-------|
| users | |
| roles | |
| permissions | |
| role_permissions | M:N |
| user_roles | M:N |
| user_branches | M:N |

**UQ:** users.username  
**IX:** users.is_active

### 10.2 `company`

| Table | Notes |
|-------|-------|
| companies | |
| branches | FK companies |

**UQ:** (company_id, code) on branches

### 10.3 `parties`

| Table | Notes |
|-------|-------|
| customers | |
| suppliers | |
| china_suppliers | FK suppliers |
| customer_contacts | |
| supplier_contacts | |

**IX:** customers.balance, customers.status

### 10.4 `china_import`

| Table | Notes |
|-------|-------|
| china_orders | |
| china_containers | |
| china_container_items | |
| china_import_batches | |
| china_import_rows | |
| container_customer_distributions | |
| landing_costs | 1:1 container |
| landing_cost_expenses | |

**UQ:** china_containers.container_number  
**IX:** china_containers.status, china_containers.arrival_date

### 10.5 `inventory`

| Table | Notes |
|-------|-------|
| fabric_categories | |
| fabric_items | |
| fabric_colors | |
| warehouses | |
| warehouse_locations | |
| fabric_rolls | |
| warehouse_stock_balances | |
| stock_movements | |
| stock_movement_lines | |
| stock_transfers | |
| stock_transfer_lines | |
| stocktake_sessions | |
| stocktake_lines | |

**UQ:** fabric_items.code; (fabric_item_id, color_code)  
**IX:** stock_balances (warehouse_id, fabric_item_id, fabric_color_id, container_id)

### 10.6 `sales`

| Table | Notes |
|-------|-------|
| sales_invoices | |
| sales_invoice_items | |
| sales_invoice_roll_details | |
| warehouse_detailing_sessions | |
| sales_returns | |
| sales_return_lines | |
| delivery_notes | |

**UQ:** sales_invoices.invoice_number per branch/year  
**IX:** sales_invoices.status, customer_id, container_id

### 10.7 `purchasing`

| Table | Notes |
|-------|-------|
| purchase_invoices | |
| purchase_invoice_items | |
| purchase_returns | |

### 10.8 `finance`

| Table | Notes |
|-------|-------|
| cashboxes | |
| receipt_vouchers | |
| receipt_voucher_allocations | |
| payment_vouchers | |
| payment_voucher_allocations | |
| cashbox_movements | |
| cashbox_transfers | |

### 10.9 `accounting`

| Table | Notes |
|-------|-------|
| accounts | |
| journal_entries | |
| journal_entry_lines | |
| posting_batches | |
| customer_statement_entries | optional materialized |
| supplier_statement_entries | |

**UQ:** accounts.code; journal_entries.entry_number  
**Check:** balanced entries via trigger or app layer

### 10.10 `documents`

| document_templates | |

### 10.11 `settings`

| system_settings | key per company/branch |

### 10.12 `audit`

| audit_logs | append-only, partitioned by month recommended |

### 10.13 `hr` (optional)

| employees, departments, attendance_records, leave_requests, shifts |

### 10.14 Cross-schema relations (critical)

```
china_import.china_containers  → inventory.fabric_rolls
inventory.warehouse_stock_balances → sales (availability check)
sales.sales_invoices → parties.customers
sales.sales_invoices → china_import.china_containers
sales.sales_invoices → accounting.journal_entries (source)
finance.receipt_vouchers → parties.customers
```

---

## 11. Future EF Core Mapping Notes

### 11.1 DbSet candidates (one DbContext initially: `ErpDbContext`)

```csharp
// Identity & company
DbSet<User>, DbSet<Role>, DbSet<Permission>, DbSet<Company>, DbSet<Branch>

// Parties
DbSet<Customer>, DbSet<Supplier>, DbSet<ChinaSupplier>

// China import
DbSet<ChinaContainer>, DbSet<ChinaContainerItem>, DbSet<ChinaImportBatch>,
DbSet<ChinaImportRow>, DbSet<LandingCost>, DbSet<LandingCostExpense>,
DbSet<ContainerCustomerDistribution>

// Catalog & inventory
DbSet<FabricCategory>, DbSet<FabricItem>, DbSet<FabricColor>,
DbSet<Warehouse>, DbSet<WarehouseLocation>, DbSet<FabricRoll>,
DbSet<WarehouseStockBalance>, DbSet<StockMovement>, DbSet<StockTransfer>,
DbSet<StocktakeSession>

// Sales & purchases
DbSet<SalesInvoice>, DbSet<SalesInvoiceItem>, DbSet<SalesInvoiceRollDetail>,
DbSet<WarehouseDetailingSession>, DbSet<SalesReturn>, DbSet<DeliveryNote>,
DbSet<PurchaseInvoice>, DbSet<PurchaseInvoiceItem>, DbSet<PurchaseReturn>

// Finance & accounting
DbSet<Cashbox>, DbSet<ReceiptVoucher>, DbSet<PaymentVoucher>,
DbSet<CashboxMovement>, DbSet<Account>, DbSet<JournalEntry>,
DbSet<JournalEntryLine>

// System
DbSet<AuditLog>, DbSet<DocumentTemplate>, DbSet<SystemSetting>
```

### 11.2 Owned types / value objects

| Value object | Owner entity | EF mapping |
|--------------|--------------|------------|
| Money | lines, vouchers | owned: Amount + Currency |
| Address | Customer, Supplier | owned type |
| Dimensions | FabricItem | optional owned |
| LandingCostSnapshot | LandingCost | persisted computed columns |
| WorkflowTimestamps | SalesInvoice | owned or explicit columns |

### 11.3 Cascade restrictions

| Relationship | Delete behavior |
|--------------|-----------------|
| SalesInvoice → Items → RollDetails | Cascade soft-delete only in draft |
| Container → Items | Restrict if InWarehouse |
| JournalEntry → Lines | Restrict on posted |
| Customer → Invoices | Restrict |
| FabricRoll → RollDetails | Restrict if sold |

**Never cascade hard-delete business documents.**

### 11.4 Soft delete pattern

Use on all master data and documents:

| Column | Use |
|--------|-----|
| IsActive | master data |
| IsArchived | containers, old invoices |
| CancelledAt, CancelReason | documents |
| ReversedByEntryId | financial docs |
| DeletedAt | optional EF soft-delete filter (prefer IsActive) |

### 11.5 Immutability after posting

| Entity | After post |
|--------|------------|
| JournalEntry + Lines | immutable |
| StockMovement (posted) | immutable |
| ReceiptVoucher / PaymentVoucher | immutable |
| SalesInvoice (Approved+) | limited edit; cancel via reversal workflow |
| LandingCost (approved) | immutable snapshot |

### 11.6 Global query filters (EF)

- `IsActive == true` on masters
- `IsArchived == false` on default lists
- Branch scoping: `BranchId == currentBranch` (optional multi-tenant filter)

---

## 12. Audit and Security Rules

| Area | Rule |
|------|------|
| Authentication | Username/password → future JWT or session |
| Authorization | Permission codes on commands (approve, post, override credit) |
| Branch scope | User sees only assigned branches unless HQ role |
| Audit | Create/Update/Delete/Approve/Post/Cancel/Print on all business entities |
| PII | Customer phone/email — restrict export permissions |
| Posting | Separate `accounting.post` from `accounting.draft` |
| Detailing | `warehouse.detail` vs `sales.approve` separation |

**AuditLog minimum fields:** Who, When, What, Entity, Before/After JSON, Branch, CorrelationId

---

## 13. Risks and Open Questions

| # | Question | Impact | Recommendation |
|---|----------|--------|----------------|
| OQ-01 | Reserve stock at SendToWarehouse or only deduct at Approve? | Inventory accuracy | Default: reserve on send, deduct on approve |
| OQ-02 | Partial detailing rejection — rollback entire invoice or per line? | Workflow | Per invoice reject → AwaitingDetailing |
| OQ-03 | Multi-container invoice allowed? | Data model | **No** for v1 — one container per invoice |
| OQ-04 | Unit price: per meter fixed at draft or recalc if length changes? | Pricing | Fixed unit price; line total = length × price |
| OQ-05 | Tax (VAT 15%) on fabric sales? | Accounting | Yes — separate tax line; confirm with accountant |
| OQ-06 | Credit partial payment at invoice creation? | UI exists | Store `PartialPaymentAmount`; post on receipt |
| OQ-07 | Link specific FabricRoll IDs at detailing or only lengths? | Traceability | v1: lengths required; roll ID optional enhancement |
| OQ-08 | Auto-post journal on invoice approve? | Accounting | Configurable setting |
| OQ-09 | Fiscal year invoice numbering | Schema | `(BranchId, FiscalYear, Sequence)` |
| OQ-10 | HR module in same DB or deferred? | Scope | Same DB, schema `hr`, feature flag |

---

## 14. Recommended Next Phase

After **domain approval** of this document:

### Phase A — Domain layer in code (no DB yet)

1. Create `ERPSystem.Domain` project with entities, enums, value objects matching §4–§7
2. Replace mock enums in Views with domain enums (`SalesInvoiceStatus`, etc.)
3. Implement domain services: `LandingCostCalculator`, `SalesInvoiceTotalCalculator`, `DetailingValidator`

### Phase B — PostgreSQL

1. Create schemas per §10
2. Apply migrations with soft-delete columns and immutability triggers
3. Seed chart of accounts, default roles, one company/branch

### Phase C — EF Core

1. `ErpDbContext` with configurations per §11
2. Repository pattern or direct DbContext in application services
3. Unit of work per command (approve invoice = transaction)

### Phase D — Wire UI to real commands

1. Replace `*SampleData` with repositories one module at a time
2. Keep `MockInteractionService` feedback patterns; swap internals for real results
3. First vertical slice: **Sales invoice draft → detailing → approve**

---

## Acceptance Criteria Checklist

| # | Criterion | Status |
|---|-----------|--------|
| 1 | Domain documented before database | ✅ |
| 2 | Fabric-specific rules captured | ✅ §5 |
| 3 | Sales detailing workflow documented | ✅ §6.2–6.3 |
| 4 | China import & landing cost documented | ✅ §6.1, §4.5 |
| 5 | PostgreSQL schema groups proposed | ✅ §10 |
| 6 | EF Core notes prepared | ✅ §11 |
| 7 | No database code added | ✅ |
| 8 | No migrations added | ✅ |
| 9 | Project builds 0/0 | ✅ (documentation only) |
| 10 | Ready for PostgreSQL after domain approval | ✅ |

---

*This document is the authoritative domain reference for ERP PRO. Changes require business review before database implementation.*
