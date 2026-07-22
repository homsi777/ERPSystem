using ERPSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERPSystem.Infrastructure.Migrations;

[DbContext(typeof(ErpDbContext))]
[Migration("20260729130000_AddOpeningBalanceDplQuantityUnit")]
public partial class AddOpeningBalanceDplQuantityUnit : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            ALTER TABLE finance.opening_balance_documents
                ADD COLUMN IF NOT EXISTS "DplQuantityUnit" integer NULL;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            ALTER TABLE finance.opening_balance_documents
                DROP COLUMN IF EXISTS "DplQuantityUnit";
            """);
    }
}
