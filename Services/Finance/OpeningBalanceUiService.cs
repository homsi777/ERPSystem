using ERPSystem.Application.Abstractions;
using ERPSystem.Application.Abstractions.Services;
using ERPSystem.Application.Commands.Finance;
using ERPSystem.Application.Common;
using ERPSystem.Application.DTOs.Finance;
using ERPSystem.Application.Queries.Finance;
using ERPSystem.Application.Results;
using ERPSystem.Application.UseCases.Finance;
using ERPSystem.Domain.Entities.Finance;
using Microsoft.Extensions.DependencyInjection;

namespace ERPSystem.Services.Finance;

public static class OpeningBalanceNavigationContext
{
    public static OpeningBalanceType? FormType { get; set; }
    public static Guid? EditDocumentId { get; set; }
    public static Guid? WorkspaceDocumentId { get; set; }
    public static string? WorkspaceInitialTab { get; set; }

    public static void BeginCreate(OpeningBalanceType type)
    {
        FormType = type;
        EditDocumentId = null;
    }

    public static void BeginEdit(Guid documentId) => EditDocumentId = documentId;

    public static void BeginWorkspace(Guid documentId, string? tab = null)
    {
        WorkspaceDocumentId = documentId;
        WorkspaceInitialTab = tab;
    }

    public static (Guid? Id, string? Tab) TakeWorkspaceContext()
    {
        var id = WorkspaceDocumentId;
        var tab = WorkspaceInitialTab;
        WorkspaceDocumentId = null;
        WorkspaceInitialTab = null;
        return (id, tab);
    }
}

public static class OpeningBalanceListRefreshHub
{
    public static event EventHandler? RefreshRequested;
    public static void RequestRefresh() => RefreshRequested?.Invoke(null, EventArgs.Empty);
}

public static class CustomerOpeningBalanceRefreshHub
{
    public static event EventHandler? RefreshRequested;
    public static void RequestRefresh() => RefreshRequested?.Invoke(null, EventArgs.Empty);
}

public static class CustomerOpeningBalanceNavigationContext
{
    public static Guid? EditDocumentId { get; set; }

    public static void BeginCreate() => EditDocumentId = null;

    public static void BeginEdit(Guid documentId) => EditDocumentId = documentId;
}

