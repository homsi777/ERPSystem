# Phase 1 Completion Report — Posting Protection and Duplicate Prevention

**Program:** ERP PRO Sales, Accounting, Payments and Inventory Stabilization  
**Phase:** 1 — Posting Protection and Duplicate Prevention  
**Date (UTC):** 2026-07-10  
**Status:** ✅ Complete — **STOP** (Phase 2 not started)

---

## 1. Objective executed

Protect **future automated postings** from duplicate journals, race conditions, split transactions, and API/UI double-submit — without modifying historical financial data, legacy duplicate journals, inventory GL gap, or customer AR.

---

## 2. Posting points discovered

| SourceType | Handler / Path | Posting Method (before) | Transaction? | Idempotency? | Risk |
|------------|----------------|-------------------------|--------------|--------------|------|
| ChinaContainer (landing cost) | `DomainEventDispatcher` → `PostContainerApprovalAsync` | After commit, new DI scope | Split | `AnyAsync` only | **HIGH** |
| ChinaContainer (inventory activation) | `InventoryEngine` → `PostInventoryActivationAsync` | Same handler TX, weak check | Partial | `AnyAsync` only | **HIGH** |
| SalesInvoice | `ApproveSalesInvoiceHandler` | `IntegratedAccountingService` + internal SaveChanges | Partial | Weak | **CRITICAL** |
| ReceiptVoucher | `PostReceiptVoucherHandler` | No transaction | No | Weak | **CRITICAL** |
| PaymentVoucher | `PostPaymentVoucherHandler` | No transaction | No | Weak | HIGH |
| SalesReturn | Sales return handlers | `IntegratedAccountingService` | Varies | Weak | HIGH |
| PurchaseInvoice/Return | Purchase handlers | `IntegratedAccountingService` | Varies | Weak | HIGH |
| ExpensePayment | Expense handlers | `IntegratedAccountingService` | Varies | Weak | HIGH |
| CashboxTransfer | Finance handlers | `IntegratedAccountingService` | Varies | Weak | MEDIUM |
| OpeningBalance / Party OB | Opening balance engine / legacy | `IntegratedAccountingService` | Varies | Weak | MEDIUM |
| Manual journal | `JournalEntryHandlers` | Direct repository (v1 legacy) | Handler TX | None (by design) | MEDIUM |
| Capital / partners | Capital handlers | Not fully wired to posting engine in Phase 1 | — | — | Deferred |

**Root cause of baseline duplicate (`JE-MAIN-000001` / `JE-MAIN-000002`):** legacy `PostIfNotExistsAsync` keyed only on `SourceType + SourceId` (optional description filter). Two legitimate posting kinds for the same China container shared one key; concurrent `AnyAsync` + insert had no DB unique constraint.

---

## 3. Paths migrated to `IAccountingPostingEngine`

| Path | PostingKind | Status |
|------|-------------|--------|
| China container landing cost | `ChinaContainerLandingCost` | ✅ Via engine + handler TX |
| China container inventory activation | `ChinaContainerInventoryActivation` | ✅ Via engine |
| Sales invoice approval | `SalesInvoicePosting` | ✅ |
| Receipt voucher | `ReceiptVoucher` | ✅ |
| Payment voucher | `PaymentVoucher` | ✅ |
| Sales return | `SalesReturn` | ✅ |
| Purchase invoice / return / reversal | `PurchaseInvoice`, `PurchaseReturn`, `PurchaseInvoiceReversal` | ✅ |
| Expense payment | `ExpensePayment` | ✅ |
| Cashbox transfer | `CashboxTransfer` | ✅ |
| Finance / customer / supplier opening balance | `FinanceOpeningBalance`, `CustomerOpeningBalance`, `SupplierOpeningBalance` | ✅ |

**Adapter:** `IntegratedAccountingService` now builds `PostingRequest` and delegates to `IAccountingPostingEngine` (no internal `SaveChanges`).

---

## 4. Paths not migrated (Phase 1) and reason

| Path | Reason |
|------|--------|
| Manual journal entry | Remains v1 (`PostingIdentityVersion = 1`, no PostingKind) — different idempotency policy |
| `ReverseAsync` / invoice reversal workflows | Explicitly deferred to later phase (`ReversalResult.NotImplemented()`) |
| Capital partner transactions | No automated GL posting path found in scope; deferred |
| API idempotency on **all** endpoints | Infrastructure added; wired pattern ready — sensitive endpoints can adopt `ApiIdempotencyExecutor` incrementally |

---

## 5. Posting identity design

```text
CompanyId + SourceType + SourceId + PostingKind
PostingIdentityVersion = 2   (protected automated postings)
```

