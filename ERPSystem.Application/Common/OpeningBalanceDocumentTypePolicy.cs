using ERPSystem.Domain.Enums;

namespace ERPSystem.Application.Common;

/// <summary>
/// Unified opening-balance documents use one source type regardless of whether their lines are
/// customer, supplier, inventory, cash, bank, capital or general-ledger lines.
/// </summary>
public static class OpeningBalanceDocumentTypePolicy
{
    public const DocumentType SourceType = DocumentType.FinanceOpeningBalance;
}
