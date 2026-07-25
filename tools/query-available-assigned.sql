\pset format aligned
\pset border 2

-- Which invoice details point at Available (0) rolls?
SELECT i."LineNumber", rd."RollSequence", rd."LengthMeters",
       fr."RollNumber", fr."Status", fr."ReservationStatus", fr."RemainingLengthMeters",
       fr."Id" AS fabric_roll_id
FROM sales.sales_invoice_roll_details rd
JOIN sales.sales_invoice_items i ON i."Id" = rd."SalesInvoiceItemId"
JOIN public."FabricRolls" fr ON fr."Id" = rd."FabricRollId"
WHERE i."SalesInvoiceId" = 'f75efd1a-a83d-4a53-849a-20f42760782d'
  AND fr."Status" = 0
ORDER BY i."LineNumber", rd."RollSequence";

-- PaymentType meaning + customer AR
SELECT column_name FROM information_schema.columns
WHERE table_schema='parties' AND table_name='customers'
  AND (column_name ILIKE '%balanc%' OR column_name ILIKE '%credit%' OR column_name ILIKE '%receiv%')
ORDER BY 1;
