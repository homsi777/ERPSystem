using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using Microsoft.Win32;
using System.Diagnostics;
using System.IO;
using QColors = QuestPDF.Helpers.Colors;
using QPageSizes = QuestPDF.Helpers.PageSizes;
using WpfWindow = System.Windows.Window;

namespace ERPSystem.Services.Documents;

public sealed record StatementLine(
    DateTime Date,
    string Reference,
    string Description,
    decimal Debit,
    decimal Credit,
    decimal Balance);

/// <summary>
/// DEPRECATED — Legacy account statement PDF (Arial, pre-theme). No live callers remain;
/// use <see cref="CustomerStatementDocumentService"/> or <see cref="SupplierStatementDocumentService"/>.
/// Scheduled for removal in Phase B after final caller audit.
/// </summary>
[Obsolete("Use CustomerStatementDocumentService or SupplierStatementDocumentService instead.")]
public static class StatementDocumentService
{
    private static bool _license;

    private static void EnsureLicense()
    {
        if (_license) return;
        QuestPDF.Settings.License = LicenseType.Community;
        _license = true;
    }

    public static void ShowStatementPreview(
        string entityName,
        string entityKind,
        DateTime? from,
        DateTime? to,
        decimal openingBalance,
        decimal closingBalance,
        IReadOnlyList<StatementLine> lines,
        bool exportPdf = false)
    {
        EnsureLicense();
        var doc = Build(entityName, entityKind, from, to, openingBalance, closingBalance, lines);
        var suggested = $"Statement-{entityKind}-{entityName}-{DateTime.Now:yyyyMMdd}.pdf";

        if (exportPdf)
        {
            var dlg = new SaveFileDialog { Filter = "PDF Document (*.pdf)|*.pdf", FileName = Sanitize(suggested) };
            if (dlg.ShowDialog() != true) return;
            doc.GeneratePdf(dlg.FileName);
            OpenFile(dlg.FileName);
            return;
        }

        var temp = Path.Combine(Path.GetTempPath(), $"erp-stmt-{Guid.NewGuid():N}.pdf");
        doc.GeneratePdf(temp);
        OpenFile(temp);
    }

    private static IDocument Build(
        string entityName,
        string entityKind,
        DateTime? from,
        DateTime? to,
        decimal opening,
        decimal closing,
        IReadOnlyList<StatementLine> lines) =>
        Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(QPageSizes.A4);
                page.Margin(30);
                page.DefaultTextStyle(t => t.FontFamily("Arial").FontSize(10));
                page.ContentFromRightToLeft();

                page.Header().BorderBottom(1).BorderColor(QColors.Grey.Lighten2).PaddingBottom(8).Column(col =>
                {
                    col.Item().Text($"كشف حساب — {entityKind}").FontSize(18).Bold();
                    col.Item().Text(entityName).FontSize(13).SemiBold();
                    var range = $"من {from:yyyy-MM-dd} إلى {to:yyyy-MM-dd}";
                    if (from is null && to is null) range = "كل الفترات";
                    col.Item().Text(range).FontSize(10);
                    col.Item().Text($"الرصيد الافتتاحي: {opening:N2}").FontSize(10).SemiBold();
                });

                page.Content().PaddingVertical(10).Column(col =>
                {
                    col.Item().Table(t =>
                    {
                        t.ColumnsDefinition(cd =>
                        {
                            cd.ConstantColumn(70);
                            cd.RelativeColumn(2);
                            cd.RelativeColumn(3);
                            cd.RelativeColumn(2);
                            cd.RelativeColumn(2);
                            cd.RelativeColumn(2);
                        });

                        t.Header(h =>
                        {
                            h.Cell().Element(HeaderCell).Text("التاريخ");
                            h.Cell().Element(HeaderCell).Text("المرجع");
                            h.Cell().Element(HeaderCell).Text("البيان");
                            h.Cell().Element(HeaderCell).AlignRight().Text("مدين");
                            h.Cell().Element(HeaderCell).AlignRight().Text("دائن");
                            h.Cell().Element(HeaderCell).AlignRight().Text("الرصيد");
                        });

                        foreach (var l in lines)
                        {
                            t.Cell().Element(BodyCell).Text(l.Date.ToString("yyyy-MM-dd"));
                            t.Cell().Element(BodyCell).Text(l.Reference);
                            t.Cell().Element(BodyCell).Text(l.Description);
                            t.Cell().Element(BodyCell).AlignRight().Text(l.Debit != 0 ? $"{l.Debit:N2}" : "—");
                            t.Cell().Element(BodyCell).AlignRight().Text(l.Credit != 0 ? $"{l.Credit:N2}" : "—");
                            t.Cell().Element(BodyCell).AlignRight().Text($"{l.Balance:N2}");
                        }

                        static IContainer HeaderCell(IContainer x) =>
                            x.Background(QColors.Grey.Lighten2).Padding(6).DefaultTextStyle(s => s.Bold().FontSize(9));
                        static IContainer BodyCell(IContainer x) =>
                            x.Padding(5).BorderBottom(1).BorderColor(QColors.Grey.Lighten3).DefaultTextStyle(s => s.FontSize(9));
                    });

                    col.Item().PaddingTop(12).AlignLeft().Width(260).Column(box =>
                    {
                        SummaryRow(box, "إجمالي المدين", lines.Sum(x => x.Debit));
                        SummaryRow(box, "إجمالي الدائن", lines.Sum(x => x.Credit));
                        box.Item().PaddingTop(4).BorderTop(1).BorderColor(QColors.Grey.Medium);
                        box.Item().PaddingTop(4).Row(r =>
                        {
                            r.RelativeItem().Text("الرصيد الختامي").Bold().FontSize(12);
                            r.ConstantItem(100).AlignRight().Text($"{closing:N2}").Bold().FontSize(12);
                        });
                    });
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

    private static void SummaryRow(ColumnDescriptor col, string label, decimal value) =>
        col.Item().Row(r =>
        {
            r.RelativeItem().Text(label).FontSize(10);
            r.ConstantItem(100).AlignRight().Text($"{value:N2}").FontSize(10);
        });

    private static void OpenFile(string path)
    {
        try { Process.Start(new ProcessStartInfo(path) { UseShellExecute = true }); }
        catch (Exception ex) { MockInteractionService.ShowWarning("تعذر فتح المستند:\n" + ex.Message, "معاينة"); }
    }

    private static string Sanitize(string name) =>
        string.Concat(name.Split(Path.GetInvalidFileNameChars()));
}
