using ERPSystem.Application.DTOs.Reports;
using ERPSystem.Application.DTOs.Sales;
using ERPSystem.Services;
using System.Globalization;

namespace ERPSystem.Services.Reports;

public static class SalesTaxReportDocumentService
{
    public static void Export(SalesTaxReportDto report, DateTime from, DateTime to, string mode)
    {
        ArgumentNullException.ThrowIfNull(report);
        if (report.Rows.Count == 0)
        {
            MockInteractionService.ShowWarning("لا توجد بيانات للتصدير.", "تصدير");
            return;
        }

        var dto = ToModuleReport(report, from, to);
        switch (mode)
        {
            case "excel":
                ModuleReportDocumentService.ExportExcel(dto);
                break;
            case "pdf":
                ModuleReportDocumentService.ShowPreview(dto, exportPdf: true);
                break;
            default:
                ModuleReportDocumentService.ShowPreview(dto, exportPdf: false);
                break;
        }
    }

    public static ModuleReportResultDto ToModuleReport(SalesTaxReportDto report, DateTime from, DateTime to)
    {
        var taxable = report.Rows.Where(r => !r.IsLegacyUntaxed).Sum(r => r.TaxableAmount);
        var outputVat = report.Rows.Where(r => !r.IsLegacyUntaxed).Sum(r => r.TaxAmount);

        return new ModuleReportResultDto
        {
            Title = "تقرير ضريبة المبيعات",
            GeneratedAt = DateTime.UtcNow,
            FromDate = from,
            ToDate = to,
            Kpis =
            [
                new ModuleReportKpiDto { Label = "المبلغ الخاضع", Value = taxable.ToString("N2", CultureInfo.InvariantCulture) },
                new ModuleReportKpiDto { Label = "ضريبة المخرجات", Value = outputVat.ToString("N2", CultureInfo.InvariantCulture) },
                new ModuleReportKpiDto { Label = "عدد السجلات", Value = report.Rows.Count.ToString(CultureInfo.InvariantCulture) }
            ],
            Columns =
            [
                new() { Key = "invoice", HeaderAr = "رقم الفاتورة", Width = 110 },
                new() { Key = "date", HeaderAr = "التاريخ", Width = 90 },
                new() { Key = "customer", HeaderAr = "العميل", IsStar = true },
                new() { Key = "taxCode", HeaderAr = "كود الضريبة", Width = 90 },
                new() { Key = "taxable", HeaderAr = "المبلغ الخاضع", Width = 100, Format = "N2" },
                new() { Key = "tax", HeaderAr = "ضريبة المخرجات", Width = 100, Format = "N2" },
                new() { Key = "status", HeaderAr = "الترحيل", Width = 90 },
                new() { Key = "journal", HeaderAr = "قيد اليومية", Width = 100 }
            ],
            Rows = report.Rows.Select(r => new Dictionary<string, object?>
            {
                ["invoice"] = r.InvoiceNumber,
                ["date"] = r.InvoiceDate,
                ["customer"] = r.CustomerName,
                ["taxCode"] = r.IsLegacyUntaxed ? "Legacy" : r.TaxCode ?? "—",
                ["taxable"] = r.TaxableAmount,
                ["tax"] = r.TaxAmount,
                ["status"] = r.PostingStatus,
                ["journal"] = r.JournalEntryNumber ?? "—"
            }).ToList()
        };
    }
}
