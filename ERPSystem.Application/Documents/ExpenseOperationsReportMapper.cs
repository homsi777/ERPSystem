using ERPSystem.Application.DTOs.Expenses;

namespace ERPSystem.Application.Documents;

public static class ExpenseOperationsReportMapper
{
    public static ExpenseReportDto ToSingleExpenseReport(ExpenseOperationsCenterDto oc)
    {
        ArgumentNullException.ThrowIfNull(oc);
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
}
