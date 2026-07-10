using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERPSystem.Infrastructure.Migrations;

public partial class AddSalesInvoiceRollDetailDraftFields : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            ALTER TABLE sales.sales_invoice_roll_details
            ADD COLUMN IF NOT EXISTS "DraftRollNumber" integer;

            ALTER TABLE sales.sales_invoice_roll_details
            ADD COLUMN IF NOT EXISTS "DraftLengthMeters" numeric(18,4);
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            ALTER TABLE sales.sales_invoice_roll_details DROP COLUMN IF EXISTS "DraftLengthMeters";
            ALTER TABLE sales.sales_invoice_roll_details DROP COLUMN IF EXISTS "DraftRollNumber";
            """);
    }
}
