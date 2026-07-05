using ERPSystem.DocumentEngine.Models;
using ERPSystem.DocumentEngine.Templates.Shared;

namespace ERPSystem.DocumentEngine.Templates.ReceiptVoucher;

/// <summary>Receipt Voucher — money received from a party.</summary>
public sealed class ReceiptVoucherTemplate : BaseDocumentTemplate
{
    public override DocumentType Type => DocumentType.ReceiptVoucher;
}
