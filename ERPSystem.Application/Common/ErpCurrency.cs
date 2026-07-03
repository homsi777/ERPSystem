using ERPSystem.Domain.Common;

namespace ERPSystem.Application.Common;

public static class ErpCurrency
{
    public const string Code = CurrencyDefaults.Code;
    public const string Symbol = CurrencyDefaults.Symbol;

    public static string Format(decimal amount, int decimals = 2) =>
        decimals == 0 ? $"{amount:N0} {Symbol}" : $"{amount:N2} {Symbol}";
}
