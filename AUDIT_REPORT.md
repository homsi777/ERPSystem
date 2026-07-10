# Payment & Accounting Workflow Forensic Audit — ERPSystem

**Prepared:** 2026-07-08
**Scope:** ERPSystem.Domain / ERPSystem.Application / ERPSystem.Infrastructure / ERPSystem.Api (EF Core + PostgreSQL)
**Method:** Static code and migration analysis — no production data was queried (see §2)
**Standard:** every claim below is backed by a file:line citation. No guesses.

---

## 1. Executive Summary

ERPSystem's ledger is internally self-consistent where it posts at all — every journal entry the code writes is balanced by a domain guard, and cash-sale approvals run inside a real database transaction with rollback. The risk is not "the math is wrong." The risk is **what the code never records**: the actual purchase cost of imported fabric never reaches Accounts Payable, sales tax is computed nowhere, two people can approve the same invoice at once, and nothing stops the same user from creating and approving their own sale. For a business moving 200,000+ rolls and 500+ containers a year, these are not edge cases — they are the main path.

**Overall rating: Amber, trending Red on the purchasing side.** The sales-approval pipeline is the most mature part of the system: real transactions, a real balanced-journal guard, real credit-limit enforcement. The purchasing / China-import pipeline — which is where the actual inventory cost and the bulk of cash outflow originates — has no three-way match, no enforced linkage between payment and invoice, and a documented gap where the largest cost line (the fabric itself) never reaches the general ledger. That asymmetry is the headline finding of this audit.

| # | Finding | Severity | Business impact |
|---|---|---|---|
| F-01 | China-import fabric cost never posts to Accounts Payable | 🔴 Critical | AP and COGS understated by the entire goods cost on the dominant purchase channel |
| F-02 | Sales tax is never calculated or posted | 🔴 Critical | Every sales invoice is under-taxed by design; tax filings cannot reconcile to the GL |
| F-03 | Supplier payments can post with zero link to any invoice, PO, or container | 🔴 Critical | AP cannot be traced to what was paid for; duplicate/ghost payments are undetectable |
| F-04 | Concurrent approval race can double-post an invoice | 🔴 Critical | Double inventory deduction and double revenue/AR under normal concurrent use |
| F-05 | No segregation of duties — one role can create, approve, and post | 🔴 Critical | No structural control against a single actor recording a fictitious sale end-to-end |
| F-06 | Database superuser password hardcoded and committed to git | 🔴 Critical | Full read/write access to the financial database for anyone with repo access |
| F-07 | Zero foreign-key constraints across the entire financial schema | 🟠 High | Database cannot itself prevent orphaned invoices, lines, or GL postings |
| F-08 | Overpayment handling is inconsistent and silently lossy on the AP side | 🟠 High | Supplier overpayments are capped and the excess vanishes with no trace |
| F-09 | No Income Statement or Balance Sheet generation; no A=L+E check anywhere | 🟠 High | Board cannot get a system-generated P&L or balance sheet; no self-check exists |
| F-10 | Invoice-numbering counter's concurrency lock is a dead no-op | 🟠 High | Concurrent invoice creation can collide; the intended fix migration is empty |
| F-11 | Purchasing-side monetary columns have no defined decimal precision | 🟡 Medium | Unconstrained numeric storage can break downstream balance comparisons |
| F-12 | No bad-debt / write-off / doubtful-accounts mechanism exists | 🟡 Medium | Uncollectible receivables have no defined, auditable exit from AR |
| F-13 | Capital Partners module isolated from GL after opening balance | 🟡 Medium | Partner capital balances can drift from the Trial Balance's equity account |
| F-14 | Container batch/stock-movement unit cost excludes fabric purchase price | 🟡 Medium | Inventory value understated, compounding F-01 onto the balance sheet |
| F-15 | Soft-delete flag enforced on only 6 of ~20 financial tables | 🟡 Medium | Archived journal entries/vouchers/invoices can still surface in reports |
| F-16 | No reversal/unapprove path for an approved sales invoice | 🟡 Medium | Erroneous approvals can only be offset via a return, never truly undone |
| F-17 | No DB check constraint preventing negative fabric-roll length | 🟡 Medium | Nothing stops a negative remaining length under concurrent deduction |
| F-18 | No idempotency key on receipt/payment vouchers | 🟡 Medium | Duplicate real-world payment entry is possible and unguarded |

---

## 2. Scope & Methodology

This audit was run against the source code and EF Core migrations in `c:\Users\Homsi\Desktop\POS ERP C#\ERPSystem` as of the current `main` branch. Before starting, two live databases were identified as reachable from this machine:

- A local PostgreSQL instance (`localhost:5432/erp_pro`) containing only 9 legacy tables (`FabricRolls`, `ImportBatches`, `Accounts`, etc.) — it does **not** match the current EF Core model and has none of the Sales/Payment/JournalEntry tables this audit is about.
- A remote server (`65.21.136.217`) reachable only via an SSH tunnel configured in the git-ignored `appsettings.Local.json`, which appears to hold the real working data.

Per your explicit choice, this audit did **not** open that tunnel or query the remote server — connecting to a live external host with embedded credentials needs a standing decision from you, not an assumption. Every finding below is therefore derived from **static analysis of the actual C# code, EF Core entity configurations, and generated migration DDL**, which is sufficient to answer nearly everything task.md asked (schema shape, constraints, transaction behavior, business-logic gaps, security posture) and describes what the system *will always do*, not just what one dataset happened to contain.

**What this audit could not do:** it could not run the six verification queries from task.md against real rows, could not compute an actual trial balance, and could not report a dollar figure for "how much AP is currently understated." §7 provides those queries rewritten against the *real* schema, ready to run the moment you authorize access to production or a restored copy of it.

---

## 3. Architecture Note

**3.1 Two entity layers.** The solution keeps a rich **domain layer** (`ERPSystem.Domain/Entities`, `/Aggregates`) with private setters and business-rule guards, separate from the **persistence layer** (`ERPSystem.Infrastructure/Persistence/Models/*Entity` classes) that EF Core actually maps to PostgreSQL. The domain layer's guards (e.g. "quantity must be positive") only protect callers that go through the aggregate's methods — the persistence POCOs underneath have public setters and no validation of their own. Every integrity guarantee in this report had to be traced to the specific code path that enforces it, because a different, more direct path to the same table may not carry the same guarantee.

