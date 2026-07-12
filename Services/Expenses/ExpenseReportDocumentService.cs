using ERPSystem.Application.DTOs.Expenses;
using ERPSystem.Services.Documents;
using QuestPDF.Elements.Table;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using QColors = QuestPDF.Helpers.Colors;
using QPageSizes = QuestPDF.Helpers.PageSizes;

namespace ERPSystem.Services.Expenses;

/// <summary>
/// Desktop Expense Report PDF generator — same Navy/Gold identity as the sales invoice print
/// (<see cref="Services.Sales.SalesDocumentService"/>), consuming the same <see cref="ExpenseReportDto"/>
/// already used by <c>ExpenseReportsControl</c>.
/// </summary>
public static class ExpenseReportDocumentService
{
    private const string Navy = "#071A2B";
    private const string NavySoft = "#102C45";
    private const string Gold = "#C99A4A";
    private const string GoldSoft = "#F6E8C9";
    private const string Paper = "#FFFCF5";
    private const string Muted = "#65717D";
    private const string Border = "#D9C9A7";
    private const string Green = "#1E6B45";
    private const string Maroon = "#8C2A2A";

    private static bool _licenseInitialized;

    private static void EnsureLicense()
    {
        if (_licenseInitialized) return;
        QuestPDF.Settings.License = LicenseType.Community;
        _licenseInitialized = true;
    }

    public static void ShowReportPreview(ExpenseReportDto report, bool exportPdf)
    {
        EnsureLicense();
        var document = Build(report);
        if (exportPdf)
        {
            PdfPreviewWindow.SaveAndOpen(document, $"تقرير مصاريف - {DateTime.Now:yyyy-MM-dd}.pdf");
            return;
        }
        PdfPreviewWindow.Show(document, $"تقرير مصاريف — {report.Title}");
    }

    private static IDocument Build(ExpenseReportDto report) =>
        Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(QPageSizes.A4.Landscape());
                page.Margin(28);
                page.DefaultTextStyle(t => t.FontFamily("Arial").FontSize(9).FontColor(Navy));
                page.ContentFromRightToLeft();

