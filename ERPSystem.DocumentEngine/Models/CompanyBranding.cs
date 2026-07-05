namespace ERPSystem.DocumentEngine.Models;

/// <summary>
/// Configurable company identity. No branding is ever hard-coded inside the
/// engine — every value flows in through this object so a document can be
/// re-branded per tenant without touching business logic or templates.
/// </summary>
public sealed class CompanyBranding
{
    public string Name { get; set; } = "Company Name";
    public string? LegalName { get; set; }
    public string? Tagline { get; set; }

    /// <summary>Logo URL/data-URI. When null a styled placeholder is shown.</summary>
    public string? LogoUrl { get; set; }

    public string? Address { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Website { get; set; }
    public string? TaxNumber { get; set; }
    public string? CommercialRegister { get; set; }

    /// <summary>Free-text legal / footer note (e.g. bank details, disclaimer).</summary>
    public string? FooterNote { get; set; }

    /// <summary>
    /// When set, a QR placeholder is rendered. The actual QR image can be
    /// supplied later via <see cref="QrImageUrl"/>; otherwise a stylised
    /// placeholder is shown.
    /// </summary>
    public string? QrContent { get; set; }

    public string? QrImageUrl { get; set; }

    /// <summary>Optional default watermark text (e.g. company short name).</summary>
    public string? WatermarkText { get; set; }
}
