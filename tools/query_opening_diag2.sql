SELECT "Id", "Code", "NameAr", "Balance", "OpeningBalancePosted"
FROM parties.customers
WHERE "Id" = '347a5534-3c1d-44ef-a3b2-20d95c586a19'
   OR "NameAr" ILIKE '%شي%';

SELECT d."Id", d."Number", d."Status", d."PostedAt", d."Type", d."CreatedAt"
FROM finance.opening_balance_documents d
WHERE d."Id" IN (
  SELECT l."DocumentId" FROM finance.opening_balance_lines l
  WHERE l."PartyId" = '347a5534-3c1d-44ef-a3b2-20d95c586a19'
     OR l."PartyName" ILIKE '%شي%'
);

SELECT jel."PartyId", jel."Debit", jel."Credit", je."SourceType", je."Status", je."EntryDate"
FROM accounting.journal_entry_lines jel
JOIN accounting.journal_entries je ON je."Id" = jel."JournalEntryId"
WHERE jel."PartyId" IN ('347a5534-3c1d-44ef-a3b2-20d95c586a19', '99e424df-7dad-4a20-82ab-6df8ac9f6a5b');
