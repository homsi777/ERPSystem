using ERPSystem.Application.Abstractions;
using ERPSystem.Application.Abstractions.Repositories;
using ERPSystem.Application.DTOs.Reports;
using ERPSystem.Application.Results;

namespace ERPSystem.Application.UseCases.Reports;

public sealed class GetModuleReportHandler(IModuleReportRepository repository)
    : IQueryHandler<GetModuleReportQuery, ApplicationResult<ModuleReportResultDto>>
{
    public async Task<ApplicationResult<ModuleReportResultDto>> HandleAsync(
        GetModuleReportQuery query,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query.ReportKey))
            return ApplicationResult<ModuleReportResultDto>.ValidationFailed(
                nameof(query.ReportKey), "Report key is required.");

        var result = await repository.RunAsync(query, cancellationToken);
        return ApplicationResult<ModuleReportResultDto>.Success(result);
    }
}
