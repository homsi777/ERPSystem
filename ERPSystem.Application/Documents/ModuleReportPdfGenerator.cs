using System.Globalization;
using ERPSystem.Application.DTOs.Reports;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using static ERPSystem.Application.Documents.FinanceDocumentTheme;

namespace ERPSystem.Application.Documents;

/// <summary>Generic module report PDF — KPIs + dynamic columns (API + WPF).</summary>
public sealed class ModuleReportPdfGenerator
{
    private static readonly CultureInfo WesternNumbers = CultureInfo.InvariantCulture;
    private readonly string _logoPath;

    public ModuleReportPdfGenerator(string fontPath, string logoPath)
    {
        ConfigureQuestPdf(fontPath);
        _logoPath = logoPath;
    }

    public static ModuleReportPdfGenerator FromContentRoot(string contentRoot)
    {
        var (fontPath, logoPath) = ResolveAssets(contentRoot);
        return new ModuleReportPdfGenerator(fontPath, logoPath);
    }

    public byte[] Generate(ModuleReportResultDto report)
    {
        ArgumentNullException.ThrowIfNull(report);

        var landscape = report.Columns.Count > 6;

        return Document.Create(document =>
        {
            document.Page(page =>
            {
                page.Size(landscape ? PageSizes.A4.Landscape() : PageSizes.A4);
                page.Margin(24);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(style => style
                    .FontFamily(FontFamily)
                    .FontSize(8f)
                    .FontColor(Navy));
                page.ContentFromRightToLeft();

                page.Header().Element(c => ComposeHeader(c, report));
                page.Content().PaddingTop(8).Column(column =>
                {
                    column.Spacing(8);
                    if (report.Kpis.Count > 0)
                        column.Item().Element(c => ComposeKpis(c, report.Kpis));
                    column.Item().Element(c => ComposeTable(c, report));
                });
                page.Footer().Element(c => ComposeFooter(c, report.Title));
            });
        }).GeneratePdf();
    }

    private void ComposeHeader(IContainer container, ModuleReportResultDto report)
    {
        container.Column(column =>
        {
            column.Item().AlignCenter().Height(56).Width(72).Image(_logoPath).FitArea();
            column.Item().PaddingTop(4).LineHorizontal(2).LineColor(Gold);
            column.Item().PaddingTop(6).Row(row =>
            {
                row.RelativeItem().Column(meta =>
                {
                    meta.Item().AlignRight().Text(report.Title).FontSize(16).Bold().FontColor(Navy);
                    if (!string.IsNullOrWhiteSpace(report.Description))
                        meta.Item().PaddingTop(2).Text(report.Description).FontSize(9).FontColor(Muted);
                    meta.Item().PaddingTop(3).Row(line =>
                    {
                        line.AutoItem().Text("الفترة:").SemiBold().FontSize(9);
                        line.AutoItem().PaddingRight(5).ContentFromLeftToRight()
                            .Text(FormatRange(report.FromDate, report.ToDate)).FontSize(9);
                    });
                    meta.Item().PaddingTop(2).Text($"{report.Rows.Count.ToString(WesternNumbers)} سجل").FontSize(8).FontColor(Muted);
                });
                row.RelativeItem().AlignLeft().Column(company =>
                {
                    company.Item().Text("شركة الأمل").FontSize(11).Bold().FontColor(Gold);
                    company.Item().ContentFromLeftToRight().Text("ALAMAL.AB").FontSize(8).FontColor(Muted);
                    company.Item().Text($"أُنشئ: {report.GeneratedAt.ToLocalTime().ToString("yyyy-MM-dd HH:mm", WesternNumbers)}").FontSize(8).FontColor(Muted);
                });
            });
        });
    }

    private static void ComposeKpis(IContainer container, IReadOnlyList<ModuleReportKpiDto> kpis) =>
        container.Row(row =>
        {
            foreach (var kpi in kpis)
            {
                row.RelativeItem().Padding(2).Background(Paper).Border(1).BorderColor(Border).Padding(8).Column(col =>
                {
                    col.Item().AlignCenter().Text(kpi.Label).FontSize(8).FontColor(Muted);
                    col.Item().PaddingTop(2).AlignCenter().Text(NormalizeDigits(kpi.Value)).FontSize(12).Bold().FontColor(Navy);
                });
            }
        });

