using ERPSystem.Application.Abstractions;
using ERPSystem.Application.Abstractions.Repositories;
using ERPSystem.Application.Abstractions.Services;
using ERPSystem.Application.Commands.Capital;
using ERPSystem.Application.Common;
using ERPSystem.Application.DTOs.Capital;
using ERPSystem.Application.Queries.Capital;
using ERPSystem.Application.Results;
using ERPSystem.Application.UseCases.Capital;
using ERPSystem.Domain.Enums;
using Microsoft.Extensions.DependencyInjection;

namespace ERPSystem.Services.Capital;

public static class CapitalNavigationContext
{
    public static Guid? EditPartnerId { get; set; }
    public static Guid? WorkspacePartnerId { get; set; }
    public static string? WorkspaceInitialTab { get; set; }
    public static Guid? PreselectedPartnerId { get; set; }
    public static string? TransactionMode { get; set; }

    public static void BeginCreate() => EditPartnerId = null;

    public static void BeginEdit(Guid partnerId) => EditPartnerId = partnerId;

    public static void BeginTransaction(Guid partnerId, string mode = "Investment")
    {
        PreselectedPartnerId = partnerId;
        TransactionMode = mode;
    }

    public static void ClearTransactionContext()
    {
        PreselectedPartnerId = null;
        TransactionMode = null;
    }

    public static void BeginWorkspace(Guid partnerId, string? tab = null)
    {
        WorkspacePartnerId = partnerId;
        WorkspaceInitialTab = tab;
    }

    public static (Guid? Id, string? Tab) TakeWorkspaceContext()
    {
        var id = WorkspacePartnerId;
        var tab = WorkspaceInitialTab;
        WorkspacePartnerId = null;
        WorkspaceInitialTab = null;
        return (id, tab);
    }
}

public static class CapitalListRefreshHub
{
    public static event EventHandler? RefreshRequested;
    public static void RequestRefresh() => RefreshRequested?.Invoke(null, EventArgs.Empty);
}

