using ERPSystem.Application.Abstractions.Repositories;
using ERPSystem.Application.Common;
using ERPSystem.Application.DTOs.Expenses;
using ERPSystem.Domain.Aggregates;
using ERPSystem.Domain.Entities.Expenses;
using ERPSystem.Domain.Entities.Finance;
using ERPSystem.Domain.Enums;
using ERPSystem.Domain.Services;

namespace ERPSystem.Application.Mapping;

public static class ExpenseMapper
{
    public static ExpenseListDto ToListDto(ExpenseAggregate aggregate, string categoryName, string? costCenterName = null) =>
        ToListDto(aggregate.Expense, categoryName, costCenterName);

    public static ExpenseListDto ToListDto(Expense expense, string categoryName, string? costCenterName = null) => new()
    {
        Id = expense.Id,
        Code = expense.Code,
        Name = expense.Name,
        CategoryKind = expense.CategoryKind,
        CategoryKindDisplay = expense.CategoryKind.ToArabic(),
        CategoryName = categoryName,
        Status = expense.Status,
        StatusDisplay = expense.Status.ToArabic(),
        StartDate = expense.StartDate,
        EndDate = expense.EndDate,
        OriginalCurrency = expense.OriginalCurrency,
        OriginalAmount = expense.OriginalAmount,
        BaseAmount = expense.BaseAmount,
        PaidAmountBase = expense.PaidAmountBase,
        RemainingBalanceBase = expense.RemainingBalanceBase,
        BaseCurrency = expense.BaseCurrency,
        Department = expense.Department,
        CostCenterId = expense.CostCenterId,
        CostCenterName = costCenterName,
        PayeeName = expense.PayeeName,
        IsRecurring = expense.IsRecurring,
        NextDueDate = expense.NextDueDate,
        IsArchived = expense.IsArchived
    };

    public static ExpenseDetailsDto ToDetailsDto(
        Expense expense,
        string categoryName,
        DateTime createdAt,
        string? createdByName,
        DateTime? updatedAt,
        string? costCenterName = null) => new()
    {
        Id = expense.Id,
        Code = expense.Code,
        Name = expense.Name,
        CategoryId = expense.CategoryId,
        CategoryKind = expense.CategoryKind,
        CategoryKindDisplay = expense.CategoryKind.ToArabic(),
        CategoryName = categoryName,
        Description = expense.Description,
        Status = expense.Status,
        StatusDisplay = expense.Status.ToArabic(),
        AllowedTransitions = ExpenseLifecycle.GetAllowedTransitions(expense.Status).ToList(),
        StartDate = expense.StartDate,
        EndDate = expense.EndDate,
        OriginalCurrency = expense.OriginalCurrency,
        OriginalAmount = expense.OriginalAmount,
        ExchangeRate = expense.ExchangeRate,
        BaseCurrency = expense.BaseCurrency,
        BaseAmount = expense.BaseAmount,
        PaidAmountBase = expense.PaidAmountBase,
        RemainingBalanceBase = expense.RemainingBalanceBase,
        PaymentMethod = expense.PaymentMethod,
        PaymentMethodDisplay = expense.PaymentMethod.ToArabic(),
        PayeeName = expense.PayeeName,
        SupplierId = expense.SupplierId,
        CostCenterId = expense.CostCenterId,
        CostCenterName = costCenterName,
        Department = expense.Department,
        ProjectCode = expense.ProjectCode,
        Notes = expense.Notes,
        IsRecurring = expense.IsRecurring,
        RecurrenceFrequency = expense.RecurrenceFrequency,
        RecurrenceDisplay = expense.RecurrenceFrequency.ToArabic(),
        CustomIntervalDays = expense.CustomIntervalDays,
        NextDueDate = expense.NextDueDate,
        RemainingInstallments = expense.RemainingInstallments,
        IsArchived = expense.IsArchived,
        CreatedAt = createdAt,
        CreatedByName = createdByName,
        UpdatedAt = updatedAt,
        Payments = expense.Payments.Select(ToPaymentDto).ToList(),
        Installments = expense.Installments.Select(ToInstallmentDto).ToList(),
        Attachments = expense.Attachments.Select(a => new ExpenseAttachmentDto
        {
            Id = a.Id,
            FileName = a.FileName,
            ContentType = a.ContentType,
            SizeBytes = a.SizeBytes
        }).ToList()
    };

