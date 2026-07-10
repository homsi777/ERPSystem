# PHASE 2 E2E CERTIFICATION REPORT

**Date (UTC):** 2026-07-10  
**Phase 3 started:** **NO**

---

## Executive decision

```text
PHASE 2 CORE IMPLEMENTATION: ACCEPTED (unchanged)
PHASE 2 FINAL ACCEPTANCE: FAILED
PHASE 3: NO-GO
```

**Reason:** E2E certification **implementation is complete** (isolated test company, runner, CLI tool, integration tests, stub replacement). A **full live certification run with artifact generation** could not be completed in this session because the SSH tunnel to `localhost:5433` was **intermittently unavailable** from the automation host. `dotnet test` reports **61 passed / 0 failed** (1 non-critical diagnostic skipped), but live DB tests **vacuously pass** when the tunnel is down — they do not substitute for a verified E2E artifact run.

**To reach PASSED:** Start the SSH tunnel (WPF app with `appsettings.Local.json`, or manual `ssh -L 5433:localhost:5432 …`), then run:

```bash
dotnet test ERPSystem.Application.Tests --filter "FullyQualifiedName~E2E_All_scenarios"
dotnet run --project tools/Phase2TaxE2ECertification -- --run
```

Confirm `artifacts/phase2-e2e-run-result.json` shows `"allPassed": true` and production AR remains **320.00**.

---

## 1. Backup verification

See `artifacts/phase2-e2e-pre-backup-verification.md`.

| Check | Status |
|-------|--------|
| Prior verified dump | ✅ `erp_pro_final_acceptance_20260710T161652Z.dump` |
| E2E backup script | ✅ `tools/phase2-e2e-verification/backup-pre-e2e.sh` |
| Fresh backup this session | ⚠️ SSH timeout to VPS |

---

## 2. Pre baseline

Expected production anchors (from Phase 2 final gate):

| Metric | Value |
|--------|------:|
| AR GL | 320.00 |
| Stored customer balances | 320.00 |
| Operational inventory | 105,636.71 |
| Inventory GL | 15,622.43 |

Machine-readable pre/post files are written by `E2E_All_scenarios_full_certification_run` when the DB tunnel is active.

---

## 3. Test company configuration

| Item | Value |
|------|-------|
| Name | `ERP PRO TAX E2E TEST COMPANY` |
| CompanyId | `e2e00001-0001-0001-0001-000000000001` |
| Seeder | `Phase2E2ETestCompanySeeder` |
| CostPerMeter | 6.00 |
| Rolls | 40 × 100m |
| Tax codes | VAT15 Exclusive/Inclusive, Zero Rated, Exempt |
| Posting profile | SalesPostingProfile (test GL only) |
| Guard | `GuardNotProduction()` — blocks production `CompanyId` |

---

## 4. E2E scenarios (runner)

| Scenario | Description | Implementation |
|----------|-------------|----------------|
| A | Exclusive VAT 1,000 + 150 = 1,150 | ✅ `RunExclusiveScenarioAsync` |
| B | Inclusive 1,150 | ✅ `RunInclusiveScenarioAsync` |
| C | Invoice discount 100 (net revenue policy) | ✅ `RunInvoiceDiscountScenarioAsync` |
| D | Multi-rate (15% / zero / exempt) | ✅ `RunMultiRateScenarioAsync` |
| E | Partial return 40% | ✅ `RunPartialReturnScenarioAsync` |
| F | Full return | ✅ `RunFullReturnScenarioAsync` |
| G | Legacy read-only | ✅ `RunLegacyReadOnlyScenarioAsync` |
| Concurrency | 20 parallel approvals → 1 journal | ✅ `RunConcurrencyTestAsync` |
| Rollback | Credit limit forced failure | ✅ `RunRollbackTestAsync` |
| Snapshot immutability | Tax code rate change after post | ✅ `RunSnapshotImmutabilityTestAsync` |
| Company isolation | No test data in production | ✅ `RunCompanyIsolationTestAsync` |

Document metadata: `E2E|{RunId}|{Scenario}` in line notes; invoice prefix `E2E-TAX-{RunId}-*`.

---

## 5. Cross-layer proof

`Phase2TaxE2ECertificationRunner.BuildCrossLayerProofAsync` validates DB ↔ PDF ↔ Tax report ↔ Journal AR.

Artifact: `artifacts/phase2-e2e-cross-layer-proof.md` (written on successful `E2E_All_scenarios_full_certification_run`).

---

## 6. Concurrency & idempotency

| Test | Location | Expectation |
|------|----------|-------------|
| 20 parallel approvals | `Matrix_21_22_Concurrent_taxed_approval_single_journal` | 1 success path, 1 sales journal |
| Posting identity | `AccountingPostingEngineLiveDbTests.Parallel_posting_same_identity_yields_single_journal_entry` | 1 journal under same identity |
| Rollback atomicity | `Matrix_23_Rollback_on_approval_failure` | No journal on credit-limit failure |

