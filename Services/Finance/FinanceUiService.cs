using ERPSystem.Application.Abstractions;
using ERPSystem.Application.Abstractions.Repositories;
using ERPSystem.Application.Abstractions.Services;
using ERPSystem.Application.Commands.Finance;
using ERPSystem.Application.DTOs.Expenses;
using ERPSystem.Application.DTOs.Finance;
using ERPSystem.Application.Queries.Customers;
using ERPSystem.Application.Queries.Finance;
using ERPSystem.Application.Results;
using ERPSystem.Application.UseCases.Finance;
using ERPSystem.Application.UseCases.Queries;
using Microsoft.Extensions.DependencyInjection;

namespace ERPSystem.Services.Finance;

public static class CashboxNavigationContext
{
    public static Guid? EditCashboxId { get; set; }
    public static Guid? TransferFromCashboxId { get; set; }

    public static void BeginCreate() => EditCashboxId = null;
    public static void BeginEdit(Guid id) => EditCashboxId = id;
    public static void BeginTransfer(Guid? fromCashboxId = null) => TransferFromCashboxId = fromCashboxId;
}

public static class CashboxListRefreshHub
{
    public static event EventHandler? RefreshRequested;
    public static void RequestRefresh() => RefreshRequested?.Invoke(null, EventArgs.Empty);
}

public sealed class FinancePartyOption
{
    public Guid Id { get; init; }
    public string Display { get; init; } = "";
}

