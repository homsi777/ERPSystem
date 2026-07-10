# TASK — ERP PRO Financial Architecture Blueprint

## Executor

Claude Code Opus 4.8

## Task Type

Architecture / Financial Workflow Design / Accounting Blueprint / Database Integrity Design / Reporting & Reconciliation Design

## Project

ERP PRO / ERPSystem
Stack: C# / .NET / EF Core / PostgreSQL
Scope: Financial Core Architecture only

## Important Rule

This is **not an implementation task**.

Do not modify production code yet.
Do not create migrations yet.
Do not refactor handlers yet.
Do not implement permissions/RBAC yet.
Do not connect to production or remote database.
Do not guess from business assumptions without marking them clearly.

The required output is a complete design document:

`Documentation/ERP_PRO_Financial_Architecture_Blueprint.md`

This document must become the approved master blueprint before any future implementation tasks are written.

---

# 1. Background

The current forensic audit found that the system has some strong accounting foundations, especially balanced journal entries and transaction-wrapped approval flows, but it also found critical financial architecture gaps.

The most important risks to address in this blueprint are:

* China import fabric purchase cost does not post to Accounts Payable.
* Container costing and stock movement costing can exclude the actual fabric purchase price.
* Sales tax / VAT is not calculated, stored, or posted.
* Supplier payments can be posted without invoice, PO, GRN, or container linkage.
* Sales invoice approval can be double-posted under concurrency.
* Receipt/payment vouchers do not have idempotency protection.
* Core financial tables lack foreign keys.
* Purchase-side decimal precision is inconsistent.
* No complete Income Statement or Balance Sheet generation exists.
* No automated Assets = Liabilities + Equity check exists.
* No complete reversal/unapprove architecture exists.
* No bad debt / write-off mechanism exists.
* Capital partner transactions are not fully integrated with the GL.
* Soft delete is not consistently enforced across financial tables.

The goal of this task is to design a world-class financial architecture for ERP PRO that closes these gaps structurally.

---

# 2. Required Final Deliverable

Create one comprehensive Markdown document:

`Documentation/ERP_PRO_Financial_Architecture_Blueprint.md`

The document must be written in clear, professional Arabic, while keeping technical class/service/table names in English where needed.

The document must include:

1. Financial Business Vision
2. ERP Financial Workflow
3. Accounting Posting Matrix
4. Database Integrity Blueprint
5. Reporting & Reconciliation Blueprint
6. Risk Mapping from the forensic audit to the proposed architecture
7. Final Implementation Roadmap, but only as future phases, not executable code tasks
8. Open Questions and Business Decisions Required Before Implementation
9. Acceptance Checklist

Every architectural recommendation must be traceable to one of these:

* existing source code behavior,
* existing entity/schema structure,
* existing forensic audit finding,
* accounting principle,
* explicit business requirement.

Do not write vague recommendations like “improve accounting.”
Every recommendation must say:

* what financial event it controls,
* what data it needs,
* what state transition triggers it,
* what GL posting it creates,
* what database guard protects it,
* what report/reconciliation will detect failure.

---

# 3. Phase 1 — Financial Business Vision

Create a section titled:

`## 1. Financial Business Vision`

This section must answer:

## 1.1 Why the Financial Core exists

Explain that the Financial Core is not just a reporting layer. It is the source of truth for:

* Accounts Receivable
* Accounts Payable
* Inventory Value
* Cost of Goods Sold
* Revenue
* Sales Tax / VAT
* Cash and Bank
* Supplier Balances
* Customer Balances
* Partner Capital
* Profit/Loss
* Audit and Reconciliation

## 1.2 Main business problems it must solve

Cover these problems clearly:

* Prevent any financial document from affecting the business without a traceable source.
* Prevent duplicate posting.
* Prevent unlinked supplier payments.
* Ensure every stock movement has a financial value.
* Ensure every import container produces correct AP and inventory value.
* Ensure every sale produces correct AR, revenue, tax, COGS, and inventory reduction.
* Ensure every correction uses reversal, return, credit note, write-off, or adjustment — not silent edit/delete.
* Ensure Trial Balance, Income Statement, and Balance Sheet are generated from the same journal source.
* Ensure customer/supplier balances reconcile with the GL.
* Ensure inventory valuation reconciles with the GL.

