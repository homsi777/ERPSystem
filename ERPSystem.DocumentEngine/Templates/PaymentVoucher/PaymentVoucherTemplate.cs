using ERPSystem.DocumentEngine.Models;
using ERPSystem.DocumentEngine.Templates.Shared;

namespace ERPSystem.DocumentEngine.Templates.PaymentVoucher;

/// <summary>Payment Voucher — money paid to a party.</summary>
public sealed class PaymentVoucherTemplate : BaseDocumentTemplate
{
    public override DocumentType Type => DocumentType.PaymentVoucher;
}
