using ERPSystem.Application.DTOs.Sales;
using ERPSystem.Domain.Enums;
using ERPSystem.Services;
using Microsoft.Win32;
using QuestPDF;
using QuestPDF.Elements.Table;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using System.Diagnostics;
using System.IO;
using WpfWindow = System.Windows.Window;
using WpfGrid = System.Windows.Controls.Grid;
using WpfRowDefinition = System.Windows.Controls.RowDefinition;
using WpfGridLength = System.Windows.GridLength;
using WpfGridUnitType = System.Windows.GridUnitType;
using WpfStackPanel = System.Windows.Controls.StackPanel;
using WpfTextBlock = System.Windows.Controls.TextBlock;
using WpfButton = System.Windows.Controls.Button;
using WpfOrientation = System.Windows.Controls.Orientation;
using WpfThickness = System.Windows.Thickness;
using WpfHAlign = System.Windows.HorizontalAlignment;
using WpfVAlign = System.Windows.VerticalAlignment;
using WpfTextAlign = System.Windows.TextAlignment;
using WpfFlowDirection = System.Windows.FlowDirection;
using WpfMessageBox = System.Windows.MessageBox;
using WpfMessageBoxButton = System.Windows.MessageBoxButton;
using WpfMessageBoxImage = System.Windows.MessageBoxImage;
using WpfFontWeights = System.Windows.FontWeights;
using WpfBrushes = System.Windows.Media.Brushes;
using WpfStartLocation = System.Windows.WindowStartupLocation;
using QColors = QuestPDF.Helpers.Colors;
using QPageSizes = QuestPDF.Helpers.PageSizes;

namespace ERPSystem.Services.Sales;

/// <summary>
/// Real invoice/delivery note PDF generator using QuestPDF (community license).
/// Replaces the mock DocumentPreviewWindow shell — renders a live A4 document
/// preview, printable and exportable to PDF.
/// </summary>
public static class SalesDocumentService
{
    private static bool _licenseInitialized;

    private static void EnsureLicense()
    {
        if (_licenseInitialized) return;
        QuestPDF.Settings.License = LicenseType.Community;
        _licenseInitialized = true;
    }

    public static void ShowInvoicePreview(
        SalesInvoiceDto invoice,
        string customerName,
        bool exportPdf,
        decimal customerBalance = 0m)
    {
        EnsureLicense();
        var resolvedCustomerName = string.IsNullOrWhiteSpace(invoice.CustomerName)
            ? customerName
            : invoice.CustomerName;
        var document = BuildInvoiceDocument(invoice, resolvedCustomerName, customerBalance);
        if (exportPdf)
        {
            SavePdf(document, BuildInvoiceFileName(resolvedCustomerName, invoice.InvoiceDate));
            return;
        }
        ShowPreview(document, $"فاتورة بيع — {invoice.InvoiceNumber}");
    }

    private static string BuildInvoiceFileName(string customerName, DateTime invoiceDate)
    {
        var name = string.IsNullOrWhiteSpace(customerName) ? "عميل" : customerName.Trim();
        return $"فاتورة - {SanitizeForFileName(name)} - {invoiceDate:yyyy-MM-dd}.pdf";
    }