## 1.3 ERP PRO financial principles

Define these as non-negotiable principles:

1. No posted financial document can be edited directly.
2. Posted documents are corrected only by reversal or linked adjustment.
3. Every GL journal entry must have a source document.
4. Every source document can post only once.
5. Every payment must be linked to a financial purpose.
6. Every AP/AR movement must be reconcilable.
7. Every inventory movement must carry cost.
8. Every tax amount must be explainable by rate, jurisdiction, and source invoice.
9. Every financial table must have database-level integrity.
10. Reports must never depend on UI-calculated values.
11. Financial periods can be locked.
12. Archiving must never hide posted financial truth.
13. Audit logs must capture financial state transitions.

## 1.4 Business domains included

Document the financial domains:

* Sales Invoice
* Sales Return / Credit Memo
* Receipt Voucher
* Purchase Order
* Goods Receipt / GRN
* Purchase Invoice
* Payment Voucher
* China Container Import
* Landing Cost
* Inventory Activation
* Stock Movement
* Customer Opening Balance
* Supplier Opening Balance
* Partner Capital
* Expenses
* Bad Debt / Write-Off
* Tax
* Period Closing
* Financial Reporting

## 1.5 Explicit out of scope for now

State clearly:

* RBAC implementation is out of scope for this blueprint.
* UI redesign is out of scope for this blueprint.
* Actual code implementation is out of scope.
* Production data verification is out of scope.
* However, the architecture must reserve places for permissions, maker-checker, audit, and approval controls.

---

# 4. Phase 2 — ERP Financial Workflow

Create a section titled:

`## 2. ERP Financial Workflow`

This section must design the full workflow states for each financial journey.

For every workflow, include:

* Document name
* Purpose
* States
* Allowed transitions
* Who/what triggers the transition conceptually
* Whether the transition posts to GL
* Whether inventory changes
* Whether AR/AP changes
* Whether the document can be cancelled
* Whether reversal is required
* Required audit events
* Required idempotency key
* Required source document reference

Use Mermaid diagrams where helpful.

---

## 2.1 Sales Invoice Workflow

Design states:

* Draft
* WarehouseDetailed
* ReadyForApproval
* Approved
* Posted
* Printed
* Delivered
* PartiallyPaid
* Paid
* Reversed
* CancelledBeforePosting
* Archived

Rules:

* Draft can be edited.
* WarehouseDetailed means roll-level quantities are selected.
* ReadyForApproval means all validations passed.
* Approved/Posted must create GL and inventory impact atomically.
* After posting, no direct edit is allowed.
* Correction after posting requires Sales Return, Credit Memo, or Reversal.
* Paid status depends on receipt allocation.
* Archive must not remove the financial effect from reports.

Required accounting events:

* Sales invoice approval
* COGS recognition
* Inventory deduction
* Tax payable recognition
* Discount recognition
* Receipt allocation
* Reversal / return

---

## 2.2 Sales Return / Credit Memo Workflow

Design states:

* Draft
* Validated
* Approved
* Posted
* Refunded
* AppliedToAR
* Reversed
* Archived

Rules:

* Must reference original Sales Invoice unless business explicitly allows standalone credit memo.
* Must restore inventory if goods are physically returned.
* Must reverse revenue/AR/tax/COGS according to the original invoice.
* Must not exceed original invoice remaining eligible return quantity/value.
* Must create audit trail.

---

## 2.3 Receipt Voucher Workflow

Design states:

* Draft
* Allocated
* Posted
* PartiallyAllocated
* FullyAllocated
* UnappliedCredit
* Reversed
* Archived

Rules:

* Receipt must be linked to customer.
* Receipt can be allocated to one or more invoices.
* Overpayment must not vanish.
* Overpayment must create customer unapplied credit / liability.
* Receipt must have external reference or idempotency key where available.
* Same real-world receipt should not be posted twice silently.
* Reversal must restore AR/open credit correctly.

---

## 2.4 Purchase Order Workflow

Design states:

* Draft
* Submitted
* Approved
* SentToSupplier
* PartiallyReceived
* FullyReceived
* PartiallyInvoiced
* FullyInvoiced
* Closed
* Cancelled
* Archived

Rules:

