using ERPSystem.Domain.Enums;

namespace ERPSystem.Application.Common;

/// <summary>
/// Length unit for sales invoice lines — derived from the container's DPL unit (meter or yard).
/// </summary>
public static class SaleLengthUnitHelper
{
    public const string MeterStorage = "meter";
    public const string YardStorage = "yard";

    public static string StorageFrom(DplQuantityUnit? unit) =>
        unit == DplQuantityUnit.Yards ? YardStorage : MeterStorage;

    public static DplQuantityUnit ParseStorage(string? storage) =>
        string.Equals(storage, YardStorage, StringComparison.OrdinalIgnoreCase)
            ? DplQuantityUnit.Yards
            : DplQuantityUnit.Meters;

    public static string DisplayArabic(string? storage) =>
        ParseStorage(storage) == DplQuantityUnit.Yards ? "يارد" : "متر";

    public static string DisplayArabic(DplQuantityUnit? unit) =>
        unit == DplQuantityUnit.Yards ? "يارد" : "متر";

    public static string StorageFromDisplay(string? displayArabic) =>
        string.Equals(displayArabic?.Trim(), "يارد", StringComparison.Ordinal)
            ? YardStorage
            : MeterStorage;

    public static string FormatLength(decimal? quantity, string? unitStorage)
    {
        if (quantity is not > 0)
            return "—";

        return $"{quantity.Value:N2} {DisplayArabic(unitStorage)}";
    }
}