    private static string SanitizeForFileName(string value)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var chars = value.ToCharArray();
        for (var i = 0; i < chars.Length; i++)
        {
            if (Array.IndexOf(invalid, chars[i]) >= 0)
                chars[i] = '-';
        }
        return new string(chars);
    }

    public static void ShowDeliveryNotePreview(SalesInvoiceDto invoice, string customerName)
    {
        EnsureLicense();
        var document = BuildDeliveryNoteDocument(invoice, customerName);
        ShowPreview(document, $"إشعار تسليم — {invoice.InvoiceNumber}");
    }

    public static void ExportDeliveryNotePdf(SalesInvoiceDto invoice, string customerName)
    {
        EnsureLicense();
        var document = BuildDeliveryNoteDocument(invoice, customerName);
        SavePdf(document, $"DeliveryNote-{invoice.InvoiceNumber}.pdf");
    }

    private static void SavePdf(IDocument document, string suggestedName)
    {
        var dlg = new SaveFileDialog
        {
            Filter = "PDF Document (*.pdf)|*.pdf",
            FileName = suggestedName
        };
        if (dlg.ShowDialog() != true) return;
        document.GeneratePdf(dlg.FileName);
        try { Process.Start(new ProcessStartInfo(dlg.FileName) { UseShellExecute = true }); }
        catch { /* opening is optional */ }
    }

    private static void ShowPreview(IDocument document, string title)
    {
        var tempPath = Path.Combine(Path.GetTempPath(), $"erp-preview-{Guid.NewGuid():N}.pdf");
        document.GeneratePdf(tempPath);

        var win = new WpfWindow
        {
            Title = title,
            Width = 640,
            Height = 380,
            WindowStartupLocation = WpfStartLocation.CenterOwner,
            Owner = System.Windows.Application.Current?.MainWindow,
            FlowDirection = WpfFlowDirection.RightToLeft,
            Background = WpfBrushes.WhiteSmoke
        };

        var grid = new WpfGrid();
        grid.RowDefinitions.Add(new WpfRowDefinition { Height = new WpfGridLength(1, WpfGridUnitType.Star) });

        var host = new WpfStackPanel
        {
            Margin = new WpfThickness(24),
            VerticalAlignment = WpfVAlign.Center,
            HorizontalAlignment = WpfHAlign.Center
        };
        host.Children.Add(new WpfTextBlock
        {
            Text = title,
            FontSize = 18,
            FontWeight = WpfFontWeights.SemiBold,
            HorizontalAlignment = WpfHAlign.Center,
            Margin = new WpfThickness(0, 0, 0, 16)
        });
        host.Children.Add(new WpfTextBlock
        {
            Text = "تم إنشاء المستند وحُفظ مؤقتاً. اضغط «فتح» لعرضه في القارئ الافتراضي.",
            FontSize = 12,
            HorizontalAlignment = WpfHAlign.Center,
            TextAlignment = WpfTextAlign.Center,
            Margin = new WpfThickness(0, 0, 0, 12)
        });
        host.Children.Add(new WpfTextBlock
        {
            Text = tempPath,
            FontSize = 10,
            HorizontalAlignment = WpfHAlign.Center,
            Foreground = WpfBrushes.Gray,
            Margin = new WpfThickness(0, 0, 0, 20)
        });

        var btnRow = new WpfStackPanel { Orientation = WpfOrientation.Horizontal, HorizontalAlignment = WpfHAlign.Center };
        var btnOpen = new WpfButton { Content = "فتح المستند", Width = 130, Height = 34, Margin = new WpfThickness(0, 0, 8, 0) };
        btnOpen.Click += (_, _) =>
        {
            try { Process.Start(new ProcessStartInfo(tempPath) { UseShellExecute = true }); }
            catch (Exception ex) { WpfMessageBox.Show(ex.Message, "خطأ", WpfMessageBoxButton.OK, WpfMessageBoxImage.Error); }
        };
        var btnPrint = new WpfButton { Content = "طباعة", Width = 100, Height = 34, Margin = new WpfThickness(0, 0, 8, 0) };
        btnPrint.Click += (_, _) => PrintPdf(tempPath);
        var btnClose = new WpfButton { Content = "إغلاق", Width = 100, Height = 34, IsCancel = true };
        btnClose.Click += (_, _) => win.Close();
        btnRow.Children.Add(btnOpen);
        btnRow.Children.Add(btnPrint);
        btnRow.Children.Add(btnClose);
        host.Children.Add(btnRow);

        WpfGrid.SetRow(host, 0);
        grid.Children.Add(host);

        win.Content = grid;
        win.ShowDialog();
    }

    private static void PrintPdf(string path)
    {
        try
        {
            var psi = new ProcessStartInfo(path)
            {
                Verb = "print",
                UseShellExecute = true,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden
            };
            Process.Start(psi);
            MockInteractionService.ShowInfo("تم إرسال المستند إلى الطابعة الافتراضية.", "طباعة");
        }
        catch (Exception ex)
        {
            WpfMessageBox.Show(
                "لم تعثر Windows على طابعة قادرة على طباعة PDF مباشرة. استخدم زر «فتح» ثم اطبع من داخل القارئ.\n\n" + ex.Message,
                "طباعة",
                WpfMessageBoxButton.OK,
                WpfMessageBoxImage.Warning);
        }
    }

    private static IDocument BuildInvoiceDocument(
        SalesInvoiceDto invoice,
        string customerName,
        decimal customerBalance) =>
        Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(QPageSizes.A4);
                page.Margin(30);
                page.DefaultTextStyle(t => t.FontFamily("Arial").FontSize(10));
                page.ContentFromRightToLeft();

                page.Header().Element(h => InvoiceHeader(h, invoice));
                page.Content().Column(col =>
                {
                    col.Spacing(12);
                    col.Item().Element(e => CustomerBox(e, invoice, customerName, customerBalance));
                    col.Item().Element(e => LinesTable(e, invoice, invoice.Lines));
                    col.Item().AlignRight().Element(e => TotalsBox(e, invoice));
                    col.Item().PaddingTop(20).Text("شكراً لتعاملكم معنا").FontSize(11).Bold().AlignCenter();
                });
                page.Footer().AlignCenter().Text(t =>
                {
                    t.Span("فاتورة رقم ").FontSize(8);
                    t.Span(invoice.InvoiceNumber).FontSize(8).Bold();
                    t.Span(" — صفحة ").FontSize(8);
                    t.CurrentPageNumber().FontSize(8);
                    t.Span(" / ").FontSize(8);
                    t.TotalPages().FontSize(8);
                });
            });
        });

    private static IDocument BuildDeliveryNoteDocument(SalesInvoiceDto invoice, string customerName) =>
        Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(QPageSizes.A4);
                page.Margin(30);
                page.DefaultTextStyle(t => t.FontFamily("Arial").FontSize(10));
                page.ContentFromRightToLeft();

                page.Header().Row(row =>
                {
                    row.RelativeItem().Column(c =>
                    {
                        c.Item().Text("إشعار تسليم بضاعة").FontSize(18).Bold();
                        c.Item().Text($"مرجع الفاتورة: {invoice.InvoiceNumber}").FontSize(10);
                        c.Item().Text($"تاريخ الإصدار: {invoice.InvoiceDate:yyyy-MM-dd}").FontSize(10);
                    });
                    row.ConstantItem(100).Height(60).Background(QColors.Grey.Lighten3).AlignCenter().AlignMiddle().Text("شعار").FontSize(10);
                });

                page.Content().Column(col =>
                {
                    col.Spacing(12);
                    col.Item().Element(e => CustomerBox(e, invoice, customerName));
                    col.Item().Element(e => LinesTable(e, invoice, invoice.Lines));

                    col.Item().PaddingTop(30).Row(r =>
                    {
                        r.RelativeItem().Column(c =>
                        {
                            c.Item().Text("سلّم:").Bold();
                            c.Item().PaddingTop(30).LineHorizontal(1);
                            c.Item().Text("التوقيع / التاريخ").FontSize(9);
                        });
                        r.ConstantItem(40);
                        r.RelativeItem().Column(c =>
                        {
                            c.Item().Text("استلم:").Bold();
                            c.Item().PaddingTop(30).LineHorizontal(1);
                            c.Item().Text("التوقيع / التاريخ").FontSize(9);
                        });
                    });
                });

                page.Footer().AlignCenter().Text($"إشعار تسليم — {invoice.InvoiceNumber}").FontSize(8);
            });
        });

    private static void InvoiceHeader(IContainer c, SalesInvoiceDto invoice)
    {
        c.PaddingBottom(10).BorderBottom(1).BorderColor(QColors.Grey.Lighten2).PaddingBottom(10).Row(row =>
        {
            row.RelativeItem().Column(col =>
            {
                col.Item().Text("فاتورة بيع").FontSize(20).Bold();
                col.Item().Text($"رقم الفاتورة: {invoice.InvoiceNumber}").FontSize(11).SemiBold();
                col.Item().Text($"تاريخ الإصدار: {invoice.InvoiceDate:yyyy-MM-dd}").FontSize(10);
                col.Item().Text($"طريقة الدفع: {DisplayPaymentType(invoice.PaymentType)}").FontSize(10);
                col.Item().Text($"الحالة: {DisplayStatus(invoice.Status)}").FontSize(10);
            });
            row.ConstantItem(110).Height(65).Background(QColors.Grey.Lighten3).AlignCenter().AlignMiddle().Text("شعار الشركة").FontSize(10);
        });
    }

    private static void CustomerBox(
        IContainer c,
        SalesInvoiceDto invoice,
        string customerName,
        decimal customerBalance = 0m) =>
        c.Background(QColors.Grey.Lighten4).Padding(10).Column(col =>
        {
            col.Item().Text("بيانات العميل").FontSize(11).Bold();
            var displayName = string.IsNullOrWhiteSpace(invoice.CustomerName)
                ? customerName
                : invoice.CustomerName;
            col.Item().Text(string.IsNullOrWhiteSpace(displayName) ? "عميل غير محدد" : displayName)
                .FontSize(12).SemiBold();
            col.Item().PaddingTop(3)
                .Text($"المستودع: {(string.IsNullOrWhiteSpace(invoice.WarehouseName) ? "غير محدد" : invoice.WarehouseName)}")
                .FontSize(10);
            col.Item().PaddingTop(3).Row(row =>
            {
                row.AutoItem().Text("آخر رصيد للعميل:").FontSize(10).SemiBold();
                row.AutoItem().PaddingRight(5).ContentFromLeftToRight()
                    .Text($"{customerBalance:N2}").FontSize(10).SemiBold();
            });
        });

    private static void LinesTable(IContainer c, SalesInvoiceDto invoice, IReadOnlyList<SalesInvoiceLineDto> lines) =>
        c.Table(t =>
        {
            t.ColumnsDefinition(cd =>
            {
                cd.RelativeColumn(3);
                cd.RelativeColumn(2);
                cd.ConstantColumn(50);
                cd.RelativeColumn(2);
                cd.RelativeColumn(2);
                cd.RelativeColumn(2);
                cd.RelativeColumn(2);
                cd.RelativeColumn(2);
            });

            t.Header(h =>
            {
                h.Cell().Element(HeaderCell).Text("الصنف");
                h.Cell().Element(HeaderCell).Text("اللون");
                h.Cell().Element(HeaderCell).AlignCenter().Text("عدد الأثواب");
                h.Cell().Element(HeaderCell).AlignRight().Text("الطول");
                h.Cell().Element(HeaderCell).AlignRight().Text("سعر الوحدة");
                h.Cell().Element(HeaderCell).AlignRight().Text("الخصم");
                h.Cell().Element(HeaderCell).AlignRight().Text("الضريبة");
                h.Cell().Element(HeaderCell).AlignRight().Text("الإجمالي");
            });

            foreach (var line in lines)
            {
                t.Cell().Element(BodyCell).Text($"{line.FabricDisplayName} ({line.FabricCode})");
                t.Cell().Element(BodyCell).Text(line.ColorDisplayName);
                t.Cell().Element(BodyCell).AlignCenter().Text(line.RollCount.ToString());
                t.Cell().Element(BodyCell).AlignRight().Text($"{line.TotalLengthMeters:N2}");
                t.Cell().Element(BodyCell).AlignRight().Text($"{line.UnitPrice:N2}");
                t.Cell().Element(BodyCell).AlignRight().Text($"{line.DiscountAmount:N2}");
                t.Cell().Element(BodyCell).AlignRight().Text(line.TaxAmount > 0 ? $"{line.TaxAmount:N2}" : "—");
                t.Cell().Element(BodyCell).AlignRight().Text($"{line.LineTotal:N2}");

                if (line.RollLengths.Count > 0)
                {
                    var breakdown = string.Join("     ", line.RollLengths.Select(r => $"({r.RollSequence}) {r.LengthMeters:N2}"));
                    t.Cell().ColumnSpan(8).Element(BreakdownCell)
                        .Text($"تفصيل أطوال الأثواب (م): {breakdown}").FontSize(8.5f).Italic().FontColor(QColors.Grey.Darken2);
                }
            }

            static IContainer HeaderCell(IContainer x) =>
                x.Background(QColors.Grey.Lighten2).Padding(6).BorderBottom(1).BorderColor(QColors.Grey.Medium).DefaultTextStyle(s => s.Bold().FontSize(10));

            static IContainer BodyCell(IContainer x) =>
                x.Padding(6).BorderBottom(1).BorderColor(QColors.Grey.Lighten3);

            static IContainer BreakdownCell(IContainer x) =>
                x.Background(QColors.Grey.Lighten5).PaddingVertical(3).PaddingHorizontal(8).BorderBottom(1).BorderColor(QColors.Grey.Lighten3).AlignRight();
        });

    private static void TotalsBox(IContainer c, SalesInvoiceDto invoice) =>
        c.Width(280).Padding(6).Column(col =>
        {
            if (invoice.IsLegacyUntaxed)
            {
                col.Item().Text("فاتورة تاريخية — بدون ضريبة (Legacy Untaxed)")
                    .FontSize(9).Italic().FontColor(QColors.Grey.Darken1);
            }

            var lineDiscount = invoice.Lines.Sum(l => l.DiscountAmount);
            AddRow(col, "المجموع الفرعي", invoice.SubTotal);
            AddRow(col, "خصم الأسطر", lineDiscount);
            AddRow(col, "خصم الفاتورة", invoice.DiscountTotal);
            if (!invoice.IsLegacyUntaxed)
            {
                var taxable = invoice.Lines.Sum(l => l.TaxableAmount);
                if (taxable > 0) AddRow(col, "المبلغ الخاضع", taxable);
            }
            AddRow(col, "الضريبة", invoice.TaxTotal);
            if (invoice.TaxTotal != 0m)
            {
                foreach (var group in invoice.Lines.Where(l => l.TaxAmount > 0).GroupBy(l => l.TaxCode))
                {
                    AddRow(col, $"  {group.Key} ({group.First().TaxRate:P0})", group.Sum(x => x.TaxAmount));
                }
            }
            if (Math.Abs(invoice.RoundingDifference) >= 0.01m)
                AddRow(col, "فرق التقريب", invoice.RoundingDifference);
            col.Item().PaddingTop(4).BorderTop(1).BorderColor(QColors.Grey.Medium);
            col.Item().PaddingTop(4).Row(r =>
            {
                r.RelativeItem().Text("الإجمالي المستحق").FontSize(12).Bold();
                r.ConstantItem(90).AlignRight().Text($"{invoice.GrandTotal:N2}").FontSize(12).Bold();
            });
        });

    private static void AddRow(ColumnDescriptor col, string label, decimal value) =>
        col.Item().Row(r =>
        {
            r.RelativeItem().Text(label).FontSize(10);
            r.ConstantItem(90).AlignRight().Text($"{value:N2}").FontSize(10);
        });

    private static string DisplayPaymentType(PaymentType t) => t switch
    {
        PaymentType.Cash => "نقداً",
        PaymentType.Credit => "بالآجل",
        _ => t.ToString()
    };

    private static string DisplayStatus(SalesInvoiceStatus s) => s switch
    {
        SalesInvoiceStatus.Draft => "مسودة",
        SalesInvoiceStatus.AwaitingDetailing => "بانتظار التفصيل",
        SalesInvoiceStatus.Detailed => "تم التفصيل",
        SalesInvoiceStatus.ReadyForApproval => "بانتظار الاعتماد",
        SalesInvoiceStatus.Approved => "معتمدة",
        SalesInvoiceStatus.Printed => "مطبوعة",
        SalesInvoiceStatus.Delivered => "مُسلمة",
        SalesInvoiceStatus.PartiallyReturned => "مرتجع جزئي",
        SalesInvoiceStatus.Returned => "مرتجعة",
        SalesInvoiceStatus.Cancelled => "ملغاة",
        _ => s.ToString()
    };
}
