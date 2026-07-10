using ERPSystem.Domain.Enums;

namespace ERPSystem.Application.DTOs.Expenses;

public sealed class ExpenseCategoryDto
{
    public Guid Id { get; init; }
    public ExpenseCategoryKind Kind { get; init; }
    public string Code { get; init; } = "";
    public string NameAr { get; init; } = "";
    public string NameEn { get; init; } = "";
    public string KindDisplay { get; init; } = "";
}

public sealed class CostCenterDto
{
    public Guid Id { get; init; }
    public string Code { get; init; } = "";
    public string Name { get; init; } = "";
    public string? Description { get; init; }
    public Guid? ParentCostCenterId { get; init; }
    public CostCenterStatus Status { get; init; }
    public string StatusDisplay { get; init; } = "";
}

public sealed class ExpenseListDto
{
    public Guid Id { get; init; }
    public string Code { get; init; } = "";
    public string Name { get; init; } = "";
    public ExpenseCategoryKind CategoryKind { get; init; }
    public string CategoryKindDisplay { get; init; } = "";
    public string CategoryName { get; init; } = "";
    public ExpenseStatus Status { get; init; }
    public string StatusDisplay { get; init; } = "";
    public DateTime StartDate { get; init; }
    public DateTime? EndDate { get; init; }
    public string OriginalCurrency { get; init; } = "";
    public decimal OriginalAmount { get; init; }
    public decimal BaseAmount { get; init; }
    public decimal PaidAmountBase { get; init; }
    public decimal RemainingBalanceBase { get; init; }
    public string BaseCurrency { get; init; } = "";
    public string? Department { get; init; }
    public Guid? CostCenterId { get; init; }
    public string? CostCenterName { get; init; }
    public string? PayeeName { get; init; }
    public bool IsRecurring { get; init; }
    public DateTime? NextDueDate { get; init; }
    public bool IsArchived { get; init; }
}

public sealed class ExpenseEntryListDto
{
    public Guid Id { get; init; }
    public Guid ExpenseId { get; init; }
    public string ExpenseCode { get; init; } = "";
    public string ExpenseName { get; init; } = "";
    public DateTime PaymentDate { get; init; }
    public decimal AmountOriginal { get; init; }
    public decimal AmountBase { get; init; }
    public string Currency { get; init; } = "USD";
    public string? Description { get; init; }
    public Guid? CashboxId { get; init; }
    public string? CashboxName { get; init; }
}

public sealed class CashboxOptionDto
{
    public Guid Id { get; init; }
    public string Code { get; init; } = "";
    public string Name { get; init; } = "";
    public string Display => Name;
}

public sealed class ExpenseDetailsDto
{
    public Guid Id { get; init; }
    public string Code { get; init; } = "";
    public string Name { get; init; } = "";
    public Guid CategoryId { get; init; }
    public ExpenseCategoryKind CategoryKind { get; init; }
    public string CategoryKindDisplay { get; init; } = "";
    public string CategoryName { get; init; } = "";
    public string? Description { get; init; }
    public ExpenseStatus Status { get; init; }
    public string StatusDisplay { get; init; } = "";
    public IReadOnlyList<ExpenseStatus> AllowedTransitions { get; init; } = [];
    public DateTime StartDate { get; init; }
    public DateTime? EndDate { get; init; }
    public string OriginalCurrency { get; init; } = "";
    public decimal OriginalAmount { get; init; }
    public decimal ExchangeRate { get; init; }
    public string BaseCurrency { get; init; } = "";
    public decimal BaseAmount { get; init; }
    public decimal PaidAmountBase { get; init; }
    public decimal RemainingBalanceBase { get; init; }
    public ExpensePaymentMethod PaymentMethod { get; init; }
    public string PaymentMethodDisplay { get; init; } = "";
    public string? PayeeName { get; init; }
    public Guid? SupplierId { get; init; }
    public Guid? CostCenterId { get; init; }
    public string? CostCenterName { get; init; }
    public string? Department { get; init; }
    public string? ProjectCode { get; init; }
    public string? Notes { get; init; }
    public bool IsRecurring { get; init; }
    public ExpenseRecurrenceFrequency RecurrenceFrequency { get; init; }
    public string RecurrenceDisplay { get; init; } = "";
    public int? CustomIntervalDays { get; init; }
    public DateTime? NextDueDate { get; init; }
    public int? RemainingInstallments { get; init; }
    public bool IsArchived { get; init; }
    public DateTime CreatedAt { get; init; }
    public string? CreatedByName { get; init; }
    public DateTime? UpdatedAt { get; init; }
    public IReadOnlyList<ExpensePaymentDto> Payments { get; init; } = [];
    public IReadOnlyList<ExpenseInstallmentDto> Installments { get; init; } = [];
    public IReadOnlyList<ExpenseAttachmentDto> Attachments { get; init; } = [];
}

