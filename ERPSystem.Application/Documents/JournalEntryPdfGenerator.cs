using System.Globalization;
using ERPSystem.Application.DTOs.Accounting;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using static ERPSystem.Application.Documents.FinanceDocumentTheme;

namespace ERPSystem.Application.Documents;

/// <summary>Journal entry / accounting voucher PDF (API + WPF).</summary>
public sealed class JournalEntryPdfGenerator
{
    private static readonly CultureInfo WesternNumbers = CultureInfo.InvariantCulture;
    private readonly string _logoPath;

    public JournalEntryPdfGenerator(string fontPath, string logoPath)
    {
        ConfigureQuestPdf(fontPath);
        _logoPath = logoPath;
    }

    public static JournalEntryPdfGenerator FromContentRoot(string contentRoot)
    {
        var (fontPath, logoPath) = ResolveAssets(contentRoot);
        return new JournalEntryPdfGenerator(fontPath, logoPath);
    }

    public byte[] Generate(JournalEntryDetailsDto entry)
    {
        ArgumentNullException.ThrowIfNull(entry);

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

                page.Header().Element(c => ComposeHeader(c, entry));
                page.Content().PaddingTop(10).Column(column =>
                {
                    column.Spacing(10);
                    column.Item().Element(ComposeMeta);
                    column.Item().Element(c => ComposeLines(c, entry.Lines));
                    column.Item().AlignLeft().Element(ComposeTotals);
                });
                page.Footer().Element(c => ComposeFooter(c, entry.EntryNumber));
            });
        }).GeneratePdf();

        void ComposeMeta(IContainer container) =>
            container.Background(Paper).Border(1).BorderColor(Border).Padding(10).Row(row =>
            {
                InfoCol(row, "رقم القيد", entry.EntryNumber);
                InfoCol(row, "التاريخ", entry.EntryDate.ToString("yyyy-MM-dd", WesternNumbers));
                InfoCol(row, "الحالة", entry.StatusDisplay);
                InfoCol(row, "المصدر", entry.SourceTypeDisplay ?? "يدوي");
            });

        void ComposeTotals(IContainer container) =>
            container.Width(260).Border(1).BorderColor(Border).Column(col =>
            {
                TotalRow(col, "إجمالي المدين", entry.DebitTotal);
                TotalRow(col, "إجمالي الدائن", entry.CreditTotal);
            });
    }

    private void ComposeHeader(IContainer container, JournalEntryDetailsDto entry)
    {
        container.Column(column =>
        {
            column.Item().AlignCenter().Height(56).Width(72).Image(_logoPath).FitArea();
            column.Item().PaddingTop(4).LineHorizontal(2).LineColor(Gold);
            column.Item().PaddingTop(6).AlignCenter().Text("قيد يومية / سند محاسبي").FontSize(17).Bold().FontColor(Navy);
            column.Item().AlignCenter().PaddingTop(4).Text(entry.Description).FontSize(10).FontColor(Gold).SemiBold();
        });
    }

    private static void ComposeLines(IContainer container, IReadOnlyList<JournalEntryLineDetailsDto> lines)
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.RelativeColumn(1.2f);
                columns.RelativeColumn(2.5f);
                columns.RelativeColumn(2f);
                columns.RelativeColumn(1.2f);
                columns.RelativeColumn(1.2f);
            });

            table.Header(header =>
            {
                HeaderCell(header, "الحساب");
                HeaderCell(header, "اسم الحساب");
                HeaderCell(header, "البيان");
                HeaderCell(header, "مدين");
                HeaderCell(header, "دائن");
            });

            foreach (var line in lines)
            {
                BodyCell(table, line.AccountCode, TextAlign.Right);
                BodyCell(table, line.AccountName, TextAlign.Right);
                BodyCell(table, string.IsNullOrWhiteSpace(line.Narrative) ? "—" : line.Narrative, TextAlign.Right);
                BodyCell(table, line.Debit > 0 ? Number(line.Debit) : "—");
                BodyCell(table, line.Credit > 0 ? Number(line.Credit) : "—");
            }
        });
    }

    private static void InfoCol(RowDescriptor row, string label, string value) =>
        row.RelativeItem().Column(col =>
        {
            col.Item().Text(label).FontSize(8).FontColor(Muted);
            col.Item().PaddingTop(2).Text(value).FontSize(9).SemiBold();
        });

    private static void TotalRow(ColumnDescriptor column, string label, decimal value) =>
        column.Item().PaddingVertical(4).PaddingHorizontal(10).Row(row =>
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

    private static void ComposeFooter(IContainer container, string entryNumber) =>
        container.AlignCenter().Text(text =>
        {
            text.Span($"قيد {entryNumber} — صفحة ").FontSize(8).FontColor(Muted);
            text.CurrentPageNumber().FontSize(8).FontColor(Muted);
            text.Span(" / ").FontSize(8).FontColor(Muted);
            text.TotalPages().FontSize(8).FontColor(Muted);
        });

    private static string Number(decimal value) => value.ToString("N2", WesternNumbers);

    private enum TextAlign { Center, Right }
}
