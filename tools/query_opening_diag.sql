-- Find opening balance docs with 30000 or customer name match
SELECT d."Id", d."DocumentNumber", d."Status", d."PostedAt", d."Type", d."CompanyId"
FROM finance.opening_balance_documents d
WHERE d."Type" = 1
ORDER BY d."CreatedAt" DESC
LIMIT 20;

SELECT l."PartyId", l."PartyName", l."Debit", l."Credit", d."Status", d."PostedAt"
FROM finance.opening_balance_lines l
JOIN finance.opening_balance_documents d ON d."Id" = l."DocumentId"
WHERE l."Debit" >= 30000 OR l."PartyName" ILIKE '%شيخ%'
ORDER BY d."CreatedAt" DESC;

-- Journal tables schema
SELECT table_schema, table_name FROM information_schema.tables
WHERE table_name ILIKE '%journal%' ORDER BY 1,2;

-- All customers with opening posted flag
SELECT "Code", "NameAr", "Balance", "OpeningBalancePosted"
FROM parties.customers
WHERE "OpeningBalancePosted" = true OR "Balance" > 0
ORDER BY "Code";
