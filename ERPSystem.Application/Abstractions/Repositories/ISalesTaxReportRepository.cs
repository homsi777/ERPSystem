using ERPSystem.Application.DTOs.Sales;

namespace ERPSystem.Application.Abstractions.Repositories;

public interface ISalesTaxReportRepository
{
    Task<SalesTaxReportDto> GetReportAsync(
        Guid companyId,
        DateTime fromDate,
        DateTime toDate,
        CancellationToken cancellationToken = default);
}
