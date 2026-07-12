using ERPSystem.Application.Documents;
using ERPSystem.Application.DTOs.Customers;
using ERPSystem.Services.Documents;
using System.IO;

namespace ERPSystem.Services.Customers;

/// <summary>Desktop customer account statement PDF — uses shared <see cref="CustomerAccountLedgerPdfGenerator"/>.</summary>
public static class CustomerStatementDocumentService
{
    private static CustomerAccountLedgerPdfGenerator? _generator;

    private static CustomerAccountLedgerPdfGenerator Generator =>
        _generator ??= CustomerAccountLedgerPdfGenerator.FromContentRoot(AppContext.BaseDirectory);

    public static void ShowLedgerPreview(
        CustomerAccountLedgerDto ledger,
        DateTime? from,
        DateTime? to,
        bool exportPdf)
    {
        ArgumentNullException.ThrowIfNull(ledger);
        var pdfBytes = Generator.Generate(ledger, from, to);
        var safeName = string.Join("_", ledger.CustomerName.Split(Path.GetInvalidFileNameChars()));

        if (exportPdf)
        {
            PdfPreviewWindow.SaveAndOpenFromBytes(
                pdfBytes,
                $"كشف حساب - {safeName} - {DateTime.Now:yyyy-MM-dd}.pdf");
            return;
        }

        PdfPreviewWindow.ShowFromBytes(pdfBytes, $"كشف حساب — {ledger.CustomerName}");
    }
}
