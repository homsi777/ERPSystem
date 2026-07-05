using ERPSystem.DocumentEngine.Models;
using ERPSystem.DocumentEngine.Templates.Shared;

namespace ERPSystem.DocumentEngine.Templates.InventoryTransfer;

/// <summary>Inventory Transfer — items moved from a source to a destination store.</summary>
public sealed class InventoryTransferTemplate : BaseDocumentTemplate
{
    public override DocumentType Type => DocumentType.InventoryTransfer;
}
