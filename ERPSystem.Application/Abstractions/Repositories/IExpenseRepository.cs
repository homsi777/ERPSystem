using ERPSystem.Domain.Aggregates;
using ERPSystem.Domain.Entities.Expenses;
using ERPSystem.Domain.Enums;

namespace ERPSystem.Application.Abstractions.Repositories;

public sealed class ExpenseListFilter
{
    public string? Search { get; init; }
    public ExpenseCategoryKind? CategoryKind { get; init; }
    public ExpenseStatus? Status { get; init; }
    public string? Currency { get; init; }
    public string? Department { get; init; }
    public Guid? CostCenterId { get; init; }
    public DateTime? FromDate { get; init; }
    public DateTime? ToDate { get; init; }
    public bool IncludeArchived { get; init; }
}

public sealed class ExpenseEntryListFilter
{
    public string? Search { get; init; }
    public Guid? ExpenseId { get; init; }
    public Guid? CashboxId { get; init; }
    public DateTime? FromDate { get; init; }
    public DateTime? ToDate { get; init; }
}

public sealed record ExpenseWithAudit(
    ExpenseAggregate Aggregate,
    DateTime CreatedAt,
    string? CreatedByName,
    DateTime? UpdatedAt,
    string? CostCenterName = null);

public interface IExpenseRepository
{
    Task<ExpenseAggregate?> GetByIdAsync(Guid id, bool includeChildren = false, CancellationToken cancellationToken = default);
    Task<ExpenseWithAudit?> GetWithAuditAsync(Guid id, bool includeChildren = false, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<ExpenseAggregate> Items, int TotalCount)> GetPagedAsync(
        Guid companyId,
        ExpenseListFilter filter,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ExpenseCategory>> GetCategoriesAsync(Guid companyId, CancellationToken cancellationToken = default);
    Task AddAsync(ExpenseAggregate aggregate, CancellationToken cancellationToken = default);
    Task UpdateAsync(ExpenseAggregate aggregate, CancellationToken cancellationToken = default);
    Task RecordPaymentAsync(
        Guid expenseId,
        ExpensePayment payment,
        ExpenseStatus newStatus,
        CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddAuditEntryAsync(ExpenseAuditEntry entry, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ExpenseAuditEntry>> GetAuditTrailAsync(Guid expenseId, CancellationToken cancellationToken = default);
    Task AddTimelineEventAsync(ExpenseTimelineEvent entry, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ExpenseTimelineEvent>> GetTimelineAsync(Guid expenseId, CancellationToken cancellationToken = default);
    Task<ExpenseDashboardData> GetDashboardDataAsync(Guid companyId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ExpensePaymentForecastPoint>> GetPaymentForecastAsync(Guid companyId, int daysAhead, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<ExpenseEntryRow> Items, int TotalCount)> GetEntriesPagedAsync(
        Guid companyId,
        ExpenseEntryListFilter filter,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
}

public sealed class ExpenseEntryRow
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

public sealed class ExpenseDashboardData
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
    public IReadOnlyList<ExpenseMonthlyTrendPoint> MonthlyTrend { get; init; } = [];
    public IReadOnlyList<ExpenseYearlyTrendPoint> YearlyTrend { get; init; } = [];
    public IReadOnlyList<ExpenseCategoryBreakdownPoint> CategoryBreakdown { get; init; } = [];
    public IReadOnlyList<ExpenseCurrencyBreakdownPoint> CurrencyBreakdown { get; init; } = [];
    public IReadOnlyList<ExpenseDepartmentBreakdownPoint> DepartmentBreakdown { get; init; } = [];
    public IReadOnlyList<ExpenseCostCenterBreakdownPoint> CostCenterBreakdown { get; init; } = [];
    public IReadOnlyList<ExpenseSupplierBreakdownPoint> SupplierBreakdown { get; init; } = [];
    public IReadOnlyList<ExpenseTopExpensePoint> HighestExpenses { get; init; } = [];
    public IReadOnlyList<ExpenseFundingSourceBreakdownPoint> FundingSourceBreakdown { get; init; } = [];
    public IReadOnlyList<ExpensePaymentForecastPoint> UpcomingDuePayments { get; init; } = [];
    public IReadOnlyList<ExpensePaymentForecastPoint> OverduePayments { get; init; } = [];
}

public sealed class ExpenseMonthlyTrendPoint
{
    public int Year { get; init; }
    public int Month { get; init; }
    public decimal AmountBase { get; init; }
}

public sealed class ExpenseYearlyTrendPoint
{
    public int Year { get; init; }
    public decimal AmountBase { get; init; }
}

public sealed class ExpenseCategoryBreakdownPoint
{
    public ExpenseCategoryKind Kind { get; init; }
    public decimal AmountBase { get; init; }
    public decimal PreviousPeriodAmountBase { get; init; }
}

public sealed class ExpenseCurrencyBreakdownPoint
{
    public string Currency { get; init; } = "";
    public decimal AmountOriginal { get; init; }
    public decimal AmountBase { get; init; }
}

public sealed class ExpenseDepartmentBreakdownPoint
{
    public string Department { get; init; } = "";
    public decimal AmountBase { get; init; }
}

public sealed class ExpenseCostCenterBreakdownPoint
{
    public Guid? CostCenterId { get; init; }
    public string CostCenter { get; init; } = "";
    public decimal AmountBase { get; init; }
}

public sealed class ExpenseSupplierBreakdownPoint
{
    public string SupplierName { get; init; } = "";
    public decimal AmountBase { get; init; }
}

public sealed class ExpenseTopExpensePoint
{
    public Guid ExpenseId { get; init; }
    public string Code { get; init; } = "";
    public string Name { get; init; } = "";
    public decimal AmountBase { get; init; }
}

public sealed class ExpenseFundingSourceBreakdownPoint
{
    public ExpenseFundingSource FundingSource { get; init; }
    public decimal AmountBase { get; init; }
}

public sealed class ExpensePaymentForecastPoint
{
    public Guid ExpenseId { get; init; }
    public string ExpenseCode { get; init; } = "";
    public string ExpenseName { get; init; } = "";
    public DateTime DueDate { get; init; }
    public decimal AmountBase { get; init; }
    public bool IsOverdue { get; init; }
}
