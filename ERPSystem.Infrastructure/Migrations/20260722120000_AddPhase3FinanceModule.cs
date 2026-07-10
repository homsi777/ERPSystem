using ERPSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERPSystem.Infrastructure.Migrations;

/// <summary>Phase 3 — Cashboxes, banks, payment methods, receipt tenders (additive only).</summary>
[DbContext(typeof(ErpDbContext))]
[Migration("20260722120000_AddPhase3FinanceModule")]
public partial class AddPhase3FinanceModule : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            CREATE TABLE IF NOT EXISTS finance.payment_methods (
                "Id" uuid NOT NULL,
                "CompanyId" uuid NOT NULL,
                "Kind" integer NOT NULL,
                "Code" character varying(20) NOT NULL,
                "Name" character varying(100) NOT NULL,
                "RequiresCashbox" boolean NOT NULL DEFAULT false,
                "RequiresBankAccount" boolean NOT NULL DEFAULT false,
                "RequiresReference" boolean NOT NULL DEFAULT false,
                "AllowsMixedTender" boolean NOT NULL DEFAULT false,
                "RequiresClearingAccount" boolean NOT NULL DEFAULT false,
                "IsActive" boolean NOT NULL DEFAULT true,
                "CreatedAt" timestamp with time zone NOT NULL DEFAULT NOW(),
                "UpdatedAt" timestamp with time zone NULL,
                "IsArchived" boolean NOT NULL DEFAULT false,
                CONSTRAINT "PK_payment_methods" PRIMARY KEY ("Id")
            );
            CREATE UNIQUE INDEX IF NOT EXISTS "IX_payment_methods_CompanyId_Code"
                ON finance.payment_methods ("CompanyId", "Code");

            CREATE TABLE IF NOT EXISTS finance.bank_accounts (
                "Id" uuid NOT NULL,
                "CompanyId" uuid NOT NULL,
                "BranchId" uuid NOT NULL,
                "Code" character varying(20) NOT NULL,
                "Name" character varying(200) NOT NULL,
                "BankName" character varying(200) NOT NULL,
                "Iban" character varying(34) NULL,
                "AccountNumberMasked" character varying(20) NULL,
                "GlAccountId" uuid NOT NULL,
                "Currency" character varying(3) NOT NULL DEFAULT 'USD',
                "IsActive" boolean NOT NULL DEFAULT true,
                "CreatedAt" timestamp with time zone NOT NULL DEFAULT NOW(),
                "UpdatedAt" timestamp with time zone NULL,
                "IsArchived" boolean NOT NULL DEFAULT false,
                CONSTRAINT "PK_bank_accounts" PRIMARY KEY ("Id")
            );
            CREATE UNIQUE INDEX IF NOT EXISTS "IX_bank_accounts_CompanyId_Code"
                ON finance.bank_accounts ("CompanyId", "Code");

            ALTER TABLE finance.cashboxes
                ADD COLUMN IF NOT EXISTS "CompanyId" uuid NULL,
                ADD COLUMN IF NOT EXISTS "AllowNegativeBalance" boolean NOT NULL DEFAULT false,
                ADD COLUMN IF NOT EXISTS "OpeningDate" date NULL;

            UPDATE finance.cashboxes c
            SET "CompanyId" = b."CompanyId"
            FROM company.branches b
            WHERE c."BranchId" = b."Id" AND c."CompanyId" IS NULL;

            ALTER TABLE finance.receipt_vouchers
                ADD COLUMN IF NOT EXISTS "PaymentMethodId" uuid NULL,
                ADD COLUMN IF NOT EXISTS "ReversalOfId" uuid NULL,
                ADD COLUMN IF NOT EXISTS "ReversalReason" character varying(500) NULL,
                ADD COLUMN IF NOT EXISTS "ReversedAt" timestamp with time zone NULL,
                ADD COLUMN IF NOT EXISTS "PostedByUserId" uuid NULL,
                ADD COLUMN IF NOT EXISTS "ApprovedAt" timestamp with time zone NULL,
                ADD COLUMN IF NOT EXISTS "SubmittedAt" timestamp with time zone NULL,
                ADD COLUMN IF NOT EXISTS "IdempotencyKey" character varying(100) NULL;

            CREATE INDEX IF NOT EXISTS "IX_receipt_vouchers_ReversalOfId"
                ON finance.receipt_vouchers ("ReversalOfId");
            CREATE UNIQUE INDEX IF NOT EXISTS "IX_receipt_vouchers_IdempotencyKey"
                ON finance.receipt_vouchers ("IdempotencyKey")
                WHERE "IdempotencyKey" IS NOT NULL;

            CREATE TABLE IF NOT EXISTS finance.receipt_tender_lines (
                "Id" uuid NOT NULL,
                "ReceiptVoucherId" uuid NOT NULL,
                "PaymentMethodId" uuid NOT NULL,
                "CashboxId" uuid NULL,
                "BankAccountId" uuid NULL,
                "Amount" numeric(18,2) NOT NULL,
                "Currency" character varying(3) NOT NULL DEFAULT 'USD',
                "ExchangeRate" numeric(18,6) NOT NULL DEFAULT 1,
                "BaseAmount" numeric(18,2) NOT NULL,
                "Reference" character varying(100) NULL,
                "ChequeNumber" character varying(50) NULL,
                "ChequeDate" date NULL,
                "CardReference" character varying(50) NULL,
                "CreatedAt" timestamp with time zone NOT NULL DEFAULT NOW(),
                "UpdatedAt" timestamp with time zone NULL,
                "IsActive" boolean NOT NULL DEFAULT true,
                "IsArchived" boolean NOT NULL DEFAULT false,
                CONSTRAINT "PK_receipt_tender_lines" PRIMARY KEY ("Id"),
                CONSTRAINT "FK_receipt_tender_lines_receipt_vouchers"
                    FOREIGN KEY ("ReceiptVoucherId") REFERENCES finance.receipt_vouchers ("Id") ON DELETE RESTRICT
            );
            CREATE INDEX IF NOT EXISTS "IX_receipt_tender_lines_ReceiptVoucherId"
                ON finance.receipt_tender_lines ("ReceiptVoucherId");

            INSERT INTO finance.payment_methods
                ("Id", "CompanyId", "Kind", "Code", "Name", "RequiresCashbox", "RequiresBankAccount", "RequiresReference", "AllowsMixedTender")
            SELECT v."Id", c."Id", v."Kind", v."Code", v."Name", v."RequiresCashbox", v."RequiresBankAccount", v."RequiresReference", v."AllowsMixedTender"
            FROM company.companies c
            CROSS JOIN (VALUES
                ('f1000001-0001-0001-0001-000000000001'::uuid, 0, 'CASH', 'نقدي', true, false, false, true),
                ('f1000002-0002-0002-0002-000000000002'::uuid, 1, 'BANK', 'تحويل بنكي', false, true, true, true),
                ('f1000003-0003-0003-0003-000000000003'::uuid, 2, 'CARD', 'بطاقة', false, true, true, true),
                ('f1000004-0004-0004-0004-000000000004'::uuid, 3, 'CHEQUE', 'شيك', true, false, true, true),
                ('f1000005-0005-0005-0005-000000000005'::uuid, 4, 'CREDIT', 'رصيد عميل', false, false, false, false),
                ('f1000006-0006-0006-0006-000000000006'::uuid, 5, 'ADVANCE', 'دفعة مقدمة', true, true, false, true),
                ('f1000099-0099-0099-0099-000000000099'::uuid, 99, 'OTHER', 'أخرى', true, true, false, true)
            ) AS v("Id", "Kind", "Code", "Name", "RequiresCashbox", "RequiresBankAccount", "RequiresReference", "AllowsMixedTender")
            ON CONFLICT ("Id") DO NOTHING;

            UPDATE finance.receipt_vouchers
            SET "PaymentMethodId" = 'f1000001-0001-0001-0001-000000000001'
            WHERE "PaymentMethodId" IS NULL;

            INSERT INTO "Accounts" ("Id", "CompanyId", "Code", "NameAr", "NameEn", "AccountType", "ParentId", "IsPostable", "IsActive", "IsArchived", "CreatedAt")
            SELECT 'a1000014-0014-0014-0014-000000001014', '11111111-1111-1111-1111-111111111111',
                   '2150', 'دفعات مقدمة عملاء', 'Customer Advances', 'Liability',
                   'b1000002-0002-0002-0002-000000000002', true, true, false, NOW() AT TIME ZONE 'UTC'
            WHERE NOT EXISTS (SELECT 1 FROM "Accounts" WHERE "Id" = 'a1000014-0014-0014-0014-000000001014');
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            DROP TABLE IF EXISTS finance.receipt_tender_lines;
            ALTER TABLE finance.receipt_vouchers
                DROP COLUMN IF EXISTS "PaymentMethodId",
                DROP COLUMN IF EXISTS "ReversalOfId",
                DROP COLUMN IF EXISTS "ReversalReason",
                DROP COLUMN IF EXISTS "ReversedAt",
                DROP COLUMN IF EXISTS "PostedByUserId",
                DROP COLUMN IF EXISTS "ApprovedAt",
                DROP COLUMN IF EXISTS "SubmittedAt",
                DROP COLUMN IF EXISTS "IdempotencyKey";
            ALTER TABLE finance.cashboxes
                DROP COLUMN IF EXISTS "CompanyId",
                DROP COLUMN IF EXISTS "AllowNegativeBalance",
                DROP COLUMN IF EXISTS "OpeningDate";
            DROP TABLE IF EXISTS finance.bank_accounts;
            DROP TABLE IF EXISTS finance.payment_methods;
            """);
    }
}
