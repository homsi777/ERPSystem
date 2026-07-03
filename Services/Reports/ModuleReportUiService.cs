using ERPSystem.Application.Abstractions.Services;
using ERPSystem.Application.Common;
using ERPSystem.Application.DTOs.Reports;
using ERPSystem.Application.Results;
using ERPSystem.Application.UseCases.Reports;
using ERPSystem.Services;
using Microsoft.Extensions.DependencyInjection;

namespace ERPSystem.Services.Reports;

public sealed class ModuleReportUiService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ICurrentBranchService _branch;

    public ModuleReportUiService(IServiceScopeFactory scopeFactory, ICurrentBranchService branch)
    {
        _scopeFactory = scopeFactory;
        _branch = branch;
    }

    public static ModuleReportUiService Instance => AppServices.GetRequiredService<ModuleReportUiService>();

    public async Task<ApplicationResult<ModuleReportResultDto>> GetReportAsync(
        string reportKey,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetModuleReportHandler>();

        var query = new GetModuleReportQuery
        {
            CompanyId = _branch.CompanyId ?? throw new InvalidOperationException("Company context is not set."),
            BranchId = _branch.BranchId ?? throw new InvalidOperationException("Branch context is not set."),
            ReportKey = reportKey,
            FromDate = ApplicationDateNormalizer.ToUtcDate(fromDate),
            ToDate = ApplicationDateNormalizer.ToUtcDate(toDate)
        };

        return await handler.HandleAsync(query, cancellationToken);
    }
}
