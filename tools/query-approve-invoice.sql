\pset format aligned
\pset border 2

SELECT i."InvoiceNumber", i."Id", i."Status", i."WarehouseId", i."GrandTotal",
       s."Status" AS detailing_status, s."CompletedAt"
FROM sales.sales_invoices i
LEFT JOIN sales.warehouse_detailing_sessions s ON s."SalesInvoiceId" = i."Id"
WHERE i."Id" = 'f75efd1a-a83d-4a53-849a-20f42760782d';

SELECT r."Id", r."RollSequence", r."DraftRollNumber", r."DraftLengthMeters",
       r."RollNumber", r."LengthMeters", r."FabricRollId", i."LineNumber"
FROM sales.sales_invoice_roll_details r
JOIN sales.sales_invoice_items i ON i."Id" = r."SalesInvoiceItemId"
WHERE i."SalesInvoiceId" = 'f75efd1a-a83d-4a53-849a-20f42760782d'
ORDER BY i."LineNumber", r."RollSequence";