* PO does not post to GL by default.
* PO is a commitment, not AP.
* PO must feed GRN and Purchase Invoice matching.
* PO quantities, prices, currency, and supplier must be preserved for matching.
* Cancellation rules depend on whether receipt or invoice already exists.

---

## 2.5 Goods Receipt / GRN Workflow

Design states:

* Draft
* Received
* QualityChecked
* Accepted
* Rejected
* PartiallyAccepted
* PostedToInventory
* Reversed
* Archived

Rules:

* GRN must reference PO or approved import/container document.
* GRN records actual received quantities.
* GRN is the physical truth.
* Purchase Invoice is the supplier’s financial claim.
* PO + GRN + Invoice must support three-way match.
* GRN may create inventory-in-transit or received-not-invoiced accounting depending on selected accounting policy.
* Any rejected quantity must not become sellable stock.

---

## 2.6 Purchase Invoice Workflow

Design states:

* Draft
* Matched
* Exception
* Approved
* Posted
* PartiallyPaid
* Paid
* Reversed
* Archived

Rules:

* Purchase Invoice must reference supplier.
* For goods, it must reference PO/GRN/container line.
* It must not be postable if matching fails beyond tolerance.
* It must post AP.
* It must update inventory value or expense depending on invoice type.
* It must support tax/VAT input if applicable.
* Over-invoicing and under-invoicing must be visible exceptions.
* Payment allocation must reduce AP.

---

## 2.7 Payment Voucher Workflow

Design states:

* Draft
* Allocated
* Approved
* Posted
* PartiallyAllocated
* FullyAllocated
* SupplierPrepayment
* Reversed
* Archived

Rules:

* Payment must be linked to supplier.
* Payment for goods must be linked to purchase invoice, PO, GRN, or container.
* If payment is made before invoice, it must post as supplier prepayment, not disappear.
* Overpayment must create supplier unapplied debit/prepayment.
* Payment must have bank/cashbox source.
* Payment must have idempotency protection.
* Reversal must restore AP/prepayment correctly.

---

## 2.8 China Container Import Workflow

Design states:

* DraftContainer
* SupplierInvoiceEntered
* LandingCostsEntered
* Approved
* InTransit
* Arrived
* Received
* CostAllocated
* StockActivated
* APPosted
* PartiallyPaid
* Paid
* Closed
* Reversed
* Archived

Rules:

* China supplier invoice amount must be captured.
* Exchange rate must be captured and locked at posting time.
* Fabric purchase cost must post to AP.
* Landing costs must post to AP/cash/clearing depending on source.
* Inventory-in-transit must include fabric cost.
* Final inventory value must include fabric cost + allocated landing costs.
* Cost per roll, cost per meter, batch cost, and stock movement cost must use one unified CostingEngine.
* No stock activation if cost allocation is incomplete.
* No AP closure if supplier invoice is missing.
* Container cannot be closed while unmatched costs or unposted accounting events exist.

Required accounting events:

* Supplier fabric invoice:

  * Dr Inventory In Transit
  * Cr Accounts Payable

* Landing cost invoice:

  * Dr Landing Cost Clearing or Inventory In Transit
  * Cr Accounts Payable / Cash / Bank

* Stock activation:

  * Dr Inventory
  * Cr Inventory In Transit / Landing Cost Clearing

* Supplier payment:

  * Dr Accounts Payable
  * Cr Cash / Bank

---

## 2.9 Inventory Movement Workflow

Design states:

* Draft
* Reserved
* Issued
* Received
* Posted
* Reversed
* Archived

Rules:

* Every inventory movement must have source document.
* Every financial inventory movement must carry unit cost.
* Negative remaining length must be impossible at domain and database levels.
* Roll-level movement must reconcile with stock movement and GL.
* Inventory adjustments require reason and audit.

---

## 2.10 Opening Balance Workflow

Design states:

* Draft
* Validated
* Approved
* Posted
* Locked
* ReversedByAdjustmentOnly

Rules:

* Opening balances must post to GL.
* Customer/supplier opening balances must reconcile with AR/AP.
* Inventory opening value must reconcile with inventory asset account.
* Partner capital opening must reconcile with equity.
* After lock, only adjustment entries are allowed.

---

## 2.11 Partner Capital Workflow

