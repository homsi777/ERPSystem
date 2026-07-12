using System.Globalization;
using ERPSystem.Application.DTOs.Capital;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using static ERPSystem.Application.Documents.FinanceDocumentTheme;

namespace ERPSystem.Application.Documents;

/// <summary>Capital / partner report PDF (API + WPF).</summary>
public sealed class CapitalReportPdfGenerator
{
    private static readonly CultureInfo WesternNumbers = CultureInfo.InvariantCulture;
    private readonly string _logoPath;

    public CapitalReportPdfGenerator(string fontPath, string logoPath)
    {
        ConfigureQuestPdf(fontPath);
        _logoPath = logoPath;
    }

    public static CapitalReportPdfGenerator FromContentRoot(string contentRoot)
    {
        var (fontPath, logoPath) = ResolveAssets(contentRoot);
        return new CapitalReportPdfGenerator(fontPath, logoPath);
    }

    public byte[] Generate(CapitalReportDto report)
    {
        ArgumentNullException.ThrowIfNull(report);
        return Generate(report.Title, report.GeneratedAt, report.Rows, report.TotalBase, report.BaseCurrency);
    }

    public byte[] GeneratePartner(CapitalOperationsCenterDto operations)
    {
        ArgumentNullException.ThrowIfNull(operations);
        var d = operations.Details;
        var f = operations.Financial;

        var rows = new List<CapitalReportRowDto>
        {
            new() { Key = "capital", Label = "رأس المال الحالي", Amount = f.CurrentCapitalBase },
            new() { Key = "invest", Label = "إجمالي الاستثمارات", Amount = f.TotalInvestmentsBase },
            new() { Key = "withdraw", Label = "إجمالي السحوبات", Amount = f.TotalWithdrawalsBase },
            new() { Key = "profit", Label = "أرباح موزعة", Amount = f.DistributedProfitBase },
            new() { Key = "undist", Label = "أرباح غير موزعة", Amount = f.UndistributedProfitBase }
        };

        foreach (var scope in operations.ScopeSummaries)
        {
            rows.Add(new CapitalReportRowDto
            {
                Key = scope.ScopeDisplay,
                Label = scope.ScopeDisplay,
                SubLabel = $"{scope.Count} مشاركة",
                Amount = scope.CapitalBase
            });
        }

        return Generate(
            $"تقرير شريك — {d.FullName}",
            DateTime.UtcNow,
            rows,
            f.CurrentCapitalBase,
            f.BaseCurrency);
    }

    private byte[] Generate(
        string title,
        DateTime generatedAt,
        IReadOnlyList<CapitalReportRowDto> rows,
        decimal totalBase,
        string baseCurrency)
    {
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

                page.Header().Element(c => ComposeHeader(c, title, generatedAt));
                page.Content().PaddingTop(10).Column(column =>
                {
                    column.Spacing(10);
                    column.Item().Element(c => ComposeRows(c, rows));
                    column.Item().AlignLeft().Element(c => ComposeTotal(c, totalBase, baseCurrency));
                });
                page.Footer().Element(c => ComposeFooter(c, title));
            });
        }).GeneratePdf();
    }

    private void ComposeHeader(IContainer container, string title, DateTime generatedAt)
    {
        container.Column(column =>
        {
            column.Item().AlignCenter().Height(56).Width(72).Image(_logoPath).FitArea();
            column.Item().PaddingTop(4).LineHorizontal(2).LineColor(Gold);
            column.Item().PaddingTop(6).AlignRight().Text(title).FontSize(16).Bold().FontColor(Navy);
            column.Item().PaddingTop(2).AlignRight()
                .Text($"أُنشئ: {generatedAt.ToLocalTime():yyyy-MM-dd HH:mm}").FontSize(8).FontColor(Muted);
        });
    }

    private static void ComposeRows(IContainer container, IReadOnlyList<CapitalReportRowDto> rows)
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.RelativeColumn(1.2f);
                columns.RelativeColumn(2.5f);
                columns.RelativeColumn(1.5f);
                columns.RelativeColumn(1.2f);
            });

            table.Header(header =>
            {
                HeaderCell(header, "المفتاح");
                HeaderCell(header, "الوصف");
                HeaderCell(header, "تفاصيل");
                HeaderCell(header, "المبلغ");
            });

            foreach (var row in rows)
            {
                BodyCell(table, row.Key, TextAlign.Right);
                BodyCell(table, row.Label, TextAlign.Right);
                BodyCell(table, row.SubLabel ?? "—", TextAlign.Right);
                BodyCell(table, Number(row.Amount));
            }
        });
    }

    private static void ComposeTotal(IContainer container, decimal totalBase, string baseCurrency) =>
        container.Width(260).Background(Navy).Padding(10).Row(row =>
        {
            row.RelativeItem().Text($"الإجمالي ({baseCurrency})").FontColor(Colors.White).Bold();
            row.ConstantItem(100).AlignLeft().ContentFromLeftToRight()
                .Text(Number(totalBase)).FontColor(GoldSoft).Bold();
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

    private static void ComposeFooter(IContainer container, string title) =>
        container.AlignCenter().Text(text =>
        {
            text.Span($"{title} — صفحة ").FontSize(8).FontColor(Muted);
            text.CurrentPageNumber().FontSize(8).FontColor(Muted);
            text.Span(" / ").FontSize(8).FontColor(Muted);
            text.TotalPages().FontSize(8).FontColor(Muted);
        });

    private static string Number(decimal value) => value.ToString("N2", WesternNumbers);

    private enum TextAlign { Center, Right }
}
