# PostgreSQL Backup and Restore — ERP PRO

This document describes how to back up and restore the ERP PRO database (`erp_pro`) on PostgreSQL.  
**Read-only reference only** — no automated backup or restore is executed by the application.

---

## Scope

| Item | Value |
|------|-------|
| Engine | PostgreSQL 15+ (Npgsql / EF Core 9) |
| Typical database name | `erp_pro` |
| Production host | VPS behind `alamal-ab.org` |
| Schemas | `sales`, `parties`, `finance`, `accounting`, `inventory`, `audit`, `identity`, `settings`, … |

Before any accounting stabilization change (Phase 1+), take a **verified backup** and record:

- Backup file path and size
- PostgreSQL version (`SELECT version();`)
- Git commit hash
- Operator name and timestamp (UTC)

---

## 1. Logical backup (recommended)

### 1.1 Full database dump

On the database server (or via SSH tunnel from a machine with `pg_dump`):

```bash
export PGPASSWORD='YOUR_PASSWORD'
pg_dump \
  -h localhost \
  -p 5432 \
  -U erp_app \
  -d erp_pro \
  -F c \
  -f "erp_pro_$(date -u +%Y%m%dT%H%M%SZ).dump"
```

- `-F c` = custom format (supports parallel restore and selective restore).
- Store dumps outside the web root; restrict file permissions (`chmod 600`).

### 1.2 Plain SQL dump (human-readable)

```bash
pg_dump \
  -h localhost \
  -p 5432 \
  -U erp_app \
  -d erp_pro \
  --no-owner \
  --no-privileges \
  -f "erp_pro_$(date -u +%Y%m%dT%H%M%SZ).sql"
```

Use plain SQL when you need to inspect or edit specific statements before restore (not recommended for production unless reviewed by a DBA).

### 1.3 Schema-only or data-only

```bash
# Schema only
pg_dump -h localhost -U erp_app -d erp_pro -s -f erp_pro_schema.sql

# Data only (after schema exists)
pg_dump -h localhost -U erp_app -d erp_pro -a -f erp_pro_data.sql
```

---

## 2. Restore

**Warning:** Restore overwrites data in the target database. Always restore to a **separate database** first when validating.

### 2.1 Restore custom format dump

```bash
createdb -h localhost -U postgres erp_pro_restore_test
pg_restore \
  -h localhost \
  -p 5432 \
  -U postgres \
  -d erp_pro_restore_test \
  --no-owner \
  --role=erp_app \
  erp_pro_YYYYMMDDTHHMMSSZ.dump
```

### 2.2 Restore plain SQL

```bash
psql -h localhost -U postgres -d erp_pro_restore_test -f erp_pro_YYYYMMDD.sql
```

### 2.3 Post-restore checks

```sql
SELECT COUNT(*) FROM sales.sales_invoices;
SELECT COUNT(*) FROM accounting.journal_entries;
SELECT COUNT(*) FROM finance.receipt_vouchers;
SELECT SUM(balance) FROM parties.customers;
```

Run the accounting baseline tool (Phase 0):

```bash
dotnet run --project tools/AccountingBaselineReport/AccountingBaselineReport.csproj
```

Compare output with `artifacts/accounting-baseline-before.json` from the backup point.

---

## 3. Point-in-time recovery (PITR)

If the server uses WAL archiving (production recommendation):

1. Ensure `archive_mode = on` and `archive_command` is configured.
2. Take a base backup with `pg_basebackup`.
3. To recover to a timestamp, restore base backup + replay WAL to target time.

Document your host-specific WAL path and retention policy on the VPS. This repo does not configure PITR automatically.

---

## 4. Local development (SSH tunnel)

WPF / tools often connect via `appsettings.Local.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5433;Database=erp_pro;Username=erp_app;Password=***"
  },
  "SshTunnel": {
    "Enabled": true,
    "Host": "65.21.136.217",
    "Port": 2727,
    "Username": "ubuntu",
    "PrivateKeyPath": "C:/Users/.../.ssh/alamal_ab_tunnel",
    "RemoteHost": "127.0.0.1",
    "RemotePort": 5432,
    "LocalPort": 5433
  }
}
```

Backup through the tunnel:

```bash
pg_dump -h localhost -p 5433 -U erp_app -d erp_pro -F c -f erp_pro_local.dump
```

---

## 5. What NOT to do

- Do **not** use `EnsureDeleted()` or drop production schemas during stabilization.
- Do **not** restore production dumps onto shared dev machines without anonymizing customer/financial data if required by policy.
- Do **not** delete old journal entries or invoices to “fix” reconciliation — use reversal/adjustment workflows (Phase 1+).
- Do **not** run destructive migrations without a fresh backup and a dry-run on a copy.

---

## 6. Rollback checklist (accounting changes)

1. Stop API / WPF users from posting new documents.
2. Confirm latest backup exists and is restorable.
3. Note current git commit and EF migration id:

   ```sql
   SELECT * FROM settings."__ef_migrations_history" ORDER BY "MigrationId" DESC LIMIT 5;
   ```

4. If rollback is required: restore database from backup **or** deploy previous application version without destructive schema changes.
5. Re-run baseline report and compare with pre-change artifacts.
6. Document incident in accounting phase completion report.

---

## 7. Related tools

| Tool | Purpose |
|------|---------|
| `tools/AccountingBaselineReport` | Read-only financial baseline JSON/MD |
| `tools/MigrateOnly` | Apply EF migrations (use only after backup) |
| `IAccountingHealthCheckService` | Read-only health checks (no data mutation) |

---

*ERP PRO — Accounting Stabilization Program — Phase 0*
