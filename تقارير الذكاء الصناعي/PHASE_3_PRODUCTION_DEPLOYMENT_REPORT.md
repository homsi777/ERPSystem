# Phase 3 Production Deployment Report

**Generated (UTC):** 2026-07-11T09:05:00Z  
**Gate decision input:** CONDITIONALLY APPROVED  
**Target commit:** `d225b60`  
**Rollback commit:** `caa3b87`

---

## 1. Maintenance window

| Item | Value |
|------|-------|
| Opened (UTC) | 2026-07-11T08:50:00Z |
| Closed (UTC) | 2026-07-11T09:01:00Z |
| API downtime | ~2 minutes (08:59:22 – 09:00:54 UTC) |
| PostgreSQL stopped | No |
| Queues/jobs deleted | No |
| E2E writes on `erp_pro` | No |

Artifact: `artifacts/phase3-production-maintenance-window.md`

---

## 2. Backup verification

| Item | Value |
|------|-------|
| File | `erp_pro_pre_phase3_production_deploy_20260711T084813Z.dump` |
| Path | `/opt/erpsystem/backups/phase3-verification/` |
| Size | 1,100,424 bytes |
| TOC count | 448 |
| `pg_restore --list` | PASSED |

Artifact: `artifacts/phase3-production-deploy-backup-verification.md`

---

## 3. Pre-baseline (production company)

| Metric | Value |
|--------|------:|
| AR GL | 320.00 |
| Operational inventory | 105,636.71 |
| Inventory GL | 15,622.43 |
| Posted receipts | 0 |
| Legacy duplicate journals | 1 (ChinaContainer — preserved) |
| Unbalanced journals | 0 |
| Cashboxes without AccountId | 2 |

Artifacts: `artifacts/phase3-production-predeploy.json`, `.md`, `-health.json`, `-health.md`

---

## 4. Target commit verification

```
origin/main = d225b60d117d1a9cf7431cdc1c33901c3fbc59db
e9af1a4 fix(finance): complete phase 3 acceptance and e2e certification
```

Production pre-deploy: `caa3b87`  
**No Phase 4 commits on `origin/main`.**

---

## 5. Migration preflight

| Check | Result |
|-------|--------|
| Pending migrations | `20260722120000_AddPhase3FinanceModule`, `20260722121000_FixPhase3FinanceSchema` |
| Manual schema repair required | **NO** |
| Tracked migrations sufficient | **YES** |
| `payment_methods` composite PK | In `20260722121000` |
| Audit columns | In `20260722121000` |
| `receipt_tender_lines` FK/indexes | In `20260722120000` |
| Reversal metadata columns | In `20260722120000` |

---

## 6. Cashbox preflight

Both production cashboxes (`CASH-MAIN`, `CASH-MAIN-000001`) classified **MissingAccount**.  
No automatic CashUsd linking applied.

Artifact: `artifacts/phase3-production-cashbox-preflight.md`

---

## 7. Legacy receipt preflight

Posted/reversed receipts: **0** — no legacy receipt remediation required.

Artifact: `artifacts/phase3-production-legacy-receipts-preflight.md`

---

## 8. Build and tests (clean `d225b60` worktree)

| Step | Result |
|------|--------|
| `dotnet build ERPSystem.Api` (Release) | 0 errors |
| `dotnet test --filter Phase3` | 10/10 PASSED |
| `npm ci && npm run build` (web-client) | PASSED |

Worktree: `../phase3-deploy-verify` @ `d225b60`

---

## 9. Migration result

| Item | Value |
|------|-------|
| Start (UTC) | 2026-07-11T09:00:25Z |
| End (UTC) | 2026-07-11T09:00:35Z (approx.) |
| Migrations applied | `20260722120000_AddPhase3FinanceModule`, `20260722121000_FixPhase3FinanceSchema` |
| Result | **SUCCESS** |

### Schema verification (post-migration)

| Object | Verified |
|--------|----------|
| `finance.payment_methods` PK `(CompanyId, Id)` | ✅ |
| `finance.bank_accounts` | ✅ |
| `finance.receipt_tender_lines` | ✅ |
| FK `receipt_tender_lines → receipt_vouchers` ON DELETE RESTRICT | ✅ |
| Indexes on payment_methods, bank_accounts, receipt_tender_lines | ✅ |
| Audit columns (CreatedByUserId, UpdatedByUserId) | ✅ |
| Cascade delete on financial records | None detected |

---

## 10. Deployment result

| Item | Value |
|------|-------|
| Deployed commit | `d225b60` |
| API publish path | `/opt/erpsystem/api` |
| Web root | `/var/www/alamal-ab.org` |
| Service | `erpsystem-api` active |
| Site | https://alamal-ab.org |

Deploy method: pinned `git reset --hard d225b60` (not open-ended `deploy-app.sh` pull).

---

## 11. Health result

```
curl https://alamal-ab.org/health → OK
```

API logs: no migration errors, no DI errors, no missing-column errors, no restart loop.

---

## 12. Smoke tests (read-only)

| Endpoint | Status |
|----------|--------|
| `GET /api/v1/finance/cashboxes` | 200 |
| `GET /api/v1/finance/bank-accounts` | 200 |
| `GET /api/v1/finance/payment-methods` | 200 |
| `GET /api/v1/finance/cashboxes/reconciliation` | 200 |
| `GET /api/v1/sales/invoices?page=1&pageSize=5` | 200 |
| `GET /api/v1/accounting/journal-entries?page=1&pageSize=5` | 200 |

No 500/502 errors observed.

### Safe write smoke

Skipped — Phase 3 isolated TEST company (`E2E-PHASE3-FINANCE`) is not seeded on production `erp_pro`. Prior E2E certification: **28/28 PASSED** on isolated test DB.

---

## 13. CashUsd verification

| Check | Count |
|-------|------:|
| New receipt vouchers since window | 0 |
| New journal lines to CashUsd since window | 0 |
| Legacy CashUsd GL balance | -870.00 (unchanged, reported only) |

---

## 14. Post-baseline

All key metrics identical to pre-baseline. See `artifacts/phase3-production-postdeploy.md`.

---

## 15. Baseline diff

**PASSED** — zero historical drift.  
Artifact: `artifacts/phase3-production-deployment-diff.md`

---

## 16. Production risks (accepted)

1. **Both cashboxes lack GL AccountId** — receipt posting and cash sales from these cashboxes will be rejected until manually mapped. No CashUsd auto-assignment.
2. **Legacy ChinaContainer duplicate journals** preserved (pre-existing).
3. **Inventory operational vs GL gap** pre-existing (105,636.71 vs 15,622.43).

---

## 17. Rollback readiness

| Item | Ready |
|------|-------|
| Fresh verified backup | ✅ |
| Rollback commit `caa3b87` available | ✅ |
| Restore command documented | ✅ |
| Rollback procedure tested on test DB (prior acceptance) | ✅ |

Rollback **not required** — deployment succeeded.

---

## 18. Final decision

```text
PHASE 3 PRODUCTION DEPLOYMENT: PASSED
```

---

## 19. Phase 4 confirmation

```text
PHASE 4 STARTED: NO
```

---

## Operator notes

- Hard-refresh https://alamal-ab.org on mobile/PWA after deploy.
- Map production cashbox GL accounts before enabling cash receipt posting.
- Local-only tooling note: `AccountingBaselineReport` at `d225b60` requires `IAccountingBaselineReportService` DI registration (one-line fix applied locally for baseline runs only; not in deployed commit).

**STOP — Phase 3 production gate complete.**
