using ERPSystem.Application.DTOs.Finance;
using ERPSystem.Domain.Enums;
using ERPSystem.Services.Documents;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using QColors = QuestPDF.Helpers.Colors;
using QPageSizes = QuestPDF.Helpers.PageSizes;

namespace ERPSystem.Services.Finance;

/// <summary>
/// Desktop Payment Voucher (سند صرف) PDF generator — same Navy/Gold identity as the sales invoice
/// print, with a maroon accent marking it as an outbound cash document. Mirrors
/// <c>ERPSystem.Api.Services.PaymentVoucherPdfService</c> for the web/API path.
/// </summary>
public static class PaymentVoucherDocumentService
{
    private const string Navy = "#071A2B";
    private const string Gold = "#C99A4A";
    private const string Paper = "#FFFCF5";
    private const string Muted = "#65717D";
    private const string Border = "#D9C9A7";
    private const string Maroon = "#8C2A2A";
    private const string MaroonSoft = "#F6E8E8";

    private static bool _licenseInitialized;

    private static void EnsureLicense()
    {
        if (_licenseInitialized) return;
        QuestPDF.Settings.License = LicenseType.Community;
        _licenseInitialized = true;
    }

    public static void ShowVoucherPreview(PaymentVoucherPrintDto voucher, bool exportPdf)
    {
        EnsureLicense();
        var document = Build(voucher);
        if (exportPdf)
        {
            PdfPreviewWindow.SaveAndOpen(document, $"سند صرف - {voucher.VoucherNumber} - {voucher.VoucherDate:yyyy-MM-dd}.pdf");
            return;
        }
        PdfPreviewWindow.Show(document, $"سند صرف — {voucher.VoucherNumber}");
    }

    private static IDocument Build(PaymentVoucherPrintDto voucher) =>
        Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(QPageSizes.A5);
                page.Margin(28);
                page.DefaultTextStyle(t => t.FontFamily("Arial").FontSize(9).FontColor(Navy));
                page.ContentFromRightToLeft();

                page.Header().Element(h => Header(h, voucher));
                page.Content().PaddingTop(12).Column(col =>
                {
                    col.Spacing(12);
                    col.Item().Element(e => Parties(e, voucher));
                    col.Item().Element(e => Amount(e, voucher));
                    col.Item().Element(Signatures);
                });
                page.Footer().AlignCenter().Text(voucher.VoucherNumber).FontSize(8).FontColor(QColors.Grey.Darken1);
            });
        });

    private static void Header(IContainer c, PaymentVoucherPrintDto voucher) =>
        c.Column(col =>
        {
            col.Item().AlignCenter().Height(50).Width(90).Background(QColors.Grey.Lighten3)
                .AlignMiddle().AlignCenter().Text("شعار الشركة").FontSize(9);
            col.Item().PaddingTop(4).LineHorizontal(2).LineColor(Gold);
            col.Item().PaddingTop(8).AlignCenter().Text("سند صرف").FontSize(18).Bold().FontColor(Navy);
            col.Item().AlignCenter().PaddingTop(4).Background(MaroonSoft).Border(1).BorderColor(Maroon)
                .PaddingVertical(3).PaddingHorizontal(10).Text(StatusLabel(voucher.Status))
                .FontSize(9).Bold().FontColor(Maroon);
        });

    private static void Parties(IContainer c, PaymentVoucherPrintDto voucher) =>
        c.Row(row =>
        {
            row.RelativeItem().Background(Paper).Border(1).BorderColor(Border).Padding(8).Column(col =>
            {
                col.Item().Text("بيانات السند").FontSize(9).Bold().FontColor(Gold);
                InfoRow(col, "رقم السند", voucher.VoucherNumber);
                InfoRow(col, "التاريخ", voucher.VoucherDate.ToString("yyyy-MM-dd"));
                InfoRow(col, "الصندوق", voucher.CashboxName);
            });
            row.ConstantItem(10);
            row.RelativeItem().Background(Paper).Border(1).BorderColor(Border).Padding(8).Column(col =>
            {
                col.Item().Text("بيانات المستفيد").FontSize(9).Bold().FontColor(Gold);
                InfoRow(col, "الاسم", string.IsNullOrWhiteSpace(voucher.SupplierName) ? "مورد غير محدد" : voucher.SupplierName);
            });
        });

    private static void InfoRow(ColumnDescriptor col, string label, string value) =>
        col.Item().PaddingTop(4).Row(row =>
        {
            row.ConstantItem(70).Text(label).FontSize(8.5f).FontColor(Muted);
            row.RelativeItem().Text(value).FontSize(9).SemiBold();
        });

    private static void Amount(IContainer c, PaymentVoucherPrintDto voucher) =>
        c.Background(MaroonSoft).Border(1).BorderColor(Maroon).Padding(12).Column(col =>
        {
            col.Item().AlignCenter().Text("المبلغ المصروف").FontSize(9).FontColor(Muted);
            col.Item().PaddingTop(3).AlignCenter().Text($"{voucher.Currency} {voucher.Amount:N2}")
                .FontSize(22).Bold().FontColor(Maroon);
        });

    private static void Signatures(IContainer c) =>
        c.PaddingTop(16).Row(row =>
        {
            row.RelativeItem().Column(col =>
            {
                col.Item().Text("المستفيد").FontSize(9).Bold();
                col.Item().PaddingTop(24).LineHorizontal(1).LineColor(Border);
                col.Item().PaddingTop(3).Text("الاسم / التوقيع").FontSize(7.5f).FontColor(Muted);
            });
            row.ConstantItem(30);
            row.RelativeItem().Column(col =>
            {
                col.Item().Text("أمين الصندوق").FontSize(9).Bold();
                col.Item().PaddingTop(24).LineHorizontal(1).LineColor(Border);
                col.Item().PaddingTop(3).Text("الاسم / التوقيع").FontSize(7.5f).FontColor(Muted);
            });
        });

    private static string StatusLabel(VoucherStatus status) => status switch
    {
        VoucherStatus.Draft => "مسودة",
        VoucherStatus.Submitted => "بانتظار الاعتماد",
        VoucherStatus.Approved => "معتمد",
        VoucherStatus.Posted => "تم الصرف",
        VoucherStatus.Cancelled => "ملغى",
        VoucherStatus.Reversed => "معكوس",
        _ => status.ToString()
    };
}
