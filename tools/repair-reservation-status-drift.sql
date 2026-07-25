-- Systemic repair: reservation row says Reserved but fabric roll Status is Available.
BEGIN;

CREATE TEMP TABLE tmp_drift_rolls AS
SELECT fr."Id" AS roll_id,
       fr."WarehouseId",
       fr."ContainerId",
       fr."FabricItemId",
       fr."FabricColorId",
       fr."RemainingLengthMeters" AS meters
FROM public."FabricRolls" fr
WHERE fr."Status" = 0
  AND fr."RemainingLengthMeters" > 0
  AND EXISTS (
    SELECT 1
    FROM inventory.inventory_reservations ir
    WHERE ir."FabricRollId" = fr."Id"
      AND ir."Status" = 1
  );

UPDATE public."FabricRolls" fr
SET "Status" = 1,
    "ReservationStatus" = 1
FROM tmp_drift_rolls d
WHERE fr."Id" = d.roll_id;

WITH roll_totals AS (
  SELECT r."WarehouseId", r."ContainerId", r."FabricItemId", r."FabricColorId",
         COALESCE(SUM(r."RemainingLengthMeters") FILTER (WHERE r."Status" = 1), 0) AS reserved_m,
         COALESCE(SUM(r."RemainingLengthMeters") FILTER (WHERE r."Status" = 0), 0) AS available_m,
         COUNT(*) FILTER (WHERE r."RemainingLengthMeters" > 0 AND r."Status" IN (0, 1)) AS roll_count
  FROM public."FabricRolls" r
  WHERE EXISTS (
    SELECT 1 FROM tmp_drift_rolls d
    WHERE d."WarehouseId" = r."WarehouseId"
      AND d."ContainerId" = r."ContainerId"
      AND d."FabricItemId" = r."FabricItemId"
      AND d."FabricColorId" = r."FabricColorId"
  )
  GROUP BY 1, 2, 3, 4
)
UPDATE inventory.warehouse_stocks s
SET "ReservedMeters" = t.reserved_m,
    "AvailableMeters" = t.available_m,
    "TotalMeters" = t.reserved_m + t.available_m,
    "RollCount" = t.roll_count::int
FROM roll_totals t
WHERE s."WarehouseId" = t."WarehouseId"
  AND s."ContainerId" = t."ContainerId"
  AND s."FabricItemId" = t."FabricItemId"
  AND s."FabricColorId" = t."FabricColorId";

SELECT COUNT(*) AS repaired_rolls FROM tmp_drift_rolls;

SELECT fr."Status", COUNT(*) AS cnt
FROM public."FabricRolls" fr
JOIN sales.sales_invoice_roll_details rd ON rd."FabricRollId" = fr."Id"
JOIN sales.sales_invoice_items i ON i."Id" = rd."SalesInvoiceItemId"
WHERE i."SalesInvoiceId" = 'f75efd1a-a83d-4a53-849a-20f42760782d'
GROUP BY fr."Status";

COMMIT;
