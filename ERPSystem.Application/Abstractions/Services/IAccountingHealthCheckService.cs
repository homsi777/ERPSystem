using ERPSystem.Application.DTOs.Accounting;

namespace ERPSystem.Application.Abstractions.Services;

/// <summary>
/// Read-only accounting health checks. Does not modify financial data.
/// </summary>
public interface IAccountingHealthCheckService
{
    Task<AccountingHealthCheckResultDto> RunAsync(
        Guid? companyId = null,
        CancellationToken cancellationToken = default);
}
