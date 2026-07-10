-- RECOVERY ONLY (test DB). Superseded by migration 20260722121000_FixPhase3FinanceSchema.
-- Prefer: dotnet ef database update
ALTER TABLE finance.payment_methods DROP CONSTRAINT IF EXISTS "PK_payment_methods";
ALTER TABLE finance.payment_methods ADD CONSTRAINT "PK_payment_methods" PRIMARY KEY ("CompanyId", "Id");

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
