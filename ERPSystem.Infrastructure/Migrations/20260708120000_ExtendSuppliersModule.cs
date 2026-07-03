using ERPSystem.Application.Common;
using ERPSystem.Infrastructure.Seed;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERPSystem.Infrastructure.Migrations;

/// <inheritdoc />
public partial class ExtendSuppliersModule : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "Address",
            schema: "parties",
            table: "suppliers",
            type: "character varying(500)",
            maxLength: 500,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "City",
            schema: "parties",
            table: "suppliers",
            type: "character varying(100)",
            maxLength: 100,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "Country",
            schema: "parties",
            table: "suppliers",
            type: "character varying(100)",
            maxLength: 100,
            nullable: true);

        migrationBuilder.AddColumn<decimal>(
            name: "CreditLimit",
            schema: "parties",
            table: "suppliers",
            type: "numeric(18,2)",
            precision: 18,
            scale: 2,
            nullable: false,
            defaultValue: 0m);

        migrationBuilder.AddColumn<string>(
            name: "CreditLimitCurrency",
            schema: "parties",
            table: "suppliers",
            type: "text",
            nullable: false,
            defaultValue: "SAR");

        migrationBuilder.AddColumn<string>(
            name: "CurrencyCode",
            schema: "parties",
            table: "suppliers",
            type: "character varying(10)",
            maxLength: 10,
            nullable: false,
            defaultValue: "SAR");

        migrationBuilder.AddColumn<string>(
            name: "Email",
            schema: "parties",
            table: "suppliers",
            type: "character varying(200)",
            maxLength: 200,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "NameAr",
            schema: "parties",
            table: "suppliers",
            type: "character varying(200)",
            maxLength: 200,
            nullable: false,
            defaultValue: "");

        migrationBuilder.AddColumn<string>(
            name: "NameEn",
            schema: "parties",
            table: "suppliers",
            type: "character varying(200)",
            maxLength: 200,
            nullable: false,
            defaultValue: "");

        migrationBuilder.AddColumn<string>(
            name: "Notes",
            schema: "parties",
            table: "suppliers",
            type: "character varying(2000)",
            maxLength: 2000,
            nullable: true);

        migrationBuilder.AddColumn<bool>(
            name: "OpeningBalancePosted",
            schema: "parties",
            table: "suppliers",
            type: "boolean",
            nullable: false,
            defaultValue: false);

        migrationBuilder.AddColumn<Guid>(
            name: "PayablesAccountId",
            schema: "parties",
            table: "suppliers",
            type: "uuid",
            nullable: false,
            defaultValue: AccountingAccountIds.AccountsPayable);

        migrationBuilder.AddColumn<int>(
            name: "PaymentTermsDays",
            schema: "parties",
            table: "suppliers",
            type: "integer",
            nullable: false,
            defaultValue: 30);

        migrationBuilder.AddColumn<string>(
            name: "Phone",
            schema: "parties",
            table: "suppliers",
            type: "character varying(50)",
            maxLength: 50,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "TaxNumber",
            schema: "parties",
            table: "suppliers",
            type: "character varying(50)",
            maxLength: 50,
            nullable: true);

        migrationBuilder.Sql("""
            UPDATE parties.suppliers
            SET "NameAr" = "Name",
                "NameEn" = COALESCE(NULLIF("NameEn", ''), "Name")
            WHERE "NameAr" = '' OR "NameAr" IS NULL;
            """);

        migrationBuilder.Sql($"""
            UPDATE parties.suppliers
            SET "PayablesAccountId" = '{AccountingAccountIds.AccountsPayable}'
            WHERE "PayablesAccountId" = '00000000-0000-0000-0000-000000000000';
            """);

        migrationBuilder.Sql($"""
            INSERT INTO accounting.accounts ("Id", "CompanyId", "Code", "NameAr", "NameEn", "AccountType", "ParentId", "IsPostable", "IsActive", "IsArchived", "CreatedAt")
            SELECT '{AccountingAccountIds.OpeningBalanceEquity}', '{DatabaseSeeder.DefaultCompanyId}', '3100', 'أرصدة افتتاحية', 'Opening Balance Equity', 'Equity', '{AccountingAccountIds.RootEquity}', true, true, false, NOW() AT TIME ZONE 'UTC'
            WHERE NOT EXISTS (SELECT 1 FROM accounting.accounts WHERE "Id" = '{AccountingAccountIds.OpeningBalanceEquity}');
            """);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "Address", schema: "parties", table: "suppliers");
        migrationBuilder.DropColumn(name: "City", schema: "parties", table: "suppliers");
        migrationBuilder.DropColumn(name: "Country", schema: "parties", table: "suppliers");
        migrationBuilder.DropColumn(name: "CreditLimit", schema: "parties", table: "suppliers");
        migrationBuilder.DropColumn(name: "CreditLimitCurrency", schema: "parties", table: "suppliers");
        migrationBuilder.DropColumn(name: "CurrencyCode", schema: "parties", table: "suppliers");
        migrationBuilder.DropColumn(name: "Email", schema: "parties", table: "suppliers");
        migrationBuilder.DropColumn(name: "NameAr", schema: "parties", table: "suppliers");
        migrationBuilder.DropColumn(name: "NameEn", schema: "parties", table: "suppliers");
        migrationBuilder.DropColumn(name: "Notes", schema: "parties", table: "suppliers");
        migrationBuilder.DropColumn(name: "OpeningBalancePosted", schema: "parties", table: "suppliers");
        migrationBuilder.DropColumn(name: "PayablesAccountId", schema: "parties", table: "suppliers");
        migrationBuilder.DropColumn(name: "PaymentTermsDays", schema: "parties", table: "suppliers");
        migrationBuilder.DropColumn(name: "Phone", schema: "parties", table: "suppliers");
        migrationBuilder.DropColumn(name: "TaxNumber", schema: "parties", table: "suppliers");
    }
}
