using ERPSystem.Application.DTOs.Reports;

namespace ERPSystem.Application.Abstractions.Repositories;

public interface IModuleReportRepository
{
    Task<ModuleReportResultDto> RunAsync(GetModuleReportQuery query, CancellationToken cancellationToken = default);
}
