using ERPSystem.Application.Abstractions.Repositories;
using ERPSystem.Domain.Aggregates;
using ERPSystem.Domain.Entities.Expenses;
using ERPSystem.Domain.Enums;
using ERPSystem.Infrastructure.Persistence;
using ERPSystem.Infrastructure.Persistence.Mapping;
using ERPSystem.Infrastructure.Persistence.Models.Expenses;
using Microsoft.EntityFrameworkCore;

namespace ERPSystem.Infrastructure.Repositories;

internal sealed class ExpenseRepository(ErpDbContext context) : IExpenseRepository
{
    public async Task<ExpenseAggregate?> GetByIdAsync(
        Guid id,
        bool includeChildren = false,
        CancellationToken cancellationToken = default)
    {
        var bundle = await GetWithAuditAsync(id, includeChildren, cancellationToken);
        return bundle?.Aggregate;
    }

    public async Task<ExpenseWithAudit?> GetWithAuditAsync(
        Guid id,
        bool includeChildren = false,
        CancellationToken cancellationToken = default)
    {
        IQueryable<ExpenseEntity> query = context.Expenses.AsNoTracking()
            .Include(e => e.CostCenter);
        if (includeChildren)
            query = query
                .Include(e => e.Payments)
                .Include(e => e.Attachments)
                .Include(e => e.Installments);

        var entity = await query.FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
        if (entity is null)
            return null;

        string? createdByName = null;
        if (entity.CreatedByUserId is Guid userId)
        {
            createdByName = await context.Users.AsNoTracking()
                .Where(u => u.Id == userId)
                .Select(u => u.FullNameAr)
                .FirstOrDefaultAsync(cancellationToken);
        }

        return new ExpenseWithAudit(
            ExpenseMapper.ToAggregate(entity),
            entity.CreatedAt,
            createdByName,
            entity.UpdatedAt,
            entity.CostCenter?.Name);
    }

    public async Task<(IReadOnlyList<ExpenseAggregate> Items, int TotalCount)> GetPagedAsync(
        Guid companyId,
        ExpenseListFilter filter,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = context.Expenses.AsNoTracking()
            .Include(e => e.CostCenter)
            .Where(e => e.CompanyId == companyId);

        if (!filter.IncludeArchived)
            query = query.Where(e => !e.IsArchived);

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var term = filter.Search.Trim();
            query = query.Where(e =>
                e.Code.Contains(term) ||
                e.Name.Contains(term) ||
                (e.PayeeName != null && e.PayeeName.Contains(term)) ||
                (e.Department != null && e.Department.Contains(term)) ||
                (e.CostCenter != null && e.CostCenter.Name.Contains(term)));
        }

        if (filter.CategoryKind is ExpenseCategoryKind kind)
            query = query.Where(e => e.CategoryKind == (int)kind);

        if (filter.Status is ExpenseStatus status)
            query = query.Where(e => e.Status == (int)status);

        if (!string.IsNullOrWhiteSpace(filter.Currency))
            query = query.Where(e => e.OriginalCurrency == filter.Currency);

        if (!string.IsNullOrWhiteSpace(filter.Department))
            query = query.Where(e => e.Department == filter.Department);

        if (filter.CostCenterId is Guid costCenterId)
            query = query.Where(e => e.CostCenterId == costCenterId);

        if (filter.FromDate is DateTime from)
            query = query.Where(e => e.StartDate >= UtcDateTimeNormalizer.ToUtc(from));

        if (filter.ToDate is DateTime to)
            query = query.Where(e => e.StartDate <= UtcDateTimeNormalizer.ToUtc(to));

        var total = await query.CountAsync(cancellationToken);
        var entities = await query
            .Include(e => e.Payments)
            .OrderByDescending(e => e.StartDate)
            .ThenByDescending(e => e.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (entities.Select(ExpenseMapper.ToAggregate).ToList(), total);
    }

