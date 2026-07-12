using System.Globalization;
using ERPSystem.Application.DTOs.Purchases;
using ERPSystem.Domain.Enums;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using static ERPSystem.Application.Documents.FinanceDocumentTheme;

namespace ERPSystem.Application.Documents;

/// <summary>Shared purchase invoice PDF renderer (API + WPF).</summary>
public sealed class PurchaseInvoicePdfGenerator
{
    private static readonly CultureInfo WesternNumbers = CultureInfo.InvariantCulture;
    private readonly string _logoPath;

    public PurchaseInvoicePdfGenerator(string fontPath, string logoPath)
    {
        ConfigureQuestPdf(fontPath);
        _logoPath = logoPath;
    }

    public static PurchaseInvoicePdfGenerator FromContentRoot(string contentRoot)
    {
        var (fontPath, logoPath) = ResolveAssets(contentRoot);
        return new PurchaseInvoicePdfGenerator(fontPath, logoPath);
    }

    public byte[] Generate(PurchaseInvoiceDetailsDto invoice)
    {
        ArgumentNullException.ThrowIfNull(invoice);

        return Document.Create(document =>
        {
            document.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(28);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(style => style
                    .FontFamily(FontFamily)
                    .FontSize(9f)
                    .FontColor(Navy));
                page.ContentFromRightToLeft();

                page.Header().Element(c => ComposeHeader(c, invoice));
                page.Content().PaddingTop(10).Column(column =>
                {
                    column.Spacing(10);
                    column.Item().Element(c => ComposeSupplier(c, invoice));
                    column.Item().Element(c => ComposeLines(c, invoice.Lines));
                    column.Item().AlignLeft().Element(c => ComposeTotals(c, invoice));
                    if (!string.IsNullOrWhiteSpace(invoice.Notes))
                        column.Item().Text($"ملاحظات: {invoice.Notes}").FontSize(8).FontColor(Muted);
                });
                page.Footer().Element(c => ComposeFooter(c, invoice.InvoiceNumber));
            });
        }).GeneratePdf();
    }

    private void ComposeHeader(IContainer container, PurchaseInvoiceDetailsDto invoice)
    {
        container.Column(column =>
        {
            column.Item().AlignCenter().Height(72).Width(88).Image(_logoPath).FitArea();
            column.Item().PaddingTop(4).LineHorizontal(2).LineColor(Gold);
            column.Item().PaddingTop(8).Row(row =>
            {
                row.RelativeItem().Column(meta =>
                {
                    meta.Item().AlignRight().Text("فاتورة مشتريات").FontSize(18).Bold().FontColor(Navy);
                    meta.Item().PaddingTop(2).Row(line =>
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
                    meta.Item().PaddingTop(2).Row(line =>
                    {
                        line.AutoItem().Text("الاستحقاق:").SemiBold();
                        line.AutoItem().PaddingRight(5).ContentFromLeftToRight()
                            .Text(invoice.DueDate.ToString("yyyy-MM-dd", WesternNumbers));
                    });
                });
                row.RelativeItem().AlignLeft().Column(company =>
                {
                    company.Item().Text("شركة الأمل").FontSize(12).Bold().FontColor(Gold);
                    company.Item().ContentFromLeftToRight().Text("ALAMAL.AB").FontSize(8).FontColor(Muted);
                    company.Item().Text($"الحالة: {invoice.StatusDisplay}").FontSize(9).FontColor(Navy);
                });
            });
        });
    }

    private static void ComposeSupplier(IContainer container, PurchaseInvoiceDetailsDto invoice) =>
        container.Background(Paper).Border(1).BorderColor(Border).Padding(10).Column(col =>
        {
            col.Item().Text("بيانات المورد").FontSize(10).Bold().FontColor(Gold);
            col.Item().PaddingTop(3).Text(invoice.SupplierName).FontSize(12).SemiBold();
            if (!string.IsNullOrWhiteSpace(invoice.SupplierReference))
                col.Item().PaddingTop(2).Text($"مرجع المورد: {invoice.SupplierReference}").FontSize(9);
            if (!string.IsNullOrWhiteSpace(invoice.WarehouseName))
                col.Item().PaddingTop(2).Text($"المستودع: {invoice.WarehouseName}").FontSize(9);
        });

    private static void ComposeLines(IContainer container, IReadOnlyList<PurchaseInvoiceLineDto> lines)
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.ConstantColumn(24);
                columns.RelativeColumn(4);
                columns.RelativeColumn(1.2f);
                columns.RelativeColumn(1.2f);
                columns.RelativeColumn(1.2f);
            });

            table.Header(header =>
            {
                HeaderCell(header, "#");
                HeaderCell(header, "البيان");
                HeaderCell(header, "الكمية");
                HeaderCell(header, "السعر");
                HeaderCell(header, "الإجمالي");
            });

            var i = 1;
            foreach (var line in lines)
            {
                var desc = line.LineType == PurchaseLineType.Inventory
                    ? (line.FabricItemName ?? line.Description)
                    : line.Description;
                var qty = line.QuantityMeters != 0 ? line.QuantityMeters : line.RollCount;
                BodyCell(table, i.ToString(WesternNumbers));
                BodyCell(table, desc, TextAlign.Right);
                BodyCell(table, Number(qty));
                BodyCell(table, Number(line.UnitPrice));
                BodyCell(table, Number(line.LineTotal));
                i++;
            }
        });
    }

    private static void ComposeTotals(IContainer container, PurchaseInvoiceDetailsDto invoice) =>
        container.Width(260).Border(1).BorderColor(Border).Column(col =>
        {
            TotalRow(col, "الإجمالي الفرعي", invoice.SubTotal);
            if (invoice.DiscountAmount != 0) TotalRow(col, "الخصم", -invoice.DiscountAmount);
            if (invoice.TaxAmount != 0) TotalRow(col, "الضريبة", invoice.TaxAmount);
            col.Item().Background(Navy).PaddingVertical(6).PaddingHorizontal(10).Row(row =>
            {
                row.RelativeItem().Text("الإجمالي").FontColor(Colors.White).Bold();
                row.ConstantItem(90).AlignLeft().ContentFromLeftToRight()
                    .Text(Number(invoice.TotalAmount)).FontColor(GoldSoft).Bold();
            });
            if (invoice.PaidAmount != 0)
            {
                TotalRow(col, "المدفوع", invoice.PaidAmount);
                TotalRow(col, "المتبقي", invoice.RemainingAmount);
            }
        });

    private static void TotalRow(ColumnDescriptor column, string label, decimal value) =>
        column.Item().PaddingVertical(3).PaddingHorizontal(10).Row(row =>
        {
            row.RelativeItem().Text(label).FontSize(9);
            row.ConstantItem(90).AlignLeft().ContentFromLeftToRight().Text(Number(value)).FontSize(9).SemiBold();
        });

    private static void HeaderCell(TableCellDescriptor table, string text) =>
        table.Cell().Background(NavySoft).Border(0.5f).BorderColor(Gold)
            .PaddingVertical(6).PaddingHorizontal(4).AlignCenter().AlignMiddle()
            .Text(text).FontColor(Colors.White).FontSize(8).SemiBold();

    private static void BodyCell(TableDescriptor table, string text, TextAlign align = TextAlign.Center)
    {
        var cell = table.Cell().BorderBottom(0.7f).BorderColor(Border)
            .PaddingVertical(5).PaddingHorizontal(4).AlignMiddle();
        var aligned = align == TextAlign.Right ? cell.AlignRight() : cell.AlignCenter();
        aligned.Text(text).FontSize(8.5f);
    }

    private static void ComposeFooter(IContainer container, string invoiceNumber) =>
        container.AlignCenter().Text(text =>
        {
            text.Span($"فاتورة مشتريات {invoiceNumber} — صفحة ").FontSize(8).FontColor(Muted);
            text.CurrentPageNumber().FontSize(8).FontColor(Muted);
            text.Span(" / ").FontSize(8).FontColor(Muted);
            text.TotalPages().FontSize(8).FontColor(Muted);
        });

    private static string Number(decimal value) => value.ToString("N2", WesternNumbers);

    private enum TextAlign { Center, Right }
}
