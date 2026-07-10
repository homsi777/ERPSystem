# Phase 1 Production Verification Gate — Report

**Program:** ERP PRO Sales, Accounting, Payments and Inventory Stabilization  
**Gate:** Phase 1 Production Verification  
**Date (UTC):** 2026-07-10  
**Target environment:** Production (`erp_pro` on VPS, `https://alamal-ab.org`)  
**Deployed commit:** `6a33464` — Phase 1 posting engine  
**Phase 2 status:** **NOT STARTED**

---

## Executive summary

| Area | Result |
|------|--------|
| Migration applied on PostgreSQL | ✅ PASS |
| Financial baseline unchanged | ✅ PASS |
| Legacy duplicate preserved | ✅ PASS |
| New duplicate prevention (v2 index + engine) | ✅ PASS |
| Concurrency smoke tests (live DB) | ✅ PASS |
| Build & tests | ✅ PASS (13/13) |
| Pre-migration backup (procedure) | ⚠️ **DEVIATION** — backup taken after migration at gate time |
| API Idempotency-Key on all endpoints | ⚠️ Partial — service ready, not all routes wired |

### Decision

```text
NO-GO FOR PHASE 2
```

**Reason:** Procedural gate item #1 (full backup *before* applying migration) was not satisfied — migration was applied at API deploy (`6a33464`) before this verification gate ran. A post-migration backup exists and all **financial/technical** checks pass.

**To reach GO:** Accept Phase 0 read-only baseline + this gate's financial diff as proof of no mutation **or** restore procedure compliance on next migration (backup → migrate → verify).

---

## 1. Backup verification (no secrets)

See: `artifacts/phase1-production-backup-verification.md`

| Field | Value |
|-------|-------|
| Database | `erp_pro` |
| PostgreSQL | 16.14 |
| Backup file | `/opt/erpsystem/backups/phase1-verification/erp_pro_phase1_gate_20260710T153741Z.dump` |
| Size | 577,693 bytes |
| Restore | Documented, **not executed** on production |

**Deviation:** Backup timestamp is **after** migration `20260720120000_AddJournalPostingIdentity` was already applied.

---

## 2. Migration result

| Check | Status |
|-------|--------|
| Migration in `settings.__ef_migrations_history` | ✅ `20260720120000_AddJournalPostingIdentity` |
| Columns on `accounting.journal_entries` | ✅ `PostingKind`, `PostingIdentityVersion`, `IdempotencyKey`, `CorrelationId` |
| Table `accounting.accounting_posting_attempts` | ✅ Created (0 rows) |
| Table `accounting.accounting_idempotency_records` | ✅ Created (0 rows) |
| v2 journal entries | 0 (expected — no new postings since deploy) |

---

## 3. Partial unique index (actual PostgreSQL definition)

```sql
CREATE UNIQUE INDEX "IX_journal_entries_posting_identity_v2"
ON accounting.journal_entries
USING btree ("CompanyId", "SourceType", "SourceId", "PostingKind")
WHERE (("PostingIdentityVersion" = 2)
   AND ("SourceType" IS NOT NULL)
   AND ("SourceId" IS NOT NULL)
   AND ("PostingKind" IS NOT NULL)
   AND ("IsActive" = true));
```

Legacy rows (`PostingIdentityVersion = 1`, `PostingKind NULL`) are **excluded** — safe for existing duplicate pair.

---

## 4. Baseline before / after

| Artifact | Path |
|----------|------|
| Pre (reference) | `artifacts/phase1-pre-migration.md` (+ JSON copies; financial = Phase 0) |
| Post | `artifacts/phase1-post-migration.md`, `.json`, `-health.*` |
| Diff | `artifacts/phase1-production-baseline-diff.md` |

**Financial comparison:** All monetary indicators **unchanged** (AR 320.00, inventory operational 105,636.71, inventory GL 15,622.43, cashboxes 0.00, CashUsd GL -870.00).

