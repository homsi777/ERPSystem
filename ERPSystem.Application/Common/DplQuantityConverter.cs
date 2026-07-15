using ERPSystem.Domain.Enums;

namespace ERPSystem.Application.Common;

public static class DplQuantityConverter
{
    /// <summary>International yard → meter (exact definition).</summary>
    public const decimal YardsToMetersFactor = 0.9144m;

    public static decimal ToMeters(decimal nativeQuantity, DplQuantityUnit unit) =>
        unit == DplQuantityUnit.Yards
            ? Math.Round(nativeQuantity * YardsToMetersFactor, 4)
            : nativeQuantity;

    public static string UnitLabel(DplQuantityUnit unit) =>
        unit == DplQuantityUnit.Yards ? "yd" : "m";

    public static string UnitLabelArabic(DplQuantityUnit unit) =>
        unit == DplQuantityUnit.Yards ? "يارد" : "متر";
}
