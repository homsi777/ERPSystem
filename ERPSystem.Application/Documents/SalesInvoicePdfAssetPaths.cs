namespace ERPSystem.Application.Documents;

/// <summary>Resolves packaged PDF assets (font + logo) from a host content root.</summary>
public static class SalesInvoicePdfAssetPaths
{
    public static (string FontPath, string LogoPath) Resolve(string contentRoot)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(contentRoot);

        var fontPath = Path.Combine(contentRoot, "Assets", "Fonts", "NotoSansArabic.ttf");
        if (!File.Exists(fontPath))
            throw new FileNotFoundException("Embedded Arabic PDF font is missing.", fontPath);

        var logoPath = Path.Combine(contentRoot, "Assets", "Brand", "company-logo.png");
        if (File.Exists(logoPath))
            return (fontPath, logoPath);

        var repositoryAsset = Path.GetFullPath(Path.Combine(contentRoot, "..", "66.png"));
        if (File.Exists(repositoryAsset))
            return (fontPath, repositoryAsset);

        throw new FileNotFoundException("Packaged company logo is missing.", logoPath);
    }
}
