using ERPSystem.DocumentEngine.Models;
using ERPSystem.DocumentEngine.Templates.Shared;

namespace ERPSystem.DocumentEngine.Templates.Quotation;

/// <summary>Quotation / Price Offer — customer, offered items, validity, terms.</summary>
public sealed class QuotationTemplate : BaseDocumentTemplate
{
    public override DocumentType Type => DocumentType.Quotation;
}