public sealed class ExpenseOperationsCenterDto
{
    public ExpenseDetailsDto Details { get; init; } = null!;
    public ExpenseFinancialSummaryDto Financial { get; init; } = null!;
    public IReadOnlyList<(string Label, bool Completed, bool Current)> LifecycleSteps { get; init; } = [];
    public IReadOnlyList<ExpenseTimelineEventDto> Timeline { get; init; } = [];
    public IReadOnlyList<ExpenseAuditEntryDto> RecentAudit { get; init; } = [];
    public ExpenseStatisticsDto Statistics { get; init; } = null!;
}

public sealed class ExpenseFinancialSummaryDto
{
    public decimal OriginalAmount { get; init; }
    public string OriginalCurrency { get; init; } = "";
    public decimal BaseAmount { get; init; }
    public string BaseCurrency { get; init; } = "";
    public decimal PaidAmountBase { get; init; }
    public decimal RemainingBalanceBase { get; init; }
    public decimal ExchangeRate { get; init; }
    public int CompletedPayments { get; init; }
    public int ScheduledPayments { get; init; }
    public int PendingInstallments { get; init; }
    public DateTime? NextPaymentDue { get; init; }
}

public sealed class ExpenseStatisticsDto
{
    public int TotalPayments { get; init; }
    public int TotalAttachments { get; init; }
    public int DaysSinceCreated { get; init; }
    public int AuditEventCount { get; init; }
}

public sealed class ExpensePaymentDto
{
    public Guid Id { get; init; }
    public DateTime PaymentDate { get; init; }
    public DateTime? DueDate { get; init; }
    public decimal AmountOriginal { get; init; }
    public decimal AmountBase { get; init; }
    public string Currency { get; init; } = "";
    public decimal ExchangeRateSnapshot { get; init; }
    public string PaymentMethodDisplay { get; init; } = "";
    public string FundingSourceDisplay { get; init; } = "";
    public string StatusDisplay { get; init; } = "";
    public string ApprovalStatusDisplay { get; init; } = "";
    public string? ReferenceNumber { get; init; }
    public string? Notes { get; init; }
    public int? InstallmentNumber { get; init; }
    public Guid? AttachmentId { get; init; }
}

public sealed class ExpenseInstallmentDto
{
    public Guid Id { get; init; }
    public int InstallmentNumber { get; init; }
    public DateTime DueDate { get; init; }
    public decimal AmountOriginal { get; init; }
    public decimal AmountBase { get; init; }
    public string Currency { get; init; } = "";
    public string StatusDisplay { get; init; } = "";
    public Guid? PaymentId { get; init; }
}

public sealed class ExpenseAttachmentDto
{
    public Guid Id { get; init; }
    public string FileName { get; init; } = "";
    public string ContentType { get; init; } = "";
    public long SizeBytes { get; init; }
}

public sealed class ExpenseAuditEntryDto
{
    public string Action { get; init; } = "";
    public string? FieldName { get; init; }
    public string? PreviousValue { get; init; }
    public string? NewValue { get; init; }
    public string UserName { get; init; } = "";
    public DateTime Timestamp { get; init; }
    public string? Reason { get; init; }
}

public sealed class ExpenseTimelineEventDto
{
    public string EventType { get; init; } = "";
    public string Title { get; init; } = "";
    public string? Description { get; init; }
    public string? PreviousValue { get; init; }
    public string? NewValue { get; init; }
    public string UserName { get; init; } = "";
    public DateTime Timestamp { get; init; }
    public string? Reason { get; init; }
}

public sealed class ExpenseDashboardDto
{
    public decimal TotalExpensesBase { get; init; }
    public decimal MonthlyExpensesBase { get; init; }
    public decimal YearlyExpensesBase { get; init; }
    public decimal CapitalExpensesBase { get; init; }
    public decimal PersonalExpensesBase { get; init; }
    public decimal OperatingExpensesBase { get; init; }
    public int ActiveCount { get; init; }
    public int PendingApprovalCount { get; init; }
    public int UpcomingPaymentsCount { get; init; }
    public int OverdueCount { get; init; }
    public decimal LargestExpenseBase { get; init; }
    public string LargestExpenseName { get; init; } = "";
    public decimal BurnRateMonthly { get; init; }
    public string BaseCurrency { get; init; } = "USD";
    public IReadOnlyList<ExpenseMonthlyTrendDto> MonthlyTrend { get; init; } = [];
    public IReadOnlyList<ExpenseYearlyTrendDto> YearlyTrend { get; init; } = [];
    public IReadOnlyList<ExpenseCategoryBreakdownDto> CategoryBreakdown { get; init; } = [];
    public IReadOnlyList<ExpenseCurrencyBreakdownDto> CurrencyBreakdown { get; init; } = [];
    public IReadOnlyList<ExpenseDepartmentBreakdownDto> DepartmentBreakdown { get; init; } = [];
    public IReadOnlyList<ExpenseCostCenterBreakdownDto> CostCenterBreakdown { get; init; } = [];
    public IReadOnlyList<ExpenseSupplierBreakdownDto> SupplierBreakdown { get; init; } = [];
    public IReadOnlyList<ExpenseTopExpenseDto> HighestExpenses { get; init; } = [];
    public IReadOnlyList<ExpenseFundingSourceBreakdownDto> FundingSourceBreakdown { get; init; } = [];
    public IReadOnlyList<ExpensePaymentForecastDto> UpcomingDuePayments { get; init; } = [];
    public IReadOnlyList<ExpensePaymentForecastDto> OverduePayments { get; init; } = [];
}