**3.2 Two purchase channels, one of them un-audited by design.** There are effectively two purchasing pipelines: a conventional `PurchaseOrder → PurchaseInvoice` flow, and a China-container-import flow (`ContainerAggregate`, `LandingCost`, `ChinaImportTypeCostAllocator`). Per the task brief's own numbers (500+ containers/year), the container path is the dominant one — and it is the one with the GL gap in F-01 and no three-way match at all (F-03/F-13... see F-03).

---

## 4. Detailed Findings

Ordered by severity. Each finding states what's wrong, the exact evidence, the business impact, the root cause, and a scoped fix.

### F-01 · General Ledger / Purchasing — 🔴 Critical
**The actual fabric purchase cost is never posted to Accounts Payable**

For containers imported from China — the dominant purchase channel — the only journal entry ever created on approval is **Dr Landing-Cost-Clearing / Cr Accounts Payable** for freight, insurance, customs, and clearance (`LandingCost.TotalSharedExpenses`). The China supplier's actual invoice amount for the fabric itself, `ChinaInvoiceAmountUsd`, is captured on the container record but is never passed to the accounting service and never posted anywhere in the GL.

Evidence: `IntegratedAccountingService.cs:24-44 PostContainerApprovalAsync`, `InventoryEngine.cs:1297-1303 CalculateContainerCostPerMeter`, `Documentation/China_Import_Diagnostic_Audit.md:422,441-449`.

```csharp
// IntegratedAccountingService.cs — the ONLY GL entry a container ever generates
Dr AccountingAccountIds.LandingCostClearing   totalExpenses   // freight+insurance+customs+clearance only
Cr AccountingAccountIds.AccountsPayable       totalExpenses
// ChinaInvoiceAmountUsd (the fabric cost itself) is never referenced by any
// IIntegratedAccountingService method — grepped every usage in the repo.
```

The project's own internal note (`Documentation/China_Import_Diagnostic_Audit.md`) already flags the related 2% tax-reserve field as "not GL" — this finding is the larger version of the same gap: the entire fabric cost, not just the tax reserve, is invisible to the ledger.

- **Impact:** Accounts Payable and Cost of Goods Sold are structurally understated by the value of every container's fabric — for a business doing 500+ containers/year at "millions of dollars" scale, this is the majority of the purchase-side liability simply not existing in the books.
- **Root cause:** `PostContainerApprovalAsync` was written against `LandingCost` only; `ChinaInvoiceAmountUsd` was added later (migration `AddChinaInvoiceAmountUsd`) as a standalone field and never wired into the posting call.
- **Fix scope:** Extend `PostContainerApprovalAsync` to also post **Dr Inventory-in-Transit / Cr Accounts Payable** for `ChinaInvoiceAmountUsd × ExchangeRateToLocalCurrency`, and correct `CalculateContainerCostPerMeter` (F-14) so the same fix doesn't leave stock valued at freight-only.

---

### F-02 · Sales / Tax — 🔴 Critical
**Sales tax is never calculated, stored, or posted**

`SalesInvoiceAggregate.TaxTotal` defaults to zero and has no public method to set it anywhere in the domain, application, or infrastructure layers. `GrandTotal = SubTotal + TaxTotal − DiscountTotal` therefore always reduces to `SubTotal − DiscountTotal`. A decorative `TaxRate = 0.15m` exists in the legacy WinForms UI model (`Core/Sales/SalesModels.cs:90-92`) but is disconnected from the actual persisted aggregate.

Evidence: `SalesInvoiceAggregate.cs:27,256-264 RecalculateGrandTotal`, `AggregateMappers.cs:89 DomainHydrator.Set(...TaxTotal)`, `Core/Sales/SalesModels.cs:90-92` (UI-only, unused).

- **Impact:** Every sales invoice ever approved under-collects tax by 100% of whatever the applicable rate should be. There is no data-level fix for past invoices — this is a going-forward compliance gap plus a historical exposure that needs its own reconciliation once quantified.
- **Root cause:** Tax calculation was never implemented on the aggregate; the property exists only as a column placeholder for a feature that was scaffolded but not built.
- **Fix scope:** Add a tax-rate resolution service (by jurisdiction/company config), a `SalesInvoiceAggregate.ApplyTax(...)` method, wire it into `CreateSalesInvoiceDraftHandler`/`ApproveSalesInvoiceHandler`, and post a `Sales_Tax_Payable` line in `PostSalesInvoiceApprovalAsync`.

---

### F-03 · Purchasing / Payments — 🔴 Critical
**Supplier payments can post with no linkage to any invoice, PO, or container**

`RecordPaymentVoucherCommand.PurchaseInvoiceId` is nullable. When it's null, `PostPaymentVoucherHandler` posts straight against the supplier's aggregate balance with zero linkage to any invoice, purchase order, goods receipt, or container — there is no goods-receipt entity in the codebase at all, and even PO→Invoice conversion copies ordered quantity 1:1 with no comparison to anything actually received.

Evidence: `FinanceHandlers.cs:217-233` (invoice-linking block only runs `if (command.PurchaseInvoiceId is Guid invoiceId)`), `PurchaseHandlers.cs:291-340 ConvertPurchaseOrderToInvoiceHandler`, `PurchaseEnums.cs:19-25` (binary Draft/Sent/Received/Cancelled, no partial-receipt state).

- **Impact:** A payment can be recorded against a supplier with no record of what it was for — both a control gap (a duplicate or fictitious payment is invisible) and an audit-trail gap.
- **Root cause:** No goods-receipt/GRN entity was ever built, so there is nothing for a three-way match to compare against even if the linkage were made mandatory.
- **Fix scope:** Introduce a minimal `GoodsReceipt` entity with received-quantity per line; make `PurchaseInvoiceId` mandatory on payment vouchers tied to goods; add a three-way match check (PO qty ↔ receipt qty ↔ invoice qty) before `PostPurchaseInvoiceHandler` allows posting.

---

### F-04 · Sales / Concurrency — 🔴 Critical
**Concurrent approval requests can double-post the same invoice**

`SalesInvoiceEntity` has no `RowVersion`/concurrency token, and its repository update is an unconditional overwrite by primary key — no `WHERE Status = 'ReadyForApproval'` guard. The only double-approval protection is an in-memory status check performed *before* the transaction opens. Journal-entry idempotency (`PostIfNotExistsAsync`) is a check-then-act with no unique DB index backing it. Two concurrent `ApproveSalesInvoiceCommand` requests for the same invoice can both pass validation, both deduct inventory, and both post journal entries.

