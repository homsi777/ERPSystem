SELECT c."Type", c."CreditLimitEnabled", c."Balance", c."CreditLimit", c."NameAr"
FROM parties.customers c
JOIN sales.sales_invoices i ON i."CustomerId" = c."Id"
WHERE i."Id" = 'f75efd1a-a83d-4a53-849a-20f42760782d';
