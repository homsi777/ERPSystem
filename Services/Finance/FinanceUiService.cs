using ERPSystem.Application.Abstractions;
using ERPSystem.Application.Abstractions.Repositories;
using ERPSystem.Application.Abstractions.Services;
using ERPSystem.Application.Commands.Finance;
using ERPSystem.Application.DTOs.Expenses;
using ERPSystem.Application.Queries.Customers;
using ERPSystem.Application.Results;
using ERPSystem.Application.UseCases.Queries;
using Microsoft.Extensions.DependencyInjection;

namespace ERPSystem.Services.Finance;

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
        using var scope = _scopeFactory.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<ICashboxRepository>();
        var boxes = await repo.GetListAsync(BranchId, cancellationToken);
        var dtos = boxes
            .Where(b => b.IsActive)
            .Select(b => new CashboxOptionDto { Id = b.Id, Code = b.Code, Name = b.Name })
            .ToList();
        return ApplicationResult<IReadOnlyList<CashboxOptionDto>>.Success(dtos);
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