Evidence: `AggregateRepositories.cs:270-282 SalesInvoiceRepository.UpdateAsync` (unconditional overwrite by Id), `DomainSpecifications.cs:7-27 InvoiceCanBeApprovedSpecification` (in-memory only), `IntegratedAccountingService.cs:437-461 PostIfNotExistsAsync` (TOCTOU, no unique index on SourceType+SourceId), `InventoryEngine.cs:617-698 IssueForInvoiceAsync` (load-mutate-save, no row lock).

- **Impact:** Under real concurrent usage (two warehouse staff, or a UI double-click plus a retry), the same invoice can generate two sets of journal entries and deduct fabric-roll length twice — a direct, silent corruption of both inventory and the GL that requires no malicious actor, just normal timing.
- **Root cause:** No optimistic concurrency token on `SalesInvoiceEntity` or `FabricRollEntity`; the transaction wrapping protects atomicity *within* one request but not isolation *between* two.
- **Fix scope:** Add a `RowVersion`/`xmin`-based concurrency token to `SalesInvoiceEntity` and `FabricRollEntity`; change the repository update to a conditional `WHERE Status = @expected AND RowVersion = @expected`; add a unique index on `journal_entries(SourceType, SourceId)`.

---

### F-05 · Security / Segregation of Duties — 🔴 Critical
**No control prevents one person from creating and approving their own invoice**

Distinct permission codes exist (`sales.create`, `sales.approve`, `finance.receipt.post`, etc.), which is the right foundation — but no code anywhere compares `CreatedByUserId` to the approving user's identity, and only one role is seeded (`Administrator`), which implicitly holds every permission via `IsSuperAdminAsync` regardless of explicit grants.

Evidence: `SalesInvoiceHandlers.cs:395-396` (permission check only, no identity comparison), `DomainSpecifications.cs:7-27 InvoiceCanBeApprovedSpecification` (status/data only), `RemainingRepositories.cs:1066-1068 IsSuperAdminAsync` grants all permissions, `DatabaseSeeder.cs:94-100` (only "Administrator" role ever seeded).

- **Impact:** Any user with both create and approve permission — which is every seeded user today — can single-handedly create, approve, and post a sales invoice end to end, with real inventory and GL effect, with no second person in the loop.
- **Root cause:** The permission model was built to gate *actions*, not to enforce *separation between actors* — a maker-checker rule was never added.
- **Fix scope:** Add a check in `ApproveSalesInvoiceHandler` (and the payment-voucher-post handlers) rejecting approval when `approverUserId == invoice.CreatedByUserId` unless the approver also holds a distinct, logged override permission; seed at least an "Approver" role distinct from "Creator."

---

### F-06 · Security / Secrets — 🔴 Critical
**Database superuser password is hardcoded and committed to git**

`appsettings.json` (repo root), `ERPSystem.Api/appsettings.json`, and `ERPSystem.Infrastructure/appsettings.json` all carry `Username=postgres;Password=12345678` in plain text, and all three are tracked in git. History shows the password was changed once (from `postgres` to `12345678`) while remaining hardcoded and committed both times, across 79 commits spanning the repo's life.

Evidence: `appsettings.json:3`, `ERPSystem.Api/appsettings.json:3`, `ERPSystem.Infrastructure/appsettings.json:3`; `git log --follow -- appsettings.json` (`baeac38` → `bedcec7`).

- **Impact:** Anyone with read access to the repository has the `postgres` superuser password to whatever database that connection string points at.
- **Root cause:** Local-dev convenience connection string was never moved to environment variables/user-secrets and got committed alongside real application code.
- **Fix scope:** Rotate the password immediately; move all connection strings to environment variables or a secrets manager; keep only `appsettings.*.example` (placeholder values) in git, matching the pattern already correctly used for `appsettings.Local.json`.

---

### F-07 · Database Schema — 🟠 High
**Zero foreign-key constraints across the entire financial schema**

Excluding Expenses, Capital, and Opening Balances (which do have real FKs), every cross-table reference among `sales_invoices`, `sales_invoice_items`, `receipt_invoice_payments`, `purchase_invoices`, `purchase_orders`, `purchase_invoice_payments`, `journal_entries`, `journal_entry_lines`, `receipt_vouchers`, `payment_vouchers`, `"Accounts"`, `"FabricRolls"`, `customers`, and `suppliers` is a bare `uuid` column with no `REFERENCES` clause and no `HasOne()/HasForeignKey()` in the EF configuration.

Evidence: `grep "AddForeignKey"` across `ERPSystem.Infrastructure/Migrations/*.cs` — only Expenses/Capital/OpeningBalance modules have any; `grep "HasOne|HasForeignKey"` across `Configurations/*.cs` — same three modules only.

- **Impact:** The database itself cannot prevent an orphaned invoice line, a journal-entry line pointing at a deleted account, or a roll-detail row referencing a fabric roll that no longer exists. Every integrity guarantee for the core financial tables depends entirely on application code never making a mistake.
- **Root cause:** Early modules (Sales, Purchasing, Accounting core) were built with GUID references but FK constraints were never added; later modules (Expenses, Capital) established a better pattern that was not retrofitted.
- **Fix scope:** Add FK constraints table-by-table, starting with `journal_entry_lines.AccountId → "Accounts".Id` and `sales_invoice_items.SalesInvoiceId → sales_invoices.Id`; each addition needs a one-time orphan-row check first.

---

### F-08 · Purchasing / Payments — 🟠 High
**Overpayment handling is inconsistent, and silently lossy on the AP side**

On the AP side, `PurchaseInvoice.ApplyPayment` caps the amount applied at `Math.Min(amount, Remaining.Amount)` — the excess is discarded with no record — while the *supplier's* aggregate balance is still reduced by the full, uncapped voucher amount, implicitly netting the excess against other open invoices with no visible trail. On the AR side there is no per-invoice cap at all; overpayment only surfaces as a hard exception if it pushes the *customer's total* balance negative, and there is no deposit/prepayment/credit-balance concept anywhere in the codebase.

Evidence: `PurchasingEntities.cs:141-153 PurchaseInvoice.ApplyPayment` (Math.Min, excess dropped), `FinanceHandlers.cs:222` `supplierAgg.Supplier.ApplyPostedPayment(voucher.Amount)` (full amount, uncapped), `Money.cs:11-19` (throws on negative — the only AR-side guard).

