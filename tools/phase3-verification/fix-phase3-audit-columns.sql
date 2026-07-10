-- RECOVERY ONLY (test DB). Superseded by migration 20260722121000_FixPhase3FinanceSchema.
-- Prefer: dotnet ef database update

ALTER TABLE finance.payment_methods
    ADD COLUMN IF NOT EXISTS "CreatedByUserId" uuid NULL,
    ADD COLUMN IF NOT EXISTS "UpdatedByUserId" uuid NULL;

ALTER TABLE finance.bank_accounts
    ADD COLUMN IF NOT EXISTS "CreatedByUserId" uuid NULL,
    ADD COLUMN IF NOT EXISTS "UpdatedByUserId" uuid NULL;

ALTER TABLE finance.receipt_tender_lines
    ADD COLUMN IF NOT EXISTS "CreatedByUserId" uuid NULL,
    ADD COLUMN IF NOT EXISTS "UpdatedByUserId" uuid NULL;
