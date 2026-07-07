using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERPSystem.Infrastructure.Migrations;

public partial class AddMovementReportIndexes : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            CREATE INDEX IF NOT EXISTS idx_stock_movements_warehouse_date
                ON public."StockMovements" ("WarehouseId", "MovementDate" DESC);

            CREATE INDEX IF NOT EXISTS idx_stock_movements_date
                ON public."StockMovements" ("MovementDate" DESC);

            CREATE INDEX IF NOT EXISTS idx_stock_movement_lines_movement_fabric
                ON inventory.stock_movement_lines ("MovementId", "FabricItemId", "FabricRollId");
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            DROP INDEX IF EXISTS inventory.idx_stock_movement_lines_movement_fabric;
            DROP INDEX IF EXISTS public.idx_stock_movements_date;
            DROP INDEX IF EXISTS public.idx_stock_movements_warehouse_date;
            """);
    }
}
