using ERPSystem.Application.Common;

namespace ERPSystem.Services.Settings;

/// <summary>
/// Shared, settings-driven currency source for forms (cashbox, capital, expense).
/// Loaded once at startup from <c>system_settings</c>; forms read the cached
/// values synchronously. Falls back to sane defaults before the first load.
/// </summary>
public static class CurrencyCatalog
{
    private static string[] _currencies = ["USD", "SAR", "EUR", "SYP", "CNY"];
    private static decimal _defaultExchangeRate = SystemSettingKeys.DefaultExchangeRateFallback;

    public static IReadOnlyList<string> Currencies => _currencies;

    public static string[] CurrencyArray => (string[])_currencies.Clone();

    public static decimal DefaultExchangeRate => _defaultExchangeRate;

    public static async Task RefreshAsync()
    {
        try
        {
            var all = await SettingsUiService.Instance.LoadAllAsync();

            if (all.TryGetValue(SystemSettingKeys.EnabledCurrencies, out var list) && !string.IsNullOrWhiteSpace(list))
            {
                var parsed = list
                    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Select(c => c.ToUpperInvariant())
                    .Distinct()
                    .ToArray();
                if (parsed.Length > 0)
                    _currencies = parsed;
            }

            if (all.TryGetValue(SystemSettingKeys.DefaultExchangeRate, out var rate) &&
                decimal.TryParse(rate, out var value) && value > 0)
            {
                _defaultExchangeRate = value;
            }
        }
        catch
        {
            // Keep defaults if settings are unavailable.
        }
    }
}
