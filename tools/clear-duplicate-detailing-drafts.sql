-- Clear detailing drafts on invoices that already have duplicate serials in draft.
-- This is data recovery after the uniqueness bug allowed corrupt drafts; not a product "patch".
\pset format aligned
\pset border 2

WITH invoice_dups AS (
  SELECT i."SalesInvoiceId", r."DraftRollNumber", COUNT(*) AS cnt
  FROM sales.sales_invoice_roll_details r
  JOIN sales.sales_invoice_items i ON i."Id" = r."SalesInvoiceItemId"
  WHERE r."DraftRollNumber" IS NOT NULL AND r."DraftRollNumber" > 0
  GROUP BY i."SalesInvoiceId", r."DraftRollNumber"
  HAVING COUNT(*) > 1
),
affected_invoices AS (
  SELECT DISTINCT "SalesInvoiceId" FROM invoice_dups
)
SELECT inv."InvoiceNumber", inv."Id", d."DraftRollNumber", d.cnt
FROM invoice_dups d
JOIN sales.sales_invoices inv ON inv."Id" = d."SalesInvoiceId"
ORDER BY inv."InvoiceNumber", d."DraftRollNumber";

UPDATE sales.sales_invoice_roll_details r
SET "DraftRollNumber" = NULL,
    "DraftLengthMeters" = NULL
FROM sales.sales_invoice_items i
JOIN (
  SELECT DISTINCT i2."SalesInvoiceId"
  FROM sales.sales_invoice_roll_details r2
  JOIN sales.sales_invoice_items i2 ON i2."Id" = r2."SalesInvoiceItemId"
  WHERE r2."DraftRollNumber" IS NOT NULL AND r2."DraftRollNumber" > 0
  GROUP BY i2."SalesInvoiceId", r2."DraftRollNumber"
  HAVING COUNT(*) > 1
) bad ON bad."SalesInvoiceId" = i."SalesInvoiceId"
WHERE r."SalesInvoiceItemId" = i."Id"
  AND (r."DraftRollNumber" IS NOT NULL OR r."DraftLengthMeters" IS NOT NULL);

SELECT COUNT(*) AS remaining_duplicate_groups
FROM (
  SELECT i."SalesInvoiceId", r."DraftRollNumber"
  FROM sales.sales_invoice_roll_details r
  JOIN sales.sales_invoice_items i ON i."Id" = r."SalesInvoiceItemId"
  WHERE r."DraftRollNumber" IS NOT NULL AND r."DraftRollNumber" > 0
  GROUP BY i."SalesInvoiceId", r."DraftRollNumber"
  HAVING COUNT(*) > 1
) x;
