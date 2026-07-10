# Phase 0 Completion Report — Baseline and Data Protection

**Program:** ERP PRO Sales, Accounting, Payments and Inventory Stabilization  
**Phase:** 0 — Baseline and Data Protection  
**Date (UTC):** 2026-07-10  
**Status:** ✅ Complete — **STOP** (Phase 1 not started)

---

## 1. Objective executed

Establish a **read-only financial baseline** and **health-check infrastructure** before any accounting code changes, per `SALES_ACCOUNTING_AUDIT_REPORT.md` and the stabilization program specification.

**No financial rows were modified.** No migrations were applied. No destructive operations were run.

---

## 2. Files added

| Path | Purpose |
|------|---------|
| `docs/accounting/POSTGRES_BACKUP_RESTORE.md` | Documented PostgreSQL backup/restore and rollback checklist |
| `ERPSystem.Application/DTOs/Accounting/AccountingBaselineDtos.cs` | Baseline + health-check DTOs |
| `ERPSystem.Application/Abstractions/Services/IAccountingBaselineReportService.cs` | Baseline report contract |
| `ERPSystem.Application/Abstractions/Services/IAccountingHealthCheckService.cs` | Health check contract |
| `ERPSystem.Infrastructure/Services/AccountingBaselineReadService.cs` | Read-only queries + service implementations |
| `tools/AccountingBaselineReport/AccountingBaselineReport.csproj` | CLI tool project |
| `tools/AccountingBaselineReport/Program.cs` | Generates JSON/MD artifacts |
| `ERPSystem.Application.Tests/AccountingHealthCheckMappingTests.cs` | Unit tests for health mapping |

---

## 3. Files modified

| Path | Change |
|------|--------|
| `ERPSystem.Infrastructure/DependencyInjection/InfrastructureServiceCollectionExtensions.cs` | DI registration for baseline + health services |
| `ERPSystem.Infrastructure/ERPSystem.Infrastructure.csproj` | `InternalsVisibleTo` Application.Tests |

---

## 4. Migrations

**None.** Phase 0 is read-only.

---

## 5. New tables / columns / indexes

**None.**

---

## 6. Previous behavior

- No centralized accounting baseline or health-check API.
- `AccountingHealth` only validated seeded GL accounts at startup.
- No documented backup/restore guide in repo.
- Audit findings in `SALES_ACCOUNTING_AUDIT_REPORT.md` were not quantified against live data.

---

## 7. New behavior

- **`IAccountingBaselineReportService`** aggregates invoice/receipt/allocation/customer/cashbox/inventory/COGS metrics and issue lists (duplicates, unbalanced entries, orphan allocations, etc.).
- **`IAccountingHealthCheckService`** runs 12 read-only checks with Pass/Fail, severity, and sample details.
- **CLI tool** writes artifacts:

  ```bash
  dotnet run --project tools/AccountingBaselineReport/AccountingBaselineReport.csproj
  ```

  Output (local, gitignored):

  - `artifacts/accounting-baseline-before.json`
  - `artifacts/accounting-baseline-before.md`
  - `artifacts/accounting-baseline-before-health.json`
  - `artifacts/accounting-baseline-before-health.md`

  Options: `--company-id <guid>`, `--output-prefix <name>`

---

## 8. Architectural decisions

1. **Single read engine** (`AccountingBaselineReadService`) shared by baseline report and health checks to avoid divergent logic.
2. **Open amount** for invoice checks: `GrandTotal − posted allocations − posted returns` (tolerance ±0.01 USD).
3. **Customer subledger** = posted AR journal lines with `PartyId` (not stored `Customer.Balance`).
4. **Cashbox GL** compared only when `Cashbox.AccountId` is set; otherwise flagged as manual review vs aggregate `CashUsd`.
5. **Inventory operational value** = available rolls (`RemainingLengthMeters × CostPerMeter`); GL = `InventoryAsset` posted balance.
6. **Artifacts gitignored** — baseline is environment-specific; tool is the reproducible source of truth.

---

## 9. Tests added

| Test | Result |
|------|--------|
| `AccountingHealthCheckMappingTests.BuildHealthCheck_Passes_When_No_Issues` | ✅ Pass |
| `AccountingHealthCheckMappingTests.BuildHealthCheck_Fails_Critical_When_Duplicate_Journals_Present` | ✅ Pass |

---

## 10. Build result

```
dotnet build ERPSystem.Api/ERPSystem.Api.csproj  → SUCCESS (0 errors)
dotnet build tools/AccountingBaselineReport      → SUCCESS
```

---

## 11. Test result

```
dotnet test --filter FullyQualifiedName~AccountingHealthCheckMappingTests  → 2/2 passed
```

**Note:** `TempDetailingDiagnosticTests` is a live-DB diagnostic test and may fail without tunnel/expected data — pre-existing, not introduced by Phase 0.

---

## 12. Data differences discovered (live DB via SSH tunnel, 2026-07-10)

| Area | Finding |
|------|---------|
| **Duplicate journals (Critical)** | 1 duplicate group: `ChinaContainer` `b9e96735-…` → `JE-MAIN-000001`, `JE-MAIN-000002` |
| **Inventory vs GL (Warning)** | Operational **105,636.71 USD** vs GL **15,622.43 USD** (large gap — expected given partial GL activation vs full roll valuation) |
| **CashUsd GL** | Balance **-870.00 USD** with operational cashboxes at **0.00** and **no AccountId** linked on cashboxes |
| **AR vs stored customers** | **Matched** at 320.00 USD |
| **Approved sales** | 1 invoice, 320.00 USD grand total |
| **Posted receipts** | 0 |
| **Unbalanced journals** | 0 |
| **Negative open invoices / over-allocation / orphans** | 0 |

Full detail: `artifacts/accounting-baseline-before.md` (local).

---

## 13. Remaining risks

1. Phase 1 must address **duplicate journal posting** (idempotency + DB unique constraint) — already confirmed on live data.
2. **Cashbox ↔ GL** linkage missing on operational cashboxes — aligns with audit finding C3/C4.
3. **Inventory valuation gap** requires reconciliation design before trusting inventory GL in Phase 7.
4. Baseline tool requires **EF Relational 9.0.6** package reference in tool project (fixed in this phase).

---

## 14. Not implemented (by design — Phase 0 scope)

- Phase 1+ posting engine, tax, allocations, reversals, UX, migrations
- Automatic backup execution
- API endpoints for baseline/health (can be added later if needed)
- WPF/UI integration for health dashboard

---

## 15. Rollback instructions

Phase 0 changes are **additive only**. Rollback = revert git commit; no database rollback required.

If baseline tool was run accidentally with write code in future phases, restore DB from backup per `docs/accounting/POSTGRES_BACKUP_RESTORE.md`.

---

## Acceptance criteria (Phase 0)

| Criterion | Met? |
|-----------|------|
| No financial row modified | ✅ |
| Baseline report generated successfully | ✅ |
| All differences surfaced (not hidden) | ✅ |
| Build successful | ✅ |
| Tests do not regress (new tests pass) | ✅ |
| Completion report created | ✅ |

---

## Next step

**STOP.** Review this report and baseline artifacts before approving **Phase 1 — Posting protection and duplicate journal prevention**.

---

*Generated as part of ERP PRO Accounting Stabilization Program.*
