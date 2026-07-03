using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERPSystem.Infrastructure.Migrations;

public partial class AddSalesReturnsAndDelivery : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            -- Extend sales_invoices with delivery details
            ALTER TABLE sales.sales_invoices ADD COLUMN IF NOT EXISTS "DeliveredToName" character varying(200);
            ALTER TABLE sales.sales_invoices ADD COLUMN IF NOT EXISTS "DeliveryDriverName" character varying(200);
            ALTER TABLE sales.sales_invoices ADD COLUMN IF NOT EXISTS "DeliveryNotes" character varying(1000);

            -- Sales returns (credit notes)
            CREATE TABLE IF NOT EXISTS sales.sales_returns (
                "Id" uuid NOT NULL,
                "CompanyId" uuid NOT NULL,
                "BranchId" uuid NOT NULL,
                "ReturnNumber" character varying(50) NOT NULL,
                "OriginalInvoiceId" uuid NOT NULL,
                "OriginalInvoiceNumber" character varying(50) NOT NULL DEFAULT '',
                "CustomerId" uuid NOT NULL,
                "WarehouseId" uuid NOT NULL,
                "ReturnDate" timestamp with time zone NOT NULL,
                "Reason" integer NOT NULL DEFAULT 0,
                "ReasonNotes" character varying(500) NULL,
                "Notes" character varying(1000) NULL,
                "Status" integer NOT NULL DEFAULT 0,
                "TotalAmount" numeric(18,2) NOT NULL DEFAULT 0,
                "PostedByUserId" uuid NULL,
                "PostedAt" timestamp with time zone NULL,
                "JournalEntryNumber" character varying(50) NULL,
                "CreatedAt" timestamp with time zone NOT NULL DEFAULT NOW(),
                "CreatedByUserId" uuid NULL,
                "UpdatedAt" timestamp with time zone NULL,
                "UpdatedByUserId" uuid NULL,
                "IsActive" boolean NOT NULL DEFAULT TRUE,
                "IsArchived" boolean NOT NULL DEFAULT FALSE,
                "CancelledAt" timestamp with time zone NULL,
                "CancelledByUserId" uuid NULL,
                "CancelReason" character varying(500) NULL,
                CONSTRAINT "PK_sales_returns" PRIMARY KEY ("Id")
            );
            CREATE UNIQUE INDEX IF NOT EXISTS "IX_sales_returns_CompanyId_ReturnNumber"
                ON sales.sales_returns ("CompanyId", "ReturnNumber");
            CREATE INDEX IF NOT EXISTS "IX_sales_returns_OriginalInvoiceId" ON sales.sales_returns ("OriginalInvoiceId");
            CREATE INDEX IF NOT EXISTS "IX_sales_returns_CustomerId" ON sales.sales_returns ("CustomerId");

            CREATE TABLE IF NOT EXISTS sales.sales_return_lines (
                "Id" uuid NOT NULL,
                "SalesReturnId" uuid NOT NULL,
                "LineNumber" integer NOT NULL,
                "OriginalInvoiceItemId" uuid NOT NULL,
                "FabricItemId" uuid NOT NULL,
                "FabricColorId" uuid NOT NULL,
                "OriginalMeters" numeric(18,4) NOT NULL DEFAULT 0,
                "ReturnMeters" numeric(18,4) NOT NULL DEFAULT 0,
                "UnitPrice" numeric(18,2) NOT NULL DEFAULT 0,
                "LineTotal" numeric(18,2) NOT NULL DEFAULT 0,
                "CreatedAt" timestamp with time zone NOT NULL DEFAULT NOW(),
                "CreatedByUserId" uuid NULL,
                "UpdatedAt" timestamp with time zone NULL,
                "UpdatedByUserId" uuid NULL,
                "IsActive" boolean NOT NULL DEFAULT TRUE,
                "IsArchived" boolean NOT NULL DEFAULT FALSE,
                CONSTRAINT "PK_sales_return_lines" PRIMARY KEY ("Id")
            );
            CREATE UNIQUE INDEX IF NOT EXISTS "IX_sales_return_lines_SalesReturnId_LineNumber"
                ON sales.sales_return_lines ("SalesReturnId", "LineNumber");

            -- Receipt-to-invoice application junction
            CREATE TABLE IF NOT EXISTS sales.receipt_invoice_payments (
                "Id" uuid NOT NULL,
                "SalesInvoiceId" uuid NOT NULL,
                "ReceiptVoucherId" uuid NOT NULL,
                "Amount" numeric(18,2) NOT NULL DEFAULT 0,
                "AppliedAt" timestamp with time zone NOT NULL,
                "CreatedAt" timestamp with time zone NOT NULL DEFAULT NOW(),
                "CreatedByUserId" uuid NULL,
                "UpdatedAt" timestamp with time zone NULL,
                "UpdatedByUserId" uuid NULL,
                "IsActive" boolean NOT NULL DEFAULT TRUE,
                "IsArchived" boolean NOT NULL DEFAULT FALSE,
                CONSTRAINT "PK_receipt_invoice_payments" PRIMARY KEY ("Id")
            );
            CREATE INDEX IF NOT EXISTS "IX_receipt_invoice_payments_SalesInvoiceId"
                ON sales.receipt_invoice_payments ("SalesInvoiceId");
            CREATE INDEX IF NOT EXISTS "IX_receipt_invoice_payments_ReceiptVoucherId"
                ON sales.receipt_invoice_payments ("ReceiptVoucherId");
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            DROP TABLE IF EXISTS sales.receipt_invoice_payments;
            DROP TABLE IF EXISTS sales.sales_return_lines;
            DROP TABLE IF EXISTS sales.sales_returns;
            ALTER TABLE sales.sales_invoices DROP COLUMN IF EXISTS "DeliveredToName";
            ALTER TABLE sales.sales_invoices DROP COLUMN IF EXISTS "DeliveryDriverName";
            ALTER TABLE sales.sales_invoices DROP COLUMN IF EXISTS "DeliveryNotes";
            """);
    }
}