Design states:

* Draft
* Approved
* Posted
* Distributed
* Withdrawn
* Reversed
* Archived

Rules:

* Capital contributions must post to GL.
* Withdrawals must post to GL.
* Profit/loss distribution must post to GL.
* Partner capital subledger must reconcile with equity accounts.
* Capital module must not remain isolated from GL.

---

## 2.12 Bad Debt / Write-Off Workflow

Design states:

* Draft
* Reviewed
* Approved
* Posted
* Reversed
* Archived

Rules:

* Must reference customer and invoice.
* Must require reason.
* Must post:

  * Dr Bad Debt Expense or Allowance for Doubtful Accounts
  * Cr Accounts Receivable
* Must reduce customer collectible balance.
* Must preserve audit trail.

---

## 2.13 Financial Period Lock Workflow

Design states:

* Open
* SoftClosed
* HardClosed
* ReopenedWithApproval

Rules:

* Open period allows normal posting.
* SoftClosed allows restricted adjustments.
* HardClosed blocks posting.
* Reopening requires audit reason.
* Reports must show period status.
* All posting services must check period status before posting.

---

# 5. Phase 3 — Accounting Posting Matrix

Create a section titled:

`## 3. Accounting Posting Matrix`

Build a complete table for all major accounting events.

For each row include:

* Event code
* Business event
* Source document
* Trigger state transition
* Debit account
* Credit account
* Amount formula
* Currency rule
* Tax rule
* Inventory effect
* AR/AP effect
* Idempotency key
* Reversal event
* Required validations
* Failure handling

---

## 3.1 Required posting events

At minimum include these events:

### Sales

1. Sales invoice approval
2. Sales discount recognition
3. Sales tax payable recognition
4. COGS recognition
5. Inventory deduction
6. Receipt voucher posting
7. Customer overpayment / unapplied credit
8. Sales return
9. Sales invoice reversal
10. Bad debt write-off

### Purchasing

11. Purchase invoice posting
12. Purchase invoice tax input
13. Supplier payment posting
14. Supplier overpayment / prepayment
15. Purchase invoice reversal
16. Three-way match variance
17. Price variance
18. Quantity variance

### China Import

19. China fabric supplier invoice posting
20. Landing cost invoice posting
21. Customs/duty posting
22. Insurance/freight posting
23. Inventory in transit recognition
24. Landing cost allocation
25. Stock activation
26. FX difference recognition
27. Container close accounting check

### Inventory

28. Stock receipt
29. Stock issue
30. Stock transfer
31. Inventory adjustment increase
32. Inventory adjustment decrease
33. Negative stock prevention event
34. Roll length correction with audit

### Cash/Bank

35. Cashbox receipt
36. Cashbox payment
37. Bank transfer
38. Bank fee
39. Bank reconciliation adjustment

### Capital / Equity

40. Partner capital contribution
41. Partner withdrawal
42. Profit distribution
43. Loss allocation

### Closing

44. Period closing entries
45. Retained earnings transfer
46. Opening balance posting
47. Opening balance adjustment

---

## 3.2 Required account groups

Define the needed Chart of Accounts groups:

* Assets

  * Cash
  * Bank
  * Accounts Receivable
  * Inventory
  * Inventory In Transit
  * Prepaid Expenses
  * Supplier Prepayments
* Liabilities

  * Accounts Payable
  * Sales Tax Payable / VAT Output
  * Customer Advances / Unapplied Credits
  * Accrued Expenses
* Equity

  * Partner Capital
  * Retained Earnings
  * Current Year Earnings
* Revenue

  * Sales Revenue
  * Sales Returns
  * Sales Discounts
* COGS

  * Cost of Goods Sold
  * Inventory Variance
* Expenses

  * Freight
  * Customs
  * Clearance
  * Insurance
  * Bad Debt Expense
  * Bank Fees
* Contra / Clearing

  * Landing Cost Clearing
  * Inventory Clearing
  * Suspense Account, only if explicitly allowed and reported

---

## 3.3 Posting engine design

Design the following conceptual services:

* `PostingEngine`
* `AccountingEvent`
* `JournalEntryFactory`
* `JournalEntryValidator`
* `IdempotencyService`
* `FinancialPeriodLockService`
* `ReversalEngine`
* `TaxEngine`
* `CostingEngine`
* `ThreeWayMatchEngine`
* `ReconciliationService`
* `AuditTrailService`

