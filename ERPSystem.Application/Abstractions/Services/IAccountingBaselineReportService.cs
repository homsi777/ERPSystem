using ERPSystem.Application.DTOs.Accounting;

namespace ERPSystem.Application.Abstractions.Services;

/// <summary>
/// Generates a read-only accounting baseline snapshot for reconciliation.
/// Does not modify financial data.
/// </summary>
public interface IAccountingBaselineReportService
{
    Task<AccountingBaselineReportDto> GenerateAsync(
        Guid? companyId = null,
        CancellationToken cancellationToken = default);
}
