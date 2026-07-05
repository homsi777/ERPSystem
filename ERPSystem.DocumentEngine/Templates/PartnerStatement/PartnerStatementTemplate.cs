using ERPSystem.DocumentEngine.Models;
using ERPSystem.DocumentEngine.Templates.Shared;

namespace ERPSystem.DocumentEngine.Templates.PartnerStatement;

/// <summary>Partner (capital) Statement — contributions, withdrawals, share of P&amp;L.</summary>
public sealed class PartnerStatementTemplate : BaseDocumentTemplate
{
    public override DocumentType Type => DocumentType.PartnerStatement;
}
