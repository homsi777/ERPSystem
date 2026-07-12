using ERPSystem.Application.Documents;
using ERPSystem.Application.DTOs.Purchases;
using ERPSystem.Services.Documents;
using System.IO;

namespace ERPSystem.Services.Purchases;

/// <summary>Purchase invoice PDF — shared <see cref="PurchaseInvoicePdfGenerator"/> + <see cref="PdfPreviewWindow"/>.</summary>
public static class PurchaseDocumentService
{
    private static PurchaseInvoicePdfGenerator? _generator;

    private static PurchaseInvoicePdfGenerator Generator =>
        _generator ??= PurchaseInvoicePdfGenerator.FromContentRoot(AppContext.BaseDirectory);

    public static void ShowInvoicePreview(PurchaseInvoiceDetailsDto invoice, bool exportPdf)
    {
        ArgumentNullException.ThrowIfNull(invoice);
        var pdfBytes = Generator.Generate(invoice);
        var safeNumber = string.Join("_", invoice.InvoiceNumber.Split(Path.GetInvalidFileNameChars()));

        if (exportPdf)
        {
            PdfPreviewWindow.SaveAndOpenFromBytes(pdfBytes, $"فاتورة مشتريات - {safeNumber}.pdf");
            return;
        }

        PdfPreviewWindow.ShowFromBytes(pdfBytes, $"فاتورة مشتريات — {invoice.InvoiceNumber}");
    }
}