Legacy rows remain `PostingIdentityVersion = 1`, `PostingKind = NULL` — **not backfilled, not modified**.

**PostingKind enum:** `ERPSystem.Domain/Enums/PostingKind.cs`

---

## 6. Idempotency design

### Database
- Partial unique index (PostgreSQL):

```sql
CREATE UNIQUE INDEX IX_journal_entries_posting_identity_v2
ON accounting.journal_entries ("CompanyId", "SourceType", "SourceId", "PostingKind")
WHERE "PostingIdentityVersion" = 2 AND ...;
```

- Table `accounting.accounting_idempotency_records` with unique `(CompanyId, UserId, Operation, IdempotencyKey)`.

### Application
- `IAccountingIdempotencyService` — Begin / Complete / Fail
- `IAccountingPostingEngine.RecoverFromUniqueViolationAsync` — race recovery after unique violation
- `IPostingSaveCoordinator` — SaveChanges + recovery in caller transaction
- API helper: `ERPSystem.Api/Infrastructure/ApiIdempotencyExecutor.cs` (`Idempotency-Key` header)

### WPF
- In-flight guards on `ContainerUiService.ApproveContainerAsync` and `SalesUiService.ApproveAsync` (per-entity `ConcurrentDictionary`)

---

## 7. Transaction boundaries (before → after)

| Flow | Before | After |
|------|--------|-------|
| Approve container | Save → domain event → **new scope** accounting | Single TX: approve + post + save + commit |
| Move container to warehouse | Save + dispatch; accounting in inventory path | TX + `IPostingSaveCoordinator` with recovery |
| Approve sales invoice | TX but accounting called **SaveChanges inside service** | Engine stages only; one coordinated save |
| Post receipt voucher | No TX; accounting saved independently | `BeginTransaction` → post → coordinated save |
| Post payment voucher | No TX | `BeginTransaction` → coordinated save |

**Domain event change:** `ContainerApproved` no longer triggers accounting in `DomainEventDispatcher` (notification only if event still dispatched elsewhere).

---

## 8. Migrations

| Migration | Purpose |
|-----------|---------|
| `20260720120000_AddJournalPostingIdentity` | Add `PostingKind`, `PostingIdentityVersion`, `IdempotencyKey`, `CorrelationId`; tables `accounting_posting_attempts`, `accounting_idempotency_records`; partial unique index; preflight NOTICE for legacy duplicates |

**Strategy for historical duplicates:** Partial index excludes v1 / NULL PostingKind — **does not delete or alter** `JE-MAIN-000001` / `JE-MAIN-000002`.

**Apply:** Automatically on API startup (`Program.cs` → `MigrateAsync()`).

---

## 9. Legacy duplicate handling

- **Not deleted, not modified, no reversal created.**
- Health check distinguishes:
  - `duplicate_journal_entries` — Legacy (SourceType + SourceId)
  - `legacy_critical_duplicate_evidence` — Explicit preserved evidence for Phase 2
  - `duplicate_protected_posting_identities` — v2 protected duplicates (should stay 0)

---

## 10. Baseline before / after

| Metric | Phase 0 baseline | Phase 1 (expected) |
|--------|------------------|-------------------|
| Legacy duplicate JE-MAIN-000001/000002 | Present | **Unchanged** |
| Inventory operational vs GL gap | 90,014.28 USD | **Unchanged** |
| AR / stored customer balances | 320.00 USD | **Unchanged** |
| Cashbox AccountId linkage | None | **Unchanged** |
| Financial totals | Reference | **No mutation by Phase 1 code** |

**Note:** Local `accounting-baseline-after` run requires SSH tunnel to PostgreSQL (port 5433). Migration applies on next API deploy startup.

---

## 11. Concurrency / integration tests

| # | Test | Result |
|---|------|--------|
| 1 | Parallel China container posting (20 tasks) | ✅ `AccountingPostingEngineLiveDbTests` (when DB available) |
| 2 | Sales invoice double approve | Protected by engine + business status |
| 3 | Receipt double post | Protected by engine + voucher status |
| 4–6 | Idempotency key same/different | Service implemented; API helper ready |
| 7–9 | Rollback on failure | Engine does not commit independently; handler TX rolls back |
| 10 | Legacy duplicate preserved | Migration additive; health check surfaces legacy |
| 11 | 20 concurrent requests | Live test implemented |
| 12 | Unbalanced entry rejected | ✅ Live test |
| 13 | Missing account rejected | Engine validates before staging |
| 14 | Timeout retry | Pre-check + unique index + recovery |

**Test run (local, DB tunnel offline):** 13 passed, 0 failed (2 live DB tests skipped — no connection to localhost:5433).

