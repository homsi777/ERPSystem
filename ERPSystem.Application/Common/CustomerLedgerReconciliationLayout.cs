using ERPSystem.Application.DTOs.Customers;

namespace ERPSystem.Application.Common;

public sealed record CustomerLedgerReconciliationLayout(
    bool HasReconciliation,
    int ReconciledCutoffIndex,
    int MarkerInsertIndex);

public static class CustomerLedgerReconciliationLayoutResolver
{
    public static CustomerLedgerReconciliationLayout Resolve(
        IReadOnlyList<CustomerAccountLedgerLineDto> lines,
        Guid? reconciliationDocumentId,
        DateTime? reconciliationDate)
    {
        var hasReconciliation = reconciliationDocumentId.HasValue || reconciliationDate.HasValue;
        if (!hasReconciliation)
            return new CustomerLedgerReconciliationLayout(false, -1, -1);

        var cutoffIndex = -1;
        if (reconciliationDocumentId.HasValue)
        {
            for (var index = 0; index < lines.Count; index++)
            {
                if (lines[index].EntryId == reconciliationDocumentId.Value)
                {
                    cutoffIndex = index;
                    break;
                }
            }
        }

        if (cutoffIndex < 0 && reconciliationDate.HasValue)
        {
            for (var index = 0; index < lines.Count; index++)
            {
                if (lines[index].TransactionDate <= reconciliationDate.Value)
                    cutoffIndex = index;
            }
        }

        return new CustomerLedgerReconciliationLayout(
            true,
            cutoffIndex,
            cutoffIndex + 1);
    }
}
