using ERPSystem.Application.Documents;
using ERPSystem.Application.DTOs.Accounting;
using ERPSystem.Services.Documents;
using System.IO;

namespace ERPSystem.Services.Accounting;

public static class AccountingJournalDocumentService
{
    private static JournalEntryPdfGenerator? _generator;

    private static JournalEntryPdfGenerator Generator =>
        _generator ??= JournalEntryPdfGenerator.FromContentRoot(AppContext.BaseDirectory);

    public static void ShowPreview(JournalEntryDetailsDto entry, bool exportPdf)
    {
        ArgumentNullException.ThrowIfNull(entry);
        var pdfBytes = Generator.Generate(entry);
        var safeNumber = string.Join("_", entry.EntryNumber.Split(Path.GetInvalidFileNameChars()));

        if (exportPdf)
        {
            PdfPreviewWindow.SaveAndOpenFromBytes(pdfBytes, $"قيد يومية - {safeNumber}.pdf");
            return;
        }

        PdfPreviewWindow.ShowFromBytes(pdfBytes, $"قيد يومية — {entry.EntryNumber}");
    }
}
