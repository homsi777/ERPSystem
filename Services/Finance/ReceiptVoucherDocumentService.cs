using ERPSystem.Application.DTOs.Finance;
using ERPSystem.Domain.Enums;
using ERPSystem.Services.Documents;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using QColors = QuestPDF.Helpers.Colors;
using QPageSizes = QuestPDF.Helpers.PageSizes;

namespace ERPSystem.Services.Finance;

/// <summary>
/// Desktop Receipt Voucher (سند قبض) PDF generator — same Navy/Gold identity as the sales invoice
/// print, with a green accent marking it as an inbound cash document. Mirrors
/// <c>ERPSystem.Api.Services.ReceiptVoucherPdfService</c> for the web/API path.
/// </summary>
public static class ReceiptVoucherDocumentService
{
    private const string Navy = "#071A2B";
    private const string NavySoft = "#102C45";
    private const string Gold = "#C99A4A";
    private const string Paper = "#FFFCF5";
    private const string Muted = "#65717D";
    private const string Border = "#D9C9A7";
    private const string Green = "#1E6B45";
    private const string GreenSoft = "#E7F3EC";

    private static bool _licenseInitialized;

    private static void EnsureLicense()
    {
        if (_licenseInitialized) return;
        QuestPDF.Settings.License = LicenseType.Community;
        _licenseInitialized = true;
    }

    public static void ShowVoucherPreview(ReceiptVoucherPrintDto voucher, bool exportPdf)
    {
        EnsureLicense();
        var document = Build(voucher);
        if (exportPdf)
        {
            PdfPreviewWindow.SaveAndOpen(document, $"سند قبض - {voucher.VoucherNumber} - {voucher.VoucherDate:yyyy-MM-dd}.pdf");
            return;
        }
        PdfPreviewWindow.Show(document, $"سند قبض — {voucher.VoucherNumber}");
    }

    private static IDocument Build(ReceiptVoucherPrintDto voucher) =>
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
                    if (voucher.Allocations.Count > 0)
                        col.Item().Element(e => Allocations(e, voucher.Allocations));
                    col.Item().Element(Signatures);
                });
                page.Footer().AlignCenter().Text(voucher.VoucherNumber).FontSize(8).FontColor(QColors.Grey.Darken1);
            });
        });

    private static void Header(IContainer c, ReceiptVoucherPrintDto voucher) =>
        c.Column(col =>
        {
            col.Item().AlignCenter().Height(50).Width(90).Background(QColors.Grey.Lighten3)
                .AlignMiddle().AlignCenter().Text("شعار الشركة").FontSize(9);
            col.Item().PaddingTop(4).LineHorizontal(2).LineColor(Gold);
            col.Item().PaddingTop(8).AlignCenter().Text("سند قبض").FontSize(18).Bold().FontColor(Navy);
            col.Item().AlignCenter().PaddingTop(4).Background(GreenSoft).Border(1).BorderColor(Green)
                .PaddingVertical(3).PaddingHorizontal(10).Text(StatusLabel(voucher.Status))
                .FontSize(9).Bold().FontColor(Green);
        });

    private static void Parties(IContainer c, ReceiptVoucherPrintDto voucher) =>
        c.Row(row =>
        {
            row.RelativeItem().Background(Paper).Border(1).BorderColor(Border).Padding(8).Column(col =>
            {
                col.Item().Text("بيانات السند").FontSize(9).Bold().FontColor(Gold);
                InfoRow(col, "رقم السند", voucher.VoucherNumber);
                InfoRow(col, "التاريخ", voucher.VoucherDate.ToString("yyyy-MM-dd"));
                InfoRow(col, "الصندوق", voucher.CashboxName);
                InfoRow(col, "طريقة الاستلام", string.IsNullOrWhiteSpace(voucher.PaymentMethodName) ? "—" : voucher.PaymentMethodName);
            });
            row.ConstantItem(10);
            row.RelativeItem().Background(Paper).Border(1).BorderColor(Border).Padding(8).Column(col =>
            {
                col.Item().Text("بيانات العميل").FontSize(9).Bold().FontColor(Gold);
                InfoRow(col, "الاسم", string.IsNullOrWhiteSpace(voucher.CustomerName) ? "عميل غير محدد" : voucher.CustomerName);
                if (!string.IsNullOrWhiteSpace(voucher.CustomerPhone))
                    InfoRow(col, "الهاتف", voucher.CustomerPhone!);
            });
        });

    private static void InfoRow(ColumnDescriptor col, string label, string value) =>
        col.Item().PaddingTop(4).Row(row =>
        {
            row.ConstantItem(70).Text(label).FontSize(8.5f).FontColor(Muted);
            row.RelativeItem().Text(value).FontSize(9).SemiBold();
        });

    private static void Amount(IContainer c, ReceiptVoucherPrintDto voucher) =>
        c.Background(GreenSoft).Border(1).BorderColor(Green).Padding(12).Column(col =>
        {
            col.Item().AlignCenter().Text("المبلغ المستلم").FontSize(9).FontColor(Muted);
            col.Item().PaddingTop(3).AlignCenter().Text($"{voucher.Currency} {voucher.Amount:N2}")
                .FontSize(22).Bold().FontColor(Green);
        });

    private static void Allocations(IContainer c, IReadOnlyList<ReceiptVoucherAllocationDto> allocations) =>
        c.Column(col =>
        {
            col.Item().Text("مقابل فواتير").FontSize(9).Bold().FontColor(Gold);
            col.Item().PaddingTop(4).Table(t =>
            {
                t.ColumnsDefinition(cd =>
                {
                    cd.RelativeColumn(2);
                    cd.RelativeColumn(1);
                });
                t.Header(h =>
                {
                    h.Cell().Element(HeaderCell).Text("رقم الفاتورة");
                    h.Cell().Element(HeaderCell).AlignCenter().Text("المبلغ المخصص");
                });
                foreach (var allocation in allocations)
                {
                    t.Cell().Element(BodyCell).Text(allocation.InvoiceNumber);
                    t.Cell().Element(BodyCell).AlignCenter().Text($"{allocation.Amount:N2}");
                }

                static IContainer HeaderCell(IContainer x) =>
                    x.Background(NavySoft).Padding(5).DefaultTextStyle(s => s.FontColor(QColors.White).FontSize(8).SemiBold());
                static IContainer BodyCell(IContainer x) =>
                    x.BorderBottom(1).BorderColor(Border).Padding(5).DefaultTextStyle(s => s.FontSize(8.5f));
            });
        });

    private static void Signatures(IContainer c) =>
        c.PaddingTop(16).Row(row =>
        {
            row.RelativeItem().Column(col =>
            {
                col.Item().Text("المستلم").FontSize(9).Bold();
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
        VoucherStatus.Posted => "تم الاستلام",
        VoucherStatus.Cancelled => "ملغى",
        VoucherStatus.Reversed => "معكوس",
        _ => status.ToString()
    };
}
