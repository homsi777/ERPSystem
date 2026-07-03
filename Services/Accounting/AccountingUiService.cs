using ERPSystem.Application.Abstractions;
using ERPSystem.Application.Abstractions.Services;
using ERPSystem.Application.Abstractions.Repositories;
using ERPSystem.Application.Commands.Accounting;
using ERPSystem.Application.Common;
using ERPSystem.Application.DTOs.Accounting;
using ERPSystem.Application.Queries.Accounting;
using ERPSystem.Application.Results;
using ERPSystem.Application.UseCases.Accounting;
using ERPSystem.Domain.Enums;
using Microsoft.Extensions.DependencyInjection;

namespace ERPSystem.Services.Accounting;

public static class AccountingNavigationContext
{
    public static Guid? EditAccountId { get; set; }
    public static Guid? PreselectedParentId { get; set; }
    public static Guid? EditJournalEntryId { get; set; }

    public static void BeginCreate(Guid? parentId = null)
    {
        EditAccountId = null;
        PreselectedParentId = parentId;
    }

    public static void BeginEdit(Guid accountId)
    {
        EditAccountId = accountId;
        PreselectedParentId = null;
    }

    public static void BeginJournalCreate() => EditJournalEntryId = null;

    public static void Clear()
    {
        EditAccountId = null;
        PreselectedParentId = null;
        EditJournalEntryId = null;
    }
}

public static class AccountingListRefreshHub
{
    public static event EventHandler? RefreshRequested;
    public static void RequestRefresh() => RefreshRequested?.Invoke(null, EventArgs.Empty);
}

