using ERPSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERPSystem.Infrastructure.Migrations;

/// <summary>
/// Corrective Phase 3 schema alignment for databases that applied the initial
/// 20260722120000_AddPhase3FinanceModule migration (Id-only payment_methods PK,
/// missing audit columns). Safe and idempotent on fresh installs.
/// </summary>
[DbContext(typeof(ErpDbContext))]
[Migration("20260722121000_FixPhase3FinanceSchema")]
public partial class FixPhase3FinanceSchema : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            ALTER TABLE finance.payment_methods
                ADD COLUMN IF NOT EXISTS "CreatedByUserId" uuid NULL,
                ADD COLUMN IF NOT EXISTS "UpdatedByUserId" uuid NULL;

            ALTER TABLE finance.bank_accounts
                ADD COLUMN IF NOT EXISTS "CreatedByUserId" uuid NULL,
                ADD COLUMN IF NOT EXISTS "UpdatedByUserId" uuid NULL;

            ALTER TABLE finance.receipt_tender_lines
                ADD COLUMN IF NOT EXISTS "CreatedByUserId" uuid NULL,
                ADD COLUMN IF NOT EXISTS "UpdatedByUserId" uuid NULL;

            DO $$
            BEGIN
                IF EXISTS (
                    SELECT 1
                    FROM pg_constraint c
                    JOIN pg_class t ON t.oid = c.conrelid
                    JOIN pg_namespace n ON n.oid = t.relnamespace
                    WHERE n.nspname = 'finance'
                      AND t.relname = 'payment_methods'
                      AND c.conname = 'PK_payment_methods'
                      AND c.contype = 'p'
                      AND array_length(c.conkey, 1) = 1
                ) THEN
                    ALTER TABLE finance.payment_methods DROP CONSTRAINT "PK_payment_methods";
                    ALTER TABLE finance.payment_methods
                        ADD CONSTRAINT "PK_payment_methods" PRIMARY KEY ("CompanyId", "Id");
                END IF;
            END $$;

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
            ON CONFLICT ("CompanyId", "Id") DO NOTHING;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            ALTER TABLE finance.payment_methods
                DROP COLUMN IF EXISTS "CreatedByUserId",
                DROP COLUMN IF EXISTS "UpdatedByUserId";

            ALTER TABLE finance.bank_accounts
                DROP COLUMN IF EXISTS "CreatedByUserId",
                DROP COLUMN IF EXISTS "UpdatedByUserId";

            ALTER TABLE finance.receipt_tender_lines
                DROP COLUMN IF EXISTS "CreatedByUserId",
                DROP COLUMN IF EXISTS "UpdatedByUserId";
            """);
    }
}
