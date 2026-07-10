using ERPSystem.Application.Abstractions.Repositories;
using ERPSystem.Application.Abstractions.Services;
using ERPSystem.Domain.Aggregates;
using ERPSystem.Domain.Entities.Sales;
using ERPSystem.Domain.Enums;
using ERPSystem.Domain.Exceptions;
using ERPSystem.Domain.ValueObjects;

namespace ERPSystem.Application.Services;

public sealed class SalesInvoiceTaxService(
    ISalesTaxEngine taxEngine,
    ITaxCodeRepository taxCodeRepository)
{
    public async Task ApplyTaxToInvoiceAsync(
        SalesInvoiceAggregate invoice,
        bool freezeSnapshots,
        CancellationToken cancellationToken = default)
    {
        if (invoice.IsLegacyUntaxed)
            return;

        if (invoice.Status is SalesInvoiceStatus.Approved or SalesInvoiceStatus.Printed
            or SalesInvoiceStatus.Delivered or SalesInvoiceStatus.Returned
            or SalesInvoiceStatus.PartiallyReturned)
            return;

        var taxCodes = await taxCodeRepository.GetActiveForCompanyAsync(invoice.CompanyId, cancellationToken);
        var codeMap = taxCodes.ToDictionary(c => c.Id);

        var lineInputs = invoice.Items.Select(item =>
        {
            TaxCode? code = null;
            if (item.TaxCodeId is Guid tid)
                codeMap.TryGetValue(tid, out code);

            if (code is not null && !code.IsEffectiveOn(invoice.InvoiceDate))
                throw new ValidationException($"Tax code {code.Code} is not effective on invoice date.");

            return new SalesTaxLineInput
            {
                LineId = item.Id,
                NetLineAmount = item.LineTotal.Amount,
                LineDiscountTotal = item.DiscountAmount.Amount,
                TaxCodeId = code?.Id,
                TaxCode = code?.Code,
                TaxName = code?.Name,
                TaxRate = code?.EffectiveRate() ?? 0m,
                IsInclusive = code?.PriceMode == TaxPriceMode.Inclusive,
                IsExempt = code?.Category == TaxCategory.Exempt,
                IsZeroRated = code?.Category == TaxCategory.ZeroRated,
                SalesTaxAccountId = code?.SalesTaxAccountId
            };
        }).ToList();

        var result = taxEngine.Calculate(new SalesTaxCalculationRequest
        {
            Currency = invoice.SubTotal.Currency,
            InvoiceDiscountTotal = invoice.DiscountTotal.Amount,
            Lines = lineInputs
        });

        var currency = invoice.SubTotal.Currency;
        var snapshots = result.LineResults
            .Where(l => l.TaxCodeId is not null || l.TaxAmount > 0)
            .Select(l => SalesInvoiceItemTaxSnapshot.CreateDraft(
                l.LineId,
                l.TaxCodeId,
                l.TaxCode,
                l.TaxName,
                l.TaxRate,
                new Money(l.TaxableAmount, currency),
                new Money(l.TaxAmount, currency),
                l.IsInclusive,
                l.SalesTaxAccountId))
            .ToList();

        invoice.ApplyTaxTotals(
            new Money(result.TaxTotal, currency),
            new Money(result.GrandTotal, currency),
            result.RoundingDifference,
            snapshots,
            freezeSnapshots);
    }

    public async Task EnsureTaxPostingReadyAsync(
        SalesInvoiceAggregate invoice,
        SalesPostingProfile profile,
        CancellationToken cancellationToken = default)
    {
        if (invoice.TaxTotal.Amount <= 0)
            return;

        if (profile.VatPayableAccountId is null || profile.VatPayableAccountId == Guid.Empty)
            throw new ValidationException(
                "VAT Payable account is not configured. Configure sales posting profile before approving taxed invoices.");

        foreach (var snapshot in invoice.ItemTaxSnapshots.Where(s => s.TaxAmount.Amount > 0))
        {
            if (snapshot.SalesTaxAccountId is null || snapshot.SalesTaxAccountId == Guid.Empty)
                throw new ValidationException(
                    $"Tax code {snapshot.TaxCode} has no sales tax GL account configured.");
        }

        await Task.CompletedTask;
    }
}
