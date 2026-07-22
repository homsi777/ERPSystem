using ERPSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERPSystem.Infrastructure.Migrations;

[DbContext(typeof(ErpDbContext))]
[Migration("20260728120000_AddOpeningStockContainerNumber")]
public partial class AddOpeningStockContainerNumber : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            ALTER TABLE finance.opening_balance_lines
                ADD COLUMN IF NOT EXISTS "ContainerNumber" character varying(100) NULL;

            ALTER TABLE inventory.opening_stock_lines
                ADD COLUMN IF NOT EXISTS "ContainerId" uuid NULL;

            CREATE INDEX IF NOT EXISTS "IX_opening_stock_lines_ContainerId"
                ON inventory.opening_stock_lines ("ContainerId");
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            DROP INDEX IF EXISTS inventory."IX_opening_stock_lines_ContainerId";
            ALTER TABLE inventory.opening_stock_lines DROP COLUMN IF EXISTS "ContainerId";
            ALTER TABLE finance.opening_balance_lines DROP COLUMN IF EXISTS "ContainerNumber";
            """);
    }
}
