using ERPSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERPSystem.Infrastructure.Migrations;

[DbContext(typeof(ErpDbContext))]
[Migration("20260724120000_AddInventoryOpeningBalanceLegacyRollSupport")]
public partial class AddInventoryOpeningBalanceLegacyRollSupport : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<Guid>(
            name: "FabricItemId",
            schema: "finance",
            table: "opening_balance_lines",
            type: "uuid",
            nullable: true);

        migrationBuilder.AddColumn<Guid>(
            name: "FabricColorId",
            schema: "finance",
            table: "opening_balance_lines",
            type: "uuid",
            nullable: true);

        migrationBuilder.AddColumn<bool>(
            name: "IsLegacyOpeningBalance",
            table: "FabricRolls",
            type: "boolean",
            nullable: false,
            defaultValue: false);

        migrationBuilder.AddColumn<bool>(
            name: "LegacyLengthConfirmed",
            table: "FabricRolls",
            type: "boolean",
            nullable: false,
            defaultValue: false);

        // Existing container-less rolls came only from the legacy opening-stock path.
        // Tagging is metadata-only; no length, stock, valuation or accounting value changes.
        migrationBuilder.Sql("""
            UPDATE public."FabricRolls"
            SET "IsLegacyOpeningBalance" = TRUE,
                "LegacyLengthConfirmed" = FALSE
            WHERE "ContainerId" = '00000000-0000-0000-0000-000000000000';
            """);

        migrationBuilder.CreateIndex(
            name: "IX_opening_balance_lines_FabricItemId_FabricColorId",
            schema: "finance",
            table: "opening_balance_lines",
            columns: new[] { "FabricItemId", "FabricColorId" });

        migrationBuilder.CreateIndex(
            name: "idx_fabric_rolls_legacy_opening",
            table: "FabricRolls",
            columns: new[] { "IsLegacyOpeningBalance", "WarehouseId", "Status" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_opening_balance_lines_FabricItemId_FabricColorId",
            schema: "finance",
            table: "opening_balance_lines");

        migrationBuilder.DropIndex(
            name: "idx_fabric_rolls_legacy_opening",
            table: "FabricRolls");

        migrationBuilder.DropColumn(
            name: "FabricItemId",
            schema: "finance",
            table: "opening_balance_lines");

        migrationBuilder.DropColumn(
            name: "FabricColorId",
            schema: "finance",
            table: "opening_balance_lines");

        migrationBuilder.DropColumn(
            name: "IsLegacyOpeningBalance",
            table: "FabricRolls");

        migrationBuilder.DropColumn(
            name: "LegacyLengthConfirmed",
            table: "FabricRolls");
    }
}
