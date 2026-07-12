using System.Globalization;
using ERPSystem.Application.DTOs.Finance;
using ERPSystem.Domain.Enums;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using static ERPSystem.Api.Services.FinanceDocumentTheme;

namespace ERPSystem.Api.Services;

/// <summary>
/// Server-side Payment Voucher (سند صرف) renderer — same Navy/Gold identity as the sales invoice
/// PDF, with a maroon accent on the amount box marking it as an outbound cash document.
/// </summary>
public sealed class PaymentVoucherPdfService
{
    private static readonly CultureInfo WesternNumbers = CultureInfo.InvariantCulture;

    private readonly string _logoPath;

    public PaymentVoucherPdfService(IWebHostEnvironment environment)
    {
        _logoPath = ResolveLogoPath(environment.ContentRootPath);
        ConfigureQuestPdf(ResolveFontPath(environment.ContentRootPath));
    }

    public byte[] Generate(PaymentVoucherPrintDto voucher)
    {
        ArgumentNullException.ThrowIfNull(voucher);

        return Document.Create(document =>
        {
            document.Page(page =>
            {
                page.Size(PageSizes.A5);
                page.Margin(28);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(style => style
                    .FontFamily(FontFamily)
                    .FontSize(9)
                    .FontColor(Navy));
                page.ContentFromRightToLeft();

                page.Header().Element(container => ComposeHeader(container, voucher));
                page.Content().PaddingTop(12).Column(column =>
                {
                    column.Spacing(12);
                    column.Item().Element(container => ComposeParties(container, voucher));
                    column.Item().Element(container => ComposeAmount(container, voucher));
                    column.Item().Element(ComposeSignatures);
                });
                page.Footer().Element(container => ComposeFooter(container, voucher.VoucherNumber));
            });
        }).GeneratePdf();
    }

    private void ComposeHeader(IContainer container, PaymentVoucherPrintDto voucher)
    {
        container.Column(column =>
        {
            column.Item().AlignCenter().Height(56).Width(70).Image(_logoPath).FitArea();
            column.Item().PaddingTop(4).LineHorizontal(2).LineColor(Gold);
            column.Item().PaddingTop(8).AlignCenter().Text("سند صرف").FontSize(18).Bold().FontColor(Navy);
            column.Item().AlignCenter().PaddingTop(4).Background(MaroonSoft).Border(1).BorderColor(Maroon)
                .PaddingVertical(3).PaddingHorizontal(10).Text(StatusLabel(voucher.Status))
                .FontSize(9).Bold().FontColor(Maroon);
        });
    }

    private static void ComposeParties(IContainer container, PaymentVoucherPrintDto voucher) =>
        container.Row(row =>
        {
            row.RelativeItem().Background(Paper).Border(1).BorderColor(Border).Padding(8).Column(col =>
            {
                col.Item().Text("بيانات السند").FontSize(9).Bold().FontColor(Gold);
                InfoRow(col, "رقم السند", voucher.VoucherNumber);
                InfoRow(col, "التاريخ", voucher.VoucherDate.ToString("yyyy-MM-dd", WesternNumbers));
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

    private static void ComposeAmount(IContainer container, PaymentVoucherPrintDto voucher) =>
        container.Background(MaroonSoft).Border(1).BorderColor(Maroon).Padding(12).Column(col =>
        {
            col.Item().AlignCenter().Text("المبلغ المصروف").FontSize(9).FontColor(Muted);
            col.Item().PaddingTop(3).AlignCenter().ContentFromLeftToRight()
                .Text($"{voucher.Currency} {Number(voucher.Amount)}").FontSize(22).Bold().FontColor(Maroon);
        });

    private static void ComposeSignatures(IContainer container) =>
        container.PaddingTop(16).Row(row =>
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

    private static void ComposeFooter(IContainer container, string voucherNumber)
    {
        container.BorderTop(1).BorderColor(Gold).PaddingTop(6).Row(row =>
        {
            row.RelativeItem().Text(voucherNumber).FontSize(7.5f).FontColor(Muted);
            row.RelativeItem().AlignLeft().DefaultTextStyle(style => style.FontSize(7.5f).FontColor(Muted))
                .Text(text =>
                {
                    text.Span("صفحة ");
                    text.CurrentPageNumber();
                    text.Span(" / ");
                    text.TotalPages();
                });
        });
    }

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

    private static string Number(decimal value) => value.ToString("N2", WesternNumbers);
}