public sealed class ExpenseMonthlyTrendDto
{
    public string Label { get; init; } = "";
    public decimal AmountBase { get; init; }
}

public sealed class ExpenseYearlyTrendDto
{
    public int Year { get; init; }
    public decimal AmountBase { get; init; }
}

public sealed class ExpenseCategoryBreakdownDto
{
    public string Label { get; init; } = "";
    public decimal AmountBase { get; init; }
    public decimal Percentage { get; init; }
    public decimal GrowthPercentage { get; init; }
}

public sealed class ExpenseCurrencyBreakdownDto
{
    public string Currency { get; init; } = "";
    public decimal AmountOriginal { get; init; }
    public decimal AmountBase { get; init; }
    public decimal ExposurePercentage { get; init; }
}

public sealed class ExpenseDepartmentBreakdownDto
{
    public string Department { get; init; } = "";
    public decimal AmountBase { get; init; }
}

public sealed class ExpenseCostCenterBreakdownDto
{
    public Guid? CostCenterId { get; init; }
    public string CostCenter { get; init; } = "";
    public decimal AmountBase { get; init; }
}

public sealed class ExpenseSupplierBreakdownDto
{
    public string SupplierName { get; init; } = "";
    public decimal AmountBase { get; init; }
}

public sealed class ExpenseTopExpenseDto
{
    public Guid ExpenseId { get; init; }
    public string Code { get; init; } = "";
    public string Name { get; init; } = "";
    public decimal AmountBase { get; init; }
}

public sealed class ExpenseFundingSourceBreakdownDto
{
    public string Label { get; init; } = "";
    public decimal AmountBase { get; init; }
}

public sealed class ExpensePaymentForecastDto
{
    public Guid ExpenseId { get; init; }
    public string ExpenseCode { get; init; } = "";
    public string ExpenseName { get; init; } = "";
    public DateTime DueDate { get; init; }
    public decimal AmountBase { get; init; }
    public bool IsOverdue { get; init; }
}

public sealed class ExpenseReportDto
{
    public string Title { get; init; } = "";
    public string ReportType { get; init; } = "";
    public DateTime GeneratedAt { get; init; }
    public IReadOnlyList<ExpenseReportRowDto> Rows { get; init; } = [];
    public decimal TotalBase { get; init; }
    public decimal TotalPaidBase { get; init; }
    public decimal TotalRemainingBase { get; init; }
    public int ExpenseCount { get; init; }
    public string BaseCurrency { get; init; } = "USD";
    public DateTime? FromDate { get; init; }
    public DateTime? ToDate { get; init; }
    public string? ScopeLabel { get; init; }
}

public sealed class ExpenseReportRowDto
{
    public Guid ExpenseId { get; init; }
    public string Code { get; init; } = "";
    public string Name { get; init; } = "";
    public string Category { get; init; } = "";
    public string CategoryKindDisplay { get; init; } = "";
    public string Status { get; init; } = "";
    public DateTime StartDate { get; init; }
    public DateTime? EndDate { get; init; }
    public decimal OriginalAmount { get; init; }
    public string Currency { get; init; } = "";
    public decimal ExchangeRate { get; init; }
    public decimal BaseAmount { get; init; }
    public decimal PaidAmountBase { get; init; }
    public decimal RemainingBalanceBase { get; init; }
    public string? Department { get; init; }
    public string? CostCenter { get; init; }
    public string? PayeeName { get; init; }
    public string? FundingSource { get; init; }
    public string PaymentMethod { get; init; } = "";
    public string? Description { get; init; }
    public string? Notes { get; init; }
    public bool IsRecurring { get; init; }
    public DateTime? NextDueDate { get; init; }
    public int PaymentCount { get; init; }
}
