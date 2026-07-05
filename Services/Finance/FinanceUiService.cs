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

/// <summary>A receipt voucher belonging to a customer (Customer OC → Receipts tab).</summary>
public sealed class CustomerReceiptRow
{
    public string VoucherNumber { get; init; } = "";
    public DateTime VoucherDate { get; init; }
    public decimal Amount { get; init; }
    public string CashboxName { get; init; } = "";
    public string StatusDisplay { get; init; } = "";

    public string DateDisplay => VoucherDate.ToString("yyyy/MM/dd");
    public string AmountDisplay => Amount.ToString("N2");
}

/// <summary>An open sales invoice a receipt can be allocated to.</summary>
public sealed class OpenInvoiceOption
{
    public Guid Id { get; init; }
    public string InvoiceNumber { get; init; } = "";
    public DateTime InvoiceDate { get; init; }
    public decimal Total { get; init; }
    public decimal Collected { get; init; }
    public decimal Remaining { get; init; }
    public decimal Allocated { get; set; }

    public string DateDisplay => InvoiceDate.ToString("yyyy/MM/dd");
    public string TotalDisplay => Total.ToString("N2");
    public string CollectedDisplay => Collected.ToString("N2");
    public string RemainingDisplay => Remaining.ToString("N2");
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
        IReadOnlyList<ReceiptInvoiceAllocationInput>? allocations = null,
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
            Amount = amount,
            Allocations = allocations ?? []
        }, cancellationToken);
    }

    /// <summary>All receipt vouchers for a customer (for the Customer OC → Receipts tab).</summary>
    public async Task<IReadOnlyList<CustomerReceiptRow>> GetReceiptsForCustomerAsync(
        Guid customerId,
        CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var voucherRepo = scope.ServiceProvider.GetRequiredService<IReceiptVoucherRepository>();
        var boxRepo = scope.ServiceProvider.GetRequiredService<ICashboxRepository>();

        var vouchers = await voucherRepo.GetListAsync(CompanyId, null, customerId, cancellationToken);
        var boxes = await boxRepo.GetListAsync(BranchId, cancellationToken);
        var boxNames = boxes.ToDictionary(b => b.Id, b => b.Name);

        return vouchers
            .OrderByDescending(v => v.VoucherDate)
            .Select(v => new CustomerReceiptRow
            {
                VoucherNumber = v.VoucherNumber,
                VoucherDate = v.VoucherDate,
                Amount = v.Amount.Amount,
                CashboxName = boxNames.GetValueOrDefault(v.CashboxId, "—"),
                StatusDisplay = MapVoucherStatus(v.Status)
            })
            .ToList();
    }

    private static string MapVoucherStatus(Domain.Enums.VoucherStatus s) => s switch
    {
        Domain.Enums.VoucherStatus.Draft => "مسودة",
        Domain.Enums.VoucherStatus.Approved => "معتمد",
        Domain.Enums.VoucherStatus.Posted => "مرحّل",
        Domain.Enums.VoucherStatus.Cancelled => "ملغى",
        _ => s.ToString()
    };

    /// <summary>Open (posted, not fully collected) sales invoices for a customer, for receipt allocation.</summary>
    public async Task<IReadOnlyList<OpenInvoiceOption>> GetOpenInvoicesForCustomerAsync(
        Guid customerId,
        CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var invoiceRepo = scope.ServiceProvider.GetRequiredService<ISalesInvoiceRepository>();
        var paymentRepo = scope.ServiceProvider.GetRequiredService<IReceiptInvoicePaymentRepository>();

        var invoices = await invoiceRepo.GetListAsync(CompanyId, BranchId, null, customerId, cancellationToken);
        var rows = new List<OpenInvoiceOption>();
        foreach (var inv in invoices)
        {
            if (inv.Status is not (Domain.Enums.SalesInvoiceStatus.Approved
                or Domain.Enums.SalesInvoiceStatus.Printed
                or Domain.Enums.SalesInvoiceStatus.Delivered))
                continue;

            var total = inv.GrandTotal.Amount;
            if (total <= 0) continue;
            var collected = await paymentRepo.GetCollectedTotalAsync(inv.Id, cancellationToken);
            var remaining = total - collected;
            if (remaining <= 0.005m) continue;

            rows.Add(new OpenInvoiceOption
            {
                Id = inv.Id,
                InvoiceNumber = inv.InvoiceNumber.Value,
                InvoiceDate = inv.InvoiceDate,
                Total = total,
                Collected = collected,
                Remaining = remaining
            });
        }
        return rows.OrderBy(r => r.InvoiceDate).ToList();
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
