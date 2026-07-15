using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERPSystem.Infrastructure.Migrations;

public partial class AddContainerDplQuantityUnit : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            ALTER TABLE china_import.containers
                ADD COLUMN IF NOT EXISTS "DplQuantityUnit" integer;

            ALTER TABLE china_import.container_items
                ADD COLUMN IF NOT EXISTS "DplQuantityNative" numeric(18,4),
                ADD COLUMN IF NOT EXISTS "DplQuantityUnit" integer;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            ALTER TABLE china_import.container_items DROP COLUMN IF EXISTS "DplQuantityUnit";
            ALTER TABLE china_import.container_items DROP COLUMN IF EXISTS "DplQuantityNative";
            ALTER TABLE china_import.containers DROP COLUMN IF EXISTS "DplQuantityUnit";
            """);
    }
}
