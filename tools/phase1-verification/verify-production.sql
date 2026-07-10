#!/usr/bin/env bash
# Phase 1 production verification SQL — read-only
set -euo pipefail
sudo -u postgres psql -d erp_pro -v ON_ERROR_STOP=1 <<'SQL'
\echo '=== MIGRATION HISTORY ==='
SELECT "MigrationId" FROM settings."__ef_migrations_history" 
WHERE "MigrationId" LIKE '%PostingIdentity%' OR "MigrationId" LIKE '%20260720%';

\echo '=== JOURNAL_ENTRIES COLUMNS ==='
SELECT column_name, data_type, is_nullable, column_default
FROM information_schema.columns
WHERE table_schema = 'accounting' AND table_name = 'journal_entries'
  AND column_name IN ('PostingKind','PostingIdentityVersion','IdempotencyKey','CorrelationId')
ORDER BY column_name;

\echo '=== NEW TABLES ==='
SELECT table_schema, table_name FROM information_schema.tables
WHERE table_schema = 'accounting'
  AND table_name IN ('accounting_posting_attempts','accounting_idempotency_records');

\echo '=== PARTIAL UNIQUE INDEX ==='
SELECT indexname, indexdef FROM pg_indexes
WHERE schemaname = 'accounting' AND indexname LIKE '%posting_identity%';

\echo '=== LEGACY DUPLICATE ENTRIES ==='
SELECT j."Id", j."EntryNumber", j."SourceType", j."SourceId",
       j."PostingIdentityVersion", j."PostingKind", j."CreatedAt",
       COALESCE(SUM(l."Debit"),0) AS debit_total,
       COALESCE(SUM(l."Credit"),0) AS credit_total
FROM accounting.journal_entries j
LEFT JOIN accounting.journal_entry_lines l ON l."JournalEntryId" = j."Id"
WHERE j."EntryNumber" IN ('JE-MAIN-000001','JE-MAIN-000002')
GROUP BY j."Id", j."EntryNumber", j."SourceType", j."SourceId",
         j."PostingIdentityVersion", j."PostingKind", j."CreatedAt"
ORDER BY j."EntryNumber";

\echo '=== JOURNAL COUNTS ==='
SELECT COUNT(*) AS total_journals FROM accounting.journal_entries WHERE "IsActive" = true;
SELECT COUNT(*) AS v2_journals FROM accounting.journal_entries WHERE "PostingIdentityVersion" = 2;
SELECT COUNT(*) AS protected_duplicates FROM (
  SELECT "CompanyId","SourceType","SourceId","PostingKind"
  FROM accounting.journal_entries
  WHERE "PostingIdentityVersion" = 2 AND "PostingKind" IS NOT NULL AND "IsActive" = true
  GROUP BY 1,2,3,4 HAVING COUNT(*) > 1
) d;

\echo '=== POSTING ATTEMPTS ==='
SELECT "Status", COUNT(*) FROM accounting.accounting_posting_attempts GROUP BY 1;
SELECT COUNT(*) AS stuck_posting FROM accounting.accounting_posting_attempts
WHERE "Status" = 0 AND "StartedAt" < NOW() - INTERVAL '15 minutes';

\echo '=== FINANCIAL SNAPSHOT ==='
SELECT 'AR_GL' AS metric, COALESCE(SUM(l."Debit")-SUM(l."Credit"),0) AS value
FROM accounting.journal_entry_lines l
JOIN accounting.journal_entries j ON j."Id" = l."JournalEntryId"
WHERE j."Status" = 1 AND l."AccountId" = '11111111-1111-1111-1111-111111111111';

SELECT 'INVENTORY_GL' AS metric, COALESCE(SUM(l."Debit")-SUM(l."Credit"),0) AS value
FROM accounting.journal_entry_lines l
JOIN accounting.journal_entries j ON j."Id" = l."JournalEntryId"
WHERE j."Status" = 1 AND l."AccountId" = '14111111-1111-1111-1111-111111111111';

SELECT 'CUSTOMER_STORED' AS metric, COALESCE(SUM("Balance"),0) AS value FROM parties.customers WHERE "IsActive" = true;
SQL
