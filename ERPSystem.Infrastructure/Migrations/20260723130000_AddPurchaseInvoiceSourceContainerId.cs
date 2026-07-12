using ERPSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERPSystem.Infrastructure.Migrations;

/// <summary>
/// Links purchase invoices to China import containers (China ↔ Purchases bridge).
/// </summary>
[DbContext(typeof(ErpDbContext))]
[Migration("20260723130000_AddPurchaseInvoiceSourceContainerId")]
public partial class AddPurchaseInvoiceSourceContainerId : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            ALTER TABLE purchasing.purchase_invoices
                ADD COLUMN IF NOT EXISTS "SourceContainerId" uuid NULL;

            CREATE UNIQUE INDEX IF NOT EXISTS "IX_purchase_invoices_SourceContainerId"
                ON purchasing.purchase_invoices ("SourceContainerId")
                WHERE "SourceContainerId" IS NOT NULL;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            DROP INDEX IF EXISTS purchasing."IX_purchase_invoices_SourceContainerId";
            ALTER TABLE purchasing.purchase_invoices
                DROP COLUMN IF EXISTS "SourceContainerId";
            """);
    }
}
