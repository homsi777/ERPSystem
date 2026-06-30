using ERPSystem.Domain.Services;

namespace ERPSystem.Application.DTOs.Containers;

public sealed class ChinaImportCostPreviewDto
{
    public decimal ChinaInvoiceAmountUsd { get; init; }
    public decimal ExchangeRateToLocalCurrency { get; init; }
    public decimal ContainerWeightKg { get; init; }
    public decimal CustomsAmountUsd { get; init; }
    public decimal ShippingUsd { get; init; }
    public decimal ClearanceUsd { get; init; }
    public decimal OtherExpensesUsd { get; init; }
    public decimal InvoiceLocalEquivalent { get; init; }
    public decimal FinancialTaxReserveUsd { get; init; }
    public decimal FinancialTaxReserveLocal { get; init; }
    public decimal TotalImportExpensesUsd { get; init; }
    public decimal TotalLengthMeters { get; init; }
    public decimal ExpenseCostPerMeterUsd { get; init; }

    public static ChinaImportCostPreviewDto Create(
        decimal chinaInvoiceAmountUsd,
        decimal exchangeRate,
        decimal containerWeightKg,
        decimal customsUsd,
        decimal shippingUsd,
        decimal clearanceUsd,
        decimal otherUsd,
        decimal totalLengthMeters)
    {
        var totalExpenses = customsUsd + shippingUsd + clearanceUsd + otherUsd;
        return new ChinaImportCostPreviewDto
        {
            ChinaInvoiceAmountUsd = chinaInvoiceAmountUsd,
            ExchangeRateToLocalCurrency = exchangeRate,
            ContainerWeightKg = containerWeightKg,
            CustomsAmountUsd = customsUsd,
            ShippingUsd = shippingUsd,
            ClearanceUsd = clearanceUsd,
            OtherExpensesUsd = otherUsd,
            InvoiceLocalEquivalent = ChinaImportFinancials.InvoiceLocalEquivalent(chinaInvoiceAmountUsd, exchangeRate),
            FinancialTaxReserveUsd = ChinaImportFinancials.TaxReserveUsd(chinaInvoiceAmountUsd),
            FinancialTaxReserveLocal = ChinaImportFinancials.TaxReserveLocal(chinaInvoiceAmountUsd, exchangeRate),
            TotalImportExpensesUsd = totalExpenses,
            TotalLengthMeters = totalLengthMeters,
            ExpenseCostPerMeterUsd = totalLengthMeters > 0 ? totalExpenses / totalLengthMeters : 0m
        };
    }
}
