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
    private const string FontFamily = FinanceDocumentTheme.FontFamily;
    private const string Navy = "#071A2B";
    private const string NavySoft = "#102C45";
    private const string Gold = "#C99A4A";
    private const string GoldSoft = "#F6E8C9";
    private const string Paper = "#FFFCF5";
    private const string Muted = "#65717D";
    private const string Border = "#D9C9A7";

    private static readonly CultureInfo WesternNumbers = CultureInfo.InvariantCulture;

    private readonly string _logoPath;

    public SalesInvoicePdfGenerator(string fontPath, string logoPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fontPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(logoPath);
        FinanceDocumentTheme.ConfigureQuestPdf(fontPath);
        _logoPath = logoPath;
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

                page.Header().ShowOnce().Element(container => ComposeHeader(container, invoice));
                page.Content().PaddingTop(12).Column(column =>
                {
                    column.Spacing(12);
                    column.Item().Element(container => ComposePartyDetails(container, operations));
                    column.Item().Element(container => ComposeLineSummary(container, invoice.Lines));
                    foreach (var line in invoice.Lines
                                 .Where(item => item.RollLengths.Count > 0)
                                 .OrderBy(item => item.LineNumber))
                    {
                        column.Item().Element(container => ComposeRollDetailGrid(container, line));
                    }
                    column.Item().Element(container => ComposeTotals(container, invoice));
                });
                page.Footer().Element(container => ComposeFooter(container, invoice.InvoiceNumber));
            });
        }).GeneratePdf();
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

    private static void ComposeLineSummary(IContainer container, IReadOnlyList<SalesInvoiceLineDto> lines)
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.RelativeColumn(3.1f);
                columns.RelativeColumn(1.7f);
                columns.RelativeColumn(1.25f);
                columns.RelativeColumn(1.55f);
                columns.RelativeColumn(1.65f);
            });

            table.Header(header =>
            {
                HeaderCell(header, "الصنف");
                HeaderCell(header, "اللون");
                HeaderCell(header, "عدد الأثواب");
                HeaderCell(header, "الطول");
                HeaderCell(header, "الإجمالي");
            });

            foreach (var line in lines.OrderBy(item => item.LineNumber))
            {
                BodyCell(table, $"{line.FabricDisplayName}\n{line.FabricCode}", TextAlign.Right);
                BodyCell(table, line.ColorDisplayName, TextAlign.Right);
                BodyCell(table, Integer(line.RollCount));
                BodyCell(table, line.TotalLengthDisplay);
                BodyCell(table, Money(line.LineTotal));

            }
        });
    }

    private static void ComposeRollDetailGrid(IContainer container, SalesInvoiceLineDto line)
    {
        const int groupsPerRow = 5;
        var rolls = line.RollLengths
            .OrderBy(roll => roll.RollNumber ?? roll.RollSequence)
            .ThenBy(roll => roll.RollSequence)
            .ToList();

        container.Column(column =>
        {
            column.Item().Background(GoldSoft).Border(1).BorderColor(Border)
                .PaddingVertical(6).PaddingHorizontal(8).Row(row =>
                {
                    row.RelativeItem().AlignRight()
                        .Text($"تفنيد الأطوال - {line.FabricDisplayName} / {line.ColorDisplayName}")
                        .FontSize(9).SemiBold().FontColor(Navy);
                    row.AutoItem().ContentFromLeftToRight()
                        .Text($"{Integer(rolls.Count)} توب")
                        .FontSize(8).SemiBold().FontColor(Gold);
                });

            column.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    for (var group = 0; group < groupsPerRow; group++)
                    {
                        columns.RelativeColumn(0.8f);
                        columns.RelativeColumn(1.15f);
                    }
                });

                table.Header(header =>
                {
                    for (var group = 0; group < groupsPerRow; group++)
                    {
                        RollHeaderCell(header, "رقم التوب");
                        RollHeaderCell(header, "الطول (م)");
                    }
                });

                for (var offset = 0; offset < rolls.Count; offset += groupsPerRow)
                {
                    for (var group = 0; group < groupsPerRow; group++)
                    {
                        var index = offset + group;
                        if (index < rolls.Count)
                        {
                            var roll = rolls[index];
                            RollBodyCell(table, Integer(roll.RollNumber ?? roll.RollSequence), true);
                            RollBodyCell(table, Number(roll.LengthMeters), false);
                        }
                        else
                        {
                            RollBodyCell(table, "", true);
                            RollBodyCell(table, "", false);
                        }
                    }
                }
            });
        });
    }

    private static void RollHeaderCell(TableCellDescriptor table, string text) =>
        table.Cell().Background(NavySoft).Border(0.5f).BorderColor(Gold)
            .PaddingVertical(4).PaddingHorizontal(2).AlignCenter().AlignMiddle()
            .Text(text).FontColor(Colors.White).FontSize(6.8f).SemiBold();

    private static void RollBodyCell(TableDescriptor table, string text, bool isRollNumber) =>
        table.Cell().Background(isRollNumber ? Paper : Colors.White)
            .Border(0.45f).BorderColor(Border)
            .PaddingVertical(3.5f).PaddingHorizontal(2)
            .AlignCenter().AlignMiddle().ContentFromLeftToRight()
            .Text(text).FontSize(7.2f).SemiBold();

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
            TotalRow(column, "إجمالي عدد الأتواب", invoice.Lines.Sum(line => line.RollCount), false);
            TotalRow(column, "المجموع الفرعي", invoice.SubTotal);
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

    private static void TotalRow(ColumnDescriptor column, string label, decimal value, bool isMoney = true) =>
        column.Item().PaddingVertical(5).PaddingHorizontal(10).Row(row =>
        {
            row.RelativeItem().Text(label);
            row.ConstantItem(95).AlignLeft().ContentFromLeftToRight()
                .Text(isMoney ? Money(value) : Integer(decimal.ToInt32(value))).SemiBold();
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
