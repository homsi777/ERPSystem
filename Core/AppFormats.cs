using System.Globalization;

namespace ERPSystem.Core;

/// <summary>Unified number/date formatting for WPF UI — always Latin digits.</summary>
public static class AppFormats
{
    private static readonly CultureInfo LatinDigits = CultureInfo.InvariantCulture;

    public static string Number(decimal value, int decimals = 2) =>
        value.ToString(decimals == 0 ? "N0" : $"N{decimals}", AppCulture.FormatCulture);

    public static string Number(int value) => value.ToString("N0", AppCulture.FormatCulture);

    public static string Number(long value) => value.ToString("N0", AppCulture.FormatCulture);

    public static string Date(DateTime value, string format = "yyyy/MM/dd") =>
        value.ToString(format, LatinDigits);

    public static string DateTime(DateTime value, string format = "yyyy/MM/dd HH:mm") =>
        value.ToString(format, LatinDigits);

    public static string Amount(decimal value) => Number(value, 2);

    public static string AmountOrDash(decimal value) => value > 0 ? Amount(value) : "—";

    public static string CurrencyUsd(decimal value, int decimals = 0) =>
        decimals == 0 ? $"{Number(value, 0)} $" : $"{Number(value, decimals)} $";
}

public static class AppFormatExtensions
{
    public static string ToAppString(this decimal value, string format = "N2") =>
        value.ToString(format, AppCulture.FormatCulture);

    public static string ToAppString(this int value, string format = "N0") =>
        value.ToString(format, AppCulture.FormatCulture);

    public static string ToAppString(this double value, string format = "N2") =>
        value.ToString(format, AppCulture.FormatCulture);
}