public sealed class OpeningBalanceUiService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ICurrentBranchService _branch;

    public OpeningBalanceUiService(IServiceScopeFactory scopeFactory, ICurrentBranchService branch)
    {
        _scopeFactory = scopeFactory;
        _branch = branch;
    }

    public static OpeningBalanceUiService Instance => AppServices.GetRequiredService<OpeningBalanceUiService>();

    private Guid CompanyId => _branch.CompanyId ?? throw new InvalidOperationException("Company context is not set.");

    public async Task<bool> CanAsync(string permission, CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var permissions = scope.ServiceProvider.GetRequiredService<IPermissionService>();
        return await permissions.CanAsync(permission, cancellationToken);
    }

    public async Task<ApplicationResult<PagedResult<OpeningBalanceListDto>>> GetListAsync(
        OpeningBalanceListFilter filter,
        int page = 1,
        int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetOpeningBalanceListHandler>();
        return await handler.HandleAsync(new GetOpeningBalanceListQuery
        {
            CompanyId = CompanyId,
            Filter = filter,
            Page = page,
            PageSize = pageSize
        }, cancellationToken);
    }

    public async Task<ApplicationResult<OpeningBalanceDetailsDto>> GetDetailsAsync(
        Guid documentId,
        CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetOpeningBalanceDetailsHandler>();
        return await handler.HandleAsync(new GetOpeningBalanceDetailsQuery { DocumentId = documentId }, cancellationToken);
    }

    public async Task<ApplicationResult<OpeningBalanceDashboardDto>> GetDashboardAsync(
        CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetOpeningBalanceDashboardHandler>();
        return await handler.HandleAsync(new GetOpeningBalanceDashboardQuery { CompanyId = CompanyId }, cancellationToken);
    }

    public async Task<ApplicationResult<OpeningBalanceLookupsDto>> GetLookupsAsync(
        CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetOpeningBalanceLookupsHandler>();
        return await handler.HandleAsync(new GetOpeningBalanceLookupsQuery { CompanyId = CompanyId }, cancellationToken);
    }

    public Task<ApplicationResult<OpeningBalanceListDto>> CreateAsync(
        CreateOpeningBalanceCommand command,
        CancellationToken cancellationToken = default) =>
        DispatchCommand<CreateOpeningBalanceCommand, ApplicationResult<OpeningBalanceListDto>>(command, cancellationToken);

    public Task<ApplicationResult<OpeningBalanceListDto>> UpdateAsync(
        UpdateOpeningBalanceCommand command,
        CancellationToken cancellationToken = default) =>
        DispatchCommand<UpdateOpeningBalanceCommand, ApplicationResult<OpeningBalanceListDto>>(command, cancellationToken);

    public Task<ApplicationResult> SubmitAsync(Guid documentId, CancellationToken cancellationToken = default) =>
        DispatchCommand<SubmitOpeningBalanceCommand, ApplicationResult>(
            new SubmitOpeningBalanceCommand { DocumentId = documentId }, cancellationToken);

    public Task<ApplicationResult> ApproveAsync(Guid documentId, string? notes, CancellationToken cancellationToken = default) =>
        DispatchCommand<ApproveOpeningBalanceCommand, ApplicationResult>(
            new ApproveOpeningBalanceCommand { DocumentId = documentId, Notes = notes }, cancellationToken);

    public Task<ApplicationResult> RejectAsync(Guid documentId, string reason, CancellationToken cancellationToken = default) =>
        DispatchCommand<RejectOpeningBalanceCommand, ApplicationResult>(
            new RejectOpeningBalanceCommand { DocumentId = documentId, Reason = reason }, cancellationToken);

    public Task<ApplicationResult<OpeningBalancePostResultDto>> PostAsync(
        Guid documentId,
        bool lockAfterPost = true,
        CancellationToken cancellationToken = default) =>
        DispatchCommand<PostOpeningBalanceCommand, ApplicationResult<OpeningBalancePostResultDto>>(
            new PostOpeningBalanceCommand { DocumentId = documentId, LockAfterPost = lockAfterPost }, cancellationToken);

    public Task<ApplicationResult> ArchiveAsync(Guid documentId, CancellationToken cancellationToken = default) =>
        DispatchCommand<ArchiveOpeningBalanceCommand, ApplicationResult>(
            new ArchiveOpeningBalanceCommand { DocumentId = documentId }, cancellationToken);

    public Task<ApplicationResult> DeleteBeforePostAsync(Guid documentId, CancellationToken cancellationToken = default) =>
        DispatchCommand<DeleteOpeningBalanceBeforePostCommand, ApplicationResult>(
            new DeleteOpeningBalanceBeforePostCommand { DocumentId = documentId }, cancellationToken);

    public Task<ApplicationResult<OpeningBalanceListDto>> DuplicateAsync(
        Guid documentId,
        CancellationToken cancellationToken = default) =>
        DispatchCommand<DuplicateOpeningBalanceCommand, ApplicationResult<OpeningBalanceListDto>>(
            new DuplicateOpeningBalanceCommand { DocumentId = documentId }, cancellationToken);

    public Task<ApplicationResult<OpeningBalanceValidationReportDto>> ValidateAsync(
        ValidateOpeningBalanceCommand command,
        CancellationToken cancellationToken = default) =>
        DispatchCommand<ValidateOpeningBalanceCommand, ApplicationResult<OpeningBalanceValidationReportDto>>(command, cancellationToken);

    public Task<ApplicationResult<OpeningBalanceImportResultDto>> ImportExcelAsync(
        ImportOpeningBalanceExcelCommand command,
        CancellationToken cancellationToken = default) =>
        DispatchCommand<ImportOpeningBalanceExcelCommand, ApplicationResult<OpeningBalanceImportResultDto>>(command, cancellationToken);

    public byte[] GetImportTemplate(OpeningBalanceType type)
    {
        using var scope = _scopeFactory.CreateScope();
        var engine = scope.ServiceProvider.GetRequiredService<IOpeningBalanceEngine>();
        return engine.BuildImportTemplate(type);
    }

    public async Task<ApplicationResult<CustomerOpeningBalanceSummaryDto>> GetCustomerSummaryAsync(
        OpeningBalanceListFilter filter,
        CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetCustomerOpeningBalanceSummaryHandler>();
        return await handler.HandleAsync(new GetCustomerOpeningBalanceSummaryQuery
        {
            CompanyId = CompanyId,
            Filter = filter
        }, cancellationToken);
    }

    /// <summary>
    /// Posts a customer or supplier opening balance through the unified engine
    /// (create → submit → approve → post). Preferred path for party submodule screens.
    /// </summary>
    public async Task<ApplicationResult<OpeningBalancePostResultDto>> PostPartyOpeningBalanceAsync(
        PostPartyOpeningBalanceCommand command,
        CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var engine = scope.ServiceProvider.GetRequiredService<IOpeningBalanceEngine>();
        return await engine.PostPartyOpeningBalanceAsync(command, cancellationToken);
    }

    private async Task<TResult> DispatchCommand<TCommand, TResult>(TCommand command, CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<ICommandHandler<TCommand, TResult>>();
        return await handler.HandleAsync(command, cancellationToken);
    }
}
