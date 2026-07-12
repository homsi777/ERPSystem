using ERPSystem.Application.Documents;
using ERPSystem.Application.DTOs.Sales;
using ERPSystem.Services.Documents;
using System.IO;

namespace ERPSystem.Services.Sales;

/// <summary>
/// Sales invoice PDF uses <see cref="SalesInvoicePdfGenerator"/>; delivery note uses <see cref="DeliveryNotePdfGenerator"/>.
/// Both route through <see cref="PdfPreviewWindow"/>.
/// </summary>
public static class SalesDocumentService
{
    private static SalesInvoicePdfGenerator? _invoiceGenerator;
    private static DeliveryNotePdfGenerator? _deliveryGenerator;

    private static SalesInvoicePdfGenerator InvoiceGenerator =>
        _invoiceGenerator ??= SalesInvoicePdfGenerator.FromContentRoot(AppContext.BaseDirectory);

    private static DeliveryNotePdfGenerator DeliveryGenerator =>
        _deliveryGenerator ??= DeliveryNotePdfGenerator.FromContentRoot(AppContext.BaseDirectory);

    public static void ShowInvoicePreview(SalesInvoiceOperationsCenterDto operations, bool exportPdf)
    {
        ArgumentNullException.ThrowIfNull(operations);
        var invoice = operations.Invoice;
        var pdfBytes = InvoiceGenerator.Generate(operations);
        var customerName = string.IsNullOrWhiteSpace(invoice.CustomerName) ? "عميل" : invoice.CustomerName.Trim();
        var fileName = BuildInvoiceFileName(customerName, invoice.InvoiceDate);

        if (exportPdf)
        {
            PdfPreviewWindow.SaveAndOpenFromBytes(pdfBytes, fileName);
            return;
        }

        PdfPreviewWindow.ShowFromBytes(pdfBytes, $"فاتورة بيع — {invoice.InvoiceNumber}");
    }

    public static void ShowDeliveryNotePreview(SalesInvoiceDto invoice, string customerName)
    {
        ArgumentNullException.ThrowIfNull(invoice);
        var pdfBytes = DeliveryGenerator.Generate(invoice, customerName);
        PdfPreviewWindow.ShowFromBytes(pdfBytes, $"إشعار تسليم — {invoice.InvoiceNumber}");
    }

    public static void ExportDeliveryNotePdf(SalesInvoiceDto invoice, string customerName)
    {
        ArgumentNullException.ThrowIfNull(invoice);
        var pdfBytes = DeliveryGenerator.Generate(invoice, customerName);
        PdfPreviewWindow.SaveAndOpenFromBytes(pdfBytes, $"DeliveryNote-{invoice.InvoiceNumber}.pdf");
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
}
