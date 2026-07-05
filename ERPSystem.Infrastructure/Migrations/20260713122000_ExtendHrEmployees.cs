using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERPSystem.Infrastructure.Migrations;

public partial class ExtendHrEmployees : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            ALTER TABLE "Employees" ALTER COLUMN "DepartmentId" DROP NOT NULL;
            ALTER TABLE "Employees" ADD COLUMN IF NOT EXISTS "Phone" character varying(50) NULL;
            ALTER TABLE "Employees" ADD COLUMN IF NOT EXISTS "Email" character varying(200) NULL;
            ALTER TABLE "Employees" ADD COLUMN IF NOT EXISTS "Notes" character varying(1000) NULL;
            CREATE UNIQUE INDEX IF NOT EXISTS "IX_Employees_CompanyId_EmployeeCode"
                ON "Employees" ("CompanyId", "EmployeeCode");
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            DROP INDEX IF EXISTS "IX_Employees_CompanyId_EmployeeCode";
            ALTER TABLE "Employees" DROP COLUMN IF EXISTS "Phone";
            ALTER TABLE "Employees" DROP COLUMN IF EXISTS "Email";
            ALTER TABLE "Employees" DROP COLUMN IF EXISTS "Notes";
            """);
    }
}
