\pset format aligned
\pset border 2

-- Sample of assigned rolls: status, remaining vs reserved
SELECT r."Id", r."RollNumber", r."Status", r."RemainingLengthMeters", r."ReservationStatus",
       r."ReservedInSalesInvoiceId", r."WarehouseId", r."CostPerMeter"
FROM public."FabricRolls" r
WHERE r."Id" IN (
  SELECT rd."FabricRollId"
  FROM sales.sales_invoice_roll_details rd
  JOIN sales.sales_invoice_items i ON i."Id" = rd."SalesInvoiceItemId"
  WHERE i."SalesInvoiceId" = 'f75efd1a-a83d-4a53-849a-20f42760782d'
)
ORDER BY r."RollNumber"
LIMIT 15;

SELECT
  COUNT(*) AS rolls,
  COUNT(*) FILTER (WHERE r."Status" <> 0) AS not_available,
  COUNT(*) FILTER (WHERE r."RemainingLengthMeters" < 109.728) AS short_length,
  COUNT(*) FILTER (WHERE r."ReservedInSalesInvoiceId" IS NOT NULL
    AND r."ReservedInSalesInvoiceId" <> 'f75efd1a-a83d-4a53-849a-20f42760782d') AS reserved_elsewhere
FROM public."FabricRolls" r
WHERE r."Id" IN (
  SELECT rd."FabricRollId"
  FROM sales.sales_invoice_roll_details rd
  JOIN sales.sales_invoice_items i ON i."Id" = rd."SalesInvoiceItemId"
  WHERE i."SalesInvoiceId" = 'f75efd1a-a83d-4a53-849a-20f42760782d'
);

-- Customer credit
SELECT c."NameAr", c."CreditLimit", c."CurrentBalance",
       i."GrandTotal", i."PaymentType", i."PartialPaymentAmount", i."CashboxId"
FROM sales.sales_invoices i
JOIN parties.customers c ON c."Id" = i."CustomerId"
WHERE i."Id" = 'f75efd1a-a83d-4a53-849a-20f42760782d';
