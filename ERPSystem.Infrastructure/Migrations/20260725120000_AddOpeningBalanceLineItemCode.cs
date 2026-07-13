using ERPSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERPSystem.Infrastructure.Migrations;

[DbContext(typeof(ErpDbContext))]
[Migration("20260725120000_AddOpeningBalanceLineItemCode")]
public partial class AddOpeningBalanceLineItemCode : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "ItemCode",
            schema: "finance",
            table: "opening_balance_lines",
            type: "character varying(100)",
            maxLength: 100,
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "ItemCode",
            schema: "finance",
            table: "opening_balance_lines");
    }
}
