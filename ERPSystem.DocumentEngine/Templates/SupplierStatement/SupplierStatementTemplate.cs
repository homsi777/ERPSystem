using ERPSystem.DocumentEngine.Models;
using ERPSystem.DocumentEngine.Templates.Shared;

namespace ERPSystem.DocumentEngine.Templates.SupplierStatement;

/// <summary>Supplier Statement — opening/closing balances + ledger rows.</summary>
public sealed class SupplierStatementTemplate : BaseDocumentTemplate
{
    public override DocumentType Type => DocumentType.SupplierStatement;
}