public sealed class AccountingUiService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ICurrentBranchService _branch;

    public AccountingUiService(IServiceScopeFactory scopeFactory, ICurrentBranchService branch)
    {
        _scopeFactory = scopeFactory;
        _branch = branch;
    }

    public static AccountingUiService Instance => AppServices.GetRequiredService<AccountingUiService>();

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

    public Task<bool> CanViewAccountsAsync(CancellationToken ct = default) =>
        CanAsync("accounting.account.view", ct);

    public Task<bool> CanCreateAccountAsync(CancellationToken ct = default) =>
        CanAsync("accounting.account.create", ct);

    public Task<bool> CanEditAccountAsync(CancellationToken ct = default) =>
        CanAsync("accounting.account.edit", ct);

    public Task<bool> CanCreateJournalAsync(CancellationToken ct = default) =>
        CanAsync("accounting.journal.create", ct);

    public Task<bool> CanPostJournalAsync(CancellationToken ct = default) =>
        CanAsync("accounting.journal.post", ct);

    public Task<bool> CanReverseJournalAsync(CancellationToken ct = default) =>
        CanAsync("accounting.journal.reverse", ct);

    public async Task<ApplicationResult<IReadOnlyList<AccountListDto>>> GetAccountsAsync(
        string? search = null,
        GlAccountType? accountType = null,
        CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetAccountListHandler>();
        return await handler.HandleAsync(new GetAccountListQuery
        {
            CompanyId = CompanyId,
            Search = search,
            AccountType = accountType
        }, cancellationToken);
    }

    public async Task<ApplicationResult<AccountDetailsDto>> GetAccountDetailsAsync(
        Guid accountId,
        CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetAccountDetailsHandler>();
        return await handler.HandleAsync(new GetAccountDetailsQuery { AccountId = accountId }, cancellationToken);
    }

    public async Task<ApplicationResult<IReadOnlyList<AccountLookupDto>>> GetPostableAccountsAsync(
        CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetPostableAccountsHandler>();
        return await handler.HandleAsync(new GetPostableAccountsQuery { CompanyId = CompanyId }, cancellationToken);
    }

    public async Task<ApplicationResult<Guid>> CreateAccountAsync(
        CreateAccountCommand command,
        CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<ICommandHandler<CreateAccountCommand, ApplicationResult<Guid>>>();
        command.CompanyId = CompanyId;
        return await handler.HandleAsync(command, cancellationToken);
    }

    public async Task<ApplicationResult> UpdateAccountAsync(
        UpdateAccountCommand command,
        CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<ICommandHandler<UpdateAccountCommand, ApplicationResult>>();
        return await handler.HandleAsync(command, cancellationToken);
    }

    public async Task<ApplicationResult> DeactivateAccountAsync(
        Guid accountId,
        CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<ICommandHandler<DeactivateAccountCommand, ApplicationResult>>();
        return await handler.HandleAsync(new DeactivateAccountCommand { AccountId = accountId }, cancellationToken);
    }

    public async Task<ApplicationResult<PagedResult<JournalEntryListDto>>> GetJournalEntriesAsync(
        JournalEntryListFilter filter,
        int page = 1,
        int pageSize = 100,
        CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetJournalEntryListHandler>();
        return await handler.HandleAsync(new GetJournalEntryListQuery
        {
            CompanyId = CompanyId,
            Filter = filter,
            Page = page,
            PageSize = pageSize
        }, cancellationToken);
    }

    public async Task<ApplicationResult<JournalEntryDetailsDto>> GetJournalDetailsAsync(
        Guid entryId,
        CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetJournalEntryDetailsHandler>();
        return await handler.HandleAsync(new GetJournalEntryDetailsQuery { EntryId = entryId }, cancellationToken);
    }

    public async Task<ApplicationResult<Guid>> CreateJournalEntryAsync(
        CreateJournalEntryCommand command,
        CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<ICommandHandler<CreateJournalEntryCommand, ApplicationResult<Guid>>>();
        command.CompanyId = CompanyId;
        command.BranchId = BranchId;
        return await handler.HandleAsync(command, cancellationToken);
    }

    public async Task<ApplicationResult> ApproveJournalAsync(Guid entryId, CancellationToken ct = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<ICommandHandler<ApproveJournalEntryCommand, ApplicationResult>>();
        return await handler.HandleAsync(new ApproveJournalEntryCommand { EntryId = entryId }, ct);
    }

    public async Task<ApplicationResult> PostJournalAsync(Guid entryId, CancellationToken ct = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<ICommandHandler<PostJournalEntryCommand, ApplicationResult>>();
        return await handler.HandleAsync(new PostJournalEntryCommand { EntryId = entryId }, ct);
    }

    public async Task<ApplicationResult<Guid>> ReverseJournalAsync(Guid entryId, CancellationToken ct = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<ICommandHandler<ReverseJournalEntryCommand, ApplicationResult<Guid>>>();
        return await handler.HandleAsync(new ReverseJournalEntryCommand { EntryId = entryId }, ct);
    }

    public async Task<ApplicationResult> CancelJournalAsync(Guid entryId, CancellationToken ct = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<ICommandHandler<CancelJournalEntryCommand, ApplicationResult>>();
        return await handler.HandleAsync(new CancelJournalEntryCommand { EntryId = entryId }, ct);
    }

    public async Task<ApplicationResult<IReadOnlyList<JournalBookListDto>>> GetJournalBooksAsync(
        CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetJournalBooksHandler>();
        return await handler.HandleAsync(new GetJournalBooksQuery { CompanyId = CompanyId }, cancellationToken);
    }

    public async Task<ApplicationResult<IReadOnlyList<TrialBalanceLineDto>>> GetTrialBalanceAsync(
        DateTime asOfDate,
        CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetTrialBalanceHandler>();
        return await handler.HandleAsync(new GetTrialBalanceQuery
        {
            CompanyId = CompanyId,
            AsOfDate = asOfDate
        }, cancellationToken);
    }

    public async Task<ApplicationResult<IReadOnlyList<AccountLedgerLineDto>>> GetAccountLedgerAsync(
        Guid accountId,
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetAccountLedgerHandler>();
        return await handler.HandleAsync(new GetAccountLedgerQuery
        {
            AccountId = accountId,
            FromDate = from,
            ToDate = to
        }, cancellationToken);
    }
}
