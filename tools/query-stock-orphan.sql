SELECT COUNT(*) AS stock_rows,
  COUNT(*) FILTER (WHERE NOT EXISTS (
    SELECT 1 FROM public."FabricRolls" r
    WHERE r."WarehouseId" = w."WarehouseId"
      AND r."FabricItemId" = w."FabricItemId"
      AND r."FabricColorId" = w."FabricColorId"
      AND r."ContainerId" = w."ContainerId"
      AND r."RemainingLengthMeters" > 0 AND r."Status" = 0
  )) AS stocks_without_available_rolls
FROM inventory.warehouse_stocks w
WHERE w."TotalMeters" > 0;
