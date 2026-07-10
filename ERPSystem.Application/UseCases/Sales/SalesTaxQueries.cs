using ERPSystem.Application.Abstractions;
using ERPSystem.Application.Abstractions.Repositories;
using ERPSystem.Application.DTOs.Sales;
using ERPSystem.Application.Results;
using ERPSystem.Application.Services;
using ERPSystem.Domain.Enums;

namespace ERPSystem.Application.UseCases.Sales;

public sealed class GetTaxCodesQuery
{
    public Guid CompanyId { get; init; }
    public DateTime? EffectiveOn { get; init; }
}

public sealed class GetTaxCodesHandler(ITaxCodeRepository taxCodeRepository)
    : IQueryHandler<GetTaxCodesQuery, ApplicationResult<IReadOnlyList<TaxCodeDto>>>
{
    public async Task<ApplicationResult<IReadOnlyList<TaxCodeDto>>> HandleAsync(
        GetTaxCodesQuery query,
        CancellationToken cancellationToken = default)
    {
        var codes = await taxCodeRepository.GetActiveForCompanyAsync(query.CompanyId, cancellationToken);
        var effectiveOn = (query.EffectiveOn ?? DateTime.UtcNow).Date;
        var list = codes
            .Where(c => c.IsEffectiveOn(effectiveOn))
            .Select(c => new TaxCodeDto
            {
                Id = c.Id,
                Code = c.Code,
                Name = c.Name,
                Rate = c.Rate,
                PriceMode = c.PriceMode,
                Category = c.Category,
                EffectiveFrom = c.EffectiveFrom,
                EffectiveTo = c.EffectiveTo
            })
            .ToList();
        return ApplicationResult<IReadOnlyList<TaxCodeDto>>.Success(list);
    }
}

public sealed class CalculateSalesInvoiceTaxQuery
{
    public Guid CompanyId { get; init; }
    public DateTime InvoiceDate { get; init; }
    public decimal InvoiceDiscountTotal { get; init; }
    public IReadOnlyList<SalesInvoiceTaxPreviewLineRequest> Lines { get; init; } = [];
}

public sealed class CalculateSalesInvoiceTaxHandler(SalesInvoiceTaxPreviewService previewService)
    : IQueryHandler<CalculateSalesInvoiceTaxQuery, ApplicationResult<SalesInvoiceTaxPreviewDto>>
{
    public async Task<ApplicationResult<SalesInvoiceTaxPreviewDto>> HandleAsync(
        CalculateSalesInvoiceTaxQuery query,
        CancellationToken cancellationToken = default)
    {
        if (query.Lines.Count == 0)
            return ApplicationResult<SalesInvoiceTaxPreviewDto>.ValidationFailed("Lines", "At least one line is required.");

        var preview = await previewService.CalculateAsync(
            query.CompanyId,
            query.InvoiceDate,
            query.InvoiceDiscountTotal,
            query.Lines,
            cancellationToken);

        return ApplicationResult<SalesInvoiceTaxPreviewDto>.Success(preview);
    }
}

public sealed class GetSalesTaxReportQuery
{
    public Guid CompanyId { get; init; }
    public DateTime FromDate { get; init; }
    public DateTime ToDate { get; init; }
    public bool IncludeLegacy { get; init; } = true;
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
        if (!query.IncludeLegacy)
        {
            report = new SalesTaxReportDto
            {
                Rows = report.Rows.Where(r => !r.IsLegacyUntaxed).ToList(),
                SummaryByTaxCode = report.SummaryByTaxCode
            };
        }

        return ApplicationResult<SalesTaxReportDto>.Success(report);
    }
}