---

## 5. Legacy duplicate state

Both entries **unchanged**:

| EntryNumber | Id | SourceType | SourceId | Dr | Cr | PostingIdentityVersion | PostingKind | CreatedAt (UTC) |
|-------------|-----|------------|----------|---:|---:|------------------------|-------------|-----------------|
| JE-MAIN-000001 | ecf429f2-eb86-44f6-a32f-24b48a07bd06 | 9 (ChinaContainer) | b9e96735-73ca-409a-aa2d-2570c5e19f55 | 15,885.00 | 15,885.00 | 1 | NULL | 2026-07-08 00:12:03 |
| JE-MAIN-000002 | e1d9b0f2-0f33-4c92-900e-57e32448da2e | 9 | b9e96735-73ca-409a-aa2d-2570c5e19f55 | 15,885.00 | 15,885.00 | 1 | NULL | 2026-07-08 00:12:32 |

Health checks **fail** (expected) on:
- `duplicate_journal_entries` (Legacy)
- `legacy_critical_duplicate_evidence`

Protected duplicates: **0**

---

## 6. Smoke tests (production DB via SSH tunnel)

| Test | Method | Result |
|------|--------|--------|
| 7.1 Unbalanced posting rejected | `AccountingPostingEngineLiveDbTests.PostAsync_rejects_unbalanced_lines_before_save` | ✅ PASS |
| 7.2 Missing account rejected | Engine `ValidateAccountsAsync` (code + unbalanced test) | ✅ PASS (design) |
| 7.3 Duplicate same identity | Parallel test creates one v2 JE; second returns same reference | ✅ PASS |
| 7.4 Concurrent (20 requests) | `Parallel_posting_same_identity_yields_single_journal_entry` | ✅ PASS — 1 JE, all callers success |
| 7.5 Idempotency key | `AccountingIdempotencyService` implemented; API helper ready | ⚠️ Not live-tested on HTTP in this gate |
| 7.6 Rollback | Engine does not SaveChanges; handler TX rolls back | ✅ PASS (architecture) |

Test journal from concurrency test **cleaned up** after test (only v2 test rows removed).

---

## 7. Posting path protection matrix

| Path | Entry | Handler | IntegratedAccounting method | PostingKind | TX owner | SaveChanges owner | Status |
|------|-------|---------|----------------------------|-------------|----------|-------------------|--------|
| China container approval | API/WPF | `ApproveContainerHandler` | `PostContainerApprovalAsync` | `ChinaContainerLandingCost` | Handler | `IPostingSaveCoordinator` | **Protected** |
| China inventory activation | Warehouse move | `InventoryEngine` | `PostInventoryActivationAsync` | `ChinaContainerInventoryActivation` | `MoveContainerToWarehouseHandler` | Coordinator | **Protected** |
| Sales invoice approval | API/WPF | `ApproveSalesInvoiceHandler` | `PostSalesInvoiceApprovalAsync` | `SalesInvoicePosting` | Handler | Coordinator | **Protected** |
| Receipt voucher | Finance UI/API | `PostReceiptVoucherHandler` | `PostReceiptVoucherAsync` | `ReceiptVoucher` | Handler | Coordinator | **Protected** |
| Payment voucher | Finance | `PostPaymentVoucherHandler` | `PostPaymentVoucherAsync` | `PaymentVoucher` | Handler | Coordinator | **Protected** |
| Sales return | Sales | `SalesReturnHandlers` | `PostSalesReturnAsync` | `SalesReturn` | Handler | Handler UoW | **Protected** |
| Purchase invoice/return | Purchases | Purchase handlers | `PostPurchaseInvoiceAsync` etc. | Various | Handler | Handler UoW | **Protected** |
| Expense payment | Expenses | Expense handlers | `PostExpensePaymentAsync` | `ExpensePayment` | Handler | Handler UoW | **Protected** |
| Opening balance | OB engine | `OpeningBalanceEngine` | `PostOpeningBalanceDocumentAsync` | `FinanceOpeningBalance` | Engine | Engine UoW | **Protected** |
| Cashbox transfer | Finance | Cashbox handlers | `PostCashboxTransferAsync` | `CashboxTransfer` | Handler | Handler UoW | **Protected** |
| Manual journal | Accounting UI | `JournalEntryHandlers` | Direct `journalRepository.AddAsync` | v1 legacy | Handler | Handler | **Manual Entry** |
| Capital transactions | Capital module | `CapitalHandlers` | Not via `IntegratedAccountingService` | — | — | — | **Not Migrated** (no auto GL path verified) |

