using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERPSystem.Infrastructure.Migrations;

/// <inheritdoc />
public partial class AddContainerFabricTypeLines : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<decimal>(
            name: "Insurance",
            schema: "china_import",
            table: "landing_costs",
            type: "numeric(18,2)",
            precision: 18,
            scale: 2,
            nullable: false,
            defaultValue: 0m);

        migrationBuilder.AddColumn<decimal>(
            name: "OtherExpense1",
            schema: "china_import",
            table: "landing_costs",
            type: "numeric(18,2)",
            precision: 18,
            scale: 2,
            nullable: false,
            defaultValue: 0m);

        migrationBuilder.AddColumn<decimal>(
            name: "OtherExpense2",
            schema: "china_import",
            table: "landing_costs",
            type: "numeric(18,2)",
            precision: 18,
            scale: 2,
            nullable: false,
            defaultValue: 0m);

        migrationBuilder.AddColumn<decimal>(
            name: "OtherExpense3",
            schema: "china_import",
            table: "landing_costs",
            type: "numeric(18,2)",
            precision: 18,
            scale: 2,
            nullable: false,
            defaultValue: 0m);

        migrationBuilder.AddColumn<decimal>(
            name: "OtherExpense4",
            schema: "china_import",
            table: "landing_costs",
            type: "numeric(18,2)",
            precision: 18,
            scale: 2,
            nullable: false,
            defaultValue: 0m);

        migrationBuilder.AddColumn<bool>(
            name: "UsesWeightedAllocation",
            schema: "china_import",
            table: "landing_costs",
            type: "boolean",
            nullable: false,
            defaultValue: false);

        migrationBuilder.CreateTable(
            name: "container_fabric_type_lines",
            schema: "china_import",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                ContainerId = table.Column<Guid>(type: "uuid", nullable: false),
                LineNumber = table.Column<int>(type: "integer", nullable: false),
                TypeDisplayName = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                MatchKey = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                FabricItemId = table.Column<Guid>(type: "uuid", nullable: true),
                FabricColorId = table.Column<Guid>(type: "uuid", nullable: true),
                LengthMeters = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                RollCount = table.Column<int>(type: "integer", nullable: false),
                NetWeightKg = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                Cbm = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                ChinaUnitPriceUsd = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                InvoiceLineAmountUsd = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                ExpenseShareUsd = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                LandedCostPerMeterUsd = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                MarginPerMeterUsd = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                SalePricePerMeterUsd = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                HasInvoiceMatch = table.Column<bool>(type: "boolean", nullable: false),
                HasPlMatch = table.Column<bool>(type: "boolean", nullable: false),
                HasDplMatch = table.Column<bool>(type: "boolean", nullable: false),
                MatchWarnings = table.Column<string>(type: "text", nullable: true),
                UsesWeightedAllocation = table.Column<bool>(type: "boolean", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                IsActive = table.Column<bool>(type: "boolean", nullable: false),
                IsArchived = table.Column<bool>(type: "boolean", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_container_fabric_type_lines", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_container_fabric_type_lines_ContainerId_LineNumber",
            schema: "china_import",
            table: "container_fabric_type_lines",
            columns: new[] { "ContainerId", "LineNumber" },
            unique: true);

        migrationBuilder.AddColumn<decimal>(
            name: "SalePricePerMeter",
            table: "FabricRolls",
            type: "numeric",
            nullable: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "SalePricePerMeter",
            table: "FabricRolls");

        migrationBuilder.DropTable(
            name: "container_fabric_type_lines",
            schema: "china_import");

        migrationBuilder.DropColumn(
            name: "Insurance",
            schema: "china_import",
            table: "landing_costs");

        migrationBuilder.DropColumn(
            name: "OtherExpense1",
            schema: "china_import",
            table: "landing_costs");

        migrationBuilder.DropColumn(
            name: "OtherExpense2",
            schema: "china_import",
            table: "landing_costs");

        migrationBuilder.DropColumn(
            name: "OtherExpense3",
            schema: "china_import",
            table: "landing_costs");

        migrationBuilder.DropColumn(
            name: "OtherExpense4",
            schema: "china_import",
            table: "landing_costs");

        migrationBuilder.DropColumn(
            name: "UsesWeightedAllocation",
            schema: "china_import",
            table: "landing_costs");
    }
}
