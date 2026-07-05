namespace ERPSystem.Application.Common;

/// <summary>
/// Canonical keys for values persisted in the <c>system_settings</c> table.
/// Keep these stable — they are the contract between the Settings UI and any
/// consumer (expense form exchange rate, numbering, finance defaults, ...).
/// </summary>
public static class SystemSettingKeys
{
    // Company
    public const string CompanyName = "CompanyName";
    public const string CompanyNameEn = "CompanyNameEn";
    public const string CompanyTaxNumber = "CompanyTaxNumber";
    public const string CompanyPhone = "CompanyPhone";
    public const string CompanyAddress = "CompanyAddress";
    public const string CompanyLogoPath = "CompanyLogoPath";

    // Finance
    public const string DefaultCurrency = "DefaultCurrency";
    public const string DefaultExchangeRate = "DefaultExchangeRate";
    public const string EnabledCurrencies = "EnabledCurrencies";

    // Numbering
    public const string InvoicePrefix = "InvoicePrefix";
    public const string ReceiptPrefix = "ReceiptPrefix";
    public const string PurchaseOrderPrefix = "POPrefix";

    // Sensible fallbacks used when a key has not been persisted yet.
    public const decimal DefaultExchangeRateFallback = 15000m;
    public const string DefaultCurrencyFallback = "USD";
}
