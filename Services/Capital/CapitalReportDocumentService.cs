using ERPSystem.Application.Documents;
using ERPSystem.Application.DTOs.Capital;
using ERPSystem.Services.Documents;

namespace ERPSystem.Services.Capital;

public static class CapitalReportDocumentService
{
    private static CapitalReportPdfGenerator? _generator;

    private static CapitalReportPdfGenerator Generator =>
        _generator ??= CapitalReportPdfGenerator.FromContentRoot(AppContext.BaseDirectory);

    public static void ShowPreview(CapitalReportDto report, bool exportPdf)
    {
        ArgumentNullException.ThrowIfNull(report);
        var pdfBytes = Generator.Generate(report);

        if (exportPdf)
        {
            PdfPreviewWindow.SaveAndOpenFromBytes(pdfBytes, $"{report.Title} - {DateTime.Now:yyyy-MM-dd}.pdf");
            return;
        }

        PdfPreviewWindow.ShowFromBytes(pdfBytes, report.Title);
    }

    public static void ExportExcel(CapitalReportDto report)
    {
        ArgumentNullException.ThrowIfNull(report);
        ListExportService.ExportRecords(
            report.Rows,
            report.Title,
            ("المفتاح", r => r.Key),
            ("الوصف", r => r.Label),
            ("تفاصيل", r => r.SubLabel ?? ""),
            ("المبلغ", r => r.Amount));
    }
}

public static class CapitalPartnerDocumentService
{
    private static CapitalReportPdfGenerator? _generator;

    private static CapitalReportPdfGenerator Generator =>
        _generator ??= CapitalReportPdfGenerator.FromContentRoot(AppContext.BaseDirectory);

    public static void ShowPreview(CapitalOperationsCenterDto operations, bool exportPdf)
    {
        ArgumentNullException.ThrowIfNull(operations);
        var pdfBytes = Generator.GeneratePartner(operations);
        var name = operations.Details.FullName;

        if (exportPdf)
        {
            PdfPreviewWindow.SaveAndOpenFromBytes(pdfBytes, $"تقرير شريك - {name} - {DateTime.Now:yyyy-MM-dd}.pdf");
            return;
        }

        PdfPreviewWindow.ShowFromBytes(pdfBytes, $"تقرير شريك — {name}");
    }

    public static void ExportExcel(CapitalOperationsCenterDto operations)
    {
        ArgumentNullException.ThrowIfNull(operations);
        var f = operations.Financial;
        var rows = new[]
        {
            new { Label = "رأس المال الحالي", Amount = f.CurrentCapitalBase },
            new { Label = "إجمالي الاستثمارات", Amount = f.TotalInvestmentsBase },
            new { Label = "إجمالي السحوبات", Amount = f.TotalWithdrawalsBase },
            new { Label = "أرباح موزعة", Amount = f.DistributedProfitBase },
            new { Label = "أرباح غير موزعة", Amount = f.UndistributedProfitBase }
        };

        ListExportService.ExportRecords(
            rows,
            $"تقرير شريك - {operations.Details.FullName}",
            ("الوصف", r => r.Label),
            ("المبلغ", r => r.Amount));
    }
}
