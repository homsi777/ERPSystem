using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERPSystem.Infrastructure.Migrations;

public partial class AddCustomerReconciliationFields : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            ALTER TABLE parties.customers
                ADD COLUMN IF NOT EXISTS "LastReconciliationDate" timestamp with time zone;
            ALTER TABLE parties.customers
                ADD COLUMN IF NOT EXISTS "LastReconciliationBalance" numeric(18,2);
            ALTER TABLE parties.customers
                ADD COLUMN IF NOT EXISTS "LastReconciliationDocumentId" uuid;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            ALTER TABLE parties.customers DROP COLUMN IF EXISTS "LastReconciliationDocumentId";
            ALTER TABLE parties.customers DROP COLUMN IF EXISTS "LastReconciliationBalance";
            ALTER TABLE parties.customers DROP COLUMN IF EXISTS "LastReconciliationDate";
            """);
    }
}