- **Impact:** A supplier overpayment leaves no record of where the excess went; a customer overpayment either crashes the whole voucher post or silently succeeds with no accounting for the credit.
- **Root cause:** No prepayment/deposit liability account or entity was ever designed into either side of the model.
- **Fix scope:** Add a customer/supplier "unapplied credit" concept (a liability account plus a ledger of unapplied amounts) that both sides post to when payment exceeds what's owed.

---

### F-09 · Reporting — 🟠 High
**No Income Statement or Balance Sheet generation exists; no Assets = Liabilities + Equity check anywhere**

Only a Trial Balance and per-account Ledger are actually implemented, both summing directly from `JournalEntryLines`. `BalanceSheetTemplate` and `IncomeStatementTemplate` exist as registered document-engine classes but override nothing beyond their `Type` property — there is no query or generation logic behind either.

Evidence: `AccountingReportRepository.cs:49-105 GetTrialBalanceAsync` (implemented), `AccountingReportRepository.cs:107-168 GetAccountLedgerAsync` (implemented), `ExecutiveReportTemplates.cs:18-28 BalanceSheetTemplate / IncomeStatementTemplate` (empty).

- **Impact:** There is no system-generated P&L or balance sheet to hand an external auditor or the board — both would have to be built manually outside the system today, and there is no automated `Assets = Liabilities + Equity` self-check.
- **Root cause:** Reporting was scaffolded (templates registered, `DocumentType` enum values reserved) but never implemented past the Trial Balance.
- **Fix scope:** Build P&L (Revenue/COGS/Expense accounts grouped by type over a period) and Balance Sheet (Asset/Liability/Equity as-of-date balances) queries against the same `JournalEntryLines` source the Trial Balance already uses, plus an automated equality check surfaced on the reports page.

---

### F-10 · Documents / Concurrency — 🟠 High
**Invoice-numbering counter's "concurrency fix" migration is an empty no-op**

Invoice/voucher numbers come from a read-increment-write on a `DocumentCounters` row, nominally protected by a `RowVersion` concurrency token. That token is hard-coded to a fixed byte array at creation and is never reassigned before `SaveChangesAsync`, so it can never detect a conflicting write. The migration named to address this, `20260626235616_FixDocumentCounterRowVersion.cs`, has empty `Up`/`Down` bodies.

Evidence: `PostgreSqlNumberingService.cs:102-132 NextAsync` (RowVersion never reassigned), `20260626235616_FixDocumentCounterRowVersion.cs:11-20` (empty Up/Down), `SalesConfigurations.cs:19` (the actual backstop: unique index on CompanyId+InvoiceNumber).

- **Impact:** Two concurrent invoice-creation requests can be handed the same invoice-number text. A DB unique index turns the collision into a hard failure for the second request rather than silent corruption, but that request fails ungracefully with no automatic retry.
- **Root cause:** The fix migration was created but its body was never filled in.
- **Fix scope:** Implement a proper Postgres `xmin`-based concurrency check with retry-on-conflict in `NextAsync`, or replace the counter table with a Postgres `SEQUENCE`.

---

### F-11 · Database Schema — 🟡 Medium
**Purchasing-side monetary columns have no defined precision; Sales-side does**

`PurchaseInvoiceEntity.SubTotal/DiscountAmount/TaxAmount/PaidAmount/Remaining`, `PurchaseInvoiceItemEntity.QuantityMeters/UnitPrice/LineTotal`, and related payment/return line entities all have no `HasPrecision()` call — DDL confirms unconstrained Postgres `numeric`. The Sales-side equivalents are consistently `numeric(18,2)` (or `18,4` for lengths).

Evidence: `RemainingConfigurations.cs:230-239 PurchaseInvoiceConfiguration` (no precision calls), `AddPurchasesModule.cs:50-80,198-200,248-250` (type: "numeric", no precision/scale), `SalesConfigurations.cs:15-18,30-33` (contrast: numeric(18,2) throughout).

- **Impact:** Unconstrained `numeric` permits arbitrary-precision values to be stored, which can silently break equality comparisons used elsewhere for balance checks (e.g. `Remaining.Amount <= 0`).
- **Root cause:** The Purchasing module configuration class was written without copying the precision pattern already established on the Sales side.
- **Fix scope:** Add `HasPrecision(18,2)` (and `18,4` for meters) to every decimal property in the Purchasing configuration classes, then a migration to alter the column type.

---

### F-12 · Accounting — 🟡 Medium
**No bad-debt / write-off / doubtful-accounts mechanism exists**

A repo-wide search for `BadDebt`, `WriteOff`, and `AllowanceForDoubtfulAccounts` returns no code matches — only the audit checklist (`task.md`) itself.

- **Impact:** Uncollectible receivables have no defined path out of Accounts Receivable other than manual, undocumented intervention directly against the ledger.
- **Root cause:** Never built.
- **Fix scope:** Add a `WriteOffReceivableHandler` posting **Dr Bad Debt Expense (or Allowance) / Cr Accounts Receivable** through the existing `IIntegratedAccountingService`, gated by its own permission and requiring a reason.

---

### F-13 · Accounting / Capital — 🟡 Medium
**Capital Partners module is isolated from the General Ledger after its initial opening balance**

`CapitalTransaction` (contributions, withdrawals, loss distributions) computes running partner-capital balances entirely in-memory from its own table. `IIntegratedAccountingService` has no `PostCapitalTransaction`-style method, and `CapitalHandlers.cs` never references the accounting service at all. Only the very first capital injection, recorded through the Opening Balances module, ever touches a GL account.

Evidence: `CapitalEntities.cs:148-158 CurrentCapitalBase` (computed from CapitalTransaction rows, not GL), `IIntegratedAccountingService.cs:16-109` (no Capital* method), `OpeningBalanceEngine.cs:660-666` (only the opening-balance capital line is GL-integrated).

- **Impact:** The Trial Balance's partner-capital/equity account will drift from what the Capital module itself reports the moment any ordinary contribution, withdrawal, or profit distribution happens after go-live.
- **Root cause:** The Capital module was built as a self-contained ledger rather than a poster into the shared GL.
- **Fix scope:** Add GL posting calls from `CapitalHandlers.cs` for each transaction type, mirroring how Expenses/Sales/Purchasing already integrate.