public sealed class FinanceUiService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ICurrentBranchService _branch;

    public FinanceUiService(IServiceScopeFactory scopeFactory, ICurrentBranchService branch)
    {
        _scopeFactory = scopeFactory;
        _branch = branch;
    }

    public static FinanceUiService Instance => AppServices.GetRequiredService<FinanceUiService>();

    private Guid CompanyId =>
        _branch.CompanyId ?? throw new InvalidOperationException("Company context is not set.");

    private Guid BranchId =>
        _branch.BranchId ?? throw new InvalidOperationException("Branch context is not set.");

    public async Task<ApplicationResult<IReadOnlyList<FinancePartyOption>>> GetCustomersAsync(
        CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetCustomerListHandler>();
        var result = await handler.HandleAsync(new GetCustomerListQuery
        {
            CompanyId = CompanyId,
            Page = 1,
            PageSize = 500
        }, cancellationToken);

        if (!result.IsSuccess || result.Value is null)
            return ApplicationResult<IReadOnlyList<FinancePartyOption>>.Failure(
                result.ErrorMessage ?? "تعذّر تحميل العملاء.");

        var options = result.Value.Items
            .Where(c => c.IsActive)
            .Select(c => new FinancePartyOption { Id = c.Id, Display = $"{c.Code} — {c.NameAr}" })
            .ToList();

        return ApplicationResult<IReadOnlyList<FinancePartyOption>>.Success(options);
    }

    public async Task<ApplicationResult<IReadOnlyList<FinancePartyOption>>> GetSuppliersAsync(
        CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<ISupplierRepository>();
        var suppliers = await repository.GetListAsync(CompanyId, cancellationToken: cancellationToken);
        var options = suppliers
            .Select(s => new FinancePartyOption
            {
                Id = s.Supplier.Id,
                Display = $"{s.Supplier.Code} — {s.Supplier.Name}"
            })
            .ToList();

        return ApplicationResult<IReadOnlyList<FinancePartyOption>>.Success(options);
    }

    public async Task<ApplicationResult<IReadOnlyList<CashboxOptionDto>>> GetCashboxesAsync(
        CancellationToken cancellationToken = default)
    {
        var list = await GetCashboxListAsync(includeInactive: false, cancellationToken);
        if (!list.IsSuccess || list.Value is null)
            return ApplicationResult<IReadOnlyList<CashboxOptionDto>>.Failure(list.ErrorMessage ?? "تعذّر تحميل الصناديق.");
        var dtos = list.Value
            .Select(b => new CashboxOptionDto { Id = b.Id, Code = b.Code, Name = b.Name })
            .ToList();
        return ApplicationResult<IReadOnlyList<CashboxOptionDto>>.Success(dtos);
    }

    public async Task<ApplicationResult<IReadOnlyList<CashboxListDto>>> GetCashboxListAsync(
        bool includeInactive = true,
        CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetCashboxListHandler>();
        return await handler.HandleAsync(new GetCashboxListQuery
        {
            BranchId = BranchId,
            IncludeInactive = includeInactive
        }, cancellationToken);
    }

    public async Task<ApplicationResult<CashboxDetailsDto>> GetCashboxDetailsAsync(
        Guid cashboxId,
        CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetCashboxDetailsHandler>();
        return await handler.HandleAsync(new GetCashboxDetailsQuery { CashboxId = cashboxId }, cancellationToken);
    }

    public async Task<ApplicationResult<CashboxOperationsCenterDto>> GetCashboxOperationsCenterAsync(
        Guid cashboxId,
        CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetCashboxOperationsCenterHandler>();
        return await handler.HandleAsync(new GetCashboxOperationsCenterQuery { CashboxId = cashboxId }, cancellationToken);
    }

    public async Task<ApplicationResult<IReadOnlyList<CashboxMovementDto>>> GetCashboxMovementsAsync(
        Guid cashboxId,
        DateTime? from = null,
        DateTime? to = null,
        CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetCashboxMovementsHandler>();
        return await handler.HandleAsync(new GetCashboxMovementsQuery
        {
            CashboxId = cashboxId,
            FromDate = from,
            ToDate = to
        }, cancellationToken);
    }

    public async Task<ApplicationResult<IReadOnlyList<CashboxTransferListDto>>> GetCashboxTransfersAsync(
        Guid? cashboxId = null,
        CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetCashboxTransferListHandler>();
        return await handler.HandleAsync(new GetCashboxTransferListQuery
        {
            BranchId = BranchId,
            CashboxId = cashboxId
        }, cancellationToken);
    }

    public async Task<string> NextCashboxCodeAsync(CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var numbering = scope.ServiceProvider.GetRequiredService<INumberingService>();
        return await numbering.NextCashboxCodeAsync(BranchId, cancellationToken);
    }

    public async Task<ApplicationResult<Guid>> CreateCashboxAsync(
        string code, string name, string currency = "USD",
        CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<ICommandHandler<CreateCashboxCommand, ApplicationResult<Guid>>>();
        return await handler.HandleAsync(new CreateCashboxCommand
        {
            CompanyId = CompanyId,
            BranchId = BranchId,
            Code = code,
            Name = name,
            Currency = currency
        }, cancellationToken);
    }

    public async Task<ApplicationResult> UpdateCashboxAsync(
        Guid cashboxId, string code, string name, string currency,
        CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<ICommandHandler<UpdateCashboxCommand, ApplicationResult>>();
        return await handler.HandleAsync(new UpdateCashboxCommand
        {
            CashboxId = cashboxId,
            Code = code,
            Name = name,
            Currency = currency
        }, cancellationToken);
    }

    public async Task<ApplicationResult> DeactivateCashboxAsync(Guid cashboxId, CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<ICommandHandler<DeactivateCashboxCommand, ApplicationResult>>();
        return await handler.HandleAsync(new DeactivateCashboxCommand { CashboxId = cashboxId }, cancellationToken);
    }

    public async Task<ApplicationResult> ActivateCashboxAsync(Guid cashboxId, CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<ICommandHandler<ActivateCashboxCommand, ApplicationResult>>();
        return await handler.HandleAsync(new ActivateCashboxCommand { CashboxId = cashboxId }, cancellationToken);
    }

    public async Task<ApplicationResult<Guid>> CreateCashboxTransferAsync(
        Guid fromCashboxId, Guid toCashboxId, decimal amount, string? notes = null,
        CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<ICommandHandler<CreateCashboxTransferCommand, ApplicationResult<Guid>>>();
        return await handler.HandleAsync(new CreateCashboxTransferCommand
        {
            CompanyId = CompanyId,
            BranchId = BranchId,
            FromCashboxId = fromCashboxId,
            ToCashboxId = toCashboxId,
            Amount = amount,
            Notes = notes,
            PostImmediately = true
        }, cancellationToken);
    }

    public async Task<bool> CanCreateCashboxAsync(CancellationToken cancellationToken = default) =>
        await CanAsync("finance.cashbox.create", cancellationToken);

    public async Task<bool> CanEditCashboxAsync(CancellationToken cancellationToken = default) =>
        await CanAsync("finance.cashbox.edit", cancellationToken);

    public async Task<bool> CanTransferCashboxAsync(CancellationToken cancellationToken = default) =>
        await CanAsync("finance.cashbox.transfer", cancellationToken);

    private async Task<bool> CanAsync(string permission, CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var permissions = scope.ServiceProvider.GetRequiredService<IPermissionService>();
        return await permissions.CanAsync(permission, cancellationToken);
    }

    public async Task<ApplicationResult<Guid>> CreateReceiptVoucherAsync(
        Guid customerId,
        Guid cashboxId,
        decimal amount,
        CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<ICommandHandler<CreateReceiptVoucherCommand, ApplicationResult<Guid>>>();
        return await handler.HandleAsync(new CreateReceiptVoucherCommand
        {
            CompanyId = CompanyId,
            BranchId = BranchId,
            CustomerId = customerId,
            CashboxId = cashboxId,
            Amount = amount
        }, cancellationToken);
    }

    public async Task<ApplicationResult> PostReceiptVoucherAsync(
        Guid voucherId,
        CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<ICommandHandler<PostReceiptVoucherCommand, ApplicationResult>>();
        return await handler.HandleAsync(new PostReceiptVoucherCommand { VoucherId = voucherId }, cancellationToken);
    }

    public async Task<ApplicationResult<Guid>> CreatePaymentVoucherAsync(
        Guid supplierId,
        Guid cashboxId,
        decimal amount,
        CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<ICommandHandler<CreatePaymentVoucherCommand, ApplicationResult<Guid>>>();
        return await handler.HandleAsync(new CreatePaymentVoucherCommand
        {
            CompanyId = CompanyId,
            BranchId = BranchId,
            SupplierId = supplierId,
            CashboxId = cashboxId,
            Amount = amount
        }, cancellationToken);
    }

    public async Task<ApplicationResult> PostPaymentVoucherAsync(
        Guid voucherId,
        Guid? purchaseInvoiceId = null,
        CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<ICommandHandler<PostPaymentVoucherCommand, ApplicationResult>>();
        return await handler.HandleAsync(new PostPaymentVoucherCommand
        {
            VoucherId = voucherId,
            PurchaseInvoiceId = purchaseInvoiceId
        }, cancellationToken);
    }
}
