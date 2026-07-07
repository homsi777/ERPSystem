using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERPSystem.Infrastructure.Migrations;

public partial class AddPerformanceIndexes : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            CREATE INDEX IF NOT EXISTS idx_fabric_rolls_warehouse_status
                ON public."FabricRolls" ("WarehouseId", "Status", "RemainingLengthMeters", "RollNumber");

            CREATE INDEX IF NOT EXISTS idx_fabric_rolls_warehouse_color
                ON public."FabricRolls" ("WarehouseId", "FabricColorId", "RemainingLengthMeters");

            CREATE INDEX IF NOT EXISTS idx_fabric_rolls_container
                ON public."FabricRolls" ("ContainerId", "Status");

            CREATE INDEX IF NOT EXISTS idx_fabric_rolls_status
                ON public."FabricRolls" ("Status");

            CREATE INDEX IF NOT EXISTS idx_fabric_rolls_available_partial
                ON public."FabricRolls" ("WarehouseId", "Status", "RollNumber")
                WHERE "Status" = 0 AND "RemainingLengthMeters" > 0;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            DROP INDEX IF EXISTS public.idx_fabric_rolls_available_partial;
            DROP INDEX IF EXISTS public.idx_fabric_rolls_status;
            DROP INDEX IF EXISTS public.idx_fabric_rolls_container;
            DROP INDEX IF EXISTS public.idx_fabric_rolls_warehouse_color;
            DROP INDEX IF EXISTS public.idx_fabric_rolls_warehouse_status;
            """);
    }
}