    public static ExpenseOperationsCenterDto ToOperationsCenterDto(
        ExpenseDetailsDto details,
        IReadOnlyList<ExpenseTimelineEventDto> timeline,
        IReadOnlyList<ExpenseAuditEntryDto> audit) => new()
    {
        Details = details,
        LifecycleSteps = ExpenseLifecycle.BuildStepper(details.Status),
        Timeline = timeline,
        RecentAudit = audit.Take(20).ToList(),
        Financial = new ExpenseFinancialSummaryDto
        {
            OriginalAmount = details.OriginalAmount,
            OriginalCurrency = details.OriginalCurrency,
            BaseAmount = details.BaseAmount,
            BaseCurrency = details.BaseCurrency,
            PaidAmountBase = details.PaidAmountBase,
            RemainingBalanceBase = details.RemainingBalanceBase,
            ExchangeRate = details.ExchangeRate,
            CompletedPayments = details.Payments.Count(p => p.StatusDisplay == ExpensePaymentStatus.Completed.ToArabic()),
            ScheduledPayments = details.Payments.Count(p => p.StatusDisplay == ExpensePaymentStatus.Scheduled.ToArabic()),
            PendingInstallments = details.Installments.Count(i =>
                i.StatusDisplay is not "مدفوع" and not "ملغى"),
            NextPaymentDue = details.Installments
                .Where(i => i.StatusDisplay is not "مدفوع" and not "ملغى")
                .Select(i => (DateTime?)i.DueDate)
                .Concat(details.Payments.Where(p => p.DueDate is not null).Select(p => p.DueDate))
                .Where(d => d is not null)
                .OrderBy(d => d)
                .FirstOrDefault()
        },
        Statistics = new ExpenseStatisticsDto
        {
            TotalPayments = details.Payments.Count,
            TotalAttachments = details.Attachments.Count,
            DaysSinceCreated = (int)(DateTime.UtcNow.Date - details.CreatedAt.Date).TotalDays,
            AuditEventCount = audit.Count
        }
    };

    public static ExpensePaymentDto ToPaymentDto(ExpensePayment p) => new()
    {
        Id = p.Id,
        PaymentDate = p.PaymentDate,
        DueDate = p.DueDate,
        AmountOriginal = p.AmountOriginal,
        AmountBase = p.AmountBase,
        Currency = p.Currency,
        ExchangeRateSnapshot = p.ExchangeRateSnapshot,
        PaymentMethodDisplay = p.PaymentMethod.ToArabic(),
        FundingSourceDisplay = p.FundingSource.ToArabic(),
        StatusDisplay = p.Status.ToArabic(),
        ApprovalStatusDisplay = p.ApprovalStatus.ToArabic(),
        ReferenceNumber = p.ReferenceNumber,
        Notes = p.Notes,
        InstallmentNumber = p.InstallmentNumber,
        AttachmentId = p.AttachmentId
    };

    public static ExpenseInstallmentDto ToInstallmentDto(ExpenseInstallment i) => new()
    {
        Id = i.Id,
        InstallmentNumber = i.InstallmentNumber,
        DueDate = i.DueDate,
        AmountOriginal = i.AmountOriginal,
        AmountBase = i.AmountBase,
        Currency = i.Currency,
        StatusDisplay = i.Status.ToArabic(),
        PaymentId = i.PaymentId
    };

    public static ExpenseCategoryDto ToCategoryDto(ExpenseCategory category) => new()
    {
        Id = category.Id,
        Kind = category.Kind,
        Code = category.Code,
        NameAr = category.NameAr,
        NameEn = category.NameEn,
        KindDisplay = category.Kind.ToArabic()
    };

    public static CostCenterDto ToCostCenterDto(CostCenter costCenter) => new()
    {
        Id = costCenter.Id,
        Code = costCenter.Code,
        Name = costCenter.Name,
        Description = costCenter.Description,
        ParentCostCenterId = costCenter.ParentCostCenterId,
        Status = costCenter.Status,
        StatusDisplay = costCenter.Status.ToArabic()
    };