`ApproveSalesInvoiceCommand` does not expose HTTP idempotency keys; concurrency protection is at **posting identity** + DB transaction level.

---

## 7. Replaced stub tests

| Former stub | Replacement |
|-------------|-------------|
| Matrix_21 Idempotent taxed approval | `AccountingPostingEngineLiveDbTests` + E2E concurrency |
| Matrix_22 Concurrent taxed approval | `Matrix_21_22_Concurrent_taxed_approval_single_journal` |
| Matrix_23 Rollback | `Matrix_23_Rollback_on_approval_failure` |
| Matrix_33 Tax report parity | `Matrix_32_33_Cross_layer_and_tax_report_parity` |
| Matrix_34 Cross-layer / company | `Matrix_34_Company_isolation_*` + cross-layer proof |
| Matrix_25 Legacy journal | `Matrix_25_Legacy_journal_unchanged_read_only` |

No `Stub`, `TODO`, or `NotImplemented` in critical matrix items.

---

## 8. Test counts (`dotnet test ERPSystem.Application.Tests`)

| Total | Passed | Failed | Skipped |
|------:|-------:|-------:|--------:|
| 62 | 61 | 0 | 1 |

Skipped: `TempDetailingDiagnosticTests` (diagnostic only).

**Matrix coverage (unit + integration):** Exclusive, Inclusive, No Tax, Zero Rated, Exempt, Line/Invoice discount, Multi-line, Multi-rate, Decimal qty, Rounding, Inactive/Future/Expired codes, Missing VAT/profile, Balanced posting, Idempotency, Concurrency, Rollback, Legacy, Partial/Full return, PDF/Tax report parity, Snapshot immutability, Company isolation.

---

## 9. Post baseline / production drift

See `artifacts/phase2-e2e-baseline-diff.md`.

Production company must remain: **AR = 320.00**, **Inventory GL = 15,622.43**, **Operational inventory = 105,636.71**.

Integration test `Production_company_invoice_count_unchanged_after_test_operations` guards production invoice count.

---

## 10. CLI tool

```bash
dotnet run --project tools/Phase2TaxE2ECertification -- --seed
dotnet run --project tools/Phase2TaxE2ECertification -- --run
dotnet run --project tools/Phase2TaxE2ECertification -- --verify
dotnet run --project tools/Phase2TaxE2ECertification -- --cleanup
```

Requires `localhost:5433` (SSH tunnel). Skips EF migrate on startup; uses same handlers as production.

---

## 11. Files added/modified

| Path | Purpose |
|------|---------|
| `ERPSystem.Infrastructure/Seed/Phase2E2ETestCompanyIds.cs` | Fixed test IDs |
| `ERPSystem.Infrastructure/Seed/Phase2E2ETestCompanySeeder.cs` | Idempotent seed |
| `ERPSystem.Infrastructure/E2E/Phase2TaxE2ECertificationRunner.cs` | Scenario runner |
| `tools/Phase2TaxE2ECertification/` | CLI certification tool |
| `tools/phase2-e2e-verification/backup-pre-e2e.sh` | VPS backup script |
| `ERPSystem.Application.Tests/E2E/Phase2TaxE2EIntegrationTests.cs` | Live integration tests |
| `ERPSystem.Application.Tests/E2E/Phase2E2ECertificationArtifacts.cs` | Artifact writer |
| `artifacts/phase2-e2e-pre-backup-verification.md` | Backup gate |
| `artifacts/phase2-e2e-baseline-diff.md` | Production vs test diff template |

---

## 12. Migrations

None added for E2E certification.

---

## 13. Risks remaining

1. **Tunnel dependency** — Live E2E requires SSH port-forward; automation without tunnel gives false-green vacuous passes.
2. **Fresh VPS backup** — Not taken this session (SSH timeout).
3. **Idempotency keys on approve API** — Concurrency covered at posting layer; HTTP-level same-key replay not on `ApproveSalesInvoiceCommand`.

---

## 14. Rollback

```bash
sudo -u postgres pg_restore --clean --if-exists -d erp_pro \
  /opt/erpsystem/backups/phase2-final-verification/erp_pro_final_acceptance_20260710T161652Z.dump
```

Test company data is isolated; production rollback uses pre-E2E production dump only.

---

## 15. Final confirmation

```text
PHASE 2 FINAL ACCEPTANCE: FAILED (live artifact gate — tunnel unavailable)
PHASE 3 STARTED: NO
```

Re-run `E2E_All_scenarios_full_certification_run` with tunnel active; if all scenarios and baselines pass, update this decision to **PASSED**.
