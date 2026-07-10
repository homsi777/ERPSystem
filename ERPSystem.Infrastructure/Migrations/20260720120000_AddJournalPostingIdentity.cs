using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERPSystem.Infrastructure.Migrations;

/// <summary>
/// Phase 1 — posting identity metadata, audit tables, and partial unique index for v2 entries only.
/// Legacy rows (PostingIdentityVersion = 1, PostingKind NULL) are intentionally untouched.
/// </summary>
public partial class AddJournalPostingIdentity : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            -- Preflight: report legacy duplicates (SourceType + SourceId) — informational only, no data mutation.
            DO $$
            DECLARE legacy_dup_count integer;
            BEGIN
                SELECT COUNT(*) INTO legacy_dup_count
                FROM (
                    SELECT "CompanyId", "SourceType", "SourceId"
                    FROM accounting.journal_entries
                    WHERE "SourceType" IS NOT NULL
                      AND "SourceId" IS NOT NULL
                      AND "IsActive" = true
                    GROUP BY "CompanyId", "SourceType", "SourceId"
                    HAVING COUNT(*) > 1
                ) d;
                RAISE NOTICE 'accounting-baseline-before: legacy duplicate source groups = %', legacy_dup_count;
            END $$;

            ALTER TABLE accounting.journal_entries
                ADD COLUMN IF NOT EXISTS "PostingKind" integer,
                ADD COLUMN IF NOT EXISTS "PostingIdentityVersion" integer NOT NULL DEFAULT 1,
                ADD COLUMN IF NOT EXISTS "IdempotencyKey" character varying(200),
                ADD COLUMN IF NOT EXISTS "CorrelationId" character varying(100);

            CREATE TABLE IF NOT EXISTS accounting.accounting_posting_attempts (
                "Id" uuid NOT NULL,
                "CompanyId" uuid NOT NULL,
                "BranchId" uuid NOT NULL,
                "SourceType" integer NOT NULL,
                "SourceId" uuid NOT NULL,
                "PostingKind" integer NOT NULL,
                "Status" integer NOT NULL,
                "IdempotencyKey" character varying(200),
                "CorrelationId" character varying(100),
                "UserId" uuid,
                "JournalEntryId" uuid,
                "ErrorCode" character varying(100),
                "ErrorMessage" character varying(2000),
                "RetryCount" integer NOT NULL DEFAULT 0,
                "StartedAt" timestamp with time zone NOT NULL,
                "CompletedAt" timestamp with time zone,
                "CreatedAt" timestamp with time zone NOT NULL,
                "UpdatedAt" timestamp with time zone,
                "IsActive" boolean NOT NULL DEFAULT true,
                CONSTRAINT "PK_accounting_posting_attempts" PRIMARY KEY ("Id")
            );

            CREATE INDEX IF NOT EXISTS "IX_accounting_posting_attempts_CompanyId_SourceType_SourceId_PostingKind"
                ON accounting.accounting_posting_attempts ("CompanyId", "SourceType", "SourceId", "PostingKind");

            CREATE INDEX IF NOT EXISTS "IX_accounting_posting_attempts_Status_StartedAt"
                ON accounting.accounting_posting_attempts ("Status", "StartedAt");

            CREATE TABLE IF NOT EXISTS accounting.accounting_idempotency_records (
                "Id" uuid NOT NULL,
                "CompanyId" uuid NOT NULL,
                "UserId" uuid NOT NULL,
                "Operation" character varying(200) NOT NULL,
                "IdempotencyKey" character varying(200) NOT NULL,
                "RequestHash" character varying(128) NOT NULL,
                "ResponseJson" text,
                "Status" integer NOT NULL,
                "FailureCode" character varying(100),
                "CompletedAt" timestamp with time zone,
                "CreatedAt" timestamp with time zone NOT NULL,
                "UpdatedAt" timestamp with time zone,
                "IsActive" boolean NOT NULL DEFAULT true,
                CONSTRAINT "PK_accounting_idempotency_records" PRIMARY KEY ("Id")
            );

            CREATE UNIQUE INDEX IF NOT EXISTS "IX_accounting_idempotency_records_identity"
                ON accounting.accounting_idempotency_records ("CompanyId", "UserId", "Operation", "IdempotencyKey");

            -- Partial unique index: only v2 protected automated postings (legacy rows excluded).
            CREATE UNIQUE INDEX IF NOT EXISTS "IX_journal_entries_posting_identity_v2"
                ON accounting.journal_entries ("CompanyId", "SourceType", "SourceId", "PostingKind")
                WHERE "PostingIdentityVersion" = 2
                  AND "SourceType" IS NOT NULL
                  AND "SourceId" IS NOT NULL
                  AND "PostingKind" IS NOT NULL
                  AND "IsActive" = true;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            DROP INDEX IF EXISTS accounting."IX_journal_entries_posting_identity_v2";
            DROP INDEX IF EXISTS accounting."IX_accounting_idempotency_records_identity";
            DROP TABLE IF EXISTS accounting.accounting_idempotency_records;
            DROP INDEX IF EXISTS accounting."IX_accounting_posting_attempts_Status_StartedAt";
            DROP INDEX IF EXISTS accounting."IX_accounting_posting_attempts_CompanyId_SourceType_SourceId_PostingKind";
            DROP TABLE IF EXISTS accounting.accounting_posting_attempts;
            ALTER TABLE accounting.journal_entries
                DROP COLUMN IF EXISTS "CorrelationId",
                DROP COLUMN IF EXISTS "IdempotencyKey",
                DROP COLUMN IF EXISTS "PostingIdentityVersion",
                DROP COLUMN IF EXISTS "PostingKind";
            """);
    }
}
