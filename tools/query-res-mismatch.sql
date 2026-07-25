\pset format aligned
\pset border 2

SELECT ir."FabricRollId", ir."Status" AS res_status, ir."ReservedMeters", ir."Strategy",
       fr."Status" AS roll_status, fr."RollNumber"
FROM inventory.inventory_reservations ir
JOIN public."FabricRolls" fr ON fr."Id" = ir."FabricRollId"
WHERE ir."ReferenceId" = 'f75efd1a-a83d-4a53-849a-20f42760782d'
  AND fr."Status" = 0
ORDER BY fr."RollNumber";

SELECT ir."Status", COUNT(*)
FROM inventory.inventory_reservations ir
WHERE ir."ReferenceId" = 'f75efd1a-a83d-4a53-849a-20f42760782d'
GROUP BY ir."Status";

-- Were these 10 ever released by another invoice?
SELECT fr."Id", fr."RollNumber", fr."Status",
       (SELECT COUNT(*) FROM inventory.inventory_reservations x WHERE x."FabricRollId" = fr."Id") AS all_reservations
FROM public."FabricRolls" fr
WHERE fr."Id" IN (
  'c1c88d58-e9ee-47b9-8b1f-c41ec18a2b9d','30446587-08d9-4fac-9e94-aeb7fb420fdc',
  'ec691e27-854b-47e2-86db-11f2c84075f6','ca85bca0-1ac0-4c9e-b369-8f9b1c60a77a',
  '047a1653-0268-457b-b524-2aebe7e1c78d','0452ffc9-91d1-4e47-af12-890f7110e119',
  '27944848-425a-4802-8ce8-41e6a01cc0dc','381bffe6-0931-41d2-a69c-3ead1b451d26',
  '83545e23-01ac-4386-a087-f25b8c232b2e','7d109f2c-a46c-453f-a978-9ffcb398ae3e'
);
