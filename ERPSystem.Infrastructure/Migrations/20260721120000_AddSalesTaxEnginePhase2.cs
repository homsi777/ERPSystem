using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERPSystem.Infrastructure.Migrations;

/// <summary>
/// Phase 2 — Sales Tax Engine (additive only). Historical invoices flagged as legacy untaxed.
/// </summary>
public partial class AddSalesTaxEnginePhase2 : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            CREATE TABLE IF NOT EXISTS sales.tax_codes (
                "Id" uuid NOT NULL,
                "CompanyId" uuid NOT NULL,
                "Code" character varying(20) NOT NULL,
                "Name" character varying(200) NOT NULL,
                "Rate" numeric(9,6) NOT NULL,
                "PriceMode" integer NOT NULL DEFAULT 0,
                "Category" integer NOT NULL DEFAULT 0,
                "SalesTaxAccountId" uuid NULL,
                "EffectiveFrom" date NOT NULL,
                "EffectiveTo" date NULL,
                "IsActive" boolean NOT NULL DEFAULT true,
                "CreatedAt" timestamp with time zone NOT NULL DEFAULT NOW(),
                "UpdatedAt" timestamp with time zone NULL,
                "IsArchived" boolean NOT NULL DEFAULT false,
                CONSTRAINT "PK_tax_codes" PRIMARY KEY ("Id")
            );

            CREATE UNIQUE INDEX IF NOT EXISTS "IX_tax_codes_CompanyId_Code"
                ON sales.tax_codes ("CompanyId", "Code");

            CREATE TABLE IF NOT EXISTS sales.sales_posting_profiles (
                "Id" uuid NOT NULL,
                "CompanyId" uuid NOT NULL,
                "AccountsReceivableAccountId" uuid NOT NULL,
                "SalesRevenueAccountId" uuid NOT NULL,
                "SalesDiscountAccountId" uuid NOT NULL,
                "VatPayableAccountId" uuid NULL,
                "InventoryAccountId" uuid NOT NULL,
                "CogsAccountId" uuid NOT NULL,
                "RoundingAccountId" uuid NULL,
                "CreatedAt" timestamp with time zone NOT NULL DEFAULT NOW(),
                "UpdatedAt" timestamp with time zone NULL,
                "IsActive" boolean NOT NULL DEFAULT true,
                "IsArchived" boolean NOT NULL DEFAULT false,
                CONSTRAINT "PK_sales_posting_profiles" PRIMARY KEY ("Id")
            );

            CREATE UNIQUE INDEX IF NOT EXISTS "IX_sales_posting_profiles_CompanyId"
                ON sales.sales_posting_profiles ("CompanyId");

            ALTER TABLE sales.sales_invoices
                ADD COLUMN IF NOT EXISTS "RoundingDifference" numeric(18,4) NOT NULL DEFAULT 0,
                ADD COLUMN IF NOT EXISTS "IsLegacyUntaxed" boolean NOT NULL DEFAULT false;

            ALTER TABLE sales.sales_invoice_items
                ADD COLUMN IF NOT EXISTS "TaxCodeId" uuid NULL;

            CREATE INDEX IF NOT EXISTS "IX_sales_invoice_items_TaxCodeId"
                ON sales.sales_invoice_items ("TaxCodeId");

            CREATE TABLE IF NOT EXISTS sales.sales_invoice_item_taxes (
                "Id" uuid NOT NULL,
                "SalesInvoiceId" uuid NOT NULL,
                "SalesInvoiceItemId" uuid NOT NULL,
                "TaxCodeId" uuid NULL,
                "TaxCode" character varying(20) NULL,
                "TaxName" character varying(200) NULL,
                "TaxRate" numeric(9,6) NOT NULL DEFAULT 0,
                "TaxableAmount" numeric(18,2) NOT NULL DEFAULT 0,
                "TaxAmount" numeric(18,2) NOT NULL DEFAULT 0,
                "IsInclusive" boolean NOT NULL DEFAULT false,
                "SalesTaxAccountId" uuid NULL,
                "IsFrozen" boolean NOT NULL DEFAULT false,
                "CreatedAt" timestamp with time zone NOT NULL DEFAULT NOW(),
                "UpdatedAt" timestamp with time zone NULL,
                "IsActive" boolean NOT NULL DEFAULT true,
                "IsArchived" boolean NOT NULL DEFAULT false,
                CONSTRAINT "PK_sales_invoice_item_taxes" PRIMARY KEY ("Id")
            );

            CREATE UNIQUE INDEX IF NOT EXISTS "IX_sales_invoice_item_taxes_ItemId"
                ON sales.sales_invoice_item_taxes ("SalesInvoiceItemId");

            CREATE INDEX IF NOT EXISTS "IX_sales_invoice_item_taxes_InvoiceId"
                ON sales.sales_invoice_item_taxes ("SalesInvoiceId");

            ALTER TABLE sales.sales_returns
                ADD COLUMN IF NOT EXISTS "TaxTotal" numeric(18,2) NOT NULL DEFAULT 0,
                ADD COLUMN IF NOT EXISTS "IsLegacyUntaxedReturn" boolean NOT NULL DEFAULT false;

            CREATE TABLE IF NOT EXISTS sales.sales_return_line_taxes (
                "Id" uuid NOT NULL,
                "SalesReturnId" uuid NOT NULL,
                "SalesReturnLineId" uuid NOT NULL,
                "TaxCodeId" uuid NULL,
                "TaxCode" character varying(20) NULL,
                "TaxRate" numeric(9,6) NOT NULL DEFAULT 0,
                "TaxableAmount" numeric(18,2) NOT NULL DEFAULT 0,
                "TaxAmount" numeric(18,2) NOT NULL DEFAULT 0,
                "SalesTaxAccountId" uuid NULL,
                "CreatedAt" timestamp with time zone NOT NULL DEFAULT NOW(),
                "UpdatedAt" timestamp with time zone NULL,
                "IsActive" boolean NOT NULL DEFAULT true,
                "IsArchived" boolean NOT NULL DEFAULT false,
                CONSTRAINT "PK_sales_return_line_taxes" PRIMARY KEY ("Id")
            );

            CREATE UNIQUE INDEX IF NOT EXISTS "IX_sales_return_line_taxes_LineId"
                ON sales.sales_return_line_taxes ("SalesReturnLineId");

            -- Flag all pre-Phase-2 invoices as legacy untaxed (no backfill of tax amounts).
            UPDATE sales.sales_invoices
            SET "IsLegacyUntaxed" = true
            WHERE "IsLegacyUntaxed" = false;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            DROP TABLE IF EXISTS sales.sales_return_line_taxes;
            DROP TABLE IF EXISTS sales.sales_invoice_item_taxes;
            DROP TABLE IF EXISTS sales.sales_posting_profiles;
            DROP TABLE IF EXISTS sales.tax_codes;
            ALTER TABLE sales.sales_returns DROP COLUMN IF EXISTS "TaxTotal";
            ALTER TABLE sales.sales_returns DROP COLUMN IF EXISTS "IsLegacyUntaxedReturn";
            ALTER TABLE sales.sales_invoice_items DROP COLUMN IF EXISTS "TaxCodeId";
            ALTER TABLE sales.sales_invoices DROP COLUMN IF EXISTS "RoundingDifference";
            ALTER TABLE sales.sales_invoices DROP COLUMN IF EXISTS "IsLegacyUntaxed";
            """);
    }
}
