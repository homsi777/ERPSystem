using ERPSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERPSystem.Infrastructure.Migrations;

/// <summary>
/// Aligns the Phase 2 tax tables with the shared PersistenceEntity audit columns.
/// </summary>
[DbContext(typeof(ErpDbContext))]
[Migration("20260721121000_AddTaxAuditUserColumns")]
public partial class AddTaxAuditUserColumns : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            ALTER TABLE sales.tax_codes
                ADD COLUMN IF NOT EXISTS "CreatedByUserId" uuid NULL,
                ADD COLUMN IF NOT EXISTS "UpdatedByUserId" uuid NULL;

            ALTER TABLE sales.sales_posting_profiles
                ADD COLUMN IF NOT EXISTS "CreatedByUserId" uuid NULL,
                ADD COLUMN IF NOT EXISTS "UpdatedByUserId" uuid NULL;

            ALTER TABLE sales.sales_invoice_item_taxes
                ADD COLUMN IF NOT EXISTS "CreatedByUserId" uuid NULL,
                ADD COLUMN IF NOT EXISTS "UpdatedByUserId" uuid NULL;

            ALTER TABLE sales.sales_return_line_taxes
                ADD COLUMN IF NOT EXISTS "CreatedByUserId" uuid NULL,
                ADD COLUMN IF NOT EXISTS "UpdatedByUserId" uuid NULL;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            ALTER TABLE sales.tax_codes
                DROP COLUMN IF EXISTS "CreatedByUserId",
                DROP COLUMN IF EXISTS "UpdatedByUserId";

            ALTER TABLE sales.sales_posting_profiles
                DROP COLUMN IF EXISTS "CreatedByUserId",
                DROP COLUMN IF EXISTS "UpdatedByUserId";

            ALTER TABLE sales.sales_invoice_item_taxes
                DROP COLUMN IF EXISTS "CreatedByUserId",
                DROP COLUMN IF EXISTS "UpdatedByUserId";

            ALTER TABLE sales.sales_return_line_taxes
                DROP COLUMN IF EXISTS "CreatedByUserId",
                DROP COLUMN IF EXISTS "UpdatedByUserId";
            """);
    }
}
