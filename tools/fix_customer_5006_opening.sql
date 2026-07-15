-- Reassign opening balance from duplicate customer (CUS-MAIN-000004 / احمد شيجو)
-- to the correct customer 5006 (احمد شيخو).
BEGIN;

UPDATE finance.opening_balance_lines
SET "PartyId" = '99e424df-7dad-4a20-82ab-6df8ac9f6a5b',
    "PartyName" = 'احمد شيخو'
WHERE "DocumentId" = 'c295ebfe-460d-4944-995d-32ce4796c3ee';

UPDATE parties.customers
SET "IsActive" = false
WHERE "Id" = '347a5534-3c1d-44ef-a3b2-20d95c586a19'
  AND "Code" = 'CUS-MAIN-000004';

COMMIT;

SELECT c."Code", c."NameAr", l."Debit", l."Credit", d."Status", d."PostedAt"
FROM finance.opening_balance_lines l
JOIN finance.opening_balance_documents d ON d."Id" = l."DocumentId"
JOIN parties.customers c ON c."Id" = l."PartyId"
WHERE c."Code" = '5006';
