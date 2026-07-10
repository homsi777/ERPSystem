using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERPSystem.Infrastructure.Migrations;

public partial class AddSalesInvoiceCashboxId : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            ALTER TABLE sales.sales_invoices
            ADD COLUMN IF NOT EXISTS "CashboxId" uuid;

            CREATE INDEX IF NOT EXISTS "IX_sales_invoices_CashboxId"
            ON sales.sales_invoices ("CashboxId");
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            DROP INDEX IF EXISTS sales."IX_sales_invoices_CashboxId";
            ALTER TABLE sales.sales_invoices DROP COLUMN IF EXISTS "CashboxId";
            """);
    }
}
