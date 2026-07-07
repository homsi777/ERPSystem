using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERPSystem.Infrastructure.Migrations;

public partial class AddContainerItemSupplierRollNumber : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            ALTER TABLE china_import.container_items
                ADD COLUMN IF NOT EXISTS "SupplierRollNumber" integer;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            ALTER TABLE china_import.container_items DROP COLUMN IF EXISTS "SupplierRollNumber";
            """);
    }
}