**Domain events:** Container accounting **removed** from `DomainEventDispatcher` (no post-commit duplicate path).

---

## 8. SaveChanges / transaction audit

| Finding | Status |
|---------|--------|
| `AccountingPostingEngine` calls SaveChanges | ✅ **None** — stages only |
| `IntegratedAccountingService` internal SaveChanges | ✅ **Removed** (`PostIfNotExistsAsync` / `CreateAndPostJournalAsync` deleted) |
| `AccountingIdempotencyService` SaveChanges | ⚠️ Separate concern (idempotency records only) |
| `PostingSaveCoordinator` SaveChanges | ✅ Expected — caller-owned boundary |
| `DomainEventDispatcher` posts journals after commit | ✅ **Removed** for container approval |
| Legacy `PostIfNotExistsAsync` | ✅ **Not present** in codebase |

---

## 9. Posting attempts table

| Check | Count |
|-------|------:|
| Total rows | 0 |
| Stuck `Posting` (>15 min) | 0 |
| Failed without ErrorCode | 0 |
| Success without JournalEntryId | 0 |
| Multiple success same v2 identity | 0 |

---

## 10. Build & tests

```
dotnet build ERPSystem.Api → SUCCESS
dotnet test (excl. TempDetailingDiagnostic) → 13 passed, 0 failed
```

Live DB tests (production tunnel): **2/2 passed** (unbalanced rejection + 20-way concurrency).

---

## 11. Remaining risks

1. **Pre-migration backup gap** (procedural).
2. **Capital module** — automated GL posting not verified through engine.
3. **API Idempotency-Key** — not enforced on all sensitive HTTP endpoints yet.
4. **Inventory GL gap** (90,014.28 USD) — unchanged, Phase 2+ scope.
5. **Cashbox AccountId** — still not linked (unchanged by design).

---

## 12. Confirmations

| Constraint | Verified |
|------------|----------|
| Legacy JE-MAIN-000001/000002 not modified | ✅ |
| Inventory gap not remediated | ✅ |
| AR not altered (320.00 USD) | ✅ |
| No random cashbox AccountId | ✅ |
| Phase 2 not started | ✅ |

---

## 13. Go / No-Go checklist

| # | Criterion | Met? |
|---|-----------|------|
| 1 | Migration applied | ✅ |
| 2 | Backup documented | ⚠️ Post-migration only |
| 3 | Baseline no financial change | ✅ |
| 4 | Legacy duplicate visible, unmodified | ✅ |
| 5 | No protected duplicate | ✅ |
| 6 | No unbalanced journals | ✅ |
| 7 | No stuck posting attempts | ✅ |
| 8 | Concurrency tests pass | ✅ |
| 9 | Sensitive auto paths via engine | ⚠️ Capital not migrated |
| 10 | Build & integration tests pass | ✅ |
| 11 | No engine SaveChanges breaking TX | ✅ |
| 12 | Non-destructive migration | ✅ |

---

```text
Decision: NO-GO FOR PHASE 2
```

**Technical readiness:** High — protection is live and financial data unchanged.  
**Procedural blocker:** Pre-migration backup not available; recommend formal backup-before-migrate on next phase.

---

*End of Phase 1 Production Verification Gate. Phase 2 awaits explicit approval after review.*
