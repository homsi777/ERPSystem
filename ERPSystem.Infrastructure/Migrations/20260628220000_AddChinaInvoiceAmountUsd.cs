using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERPSystem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddChinaInvoiceAmountUsd : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "ChinaInvoiceAmountUsd",
                schema: "china_import",
                table: "containers",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "FinancialTaxReservePostedLocal",
                schema: "china_import",
                table: "containers",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ChinaInvoiceAmountUsd",
                schema: "china_import",
                table: "containers");

            migrationBuilder.DropColumn(
                name: "FinancialTaxReservePostedLocal",
                schema: "china_import",
                table: "containers");
        }
    }
}