---

### F-14 · Inventory Costing — 🟡 Medium
**Container batch/stock-movement unit cost excludes the fabric purchase price**

`CalculateContainerCostPerMeter` — used for `FabricBatchEntity.LandingCostPerMeter`, `StockMovementLineEntity.UnitCost/TotalValue`, and the inventory-activation GL posting amount — divides only `LandingCost.TotalSharedExpenses` by total meters, never adding `ChinaUnitPriceUsd`. The per-roll `FabricRoll.CostPerMeter` computed elsewhere in the same engine *does* correctly combine both, so the two cost figures the system produces for the same container disagree with each other.

Evidence: `InventoryEngine.cs:1297-1303 CalculateContainerCostPerMeter` (freight-only), `InventoryEngine.cs:96-99` (correct, per-roll figure).

- **Impact:** Stock-movement reports and the inventory-activation journal entry understate inventory value by the fabric price, compounding F-01's AP gap into the balance sheet's asset side.
- **Root cause:** Two independent cost calculations were written for the same concept at different points in the pipeline and never reconciled.
- **Fix scope:** Fix together with F-01 — once `ChinaInvoiceAmountUsd` is posted to AP, `CalculateContainerCostPerMeter` should be replaced by summing the already-correct per-roll `FabricRoll.CostPerMeter` values.

---

### F-15 · Database Schema — 🟡 Medium
**Soft-delete flag exists on every financial table but is enforced on barely a third of them**

`IsActive`/`IsArchived` are inherited by every entity, but the EF Core global query filter that actually excludes archived rows is configured on only 6 tables: `sales_invoices`, `sales_returns`, `customers`, `suppliers`, `warehouses`, `containers`. It is absent on `journal_entries`, `receipt_vouchers`, `payment_vouchers`, `purchase_invoices`, `cashboxes`, and `"FabricRolls"`.

Evidence: `grep "HasQueryFilter" ERPSystem.Infrastructure/Configurations/*.cs` — exactly 6 matches.

- **Impact:** If a journal entry, payment voucher, purchase invoice, or fabric roll is ever archived, it will still appear in ordinary reports and balance calculations that query those tables directly.
- **Root cause:** The query filter was added per-module as each module's screens needed it, not systematically.
- **Fix scope:** Add the same `HasQueryFilter(x => x.IsActive && !x.IsArchived)` to the remaining six configuration classes.

---

### F-16 · Sales — 🟡 Medium
**No reversal/unapprove path exists for an approved sales invoice**

`SalesInvoiceAggregate.Cancel()` explicitly refuses to cancel Approved/Printed/Delivered invoices ("Posted invoices must be reversed, not cancelled"), but no `Reverse()`/`Unapprove()` method, handler, or accounting-service call exists anywhere. The `ReversedByJournalEntryId` column exists on the entity and is round-tripped by the persistence mapper but is never assigned by any code path.

Evidence: `SalesInvoiceAggregate.cs:273-280 Cancel()` (refuses for posted invoices), `SalesInvoiceAggregate.cs:41 ReversedByJournalEntryId` (never set — grepped whole repo).

- **Impact:** An erroneously-approved invoice has no built-in correction path other than a Sales Return, which is a forward-only new document, not an undo.
- **Root cause:** The data model anticipated reversal (the column exists) but the workflow was never implemented.
- **Fix scope:** Either implement a real `Reverse()` that posts a mirrored contra journal entry and restores inventory, or remove the dead column and formally document Sales Returns as the only correction mechanism.

---

### F-17 · Inventory — 🟡 Medium
**No database check constraint prevents negative remaining fabric length**

`FabricRoll.DeductLength` guards against over-deduction, but that guard lives on the domain class, not on `FabricRollEntity` (the one EF actually persists), and there is no `CHECK (RemainingLengthMeters >= 0)` constraint in any migration. Combined with F-04's lack of a concurrency token, a race between two concurrent deductions on the same roll is possible.

Evidence: `CatalogEntities.cs:144-154` (domain guard, not enforced at persistence), `grep "CHECK|AddCheckConstraint" ERPSystem.Infrastructure/Migrations/*.cs` — 0 results.

- **Impact:** A negative remaining length is possible under concurrent load with nothing in the database to reject it.
- **Root cause:** Same missing-concurrency-token root cause as F-04, compounded by no DB-level CHECK as a last resort.
- **Fix scope:** Add `CHECK ("RemainingLengthMeters" >= 0)` to `"FabricRolls"` as a cheap backstop independent of the concurrency-token fix in F-04.

---

### F-18 · Payments — 🟡 Medium
**No idempotency key on receipt/payment vouchers — duplicate entry is unguarded**

`ReceiptVoucherEntity`/`PaymentVoucherEntity` have no reference/check-number/idempotency field. The only uniqueness enforced is the system-generated voucher number, which is freshly minted every call — so a user (or a retried network request) re-entering the same real-world payment simply produces two vouchers with two different numbers, both posting successfully.

Evidence: `FinanceEntities.cs:3-27` (no reference/idempotency column on either voucher type).

- **Impact:** Duplicate cash-receipt or supplier-payment entry — accidental or otherwise — is invisible to the system.
- **Root cause:** No idempotency key was designed into the voucher creation command.
- **Fix scope:** Add an optional client-supplied idempotency key honored server-side, plus a soft duplicate-detection check (same customer/supplier + amount + date within N minutes) surfaced as a warning before posting.

---

**4 further low/informational observations** (stale EF model snapshot, JWT dev-secret placeholder, missing unique index on `Account.Code`, PascalCase/no-schema outliers on `"Accounts"`/`"FabricRolls"`) are in §11 for completeness but don't change the audit's overall rating.

---

## 5. Database Schema Analysis

All facts below are drawn from the persistence-layer `*Entity` classes, their `IEntityTypeConfiguration<T>` classes, and the generated migrations — the layer EF Core actually maps to PostgreSQL (see §3.1).

