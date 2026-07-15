using ERPSystem.Domain.Enums;

namespace ERPSystem.Application.Common;

/// <summary>
/// Presentation helpers for China import screens — labels and values follow the manager's
/// DPL unit choice (meter or yard). Storage and costing remain meter-based internally.
/// </summary>
public static class ChinaImportLengthDisplay
{
    public static DplQuantityUnit Resolve(DplQuantityUnit? unit) =>
        unit ?? DplQuantityUnit.Meters;

    public static bool IsYards(DplQuantityUnit? unit) =>
        Resolve(unit) == DplQuantityUnit.Yards;

    public static string UnitArabic(DplQuantityUnit? unit) =>
        SaleLengthUnitHelper.DisplayArabic(unit);

    public static string LengthAbbrev(DplQuantityUnit? unit) =>
        IsYards(unit) ? "ي" : "م";

    public static string TotalLengthLabel(DplQuantityUnit? unit) =>
        IsYards(unit) ? "إجمالي الياردات" : "إجمالي الأمتار";

    public static string LengthColumnHeader(DplQuantityUnit? unit) =>
        IsYards(unit) ? "يارد" : "أمتار";

    public static string LengthInSystemLabel(DplQuantityUnit? unit) =>
        IsYards(unit) ? "الياردات في النظام" : "الأمتار في النظام";

    public static string PerUnitSuffix(DplQuantityUnit? unit) =>
        IsYards(unit) ? "/ي" : "/م";

    public static string DollarPerUnitSuffix(DplQuantityUnit? unit) =>
        IsYards(unit) ? "$/ي" : "$/م";

    public static string CostPerUnitLabel(DplQuantityUnit? unit) =>
        IsYards(unit) ? "تكلفة/ي" : "تكلفة/م";

    public static string ChinaPricePerUnitLabel(DplQuantityUnit? unit) =>
        IsYards(unit) ? "سعر الصين/ي" : "سعر الصين/م";

    public static string SalePricePerUnitLabel(DplQuantityUnit? unit) =>
        IsYards(unit) ? "سعر البيع/ي" : "سعر البيع/م";

    public static string CustomsCostPerUnitLabel(DplQuantityUnit? unit) =>
        IsYards(unit) ? "تكلفة الجمارك/ي" : "تكلفة الجمارك/م";

    public static string ExpenseCostPerUnitLabel(DplQuantityUnit? unit) =>
        IsYards(unit) ? "تكلفة الوصول/ي" : "تكلفة الوصول/م";

    public static string ExpenseCostPerUnitShortLabel(DplQuantityUnit? unit) =>
        IsYards(unit) ? "تكلفة المصاريف/ي" : "تكلفة المصاريف/م";

    public static string GramPerUnitLabel(DplQuantityUnit? unit) =>
        IsYards(unit) ? "غ/ي" : "غ/م";

    public static string AvgGramPerUnitLabel(DplQuantityUnit? unit) =>
        IsYards(unit) ? "متوسط غ/ي" : "متوسط غ/م";

    public static string MarginPerUnitLabel(DplQuantityUnit? unit) =>
        IsYards(unit) ? "هامش الربح ($/ي)" : "هامش الربح ($/م)";

    public static string FinalSalePriceLabel(DplQuantityUnit? unit) =>
        IsYards(unit) ? "سعر البيع النهائي ($/ي)" : "سعر البيع النهائي ($/م)";

    public static string LandingCostDistributionHint(DplQuantityUnit? unit) =>
        IsYards(unit)
            ? "تكاليف الوصول ($) تُوزَّع على الياردات. احتياطي 2% ضريبة مالية يُرحَّل عند اعتماد الحاوية — لا يدخل سعر اليارد."
            : "تكاليف الوصول ($) تُوزَّع على الأمتار. احتياطي 2% ضريبة مالية يُرحَّل عند اعتماد الحاوية — لا يدخل سعر المتر.";

    public static string FlatCostEntryHint(DplQuantityUnit? unit) =>
        IsYards(unit)
            ? "وضع معدّل مسطح (DPL فقط): المصاريف تُوزَّع على إجمالي الياردات. لإتاحة التكلفة حسب النوع ارفع الفاتورة + PL."
            : "وضع معدّل مسطح (DPL فقط): المصاريف تُوزَّع على إجمالي الأمتار. لإتاحة التكلفة حسب النوع ارفع الفاتورة + PL.";

    public static string SalePriceBanner(DplQuantityUnit? unit) =>
        IsYards(unit)
            ? "أدخل هامش الربح ($/ي) لكل نوع. سعر البيع = تكلفة اليارد المحسوبة + الهامش. الاعتماد يتطلب إدخال سعر لكل نوع."
            : "أدخل هامش الربح ($/م) لكل نوع. سعر البيع = تكلفة المتر المحسوبة + الهامش. الاعتماد يتطلب إدخال سعر لكل نوع.";

    /// <summary>Convert stored meter length to display length in the selected unit.</summary>
    public static decimal FromStoredLength(decimal lengthMeters, DplQuantityUnit? unit) =>
        IsYards(unit)
            ? Math.Round(lengthMeters / DplQuantityConverter.YardsToMetersFactor, 4)
            : lengthMeters;

    /// <summary>Convert stored per-meter rate to display rate in the selected unit.</summary>
    public static decimal FromStoredRate(decimal perMeter, DplQuantityUnit? unit) =>
        IsYards(unit)
            ? Math.Round(perMeter * DplQuantityConverter.YardsToMetersFactor, 6)
            : perMeter;

    /// <summary>Convert user-entered per-unit rate back to per-meter storage.</summary>
    public static decimal ToStoredRate(decimal displayRate, DplQuantityUnit? unit) =>
        IsYards(unit)
            ? Math.Round(displayRate / DplQuantityConverter.YardsToMetersFactor, 6)
            : displayRate;

    public static string FormatLength(decimal lengthMeters, DplQuantityUnit? unit, string format = "N2") =>
        $"{FromStoredLength(lengthMeters, unit).ToString(format)} {LengthAbbrev(unit)}";

    public static string FormatRate(decimal perMeter, DplQuantityUnit? unit, string format = "N4") =>
        $"{FromStoredRate(perMeter, unit).ToString(format)} {DollarPerUnitSuffix(unit)}";
}
