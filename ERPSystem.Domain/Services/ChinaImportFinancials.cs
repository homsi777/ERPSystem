namespace ERPSystem.Domain.Services;

/// <summary>
/// Financial rules for China import — all monetary inputs are USD.
/// The 2% financial tax reserve is NOT part of landing cost / cost per meter.
/// </summary>
public static class ChinaImportFinancials
{
    public const decimal FinancialTaxReserveRate = 0.02m;

    public static decimal TaxReserveUsd(decimal chinaInvoiceAmountUsd) =>
        chinaInvoiceAmountUsd > 0 ? chinaInvoiceAmountUsd * FinancialTaxReserveRate : 0m;

    public static decimal TaxReserveLocal(decimal chinaInvoiceAmountUsd, decimal exchangeRateToLocal) =>
        TaxReserveUsd(chinaInvoiceAmountUsd) * exchangeRateToLocal;

    public static decimal InvoiceLocalEquivalent(decimal chinaInvoiceAmountUsd, decimal exchangeRateToLocal) =>
        chinaInvoiceAmountUsd * exchangeRateToLocal;
}
