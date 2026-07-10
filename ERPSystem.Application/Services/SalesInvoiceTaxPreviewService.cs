using ERPSystem.Application.Abstractions.Repositories;
using ERPSystem.Application.Abstractions.Services;
using ERPSystem.Application.DTOs.Sales;
using ERPSystem.Domain.Enums;

namespace ERPSystem.Application.Services;

public sealed class SalesInvoiceTaxPreviewService(
    ISalesTaxEngine taxEngine,
    ITaxCodeRepository taxCodeRepository)
{
    public async Task<SalesInvoiceTaxPreviewDto> CalculateAsync(
        Guid companyId,
        DateTime invoiceDate,
        decimal invoiceDiscountTotal,
        IReadOnlyList<SalesInvoiceTaxPreviewLineRequest> lines,
        CancellationToken cancellationToken = default)
    {
        var errors = new List<string>();
        var taxCodes = await taxCodeRepository.GetActiveForCompanyAsync(companyId, cancellationToken);
        var codeMap = taxCodes.ToDictionary(c => c.Id);

        var lineInputs = new List<(SalesTaxLineInput Input, int LineNumber, decimal RequestLineDiscount)>();
        foreach (var line in lines)
        {
            Domain.Entities.Sales.TaxCode? code = null;
            if (line.TaxCodeId is Guid tid)
            {
                if (!codeMap.TryGetValue(tid, out code))
                {
                    errors.Add($"Line {line.LineNumber}: tax code not found or inactive.");
                    continue;
                }

                if (!code.IsEffectiveOn(invoiceDate))
                    errors.Add($"Line {line.LineNumber}: tax code {code.Code} is not effective on {invoiceDate:yyyy-MM-dd}.");
            }

            var lineId = line.LineId == Guid.Empty ? Guid.NewGuid() : line.LineId;
            lineInputs.Add((
                new SalesTaxLineInput
                {
                    LineId = lineId,
                    NetLineAmount = line.NetLineAmount,
                    LineDiscountTotal = line.LineDiscountTotal,
                    TaxCodeId = code?.Id,
                    TaxCode = code?.Code,
                    TaxName = code?.Name,
                    TaxRate = code?.EffectiveRate() ?? 0m,
                    IsInclusive = code?.PriceMode == TaxPriceMode.Inclusive,
                    IsExempt = code?.Category == TaxCategory.Exempt,
                    IsZeroRated = code?.Category == TaxCategory.ZeroRated,
                    SalesTaxAccountId = code?.SalesTaxAccountId
                },
                line.LineNumber,
                line.LineDiscountTotal));
        }

        if (lineInputs.Count == 0)
        {
            return new SalesInvoiceTaxPreviewDto
            {
                ValidationErrors = errors.Count > 0 ? errors : ["At least one line is required."]
            };
        }

        var result = taxEngine.Calculate(new SalesTaxCalculationRequest
        {
            Currency = "USD",
            InvoiceDiscountTotal = invoiceDiscountTotal,
            Lines = lineInputs.Select(x => x.Input).ToList()
        });

        return new SalesInvoiceTaxPreviewDto
        {
            SubtotalBeforeDiscount = result.SubtotalBeforeDiscount,
            LineDiscountTotal = result.LineDiscountTotal,
            InvoiceDiscountTotal = result.InvoiceDiscountTotal,
            TaxableAmount = result.TaxableAmount,
            TaxTotal = result.TaxTotal,
            GrandTotal = result.GrandTotal,
            RoundingDifference = result.RoundingDifference,
            Lines = result.LineResults.Select(l =>
            {
                var meta = lineInputs.First(x => x.Input.LineId == l.LineId);
                var src = meta.Input;
                return new SalesInvoiceTaxPreviewLineDto
                {
                    LineId = l.LineId,
                    LineNumber = meta.LineNumber,
                    TaxCodeId = l.TaxCodeId,
                    TaxCode = l.TaxCode,
                    TaxName = l.TaxName,
                    TaxRate = l.TaxRate,
                    TaxCategory = src.IsExempt ? TaxCategory.Exempt : src.IsZeroRated ? TaxCategory.ZeroRated : l.TaxCodeId is null ? null : TaxCategory.Standard,
                    IsInclusive = l.IsInclusive,
                    LineDiscountTotal = meta.RequestLineDiscount,
                    TaxableAmount = l.TaxableAmount,
                    TaxAmount = l.TaxAmount,
                    LineGrandTotal = l.LineGrandTotal
                };
            }).ToList(),
            TaxSummary = result.TaxSummary.Select(s => new SalesTaxSummaryLineDto
            {
                TaxCodeId = s.TaxCodeId,
                TaxCode = s.TaxCode,
                TaxName = s.TaxName,
                TaxRate = s.TaxRate,
                TaxableAmount = s.TaxableAmount,
                TaxAmount = s.TaxAmount
            }).ToList(),
            ValidationErrors = errors
        };
    }
}

public sealed class SalesInvoiceTaxPreviewLineRequest
{
    public Guid LineId { get; init; }
    public int LineNumber { get; init; }
    public decimal NetLineAmount { get; init; }
    public decimal LineDiscountTotal { get; init; }
    public Guid? TaxCodeId { get; init; }
}
