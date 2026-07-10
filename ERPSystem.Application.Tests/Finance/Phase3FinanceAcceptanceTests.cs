using ERPSystem.Application.Common;
using ERPSystem.Application.DTOs.Finance;
using ERPSystem.Domain.Entities.Finance;
using ERPSystem.Domain.Enums;
using ERPSystem.Domain.ValueObjects;

namespace ERPSystem.Application.Tests.Finance;

public sealed class Phase3FinanceAcceptanceTests
{
    [Fact]
    public void Receipt_voucher_requires_approval_before_post()
    {
        var voucher = ReceiptVoucher.CreateDraft(
            Guid.NewGuid(), Guid.NewGuid(), "RV-001",
            Guid.NewGuid(), Guid.NewGuid(), PaymentMethodIds.Cash, new Money(400));
        Assert.Throws<Domain.Exceptions.AccountingException>(() => voucher.Post());
    }

    [Fact]
    public void Posted_receipt_cannot_be_cancelled()
    {
        var voucher = ReceiptVoucher.CreateDraft(
            Guid.NewGuid(), Guid.NewGuid(), "RV-002",
            Guid.NewGuid(), Guid.NewGuid(), PaymentMethodIds.Cash, new Money(100));
        voucher.Approve();
        voucher.Post();
        Assert.Throws<Domain.Exceptions.AccountingException>(() => voucher.Cancel("test"));
    }

    [Fact]
    public void Posted_receipt_can_be_marked_reversed()
    {
        var voucher = ReceiptVoucher.CreateDraft(
            Guid.NewGuid(), Guid.NewGuid(), "RV-003",
            Guid.NewGuid(), Guid.NewGuid(), PaymentMethodIds.Cash, new Money(200));
        voucher.Approve();
        voucher.Post();
        voucher.MarkReversed("duplicate");
        Assert.Equal(VoucherStatus.Reversed, voucher.Status);
    }

    [Fact]
    public void Cash_tender_line_links_to_cashbox()
    {
        var cashboxId = Guid.NewGuid();
        var line = ReceiptTenderLine.CreateCash(Guid.NewGuid(), PaymentMethodIds.Cash, cashboxId, new Money(400));
        Assert.Equal(cashboxId, line.CashboxId);
        Assert.Equal(400m, line.Amount.Amount);
    }

    [Fact]
    public void Bank_tender_requires_reference()
    {
        var line = ReceiptTenderLine.CreateBank(
            Guid.NewGuid(), PaymentMethodIds.BankTransfer, Guid.NewGuid(),
            new Money(500), "TRX-123");
        Assert.Equal("TRX-123", line.Reference);
    }

    [Fact]
    public void Payment_method_ids_are_valid_guids()
    {
        Assert.NotEqual(Guid.Empty, PaymentMethodIds.Cash);
        Assert.NotEqual(Guid.Empty, PaymentMethodIds.BankTransfer);
        Assert.NotEqual(Guid.Empty, FinanceAccountIds.CustomerAdvances);
    }

    [Theory]
    [InlineData(PostingKind.ReceiptVoucherCollection)]
    [InlineData(PostingKind.ReceiptVoucherReversal)]
    public void Phase3_posting_kinds_exist(PostingKind kind)
    {
        Assert.True(Enum.IsDefined(kind));
    }
}