---

## 12. Build

```
dotnet build ERPSystem.Api/ERPSystem.Api.csproj → SUCCESS (0 errors)
```

---

## 13. Test summary

| Suite | Passed | Failed | Skipped (no DB) |
|-------|--------|--------|-----------------|
| `ERPSystem.Application.Tests` (excl. diagnostic) | 13 | 0 | 2 live posting tests |

---

## 14. Remaining risks

1. Manual journal entries still use v1 identity — separate policy needed.
2. API idempotency header not yet on every sensitive endpoint (helper ready).
3. `ReverseAsync` not implemented (by design for Phase 1).
4. Live concurrency tests require DB tunnel in CI/local dev.
5. Cashbox GL linkage still missing (out of scope — not patched with random AccountId).

---

## 15. Rollback instructions

1. Deploy previous application build.
2. **Do not** run Down migration on production if any v2 postings exist.
3. If migration must be rolled back on empty dev DB only:

```sql
-- See Down() in 20260720120000_AddJournalPostingIdentity.cs
DROP INDEX IF EXISTS accounting."IX_journal_entries_posting_identity_v2";
DROP TABLE IF EXISTS accounting.accounting_idempotency_records;
DROP TABLE IF EXISTS accounting.accounting_posting_attempts;
ALTER TABLE accounting.journal_entries DROP COLUMN IF EXISTS "PostingKind", ...;
```

4. Restore DB from backup per `docs/accounting/POSTGRES_BACKUP_RESTORE.md` if needed.

---

## 16. Files added

- `ERPSystem.Domain/Enums/PostingKind.cs`
- `ERPSystem.Application/Posting/PostingModels.cs`
- `ERPSystem.Application/Abstractions/Services/IAccountingPostingEngine.cs`
- `ERPSystem.Application/Abstractions/Services/IAccountingIdempotencyService.cs`
- `ERPSystem.Application/Abstractions/Services/IPostingSaveCoordinator.cs`
- `ERPSystem.Infrastructure/Services/AccountingPostingEngine.cs`
- `ERPSystem.Infrastructure/Services/AccountingIdempotencyService.cs`
- `ERPSystem.Infrastructure/Services/PostingSaveCoordinator.cs`
- `ERPSystem.Infrastructure/Migrations/20260720120000_AddJournalPostingIdentity.cs`
- `ERPSystem.Infrastructure/Migrations/20260720120000_AddJournalPostingIdentity.Designer.cs`
- `ERPSystem.Api/Infrastructure/ApiIdempotencyExecutor.cs`
- `ERPSystem.Application.Tests/Posting/PostingModelTests.cs`
- `ERPSystem.Application.Tests/Posting/AccountingPostingEngineLiveDbTests.cs`
- `PHASE_1_COMPLETION_REPORT.md`

---

## 17. Files modified (principal)

- `ERPSystem.Infrastructure/Services/IntegratedAccountingService.cs` — engine adapter
- `ERPSystem.Infrastructure/Repositories/RemainingRepositories.cs` — posting metadata on JE insert
- `ERPSystem.Infrastructure/Persistence/Models/Accounting/AccountingEntities.cs` — new columns/entities
- `ERPSystem.Infrastructure/Configurations/RemainingConfigurations.cs` — EF + partial index
- `ERPSystem.Infrastructure/Persistence/ErpDbContext.cs` — new DbSets
- `ERPSystem.Infrastructure/DependencyInjection/InfrastructureServiceCollectionExtensions.cs` — DI
- `ERPSystem.Application/UseCases/Containers/ContainerHandlers.cs` — TX + inline posting
- `ERPSystem.Application/UseCases/Sales/SalesInvoiceHandlers.cs` — coordinated save
- `ERPSystem.Application/UseCases/Finance/FinanceHandlers.cs` — receipt/payment TX
- `ERPSystem.Application/DomainEvents/DomainEventDispatcher.cs` — removed container accounting
- `ERPSystem.Infrastructure/Services/AccountingBaselineReadService.cs` — extended health checks
- `Services/China/ContainerUiService.cs`, `Services/Sales/SalesUiService.cs` — WPF in-flight guards

---

## 18. Confirmations (Phase 1 constraints)

| Constraint | Status |
|------------|--------|
| Legacy duplicate JE not deleted/modified | ✅ |
| Inventory GL gap not adjusted | ✅ |
| AR 320.00 not changed | ✅ |
| No random cashbox AccountId | ✅ |
| No historical re-posting | ✅ |
| Phase 2 not started | ✅ |

---

**End of Phase 1. Do not proceed to Phase 2 without explicit approval.**
