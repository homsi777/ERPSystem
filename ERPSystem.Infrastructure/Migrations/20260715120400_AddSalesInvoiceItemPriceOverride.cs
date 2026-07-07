using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERPSystem.Infrastructure.Migrations;

public partial class AddSalesInvoiceItemPriceOverride : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            ALTER TABLE sales.sales_invoice_items
                ADD COLUMN IF NOT EXISTS "OriginalUnitPrice" numeric(18,2) NOT NULL DEFAULT 0;
            ALTER TABLE sales.sales_invoice_items
                ADD COLUMN IF NOT EXISTS "DiscountAmount" numeric(18,2) NOT NULL DEFAULT 0;
            ALTER TABLE sales.sales_invoice_items
                ADD COLUMN IF NOT EXISTS "DiscountReason" character varying(300);
            ALTER TABLE sales.sales_invoice_items
                ADD COLUMN IF NOT EXISTS "PriceModifiedByUserId" uuid;
            ALTER TABLE sales.sales_invoice_items
                ADD COLUMN IF NOT EXISTS "PriceModifiedAt" timestamp with time zone;

            UPDATE sales.sales_invoice_items
                SET "OriginalUnitPrice" = "UnitPrice"
                WHERE "OriginalUnitPrice" = 0;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            ALTER TABLE sales.sales_invoice_items DROP COLUMN IF EXISTS "OriginalUnitPrice";
            ALTER TABLE sales.sales_invoice_items DROP COLUMN IF EXISTS "DiscountAmount";
            ALTER TABLE sales.sales_invoice_items DROP COLUMN IF EXISTS "DiscountReason";
            ALTER TABLE sales.sales_invoice_items DROP COLUMN IF EXISTS "PriceModifiedByUserId";
            ALTER TABLE sales.sales_invoice_items DROP COLUMN IF EXISTS "PriceModifiedAt";
            """);
    }
}
