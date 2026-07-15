SELECT "Code", "NameAr", "Balance", "OpeningBalancePosted", "Id"
FROM parties.customers WHERE "Code" = '5006';

SELECT l."PartyId", l."PartyName", l."Debit", l."Credit", d."Status", d."PostedAt", d."Type"
FROM finance.opening_balance_lines l
JOIN finance.opening_balance_documents d ON d."Id" = l."DocumentId"
WHERE l."PartyName" ILIKE '%شيخو%' OR l."PartyId" IN (SELECT "Id" FROM parties.customers WHERE "Code" = '5006');

SELECT jel."PartyId", jel."Debit", jel."Credit", je."SourceType", je."Status"
FROM finance.journal_entry_lines jel
JOIN finance.journal_entries je ON je."Id" = jel."JournalEntryId"
WHERE jel."PartyId" IN (SELECT "Id" FROM parties.customers WHERE "Code" = '5006');

SELECT "CustomerId", "Amount", "Status", "VoucherDate"
FROM finance.receipt_vouchers
WHERE "CustomerId" IN (SELECT "Id" FROM parties.customers WHERE "Code" = '5006');
