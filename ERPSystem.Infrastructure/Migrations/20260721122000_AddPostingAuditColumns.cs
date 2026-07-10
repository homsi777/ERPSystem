using ERPSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERPSystem.Infrastructure.Migrations;

/// <summary>
/// Aligns posting protection tables with the shared PersistenceEntity audit columns.
/// </summary>
[DbContext(typeof(ErpDbContext))]
[Migration("20260721122000_AddPostingAuditColumns")]
public partial class AddPostingAuditColumns : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            ALTER TABLE accounting.accounting_posting_attempts
                ADD COLUMN IF NOT EXISTS "CreatedByUserId" uuid NULL,
                ADD COLUMN IF NOT EXISTS "UpdatedByUserId" uuid NULL,
                ADD COLUMN IF NOT EXISTS "IsArchived" boolean NOT NULL DEFAULT false;

            ALTER TABLE accounting.accounting_idempotency_records
                ADD COLUMN IF NOT EXISTS "CreatedByUserId" uuid NULL,
                ADD COLUMN IF NOT EXISTS "UpdatedByUserId" uuid NULL,
                ADD COLUMN IF NOT EXISTS "IsArchived" boolean NOT NULL DEFAULT false;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            ALTER TABLE accounting.accounting_posting_attempts
                DROP COLUMN IF EXISTS "CreatedByUserId",
                DROP COLUMN IF EXISTS "UpdatedByUserId",
                DROP COLUMN IF EXISTS "IsArchived";

            ALTER TABLE accounting.accounting_idempotency_records
                DROP COLUMN IF EXISTS "CreatedByUserId",
                DROP COLUMN IF EXISTS "UpdatedByUserId",
                DROP COLUMN IF EXISTS "IsArchived";
            """);
    }
}
