using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERPSystem.Infrastructure.Migrations;

public partial class ExpenseModuleProductionCompletion : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "cost_centers",
            schema: "finance",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                Code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                Description = table.Column<string>(type: "text", nullable: true),
                ParentCostCenterId = table.Column<Guid>(type: "uuid", nullable: true),
                Status = table.Column<int>(type: "integer", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                IsActive = table.Column<bool>(type: "boolean", nullable: false),
                IsArchived = table.Column<bool>(type: "boolean", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_cost_centers", x => x.Id);
                table.ForeignKey(
                    name: "FK_cost_centers_cost_centers_ParentCostCenterId",
                    column: x => x.ParentCostCenterId,
                    principalSchema: "finance",
                    principalTable: "cost_centers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.AddColumn<Guid>(
            name: "CostCenterId",
            schema: "expenses",
            table: "expenses",
            type: "uuid",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "Reason",
            schema: "expenses",
            table: "expense_audit_logs",
            type: "text",
            nullable: true);

        migrationBuilder.AddColumn<DateTime>(
            name: "DueDate",
            schema: "expenses",
            table: "expense_payments",
            type: "timestamp with time zone",
            nullable: true);

        migrationBuilder.AddColumn<decimal>(
            name: "ExchangeRateSnapshot",
            schema: "expenses",
            table: "expense_payments",
            type: "numeric(18,6)",
            precision: 18,
            scale: 6,
            nullable: false,
            defaultValue: 1m);

        migrationBuilder.AddColumn<int>(
            name: "FundingSource",
            schema: "expenses",
            table: "expense_payments",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<int>(
            name: "Status",
            schema: "expenses",
            table: "expense_payments",
            type: "integer",
            nullable: false,
            defaultValue: 2);

        migrationBuilder.AddColumn<int>(
            name: "ApprovalStatus",
            schema: "expenses",
            table: "expense_payments",
            type: "integer",
            nullable: false,
            defaultValue: 1);

        migrationBuilder.AddColumn<string>(
            name: "ReferenceNumber",
            schema: "expenses",
            table: "expense_payments",
            type: "character varying(100)",
            maxLength: 100,
            nullable: true);

        migrationBuilder.AddColumn<int>(
            name: "InstallmentNumber",
            schema: "expenses",
            table: "expense_payments",
            type: "integer",
            nullable: true);

        migrationBuilder.AddColumn<Guid>(
            name: "AttachmentId",
            schema: "expenses",
            table: "expense_payments",
            type: "uuid",
            nullable: true);

        migrationBuilder.AddColumn<Guid>(
            name: "AdjustedFromPaymentId",
            schema: "expenses",
            table: "expense_payments",
            type: "uuid",
            nullable: true);

        migrationBuilder.Sql("""
            UPDATE expenses.expense_payments
            SET "ReferenceNumber" = "Reference"
            WHERE "Reference" IS NOT NULL;
            """);

        migrationBuilder.DropColumn(
            name: "Reference",
            schema: "expenses",
            table: "expense_payments");

        migrationBuilder.DropColumn(
            name: "CostCenter",
            schema: "expenses",
            table: "expenses");

        migrationBuilder.Sql("""
            UPDATE expenses.expenses SET "Status" = CASE "Status"
                WHEN 1 THEN 2
                WHEN 2 THEN 3
                WHEN 3 THEN 6
                WHEN 4 THEN 8
                WHEN 5 THEN 7
                ELSE "Status"
            END;
            """);

        migrationBuilder.CreateTable(
            name: "expense_installments",
            schema: "expenses",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                ExpenseId = table.Column<Guid>(type: "uuid", nullable: false),
                InstallmentNumber = table.Column<int>(type: "integer", nullable: false),
                DueDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                AmountOriginal = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                AmountBase = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                Currency = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                Status = table.Column<int>(type: "integer", nullable: false),
                PaymentId = table.Column<Guid>(type: "uuid", nullable: true),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                IsActive = table.Column<bool>(type: "boolean", nullable: false),
                IsArchived = table.Column<bool>(type: "boolean", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_expense_installments", x => x.Id);
                table.ForeignKey(
                    name: "FK_expense_installments_expenses_ExpenseId",
                    column: x => x.ExpenseId,
                    principalSchema: "expenses",
                    principalTable: "expenses",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "expense_timeline_events",
            schema: "expenses",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                ExpenseId = table.Column<Guid>(type: "uuid", nullable: false),
                EventType = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                Description = table.Column<string>(type: "text", nullable: true),
                PreviousValue = table.Column<string>(type: "text", nullable: true),
                NewValue = table.Column<string>(type: "text", nullable: true),
                UserId = table.Column<Guid>(type: "uuid", nullable: false),
                UserName = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                Reason = table.Column<string>(type: "text", nullable: true)
            },
            constraints: table => table.PrimaryKey("PK_expense_timeline_events", x => x.Id));

        migrationBuilder.CreateIndex(name: "IX_cost_centers_CompanyId_Code", schema: "finance", table: "cost_centers", columns: new[] { "CompanyId", "Code" }, unique: true);
        migrationBuilder.CreateIndex(name: "IX_expenses_CostCenterId", schema: "expenses", table: "expenses", column: "CostCenterId");
        migrationBuilder.CreateIndex(name: "IX_expense_payments_DueDate", schema: "expenses", table: "expense_payments", column: "DueDate");
        migrationBuilder.CreateIndex(name: "IX_expense_payments_Status", schema: "expenses", table: "expense_payments", column: "Status");
        migrationBuilder.CreateIndex(name: "IX_expense_installments_DueDate", schema: "expenses", table: "expense_installments", column: "DueDate");
        migrationBuilder.CreateIndex(name: "IX_expense_installments_ExpenseId", schema: "expenses", table: "expense_installments", column: "ExpenseId");
        migrationBuilder.CreateIndex(name: "IX_expense_timeline_events_ExpenseId", schema: "expenses", table: "expense_timeline_events", column: "ExpenseId");
        migrationBuilder.CreateIndex(name: "IX_expense_timeline_events_Timestamp", schema: "expenses", table: "expense_timeline_events", column: "Timestamp");

        migrationBuilder.AddForeignKey(
            name: "FK_expenses_cost_centers_CostCenterId",
            schema: "expenses",
            table: "expenses",
            column: "CostCenterId",
            principalSchema: "finance",
            principalTable: "cost_centers",
            principalColumn: "Id",
            onDelete: ReferentialAction.SetNull);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(name: "FK_expenses_cost_centers_CostCenterId", schema: "expenses", table: "expenses");
        migrationBuilder.DropTable(name: "expense_timeline_events", schema: "expenses");
        migrationBuilder.DropTable(name: "expense_installments", schema: "expenses");
        migrationBuilder.DropColumn(name: "CostCenterId", schema: "expenses", table: "expenses");
        migrationBuilder.DropColumn(name: "Reason", schema: "expenses", table: "expense_audit_logs");
        migrationBuilder.AddColumn<string>(name: "CostCenter", schema: "expenses", table: "expenses", type: "character varying(100)", maxLength: 100, nullable: true);
        migrationBuilder.DropTable(name: "cost_centers", schema: "finance");
    }
}
