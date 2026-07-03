using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERPSystem.Infrastructure.Migrations;

public partial class AddPurchasesModule : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<Guid>(
            name: "BranchId",
            schema: "purchasing",
            table: "purchase_invoices",
            type: "uuid",
            nullable: false,
            defaultValue: Guid.Empty);

        migrationBuilder.AddColumn<string>(
            name: "SupplierReference",
            schema: "purchasing",
            table: "purchase_invoices",
            type: "text",
            nullable: true);

        migrationBuilder.AddColumn<DateTime>(
            name: "DueDate",
            schema: "purchasing",
            table: "purchase_invoices",
            type: "timestamp with time zone",
            nullable: false,
            defaultValue: new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc));

        migrationBuilder.AddColumn<Guid>(
            name: "WarehouseId",
            schema: "purchasing",
            table: "purchase_invoices",
            type: "uuid",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "CurrencyCode",
            schema: "purchasing",
            table: "purchase_invoices",
            type: "character varying(10)",
            maxLength: 10,
            nullable: false,
            defaultValue: "SAR");

        migrationBuilder.AddColumn<decimal>(
            name: "SubTotal",
            schema: "purchasing",
            table: "purchase_invoices",
            type: "numeric",
            nullable: false,
            defaultValue: 0m);

        migrationBuilder.AddColumn<decimal>(
            name: "DiscountAmount",
            schema: "purchasing",
            table: "purchase_invoices",
            type: "numeric",
            nullable: false,
            defaultValue: 0m);

        migrationBuilder.AddColumn<decimal>(
            name: "TaxAmount",
            schema: "purchasing",
            table: "purchase_invoices",
            type: "numeric",
            nullable: false,
            defaultValue: 0m);

        migrationBuilder.AddColumn<decimal>(
            name: "PaidAmount",
            schema: "purchasing",
            table: "purchase_invoices",
            type: "numeric",
            nullable: false,
            defaultValue: 0m);

        migrationBuilder.AddColumn<Guid>(
            name: "PurchaseOrderId",
            schema: "purchasing",
            table: "purchase_invoices",
            type: "uuid",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "Notes",
            schema: "purchasing",
            table: "purchase_invoices",
            type: "text",
            nullable: true);

        migrationBuilder.AddColumn<DateTime>(
            name: "PostedAt",
            schema: "purchasing",
            table: "purchase_invoices",
            type: "timestamp with time zone",
            nullable: true);

        migrationBuilder.AddColumn<Guid>(
            name: "PostedByUserId",
            schema: "purchasing",
            table: "purchase_invoices",
            type: "uuid",
            nullable: true);

        migrationBuilder.RenameTable(
            name: "PurchaseInvoiceItems",
            newName: "purchase_invoice_items",
            newSchema: "purchasing");

        migrationBuilder.AddColumn<int>(
            name: "LineType",
            schema: "purchasing",
            table: "purchase_invoice_items",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<Guid>(
            name: "FabricColorId",
            schema: "purchasing",
            table: "purchase_invoice_items",
            type: "uuid",
            nullable: true);

        migrationBuilder.AddColumn<Guid>(
            name: "ExpenseAccountId",
            schema: "purchasing",
            table: "purchase_invoice_items",
            type: "uuid",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "Description",
            schema: "purchasing",
            table: "purchase_invoice_items",
            type: "text",
            nullable: false,
            defaultValue: "");

        migrationBuilder.AddColumn<int>(
            name: "RollCount",
            schema: "purchasing",
            table: "purchase_invoice_items",
            type: "integer",
            nullable: false,
            defaultValue: 1);

        migrationBuilder.AlterColumn<Guid>(
            name: "FabricItemId",
            schema: "purchasing",
            table: "purchase_invoice_items",
            type: "uuid",
            nullable: true,
            oldClrType: typeof(Guid),
            oldType: "uuid");

        migrationBuilder.CreateTable(
            name: "purchase_orders",
            schema: "purchasing",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                BranchId = table.Column<Guid>(type: "uuid", nullable: false),
                OrderNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                SupplierId = table.Column<Guid>(type: "uuid", nullable: false),
                OrderDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                ExpectedDeliveryDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                TotalAmount = table.Column<decimal>(type: "numeric", nullable: false),
                Status = table.Column<int>(type: "integer", nullable: false),
                Notes = table.Column<string>(type: "text", nullable: true),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                IsActive = table.Column<bool>(type: "boolean", nullable: false),
                IsArchived = table.Column<bool>(type: "boolean", nullable: false),
                CancelledAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                CancelledByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                CancelReason = table.Column<string>(type: "text", nullable: true)
            },
            constraints: table => table.PrimaryKey("PK_purchase_orders", x => x.Id));

        migrationBuilder.CreateTable(
            name: "purchase_order_lines",
            schema: "purchasing",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                PurchaseOrderId = table.Column<Guid>(type: "uuid", nullable: false),
                FabricItemId = table.Column<Guid>(type: "uuid", nullable: true),
                Description = table.Column<string>(type: "text", nullable: false),
                Quantity = table.Column<decimal>(type: "numeric", nullable: false),
                UnitCost = table.Column<decimal>(type: "numeric", nullable: false),
                LineTotal = table.Column<decimal>(type: "numeric", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                IsActive = table.Column<bool>(type: "boolean", nullable: false),
                IsArchived = table.Column<bool>(type: "boolean", nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_purchase_order_lines", x => x.Id));

        migrationBuilder.CreateTable(
            name: "purchase_returns",
            schema: "purchasing",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                BranchId = table.Column<Guid>(type: "uuid", nullable: false),
                ReturnNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                OriginalInvoiceId = table.Column<Guid>(type: "uuid", nullable: false),
                ReturnDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                TotalAmount = table.Column<decimal>(type: "numeric", nullable: false),
                Status = table.Column<int>(type: "integer", nullable: false),
                Notes = table.Column<string>(type: "text", nullable: true),
                PostedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                IsActive = table.Column<bool>(type: "boolean", nullable: false),
                IsArchived = table.Column<bool>(type: "boolean", nullable: false),
                CancelledAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                CancelledByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                CancelReason = table.Column<string>(type: "text", nullable: true)
            },
            constraints: table => table.PrimaryKey("PK_purchase_returns", x => x.Id));

        migrationBuilder.CreateTable(
            name: "purchase_return_lines",
            schema: "purchasing",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                PurchaseReturnId = table.Column<Guid>(type: "uuid", nullable: false),
                OriginalInvoiceItemId = table.Column<Guid>(type: "uuid", nullable: false),
                LineType = table.Column<int>(type: "integer", nullable: false),
                FabricItemId = table.Column<Guid>(type: "uuid", nullable: true),
                FabricColorId = table.Column<Guid>(type: "uuid", nullable: true),
                QuantityMeters = table.Column<decimal>(type: "numeric", nullable: false),
                UnitPrice = table.Column<decimal>(type: "numeric", nullable: false),
                LineTotal = table.Column<decimal>(type: "numeric", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                IsActive = table.Column<bool>(type: "boolean", nullable: false),
                IsArchived = table.Column<bool>(type: "boolean", nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_purchase_return_lines", x => x.Id));

        migrationBuilder.CreateTable(
            name: "purchase_invoice_payments",
            schema: "purchasing",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                PurchaseInvoiceId = table.Column<Guid>(type: "uuid", nullable: false),
                PaymentVoucherId = table.Column<Guid>(type: "uuid", nullable: false),
                Amount = table.Column<decimal>(type: "numeric", nullable: false),
                AppliedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                IsActive = table.Column<bool>(type: "boolean", nullable: false),
                IsArchived = table.Column<bool>(type: "boolean", nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_purchase_invoice_payments", x => x.Id));

        migrationBuilder.CreateIndex(
            name: "IX_purchase_orders_CompanyId_OrderNumber",
            schema: "purchasing",
            table: "purchase_orders",
            columns: new[] { "CompanyId", "OrderNumber" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_purchase_returns_CompanyId_ReturnNumber",
            schema: "purchasing",
            table: "purchase_returns",
            columns: new[] { "CompanyId", "ReturnNumber" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_purchase_invoice_payments_PurchaseInvoiceId",
            schema: "purchasing",
            table: "purchase_invoice_payments",
            column: "PurchaseInvoiceId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "purchase_invoice_payments", schema: "purchasing");
        migrationBuilder.DropTable(name: "purchase_return_lines", schema: "purchasing");
        migrationBuilder.DropTable(name: "purchase_returns", schema: "purchasing");
        migrationBuilder.DropTable(name: "purchase_order_lines", schema: "purchasing");
        migrationBuilder.DropTable(name: "purchase_orders", schema: "purchasing");

        migrationBuilder.DropColumn(name: "BranchId", schema: "purchasing", table: "purchase_invoices");
        migrationBuilder.DropColumn(name: "SupplierReference", schema: "purchasing", table: "purchase_invoices");
        migrationBuilder.DropColumn(name: "DueDate", schema: "purchasing", table: "purchase_invoices");
        migrationBuilder.DropColumn(name: "WarehouseId", schema: "purchasing", table: "purchase_invoices");
        migrationBuilder.DropColumn(name: "CurrencyCode", schema: "purchasing", table: "purchase_invoices");
        migrationBuilder.DropColumn(name: "SubTotal", schema: "purchasing", table: "purchase_invoices");
        migrationBuilder.DropColumn(name: "DiscountAmount", schema: "purchasing", table: "purchase_invoices");
        migrationBuilder.DropColumn(name: "TaxAmount", schema: "purchasing", table: "purchase_invoices");
        migrationBuilder.DropColumn(name: "PaidAmount", schema: "purchasing", table: "purchase_invoices");
        migrationBuilder.DropColumn(name: "PurchaseOrderId", schema: "purchasing", table: "purchase_invoices");
        migrationBuilder.DropColumn(name: "Notes", schema: "purchasing", table: "purchase_invoices");
        migrationBuilder.DropColumn(name: "PostedAt", schema: "purchasing", table: "purchase_invoices");
        migrationBuilder.DropColumn(name: "PostedByUserId", schema: "purchasing", table: "purchase_invoices");

        migrationBuilder.DropColumn(name: "LineType", schema: "purchasing", table: "purchase_invoice_items");
        migrationBuilder.DropColumn(name: "FabricColorId", schema: "purchasing", table: "purchase_invoice_items");
        migrationBuilder.DropColumn(name: "ExpenseAccountId", schema: "purchasing", table: "purchase_invoice_items");
        migrationBuilder.DropColumn(name: "Description", schema: "purchasing", table: "purchase_invoice_items");
        migrationBuilder.DropColumn(name: "RollCount", schema: "purchasing", table: "purchase_invoice_items");

        migrationBuilder.RenameTable(
            name: "purchase_invoice_items",
            schema: "purchasing",
            newName: "PurchaseInvoiceItems");
    }
}
