using ERPSystem.DocumentEngine.Models;
using ERPSystem.DocumentEngine.Templates.Shared;

namespace ERPSystem.DocumentEngine.Templates.PurchaseInvoice;

/// <summary>Purchase Invoice — supplier bill-from, received items, tax, totals.</summary>
public sealed class PurchaseInvoiceTemplate : BaseDocumentTemplate
{
    public override DocumentType Type => DocumentType.PurchaseInvoice;
}

/// <summary>Purchase Order — supplier ordered items and terms.</summary>
public sealed class PurchaseOrderTemplate : BaseDocumentTemplate
{
    public override DocumentType Type => DocumentType.PurchaseOrder;
}
