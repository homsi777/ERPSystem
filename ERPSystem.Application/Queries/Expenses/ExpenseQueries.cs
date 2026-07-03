using ERPSystem.Application.Abstractions.Repositories;
using ERPSystem.Domain.Enums;

namespace ERPSystem.Application.Queries.Expenses;

public sealed class GetExpenseListQuery
{
    public Guid CompanyId { get; init; }
    public ExpenseListFilter Filter { get; init; } = new();
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 50;
}

public sealed class GetExpenseDetailsQuery
{
    public Guid ExpenseId { get; init; }
}

public sealed class GetExpenseOperationsCenterQuery
{
    public Guid ExpenseId { get; init; }
}

public sealed class GetExpenseDashboardQuery
{
    public Guid CompanyId { get; init; }
}

public sealed class GetExpenseCategoriesQuery
{
    public Guid CompanyId { get; init; }
}

public sealed class GetExpenseAuditTrailQuery
{
    public Guid ExpenseId { get; init; }
}

public sealed class GetExpenseTimelineQuery
{
    public Guid ExpenseId { get; init; }
}

public sealed class GetCostCentersQuery
{
    public Guid CompanyId { get; init; }
}

public sealed class GetExpenseReportQuery
{
    public Guid CompanyId { get; init; }
    public string ReportType { get; init; } = "Summary";
    public DateTime? FromDate { get; init; }
    public DateTime? ToDate { get; init; }
    public ExpenseCategoryKind? CategoryKind { get; init; }
    public Guid? CostCenterId { get; init; }
}

public sealed class GetExpensePaymentForecastQuery
{
    public Guid CompanyId { get; init; }
    public int DaysAhead { get; init; } = 30;
}

public sealed class GetExpenseEntriesQuery
{
    public Guid CompanyId { get; init; }
    public ExpenseEntryListFilter Filter { get; init; } = new();
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 100;
}
