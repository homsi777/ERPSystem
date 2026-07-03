using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERPSystem.Infrastructure.Migrations;

/// <inheritdoc />
public partial class AddFabricTypeAliases : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "fabric_type_aliases",
            schema: "china_import",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                SupplierId = table.Column<Guid>(type: "uuid", nullable: false),
                FabricItemId = table.Column<Guid>(type: "uuid", nullable: false),
                FabricColorId = table.Column<Guid>(type: "uuid", nullable: false),
                DplMatchKey = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                InvoiceDescriptionMatchKey = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                InvoiceDescription = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                IsActive = table.Column<bool>(type: "boolean", nullable: false),
                IsArchived = table.Column<bool>(type: "boolean", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_fabric_type_aliases", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_fabric_type_aliases_CompanyId_SupplierId_DplMatchKey",
            schema: "china_import",
            table: "fabric_type_aliases",
            columns: new[] { "CompanyId", "SupplierId", "DplMatchKey" },
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "fabric_type_aliases",
            schema: "china_import");
    }
}