For each service define:

* Responsibility
* Inputs
* Outputs
* What it must not do
* Which workflows call it
* What database constraints support it

---

## 3.4 Idempotency design

Define idempotency rules:

* Every posting event must have `SourceType + SourceId + EventType`.
* Database must enforce uniqueness on posted journal source.
* Retried requests must return existing result, not create a duplicate.
* Receipt/payment vouchers must support external reference or client idempotency key.
* Duplicate detection must warn on same party + amount + date + method + reference.
* Idempotency must exist at both application and database levels.

---

## 3.5 Reversal design

Define reversal rules:

* Posted journal entries are never deleted.
* Reversal creates a mirrored journal entry.
* Reversal links to original journal entry.
* Source document status changes to Reversed.
* Reversal requires reason.
* Reversal checks financial period status.
* Reversal updates subledger state.
* Inventory reversal must restore roll-level quantities only when physically valid.
* Reports must show original and reversal clearly.

---

# 6. Phase 4 — Database Integrity Blueprint

Create a section titled:

`## 4. Database Integrity Blueprint`

This section must define the database-level protections required to make the financial architecture safe.

Do not write migrations yet.
Only design the target constraints, indexes, and migration sequence.

---

## 4.1 Foreign Keys

Design required FKs for:

### Accounting

* `journal_entry_lines.JournalEntryId → journal_entries.Id`
* `journal_entry_lines.AccountId → Accounts.Id`
* `journal_entries.SourceDocumentId → source table pattern or polymorphic source registry`

### Sales

* `sales_invoice_items.SalesInvoiceId → sales_invoices.Id`
* `sales_invoices.CustomerId → customers.Id`
* `receipt_invoice_payments.SalesInvoiceId → sales_invoices.Id`
* `receipt_invoice_payments.ReceiptVoucherId → receipt_vouchers.Id`
* `receipt_vouchers.CustomerId → customers.Id`

### Purchasing

* `purchase_invoice_items.PurchaseInvoiceId → purchase_invoices.Id`
* `purchase_invoices.SupplierId → suppliers.Id`
* `purchase_orders.SupplierId → suppliers.Id`
* `purchase_invoice_payments.PurchaseInvoiceId → purchase_invoices.Id`
* `purchase_invoice_payments.PaymentVoucherId → payment_vouchers.Id`
* `payment_vouchers.SupplierId → suppliers.Id`

### GRN / Three-Way Match

Design proposed FKs for future entities:

* `goods_receipts.PurchaseOrderId → purchase_orders.Id`
* `goods_receipt_lines.GoodsReceiptId → goods_receipts.Id`
* `goods_receipt_lines.PurchaseOrderLineId → purchase_order_lines.Id`
* `purchase_invoice_lines.GoodsReceiptLineId → goods_receipt_lines.Id`

### Inventory

* `stock_movement_lines.StockMovementId → stock_movements.Id`
* `stock_movement_lines.FabricRollId → FabricRolls.Id`
* `FabricRolls.ContainerId → containers.Id`
* `FabricRolls.WarehouseId → warehouses.Id`

### China Import

* `containers.SupplierId → suppliers.Id`
* `landing_costs.ContainerId → containers.Id`
* `container_cost_allocations.ContainerId → containers.Id`
* `container_cost_allocations.FabricRollId → FabricRolls.Id`

### Capital

* `capital_transactions.PartnerId → partners.Id`
* `capital_transactions.JournalEntryId → journal_entries.Id`

---

## 4.2 Unique Indexes

Design unique indexes for:

* `journal_entries(SourceType, SourceId, EventType)`
* `sales_invoices(CompanyId, InvoiceNumber)`
* `purchase_invoices(CompanyId, InvoiceNumber)`
* `receipt_vouchers(CompanyId, VoucherNumber)`
* `payment_vouchers(CompanyId, VoucherNumber)`
* `document_counters(BranchId, DocumentType)`
* `Accounts(CompanyId, Code)`
* `customers(CompanyId, Code)`
* `suppliers(CompanyId, Code)`
* `FabricRolls(RollNumber)` or company/container scoped equivalent
* external payment reference where available:

  * party + amount + date + method + external reference

