using ERPSystem.Application.DTOs.Purchases;
using ERPSystem.Domain.Enums;
using Microsoft.Win32;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using System.Diagnostics;
using System.IO;
using QColors = QuestPDF.Helpers.Colors;
using QPageSizes = QuestPDF.Helpers.PageSizes;

namespace ERPSystem.Services.Purchases;

/// <summary>
/// Purchase invoice PDF generator (template B) — mirrors the sales invoice
/// layout adapted to supplier/purchase context, QuestPDF community license.
/// </summary>
public static class PurchaseDocumentService
{
    private static bool _license;

    private static void EnsureLicense()
    {
        if (_license) return;
        QuestPDF.Settings.License = LicenseType.Community;
        _license = true;
    }

    public static void ShowInvoicePreview(PurchaseInvoiceDetailsDto invoice, bool exportPdf)
    {
        EnsureLicense();
        var doc = Build(invoice);
        if (exportPdf)
        {
            var dlg = new SaveFileDialog
            {
                Filter = "PDF Document (*.pdf)|*.pdf",
                FileName = $"PurchaseInvoice-{invoice.InvoiceNumber}.pdf"
            };
            if (dlg.ShowDialog() != true) return;
            doc.GeneratePdf(dlg.FileName);
            Open(dlg.FileName);
            return;
        }

        var temp = Path.Combine(Path.GetTempPath(), $"erp-pinv-{Guid.NewGuid():N}.pdf");
        doc.GeneratePdf(temp);
        Open(temp);
    }

    private static IDocument Build(PurchaseInvoiceDetailsDto inv) =>
        Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(QPageSizes.A4);
                page.Margin(30);
                page.DefaultTextStyle(t => t.FontFamily("Arial").FontSize(10));
                page.ContentFromRightToLeft();

                page.Header().BorderBottom(1).BorderColor(QColors.Grey.Lighten2).PaddingBottom(10).Row(row =>
                {
                    row.RelativeItem().Column(col =>
                    {
                        col.Item().Text("فاتورة مشتريات").FontSize(20).Bold();
                        col.Item().Text($"رقم الفاتورة: {inv.InvoiceNumber}").FontSize(11).SemiBold();
                        col.Item().Text($"تاريخ الإصدار: {inv.InvoiceDate:yyyy-MM-dd}").FontSize(10);
                        col.Item().Text($"تاريخ الاستحقاق: {inv.DueDate:yyyy-MM-dd}").FontSize(10);
                        col.Item().Text($"الحالة: {inv.StatusDisplay}").FontSize(10);
                    });
                    row.ConstantItem(110).Height(65).Background(QColors.Grey.Lighten3).AlignCenter().AlignMiddle().Text("شعار الشركة").FontSize(10);
                });

                page.Content().Column(col =>
                {
                    col.Spacing(12);
                    col.Item().Background(QColors.Grey.Lighten4).Padding(10).Column(c =>
                    {
                        c.Item().Text("بيانات المورد").FontSize(11).Bold();
                        c.Item().Text(inv.SupplierName).FontSize(12).SemiBold();
                        if (!string.IsNullOrWhiteSpace(inv.SupplierReference))
                            c.Item().Text($"مرجع المورد: {inv.SupplierReference}").FontSize(10);
                        if (!string.IsNullOrWhiteSpace(inv.WarehouseName))
                            c.Item().Text($"المستودع: {inv.WarehouseName}").FontSize(10);
                    });

                    col.Item().Table(t =>
                    {
                        t.ColumnsDefinition(cd =>
                        {
                            cd.ConstantColumn(28);
                            cd.RelativeColumn(5);
                            cd.ConstantColumn(60);
                            cd.RelativeColumn(2);
                            cd.RelativeColumn(2);
                        });

                        t.Header(h =>
                        {
                            h.Cell().Element(HeaderCell).Text("#");
                            h.Cell().Element(HeaderCell).Text("البيان");
                            h.Cell().Element(HeaderCell).AlignCenter().Text("الكمية");
                            h.Cell().Element(HeaderCell).AlignRight().Text("السعر");
                            h.Cell().Element(HeaderCell).AlignRight().Text("الإجمالي");
                        });

                        var i = 1;
                        foreach (var line in inv.Lines)
                        {
                            var desc = line.LineType == PurchaseLineType.Inventory
                                ? (line.FabricItemName ?? line.Description)
                                : line.Description;
                            var qty = line.QuantityMeters != 0 ? line.QuantityMeters : line.RollCount;
                            t.Cell().Element(BodyCell).Text(i.ToString());
                            t.Cell().Element(BodyCell).Text(desc);
                            t.Cell().Element(BodyCell).AlignCenter().Text($"{qty:N2}");
                            t.Cell().Element(BodyCell).AlignRight().Text($"{line.UnitPrice:N2}");
                            t.Cell().Element(BodyCell).AlignRight().Text($"{line.LineTotal:N2}");
                            i++;
                        }

                        static IContainer HeaderCell(IContainer x) =>
                            x.Background(QColors.Grey.Lighten2).Padding(6).DefaultTextStyle(s => s.Bold().FontSize(10));
                        static IContainer BodyCell(IContainer x) =>
                            x.Padding(6).BorderBottom(1).BorderColor(QColors.Grey.Lighten3);
                    });

                    col.Item().AlignLeft().Width(240).Padding(6).Column(box =>
                    {
                        Row(box, "الإجمالي الفرعي", inv.SubTotal);
                        if (inv.DiscountAmount != 0) Row(box, "الخصم", -inv.DiscountAmount);
                        if (inv.TaxAmount != 0) Row(box, "الضريبة", inv.TaxAmount);
                        box.Item().PaddingTop(4).BorderTop(1).BorderColor(QColors.Grey.Medium);
                        box.Item().PaddingTop(4).Row(r =>
                        {
                            r.RelativeItem().Text("الإجمالي").Bold().FontSize(12);
                            r.ConstantItem(90).AlignRight().Text($"{inv.TotalAmount:N2}").Bold().FontSize(12);
                        });
                        if (inv.PaidAmount != 0)
                        {
                            Row(box, "المدفوع", inv.PaidAmount);
                            Row(box, "المتبقي", inv.RemainingAmount);
                        }
                    });

                    if (!string.IsNullOrWhiteSpace(inv.Notes))
                        col.Item().PaddingTop(8).Text($"ملاحظات: {inv.Notes}").FontSize(9);
                });

                page.Footer().AlignCenter().Text(t =>
                {
                    t.Span($"فاتورة مشتريات {inv.InvoiceNumber} — صفحة ").FontSize(8);
                    t.CurrentPageNumber().FontSize(8);
                    t.Span(" / ").FontSize(8);
                    t.TotalPages().FontSize(8);
                });
            });
        });

    private static void Row(ColumnDescriptor col, string label, decimal value) =>
        col.Item().Row(r =>
        {
            r.RelativeItem().Text(label).FontSize(10);
            r.ConstantItem(90).AlignRight().Text($"{value:N2}").FontSize(10);
        });

    private static void Open(string path)
    {
        try { Process.Start(new ProcessStartInfo(path) { UseShellExecute = true }); }
        catch (Exception ex) { MockInteractionService.ShowWarning("تعذر فتح المستند:\n" + ex.Message, "معاينة"); }
    }
}
