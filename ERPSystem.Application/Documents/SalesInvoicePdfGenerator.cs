using System.Globalization;
using ERPSystem.Application.DTOs.Sales;
using ERPSystem.Domain.Enums;
using QuestPDF.Drawing;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace ERPSystem.Application.Documents;

/// <summary>
/// Single shared Sales Invoice PDF renderer for API and WPF.
/// Consumes existing calculated DTO values and never recalculates business totals.
/// </summary>
public sealed class SalesInvoicePdfGenerator
{
    private const string FontFamily = "Noto Sans Arabic";
    private const string Navy = "#071A2B";
    private const string NavySoft = "#102C45";
    private const string Gold = "#C99A4A";
    private const string GoldSoft = "#F6E8C9";
    private const string Paper = "#FFFCF5";
    private const string Muted = "#65717D";
    private const string Border = "#D9C9A7";

    private static readonly CultureInfo WesternNumbers = CultureInfo.InvariantCulture;
    private static readonly object SetupLock = new();
    private static bool _configured;

    private readonly string _logoPath;

    public SalesInvoicePdfGenerator(string fontPath, string logoPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fontPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(logoPath);
        _logoPath = logoPath;
        ConfigureQuestPdf(fontPath);
    }

    public static SalesInvoicePdfGenerator FromContentRoot(string contentRoot)
    {
        var (fontPath, logoPath) = SalesInvoicePdfAssetPaths.Resolve(contentRoot);
        return new SalesInvoicePdfGenerator(fontPath, logoPath);
    }

