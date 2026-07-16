using ERPSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERPSystem.Infrastructure.Migrations;

[DbContext(typeof(ErpDbContext))]
[Migration("20260724120000_AddPaymentVoucherBankAndInvoiceAllocation")]
public sealed class AddPaymentVoucherBankAndInvoiceAllocation : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder) => migrationBuilder.Sql("""
        ALTER TABLE finance.payment_vouchers ALTER COLUMN "CashboxId" DROP NOT NULL;
        ALTER TABLE finance.payment_vouchers ADD COLUMN IF NOT EXISTS "BankAccountId" uuid NULL;
        ALTER TABLE finance.payment_vouchers ADD COLUMN IF NOT EXISTS "PaymentMethodId" uuid NOT NULL DEFAULT 'f1000001-0001-0001-0001-000000000001';
        ALTER TABLE finance.payment_vouchers ADD COLUMN IF NOT EXISTS "PurchaseInvoiceId" uuid NULL;
        ALTER TABLE finance.payment_vouchers ADD COLUMN IF NOT EXISTS "Reference" varchar(100) NULL;
        ALTER TABLE finance.payment_vouchers ADD COLUMN IF NOT EXISTS "Currency" varchar(3) NOT NULL DEFAULT 'USD';
        CREATE INDEX IF NOT EXISTS "IX_payment_vouchers_PurchaseInvoiceId" ON finance.payment_vouchers ("PurchaseInvoiceId");
        ALTER TABLE finance.payment_vouchers DROP CONSTRAINT IF EXISTS "CK_payment_vouchers_single_source";
        ALTER TABLE finance.payment_vouchers ADD CONSTRAINT "CK_payment_vouchers_single_source"
            CHECK (("CashboxId" IS NOT NULL AND "BankAccountId" IS NULL) OR ("CashboxId" IS NULL AND "BankAccountId" IS NOT NULL));
        """);

    protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.Sql("""
        ALTER TABLE finance.payment_vouchers DROP CONSTRAINT IF EXISTS "CK_payment_vouchers_single_source";
        DROP INDEX IF EXISTS finance."IX_payment_vouchers_PurchaseInvoiceId";
        ALTER TABLE finance.payment_vouchers DROP COLUMN IF EXISTS "BankAccountId";
        ALTER TABLE finance.payment_vouchers DROP COLUMN IF EXISTS "PaymentMethodId";
        ALTER TABLE finance.payment_vouchers DROP COLUMN IF EXISTS "PurchaseInvoiceId";
        ALTER TABLE finance.payment_vouchers DROP COLUMN IF EXISTS "Reference";
        ALTER TABLE finance.payment_vouchers DROP COLUMN IF EXISTS "Currency";
        ALTER TABLE finance.payment_vouchers ALTER COLUMN "CashboxId" SET NOT NULL;
        """);
}
