using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERPSystem.Infrastructure.Migrations;

/// <inheritdoc />
public partial class AddExpensesModule : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("CREATE SCHEMA IF NOT EXISTS expenses;");

        migrationBuilder.CreateTable(
            name: "expense_categories",
            schema: "expenses",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                Kind = table.Column<int>(type: "integer", nullable: false),
                Code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                NameAr = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                NameEn = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                Description = table.Column<string>(type: "text", nullable: true),
                IsSystem = table.Column<bool>(type: "boolean", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                IsActive = table.Column<bool>(type: "boolean", nullable: false),
                IsArchived = table.Column<bool>(type: "boolean", nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_expense_categories", x => x.Id));

        migrationBuilder.CreateTable(
            name: "expenses",
            schema: "expenses",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                BranchId = table.Column<Guid>(type: "uuid", nullable: false),
                Code = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                Name = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                CategoryId = table.Column<Guid>(type: "uuid", nullable: false),
                CategoryKind = table.Column<int>(type: "integer", nullable: false),
                Description = table.Column<string>(type: "text", nullable: true),
                Status = table.Column<int>(type: "integer", nullable: false),
                StartDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                EndDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                OriginalCurrency = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                OriginalAmount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                ExchangeRate = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                BaseCurrency = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                BaseAmount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                PaymentMethod = table.Column<int>(type: "integer", nullable: false),
                PayeeName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                SupplierId = table.Column<Guid>(type: "uuid", nullable: true),
                CostCenter = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                Department = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                ProjectCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                Notes = table.Column<string>(type: "text", nullable: true),
                IsRecurring = table.Column<bool>(type: "boolean", nullable: false),
                RecurrenceFrequency = table.Column<int>(type: "integer", nullable: false),
                CustomIntervalDays = table.Column<int>(type: "integer", nullable: true),
                NextDueDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                RemainingInstallments = table.Column<int>(type: "integer", nullable: true),
                IntegrationReferenceType = table.Column<string>(type: "text", nullable: true),
                IntegrationReferenceId = table.Column<Guid>(type: "uuid", nullable: true),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                IsActive = table.Column<bool>(type: "boolean", nullable: false),
                IsArchived = table.Column<bool>(type: "boolean", nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_expenses", x => x.Id));

        migrationBuilder.CreateTable(
            name: "expense_payments",
            schema: "expenses",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                ExpenseId = table.Column<Guid>(type: "uuid", nullable: false),
                PaymentDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                AmountOriginal = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                AmountBase = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                Currency = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                PaymentMethod = table.Column<int>(type: "integer", nullable: false),
                Reference = table.Column<string>(type: "text", nullable: true),
                Notes = table.Column<string>(type: "text", nullable: true),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                IsActive = table.Column<bool>(type: "boolean", nullable: false),
                IsArchived = table.Column<bool>(type: "boolean", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_expense_payments", x => x.Id);
                table.ForeignKey(
                    name: "FK_expense_payments_expenses_ExpenseId",
                    column: x => x.ExpenseId,
                    principalSchema: "expenses",
                    principalTable: "expenses",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "expense_attachments",
            schema: "expenses",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                ExpenseId = table.Column<Guid>(type: "uuid", nullable: false),
                FileName = table.Column<string>(type: "character varying(260)", maxLength: 260, nullable: false),
                StoragePath = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                ContentType = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                SizeBytes = table.Column<long>(type: "bigint", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                IsActive = table.Column<bool>(type: "boolean", nullable: false),
                IsArchived = table.Column<bool>(type: "boolean", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_expense_attachments", x => x.Id);
                table.ForeignKey(
                    name: "FK_expense_attachments_expenses_ExpenseId",
                    column: x => x.ExpenseId,
                    principalSchema: "expenses",
                    principalTable: "expenses",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "expense_audit_logs",
            schema: "expenses",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                ExpenseId = table.Column<Guid>(type: "uuid", nullable: false),
                Action = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                FieldName = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                PreviousValue = table.Column<string>(type: "text", nullable: true),
                NewValue = table.Column<string>(type: "text", nullable: true),
                UserId = table.Column<Guid>(type: "uuid", nullable: false),
                UserName = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_expense_audit_logs", x => x.Id));

        migrationBuilder.CreateIndex(
            name: "IX_expense_categories_CompanyId_Code",
            schema: "expenses",
            table: "expense_categories",
            columns: new[] { "CompanyId", "Code" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_expenses_CompanyId",
            schema: "expenses",
            table: "expenses",
            column: "CompanyId");

        migrationBuilder.CreateIndex(
            name: "IX_expenses_CompanyId_Code",
            schema: "expenses",
            table: "expenses",
            columns: new[] { "CompanyId", "Code" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_expenses_CategoryKind",
            schema: "expenses",
            table: "expenses",
            column: "CategoryKind");

        migrationBuilder.CreateIndex(
            name: "IX_expenses_NextDueDate",
            schema: "expenses",
            table: "expenses",
            column: "NextDueDate");

        migrationBuilder.CreateIndex(
            name: "IX_expenses_Status",
            schema: "expenses",
            table: "expenses",
            column: "Status");

        migrationBuilder.CreateIndex(
            name: "IX_expense_payments_ExpenseId",
            schema: "expenses",
            table: "expense_payments",
            column: "ExpenseId");

        migrationBuilder.CreateIndex(
            name: "IX_expense_attachments_ExpenseId",
            schema: "expenses",
            table: "expense_attachments",
            column: "ExpenseId");

        migrationBuilder.CreateIndex(
            name: "IX_expense_audit_logs_ExpenseId",
            schema: "expenses",
            table: "expense_audit_logs",
            column: "ExpenseId");

        migrationBuilder.CreateIndex(
            name: "IX_expense_audit_logs_Timestamp",
            schema: "expenses",
            table: "expense_audit_logs",
            column: "Timestamp");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "expense_audit_logs", schema: "expenses");
        migrationBuilder.DropTable(name: "expense_attachments", schema: "expenses");
        migrationBuilder.DropTable(name: "expense_payments", schema: "expenses");
        migrationBuilder.DropTable(name: "expenses", schema: "expenses");
        migrationBuilder.DropTable(name: "expense_categories", schema: "expenses");
    }
}
