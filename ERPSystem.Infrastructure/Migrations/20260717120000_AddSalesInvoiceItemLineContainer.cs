using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERPSystem.Infrastructure.Migrations;

public partial class AddSalesInvoiceItemLineContainer : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            ALTER TABLE sales.sales_invoice_items
            ADD COLUMN IF NOT EXISTS "ChinaContainerId" uuid;

            UPDATE sales.sales_invoice_items item
            SET "ChinaContainerId" = invoice."ChinaContainerId"
            FROM sales.sales_invoices invoice
            WHERE item."SalesInvoiceId" = invoice."Id"
              AND item."ChinaContainerId" IS NULL;

            ALTER TABLE sales.sales_invoice_items
            ALTER COLUMN "ChinaContainerId" SET NOT NULL;

            CREATE INDEX IF NOT EXISTS "IX_sales_invoice_items_ChinaContainerId"
            ON sales.sales_invoice_items ("ChinaContainerId");

            CREATE INDEX IF NOT EXISTS "IX_sales_invoice_items_SalesInvoiceId_ChinaContainerId"
            ON sales.sales_invoice_items ("SalesInvoiceId", "ChinaContainerId");

            CREATE INDEX IF NOT EXISTS "IX_sales_invoice_roll_details_FabricRollId"
            ON sales.sales_invoice_roll_details ("FabricRollId");
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            DROP INDEX IF EXISTS sales."IX_sales_invoice_roll_details_FabricRollId";
            DROP INDEX IF EXISTS sales."IX_sales_invoice_items_SalesInvoiceId_ChinaContainerId";
            DROP INDEX IF EXISTS sales."IX_sales_invoice_items_ChinaContainerId";
            ALTER TABLE sales.sales_invoice_items DROP COLUMN IF EXISTS "ChinaContainerId";
            """);
    }
}
