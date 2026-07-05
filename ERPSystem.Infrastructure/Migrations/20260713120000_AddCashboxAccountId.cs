using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERPSystem.Infrastructure.Migrations;

public partial class AddCashboxAccountId : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            ALTER TABLE finance.cashboxes ADD COLUMN IF NOT EXISTS "AccountId" uuid NULL;
            UPDATE finance.cashboxes
                SET "AccountId" = 'a1000007-0007-0007-0007-000000000007'
                WHERE "AccountId" IS NULL;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""ALTER TABLE finance.cashboxes DROP COLUMN IF EXISTS "AccountId";""");
    }
}
