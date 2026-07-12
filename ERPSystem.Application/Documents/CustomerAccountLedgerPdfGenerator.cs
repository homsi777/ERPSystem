using System.Globalization;
using ERPSystem.Application.DTOs.Customers;
using ERPSystem.Domain.Enums;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using static ERPSystem.Application.Documents.FinanceDocumentTheme;

namespace ERPSystem.Application.Documents;

/// <summary>Shared customer account statement PDF — detailed fabric ledger (API + WPF).</summary>
public sealed class CustomerAccountLedgerPdfGenerator
{
    private static readonly CultureInfo WesternNumbers = CultureInfo.InvariantCulture;
    private readonly string _logoPath;

    public CustomerAccountLedgerPdfGenerator(string fontPath, string logoPath)
    {
        ConfigureQuestPdf(fontPath);
        _logoPath = logoPath;
    }

    public static CustomerAccountLedgerPdfGenerator FromContentRoot(string contentRoot)
    {
        var (fontPath, logoPath) = ResolveAssets(contentRoot);
        return new CustomerAccountLedgerPdfGenerator(fontPath, logoPath);
    }

    public byte[] Generate(CustomerAccountLedgerDto ledger, DateTime? from, DateTime? to)
    {
        ArgumentNullException.ThrowIfNull(ledger);

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

                page.Header().Element(c => ComposeHeader(c, ledger, from, to));
                page.Content().PaddingTop(8).Column(column =>
                {
                    column.Spacing(8);
                    column.Item().Element(ComposeSummary);
                    column.Item().Element(c => ComposeLinesTable(c, ledger.Lines));
                    column.Item().AlignLeft().Element(c => ComposeClosingBox(c, ledger));
                });
                page.Footer().Element(c => ComposeFooter(c, ledger.CustomerName));
            });
        }).GeneratePdf();

        void ComposeSummary(IContainer container) =>
            container.Border(1).BorderColor(Border).Background(Paper).Padding(10).Row(row =>
            {
                SummaryItem(row, "رصيد افتتاحي", Amount(ledger.OpeningBalance));
                SummaryItem(row, "رصيد ختامي", Amount(ledger.ClosingBalance));
                SummaryItem(row, "رصيد المطابقة",
                    ledger.LastReconciliationBalance.HasValue
                        ? Amount(ledger.LastReconciliationBalance.Value)
                        : "—");
                SummaryItem(row, "تاريخ آخر مطابقة",
                    ledger.LastReconciliationDate.HasValue
                        ? ledger.LastReconciliationDate.Value.ToString("yyyy/MM/dd", WesternNumbers)
                        : "—");
            });
    }

    private void ComposeHeader(IContainer container, CustomerAccountLedgerDto ledger, DateTime? from, DateTime? to)
    {
        container.Column(column =>
        {
            column.Item().AlignCenter().Height(56).Width(72).Image(_logoPath).FitArea();
            column.Item().PaddingTop(4).LineHorizontal(2).LineColor(Gold);
            column.Item().PaddingTop(6).Row(row =>
            {
                row.RelativeItem().Column(meta =>
                {
                    meta.Item().AlignRight().Text("كشف حساب عميل").FontSize(16).Bold().FontColor(Navy);
                    meta.Item().PaddingTop(2).Text(ledger.CustomerName).FontSize(11).FontColor(Gold).SemiBold();
                    meta.Item().PaddingTop(3).Row(line =>
                    {
                        line.AutoItem().Text("الفترة:").SemiBold().FontSize(9);
                        line.AutoItem().PaddingRight(5).ContentFromLeftToRight()
                            .Text(FormatRange(from, to)).FontSize(9);
                    });
                });
            });
        });
    }

    private static void ComposeLinesTable(IContainer container, IReadOnlyList<CustomerAccountLedgerLineDto> lines)
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.RelativeColumn(1.1f);
                columns.RelativeColumn(1.8f);
                columns.RelativeColumn(0.7f);
                columns.RelativeColumn(0.8f);
                columns.RelativeColumn(0.7f);
                columns.RelativeColumn(0.9f);
                columns.RelativeColumn(1f);
                columns.RelativeColumn(0.8f);
                columns.RelativeColumn(1f);
                columns.RelativeColumn(0.9f);
            });

            HeaderCell(table, "نوع الحركة");
            HeaderCell(table, "نوع البضاعة");
            HeaderCell(table, "عدد الأثواب");
            HeaderCell(table, "مجموع الأطوال");
            HeaderCell(table, "السعر");
            HeaderCell(table, "مجموع المبلغ");
            HeaderCell(table, "رقم المستند");
            HeaderCell(table, "التاريخ");
            HeaderCell(table, "ملاحظة");
            HeaderCell(table, "الرصيد المتراكم");

            if (lines.Count == 0)
            {
                table.Cell().ColumnSpan(10).Padding(16).AlignCenter()
                    .Text("لا توجد حركات في الفترة المحددة").FontSize(9).FontColor(Muted);
                return;
            }

            foreach (var line in lines)
            {
                BodyCell(table, MovementLabel(line.MovementType));
                BodyCell(table, string.IsNullOrWhiteSpace(line.FabricDescription) ? "—" : line.FabricDescription, TextAlign.Right);
                BodyCell(table, FormatOptionalInt(line.RollCount));
                BodyCell(table, FormatOptionalDecimal(line.TotalMeters));
                BodyCell(table, FormatOptionalDecimal(line.UnitPrice));
                BodyCell(table, Amount(line.LineAmount));
                BodyCell(table, line.DocumentNumber);
                BodyCell(table, line.TransactionDate.ToString("yyyy/MM/dd", WesternNumbers));
                BodyCell(table, string.IsNullOrWhiteSpace(line.Notes) ? "—" : line.Notes, TextAlign.Right);
                BodyCell(table, Amount(line.RunningBalance));
            }
        });
    }

    private static void ComposeClosingBox(IContainer container, CustomerAccountLedgerDto ledger) =>
        container.Width(260).Border(1).BorderColor(Border).Column(column =>
        {
            TotalRow(column, "الرصيد الافتتاحي", ledger.OpeningBalance);
            TotalRow(column, "إجمالي الحركات", ledger.Lines.Sum(l => l.LineAmount));
            column.Item().Background(Navy).PaddingVertical(7).PaddingHorizontal(10).Row(row =>
            {
                row.RelativeItem().Text("الرصيد الختامي").FontColor(Colors.White).FontSize(9).Bold();
                row.ConstantItem(90).AlignLeft().ContentFromLeftToRight()
                    .Text(Amount(ledger.ClosingBalance)).FontColor(GoldSoft).FontSize(9).Bold();
            });
        });

    private static void SummaryItem(RowDescriptor row, string label, string value)
    {
        row.RelativeItem().PaddingHorizontal(4).Column(col =>
        {
            col.Item().Text(label).FontSize(8).FontColor(Muted);
            col.Item().PaddingTop(2).Text(value).FontSize(10).SemiBold().FontColor(Navy);
        });
    }

    private static void TotalRow(ColumnDescriptor column, string label, decimal value) =>
        column.Item().PaddingVertical(4).PaddingHorizontal(10).Row(row =>
        {
            row.RelativeItem().Text(label).FontSize(9);
            row.ConstantItem(90).AlignLeft().ContentFromLeftToRight().Text(Amount(value)).FontSize(9).SemiBold();
        });

    private static void HeaderCell(TableDescriptor table, string text) =>
        table.Cell().Background(NavySoft).Border(0.5f).BorderColor(Gold)
            .PaddingVertical(5).PaddingHorizontal(3).AlignCenter().AlignMiddle()
            .Text(text).FontColor(Colors.White).FontSize(7.5f).SemiBold();

    private static void BodyCell(TableDescriptor table, string text, TextAlign align = TextAlign.Center)
    {
        var cell = table.Cell().BorderBottom(0.6f).BorderColor(Border)
            .PaddingVertical(5).PaddingHorizontal(3).AlignMiddle();
        var aligned = align == TextAlign.Right ? cell.AlignRight() : cell.AlignCenter();
        aligned.Text(text).FontSize(7.2f);
    }

    private static void ComposeFooter(IContainer container, string customerName)
    {
        container.BorderTop(1).BorderColor(Gold).PaddingTop(5).Row(row =>
        {
            row.RelativeItem().Text("شكرًا لتعاملكم معنا").FontSize(7.5f).FontColor(Muted);
            row.RelativeItem().AlignCenter().Text(customerName).FontSize(7.5f).FontColor(Muted);
            row.RelativeItem().AlignLeft().DefaultTextStyle(style => style.FontSize(7.5f).FontColor(Muted))
                .Text(text =>
                {
                    text.Span("صفحة ");
                    text.CurrentPageNumber();
                    text.Span(" / ");
                    text.TotalPages();
                });
        });
    }

    private static string MovementLabel(CustomerAccountMovementType type) => type switch
    {
        CustomerAccountMovementType.SalesInvoice => "فاتورة بيع",
        CustomerAccountMovementType.SalesReturn => "مرتجع",
        CustomerAccountMovementType.ReceiptVoucher => "سند قبض",
        _ => type.ToString()
    };

    private static string FormatRange(DateTime? from, DateTime? to)
    {
        if (from is null && to is null) return "كل الفترات";
        if (from is null) return $"حتى {to:yyyy/MM/dd}";
        if (to is null) return $"من {from:yyyy/MM/dd}";
        return $"{from:yyyy/MM/dd} — {to:yyyy/MM/dd}";
    }

    private static string Amount(decimal value) => $"{value.ToString("N2", WesternNumbers)} $";

    private static string FormatOptionalDecimal(decimal? value) =>
        value.HasValue ? value.Value.ToString("N2", WesternNumbers) : "—";

    private static string FormatOptionalInt(int? value) =>
        value.HasValue ? value.Value.ToString(WesternNumbers) : "—";

    private enum TextAlign
    {
        Center,
        Right
    }
}
