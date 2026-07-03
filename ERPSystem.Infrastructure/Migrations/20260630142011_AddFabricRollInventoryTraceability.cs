using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERPSystem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddFabricRollInventoryTraceability : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ContainerItemId",
                table: "FabricRolls",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "CostPerMeter",
                table: "FabricRolls",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "LotCode",
                table: "FabricRolls",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "RemainingLengthMeters",
                table: "FabricRolls",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.Sql(
                "UPDATE \"FabricRolls\" SET \"RemainingLengthMeters\" = \"LengthMeters\" WHERE \"RemainingLengthMeters\" = 0;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ContainerItemId",
                table: "FabricRolls");

            migrationBuilder.DropColumn(
                name: "CostPerMeter",
                table: "FabricRolls");

            migrationBuilder.DropColumn(
                name: "LotCode",
                table: "FabricRolls");

            migrationBuilder.DropColumn(
                name: "RemainingLengthMeters",
                table: "FabricRolls");
        }
    }
}
