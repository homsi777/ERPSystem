using ERPSystem.Application.Abstractions;
using ERPSystem.Application.Abstractions.Repositories;
using ERPSystem.Application.Abstractions.Services;
using ERPSystem.Application.Common;
using ERPSystem.Application.Commands.Expenses;
using ERPSystem.Application.DTOs.Expenses;
using ERPSystem.Application.Queries.Expenses;
using ERPSystem.Application.Results;
using ERPSystem.Application.UseCases.Expenses;
using ERPSystem.Domain.Enums;
using Microsoft.Extensions.DependencyInjection;

namespace ERPSystem.Services.Expenses;

public static class ExpenseNavigationContext
{
    public static Guid? EditExpenseId { get; set; }
    public static Guid? WorkspaceExpenseId { get; set; }
    public static string? WorkspaceInitialTab { get; set; }
    public static Guid? PreselectedExpenseId { get; set; }
    public static Guid? EntriesFilterExpenseId { get; set; }

    public static void BeginCreate() => EditExpenseId = null;

    public static void BeginEdit(Guid expenseId) => EditExpenseId = expenseId;

    public static void BeginEntryFor(Guid expenseId) => PreselectedExpenseId = expenseId;

    public static void BeginEntriesFor(Guid expenseId) => EntriesFilterExpenseId = expenseId;

    public static void ClearPreselection() => PreselectedExpenseId = null;

    public static Guid? TakeEntriesFilter()
    {
        var id = EntriesFilterExpenseId;
        EntriesFilterExpenseId = null;
        return id;
    }

    public static void BeginWorkspace(Guid expenseId, string? tab = null)
    {
        WorkspaceExpenseId = expenseId;
        WorkspaceInitialTab = tab;
    }

    public static (Guid? Id, string? Tab) TakeWorkspaceContext()
    {
        var id = WorkspaceExpenseId;
        var tab = WorkspaceInitialTab;
        WorkspaceExpenseId = null;
        WorkspaceInitialTab = null;
        return (id, tab);
    }
}

public static class ExpenseListRefreshHub
{
    public static event EventHandler? RefreshRequested;
    public static void RequestRefresh() => RefreshRequested?.Invoke(null, EventArgs.Empty);
}