    public async Task<IReadOnlyList<ExpenseCategory>> GetCategoriesAsync(
        Guid companyId,
        CancellationToken cancellationToken = default)
    {
        var entities = await context.ExpenseCategories.AsNoTracking()
            .Where(c => c.CompanyId == companyId && c.IsActive)
            .OrderBy(c => c.Kind)
            .ThenBy(c => c.Code)
            .ToListAsync(cancellationToken);
        return entities.Select(ExpenseMapper.ToDomain).ToList();
    }

    public async Task AddAsync(ExpenseAggregate aggregate, CancellationToken cancellationToken = default) =>
        await context.Expenses.AddAsync(ExpenseMapper.ToEntity(aggregate), cancellationToken);

    public async Task UpdateAsync(ExpenseAggregate aggregate, CancellationToken cancellationToken = default)
    {
        var entity = await context.Expenses
            .Include(e => e.Payments)
            .Include(e => e.Attachments)
            .Include(e => e.Installments)
            .FirstOrDefaultAsync(e => e.Id == aggregate.Id, cancellationToken)
            ?? throw new InvalidOperationException("Expense not found.");

        ExpenseMapper.UpdateEntity(entity, aggregate);
    }

    public async Task RecordPaymentAsync(
        Guid expenseId,
        ExpensePayment payment,
        ExpenseStatus newStatus,
        CancellationToken cancellationToken = default)
    {
        var entity = await context.Expenses
            .FirstOrDefaultAsync(e => e.Id == expenseId, cancellationToken)
            ?? throw new InvalidOperationException("Expense not found.");

        entity.Status = (int)newStatus;
        entity.UpdatedAt = DateTime.UtcNow;
        await context.ExpensePayments.AddAsync(
            ExpenseMapper.MapPaymentEntity(payment, expenseId),
            cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await context.Expenses.FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
        if (entity is not null)
            context.Expenses.Remove(entity);
    }

    public async Task AddAuditEntryAsync(ExpenseAuditEntry entry, CancellationToken cancellationToken = default) =>
        await context.ExpenseAuditLogs.AddAsync(ExpenseMapper.ToAuditEntity(entry), cancellationToken);

    public async Task<IReadOnlyList<ExpenseAuditEntry>> GetAuditTrailAsync(
        Guid expenseId,
        CancellationToken cancellationToken = default)
    {
        var logs = await context.ExpenseAuditLogs.AsNoTracking()
            .Where(l => l.ExpenseId == expenseId)
            .OrderByDescending(l => l.Timestamp)
            .ToListAsync(cancellationToken);
        return logs.Select(ExpenseMapper.ToAuditDomain).ToList();
    }

    public async Task AddTimelineEventAsync(ExpenseTimelineEvent entry, CancellationToken cancellationToken = default) =>
        await context.ExpenseTimelineEvents.AddAsync(ExpenseMapper.ToTimelineEntity(entry), cancellationToken);

    public async Task<IReadOnlyList<ExpenseTimelineEvent>> GetTimelineAsync(
        Guid expenseId,
        CancellationToken cancellationToken = default)
    {
        var events = await context.ExpenseTimelineEvents.AsNoTracking()
            .Where(e => e.ExpenseId == expenseId)
            .OrderByDescending(e => e.Timestamp)
            .ToListAsync(cancellationToken);
        return events.Select(ExpenseMapper.ToTimelineDomain).ToList();
    }

    public async Task<ExpenseDashboardData> GetDashboardDataAsync(
        Guid companyId,
        CancellationToken cancellationToken = default)
    {
        var today = DateTime.UtcNow.Date;
        var monthStart = new DateTime(today.Year, today.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var yearStart = new DateTime(today.Year, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var upcomingEnd = today.AddDays(30);
        var prevMonthStart = monthStart.AddMonths(-1);

        var expenses = await context.Expenses.AsNoTracking()
            .Include(e => e.CostCenter)
            .Where(e => e.CompanyId == companyId && !e.IsArchived && e.IsActive)
            .ToListAsync(cancellationToken);

        var payments = await context.ExpensePayments.AsNoTracking()
            .Where(p => p.Expense!.CompanyId == companyId && p.IsActive)
            .Include(p => p.Expense)
            .ToListAsync(cancellationToken);

        var total = expenses.Sum(e => e.BaseAmount);
        var monthly = expenses.Where(e => e.StartDate >= monthStart).Sum(e => e.BaseAmount);
        var yearly = expenses.Where(e => e.StartDate >= yearStart).Sum(e => e.BaseAmount);
        var capital = expenses.Where(e => e.CategoryKind == (int)ExpenseCategoryKind.Capital).Sum(e => e.BaseAmount);
        var personal = expenses.Where(e => e.CategoryKind == (int)ExpenseCategoryKind.Personal).Sum(e => e.BaseAmount);
        var operating = expenses.Where(e => e.CategoryKind == (int)ExpenseCategoryKind.Operating).Sum(e => e.BaseAmount);
        var activeCount = expenses.Count(e => e.Status is (int)ExpenseStatus.Approved or (int)ExpenseStatus.Scheduled or (int)ExpenseStatus.PartiallyPaid);
        var pendingApproval = expenses.Count(e => e.Status == (int)ExpenseStatus.PendingApproval);

        var upcomingInstallments = await context.ExpenseInstallments.AsNoTracking()
            .Where(i => i.Expense!.CompanyId == companyId && i.DueDate >= today && i.DueDate <= upcomingEnd
                && i.Status != (int)ExpenseInstallmentStatus.Paid && i.Status != (int)ExpenseInstallmentStatus.Cancelled)
            .CountAsync(cancellationToken);

        var upcomingPayments = payments.Count(p =>
            p.DueDate is not null && p.DueDate >= today && p.DueDate <= upcomingEnd
            && p.Status is (int)ExpensePaymentStatus.Scheduled or (int)ExpensePaymentStatus.Pending);

        var overdueInstallments = await context.ExpenseInstallments.AsNoTracking()
            .Where(i => i.Expense!.CompanyId == companyId && i.DueDate < today
                && i.Status != (int)ExpenseInstallmentStatus.Paid && i.Status != (int)ExpenseInstallmentStatus.Cancelled)
            .CountAsync(cancellationToken);

        var overduePayments = payments.Count(p =>
            p.DueDate is not null && p.DueDate < today
            && p.Status is (int)ExpensePaymentStatus.Scheduled or (int)ExpensePaymentStatus.Pending);

        var largest = expenses.OrderByDescending(e => e.BaseAmount).FirstOrDefault();

        var trendStart = monthStart.AddMonths(-11);
        var monthlyTrend = expenses
            .Where(e => e.StartDate >= trendStart)
            .GroupBy(e => new { e.StartDate.Year, e.StartDate.Month })
            .OrderBy(g => g.Key.Year).ThenBy(g => g.Key.Month)
            .Select(g => new ExpenseMonthlyTrendPoint
            {
                Year = g.Key.Year,
                Month = g.Key.Month,
                AmountBase = g.Sum(x => x.BaseAmount)
            })
            .ToList();

        var yearlyTrend = expenses
            .GroupBy(e => e.StartDate.Year)
            .OrderBy(g => g.Key)
            .Select(g => new ExpenseYearlyTrendPoint
            {
                Year = g.Key,
                AmountBase = g.Sum(x => x.BaseAmount)
            })
            .ToList();

        var categoryBreakdown = expenses
            .GroupBy(e => (ExpenseCategoryKind)e.CategoryKind)
            .Select(g => new ExpenseCategoryBreakdownPoint
            {
                Kind = g.Key,
                AmountBase = g.Sum(x => x.BaseAmount),
                PreviousPeriodAmountBase = g.Where(x => x.StartDate >= prevMonthStart && x.StartDate < monthStart)
                    .Sum(x => x.BaseAmount)
            })
            .ToList();

        var currencyBreakdown = expenses
            .GroupBy(e => e.OriginalCurrency)
            .Select(g => new ExpenseCurrencyBreakdownPoint
            {
                Currency = g.Key,
                AmountOriginal = g.Sum(x => x.OriginalAmount),
                AmountBase = g.Sum(x => x.BaseAmount)
            })
            .ToList();

        var departmentBreakdown = expenses
            .Where(e => !string.IsNullOrWhiteSpace(e.Department))
            .GroupBy(e => e.Department!)
            .OrderByDescending(g => g.Sum(x => x.BaseAmount))
            .Take(8)
            .Select(g => new ExpenseDepartmentBreakdownPoint
            {
                Department = g.Key,
                AmountBase = g.Sum(x => x.BaseAmount)
            })
            .ToList();

        var costCenterBreakdown = expenses
            .Where(e => e.CostCenterId is not null)
            .GroupBy(e => new { e.CostCenterId, Name = e.CostCenter?.Name ?? "—" })
            .OrderByDescending(g => g.Sum(x => x.BaseAmount))
            .Take(8)
            .Select(g => new ExpenseCostCenterBreakdownPoint
            {
                CostCenterId = g.Key.CostCenterId,
                CostCenter = g.Key.Name,
                AmountBase = g.Sum(x => x.BaseAmount)
            })
            .ToList();

        var supplierBreakdown = expenses
            .Where(e => !string.IsNullOrWhiteSpace(e.PayeeName))
            .GroupBy(e => e.PayeeName!)
            .OrderByDescending(g => g.Sum(x => x.BaseAmount))
            .Take(8)
            .Select(g => new ExpenseSupplierBreakdownPoint
            {
                SupplierName = g.Key,
                AmountBase = g.Sum(x => x.BaseAmount)
            })
            .ToList();

        var highestExpenses = expenses
            .OrderByDescending(e => e.BaseAmount)
            .Take(10)
            .Select(e => new ExpenseTopExpensePoint
            {
                ExpenseId = e.Id,
                Code = e.Code,
                Name = e.Name,
                AmountBase = e.BaseAmount
            })
            .ToList();

        var fundingSourceBreakdown = payments
            .Where(p => p.Status == (int)ExpensePaymentStatus.Completed)
            .GroupBy(p => (ExpenseFundingSource)p.FundingSource)
            .Select(g => new ExpenseFundingSourceBreakdownPoint
            {
                FundingSource = g.Key,
                AmountBase = g.Sum(x => x.AmountBase)
            })
            .ToList();

        var forecastPoints = await BuildPaymentForecastPointsAsync(companyId, 60, cancellationToken);

        var last3Months = monthlyTrend.TakeLast(3).ToList();
        var burnRate = last3Months.Count > 0 ? last3Months.Average(m => m.AmountBase) : monthly;

        return new ExpenseDashboardData
        {
            TotalExpensesBase = total,
            MonthlyExpensesBase = monthly,
            YearlyExpensesBase = yearly,
            CapitalExpensesBase = capital,
            PersonalExpensesBase = personal,
            OperatingExpensesBase = operating,
            ActiveCount = activeCount,
            PendingApprovalCount = pendingApproval,
            UpcomingPaymentsCount = upcomingInstallments + upcomingPayments,
            OverdueCount = overdueInstallments + overduePayments,
            LargestExpenseBase = largest?.BaseAmount ?? 0,
            LargestExpenseName = largest?.Name ?? "",
            BurnRateMonthly = burnRate,
            MonthlyTrend = monthlyTrend,
            YearlyTrend = yearlyTrend,
            CategoryBreakdown = categoryBreakdown,
            CurrencyBreakdown = currencyBreakdown,
            DepartmentBreakdown = departmentBreakdown,
            CostCenterBreakdown = costCenterBreakdown,
            SupplierBreakdown = supplierBreakdown,
            HighestExpenses = highestExpenses,
            FundingSourceBreakdown = fundingSourceBreakdown,
            UpcomingDuePayments = forecastPoints.Where(p => !p.IsOverdue).Take(15).ToList(),
            OverduePayments = forecastPoints.Where(p => p.IsOverdue).Take(15).ToList()
        };
    }

    public async Task<IReadOnlyList<ExpensePaymentForecastPoint>> GetPaymentForecastAsync(
        Guid companyId,
        int daysAhead,
        CancellationToken cancellationToken = default) =>
        await BuildPaymentForecastPointsAsync(companyId, daysAhead, cancellationToken);

    private async Task<IReadOnlyList<ExpensePaymentForecastPoint>> BuildPaymentForecastPointsAsync(
        Guid companyId,
        int daysAhead,
        CancellationToken cancellationToken)
    {
        var today = DateTime.UtcNow.Date;
        var end = today.AddDays(daysAhead);

        var installmentPoints = await context.ExpenseInstallments.AsNoTracking()
            .Where(i => i.Expense!.CompanyId == companyId
                && i.Status != (int)ExpenseInstallmentStatus.Paid
                && i.Status != (int)ExpenseInstallmentStatus.Cancelled
                && i.DueDate <= end)
            .Select(i => new ExpensePaymentForecastPoint
            {
                ExpenseId = i.ExpenseId,
                ExpenseCode = i.Expense!.Code,
                ExpenseName = i.Expense.Name,
                DueDate = i.DueDate,
                AmountBase = i.AmountBase,
                IsOverdue = i.DueDate < today
            })
            .ToListAsync(cancellationToken);

        var paymentPoints = await context.ExpensePayments.AsNoTracking()
            .Where(p => p.Expense!.CompanyId == companyId
                && p.DueDate != null
                && p.DueDate <= end
                && (p.Status == (int)ExpensePaymentStatus.Scheduled || p.Status == (int)ExpensePaymentStatus.Pending))
            .Select(p => new ExpensePaymentForecastPoint
            {
                ExpenseId = p.ExpenseId,
                ExpenseCode = p.Expense!.Code,
                ExpenseName = p.Expense.Name,
                DueDate = p.DueDate!.Value,
                AmountBase = p.AmountBase,
                IsOverdue = p.DueDate < today
            })
            .ToListAsync(cancellationToken);

        return installmentPoints
            .Concat(paymentPoints)
            .OrderBy(p => p.DueDate)
            .ToList();
    }

    public async Task<(IReadOnlyList<ExpenseEntryRow> Items, int TotalCount)> GetEntriesPagedAsync(
        Guid companyId,
        ExpenseEntryListFilter filter,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = context.ExpensePayments.AsNoTracking()
            .Include(p => p.Expense)
            .Where(p => p.Expense!.CompanyId == companyId
                && p.Status == (int)ExpensePaymentStatus.Completed);

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var term = filter.Search.Trim();
            query = query.Where(p =>
                (p.Notes != null && p.Notes.Contains(term)) ||
                p.Expense!.Name.Contains(term) ||
                p.Expense.Code.Contains(term));
        }

        if (filter.ExpenseId is Guid expenseId)
            query = query.Where(p => p.ExpenseId == expenseId);

        if (filter.CashboxId is Guid cashboxId)
            query = query.Where(p => p.CashboxId == cashboxId);

        if (filter.FromDate is DateTime from)
            query = query.Where(p => p.PaymentDate >= UtcDateTimeNormalizer.ToUtc(from));

        if (filter.ToDate is DateTime to)
            query = query.Where(p => p.PaymentDate <= UtcDateTimeNormalizer.ToUtc(to));

        var total = await query.CountAsync(cancellationToken);

        var cashboxMap = await context.Cashboxes.AsNoTracking()
            .ToDictionaryAsync(c => c.Id, c => c.Name, cancellationToken);

        var pageItems = await query
            .OrderByDescending(p => p.PaymentDate)
            .ThenByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var rows = pageItems.Select(p => new ExpenseEntryRow
        {
            Id = p.Id,
            ExpenseId = p.ExpenseId,
            ExpenseCode = p.Expense!.Code,
            ExpenseName = p.Expense.Name,
            PaymentDate = p.PaymentDate,
            AmountOriginal = p.AmountOriginal,
            AmountBase = p.AmountBase,
            Currency = p.Currency,
            Description = p.Notes,
            CashboxId = p.CashboxId,
            CashboxName = p.CashboxId is Guid cbId ? cashboxMap.GetValueOrDefault(cbId) : null
        }).ToList();

        return (rows, total);
    }
}
