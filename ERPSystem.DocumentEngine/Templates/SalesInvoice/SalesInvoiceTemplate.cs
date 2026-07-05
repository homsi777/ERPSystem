using ERPSystem.DocumentEngine.Models;
using ERPSystem.DocumentEngine.Templates.Shared;

namespace ERPSystem.DocumentEngine.Templates.SalesInvoice;

/// <summary>Sales Invoice — customer bill-to, line items, tax, totals, signatures.</summary>
public sealed class SalesInvoiceTemplate : BaseDocumentTemplate
{
    public override DocumentType Type => DocumentType.SalesInvoice;
}
