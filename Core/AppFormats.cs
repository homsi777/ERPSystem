using System.Globalization;
using ERPSystem.Application.Common;

namespace ERPSystem.Core;

/// <summary>Unified number/date formatting for WPF UI — always Latin digits.</summary>
public static class AppFormats
{
    public static string Number(decimal value, int decimals = 2) =>
        LatinDigits.Format(value, decimals == 0 ? "N0" : $"N{decimals}");

    public static string Number(int value) => LatinDigits.Format(value, "N0");

    public static string Number(long value) => LatinDigits.Format(value, "N0");

    public static string Date(DateTime value, string format = "yyyy/MM/dd") =>
        LatinDigits.Format(value, format);

    public static string DateTime(DateTime value, string format = "yyyy/MM/dd HH:mm") =>
        LatinDigits.Format(value, format);

    public static string Amount(decimal value) => Number(value, 2);

    public static string AmountOrDash(decimal value) => value > 0 ? Amount(value) : "—";

    public static string CurrencyUsd(decimal value, int decimals = 0) =>
        decimals == 0 ? $"{Number(value, 0)} $" : $"{Number(value, decimals)} $";

    public static string Text(string template, params object[] args) =>
        LatinDigits.FormatText(template, args);
}

public static class AppFormatExtensions
{
    public static string ToAppString(this decimal value, string format = "N2") =>
        LatinDigits.Format(value, format);

    public static string ToAppString(this int value, string format = "N0") =>
        LatinDigits.Format(value, format);

    public static string ToAppString(this double value, string format = "N2") =>
        LatinDigits.Format(value, format);
}
