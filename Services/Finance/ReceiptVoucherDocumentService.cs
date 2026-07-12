using ERPSystem.Application.Documents;
using ERPSystem.Application.DTOs.Finance;
using ERPSystem.Services.Documents;

namespace ERPSystem.Services.Finance;

/// <summary>Desktop receipt voucher print — uses the shared <see cref="ReceiptVoucherPdfGenerator"/> (same as web/API).</summary>
public static class ReceiptVoucherDocumentService
{
    private static ReceiptVoucherPdfGenerator? _generator;

    private static ReceiptVoucherPdfGenerator Generator =>
        _generator ??= ReceiptVoucherPdfGenerator.FromContentRoot(AppContext.BaseDirectory);

    public static void ShowVoucherPreview(ReceiptVoucherPrintDto voucher, bool exportPdf)
    {
        ArgumentNullException.ThrowIfNull(voucher);
        var pdfBytes = Generator.Generate(voucher);
        var fileName = $"سند قبض - {voucher.VoucherNumber} - {voucher.VoucherDate:yyyy-MM-dd}.pdf";

        if (exportPdf)
        {
            PdfPreviewWindow.SaveAndOpenFromBytes(pdfBytes, fileName);
            return;
        }

        PdfPreviewWindow.ShowFromBytes(pdfBytes, $"سند قبض — {voucher.VoucherNumber}");
    }
}
