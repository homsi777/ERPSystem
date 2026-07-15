using System.Globalization;
using System.Windows;
using System.Windows.Markup;
using System.Windows.Media;

namespace ERPSystem.Core;

/// <summary>
/// Arabic UI with Latin digits (0-9) everywhere in WPF.
/// Arabic text and RTL are retained while numeric and date values use a Gregorian Latin-digit format.
/// </summary>
public static class AppCulture
{
    private const string LatinLanguageTag = "en-US";
    private const string ArabicLatinDigitsTag = "ar-EG-u-nu-latn";

    public static CultureInfo ArabicWithLatinDigits { get; } = CreateArabicWithLatinDigits();
    public static CultureInfo ArabicGregorianWithLatinDigits { get; } = CreateArabicGregorianWithLatinDigits();

    /// <summary>Culture for all user-visible numbers and numeric date parts.</summary>
    public static CultureInfo FormatCulture { get; } = CultureInfo.GetCultureInfo(LatinLanguageTag);

    static AppCulture()
    {
        Apply();
    }

    public static void Apply()
    {
        Apply(FormatCulture);
    }

    public static void ApplyForLanguage(AppLanguage language)
    {
        _ = language;
        Apply(FormatCulture);
    }

    /// <summary>
    /// Forces Western-digit GLYPH rendering app-wide. WPF's text engine can substitute
    /// Arabic-Indic glyphs for plain '0'-'9' characters at paint time based on the
    /// effective Language/culture (NumberSubstitution "AsCulture"/"Context" modes) — this
    /// happens regardless of what the bound string actually contains. Overriding the
    /// default metadata for every DependencyObject makes "European" (always Western
    /// digits) the app-wide default, closing that gap for every control, including ones
    /// with no explicit Style (e.g. third-party or auto-generated elements).
    /// </summary>
    public static void ConfigureWpfPresentation()
    {
        FrameworkElement.LanguageProperty.OverrideMetadata(
            typeof(FrameworkElement),
            new FrameworkPropertyMetadata(XmlLanguage.GetLanguage(LatinLanguageTag)));

        var latin = CultureInfo.GetCultureInfo(LatinLanguageTag);
        NumberSubstitution.CultureSourceProperty.OverrideMetadata(
            typeof(DependencyObject),
            new FrameworkPropertyMetadata(NumberCultureSource.Override));
        NumberSubstitution.CultureOverrideProperty.OverrideMetadata(
            typeof(DependencyObject),
            new FrameworkPropertyMetadata(latin));
        NumberSubstitution.SubstitutionProperty.OverrideMetadata(
            typeof(DependencyObject),
            new FrameworkPropertyMetadata(NumberSubstitutionMethod.European));
    }

    private static void Apply(CultureInfo culture)
    {
        AssertLatinDigits(culture);
        CultureInfo.DefaultThreadCurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;
        Thread.CurrentThread.CurrentCulture = culture;
        Thread.CurrentThread.CurrentUICulture = culture;
    }

    private static CultureInfo CreateArabicWithLatinDigits()
    {
        var culture = TryGetCulture(ArabicLatinDigitsTag)
            ?? (CultureInfo)CultureInfo.GetCultureInfo("ar-EG").Clone();
        ApplyGregorianLatinDigits(culture);
        return culture;
    }

    private static CultureInfo CreateArabicGregorianWithLatinDigits()
    {
        var culture = TryGetCulture(ArabicLatinDigitsTag)
            ?? (CultureInfo)CultureInfo.GetCultureInfo("ar-EG").Clone();
        ApplyGregorianLatinDigits(culture);
        return culture;
    }

    private static CultureInfo? TryGetCulture(string name)
    {
        try
        {
            return (CultureInfo)CultureInfo.GetCultureInfo(name).Clone();
        }
        catch (CultureNotFoundException)
        {
            return null;
        }
    }

    private static void ApplyGregorianLatinDigits(CultureInfo culture)
    {
        var latin = CultureInfo.GetCultureInfo(LatinLanguageTag);
        culture.NumberFormat = (NumberFormatInfo)latin.NumberFormat.Clone();
        culture.NumberFormat.DigitSubstitution = DigitShapes.None;

        var dateFormat = (DateTimeFormatInfo)culture.DateTimeFormat.Clone();
        dateFormat.Calendar = new GregorianCalendar();
        culture.DateTimeFormat = dateFormat;
    }

    private static void AssertLatinDigits(CultureInfo culture)
    {
        var rendered = string.Concat(
            1234.5m.ToString("N1", culture),
            " ",
            new DateTime(2026, 7, 15).ToString("yyyy/MM/dd", culture),
            " ",
            FormatArabicDateWithLatinDayNumbers(new DateTime(2026, 7, 15), culture));
        if (rendered.Any(c => c is >= '٠' and <= '٩' or >= '۰' and <= '۹'))
            throw new InvalidOperationException("تهيئة ثقافة التطبيق يجب أن تعرض الأرقام بالأرقام الغربية.");
    }

    /// <summary>Arabic weekday/month names with Latin day/year numbers.</summary>
    public static string FormatArabicDateWithLatinDayNumbers(DateTime value, CultureInfo? culture = null)
    {
        culture ??= ArabicGregorianWithLatinDigits;
        var dayName = culture.DateTimeFormat.GetDayName(value.DayOfWeek);
        var monthName = culture.DateTimeFormat.GetMonthName(value.Month);
        return $"{dayName}، {value.Day.ToString(CultureInfo.InvariantCulture)} {monthName} {value.Year.ToString(CultureInfo.InvariantCulture)}";
    }
}
