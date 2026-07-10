# Phase 3 verification tools (test / recovery only)

**Authoritative schema path:** `dotnet ef database update` via:

- `20260722120000_AddPhase3FinanceModule`
- `20260722121000_FixPhase3FinanceSchema`

Do **not** apply manual SQL on production. Production migration requires explicit operator approval.

## Scripts

| File | Purpose |
|------|---------|
| `backup-pre-phase3.sh` | Read-only backup of `erp_pro` before Phase 3 (VPS, sudo) |
| `restore-test-db.sh` | Restore isolated `erp_pro_phase3_e2e` from verified dump |
| `grant-finance-erp-app.sql` | Grants `erp_app` on `finance` schema (test DB ops) |
| `fix-phase3-audit-columns.sql` | **Recovery only** — superseded by `20260722121000` |
| `fix-payment-methods-composite-pk.sql` | **Recovery only** — superseded by `20260722121000` |
| `apply-phase3-migration.sql` | **Recovery only** — use when EF migrate cannot run on test clone |

No connection strings or passwords are stored in these files.
