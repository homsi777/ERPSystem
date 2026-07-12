using ERPSystem.Application.Documents;
using ERPSystem.Application.DTOs.Expenses;
using ERPSystem.Services.Documents;

namespace ERPSystem.Services.Expenses;

/// <summary>Desktop expense report print — uses the shared <see cref="ExpenseReportPdfGenerator"/> (same as web/API).</summary>
public static class ExpenseReportDocumentService
{
    private static ExpenseReportPdfGenerator? _generator;

    private static ExpenseReportPdfGenerator Generator =>
        _generator ??= ExpenseReportPdfGenerator.FromContentRoot(AppContext.BaseDirectory);

    public static void ShowReportPreview(ExpenseReportDto report, bool exportPdf)
    {
        ArgumentNullException.ThrowIfNull(report);
        var pdfBytes = Generator.Generate(report);

        if (exportPdf)
        {
            PdfPreviewWindow.SaveAndOpenFromBytes(pdfBytes, $"تقرير مصاريف - {DateTime.Now:yyyy-MM-dd}.pdf");
            return;
        }

        PdfPreviewWindow.ShowFromBytes(pdfBytes, $"تقرير مصاريف — {report.Title}");
    }
}
