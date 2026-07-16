using ERPSystem.Application.Common;
using ERPSystem.Domain.Entities.Finance;
using ERPSystem.Domain.Exceptions;
using ERPSystem.Domain.Validators;
using ERPSystem.Domain.ValueObjects;

namespace ERPSystem.Application.Tests.Finance;

public sealed class SupplierPaymentVoucherDomainTests
{
    [Fact]
    public void Cash_payment_has_exactly_one_source_and_keeps_invoice_allocation()
    {
        var invoiceId = Guid.NewGuid();
        var voucher = PaymentVoucher.CreateDraft(Guid.NewGuid(), Guid.NewGuid(), "PAY-1", Guid.NewGuid(),
            Guid.NewGuid(), null, PaymentMethodIds.Cash, new Money(25m), invoiceId);

        PaymentVoucherValidator.Validate(voucher);

        Assert.NotNull(voucher.CashboxId);
        Assert.Null(voucher.BankAccountId);
        Assert.Equal(invoiceId, voucher.PurchaseInvoiceId);
    }

    [Fact]
    public void Voucher_with_cash_and_bank_is_rejected()
    {
        var voucher = PaymentVoucher.CreateDraft(Guid.NewGuid(), Guid.NewGuid(), "PAY-2", Guid.NewGuid(),
            Guid.NewGuid(), Guid.NewGuid(), PaymentMethodIds.BankTransfer, new Money(25m));

        Assert.Throws<ValidationException>(() => PaymentVoucherValidator.Validate(voucher));
    }
}