Explain which indexes are hard uniqueness and which are duplicate-warning candidates.

---

## 4.3 Check Constraints

Design check constraints for:

* Amounts cannot be negative unless explicitly allowed by document type.
* Debit and Credit cannot both be positive on the same journal line.
* Debit and Credit cannot both be zero.
* Fabric roll remaining length must be `>= 0`.
* Fabric roll remaining length must be `<= original length`.
* Tax rate must be between 0 and 1 or 0 and 100 depending on standard chosen.
* Exchange rate must be positive.
* Quantity meters must be positive.
* Unit price must be non-negative.
* Paid amount cannot exceed total unless overpayment is posted to unapplied credit/prepayment.
* Document status must be valid enum value.
* Financial period end date must be after start date.

---

## 4.4 Decimal Precision

Define standard precision:

* Money: `numeric(18,2)`
* Exchange rate: `numeric(18,6)` or `numeric(18,8)`
* Fabric length meters: `numeric(18,4)`
* Unit cost per meter: `numeric(18,6)`
* Percentage/tax rate: `numeric(9,6)`
* Quantity: `numeric(18,4)`

Identify all purchase-side and import-side monetary columns that must be aligned with this standard.

---

## 4.5 Concurrency

Design concurrency protections:

* Use PostgreSQL `xmin` or explicit `RowVersion`.
* Required for:

  * sales invoices
  * purchase invoices
  * receipt vouchers
  * payment vouchers
  * document counters
  * fabric rolls
  * stock movements
  * containers
  * journal entries
* Approval must update with expected status.
* Inventory deduction must update with expected remaining length/version.
* Number generation must use sequence or retry-safe counter.
* Double approval must be impossible.
* Double inventory deduction must be impossible.

---

## 4.6 Soft Delete / Archive Rules

Define:

* Posted financial rows are not physically deleted.
* `IsArchived` hides from operational screens but not financial reports.
* Financial reports must include posted documents regardless of archive flag unless explicitly filtered.
* Query filters must not accidentally hide posted GL truth.
* Archive action must write audit log.
* Archive must be blocked for documents with unresolved financial impact unless design explicitly allows financial archive.

---

## 4.7 Audit Trail

Design audit requirements:

* State changes
* Approval
* Posting
* Reversal
* Payment allocation
* Tax change
* Cost allocation
* Price override
* Quantity override
* Supplier/customer balance adjustment
* Period lock/unlock
* Failed posting attempt
* Duplicate detection warning override

Each audit log must include:

* Entity type
* Entity id
* Old state
* New state
* User id if available
* Timestamp
* Reason
* Correlation id
* Source command
* Financial impact summary

---

## 4.8 Migration Safety Plan

Create a future migration strategy, but do not implement it.

Include:

1. Pre-migration diagnostics
2. Orphan row detection
3. Duplicate document detection
4. Negative stock detection
5. Decimal normalization check
6. Backfill source document references
7. Add nullable FK first if needed
8. Clean data
9. Enforce NOT NULL
10. Add FK
11. Add unique index
12. Add check constraints
13. Add concurrency tokens
14. Deploy with feature flags where needed
15. Run reconciliation reports after migration

---

# 7. Phase 5 — Reporting & Reconciliation Blueprint

Create a section titled:

`## 5. Reporting & Reconciliation Blueprint`

This section must design the reports and automated checks needed to prove the financial system is correct.

---

## 5.1 Core Financial Statements

Design:

### Trial Balance

* Source: posted journal entry lines only.
* Group by account.
* Debit/Credit totals.
* Must balance within tolerance.
* Must support date range and company/branch.

### Income Statement

* Revenue
* Sales returns
* Sales discounts
* Net revenue
* COGS
* Gross profit
* Operating expenses
* Other income/expenses
* Net profit/loss

### Balance Sheet

* Assets
* Liabilities
* Equity
* Current year earnings
* Retained earnings
* Must include automated:

  * Assets = Liabilities + Equity

### General Ledger

* Account movement details
* Opening balance
* Debits
* Credits
* Ending balance
* Source document links

---

## 5.2 Subledger Reports

Design:

