using System.Globalization;
using System.Text.RegularExpressions;

namespace ERPSystem.Application.Common;

public static partial class ChinaImportTypeNameNormalizer
{
    public static string NormalizeForMatch(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "";

        var collapsed = PackingListCatalogNormalizer.CollapseWhitespace(value);
        return collapsed.ToUpperInvariant();
    }

    public static string BuildDplMatchKey(string fabricCode, string color) =>
        NormalizeForMatch($"{fabricCode} {color}");

    public static string BuildDescriptionMatchKey(string description) =>
        NormalizeForMatch(description);

    public static bool KeysMatch(string keyA, string keyB)
    {
        if (string.IsNullOrEmpty(keyA) || string.IsNullOrEmpty(keyB))
            return false;
        if (keyA == keyB)
            return true;

        return keyA.Contains(keyB, StringComparison.Ordinal) ||
               keyB.Contains(keyA, StringComparison.Ordinal);
    }

    public static bool TryParseDecimal(string? text, out decimal value)
    {
        value = 0m;
        if (string.IsNullOrWhiteSpace(text))
            return false;

        text = text.Trim().Replace(",", "");
        return decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out value) ||
               decimal.TryParse(text, out value);
    }

    public static bool TryParseInt(string? text, out int value)
    {
        value = 0;
        if (string.IsNullOrWhiteSpace(text))
            return false;

        text = text.Trim().Replace(",", "");
        return int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out value) ||
               int.TryParse(text, out value);
    }

    public static bool RowContainsAny(string? text, params string[] tokens) =>
        !string.IsNullOrWhiteSpace(text) &&
        tokens.Any(t => text.Contains(t, StringComparison.OrdinalIgnoreCase));

    public static bool IsDataRowLabel(string? description) =>
        !string.IsNullOrWhiteSpace(description) &&
        !RowContainsAny(description,
            "TEXTILES FABRIC",
            "SEA FREIGHT",
            "INSURANCE",
            "TOTAL",
            "SAY TOTAL",
            "QUANTITIES",
            "DESCRIPTIONS",
            "ARTICLE");
}
