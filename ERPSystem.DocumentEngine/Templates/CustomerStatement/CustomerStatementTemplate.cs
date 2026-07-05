using ERPSystem.DocumentEngine.Models;
using ERPSystem.DocumentEngine.Templates.Shared;

namespace ERPSystem.DocumentEngine.Templates.CustomerStatement;

/// <summary>Customer Statement — opening/closing balances + ledger rows.</summary>
public sealed class CustomerStatementTemplate : BaseDocumentTemplate
{
    public override DocumentType Type => DocumentType.CustomerStatement;
}
