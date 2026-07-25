\pset format aligned
\pset border 2

SELECT column_name FROM information_schema.columns
WHERE table_name = 'FabricRolls' AND column_name ILIKE '%reserv%' OR (table_name = 'FabricRolls' AND column_name ILIKE '%status%')
ORDER BY 1;

SELECT r."Status", r."ReservationStatus", COUNT(*) 
FROM public."FabricRolls" r
WHERE r."Id" IN (
  SELECT rd."FabricRollId"
  FROM sales.sales_invoice_roll_details rd
  JOIN sales.sales_invoice_items i ON i."Id" = rd."SalesInvoiceItemId"
  WHERE i."SalesInvoiceId" = 'f75efd1a-a83d-4a53-849a-20f42760782d'
)
GROUP BY r."Status", r."ReservationStatus";

SELECT COUNT(*) AS reservations
FROM inventory.inventory_reservations
WHERE "ReferenceId" = 'f75efd1a-a83d-4a53-849a-20f42760782d';

SELECT c."NameAr", c."CreditLimit", i."GrandTotal", i."PaymentType", i."PartialPaymentAmount", i."CashboxId"
FROM sales.sales_invoices i
JOIN parties.customers c ON c."Id" = i."CustomerId"
WHERE i."Id" = 'f75efd1a-a83d-4a53-849a-20f42760782d';