| Table | PK | Unique key | Decimal precision | Soft-delete filter | Concurrency token |
|---|---|---|---|---|---|
| `sales_invoices` | uuid | (CompanyId, InvoiceNumber) | numeric(18,2) | ✅ Enforced | ❌ None |
| `sales_invoice_items` | uuid | (SalesInvoiceId, LineNumber) | numeric(18,2) | ⚠️ Column only | ❌ None |
| `receipt_invoice_payments` | uuid | none (non-unique index) | numeric(18,2) | ⚠️ Column only | ❌ None |
| `purchase_invoices` | uuid | (CompanyId, InvoiceNumber) | ⚠️ Unconstrained | ⚠️ Column only | ❌ None |
| `purchase_invoice_payments` | uuid | none (non-unique index) | ⚠️ Unconstrained | ⚠️ Column only | ❌ None |
| `journal_entries` / `journal_entry_lines` | uuid | none | numeric(18,2) | ⚠️ Column only | ❌ None |
| `receipt_vouchers` / `payment_vouchers` | uuid | (CompanyId, VoucherNumber) | numeric(18,2) | ⚠️ Column only | ❌ None |
| `"FabricRolls"` | uuid | none | numeric(18,4) | ⚠️ Column only | ❌ None |
| `customers` / `suppliers` | uuid | (CompanyId, Code) | numeric(18,2) | ✅ Enforced | ❌ None |
| `"Accounts"` | uuid | ❌ None | unconfigured (no config class at all) | ❌ None | ❌ None |
| `document_counters` | uuid | (BranchId, DocumentType) | n/a | n/a | ⚠️ Present but broken (F-10) |

"Column only" = `IsActive`/`IsArchived` exist on the row but no EF global query filter excludes them from normal queries (see F-15).

**5.1 Referential integrity.** Only three modules have real foreign keys in the entire schema: **Expenses** (`CostCenterId → finance.cost_centers`, `SetNull`), **Capital Partners** (participations/bank accounts/transactions → partners), and **Opening Balances** (lines/events → documents, `CASCADE`). Every other relationship discussed in this report — `CustomerId`, `SupplierId`, `AccountId`, `SalesInvoiceId`, `FabricRollId`, `PurchaseOrderId` — is an unconstrained `uuid` column (F-07). Practically, this means there is no cascade-delete risk from the schema's own definitions (there's nothing to cascade), but also zero DB-level protection against orphaned rows.

**5.2 Missing entities the task brief assumed exist:**
- **Goods Receipt / GRN** — does not exist anywhere in the codebase (searched Domain, Infrastructure, Core/Purchases; zero hits outside `task.md` itself).
- **Customer/Supplier price list** — does not exist as a reusable entity; pricing is either read from container landing-cost stock or overridden per invoice line.
- **Deposit / prepayment / credit-balance account** — does not exist (F-08).
- **PaymentSchedule / Installment tied to Sales or Purchase invoices** — does not exist; the only installment concept in the codebase belongs to the unrelated Expenses module.
- **FinancialAuditLog (field-level change log)** — a generic `audit_logs` table with `OldValuesJson`/`NewValuesJson` does exist, but it is called manually from only three narrow spots (container audit, customer reconciliation, sales price-override) — not from receipt/payment/purchase/journal posting.

---

## 6. Payment Journey Documentation

### 6.1 Sales — cash or credit sale (same code path for both)

ERPSystem doesn't branch sales logic by payment type the way task.md's Scenario A/B assumed — cash vs. credit only changes whether a `PartialPaymentAmount` was declared up front; the approval posting is identical either way.

```
1. Draft            CreateSalesInvoiceDraftHandler
2. Detail rolls      CompleteWarehouseDetailingHandler — meters entered per roll
3. Approve           Credit-limit check → deduct inventory → post GL, one transaction
4. Receipt           PostReceiptVoucherHandler — separate transaction (F-04-adjacent)
```

**Numbers, traced through the real code:** a 300m sale at a $4.20/m unit price with a $50 per-line discount override:

```
SubTotal        = 300 x 4.20                      = 1,260.00
DiscountTotal    (SalesDiscounts, contra-revenue)  =    50.00
TaxTotal         (F-02: never computed)            =     0.00
GrandTotal       = SubTotal + TaxTotal - Discount  = 1,210.00

Journal on Approve (IntegratedAccountingService.cs:68-100):
  Dr Accounts Receivable          1,210.00
  Cr Sales Revenue                1,260.00   (= GrandTotal + line discount)
  Dr Sales Discounts                 50.00
  Dr Cost of Goods Sold        (roll.CostPerMeter x 300)
  Cr Inventory Asset           (roll.CostPerMeter x 300)
```

Debits and credits above net to zero by construction (`AccountingAggregate.ValidateBalanced`, tolerance 0.01) — this invoice, in isolation, is arithmetically sound. It is simply under-taxed (F-02) and vulnerable to the double-post race in F-04.

### 6.2 Purchasing — conventional PurchaseInvoice path

```
PO (optional)  --->  [NO RECEIPT STEP — no GRN entity exists, F-03]  --->  Invoice = receipt (PostPurchaseInvoiceHandler
                                                                             creates FabricRolls AND posts GL in one call)  --->  Payment (PurchaseInvoiceId optional, F-03)
```

```
PostPurchaseInvoiceAsync (IntegratedAccountingService.cs:140-178):
  Dr Inventory Asset / Expense     TotalAmount
  Cr Accounts Payable              TotalAmount
```

### 6.3 Purchasing — China container import (the dominant channel)

```
1. Draft container --> 2. Landing cost entered --> 3. Approve (posts freight-only GL entry, F-01)
   --> [FABRIC COST NEVER POSTED TO AP, F-01] --> 4. Move to warehouse (FabricRolls created with
       correct per-roll cost, but batch/movement cost is freight-only, F-14)
```

```
On Approve (freight/customs/clearance ONLY):
  Dr Landing-Cost-Clearing        TotalSharedExpenses
  Cr Accounts Payable             TotalSharedExpenses

On warehouse transfer:
  Dr Inventory Asset              inventoryValue   (freight-only per-meter, F-14)
  Cr Landing-Cost-Clearing        inventoryValue

MISSING — never posted anywhere:
  Dr Inventory-in-Transit         ChinaInvoiceAmountUsd x ExchangeRateToLocalCurrency
  Cr Accounts Payable             ChinaInvoiceAmountUsd x ExchangeRateToLocalCurrency
```

### 6.4 Sales Returns / credit memo (implemented, and correctly balanced)

Unlike most other flows in this audit, Sales Returns are fully wired end to end: inventory is restored, AR is reduced, and the GL entry is a clean 4-line, balanced reversal — worth using as the template when fixing the gaps above.