* Customer Statement
* Supplier Statement
* AR Aging
* AP Aging
* Customer unapplied credits
* Supplier prepayments
* Receipt allocation report
* Payment allocation report
* Open invoices
* Partially paid invoices
* Overdue invoices
* Bad debt/write-off report

Each subledger report must reconcile to GL.

---

## 5.3 Inventory and Costing Reports

Design:

* Inventory Valuation
* Fabric Roll Ledger
* Stock Movement Report
* Container Cost Sheet
* Landing Cost Allocation Report
* Cost per Meter Report
* Inventory In Transit Report
* COGS Reconciliation
* Negative Stock Exception Report
* Stock vs GL Inventory Reconciliation

The Container Cost Sheet must show:

* Fabric supplier invoice amount
* Exchange rate
* Fabric cost in local currency
* Freight
* Customs
* Clearance
* Insurance
* Other landing costs
* Total landed cost
* Total meters
* Cost per meter
* Cost per roll
* Variance
* Posted AP
* Posted inventory value
* Remaining unmatched costs

---

## 5.4 Tax Reports

Design:

* Sales Tax / VAT Output Report
* Purchase Tax / VAT Input Report
* Tax Payable Summary
* Tax by invoice
* Tax by period
* Tax reconciliation to GL
* Tax exceptions:

  * invoice with zero tax where tax should apply
  * tax amount mismatch
  * missing tax rate
  * manual override

---

## 5.5 Reconciliation Checks

Design automated reconciliation checks:

1. Journal entries balance check
2. AR subledger vs GL AR
3. AP subledger vs GL AP
4. Inventory valuation vs GL inventory
5. Inventory in transit vs open containers
6. Landing cost clearing should clear after stock activation
7. Customer unapplied credits vs liability account
8. Supplier prepayments vs asset account
9. Sales tax report vs tax payable GL
10. Trial Balance total debit vs total credit
11. Balance Sheet A = L + E
12. Duplicate posting check
13. Duplicate voucher check
14. Unlinked payment check
15. Orphan journal line check
16. Negative fabric length check
17. Posted document without journal check
18. Journal without source document check
19. Closed period posting check
20. Archived posted document visibility check

For each check include:

* Purpose
* Query concept
* Expected result
* Severity if failed
* Suggested remediation path

---

## 5.6 Financial Dashboard KPIs

Design dashboard KPIs:

* Cash balance
* AR total
* AP total
* Overdue receivables
* Overdue payables
* Inventory value
* Inventory in transit
* Landing cost clearing balance
* Sales this month
* Gross margin
* Net profit
* Tax payable
* Unposted documents
* Failed postings
* Reconciliation exceptions
* Containers with incomplete costing
* Supplier payments without allocation
* Customer credits unapplied
* Negative stock exceptions

---

# 8. Risk Mapping Section

Create a section titled:

`## 6. Risk Mapping From Audit Findings To Architecture`

Create a table with:

* Audit finding code
* Risk summary
* Proposed architectural control
* Workflow affected
* Posting matrix affected
* Database integrity control
* Report/reconciliation detection
* Priority

At minimum map:

* F-01 China fabric cost not posted to AP
* F-02 Sales tax not calculated/posted
* F-03 Supplier payments unlinked
* F-04 Double approval / double posting
* F-05 Segregation of duties, but design only for now
* F-07 Missing FKs
* F-08 Overpayment handling
* F-09 Missing financial statements
* F-10 Document counter concurrency
* F-11 Decimal precision
* F-12 Bad debt/write-off
* F-13 Capital not integrated with GL
* F-14 Container cost excludes fabric price
* F-15 Soft delete filters
* F-16 No reversal path
* F-17 Negative roll length
* F-18 No idempotency key

---

# 9. Future Implementation Roadmap

Create a section titled:

`## 7. Future Implementation Roadmap`

Important: This is only a roadmap, not executable coding tasks.

Divide future work into safe implementation waves:

## Wave 1 — Financial Safety Foundation

* Posting idempotency
* Journal source uniqueness
* RowVersion/xmin concurrency
* Document counter fix
* Negative stock check
* Decimal precision standard
* FK diagnostics before enforcement

## Wave 2 — China Import and Costing Fix

* Post fabric supplier invoice to AP
* Unified CostingEngine
* Inventory in transit
* Landing cost allocation
* Container cost sheet
* Inventory activation posting

