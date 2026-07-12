using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERPSystem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPurchaseInvoiceSourceContainerId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "SourceContainerId",
                schema: "purchasing",
                table: "purchase_invoices",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_purchase_invoices_SourceContainerId",
                schema: "purchasing",
                table: "purchase_invoices",
                column: "SourceContainerId",
                unique: true,
                filter: "\"SourceContainerId\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_purchase_invoices_SourceContainerId",
                schema: "purchasing",
                table: "purchase_invoices");

            migrationBuilder.DropColumn(
                name: "SourceContainerId",
                schema: "purchasing",
                table: "purchase_invoices");
        }
    }
}
