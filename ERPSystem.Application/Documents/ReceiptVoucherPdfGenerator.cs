using System.Globalization;
using ERPSystem.Application.DTOs.Finance;
using ERPSystem.Domain.Enums;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using static ERPSystem.Application.Documents.FinanceDocumentTheme;

namespace ERPSystem.Application.Documents;

/// <summary>Shared Receipt Voucher (سند قبض) PDF renderer for API and WPF.</summary>
public sealed class ReceiptVoucherPdfGenerator
{
    private static readonly CultureInfo WesternNumbers = CultureInfo.InvariantCulture;
    private readonly string _logoPath;

    public ReceiptVoucherPdfGenerator(string fontPath, string logoPath)
    {
        ConfigureQuestPdf(fontPath);
        _logoPath = logoPath;
    }

    public static ReceiptVoucherPdfGenerator FromContentRoot(string contentRoot)
    {
        var (fontPath, logoPath) = ResolveAssets(contentRoot);
        return new ReceiptVoucherPdfGenerator(fontPath, logoPath);
    }

    public byte[] Generate(ReceiptVoucherPrintDto voucher)
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
                    if (voucher.Allocations.Count > 0)
                        column.Item().Element(container => ComposeAllocations(container, voucher.Allocations));
                    column.Item().Element(ComposeSignatures);
                });
                page.Footer().Element(container => ComposeFooter(container, voucher.VoucherNumber));
            });
        }).GeneratePdf();
    }

    private void ComposeHeader(IContainer container, ReceiptVoucherPrintDto voucher)
    {
        container.Column(column =>
        {
            column.Item().AlignCenter().Height(56).Width(70).Image(_logoPath).FitArea();
            column.Item().PaddingTop(4).LineHorizontal(2).LineColor(Gold);
            column.Item().PaddingTop(8).AlignCenter().Text("سند قبض").FontSize(18).Bold().FontColor(Navy);
            column.Item().AlignCenter().PaddingTop(4).Background(GreenSoft).Border(1).BorderColor(Green)
                .PaddingVertical(3).PaddingHorizontal(10).Text(StatusLabel(voucher.Status))
                .FontSize(9).Bold().FontColor(Green);
        });
    }

    private static void ComposeParties(IContainer container, ReceiptVoucherPrintDto voucher) =>
        container.Row(row =>
        {
            row.RelativeItem().Background(Paper).Border(1).BorderColor(Border).Padding(8).Column(col =>
            {
                col.Item().Text("بيانات السند").FontSize(9).Bold().FontColor(Gold);
                InfoRow(col, "رقم السند", voucher.VoucherNumber);
                InfoRow(col, "التاريخ", voucher.VoucherDate.ToString("yyyy-MM-dd", WesternNumbers));
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

    private static void ComposeAmount(IContainer container, ReceiptVoucherPrintDto voucher) =>
        container.Background(GreenSoft).Border(1).BorderColor(Green).Padding(12).Column(col =>
        {
            col.Item().AlignCenter().Text("المبلغ المستلم").FontSize(9).FontColor(Muted);
            col.Item().PaddingTop(3).AlignCenter().ContentFromLeftToRight()
                .Text($"{voucher.Currency} {Number(voucher.Amount)}").FontSize(22).Bold().FontColor(Green);
        });

    private static void ComposeAllocations(IContainer container, IReadOnlyList<ReceiptVoucherAllocationDto> allocations) =>
        container.Column(col =>
        {
            col.Item().Text("مقابل فواتير").FontSize(9).Bold().FontColor(Gold);
            col.Item().PaddingTop(4).Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(2);
                    columns.RelativeColumn(1);
                });
                table.Header(header =>
                {
                    header.Cell().Element(HeaderCell).Text("رقم الفاتورة");
                    header.Cell().Element(HeaderCell).AlignCenter().Text("المبلغ المخصص");
                });
                foreach (var allocation in allocations)
                {
                    table.Cell().Element(BodyCell).Text(allocation.InvoiceNumber);
                    table.Cell().Element(BodyCell).AlignCenter().Text(Number(allocation.Amount));
                }

                static IContainer HeaderCell(IContainer x) =>
                    x.Background(NavySoft).PaddingVertical(5).PaddingHorizontal(6).AlignRight()
                        .DefaultTextStyle(s => s.FontColor(Colors.White).FontSize(8).SemiBold());
                static IContainer BodyCell(IContainer x) =>
                    x.BorderBottom(0.7f).BorderColor(Border).PaddingVertical(5).PaddingHorizontal(6)
                        .DefaultTextStyle(s => s.FontSize(8.5f));
            });
        });

    private static void ComposeSignatures(IContainer container) =>
        container.PaddingTop(16).Row(row =>
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
        VoucherStatus.Posted => "تم الاستلام",
        VoucherStatus.Cancelled => "ملغى",
        VoucherStatus.Reversed => "معكوس",
        _ => status.ToString()
    };

    private static string Number(decimal value) => value.ToString("N2", WesternNumbers);
}
