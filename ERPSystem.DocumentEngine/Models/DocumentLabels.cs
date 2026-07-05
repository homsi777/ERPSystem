namespace ERPSystem.DocumentEngine.Models;

/// <summary>
/// Localised strings for engine-owned chrome (section titles, footer, badges).
/// Business field labels are supplied by the caller inside the document model;
/// these cover only the fixed scaffolding the engine draws itself.
/// </summary>
public sealed class DocumentLabels
{
    public string Notes { get; set; } = "Notes";
    public string Terms { get; set; } = "Terms & Conditions";
    public string Attachments { get; set; } = "Attachments";
    public string Timeline { get; set; } = "Activity";
    public string Summary { get; set; } = "Summary";
    public string Details { get; set; } = "Details";
    public string Subtotal { get; set; } = "Subtotal";
    public string Tax { get; set; } = "Tax";
    public string GrandTotal { get; set; } = "Total";
    public string Taxes { get; set; } = "Taxes";
    public string Page { get; set; } = "Page";
    public string Of { get; set; } = "of";
    public string Approved { get; set; } = "Approved";
    public string Pending { get; set; } = "Pending";
    public string Rejected { get; set; } = "Rejected";
    public string TaxNumber { get; set; } = "Tax No.";
    public string CommercialRegister { get; set; } = "C.R.";
    public string Signature { get; set; } = "Signature";
    public string Stamp { get; set; } = "Stamp";
    public string PreviewPrint { get; set; } = "Print";
    public string PreviewDownload { get; set; } = "Download PDF";

    public static DocumentLabels English() => new();

    public static DocumentLabels Arabic() => new()
    {
        Notes = "ملاحظات",
        Terms = "الشروط والأحكام",
        Attachments = "المرفقات",
        Timeline = "النشاط",
        Summary = "ملخص",
        Details = "التفاصيل",
        Subtotal = "الإجمالي الفرعي",
        Tax = "الضريبة",
        GrandTotal = "الإجمالي",
        Taxes = "الضرائب",
        Page = "صفحة",
        Of = "من",
        Approved = "معتمد",
        Pending = "قيد الاعتماد",
        Rejected = "مرفوض",
        TaxNumber = "الرقم الضريبي",
        CommercialRegister = "س.ت",
        Signature = "التوقيع",
        Stamp = "الختم",
        PreviewPrint = "طباعة",
        PreviewDownload = "تحميل PDF"
    };

    public static DocumentLabels For(string language, TextDirection direction)
    {
        if (!string.IsNullOrWhiteSpace(language) &&
            language.StartsWith("ar", System.StringComparison.OrdinalIgnoreCase))
        {
            return Arabic();
        }

        return direction == TextDirection.Rtl ? Arabic() : English();
    }
}
