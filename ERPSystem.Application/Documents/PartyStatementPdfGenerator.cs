using System.Globalization;
using ERPSystem.Application.DTOs.Suppliers;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using static ERPSystem.Application.Documents.FinanceDocumentTheme;

namespace ERPSystem.Application.Documents;

/// <summary>Party account statement PDF — supplier and generic debit/credit ledgers (API + WPF).</summary>
public sealed class PartyStatementPdfGenerator
{
    private static readonly CultureInfo WesternNumbers = CultureInfo.InvariantCulture;
    private readonly string _logoPath;

    public PartyStatementPdfGenerator(string fontPath, string logoPath)
    {
        ConfigureQuestPdf(fontPath);
        _logoPath = logoPath;
    }

    public static PartyStatementPdfGenerator FromContentRoot(string contentRoot)
    {
        var (fontPath, logoPath) = ResolveAssets(contentRoot);
        return new PartyStatementPdfGenerator(fontPath, logoPath);
    }

    public byte[] Generate(SupplierStatementDto statement, DateTime? from, DateTime? to) =>
        Generate(
            statement.SupplierName,
            "كشف حساب مورد",
            statement.OpeningBalance,
            statement.ClosingBalance,
            statement.Lines.Select(l => new PartyStatementLine(
                l.EntryDate,
                l.DocumentNumber,
                l.Description,
                l.Debit,
                l.Credit,
                l.RunningBalance)).ToList(),
            from,
            to);

    public byte[] Generate(
        string partyName,
        string title,
        decimal openingBalance,
        decimal closingBalance,
        IReadOnlyList<PartyStatementLine> lines,
        DateTime? from,
        DateTime? to)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(partyName);

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

                page.Header().Element(c => ComposeHeader(c, partyName, title, from, to, openingBalance));
                page.Content().PaddingTop(10).Column(column =>
                {
                    column.Spacing(10);
                    column.Item().Element(c => ComposeTable(c, lines));
                    column.Item().AlignLeft().Element(c => ComposeClosing(c, lines, closingBalance));
                });
                page.Footer().Element(c => ComposeFooter(c, partyName));
            });
        }).GeneratePdf();
    }

    private void ComposeHeader(
        IContainer container,
        string partyName,
        string title,
        DateTime? from,
        DateTime? to,
        decimal openingBalance)
    {
        container.Column(column =>
        {
            column.Item().AlignCenter().Height(56).Width(72).Image(_logoPath).FitArea();
            column.Item().PaddingTop(4).LineHorizontal(2).LineColor(Gold);
            column.Item().PaddingTop(6).AlignRight().Text(title).FontSize(16).Bold().FontColor(Navy);
            column.Item().PaddingTop(2).AlignRight().Text(partyName).FontSize(11).FontColor(Gold).SemiBold();
            column.Item().PaddingTop(4).Row(row =>
            {
                row.AutoItem().Text("الفترة:").SemiBold().FontSize(9);
                row.AutoItem().PaddingRight(5).ContentFromLeftToRight()
                    .Text(FormatRange(from, to)).FontSize(9);
            });
            column.Item().PaddingTop(2).Text($"الرصيد الافتتاحي: {Number(openingBalance)}").FontSize(9).SemiBold();
        });
    }

    private static void ComposeTable(IContainer container, IReadOnlyList<PartyStatementLine> lines)
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.RelativeColumn(1.1f);
                columns.RelativeColumn(1.2f);
                columns.RelativeColumn(2.5f);
                columns.RelativeColumn(1.1f);
                columns.RelativeColumn(1.1f);
                columns.RelativeColumn(1.1f);
            });

            table.Header(header =>
            {
                HeaderCell(header, "التاريخ");
                HeaderCell(header, "المرجع");
                HeaderCell(header, "البيان");
                HeaderCell(header, "مدين");
                HeaderCell(header, "دائن");
                HeaderCell(header, "الرصيد");
            });

            foreach (var line in lines)
            {
                BodyCell(table, line.Date.ToString("yyyy-MM-dd", WesternNumbers));
                BodyCell(table, line.Reference, TextAlign.Right);
                BodyCell(table, line.Description, TextAlign.Right);
                BodyCell(table, line.Debit != 0 ? Number(line.Debit) : "—");
                BodyCell(table, line.Credit != 0 ? Number(line.Credit) : "—");
                BodyCell(table, Number(line.Balance));
            }
        });
    }

    private static void ComposeClosing(IContainer container, IReadOnlyList<PartyStatementLine> lines, decimal closingBalance) =>
        container.Width(260).Border(1).BorderColor(Border).Column(col =>
        {
            TotalRow(col, "إجمالي المدين", lines.Sum(x => x.Debit));
            TotalRow(col, "إجمالي الدائن", lines.Sum(x => x.Credit));
            col.Item().Background(Navy).PaddingVertical(6).PaddingHorizontal(10).Row(row =>
            {
                row.RelativeItem().Text("الرصيد الختامي").FontColor(Colors.White).Bold();
                row.ConstantItem(90).AlignLeft().ContentFromLeftToRight()
                    .Text(Number(closingBalance)).FontColor(GoldSoft).Bold();
            });
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

    private static void ComposeFooter(IContainer container, string partyName) =>
        container.AlignCenter().Text(text =>
        {
            text.Span($"كشف حساب — {partyName} — صفحة ").FontSize(8).FontColor(Muted);
            text.CurrentPageNumber().FontSize(8).FontColor(Muted);
            text.Span(" / ").FontSize(8).FontColor(Muted);
            text.TotalPages().FontSize(8).FontColor(Muted);
        });

    private static string Number(decimal value) => value.ToString("N2", WesternNumbers);

    private static string FormatRange(DateTime? from, DateTime? to)
    {
        if (from is null && to is null) return "كل الفترات";
        if (from is null) return $"حتى {to:yyyy-MM-dd}";
        if (to is null) return $"من {from:yyyy-MM-dd}";
        return $"{from:yyyy-MM-dd} → {to:yyyy-MM-dd}";
    }

    private enum TextAlign { Center, Right }
}

public sealed record PartyStatementLine(
    DateTime Date,
    string Reference,
    string Description,
    decimal Debit,
    decimal Credit,
    decimal Balance);