```
PostSalesReturnAsync (IntegratedAccountingService.cs:180-213):
  Dr Sales Revenue                revenueReversal
  Cr Accounts Receivable          revenueReversal
  Dr Inventory Asset              cogsReversalAmount   (if any)
  Cr Cost of Goods Sold           cogsReversalAmount   (if any)
```

---

## 7. Reconciliation Queries — rewritten against the real schema

task.md's six verification queries assumed table/column names (`SalesInvoice.TotalAmount`, a single `Payment` table, etc.) that don't exist in this codebase. Below are the same checks rewritten against the actual PostgreSQL schema discovered during this audit. Confirm exact schema prefixes (e.g. `sales.`, `finance.`) against your live database before running, since some tables were created without an explicit schema (`"Accounts"`, `"FabricRolls"`).

**7.1 Invoice total integrity**
```sql
SELECT si."Id", si."InvoiceNumber", si."GrandTotal", si."SubTotal", si."TaxTotal", si."DiscountTotal",
       SUM(sii."LineTotal") AS calculated_subtotal,
       CASE WHEN si."GrandTotal" = (SUM(sii."LineTotal") + si."TaxTotal" - si."DiscountTotal")
            THEN 'OK' ELSE 'MISMATCH' END AS verification
FROM sales_invoices si
LEFT JOIN sales_invoice_items sii ON sii."SalesInvoiceId" = si."Id"
GROUP BY si."Id";
-- Expected finding given F-02: TaxTotal is 0 on every row without exception.
```

**7.2 Inventory reconciliation (FabricRolls)**
```sql
SELECT fr."Id", fr."RollNumber", fr."LengthMeters", fr."RemainingLengthMeters",
       (fr."LengthMeters" - fr."RemainingLengthMeters") AS implied_issued,
       CASE WHEN fr."RemainingLengthMeters" < 0 THEN 'NEGATIVE - CRITICAL' ELSE 'OK' END AS check1
FROM "FabricRolls" fr
WHERE fr."RemainingLengthMeters" < 0 OR fr."RemainingLengthMeters" > fr."LengthMeters";
-- Given no CHECK constraint (F-17) and no concurrency token (F-04), run this weekly, not just at audit time.
```

**7.3 AR / AP outstanding balance vs. GL**
```sql
-- AR: derived balance (application logic) vs GL AccountsReceivable balance
WITH ar_derived AS (
  SELECT si."CustomerId", SUM(si."GrandTotal") AS invoiced,
         COALESCE(SUM(rip."Amount"), 0) AS collected
  FROM sales_invoices si
  LEFT JOIN receipt_invoice_payments rip ON rip."SalesInvoiceId" = si."Id"
  WHERE si."Status" >= 4 -- Approved
  GROUP BY si."CustomerId"
)
SELECT SUM(invoiced - collected) AS ar_derived_total FROM ar_derived;

SELECT SUM(jel."Debit" - jel."Credit") AS ar_gl_balance
FROM journal_entry_lines jel
JOIN journal_entries je ON je."Id" = jel."JournalEntryId"
WHERE jel."AccountId" = '<AccountingAccountIds.AccountsReceivable>' AND je."Status" = 2 /* Posted */;
-- Compare the two totals; any gap indicates a posting that bypassed PostSalesInvoiceApprovalAsync/PostReceiptVoucherAsync.
```

**7.4 Journal entry balance verification (should always pass — verifies no bypass of the domain guard)**
```sql
SELECT je."Id", je."EntryNumber",
       SUM(jel."Debit") AS total_debit, SUM(jel."Credit") AS total_credit
FROM journal_entries je
JOIN journal_entry_lines jel ON jel."JournalEntryId" = je."Id"
GROUP BY je."Id", je."EntryNumber"
HAVING ABS(SUM(jel."Debit") - SUM(jel."Credit")) > 0.01;
-- Any row returned here means AccountingAggregate.ValidateBalanced() was bypassed
-- (e.g. via the two [Obsolete] PostCustomerOpeningBalanceAsync/PostSupplierOpeningBalanceAsync
-- methods noted in F-13's evidence, or a direct DB write) -- treat as CRITICAL if non-empty.
```

**7.5 Orphaned / unlinked supplier payments (F-03)**
```sql
SELECT pv."Id", pv."VoucherNumber", pv."Amount", pv."SupplierId"
FROM payment_vouchers pv
WHERE NOT EXISTS (
  SELECT 1 FROM purchase_invoice_payments pip WHERE pip."PaymentVoucherId" = pv."Id"
);
-- Every row here is a payment with no link to any purchase invoice at all — expected to be non-trivial given F-03.
```

**7.6 Duplicate invoice numbers (should be zero — verifies the unique index actually holds)**
```sql
SELECT "CompanyId", "InvoiceNumber", COUNT(*) FROM sales_invoices
GROUP BY "CompanyId", "InvoiceNumber" HAVING COUNT(*) > 1;
SELECT "CompanyId", "InvoiceNumber", COUNT(*) FROM purchase_invoices
GROUP BY "CompanyId", "InvoiceNumber" HAVING COUNT(*) > 1;
```

**7.7 Segregation-of-duties violations (F-05)**
```sql
SELECT "Id", "InvoiceNumber", "CreatedByUserId", "ApprovedByUserId"
FROM sales_invoices
WHERE "CreatedByUserId" = "ApprovedByUserId" AND "Status" >= 4; -- Approved
```

**7.8 Archived-but-still-visible rows (F-15)**
```sql
SELECT 'journal_entries' AS tbl, COUNT(*) FROM journal_entries WHERE "IsActive" = false OR "IsArchived" = true
UNION ALL SELECT 'receipt_vouchers', COUNT(*) FROM receipt_vouchers WHERE "IsActive" = false OR "IsArchived" = true
UNION ALL SELECT 'payment_vouchers', COUNT(*) FROM payment_vouchers WHERE "IsActive" = false OR "IsArchived" = true
UNION ALL SELECT 'purchase_invoices', COUNT(*) FROM purchase_invoices WHERE "IsActive" = false OR "IsArchived" = true
UNION ALL SELECT 'FabricRolls', COUNT(*) FROM "FabricRolls" WHERE "IsActive" = false OR "IsArchived" = true;
-- Any non-zero count is a row silently leaking into normal queries/reports today.
```

---

## 8. Fraud Risk Matrix

