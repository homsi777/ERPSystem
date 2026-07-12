using ERPSystem.Application.Documents;
using ERPSystem.Application.DTOs.Finance;
using ERPSystem.Services.Documents;

namespace ERPSystem.Services.Finance;

/// <summary>Desktop payment voucher print — uses the shared <see cref="PaymentVoucherPdfGenerator"/> (same as web/API).</summary>
public static class PaymentVoucherDocumentService
{
    private static PaymentVoucherPdfGenerator? _generator;

    private static PaymentVoucherPdfGenerator Generator =>
        _generator ??= PaymentVoucherPdfGenerator.FromContentRoot(AppContext.BaseDirectory);

    public static void ShowVoucherPreview(PaymentVoucherPrintDto voucher, bool exportPdf)
    {
        ArgumentNullException.ThrowIfNull(voucher);
        var pdfBytes = Generator.Generate(voucher);
        var fileName = $"سند صرف - {voucher.VoucherNumber} - {voucher.VoucherDate:yyyy-MM-dd}.pdf";

        if (exportPdf)
        {
            PdfPreviewWindow.SaveAndOpenFromBytes(pdfBytes, fileName);
            return;
        }

        PdfPreviewWindow.ShowFromBytes(pdfBytes, $"سند صرف — {voucher.VoucherNumber}");
    }
}
