using System.Globalization;
using System.Windows;
using System.Windows.Markup;

namespace ERPSystem.Core;

/// <summary>
/// Arabic UI with Latin digits (0-9) everywhere in WPF.
/// Thread culture uses en-US number formatting; WPF Language is en-US so RTL fonts do not swap digit glyphs.
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
        CultureInfo.DefaultThreadCurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;
        Thread.CurrentThread.CurrentCulture = culture;
        Thread.CurrentThread.CurrentUICulture = culture;
    }

    private static CultureInfo CreateArabicWithLatinDigits()
    {
        var culture = (CultureInfo)CultureInfo.GetCultureInfo("ar-SA").Clone();
        culture.NumberFormat = (NumberFormatInfo)CultureInfo.GetCultureInfo(LatinLanguageTag).NumberFormat.Clone();
        return culture;
    }

    private static CultureInfo CreateArabicGregorianWithLatinDigits()
    {
        var culture = (CultureInfo)CultureInfo.GetCultureInfo("ar-EG").Clone();
        culture.DateTimeFormat.Calendar = new GregorianCalendar();
        culture.NumberFormat = (NumberFormatInfo)CultureInfo.GetCultureInfo(LatinLanguageTag).NumberFormat.Clone();
        return culture;
    }
}
