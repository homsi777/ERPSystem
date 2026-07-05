using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERPSystem.Infrastructure.Migrations;

public partial class AddCustomerCreditLimitEnabled : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            ALTER TABLE parties.customers
                ADD COLUMN IF NOT EXISTS "CreditLimitEnabled" boolean NOT NULL DEFAULT FALSE;

            UPDATE parties.customers
            SET "CreditLimitEnabled" = TRUE
            WHERE "Type" = 1 AND "CreditLimit" > 0;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""ALTER TABLE parties.customers DROP COLUMN IF EXISTS "CreditLimitEnabled";""");
    }
}
