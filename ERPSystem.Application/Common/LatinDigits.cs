using System.Globalization;
using System.Text;

namespace ERPSystem.Application.Common;

/// <summary>
/// Guarantees Western digits (0-9) in all user-visible text, regardless of Windows locale or WPF numeral shaping.
/// </summary>
public static class LatinDigits
{
    private static readonly CultureInfo FormatCulture = CultureInfo.GetCultureInfo("en-US");

    public static string Normalize(string? text)
    {
        if (string.IsNullOrEmpty(text))
            return text ?? string.Empty;

        var changed = false;
        var buffer = new StringBuilder(text.Length);
        foreach (var ch in text)
        {
            var mapped = MapDigit(ch);
            if (mapped != ch)
                changed = true;
            buffer.Append(mapped);
        }

        return changed ? buffer.ToString() : text;
    }

    public static string Format(object? value, string? format = null)
    {
        if (value is null or DBNull)
            return string.Empty;

        if (value is string s)
            return Normalize(s);

        if (value is DateTime dt)
            return Normalize(dt.ToString(string.IsNullOrWhiteSpace(format) ? "yyyy/MM/dd" : format, CultureInfo.InvariantCulture));

        if (value is DateTimeOffset dto)
            return Format(dto.DateTime, format);

        if (value is bool b)
            return b ? "نعم" : "لا";

        if (value is IFormattable formattable)
        {
            var fmt = string.IsNullOrWhiteSpace(format) ? null : format;
            return Normalize(formattable.ToString(fmt, FormatCulture) ?? string.Empty);
        }

        return Normalize(value.ToString() ?? string.Empty);
    }

    public static string FormatText(FormattableString text) =>
        Normalize(text.ToString(FormatCulture));

    public static string FormatText(string template, params object[] args) =>
        Normalize(string.Format(FormatCulture, template, args));

    private static char MapDigit(char ch) => ch switch
    {
        '٠' => '0',
        '١' => '1',
        '٢' => '2',
        '٣' => '3',
        '٤' => '4',
        '٥' => '5',
        '٦' => '6',
        '٧' => '7',
        '٨' => '8',
        '٩' => '9',
        '۰' => '0',
        '۱' => '1',
        '۲' => '2',
        '۳' => '3',
        '۴' => '4',
        '۵' => '5',
        '۶' => '6',
        '۷' => '7',
        '۸' => '8',
        '۹' => '9',
        _ => ch
    };
}
