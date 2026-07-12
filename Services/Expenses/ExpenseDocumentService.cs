using ERPSystem.Application.DTOs.Expenses;
using ERPSystem.Core.Actions;
using ERPSystem.Services;
using ERPSystem.Services.Documents;
using System.Windows;

namespace ERPSystem.Services.Expenses;

/// <summary>Desktop expense export — PDF/Excel/print/share from operations-center data (same source as web).</summary>
public static class ExpenseDocumentService
{
    public static async Task HandleExportAsync(EntityActionId actionId, ExpenseListDto expense)
    {
        if (!AppServices.IsInitialized)
            return;

        var ocResult = await ExpenseUiService.Instance.GetOperationsCenterAsync(expense.Id);
        if (!ApplicationResultPresenter.Present(ocResult) || ocResult.Value is null)
            return;

        var oc = ocResult.Value;
        switch (actionId)
        {
            case EntityActionId.ExpenseExportPdf:
                ExpenseReportDocumentService.ShowReportPreview(BuildReport(oc), exportPdf: true);
                break;
            case EntityActionId.ExpensePrint:
                ExpenseReportDocumentService.ShowReportPreview(BuildReport(oc), exportPdf: false);
                break;
            case EntityActionId.ExpenseExportExcel:
                ExportExcel(oc);
                break;
            case EntityActionId.ExpenseShareReport:
                ShareReport(oc);
                break;
        }
    }

    private static ExpenseReportDto BuildReport(ExpenseOperationsCenterDto oc)
    {
        var d = oc.Details;
        var f = oc.Financial;
        return new ExpenseReportDto
        {
            Title = $"مصروف {d.Code} — {d.Name}",
            ReportType = "Single",
            GeneratedAt = DateTime.UtcNow,
            BaseCurrency = f.BaseCurrency,
            ExpenseCount = 1,
            TotalBase = f.BaseAmount,
            TotalPaidBase = f.PaidAmountBase,
            TotalRemainingBase = f.RemainingBalanceBase,
            ScopeLabel = d.Name,
            Rows =
            [
                new ExpenseReportRowDto
                {
                    ExpenseId = d.Id,
                    Code = d.Code,
                    Name = d.Name,
                    Category = d.CategoryName,
                    CategoryKindDisplay = d.CategoryKindDisplay,
                    Status = d.StatusDisplay,
                    StartDate = d.StartDate,
                    EndDate = d.EndDate,
                    OriginalAmount = d.OriginalAmount,
                    Currency = d.OriginalCurrency,
                    ExchangeRate = d.ExchangeRate,
                    BaseAmount = f.BaseAmount,
                    PaidAmountBase = f.PaidAmountBase,
                    RemainingBalanceBase = f.RemainingBalanceBase,
                    Department = d.Department,
                    CostCenter = d.CostCenterName,
                    PayeeName = d.PayeeName,
                    PaymentMethod = d.PaymentMethodDisplay,
                    Description = d.Description,
                    Notes = d.Notes,
                    IsRecurring = d.IsRecurring,
                    NextDueDate = d.NextDueDate,
                    PaymentCount = f.CompletedPayments
                }
            ]
        };
    }

    private static void ExportExcel(ExpenseOperationsCenterDto oc)
    {
        var d = oc.Details;
        var payments = d.Payments.ToList();

        if (payments.Count == 0)
        {
            ListExportService.ExportRecords(
                [d],
                $"مصروف {d.Code}",
                ("الكود", x => x.Code),
                ("الاسم", x => x.Name),
                ("الفئة", x => x.CategoryName),
                ("الحالة", x => x.StatusDisplay),
                ("المدفوع", x => x.PaidAmountBase),
                ("المتبقي", x => x.RemainingBalanceBase),
                ("العملة", x => x.BaseCurrency));
            return;
        }

        ListExportService.ExportRecords(
            payments,
            $"قيود {d.Code}",
            ("التاريخ", p => p.PaymentDate),
            ("المبلغ", p => p.AmountBase),
            ("العملة", p => p.Currency),
            ("طريقة الدفع", p => p.PaymentMethodDisplay),
            ("مصدر التمويل", p => p.FundingSourceDisplay),
            ("المرجع", p => p.ReferenceNumber),
            ("ملاحظات", p => p.Notes));
    }

    private static void ShareReport(ExpenseOperationsCenterDto oc)
    {
        var d = oc.Details;
        var f = oc.Financial;
        var text =
            $"مصروف: {d.Name}\n" +
            $"الكود: {d.Code}\n" +
            $"المدفوع: {f.PaidAmountBase:N2} {f.BaseCurrency}\n" +
            $"المتبقي: {f.RemainingBalanceBase:N2} {f.BaseCurrency}";

        try
        {
            Clipboard.SetText(text);
            MockInteractionService.ShowSuccess("تم نسخ ملخص المصروف.");
        }
        catch
        {
            MockInteractionService.ShowWarning("تعذّر نسخ الملخص.", "مشاركة");
        }
    }
}