    private static void ComposeTable(IContainer container, ModuleReportResultDto report)
    {
        if (report.Rows.Count == 0)
        {
            container.AlignCenter().Text("لا توجد بيانات ضمن الفترة المحددة.").FontSize(10).FontColor(Muted);
            return;
        }

        container.Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                foreach (var _ in report.Columns)
                    columns.RelativeColumn();
            });

            table.Header(header =>
            {
                foreach (var col in report.Columns)
                    HeaderCell(header, col.HeaderAr);
            });

            foreach (var row in report.Rows)
            {
                foreach (var col in report.Columns)
                {
                    row.TryGetValue(col.Key, out var value);
                    BodyCell(table, FormatCell(value, col.Format));
                }
            }
        });
    }

    private static string FormatCell(object? value, string? format)
    {
        if (value is null) return "—";
        return value switch
        {
            DateTime dt => dt.ToLocalTime().ToString(format?.Replace("yyyy/MM/dd", "yyyy-MM-dd") ?? "yyyy-MM-dd", WesternNumbers),
            DateTimeOffset dto => dto.ToLocalTime().ToString(format ?? "yyyy-MM-dd", WesternNumbers),
            decimal d => d.ToString(format ?? "N2", WesternNumbers),
            double d => d.ToString(format ?? "N2", WesternNumbers),
            float f => f.ToString(format ?? "N2", WesternNumbers),
            int i => i.ToString(WesternNumbers),
            long l => l.ToString(WesternNumbers),
            bool b => b ? "نعم" : "لا",
            _ => NormalizeDigits(value.ToString() ?? "—")
        };
    }

    private static void HeaderCell(TableCellDescriptor table, string text) =>
        table.Cell().Background(NavySoft).Border(0.5f).BorderColor(Gold)
            .PaddingVertical(5).PaddingHorizontal(3).AlignCenter().AlignMiddle()
            .Text(text).FontColor(Colors.White).FontSize(7.5f).SemiBold();

    private static void BodyCell(TableDescriptor table, string text) =>
        table.Cell().BorderBottom(0.6f).BorderColor(Border)
            .PaddingVertical(4).PaddingHorizontal(3).AlignCenter().AlignMiddle()
            .Text(text).FontSize(7.5f);

    private static void ComposeFooter(IContainer container, string title) =>
        container.AlignCenter().Text(text =>
        {
            text.Span($"{title} — صفحة ").FontSize(8).FontColor(Muted);
            text.CurrentPageNumber().FontSize(8).FontColor(Muted);
            text.Span(" / ").FontSize(8).FontColor(Muted);
            text.TotalPages().FontSize(8).FontColor(Muted);
        });

    private static string FormatRange(DateTime? from, DateTime? to)
    {
        if (from is null && to is null) return "كل الفترات";
        if (from is null) return $"حتى {to!.Value.ToString("yyyy-MM-dd", WesternNumbers)}";
        if (to is null) return $"من {from.Value.ToString("yyyy-MM-dd", WesternNumbers)}";
        return $"{from.Value.ToString("yyyy-MM-dd", WesternNumbers)} → {to.Value.ToString("yyyy-MM-dd", WesternNumbers)}";
    }

    private static string NormalizeDigits(string value) =>
        value
            .Replace('٠', '0').Replace('١', '1').Replace('٢', '2').Replace('٣', '3').Replace('٤', '4')
            .Replace('٥', '5').Replace('٦', '6').Replace('٧', '7').Replace('٨', '8').Replace('٩', '9')
            .Replace('۰', '0').Replace('۱', '1').Replace('۲', '2').Replace('۳', '3').Replace('۴', '4')
            .Replace('۵', '5').Replace('۶', '6').Replace('۷', '7').Replace('۸', '8').Replace('۹', '9');
}
