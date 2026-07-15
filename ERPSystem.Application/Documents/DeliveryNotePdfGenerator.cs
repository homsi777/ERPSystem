using System.Globalization;
using ERPSystem.Application.DTOs.Sales;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using static ERPSystem.Application.Documents.FinanceDocumentTheme;

namespace ERPSystem.Application.Documents;

/// <summary>Delivery note PDF — same field mapping as legacy inline renderer, shared theme/font.</summary>
public sealed class DeliveryNotePdfGenerator
{
    private static readonly CultureInfo WesternNumbers = CultureInfo.InvariantCulture;
    private readonly string _logoPath;

    public DeliveryNotePdfGenerator(string fontPath, string logoPath)
    {
        ConfigureQuestPdf(fontPath);
        _logoPath = logoPath;
    }

    public static DeliveryNotePdfGenerator FromContentRoot(string contentRoot)
    {
        var (fontPath, logoPath) = ResolveAssets(contentRoot);
        return new DeliveryNotePdfGenerator(fontPath, logoPath);
    }

    public byte[] Generate(SalesInvoiceDto invoice, string customerName)
    {
        ArgumentNullException.ThrowIfNull(invoice);

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(30);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(t => t.FontFamily(FontFamily).FontSize(10));
                page.ContentFromRightToLeft();

                page.Header().Row(row =>
                {
                    row.RelativeItem().Column(c =>
                    {
                        c.Item().Text("إشعار تسليم بضاعة").FontSize(18).Bold().FontColor(Navy);
                        c.Item().Text($"مرجع الفاتورة: {invoice.InvoiceNumber}").FontSize(10);
                        c.Item().Text($"تاريخ الإصدار: {invoice.InvoiceDate:yyyy-MM-dd}").FontSize(10);
                    });
                    row.ConstantItem(100).Height(60).AlignCenter().AlignMiddle()
                        .Image(_logoPath).FitArea();
                });

                page.Content().Column(col =>
                {
                    col.Spacing(12);
                    col.Item().Element(e => ComposeCustomerBox(e, invoice, customerName));
                    col.Item().Element(e => ComposeLinesTable(e, invoice.Lines));

                    col.Item().PaddingTop(30).Row(r =>
                    {
                        r.RelativeItem().Column(c =>
                        {
                            c.Item().Text("سلّم:").Bold();
                            c.Item().PaddingTop(30).LineHorizontal(1).LineColor(Border);
                            c.Item().Text("التوقيع / التاريخ").FontSize(9).FontColor(Muted);
                        });
                        r.ConstantItem(40);
                        r.RelativeItem().Column(c =>
                        {
                            c.Item().Text("استلم:").Bold();
                            c.Item().PaddingTop(30).LineHorizontal(1).LineColor(Border);
                            c.Item().Text("التوقيع / التاريخ").FontSize(9).FontColor(Muted);
                        });
                    });
                });

                page.Footer().AlignCenter()
                    .Text($"إشعار تسليم — {invoice.InvoiceNumber}").FontSize(8).FontColor(Muted);
            });
        }).GeneratePdf();
    }

    private static void ComposeCustomerBox(IContainer c, SalesInvoiceDto invoice, string customerName)
    {
        c.Background(GoldSoft).Padding(10).Column(col =>
        {
            col.Item().Text("بيانات العميل").FontSize(11).Bold().FontColor(Navy);
            var displayName = string.IsNullOrWhiteSpace(invoice.CustomerName)
                ? customerName
                : invoice.CustomerName;
            col.Item().Text(string.IsNullOrWhiteSpace(displayName) ? "عميل غير محدد" : displayName)
                .FontSize(12).SemiBold();
            col.Item().PaddingTop(3)
                .Text($"المستودع: {(string.IsNullOrWhiteSpace(invoice.WarehouseName) ? "غير محدد" : invoice.WarehouseName)}")
                .FontSize(10);
        });
    }

    private static void ComposeLinesTable(IContainer c, IReadOnlyList<SalesInvoiceLineDto> lines) =>
        c.Table(t =>
        {
            t.ColumnsDefinition(cd =>
            {
                cd.RelativeColumn(3);
                cd.RelativeColumn(2);
                cd.ConstantColumn(50);
                cd.RelativeColumn(2);
                cd.RelativeColumn(2);
                cd.RelativeColumn(2);
                cd.RelativeColumn(2);
                cd.RelativeColumn(2);
            });

            t.Header(h =>
            {
                h.Cell().Element(HeaderCell).Text("الصنف");
                h.Cell().Element(HeaderCell).Text("اللون");
                h.Cell().Element(HeaderCell).AlignCenter().Text("عدد الأثواب");
                h.Cell().Element(HeaderCell).AlignRight().Text("الطول");
                h.Cell().Element(HeaderCell).AlignRight().Text("سعر الوحدة");
                h.Cell().Element(HeaderCell).AlignRight().Text("الخصم");
                h.Cell().Element(HeaderCell).AlignRight().Text("الضريبة");
                h.Cell().Element(HeaderCell).AlignRight().Text("الإجمالي");
            });

            foreach (var line in lines)
            {
                t.Cell().Element(BodyCell).Text($"{line.FabricDisplayName} ({line.FabricCode})");
                t.Cell().Element(BodyCell).Text(line.ColorDisplayName);
                t.Cell().Element(BodyCell).AlignCenter().Text(line.RollCount.ToString(WesternNumbers));
                t.Cell().Element(BodyCell).AlignRight().Text(line.TotalLengthDisplay);
                t.Cell().Element(BodyCell).AlignRight().Text($"{line.UnitPrice:N2}");
                t.Cell().Element(BodyCell).AlignRight().Text($"{line.DiscountAmount:N2}");
                t.Cell().Element(BodyCell).AlignRight().Text(line.TaxAmount > 0 ? $"{line.TaxAmount:N2}" : "—");
                t.Cell().Element(BodyCell).AlignRight().Text($"{line.LineTotal:N2}");

                if (line.RollLengths.Count > 0)
                {
                    var breakdown = string.Join("     ",
                        line.RollLengths.Select(r => $"({r.RollSequence}) {r.LengthMeters:N2}"));
                    t.Cell().ColumnSpan(8).Element(BreakdownCell)
                        .Text($"تفصيل أطوال الأثواب (م): {breakdown}").FontSize(8.5f).Italic().FontColor(Muted);
                }
            }

            static IContainer HeaderCell(IContainer x) =>
                x.Background(NavySoft).Padding(6).DefaultTextStyle(s => s.Bold().FontSize(10).FontColor(Colors.White));

            static IContainer BodyCell(IContainer x) =>
                x.Padding(6).BorderBottom(1).BorderColor(Border);

            static IContainer BreakdownCell(IContainer x) =>
                x.Background(Paper).PaddingVertical(3).PaddingHorizontal(8).BorderBottom(1).BorderColor(Border).AlignRight();
        });
}
