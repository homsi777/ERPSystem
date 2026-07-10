using ERPSystem.Application.Abstractions;
using ERPSystem.Application.Abstractions.Repositories;
using ERPSystem.Application.DTOs.Sales;
using ERPSystem.Application.Results;

namespace ERPSystem.Application.UseCases.Sales;

public sealed class GetSalesTaxReportQuery
{
    public Guid CompanyId { get; init; }
    public DateTime FromDate { get; init; }
    public DateTime ToDate { get; init; }
}

public sealed class GetSalesTaxReportHandler(ISalesTaxReportRepository repository)
    : IQueryHandler<GetSalesTaxReportQuery, ApplicationResult<SalesTaxReportDto>>
{
    public async Task<ApplicationResult<SalesTaxReportDto>> HandleAsync(
        GetSalesTaxReportQuery query,
        CancellationToken cancellationToken = default)
    {
        if (query.FromDate > query.ToDate)
            return ApplicationResult<SalesTaxReportDto>.ValidationFailed(nameof(query.FromDate), "From date must be before to date.");

        var report = await repository.GetReportAsync(query.CompanyId, query.FromDate, query.ToDate, cancellationToken);
        return ApplicationResult<SalesTaxReportDto>.Success(report);
    }
}
