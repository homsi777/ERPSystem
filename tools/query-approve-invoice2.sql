\pset format aligned
\pset border 2

SELECT column_name, data_type
FROM information_schema.columns
WHERE table_schema = 'sales' AND table_name = 'sales_invoice_roll_details'
ORDER BY ordinal_position;

SELECT r."Id", r."RollSequence", r."DraftRollNumber", r."DraftLengthMeters",
       r."LengthMeters", r."FabricRollId", i."LineNumber"
FROM sales.sales_invoice_roll_details r
JOIN sales.sales_invoice_items i ON i."Id" = r."SalesInvoiceItemId"
WHERE i."SalesInvoiceId" = 'f75efd1a-a83d-4a53-849a-20f42760782d'
ORDER BY i."LineNumber", r."RollSequence";

SELECT * FROM sales.warehouse_detailing_sessions
WHERE "SalesInvoiceId" = 'f75efd1a-a83d-4a53-849a-20f42760782d';