    public static ExpenseDashboardDto ToDashboardDto(ExpenseDashboardData data, string baseCurrency)
    {
        var total = data.CategoryBreakdown.Sum(c => c.AmountBase);
        var currencyTotal = data.CurrencyBreakdown.Sum(c => c.AmountBase);
        return new ExpenseDashboardDto
        {
            TotalExpensesBase = data.TotalExpensesBase,
            MonthlyExpensesBase = data.MonthlyExpensesBase,
            YearlyExpensesBase = data.YearlyExpensesBase,
            CapitalExpensesBase = data.CapitalExpensesBase,
            PersonalExpensesBase = data.PersonalExpensesBase,
            OperatingExpensesBase = data.OperatingExpensesBase,
            ActiveCount = data.ActiveCount,
            PendingApprovalCount = data.PendingApprovalCount,
            UpcomingPaymentsCount = data.UpcomingPaymentsCount,
            OverdueCount = data.OverdueCount,
            LargestExpenseBase = data.LargestExpenseBase,
            LargestExpenseName = data.LargestExpenseName,
            BurnRateMonthly = data.BurnRateMonthly,
            BaseCurrency = baseCurrency,
            MonthlyTrend = data.MonthlyTrend.Select(m => new ExpenseMonthlyTrendDto
            {
                Label = $"{m.Year}/{m.Month:D2}",
                AmountBase = m.AmountBase
            }).ToList(),
            YearlyTrend = data.YearlyTrend.Select(y => new ExpenseYearlyTrendDto
            {
                Year = y.Year,
                AmountBase = y.AmountBase
            }).ToList(),
            CategoryBreakdown = data.CategoryBreakdown.Select(c => new ExpenseCategoryBreakdownDto
            {
                Label = c.Kind.ToArabic(),
                AmountBase = c.AmountBase,
                Percentage = total > 0 ? Math.Round(c.AmountBase / total * 100, 1) : 0,
                GrowthPercentage = c.PreviousPeriodAmountBase > 0
                    ? Math.Round((c.AmountBase - c.PreviousPeriodAmountBase) / c.PreviousPeriodAmountBase * 100, 1)
                    : 0
            }).ToList(),
            CurrencyBreakdown = data.CurrencyBreakdown.Select(c => new ExpenseCurrencyBreakdownDto
            {
                Currency = c.Currency,
                AmountOriginal = c.AmountOriginal,
                AmountBase = c.AmountBase,
                ExposurePercentage = currencyTotal > 0 ? Math.Round(c.AmountBase / currencyTotal * 100, 1) : 0
            }).ToList(),
            DepartmentBreakdown = data.DepartmentBreakdown.Select(d => new ExpenseDepartmentBreakdownDto
            {
                Department = d.Department,
                AmountBase = d.AmountBase
            }).ToList(),
            CostCenterBreakdown = data.CostCenterBreakdown.Select(c => new ExpenseCostCenterBreakdownDto
            {
                CostCenterId = c.CostCenterId,
                CostCenter = c.CostCenter,
                AmountBase = c.AmountBase
            }).ToList(),
            SupplierBreakdown = data.SupplierBreakdown.Select(s => new ExpenseSupplierBreakdownDto
            {
                SupplierName = s.SupplierName,
                AmountBase = s.AmountBase
            }).ToList(),
            HighestExpenses = data.HighestExpenses.Select(e => new ExpenseTopExpenseDto
            {
                ExpenseId = e.ExpenseId,
                Code = e.Code,
                Name = e.Name,
                AmountBase = e.AmountBase
            }).ToList(),
            FundingSourceBreakdown = data.FundingSourceBreakdown.Select(f => new ExpenseFundingSourceBreakdownDto
            {
                Label = f.FundingSource.ToArabic(),
                AmountBase = f.AmountBase
            }).ToList(),
            UpcomingDuePayments = data.UpcomingDuePayments.Select(ToForecastDto).ToList(),
            OverduePayments = data.OverduePayments.Select(ToForecastDto).ToList()
        };
    }

    public static ExpenseAuditEntryDto ToAuditDto(ExpenseAuditEntry entry) => new()
    {
        Action = entry.Action,
        FieldName = entry.FieldName,
        PreviousValue = entry.PreviousValue,
        NewValue = entry.NewValue,
        UserName = entry.UserName,
        Timestamp = entry.Timestamp,
        Reason = entry.Reason
    };

    public static ExpenseTimelineEventDto ToTimelineDto(ExpenseTimelineEvent entry) => new()
    {
        EventType = entry.EventType,
        Title = entry.Title,
        Description = entry.Description,
        PreviousValue = entry.PreviousValue,
        NewValue = entry.NewValue,
        UserName = entry.UserName,
        Timestamp = entry.Timestamp,
        Reason = entry.Reason
    };

    private static ExpensePaymentForecastDto ToForecastDto(ExpensePaymentForecastPoint p) => new()
    {
        ExpenseId = p.ExpenseId,
        ExpenseCode = p.ExpenseCode,
        ExpenseName = p.ExpenseName,
        DueDate = p.DueDate,
        AmountBase = p.AmountBase,
        IsOverdue = p.IsOverdue
    };
}