                page.Header().Element(h => Header(h, report));
                page.Content().PaddingTop(10).Column(col =>
                {
                    col.Spacing(10);
                    col.Item().Element(e => Summary(e, report));
                    col.Item().Element(e => Rows(e, report.Rows));
                    col.Item().AlignLeft().Element(e => TotalsFooter(e, report));
                });
                page.Footer().AlignCenter().Text(t =>
                {
                    t.Span("صفحة ").FontSize(8);
                    t.CurrentPageNumber().FontSize(8);
                    t.Span(" / ").FontSize(8);
                    t.TotalPages().FontSize(8);
                });
            });
        });

    private static void Header(IContainer c, ExpenseReportDto report) =>
        c.PaddingBottom(8).BorderBottom(1).BorderColor(QColors.Grey.Lighten2).Column(col =>
        {
            col.Item().AlignCenter().Height(60).Width(110).Background(QColors.Grey.Lighten3)
                .AlignMiddle().AlignCenter().Text("شعار الشركة").FontSize(10);
            col.Item().PaddingTop(4).LineHorizontal(2).LineColor(Gold);
            col.Item().PaddingTop(8).Row(row =>
            {
                row.RelativeItem().Column(meta =>
                {
                    meta.Item().Text("تقرير مصاريف").FontSize(18).Bold().FontColor(Navy);
                    meta.Item().PaddingTop(2).Text(string.IsNullOrWhiteSpace(report.Title) ? "تقرير مفصل" : report.Title)
                        .FontSize(11).FontColor(Gold).SemiBold();
                    meta.Item().PaddingTop(3).Text($"الفترة: {FormatRange(report.FromDate, report.ToDate)}").FontSize(10);
                    if (!string.IsNullOrWhiteSpace(report.ScopeLabel))
                        meta.Item().PaddingTop(2).Text($"النطاق: {report.ScopeLabel}").FontSize(10);
                });
                row.ConstantItem(160).AlignLeft().Text($"أُنشئ: {report.GeneratedAt.ToLocalTime():yyyy-MM-dd HH:mm}")
                    .FontSize(9).FontColor(QColors.Grey.Darken1);
            });
        });

    private static void Summary(IContainer c, ExpenseReportDto report) =>
        c.Row(row =>
        {
            Card(row, "عدد المصاريف", report.ExpenseCount.ToString(), Navy);
            Card(row, $"الإجمالي ({report.BaseCurrency})", $"{report.TotalBase:N2}", Navy);
            Card(row, $"مدفوع ({report.BaseCurrency})", $"{report.TotalPaidBase:N2}", Green);
            Card(row, $"متبقي ({report.BaseCurrency})", $"{report.TotalRemainingBase:N2}", Maroon);
        });

    private static void Card(RowDescriptor row, string label, string value, string accent) =>
        row.RelativeItem().Padding(3).Background(Paper).Border(1).BorderColor(Border).Padding(8).Column(col =>
        {
            col.Item().AlignCenter().Text(label).FontSize(9).FontColor(Muted);
            col.Item().PaddingTop(2).AlignCenter().Text(value).FontSize(15).Bold().FontColor(accent);
        });

    private static void Rows(IContainer c, IReadOnlyList<ExpenseReportRowDto> rows) =>
        c.Table(t =>
        {
            t.ColumnsDefinition(cd =>
            {
                cd.RelativeColumn(2.3f);
                cd.RelativeColumn(1.4f);
                cd.RelativeColumn(1.1f);
                cd.RelativeColumn(1.2f);
                cd.RelativeColumn(1.5f);
                cd.RelativeColumn(1.3f);
                cd.RelativeColumn(1.3f);
                cd.RelativeColumn(1.3f);
                cd.RelativeColumn(1.6f);
                cd.RelativeColumn(1.3f);
            });

            t.Header(h =>
            {
                h.Cell().Element(HeaderCell).Text("المصروف");
                h.Cell().Element(HeaderCell).Text("الفئة");
                h.Cell().Element(HeaderCell).Text("الحالة");
                h.Cell().Element(HeaderCell).Text("التاريخ");
                h.Cell().Element(HeaderCell).AlignRight().Text("المبلغ الأصلي");
                h.Cell().Element(HeaderCell).AlignRight().Text("بالأساس");
                h.Cell().Element(HeaderCell).AlignRight().Text("مدفوع");
                h.Cell().Element(HeaderCell).AlignRight().Text("متبقي");
                h.Cell().Element(HeaderCell).Text("المستفيد");
                h.Cell().Element(HeaderCell).Text("طريقة الدفع");
            });

            foreach (var row in rows)
            {
                t.Cell().Element(BodyCell).Text($"{row.Name} ({row.Code})");
                t.Cell().Element(BodyCell).Text(row.CategoryKindDisplay is { Length: > 0 } k ? $"{row.Category} — {k}" : row.Category);
                t.Cell().Element(BodyCell).Text(row.Status);
                t.Cell().Element(BodyCell).Text(row.EndDate is { } end && end.Date != row.StartDate.Date
                    ? $"{row.StartDate:yyyy-MM-dd} → {end:yyyy-MM-dd}"
                    : row.StartDate.ToString("yyyy-MM-dd"));
                t.Cell().Element(BodyCell).AlignRight().Text($"{row.OriginalAmount:N2} {row.Currency}");
                t.Cell().Element(BodyCell).AlignRight().Text($"{row.BaseAmount:N2}");
                t.Cell().Element(BodyCell).AlignRight().Text($"{row.PaidAmountBase:N2}");
                t.Cell().Element(BodyCell).AlignRight().Text($"{row.RemainingBalanceBase:N2}");
                t.Cell().Element(BodyCell).Text(string.IsNullOrWhiteSpace(row.PayeeName) ? "—" : row.PayeeName);
                t.Cell().Element(BodyCell).Text(row.PaymentMethod);
            }

            static IContainer HeaderCell(IContainer x) =>
                x.Background(NavySoft).Padding(5).Border(0.5f).BorderColor(Gold)
                    .DefaultTextStyle(s => s.Bold().FontSize(8).FontColor(QColors.White));
            static IContainer BodyCell(IContainer x) =>
                x.Padding(5).BorderBottom(1).BorderColor(Border).DefaultTextStyle(s => s.FontSize(7.5f));
        });

    private static void TotalsFooter(IContainer c, ExpenseReportDto report) =>
        c.Width(300).Border(1).BorderColor(Border).Column(col =>
        {
            TotalRow(col, $"إجمالي المبلغ ({report.BaseCurrency})", report.TotalBase);
            TotalRow(col, $"إجمالي المدفوع ({report.BaseCurrency})", report.TotalPaidBase);
            col.Item().Background(Navy).PaddingVertical(8).PaddingHorizontal(10).Row(r =>
            {
                r.RelativeItem().Text($"المتبقي ({report.BaseCurrency})").FontColor(QColors.White).FontSize(10).Bold();
                r.ConstantItem(95).AlignRight().Text($"{report.TotalRemainingBase:N2}").FontColor(GoldSoft).FontSize(10).Bold();
            });
        });

    private static void TotalRow(ColumnDescriptor col, string label, decimal value) =>
        col.Item().PaddingVertical(4).PaddingHorizontal(10).Row(r =>
        {
            r.RelativeItem().Text(label).FontSize(9);
            r.ConstantItem(95).AlignRight().Text($"{value:N2}").FontSize(9).SemiBold();
        });

    private static string FormatRange(DateTime? from, DateTime? to)
    {
        if (from is null && to is null) return "كل الفترات";
        return $"{from?.ToString("yyyy-MM-dd") ?? "—"} → {to?.ToString("yyyy-MM-dd") ?? "—"}";
    }
}
