using QuestPDF.Drawing;
using QuestPDF.Infrastructure;

namespace ERPSystem.Application.Documents;

/// <summary>
/// Shared QuestPDF branding for finance documents (vouchers, expense reports) — same Navy/Gold
/// identity as <see cref="SalesInvoicePdfGenerator"/>.
/// </summary>
public static class FinanceDocumentTheme
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

    public static (string FontPath, string LogoPath) ResolveAssets(string contentRoot) =>
        SalesInvoicePdfAssetPaths.Resolve(contentRoot);
}
