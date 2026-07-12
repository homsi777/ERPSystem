using ERPSystem.Application.Documents;
using ERPSystem.Application.DTOs.Reports;
using ERPSystem.Services;
using ERPSystem.Services.Documents;
using System.IO;

namespace ERPSystem.Services.Reports;

public static class ModuleReportDocumentService
{
    private static ModuleReportPdfGenerator? _generator;

    private static ModuleReportPdfGenerator Generator =>
        _generator ??= ModuleReportPdfGenerator.FromContentRoot(AppContext.BaseDirectory);

    public static void ShowPreview(ModuleReportResultDto report, bool exportPdf)
    {
        ArgumentNullException.ThrowIfNull(report);
        var pdfBytes = Generator.Generate(report);
        var safeTitle = string.Concat((report.Title ?? "تقرير").Split(Path.GetInvalidFileNameChars()));

        if (exportPdf)
        {
            PdfPreviewWindow.SaveAndOpenFromBytes(pdfBytes, $"{safeTitle} - {DateTime.Now:yyyy-MM-dd}.pdf");
            return;
        }

        PdfPreviewWindow.ShowFromBytes(pdfBytes, report.Title ?? "تقرير");
    }

    public static void ExportExcel(ModuleReportResultDto report)
    {
        ArgumentNullException.ThrowIfNull(report);
        if (report.Rows.Count == 0)
        {
            MockInteractionService.ShowWarning("لا توجد بيانات للتصدير.", "تصدير");
            return;
        }

        var columns = report.Columns
            .Select(c => (c.HeaderAr, (Func<Dictionary<string, object?>, object?>)(row =>
            {
                row.TryGetValue(c.Key, out var value);
                return value;
            })))
            .ToArray();

        ListExportService.ExportRecords(report.Rows, report.Title, columns);
    }
}
