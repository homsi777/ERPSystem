using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERPSystem.Infrastructure.Migrations;

public partial class AddSalesInvoiceItemNotes : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            ALTER TABLE sales.sales_invoice_items
                ADD COLUMN IF NOT EXISTS "Notes" character varying(500);
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""ALTER TABLE sales.sales_invoice_items DROP COLUMN IF EXISTS "Notes";""");
    }
}