    public byte[] Generate(SalesInvoiceOperationsCenterDto operations)
    {
        ArgumentNullException.ThrowIfNull(operations);
        var invoice = operations.Invoice;

        return Document.Create(document =>
        {
            document.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(30);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(style => style
                    .FontFamily(FontFamily)
                    .FontSize(9)
                    .FontColor(Navy));
                page.ContentFromRightToLeft();

                page.Header().Element(container => ComposeHeader(container, invoice));
                page.Content().PaddingTop(12).Column(column =>
                {
                    column.Spacing(12);
                    column.Item().Element(container => ComposePartyDetails(container, operations));
                    column.Item().Element(container => ComposeLines(container, invoice.Lines));
                    column.Item().Element(container => ComposeTotals(container, invoice));
                });
                page.Footer().Element(container => ComposeFooter(container, invoice.InvoiceNumber));
            });
        }).GeneratePdf();
    }

    private static void ConfigureQuestPdf(string fontPath)
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

    private void ComposeHeader(IContainer container, SalesInvoiceDto invoice)
    {
        container.Column(column =>
        {
            column.Item().AlignCenter().Height(82).Width(100).Image(_logoPath).FitArea();
            column.Item().PaddingTop(5).LineHorizontal(2).LineColor(Gold);
            column.Item().PaddingTop(10).Row(row =>
            {
                row.RelativeItem(3).Column(meta =>
                {
                    meta.Item().AlignRight().Text("فاتورة بيع").FontSize(20).Bold().FontColor(Navy);
                    meta.Item().PaddingTop(3).Row(line =>
                    {
                        line.AutoItem().Text("رقم الفاتورة:").SemiBold();
                        line.AutoItem().PaddingRight(5).ContentFromLeftToRight()
                            .Text(invoice.InvoiceNumber).FontColor(Gold).SemiBold();
                    });
                    meta.Item().PaddingTop(2).Row(line =>
                    {
                        line.AutoItem().Text("التاريخ:").SemiBold();
                        line.AutoItem().PaddingRight(5).ContentFromLeftToRight()
                            .Text(invoice.InvoiceDate.ToString("yyyy-MM-dd", WesternNumbers));
                    });
                });

                row.RelativeItem(2).BorderRight(1).BorderColor(Border).PaddingRight(14).Column(company =>
                {
                    company.Item().AlignLeft().Text("شركة الأمل").FontSize(13).Bold().FontColor(Gold);
                    company.Item().AlignLeft().ContentFromLeftToRight().Text("ALAMAL.AB").FontSize(8).FontColor(Muted);
                    company.Item().AlignLeft().Text("العنوان: غير محدد").FontSize(8).FontColor(Muted);
                    company.Item().AlignLeft().Text("الهاتف: غير محدد").FontSize(8).FontColor(Muted);
                });
            });
        });
    }

    private static void ComposePartyDetails(IContainer container, SalesInvoiceOperationsCenterDto operations)
    {
        var invoice = operations.Invoice;
        container.Background(Paper).Border(1).BorderColor(Border).Padding(12).Row(row =>
        {
            row.RelativeItem().Column(customer =>
            {
                customer.Item().Text("بيانات العميل").FontSize(10).Bold().FontColor(Gold);
                customer.Item().PaddingTop(3)
                    .Text(string.IsNullOrWhiteSpace(invoice.CustomerName) ? "عميل غير محدد" : invoice.CustomerName)
                    .FontSize(12).SemiBold();
                if (!string.IsNullOrWhiteSpace(operations.CustomerPhone))
                {
                    customer.Item().PaddingTop(2).ContentFromLeftToRight()
                        .Text(operations.CustomerPhone).FontSize(8).FontColor(Muted);
                }
                customer.Item().PaddingTop(3).Row(balance =>
                {
                    balance.AutoItem().Text("آخر رصيد للعميل:").FontSize(9).SemiBold();
                    balance.AutoItem().PaddingRight(5).ContentFromLeftToRight()
                        .Text(Money(operations.CustomerBalance)).FontSize(9).FontColor(Gold).SemiBold();
                });
            });

            row.RelativeItem().AlignLeft().Column(details =>
            {
                details.Item().Text($"نوع الدفع: {PaymentTypeLabel(invoice.PaymentType)}").SemiBold();
                var warehouseName = string.IsNullOrWhiteSpace(operations.WarehouseName)
                    ? invoice.WarehouseName
                    : operations.WarehouseName;
                details.Item().PaddingTop(2)
                    .Text($"المستودع: {(string.IsNullOrWhiteSpace(warehouseName) ? "غير محدد" : warehouseName)}");
                details.Item().PaddingTop(2).Row(line =>
                {
                    line.AutoItem().Text("الحالة:");
                    line.AutoItem().PaddingRight(4).Text(StatusLabel(invoice.Status)).FontColor(Gold).SemiBold();
                });
            });
        });
    }

    private static void ComposeLines(IContainer container, IReadOnlyList<SalesInvoiceLineDto> lines)
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.RelativeColumn(2.6f);
                columns.RelativeColumn(1.3f);
                columns.RelativeColumn(1.05f);
                columns.RelativeColumn(1.2f);
                columns.RelativeColumn(1.35f);
                columns.RelativeColumn(1.2f);
                columns.RelativeColumn(1.2f);
                columns.RelativeColumn(1.45f);
            });

            table.Header(header =>
            {
                HeaderCell(header, "الصنف");
                HeaderCell(header, "اللون");
                HeaderCell(header, "عدد الأثواب");
                HeaderCell(header, "الطول");
                HeaderCell(header, "سعر الوحدة");
                HeaderCell(header, "الخصم");
                HeaderCell(header, "الضريبة");
                HeaderCell(header, "الإجمالي");
            });

            foreach (var line in lines.OrderBy(item => item.LineNumber))
            {
                BodyCell(table, $"{line.FabricDisplayName}\n{line.FabricCode}", TextAlign.Right);
                BodyCell(table, line.ColorDisplayName, TextAlign.Right);
                BodyCell(table, Integer(line.RollCount));
                BodyCell(table, Number(line.TotalLengthMeters));
                BodyCell(table, Money(line.UnitPrice));
                BodyCell(table, Money(line.DiscountAmount));
                BodyCell(table, Money(line.TaxAmount));
                BodyCell(table, Money(line.LineTotal));

                if (line.RollLengths.Count > 0)
                {
                    var breakdown = string.Join("     ", line.RollLengths.Select(r => $"({r.RollSequence}) {Number(r.LengthMeters)}"));
                    table.Cell().ColumnSpan(8).Background(Paper).BorderBottom(0.7f).BorderColor(Border)
                        .PaddingVertical(4).PaddingHorizontal(8).AlignRight()
                        .Text($"تفصيل أطوال الأثواب (م): {breakdown}").FontSize(7.5f).FontColor(Muted).Italic();
                }
            }
        });
    }

    private static void HeaderCell(TableCellDescriptor table, string text) =>
        table.Cell().Background(NavySoft).Border(0.5f).BorderColor(Gold)
            .PaddingVertical(7).PaddingHorizontal(4).AlignCenter().AlignMiddle()
            .Text(text).FontColor(Colors.White).FontSize(8).SemiBold();

    private static void BodyCell(TableDescriptor table, string text, TextAlign align = TextAlign.Center)
    {
        var cell = table.Cell().BorderBottom(0.7f).BorderColor(Border)
            .PaddingVertical(7).PaddingHorizontal(4).AlignMiddle();
        var aligned = align == TextAlign.Right ? cell.AlignRight() : cell.AlignCenter();
        aligned.Text(text).FontSize(8);
    }

    private static void ComposeTotals(IContainer container, SalesInvoiceDto invoice)
    {
        container.AlignLeft().Width(255).Border(1).BorderColor(Border).Column(column =>
        {
            var lineDiscount = invoice.Lines.Sum(line => line.DiscountAmount);
            TotalRow(column, "المجموع الفرعي", invoice.SubTotal);
            TotalRow(column, "خصم الأسطر", lineDiscount);
            TotalRow(column, "إجمالي الخصم", invoice.DiscountTotal);
            TotalRow(column, "إجمالي الضريبة", invoice.TaxTotal);
            if (Math.Abs(invoice.RoundingDifference) >= 0.01m)
                TotalRow(column, "فرق التقريب", invoice.RoundingDifference);

            column.Item().Background(Navy).PaddingVertical(9).PaddingHorizontal(10).Row(row =>
            {
                row.RelativeItem().Text("الإجمالي النهائي").FontColor(Colors.White).FontSize(11).Bold();
                row.ConstantItem(95).AlignLeft().ContentFromLeftToRight()
                    .Text(Money(invoice.GrandTotal)).FontColor(GoldSoft).FontSize(11).Bold();
            });
        });
    }

    private static void TotalRow(ColumnDescriptor column, string label, decimal value) =>
        column.Item().PaddingVertical(5).PaddingHorizontal(10).Row(row =>
        {
            row.RelativeItem().Text(label);
            row.ConstantItem(95).AlignLeft().ContentFromLeftToRight().Text(Money(value)).SemiBold();
        });

    private static void ComposeFooter(IContainer container, string invoiceNumber)
    {
        container.BorderTop(1).BorderColor(Gold).PaddingTop(6).Row(row =>
        {
            row.RelativeItem().Text("شكرًا لتعاملكم معنا").FontSize(8).FontColor(Muted);
            row.RelativeItem().AlignCenter().ContentFromLeftToRight().Text(invoiceNumber).FontSize(8).FontColor(Muted);
            row.RelativeItem().AlignLeft().DefaultTextStyle(style => style.FontSize(8).FontColor(Muted))
                .Text(text =>
                {
                    text.Span("صفحة ");
                    text.CurrentPageNumber();
                    text.Span(" / ");
                    text.TotalPages();
                });
        });
    }

    private static string PaymentTypeLabel(PaymentType value) => value switch
    {
        PaymentType.Cash => "نقدي",
        PaymentType.Credit => "آجل",
        _ => value.ToString()
    };

    private static string StatusLabel(SalesInvoiceStatus value) => value switch
    {
        SalesInvoiceStatus.Draft => "مسودة",
        SalesInvoiceStatus.AwaitingDetailing => "بانتظار التفصيل",
        SalesInvoiceStatus.Detailed => "تم التفصيل",
        SalesInvoiceStatus.ReadyForApproval => "بانتظار الاعتماد",
        SalesInvoiceStatus.Approved => "معتمدة",
        SalesInvoiceStatus.Printed => "مطبوعة",
        SalesInvoiceStatus.Delivered => "مسلّمة",
        SalesInvoiceStatus.PartiallyReturned => "مرتجع جزئي",
        SalesInvoiceStatus.Returned => "مرتجعة",
        SalesInvoiceStatus.Cancelled => "ملغاة",
        _ => value.ToString()
    };

    private static string Integer(int value) => value.ToString("0", WesternNumbers);
    private static string Number(decimal value) => value.ToString("0.##", WesternNumbers);
    private static string Money(decimal value) => value.ToString("N2", WesternNumbers);

    private enum TextAlign
    {
        Center,
        Right
    }
}
