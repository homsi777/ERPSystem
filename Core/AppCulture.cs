using System.Globalization;
using System.Windows;
using System.Windows.Markup;

namespace ERPSystem.Core;

/// <summary>
/// Arabic UI with Latin digits (0-9) everywhere in WPF.
/// Arabic text and RTL are retained while numeric and date values use a Gregorian Latin-digit format.
/// </summary>
public static class AppCulture
{
    private const string LatinLanguageTag = "en-US";

    public static CultureInfo ArabicWithLatinDigits { get; } = CreateArabicWithLatinDigits();
    public static CultureInfo ArabicGregorianWithLatinDigits { get; } = CreateArabicGregorianWithLatinDigits();

    public static CultureInfo FormatCulture => Thread.CurrentThread.CurrentCulture;

    static AppCulture()
    {
        Apply();
    }

    public static void Apply()
    {
        Apply(ArabicWithLatinDigits);
    }

    public static void ApplyForLanguage(AppLanguage language)
    {
        Apply(language == AppLanguage.Arabic
            ? ArabicWithLatinDigits
            : CultureInfo.GetCultureInfo(LatinLanguageTag));
    }

    public static void ConfigureWpfPresentation()
    {
        FrameworkElement.LanguageProperty.OverrideMetadata(
            typeof(FrameworkElement),
            new FrameworkPropertyMetadata(XmlLanguage.GetLanguage(LatinLanguageTag)));
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
        // ar-EG supports the Gregorian calendar while retaining Arabic date names.
        // ar-SA may support only the Um Al-Qura calendar on some Windows installations.
        var culture = (CultureInfo)CultureInfo.GetCultureInfo("ar-EG").Clone();
        ApplyGregorianLatinDigits(culture);
        return culture;
    }

    private static CultureInfo CreateArabicGregorianWithLatinDigits()
    {
        var culture = (CultureInfo)CultureInfo.GetCultureInfo("ar-EG").Clone();
        ApplyGregorianLatinDigits(culture);
        return culture;
    }

    private static void ApplyGregorianLatinDigits(CultureInfo culture)
    {
        // NativeDigits is owned by NumberFormatInfo. Cloning en-US is more reliable than
        // relying on each Arabic Windows locale's digit-shaping preference.
        culture.NumberFormat = (NumberFormatInfo)CultureInfo.GetCultureInfo(LatinLanguageTag).NumberFormat.Clone();
        culture.DateTimeFormat.Calendar = new GregorianCalendar();
    }

    private static void AssertLatinDigits(CultureInfo culture)
    {
        var rendered = string.Concat(
            1234.5m.ToString("N1", culture),
            " ",
            new DateTime(2026, 7, 15).ToString("yyyy/MM/dd", culture));
        if (rendered.Any(c => c is >= '٠' and <= '٩' or >= '۰' and <= '۹'))
            throw new InvalidOperationException("تهيئة ثقافة التطبيق يجب أن تعرض الأرقام بالأرقام الغربية.");
    }
}
