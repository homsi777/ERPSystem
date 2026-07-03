using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERPSystem.Infrastructure.Migrations;

public partial class ExpensePaymentCashbox : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<Guid>(
            name: "CashboxId",
            schema: "expenses",
            table: "expense_payments",
            type: "uuid",
            nullable: true);

        migrationBuilder.CreateIndex(
            name: "IX_expense_payments_CashboxId",
            schema: "expenses",
            table: "expense_payments",
            column: "CashboxId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_expense_payments_CashboxId",
            schema: "expenses",
            table: "expense_payments");

        migrationBuilder.DropColumn(
            name: "CashboxId",
            schema: "expenses",
            table: "expense_payments");
    }
}
