using System.Globalization;
using ERPSystem.Application.DTOs.Inventory;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using static ERPSystem.Application.Documents.FinanceDocumentTheme;

namespace ERPSystem.Application.Documents;

/// <summary>Warehouse stock report PDF (API + WPF).</summary>
public sealed class WarehouseStockReportPdfGenerator
{
    private static readonly CultureInfo WesternNumbers = CultureInfo.InvariantCulture;
    private readonly string _logoPath;

    public WarehouseStockReportPdfGenerator(string fontPath, string logoPath)
    {
        ConfigureQuestPdf(fontPath);
        _logoPath = logoPath;
    }

    public static WarehouseStockReportPdfGenerator FromContentRoot(string contentRoot)
    {
        var (fontPath, logoPath) = ResolveAssets(contentRoot);
        return new WarehouseStockReportPdfGenerator(fontPath, logoPath);
    }

    public byte[] Generate(InventoryOperationsCenterDto data)
    {
        ArgumentNullException.ThrowIfNull(data);
        var warehouse = data.Warehouse;

        return Document.Create(document =>
        {
            document.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(24);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(style => style
                    .FontFamily(FontFamily)
                    .FontSize(8f)
                    .FontColor(Navy));
                page.ContentFromRightToLeft();

                page.Header().Element(c => ComposeHeader(c, warehouse, data));
                page.Content().PaddingTop(8).Column(column =>
                {
                    column.Spacing(8);
                    column.Item().Element(ComposeSummary);
                    column.Item().Element(c => ComposeStockTable(c, data.Stock));
                });
                page.Footer().Element(c => ComposeFooter(c, warehouse.NameAr));
            });
        }).GeneratePdf();

        void ComposeSummary(IContainer container) =>
            container.Row(row =>
            {
                SummaryCard(row, "قيمة المخزون", Number(data.InventoryValue));
                SummaryCard(row, "Rolls", warehouse.RollCount.ToString(WesternNumbers));
                SummaryCard(row, "الأمتار", Number(warehouse.TotalMeters));
                SummaryCard(row, "تحويلات معلقة", data.PendingTransfers.ToString(WesternNumbers));
            });
    }

    private void ComposeHeader(IContainer container, WarehouseListExtendedDto warehouse, InventoryOperationsCenterDto data)
    {
        container.Column(column =>
        {
            column.Item().AlignCenter().Height(56).Width(72).Image(_logoPath).FitArea();
            column.Item().PaddingTop(4).LineHorizontal(2).LineColor(Gold);
            column.Item().PaddingTop(6).Row(row =>
            {
                row.RelativeItem().Column(meta =>
                {
                    meta.Item().AlignRight().Text("تقرير مخزون مستودع").FontSize(16).Bold().FontColor(Navy);
                    meta.Item().PaddingTop(2).Text($"{warehouse.NameAr} ({warehouse.Code})").FontSize(10).FontColor(Gold).SemiBold();
                    if (!string.IsNullOrWhiteSpace(data.CostCenterName))
                        meta.Item().PaddingTop(2).Text($"مركز التكلفة: {data.CostCenterName}").FontSize(8).FontColor(Muted);
                });
                row.RelativeItem().AlignLeft().Column(company =>
                {
                    company.Item().Text("شركة الأمل").FontSize(11).Bold().FontColor(Gold);
                    company.Item().Text($"أُنشئ: {DateTime.Now:yyyy-MM-dd HH:mm}").FontSize(8).FontColor(Muted);
                });
            });
        });
    }

    private static void ComposeStockTable(IContainer container, IReadOnlyList<FabricStockBalanceDto> stock)
    {
        if (stock.Count == 0)
        {
            container.AlignCenter().Text("لا توجد أرصدة مخزون.").FontSize(10).FontColor(Muted);
            return;
        }

        container.Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.RelativeColumn(2f);
                columns.RelativeColumn(1.2f);
                columns.RelativeColumn(1.2f);
                columns.RelativeColumn(0.8f);
                columns.RelativeColumn(1f);
                columns.RelativeColumn(1f);
                columns.RelativeColumn(1f);
                columns.RelativeColumn(1f);
            });

            table.Header(header =>
            {
                HeaderCell(header, "القماش");
                HeaderCell(header, "اللون");
                HeaderCell(header, "الحاوية");
                HeaderCell(header, "Rolls");
                HeaderCell(header, "أمتار");
                HeaderCell(header, "محجوز");
                HeaderCell(header, "متاح");
                HeaderCell(header, "قيمة");
            });

            foreach (var row in stock)
            {
                BodyCell(table, row.FabricName, TextAlign.Right);
                BodyCell(table, row.ColorName, TextAlign.Right);
                BodyCell(table, row.ContainerNumber, TextAlign.Right);
                BodyCell(table, row.RollCount.ToString(WesternNumbers));
                BodyCell(table, Number(row.TotalMeters));
                BodyCell(table, Number(row.ReservedMeters));
                BodyCell(table, Number(row.AvailableMeters));
                BodyCell(table, Number(row.InventoryValue));
            }
        });
    }

    private static void SummaryCard(RowDescriptor row, string label, string value) =>
        row.RelativeItem().Padding(2).Background(Paper).Border(1).BorderColor(Border).Padding(8).Column(col =>
        {
            col.Item().AlignCenter().Text(label).FontSize(8).FontColor(Muted);
            col.Item().PaddingTop(2).AlignCenter().ContentFromLeftToRight().Text(value).FontSize(12).Bold().FontColor(Navy);
        });

    private static void HeaderCell(TableCellDescriptor table, string text) =>
        table.Cell().Background(NavySoft).Border(0.5f).BorderColor(Gold)
            .PaddingVertical(5).PaddingHorizontal(3).AlignCenter().AlignMiddle()
            .Text(text).FontColor(Colors.White).FontSize(7.5f).SemiBold();

    private static void BodyCell(TableDescriptor table, string text, TextAlign align = TextAlign.Center)
    {
        var cell = table.Cell().BorderBottom(0.6f).BorderColor(Border)
            .PaddingVertical(4).PaddingHorizontal(3).AlignMiddle();
        var aligned = align == TextAlign.Right ? cell.AlignRight() : cell.AlignCenter();
        aligned.Text(text).FontSize(7.5f);
    }

    private static void ComposeFooter(IContainer container, string warehouseName) =>
        container.AlignCenter().Text(text =>
        {
            text.Span($"مخزون {warehouseName} — صفحة ").FontSize(8).FontColor(Muted);
            text.CurrentPageNumber().FontSize(8).FontColor(Muted);
            text.Span(" / ").FontSize(8).FontColor(Muted);
            text.TotalPages().FontSize(8).FontColor(Muted);
        });

    private static string Number(decimal value) => value.ToString("N2", WesternNumbers);

    private enum TextAlign { Center, Right }
}
