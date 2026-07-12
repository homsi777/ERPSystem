using QuestPDF.Drawing;
using QuestPDF.Infrastructure;

namespace ERPSystem.Api.Services;

/// <summary>
/// Shared QuestPDF branding (colors, font, logo) for finance/report documents — same visual
/// identity as <see cref="ERPSystem.Application.Documents.SalesInvoicePdfGenerator"/> (Navy/Gold), so every printed document in the
/// system reads as one family. Receipt/payment vouchers add a green/red accent only for the
/// amount box and status badge, matching standard inbound/outbound cash convention.
/// </summary>
internal static class FinanceDocumentTheme
{
    public const string FontFamily = "Noto Sans Arabic";
    public const string Navy = "#071A2B";
    public const string NavySoft = "#102C45";
    public const string Gold = "#C99A4A";
    public const string GoldSoft = "#F6E8C9";
    public const string Paper = "#FFFCF5";
    public const string Muted = "#65717D";
    public const string Border = "#D9C9A7";
    public const string Green = "#1E6B45";
    public const string GreenSoft = "#E7F3EC";
    public const string Maroon = "#8C2A2A";
    public const string MaroonSoft = "#F6E8E8";

    private static readonly object SetupLock = new();
    private static bool _configured;

    public static void ConfigureQuestPdf(string fontPath)
    {
        lock (SetupLock)
        {
            if (_configured)
                return;

            if (!File.Exists(fontPath))
                throw new FileNotFoundException("Embedded Arabic PDF font is missing.", fontPath);

            QuestPDF.Settings.License = LicenseType.Community;
            using var font = File.OpenRead(fontPath);
            FontManager.RegisterFontWithCustomName(FontFamily, font);
            _configured = true;
        }
    }

    public static string ResolveLogoPath(string contentRootPath)
    {
        var packaged = Path.Combine(contentRootPath, "Assets", "Brand", "company-logo.png");
        if (File.Exists(packaged))
            return packaged;

        var repositoryAsset = Path.GetFullPath(Path.Combine(contentRootPath, "..", "66.png"));
        if (File.Exists(repositoryAsset))
            return repositoryAsset;

        throw new FileNotFoundException("Packaged company logo is missing.", packaged);
    }

    public static string ResolveFontPath(string contentRootPath) =>
        Path.Combine(contentRootPath, "Assets", "Fonts", "NotoSansArabic.ttf");
}