| Risk | Current control | Gap | Rating |
|---|---|---|---|
| Same person creates & approves a fictitious sale | Permission codes exist | No identity comparison; single seeded role holds both (F-05) | 5 / 5 |
| Supplier payment with no invoice/PO/receipt trail | Supplier balance can't go negative | `PurchaseInvoiceId` optional; no GRN exists at all (F-03) | 5 / 5 |
| Double-approval / double-posting under concurrency | In-transaction atomicity | No cross-request isolation, no RowVersion (F-04) | 4 / 5 |
| Overpayment used to obscure a kickback / skim | Negative-balance guard (AR only) | AP excess silently capped & untracked (F-08) | 4 / 5 |
| Duplicate real-world payment entered twice | Unique system-generated voucher number | No idempotency key / reference field (F-18) | 3 / 5 |
| Financial DB accessed by unauthorized party | None beyond OS/network access | Superuser password hardcoded in git (F-06) | 5 / 5 |
| Price override used to under-bill a favored customer | Override captured & logged to audit_logs on approval | Server never validates `OriginalUnitPrice` against any catalog — client-supplied and self-reported | 3 / 5 |
| Orphaned/tampered GL line via missing FK | Application-layer account-existence check only | No DB-level FK anywhere in core financial schema (F-07) | 3 / 5 |

---

## 9. Compliance Assessment

### 9.1 IFRS / GAAP
- **Revenue recognition:** full `GrandTotal` is recognized as revenue at invoice *approval*, uniformly, regardless of declared payment type or how much cash was actually collected up front. Consistent with an accrual-basis, delivery/invoice-triggered recognition policy — reasonable under IFRS *if delivery has in fact occurred by approval time*, which was outside this audit's ability to confirm from code alone.
- **Accrual vs. cash basis:** accrual — AR/AP are recognized independently of cash timing. Consistent with GAAP's matching principle for the postings that do exist.
- **Matching principle (COGS):** correctly implemented on the sales side — COGS is posted in the same journal entry as the revenue it matches. **Not** correctly implemented on the purchasing side — F-01/F-14 mean the cost basis feeding that COGS calculation is itself short the fabric purchase price.
- **Allowance for doubtful accounts:** not implemented (F-12) — a gap against both IFRS 9 (expected credit loss) and standard GAAP practice.
- **Foreign-exchange gain/loss:** exchange rates are captured for China-container costing but there is no realized/unrealized FX gain-or-loss posting anywhere, and AR/AP/payments are effectively single-currency (USD) with no revaluation logic.

### 9.2 Tax compliance
Sales tax is not computed at all (F-02) — any VAT/sales-tax return prepared from this system's data would need to be built entirely outside it, with no way to reconcile against a `Sales_Tax_Payable` GL balance because that balance doesn't exist. This is the single largest tax-compliance exposure in the system.

### 9.3 Internal control framework (COSO, quick read)

| Component | Assessment |
|---|---|
| Control environment | Permission model exists but is under-utilized (single super-role in practice) — see F-05 |
| Risk assessment | No evidence of a formalized fraud-risk review process in-code (expected; this is organizational, not code) |
| Control activities | Strong where it exists (balanced-journal guard, transactional approval) — but not applied uniformly (F-03, F-04, F-08) |
| Information & communication | Trial Balance/Ledger reporting works; P&L/Balance Sheet do not exist yet (F-09) |
| Monitoring | Generic `audit_logs` table exists but is invoked from only 3 of the many financial state transitions |

---

## 10. Recommendations & Roadmap

**Priority 1 — fix before the next audit cycle:** Post the China fabric purchase cost to AP (F-01, together with F-14); implement sales tax calculation and posting (F-02); require invoice linkage on goods-related supplier payments and add a concurrency token to stop double-approval (F-03, F-04); add a maker-checker rule to invoice approval (F-05); rotate and remove the hardcoded DB password from git (F-06).

**Priority 2 — next quarter:** Add foreign-key constraints across the core financial schema (F-07); design an unapplied-credit/deposit mechanism for overpayments on both AR and AP (F-08); build Income Statement and Balance Sheet generation with an automated A=L+E check (F-09); fix the invoice-numbering concurrency token or replace it with a Postgres sequence (F-10); bring Purchasing-side decimal precision in line with Sales (F-11).

**Priority 3 — planned enhancement:** Bad-debt/write-off workflow (F-12); wire Capital Partners transactions into the GL (F-13); extend the soft-delete query filter to the remaining six tables (F-15); implement invoice reversal or formally retire the dead `ReversedByJournalEntryId` column (F-16); add a DB check constraint against negative fabric-roll length (F-17); add an idempotency key to payment vouchers (F-18).

---

## 11. Appendix: Additional Observations & Limitations

**11.1 Additional low/informational observations**
- `ErpDbContextModelSnapshot.cs` is stale — missing entire modules (Purchase Orders, Capital, Opening Balances) added via raw-SQL migrations that EF's snapshot tooling doesn't track. Doesn't affect the live schema, but risks incorrect output from a future `dotnet ef migrations add`.
- The dev JWT signing secret in `appsettings.Development.json` is an obvious placeholder string, not a real leaked secret — but confirm it's actually overridden via `JWT_SECRET` in any real deployment.
- `Account` (chart of accounts) has no `IEntityTypeConfiguration` class at all — no `HasMaxLength`, no unique index on `Code`, unlike every other master table in the schema.
- `"Accounts"` and `"FabricRolls"` are PascalCase, schema-less table names — an inconsistency against every other table's `snake_case`, schema-qualified convention (e.g. `finance.cost_centers`). Cosmetic, but worth normalizing before it causes a tooling surprise.

**11.2 Limitations of this audit.** This was a static, code-only review (per your choice in scoping — see §2): no production or restored-production data was queried, so every quantitative claim above ("understated by the entire fabric cost," "every invoice under-taxed") is a structural/logical claim about what the code does on *every* row, not a measured dollar figure from your actual ledger. The reconciliation queries in §7 are ready to run the moment you're comfortable granting access to real data, and will convert every finding above into an exact number.

This review also did not execute the application, run its test suite, or examine the WPF/WinForms UI layer beyond the specific controls cited (e.g. `NewSalesInvoiceControl.xaml.cs`) — UI-level validation that duplicates or diverges from the server-side rules documented here was out of scope.

---

*Prepared by static analysis of the ERPSystem repository, 2026-07-08. Every citation above is a file path and line range in the codebase at time of review — re-verify against current `HEAD` before acting, since line numbers drift with unrelated commits.*
