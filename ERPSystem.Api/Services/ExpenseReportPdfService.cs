using System.Globalization;
using ERPSystem.Application.DTOs.Expenses;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using static ERPSystem.Api.Services.FinanceDocumentTheme;

namespace ERPSystem.Api.Services;

/// <summary>
/// Server-side Expense Report renderer — same Navy/Gold identity as the sales invoice PDF.
/// Consumes the existing <see cref="ExpenseReportDto"/> as-is; no recalculation.
/// </summary>
public sealed class ExpenseReportPdfService
{
    private static readonly CultureInfo WesternNumbers = CultureInfo.InvariantCulture;

    private readonly string _logoPath;

    public ExpenseReportPdfService(IWebHostEnvironment environment)
    {
        _logoPath = ResolveLogoPath(environment.ContentRootPath);
        ConfigureQuestPdf(ResolveFontPath(environment.ContentRootPath));
    }

    public byte[] Generate(ExpenseReportDto report)
    {
        ArgumentNullException.ThrowIfNull(report);

        return Document.Create(document =>
        {
            document.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(28);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(style => style
                    .FontFamily(FontFamily)
                    .FontSize(8.5f)
                    .FontColor(Navy));
                page.ContentFromRightToLeft();

                page.Header().Element(container => ComposeHeader(container, report));
                page.Content().PaddingTop(10).Column(column =>
                {
                    column.Spacing(10);
                    column.Item().Element(container => ComposeSummary(container, report));
                    column.Item().Element(container => ComposeRows(container, report.Rows));
                    column.Item().AlignLeft().Element(container => ComposeTotalsFooter(container, report));
                });
                page.Footer().Element(container => ComposeFooter(container, report.Title));
            });
        }).GeneratePdf();
    }

    private void ComposeHeader(IContainer container, ExpenseReportDto report)
    {
        container.Column(column =>
        {
            column.Item().AlignCenter().Height(64).Width(80).Image(_logoPath).FitArea();
            column.Item().PaddingTop(4).LineHorizontal(2).LineColor(Gold);
            column.Item().PaddingTop(8).Row(row =>
            {
                row.RelativeItem(3).Column(meta =>
                {
                    meta.Item().AlignRight().Text("تقرير مصاريف").FontSize(17).Bold().FontColor(Navy);
                    meta.Item().PaddingTop(2).Text(string.IsNullOrWhiteSpace(report.Title) ? "تقرير مفصل" : report.Title)
                        .FontSize(10).FontColor(Gold).SemiBold();
                    meta.Item().PaddingTop(3).Row(line =>
                    {
                        line.AutoItem().Text("الفترة:").SemiBold().FontSize(9);
                        line.AutoItem().PaddingRight(5).ContentFromLeftToRight()
                            .Text(FormatRange(report.FromDate, report.ToDate)).FontSize(9);
                    });
                    if (!string.IsNullOrWhiteSpace(report.ScopeLabel))
                    {
                        meta.Item().PaddingTop(2).Row(line =>
                        {
                            line.AutoItem().Text("النطاق:").SemiBold().FontSize(9);
                            line.AutoItem().PaddingRight(5).Text(report.ScopeLabel!).FontSize(9);
                        });
                    }
                });

                row.RelativeItem(2).BorderRight(1).BorderColor(Border).PaddingRight(14).Column(company =>
                {
                    company.Item().AlignLeft().Text("شركة الأمل").FontSize(12).Bold().FontColor(Gold);
                    company.Item().AlignLeft().ContentFromLeftToRight().Text("ALAMAL.AB").FontSize(8).FontColor(Muted);
                    company.Item().AlignLeft().Text($"أُنشئ: {report.GeneratedAt.ToLocalTime():yyyy-MM-dd HH:mm}").FontSize(8).FontColor(Muted);
                });
            });
        });
    }

    private static void ComposeSummary(IContainer container, ExpenseReportDto report)
    {
        container.Row(row =>
        {
            SummaryCard(row, "عدد المصاريف", report.ExpenseCount.ToString(WesternNumbers), Navy);
            SummaryCard(row, $"الإجمالي ({report.BaseCurrency})", Number(report.TotalBase), Navy);
            SummaryCard(row, $"مدفوع ({report.BaseCurrency})", Number(report.TotalPaidBase), Green);
            SummaryCard(row, $"متبقي ({report.BaseCurrency})", Number(report.TotalRemainingBase), Maroon);
        });
    }

    private static void SummaryCard(RowDescriptor row, string label, string value, string accent) =>
        row.RelativeItem().Padding(3).Background(Paper).Border(1).BorderColor(Border).Padding(8).Column(col =>
        {
            col.Item().AlignCenter().Text(label).FontSize(8).FontColor(Muted);
            col.Item().PaddingTop(2).AlignCenter().ContentFromLeftToRight().Text(value).FontSize(14).Bold().FontColor(accent);
        });

    private static void ComposeRows(IContainer container, IReadOnlyList<ExpenseReportRowDto> rows)
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.RelativeColumn(2.3f); // name/code
                columns.RelativeColumn(1.4f); // category
                columns.RelativeColumn(1.1f); // status
                columns.RelativeColumn(1.2f); // date
                columns.RelativeColumn(1.5f); // original amount + currency
                columns.RelativeColumn(1.3f); // base amount
                columns.RelativeColumn(1.3f); // paid
                columns.RelativeColumn(1.3f); // remaining
                columns.RelativeColumn(1.6f); // payee
                columns.RelativeColumn(1.3f); // payment method
            });

            table.Header(header =>
            {
                HeaderCell(header, "المصروف");
                HeaderCell(header, "الفئة");
                HeaderCell(header, "الحالة");
                HeaderCell(header, "التاريخ");
                HeaderCell(header, "المبلغ الأصلي");
                HeaderCell(header, "بالأساس");
                HeaderCell(header, "مدفوع");
                HeaderCell(header, "متبقي");
                HeaderCell(header, "المستفيد");
                HeaderCell(header, "طريقة الدفع");
            });

            foreach (var row in rows)
            {
                BodyCell(table, $"{row.Name}\n{row.Code}", TextAlign.Right);
                BodyCell(table, row.CategoryKindDisplay is { Length: > 0 } k ? $"{row.Category}\n{k}" : row.Category, TextAlign.Right);
                BodyCell(table, row.Status);
                BodyCell(table, row.EndDate is { } end && end.Date != row.StartDate.Date
                    ? $"{row.StartDate:yyyy-MM-dd}\n{end:yyyy-MM-dd}"
                    : row.StartDate.ToString("yyyy-MM-dd", WesternNumbers));
                BodyCell(table, $"{Number(row.OriginalAmount)} {row.Currency}");
                BodyCell(table, Number(row.BaseAmount));
                BodyCell(table, Number(row.PaidAmountBase));
                BodyCell(table, Number(row.RemainingBalanceBase));
                BodyCell(table, string.IsNullOrWhiteSpace(row.PayeeName) ? "—" : row.PayeeName, TextAlign.Right);
                BodyCell(table, row.PaymentMethod);
            }
        });
    }

    private static void ComposeTotalsFooter(IContainer container, ExpenseReportDto report) =>
        container.Width(300).Border(1).BorderColor(Border).Column(column =>
        {
            TotalRow(column, $"إجمالي المبلغ ({report.BaseCurrency})", report.TotalBase);
            TotalRow(column, $"إجمالي المدفوع ({report.BaseCurrency})", report.TotalPaidBase);
            column.Item().Background(Navy).PaddingVertical(8).PaddingHorizontal(10).Row(row =>
            {
                row.RelativeItem().Text($"المتبقي ({report.BaseCurrency})").FontColor(Colors.White).FontSize(10).Bold();
                row.ConstantItem(95).AlignLeft().ContentFromLeftToRight()
                    .Text(Number(report.TotalRemainingBase)).FontColor(GoldSoft).FontSize(10).Bold();
            });
        });

    private static void TotalRow(ColumnDescriptor column, string label, decimal value) =>
        column.Item().PaddingVertical(4).PaddingHorizontal(10).Row(row =>
        {
            row.RelativeItem().Text(label).FontSize(9);
            row.ConstantItem(95).AlignLeft().ContentFromLeftToRight().Text(Number(value)).FontSize(9).SemiBold();
        });

    private static void HeaderCell(TableCellDescriptor table, string text) =>
        table.Cell().Background(NavySoft).Border(0.5f).BorderColor(Gold)
            .PaddingVertical(6).PaddingHorizontal(4).AlignCenter().AlignMiddle()
            .Text(text).FontColor(Colors.White).FontSize(8).SemiBold();

    private static void BodyCell(TableDescriptor table, string text, TextAlign align = TextAlign.Center)
    {
        var cell = table.Cell().BorderBottom(0.7f).BorderColor(Border)
            .PaddingVertical(6).PaddingHorizontal(4).AlignMiddle();
        var aligned = align == TextAlign.Right ? cell.AlignRight() : cell.AlignCenter();
        aligned.Text(text).FontSize(7.5f);
    }

    private static void ComposeFooter(IContainer container, string title)
    {
        container.BorderTop(1).BorderColor(Gold).PaddingTop(6).Row(row =>
        {
            row.RelativeItem().Text("شكرًا لتعاملكم معنا").FontSize(8).FontColor(Muted);
            row.RelativeItem().AlignCenter().Text(title).FontSize(8).FontColor(Muted);
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

    private static string FormatRange(DateTime? from, DateTime? to)
    {
        if (from is null && to is null) return "كل الفترات";
        var fromText = from?.ToString("yyyy-MM-dd", WesternNumbers) ?? "—";
        var toText = to?.ToString("yyyy-MM-dd", WesternNumbers) ?? "—";
        return $"{fromText} → {toText}";
    }

    private static string Number(decimal value) => value.ToString("N2", WesternNumbers);

    private enum TextAlign
    {
        Center,
        Right
    }
}