## Wave 3 — Tax Engine

* Tax configuration
* Invoice tax calculation
* Tax payable posting
* Tax reporting
* Tax reconciliation

## Wave 4 — Purchasing Controls

* GRN entity
* Three-way match
* Mandatory payment allocation
* Supplier prepayment handling
* AP aging

## Wave 5 — Reversal and Corrections

* ReversalEngine
* Sales invoice reversal
* Purchase invoice reversal
* Receipt/payment reversal
* Inventory adjustment correction
* Audit trail

## Wave 6 — Reporting

* Income Statement
* Balance Sheet
* A = L + E check
* AR/AP reconciliation
* Inventory valuation reconciliation
* Tax reconciliation

## Wave 7 — Capital / Write-Off / Closing

* Capital GL integration
* Bad debt/write-off
* Period lock
* Closing entries
* Retained earnings

---

# 10. Open Questions

Create a section titled:

`## 8. Open Questions Before Implementation`

Include business decisions needed from the owner:

1. What is the official base currency?
2. Are China supplier invoices in USD only or multiple currencies?
3. Should inventory be recognized at supplier invoice date, container approval, arrival, or warehouse activation?
4. What tax model is required: VAT, sales tax, or both?
5. Are sales taxes jurisdiction-based or fixed company rate?
6. Should PO approval create commitments only or accounting encumbrance?
7. Should GRN create inventory before supplier invoice?
8. How should price variance be treated?
9. How should quantity variance be treated?
10. Are customer overpayments allowed?
11. Are supplier prepayments common?
12. Should sales invoice approval equal delivery, or should delivery be a separate revenue recognition event?
13. Is COGS recognized on invoice approval or delivery?
14. How many financial periods are allowed open?
15. Who can reopen a closed period later, conceptually?
16. Is archive allowed for posted documents?
17. Should reversal be allowed across closed periods?
18. What is the required decimal precision for fabric pricing?
19. How should landed cost be allocated: by meters, value, weight, roll count, or manual?
20. Should partner capital be tracked per partner account in GL?

---

# 11. Acceptance Checklist

Create a final section titled:

`## 9. Acceptance Checklist`

The blueprint is accepted only if all items are satisfied:

* All five requested phases are documented.
* Every major financial journey has states and transitions.
* Every posting event has debit/credit design.
* China import fabric cost is explicitly handled.
* Landing cost and fabric cost are unified into inventory costing.
* Sales tax/VAT is explicitly handled.
* Supplier payments cannot remain financially meaningless.
* Overpayments are handled through unapplied credit/prepayment.
* Reversal architecture is defined.
* Idempotency architecture is defined.
* Concurrency architecture is defined.
* FK strategy is defined.
* Decimal precision standard is defined.
* Negative stock protection is defined.
* Soft delete/archive reporting behavior is defined.
* Audit trail requirements are defined.
* Trial Balance, Income Statement, and Balance Sheet are designed.
* A = L + E check is designed.
* AR/AP/Inventory/Tax reconciliations are designed.
* Risk mapping from audit findings to architecture is included.
* Future implementation waves are listed without writing actual implementation code.
* Open business questions are listed.
* The document is clear enough to generate future Cursor/Claude implementation tasks from it.

---

# 12. Quality Bar

The final document must be detailed enough that a senior ERP architect, accountant, and backend engineer can review it together.

Avoid generic advice.

Prefer tables.

Prefer explicit state machines.

Prefer explicit accounting entries.

Prefer explicit database constraints.

Prefer explicit report reconciliation checks.

Do not hide uncertainty.
When a business rule requires owner decision, mark it as:

`Decision Required`

When a design depends on current source code verification, mark it as:

`Needs Code Verification`

When a design is recommended as best practice but not currently implemented, mark it as:

`Proposed Architecture`

---

# 13. Final Output Required From Claude Code

After completing the analysis, output:

1. Path of created document:
   `Documentation/ERP_PRO_Financial_Architecture_Blueprint.md`

2. Short summary:

   * what sections were created,
   * what risks were covered,
   * what decisions are still required.

3. Do not claim implementation is complete.

4. Do not create coding tasks yet.

5. Do not modify application source code.
