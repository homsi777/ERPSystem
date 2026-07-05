using ERPSystem.DocumentEngine.Models;
using ERPSystem.DocumentEngine.Templates.Shared;

namespace ERPSystem.DocumentEngine.Templates.InventoryReport;

/// <summary>Inventory Report — stock levels, valuation and movement summary.</summary>
public sealed class InventoryReportTemplate : BaseDocumentTemplate
{
    public override DocumentType Type => DocumentType.InventoryReport;
}
