using ERPSystem.DocumentEngine.Models;
using ERPSystem.DocumentEngine.Templates.Shared;

namespace ERPSystem.DocumentEngine.Templates.Stocktake;

/// <summary>Stocktake / physical count — counted vs system quantities and variance.</summary>
public sealed class StocktakeTemplate : BaseDocumentTemplate
{
    public override DocumentType Type => DocumentType.Stocktake;
}

/// <summary>Opening Stock — initial quantities and values per item.</summary>
public sealed class OpeningStockTemplate : BaseDocumentTemplate
{
    public override DocumentType Type => DocumentType.OpeningStock;
}