public sealed class CapitalPartnerUiService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ICurrentBranchService _branch;

    public CapitalPartnerUiService(IServiceScopeFactory scopeFactory, ICurrentBranchService branch)
    {
        _scopeFactory = scopeFactory;
        _branch = branch;
    }

    public static CapitalPartnerUiService Instance => AppServices.GetRequiredService<CapitalPartnerUiService>();

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
        CanAsync("capital.view", cancellationToken);

    public Task<bool> CanCreateAsync(CancellationToken cancellationToken = default) =>
        CanAsync("capital.create", cancellationToken);

    public Task<bool> CanEditAsync(CancellationToken cancellationToken = default) =>
        CanAsync("capital.edit", cancellationToken);

    public Task<bool> CanApproveAsync(CancellationToken cancellationToken = default) =>
        CanAsync("capital.approve", cancellationToken);

    public Task<bool> CanArchiveAsync(CancellationToken cancellationToken = default) =>
        CanAsync("capital.archive", cancellationToken);

    public async Task<ApplicationResult<PagedResult<CapitalPartnerListDto>>> GetListAsync(
        CapitalPartnerListFilter filter,
        int page = 1,
        int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetCapitalPartnerListHandler>();
        return await handler.HandleAsync(new GetCapitalPartnerListQuery
        {
            CompanyId = CompanyId,
            Filter = filter,
            Page = page,
            PageSize = pageSize
        }, cancellationToken);
    }

    public async Task<ApplicationResult<CapitalPartnerDetailsDto>> GetDetailsAsync(
        Guid partnerId,
        CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetCapitalPartnerDetailsHandler>();
        return await handler.HandleAsync(new GetCapitalPartnerDetailsQuery { PartnerId = partnerId }, cancellationToken);
    }

    public async Task<ApplicationResult<CapitalOperationsCenterDto>> GetOperationsCenterAsync(
        Guid partnerId,
        CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetCapitalPartnerOperationsCenterHandler>();
        return await handler.HandleAsync(new GetCapitalPartnerOperationsCenterQuery { PartnerId = partnerId }, cancellationToken);
    }

    public async Task<ApplicationResult<CapitalDashboardDto>> GetDashboardAsync(
        CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetCapitalDashboardHandler>();
        return await handler.HandleAsync(new GetCapitalDashboardQuery { CompanyId = CompanyId }, cancellationToken);
    }

    public async Task<ApplicationResult<Guid>> CreatePartnerAsync(
        CreateCapitalPartnerCommand command,
        CancellationToken cancellationToken = default)
    {
        command.CompanyId = CompanyId;
        command.BranchId = BranchId;
        using var scope = _scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<ICommandHandler<CreateCapitalPartnerCommand, ApplicationResult<Guid>>>();
        return await handler.HandleAsync(command, cancellationToken);
    }

    public async Task<ApplicationResult> UpdatePartnerAsync(
        UpdateCapitalPartnerCommand command,
        CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<ICommandHandler<UpdateCapitalPartnerCommand, ApplicationResult>>();
        return await handler.HandleAsync(command, cancellationToken);
    }

    public async Task<ApplicationResult<Guid>> RecordTransactionAsync(
        RecordCapitalTransactionCommand command,
        CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<ICommandHandler<RecordCapitalTransactionCommand, ApplicationResult<Guid>>>();
        return await handler.HandleAsync(command, cancellationToken);
    }

    public async Task<ApplicationResult> SetCompanyOwnershipAsync(
        Guid partnerId,
        decimal ownershipPercentage,
        CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<ICommandHandler<SetPartnerCompanyOwnershipCommand, ApplicationResult>>();
        return await handler.HandleAsync(new SetPartnerCompanyOwnershipCommand
        {
            PartnerId = partnerId,
            OwnershipPercentage = ownershipPercentage
        }, cancellationToken);
    }

    public async Task<ApplicationResult<Guid>> CreatePartnerWithSetupAsync(
        string fullName,
        decimal ownershipPercentage,
        decimal initialAmount,
        string currency,
        string? nationalId,
        string? phone,
        string? notes,
        CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<ICommandHandler<CreateCapitalPartnerWithSetupCommand, ApplicationResult<Guid>>>();
        return await handler.HandleAsync(new CreateCapitalPartnerWithSetupCommand
        {
            CompanyId = CompanyId,
            BranchId = BranchId,
            FullName = fullName,
            NationalId = nationalId,
            Phone = phone,
            Notes = notes,
            DefaultCurrency = currency,
            RiskLevel = PartnerRiskLevel.Medium,
            OwnershipPercentage = ownershipPercentage,
            InitialInvestmentAmount = initialAmount
        }, cancellationToken);
    }

    public async Task<ApplicationResult<PagedResult<CapitalTransactionListDto>>> GetTransactionsAsync(
        CapitalTransactionListFilter filter,
        int page = 1,
        int pageSize = 100,
        CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetCapitalTransactionsHandler>();
        return await handler.HandleAsync(new GetCapitalTransactionsQuery
        {
            CompanyId = CompanyId,
            Filter = filter,
            Page = page,
            PageSize = pageSize
        }, cancellationToken);
    }

    public async Task<ApplicationResult> ArchivePartnerAsync(
        Guid partnerId,
        string? notes = null,
        CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<ICommandHandler<ArchiveCapitalPartnerCommand, ApplicationResult>>();
        return await handler.HandleAsync(new ArchiveCapitalPartnerCommand { PartnerId = partnerId, Notes = notes }, cancellationToken);
    }

    public async Task<ApplicationResult<CapitalReportDto>> GetReportAsync(
        string reportType,
        CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetCapitalReportHandler>();
        return await handler.HandleAsync(new GetCapitalReportQuery
        {
            CompanyId = CompanyId,
            ReportType = reportType
        }, cancellationToken);
    }

    public async Task<ApplicationResult<IReadOnlyList<ProfitDistributionListDto>>> GetDistributionsAsync(
        CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetProfitDistributionListHandler>();
        return await handler.HandleAsync(new GetProfitDistributionListQuery { CompanyId = CompanyId }, cancellationToken);
    }

    public async Task<ApplicationResult<IReadOnlyList<PartnerAuditEntryDto>>> GetAuditTrailAsync(
        Guid partnerId,
        CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetCapitalPartnerAuditTrailHandler>();
        return await handler.HandleAsync(new GetCapitalPartnerAuditTrailQuery { PartnerId = partnerId }, cancellationToken);
    }

    public async Task<ApplicationResult<IReadOnlyList<PartnerTimelineEventDto>>> GetTimelineAsync(
        Guid partnerId,
        CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetCapitalPartnerTimelineHandler>();
        return await handler.HandleAsync(new GetCapitalPartnerTimelineQuery { PartnerId = partnerId }, cancellationToken);
    }
}