public sealed class ExpenseUiService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ICurrentBranchService _branch;

    public ExpenseUiService(IServiceScopeFactory scopeFactory, ICurrentBranchService branch)
    {
        _scopeFactory = scopeFactory;
        _branch = branch;
    }

    public static ExpenseUiService Instance => AppServices.GetRequiredService<ExpenseUiService>();

    private Guid CompanyId =>
        _branch.CompanyId ?? throw new InvalidOperationException("Company context is not set.");

    private Guid BranchId =>
        _branch.BranchId ?? throw new InvalidOperationException("Branch context is not set.");

    public async Task<bool> CanAsync(string permission, CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var permissions = scope.ServiceProvider.GetRequiredService<IPermissionService>();
        return await permissions.CanAsync(permission, cancellationToken);
    }

    public Task<bool> CanViewAsync(CancellationToken cancellationToken = default) =>
        CanAsync("expenses.view", cancellationToken);

    public Task<bool> CanCreateAsync(CancellationToken cancellationToken = default) =>
        CanAsync("expenses.create", cancellationToken);

    public Task<bool> CanEditAsync(CancellationToken cancellationToken = default) =>
        CanAsync("expenses.edit", cancellationToken);

    public Task<bool> CanApproveAsync(CancellationToken cancellationToken = default) =>
        CanAsync("expenses.approve", cancellationToken);

    public Task<bool> CanDeleteAsync(CancellationToken cancellationToken = default) =>
        CanAsync("expenses.delete", cancellationToken);

    public Task<bool> CanArchiveAsync(CancellationToken cancellationToken = default) =>
        CanAsync("expenses.archive", cancellationToken);

    public async Task<ApplicationResult<PagedResult<ExpenseListDto>>> GetListAsync(
        ExpenseListFilter filter,
        int page = 1,
        int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetExpenseListHandler>();
        return await handler.HandleAsync(new GetExpenseListQuery
        {
            CompanyId = CompanyId,
            Filter = filter,
            Page = page,
            PageSize = pageSize
        }, cancellationToken);
    }

    public Task<ApplicationResult<PagedResult<ExpenseListDto>>> GetDefinitionsAsync(
        string? search = null,
        CancellationToken cancellationToken = default) =>
        GetListAsync(new ExpenseListFilter { Search = search }, 1, 500, cancellationToken);

    public async Task<ApplicationResult<PagedResult<ExpenseEntryListDto>>> GetEntriesAsync(
        ExpenseEntryListFilter filter,
        int page = 1,
        int pageSize = 100,
        CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetExpenseEntriesHandler>();
        return await handler.HandleAsync(new GetExpenseEntriesQuery
        {
            CompanyId = CompanyId,
            Filter = filter,
            Page = page,
            PageSize = pageSize
        }, cancellationToken);
    }

    public async Task<ApplicationResult<IReadOnlyList<CashboxOptionDto>>> GetCashboxesAsync(
        CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<ICashboxRepository>();
        var boxes = await repo.GetListAsync(BranchId, cancellationToken);
        var dtos = boxes
            .Where(b => b.IsActive)
            .Select(b => new CashboxOptionDto { Id = b.Id, Code = b.Code, Name = b.Name })
            .ToList();
        return ApplicationResult<IReadOnlyList<CashboxOptionDto>>.Success(dtos);
    }

    public Task<ApplicationResult<Guid>> CreateDefinitionAsync(
        CreateExpenseCommand command,
        CancellationToken cancellationToken = default)
    {
        command.CompanyId = CompanyId;
        command.BranchId = BranchId;
        return CreateAsync(command, cancellationToken);
    }

    public Task<ApplicationResult> UpdateDefinitionAsync(
        UpdateExpenseCommand command,
        CancellationToken cancellationToken = default) =>
        UpdateAsync(command, cancellationToken);

    public async Task<ApplicationResult<ExpenseDetailsDto>> GetDetailsAsync(
        Guid expenseId,
        CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetExpenseDetailsHandler>();
        return await handler.HandleAsync(new GetExpenseDetailsQuery { ExpenseId = expenseId }, cancellationToken);
    }

    public async Task<ApplicationResult<ExpenseOperationsCenterDto>> GetOperationsCenterAsync(
        Guid expenseId,
        CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetExpenseOperationsCenterHandler>();
        return await handler.HandleAsync(new GetExpenseOperationsCenterQuery { ExpenseId = expenseId }, cancellationToken);
    }

    public async Task<ApplicationResult<ExpenseDashboardDto>> GetDashboardAsync(
        CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetExpenseDashboardHandler>();
        return await handler.HandleAsync(new GetExpenseDashboardQuery { CompanyId = CompanyId }, cancellationToken);
    }

    public async Task<ApplicationResult<IReadOnlyList<ExpenseCategoryDto>>> GetCategoriesAsync(
        CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetExpenseCategoriesHandler>();
        return await handler.HandleAsync(new GetExpenseCategoriesQuery { CompanyId = CompanyId }, cancellationToken);
    }

    public async Task<ApplicationResult<IReadOnlyList<CostCenterDto>>> GetCostCentersAsync(
        CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetCostCentersHandler>();
        return await handler.HandleAsync(new GetCostCentersQuery { CompanyId = CompanyId }, cancellationToken);
    }

    public async Task<ApplicationResult<IReadOnlyList<ExpenseAuditEntryDto>>> GetAuditTrailAsync(
        Guid expenseId,
        CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetExpenseAuditTrailHandler>();
        return await handler.HandleAsync(new GetExpenseAuditTrailQuery { ExpenseId = expenseId }, cancellationToken);
    }

    public async Task<ApplicationResult<IReadOnlyList<ExpenseTimelineEventDto>>> GetTimelineAsync(
        Guid expenseId,
        CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetExpenseTimelineHandler>();
        return await handler.HandleAsync(new GetExpenseTimelineQuery { ExpenseId = expenseId }, cancellationToken);
    }

    public async Task<ApplicationResult<ExpenseReportDto>> GetReportAsync(
        string reportType,
        DateTime? from,
        DateTime? to,
        ExpenseCategoryKind? kind = null,
        Guid? costCenterId = null,
        CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetExpenseReportHandler>();
        return await handler.HandleAsync(new GetExpenseReportQuery
        {
            CompanyId = CompanyId,
            ReportType = reportType,
            FromDate = ApplicationDateNormalizer.ToUtcDate(from),
            ToDate = ApplicationDateNormalizer.ToUtcDate(to),
            CategoryKind = kind,
            CostCenterId = costCenterId
        }, cancellationToken);
    }

    public async Task<ApplicationResult<Guid>> CreateAsync(CreateExpenseCommand command, CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<ICommandHandler<CreateExpenseCommand, ApplicationResult<Guid>>>();
        command.CompanyId = CompanyId;
        command.BranchId = BranchId;
        return await handler.HandleAsync(command, cancellationToken);
    }

    public async Task<ApplicationResult> UpdateAsync(UpdateExpenseCommand command, CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<ICommandHandler<UpdateExpenseCommand, ApplicationResult>>();
        return await handler.HandleAsync(command, cancellationToken);
    }

    public async Task<ApplicationResult> ApproveAsync(Guid expenseId, string? reason = null, CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<ICommandHandler<ApproveExpenseCommand, ApplicationResult>>();
        return await handler.HandleAsync(new ApproveExpenseCommand { ExpenseId = expenseId, Reason = reason }, cancellationToken);
    }

    public async Task<ApplicationResult> RejectAsync(Guid expenseId, string? reason = null, CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<ICommandHandler<RejectExpenseCommand, ApplicationResult>>();
        return await handler.HandleAsync(new RejectExpenseCommand { ExpenseId = expenseId, Reason = reason }, cancellationToken);
    }

    public async Task<ApplicationResult> ScheduleAsync(Guid expenseId, IReadOnlyList<ExpenseInstallmentInput> installments, CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<ICommandHandler<ScheduleExpenseCommand, ApplicationResult>>();
        return await handler.HandleAsync(new ScheduleExpenseCommand { ExpenseId = expenseId, Installments = installments }, cancellationToken);
    }

    public async Task<ApplicationResult> CloseAsync(Guid expenseId, CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<ICommandHandler<CloseExpenseCommand, ApplicationResult>>();
        return await handler.HandleAsync(new CloseExpenseCommand { ExpenseId = expenseId }, cancellationToken);
    }

    public async Task<ApplicationResult> CancelAsync(Guid expenseId, string? reason = null, CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<ICommandHandler<CancelExpenseCommand, ApplicationResult>>();
        return await handler.HandleAsync(new CancelExpenseCommand { ExpenseId = expenseId, Reason = reason }, cancellationToken);
    }

    public async Task<ApplicationResult> ArchiveAsync(Guid expenseId, string? reason = null, CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<ICommandHandler<ArchiveExpenseCommand, ApplicationResult>>();
        return await handler.HandleAsync(new ArchiveExpenseCommand { ExpenseId = expenseId, Reason = reason }, cancellationToken);
    }

    public async Task<ApplicationResult> DeleteAsync(Guid expenseId, CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<ICommandHandler<DeleteExpenseCommand, ApplicationResult>>();
        return await handler.HandleAsync(new DeleteExpenseCommand { ExpenseId = expenseId }, cancellationToken);
    }

    public async Task<ApplicationResult<Guid>> DuplicateAsync(Guid expenseId, CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<ICommandHandler<DuplicateExpenseCommand, ApplicationResult<Guid>>>();
        return await handler.HandleAsync(new DuplicateExpenseCommand
        {
            ExpenseId = expenseId,
            BranchId = BranchId
        }, cancellationToken);
    }

    public async Task<ApplicationResult> RecordPaymentAsync(RecordExpensePaymentCommand command, CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<ICommandHandler<RecordExpensePaymentCommand, ApplicationResult>>();
        return await handler.HandleAsync(command, cancellationToken);
    }

    public async Task<ApplicationResult> SchedulePaymentAsync(ScheduleExpensePaymentCommand command, CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<ICommandHandler<ScheduleExpensePaymentCommand, ApplicationResult>>();
        return await handler.HandleAsync(command, cancellationToken);
    }
}
