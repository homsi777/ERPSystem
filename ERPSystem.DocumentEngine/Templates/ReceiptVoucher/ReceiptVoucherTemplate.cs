using ERPSystem.DocumentEngine.Models;
using ERPSystem.DocumentEngine.Templates.Shared;

namespace ERPSystem.DocumentEngine.Templates.ReceiptVoucher;

/// <summary>Receipt Voucher — money received from a party (Phase 3 fields via HeaderFields).</summary>
public sealed class ReceiptVoucherTemplate : BaseDocumentTemplate
{
    public override DocumentType Type => DocumentType.ReceiptVoucher;

    public override string RenderBody(DocumentModel model, RenderContext ctx)
    {
        var body = base.RenderBody(model, ctx);
        if (string.Equals(model.StatusLabel, "REVERSED", StringComparison.OrdinalIgnoreCase))
        {
            body += """
                <section class="doc-section doc-section--alert">
                  <h3>REVERSED</h3>
                </section>
                """;
        }
        return body;
    }
}
