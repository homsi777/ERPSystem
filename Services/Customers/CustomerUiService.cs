using ERPSystem.Application.Abstractions;
using ERPSystem.Application.Abstractions.Services;
using ERPSystem.Application.Commands.Customers;
using ERPSystem.Application.Common;
using ERPSystem.Application.DTOs.Customers;
using ERPSystem.Application.Queries.Customers;
using ERPSystem.Application.Results;
using ERPSystem.Application.UseCases.Customers;
using ERPSystem.Application.UseCases.Queries;
using ERPSystem.Domain.Enums;
using Microsoft.Extensions.DependencyInjection;

namespace ERPSystem.Services.Customers;

public static class CustomerNavigationContext
{
    public static Guid? EditCustomerId { get; set; }
    public static Guid? StatementCustomerId { get; set; }
    public static string? StatementCustomerName { get; set; }

    public static void BeginCreate() => EditCustomerId = null;

    public static void BeginEdit(Guid customerId) => EditCustomerId = customerId;

    public static void BeginStatement(Guid customerId, string customerName)
    {
        StatementCustomerId = customerId;
        StatementCustomerName = customerName;
    }

    public static (Guid? Id, string? Name) TakeStatementContext()
    {
        var id = StatementCustomerId;
        var name = StatementCustomerName;
        StatementCustomerId = null;
        StatementCustomerName = null;
        return (id, name);
    }
}

public sealed class CustomerUiService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ICurrentBranchService _branch;

    public CustomerUiService(IServiceScopeFactory scopeFactory, ICurrentBranchService branch)
    {
        _scopeFactory = scopeFactory;
        _branch = branch;
    }

    public static CustomerUiService Instance =>
        AppServices.GetRequiredService<CustomerUiService>();

    private Guid CompanyId =>
        _branch.CompanyId ?? throw new InvalidOperationException("Company context is not set.");

    public async Task<ApplicationResult<PagedResult<CustomerListDto>>> GetListAsync(
        string? search,
        int page = 1,
        int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetCustomerListHandler>();
        return await handler.HandleAsync(new GetCustomerListQuery
        {
            CompanyId = CompanyId,
            Search = search,
            Page = page,
            PageSize = pageSize
        }, cancellationToken);
    }

    public async Task<ApplicationResult<CustomerDetailsDto>> GetDetailsAsync(
        Guid customerId,
        CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetCustomerDetailsHandler>();
        return await handler.HandleAsync(new GetCustomerDetailsQuery { CustomerId = customerId }, cancellationToken);
    }

    public async Task<ApplicationResult<CustomerOperationsCenterDto>> GetOperationsCenterAsync(
        Guid customerId,
        CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetCustomerOperationsCenterHandler>();
        return await handler.HandleAsync(new GetCustomerOperationsCenterQuery { CustomerId = customerId }, cancellationToken);
    }

    public async Task<ApplicationResult<CustomerStatementDto>> GetStatementAsync(
        Guid customerId,
        DateTime? from,
        DateTime? to,
        CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetCustomerStatementHandler>();
        return await handler.HandleAsync(new GetCustomerStatementQuery
        {
            CustomerId = customerId,
            FromDate = from,
            ToDate = to
        }, cancellationToken);
    }

    public async Task<ApplicationResult<IReadOnlyList<CustomerSalesDetailDto>>> GetSalesDetailsAsync(
        Guid customerId,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetCustomerSalesDetailsHandler>();
        return await handler.HandleAsync(new GetCustomerSalesDetailsQuery
        {
            CustomerId = customerId,
            FromDate = fromDate,
            ToDate = toDate
        }, cancellationToken);
    }

    public async Task<ApplicationResult<CustomerAccountLedgerDto>> GetAccountLedgerAsync(
        Guid customerId,
        DateTime? from,
        DateTime? to,
        CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetCustomerAccountLedgerHandler>();
        return await handler.HandleAsync(new GetCustomerAccountLedgerQuery
        {
            CustomerId = customerId,
            FromDate = from,
            ToDate = to
        }, cancellationToken);
    }

    public async Task<ApplicationResult> ReconcileAccountAsync(
        Guid customerId,
        DateTime reconciliationDate,
        Guid documentId,
        decimal balanceAtReconciliation,
        CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<ICommandHandler<ReconcileCustomerAccountCommand, ApplicationResult>>();
        return await handler.HandleAsync(new ReconcileCustomerAccountCommand
        {
            CustomerId = customerId,
            ReconciliationDate = reconciliationDate,
            DocumentId = documentId,
            BalanceAtReconciliation = balanceAtReconciliation
        }, cancellationToken);
    }

    public async Task<string> NextCustomerCodeAsync(CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var numbering = scope.ServiceProvider.GetRequiredService<INumberingService>();
        var branchId = _branch.BranchId ?? throw new InvalidOperationException("Branch context is not set.");
        return await numbering.NextCustomerCodeAsync(branchId, cancellationToken);
    }

    public async Task<ApplicationResult<Guid>> CreateAsync(
        string code,
        string nameAr,
        string nameEn,
        CustomerType type,
        decimal creditLimit,
        bool creditLimitEnabled,
        CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<ICommandHandler<CreateCustomerCommand, ApplicationResult<Guid>>>();
        return await handler.HandleAsync(new CreateCustomerCommand
        {
            CompanyId = CompanyId,
            Code = code,
            NameAr = nameAr,
            NameEn = nameEn,
            Type = type,
            CreditLimit = creditLimit,
            CreditLimitEnabled = creditLimitEnabled
        }, cancellationToken);
    }

    public async Task<ApplicationResult> UpdateAsync(
        Guid customerId,
        string nameAr,
        string nameEn,
        decimal creditLimit,
        int paymentTermsDays,
        bool creditLimitEnabled,
        CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<ICommandHandler<UpdateCustomerCommand, ApplicationResult>>();
        return await handler.HandleAsync(new UpdateCustomerCommand
        {
            CustomerId = customerId,
            NameAr = nameAr,
            NameEn = nameEn,
            CreditLimit = creditLimit,
            PaymentTermsDays = paymentTermsDays,
            CreditLimitEnabled = creditLimitEnabled
        }, cancellationToken);
    }

    public async Task<ApplicationResult> DeactivateAsync(
        Guid customerId,
        CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<ICommandHandler<DeactivateCustomerCommand, ApplicationResult>>();
        return await handler.HandleAsync(new DeactivateCustomerCommand { CustomerId = customerId }, cancellationToken);
    }

    [Obsolete("Use OpeningBalanceUiService.PostPartyOpeningBalanceAsync instead.")]
    public async Task<ApplicationResult<CustomerOpeningBalanceResultDto>> PostOpeningBalanceAsync(
        PostCustomerOpeningBalanceCommand command, CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<ICommandHandler<PostCustomerOpeningBalanceCommand, ApplicationResult<CustomerOpeningBalanceResultDto>>>();
        return await handler.HandleAsync(command, cancellationToken);
    }

    public async Task<bool> CanCreateAsync(CancellationToken cancellationToken = default) =>
        await CanAsync("customers.create", cancellationToken);

    public async Task<bool> CanDeactivateAsync(CancellationToken cancellationToken = default) =>
        await CanAsync("customers.deactivate", cancellationToken);

    public async Task<bool> CanPostOpeningBalanceAsync(CancellationToken cancellationToken = default) =>
        await CanAsync("customers.opening-balance", cancellationToken);

    private async Task<bool> CanAsync(string permission, CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var permissions = scope.ServiceProvider.GetRequiredService<IPermissionService>();
        return await permissions.CanAsync(permission, cancellationToken);
    }
}
