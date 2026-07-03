using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERPSystem.Infrastructure.Migrations;

/// <inheritdoc />
public partial class AddCapitalPartnersModule : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("CREATE SCHEMA IF NOT EXISTS capital;");

        migrationBuilder.CreateTable(
            name: "partners",
            schema: "capital",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                Code = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                FullName = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                PhotoPath = table.Column<string>(type: "text", nullable: true),
                NationalId = table.Column<string>(type: "text", nullable: true),
                Phone = table.Column<string>(type: "text", nullable: true),
                Email = table.Column<string>(type: "text", nullable: true),
                Address = table.Column<string>(type: "text", nullable: true),
                Notes = table.Column<string>(type: "text", nullable: true),
                DefaultCurrency = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                Status = table.Column<int>(type: "integer", nullable: false),
                RiskLevel = table.Column<int>(type: "integer", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                IsActive = table.Column<bool>(type: "boolean", nullable: false),
                IsArchived = table.Column<bool>(type: "boolean", nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_partners", x => x.Id));

        migrationBuilder.CreateIndex(
            name: "IX_partners_CompanyId",
            schema: "capital",
            table: "partners",
            column: "CompanyId");

        migrationBuilder.CreateIndex(
            name: "IX_partners_CompanyId_Code",
            schema: "capital",
            table: "partners",
            columns: new[] { "CompanyId", "Code" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_partners_Status",
            schema: "capital",
            table: "partners",
            column: "Status");

        migrationBuilder.CreateTable(
            name: "partner_participations",
            schema: "capital",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                PartnerId = table.Column<Guid>(type: "uuid", nullable: false),
                Scope = table.Column<int>(type: "integer", nullable: false),
                OwnershipPercentage = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                ProjectCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                ContainerId = table.Column<Guid>(type: "uuid", nullable: true),
                ContainerNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                IsActive = table.Column<bool>(type: "boolean", nullable: false),
                EffectiveFrom = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_partner_participations", x => x.Id);
                table.ForeignKey(
                    name: "FK_partner_participations_partners_PartnerId",
                    column: x => x.PartnerId,
                    principalSchema: "capital",
                    principalTable: "partners",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "partner_bank_accounts",
            schema: "capital",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                PartnerId = table.Column<Guid>(type: "uuid", nullable: false),
                BankName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                AccountNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                Iban = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                Currency = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                IsDefault = table.Column<bool>(type: "boolean", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_partner_bank_accounts", x => x.Id);
                table.ForeignKey(
                    name: "FK_partner_bank_accounts_partners_PartnerId",
                    column: x => x.PartnerId,
                    principalSchema: "capital",
                    principalTable: "partners",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "capital_transactions",
            schema: "capital",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                PartnerId = table.Column<Guid>(type: "uuid", nullable: false),
                ParticipationId = table.Column<Guid>(type: "uuid", nullable: true),
                Type = table.Column<int>(type: "integer", nullable: false),
                AmountOriginal = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                Currency = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                ExchangeRate = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                BaseCurrency = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                AmountBase = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                TransactionDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                Scope = table.Column<int>(type: "integer", nullable: false),
                ProjectId = table.Column<Guid>(type: "uuid", nullable: true),
                ProjectCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                ContainerId = table.Column<Guid>(type: "uuid", nullable: true),
                ApprovalStatus = table.Column<int>(type: "integer", nullable: false),
                ReferenceNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                Notes = table.Column<string>(type: "text", nullable: true),
                ProfitDistributionId = table.Column<Guid>(type: "uuid", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_capital_transactions", x => x.Id);
                table.ForeignKey(
                    name: "FK_capital_transactions_partners_PartnerId",
                    column: x => x.PartnerId,
                    principalSchema: "capital",
                    principalTable: "partners",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "profit_distributions",
            schema: "capital",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                Code = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                Scope = table.Column<int>(type: "integer", nullable: false),
                ProjectCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                ContainerId = table.Column<Guid>(type: "uuid", nullable: true),
                PeriodStart = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                PeriodEnd = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                GrossRevenue = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                TotalCosts = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                NetProfit = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                NetLoss = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                BaseCurrency = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                Status = table.Column<int>(type: "integer", nullable: false),
                Notes = table.Column<string>(type: "text", nullable: true),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                IsActive = table.Column<bool>(type: "boolean", nullable: false),
                IsArchived = table.Column<bool>(type: "boolean", nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_profit_distributions", x => x.Id));

        migrationBuilder.CreateTable(
            name: "profit_distribution_lines",
            schema: "capital",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                DistributionId = table.Column<Guid>(type: "uuid", nullable: false),
                PartnerId = table.Column<Guid>(type: "uuid", nullable: false),
                OwnershipPercentage = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                PartnerShare = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                CompanyShare = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_profit_distribution_lines", x => x.Id);
                table.ForeignKey(
                    name: "FK_profit_distribution_lines_profit_distributions_Distribution~",
                    column: x => x.DistributionId,
                    principalSchema: "capital",
                    principalTable: "profit_distributions",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "partner_audit_logs",
            schema: "capital",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                PartnerId = table.Column<Guid>(type: "uuid", nullable: false),
                Action = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                FieldName = table.Column<string>(type: "text", nullable: true),
                PreviousValue = table.Column<string>(type: "text", nullable: true),
                NewValue = table.Column<string>(type: "text", nullable: true),
                UserId = table.Column<Guid>(type: "uuid", nullable: false),
                UserName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                Notes = table.Column<string>(type: "text", nullable: true)
            },
            constraints: table => table.PrimaryKey("PK_partner_audit_logs", x => x.Id));

        migrationBuilder.CreateTable(
            name: "partner_timeline_events",
            schema: "capital",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                PartnerId = table.Column<Guid>(type: "uuid", nullable: false),
                EventType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                Title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                Description = table.Column<string>(type: "text", nullable: true),
                PreviousValue = table.Column<string>(type: "text", nullable: true),
                NewValue = table.Column<string>(type: "text", nullable: true),
                UserId = table.Column<Guid>(type: "uuid", nullable: false),
                UserName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                Notes = table.Column<string>(type: "text", nullable: true)
            },
            constraints: table => table.PrimaryKey("PK_partner_timeline_events", x => x.Id));

        migrationBuilder.CreateIndex(name: "IX_partner_participations_PartnerId", schema: "capital", table: "partner_participations", column: "PartnerId");
        migrationBuilder.CreateIndex(name: "IX_partner_participations_Scope", schema: "capital", table: "partner_participations", column: "Scope");
        migrationBuilder.CreateIndex(name: "IX_capital_transactions_PartnerId", schema: "capital", table: "capital_transactions", column: "PartnerId");
        migrationBuilder.CreateIndex(name: "IX_capital_transactions_TransactionDate", schema: "capital", table: "capital_transactions", column: "TransactionDate");
        migrationBuilder.CreateIndex(name: "IX_capital_transactions_Scope", schema: "capital", table: "capital_transactions", column: "Scope");
        migrationBuilder.CreateIndex(name: "IX_profit_distributions_CompanyId_Code", schema: "capital", table: "profit_distributions", columns: new[] { "CompanyId", "Code" }, unique: true);
        migrationBuilder.CreateIndex(name: "IX_profit_distributions_Status", schema: "capital", table: "profit_distributions", column: "Status");
        migrationBuilder.CreateIndex(name: "IX_profit_distribution_lines_DistributionId", schema: "capital", table: "profit_distribution_lines", column: "DistributionId");
        migrationBuilder.CreateIndex(name: "IX_profit_distribution_lines_PartnerId", schema: "capital", table: "profit_distribution_lines", column: "PartnerId");
        migrationBuilder.CreateIndex(name: "IX_partner_audit_logs_PartnerId", schema: "capital", table: "partner_audit_logs", column: "PartnerId");
        migrationBuilder.CreateIndex(name: "IX_partner_audit_logs_Timestamp", schema: "capital", table: "partner_audit_logs", column: "Timestamp");
        migrationBuilder.CreateIndex(name: "IX_partner_timeline_events_PartnerId", schema: "capital", table: "partner_timeline_events", column: "PartnerId");
        migrationBuilder.CreateIndex(name: "IX_partner_timeline_events_Timestamp", schema: "capital", table: "partner_timeline_events", column: "Timestamp");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "partner_timeline_events", schema: "capital");
        migrationBuilder.DropTable(name: "partner_audit_logs", schema: "capital");
        migrationBuilder.DropTable(name: "profit_distribution_lines", schema: "capital");
        migrationBuilder.DropTable(name: "capital_transactions", schema: "capital");
        migrationBuilder.DropTable(name: "partner_bank_accounts", schema: "capital");
        migrationBuilder.DropTable(name: "partner_participations", schema: "capital");
        migrationBuilder.DropTable(name: "profit_distributions", schema: "capital");
        migrationBuilder.DropTable(name: "partners", schema: "capital");
    }
}
