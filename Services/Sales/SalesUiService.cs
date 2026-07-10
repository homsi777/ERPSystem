using System.Collections.Concurrent;
using ERPSystem.Application.Abstractions;
using ERPSystem.Application.Abstractions.Services;
using ERPSystem.Application.Commands.Sales;
using ERPSystem.Application.Common;
using ERPSystem.Application.DTOs.Sales;
using ERPSystem.Application.DTOs.Warehouses;
using ERPSystem.Application.Queries.Sales;
using ERPSystem.Application.Queries.Warehouses;
using ERPSystem.Application.Results;
using ERPSystem.Application.UseCases.Queries;
using ERPSystem.Application.UseCases.Sales;
using ERPSystem.Domain.Enums;
using ERPSystem.Infrastructure.Seed;
using Microsoft.Extensions.DependencyInjection;

namespace ERPSystem.Services.Sales;

public static class SalesNavigationContext
{
    public static Guid? EditInvoiceId { get; set; }
    public static Guid? DetailingInvoiceId { get; set; }
    public static string? DetailingInvoiceNumber { get; set; }
    public static Guid? ReturnsFilterInvoiceId { get; set; }
    public static string? ReturnsFilterInvoiceNumber { get; set; }

    public static void BeginCreate() => EditInvoiceId = null;

    public static void BeginEdit(Guid invoiceId) => EditInvoiceId = invoiceId;

    public static void BeginDetailing(Guid? invoiceId, string? invoiceNumber = null)
    {
        DetailingInvoiceId = invoiceId;
        DetailingInvoiceNumber = invoiceNumber;
    }

    public static void BeginViewReturns(Guid invoiceId, string? invoiceNumber = null)
    {
        ReturnsFilterInvoiceId = invoiceId;
        ReturnsFilterInvoiceNumber = invoiceNumber;
    }

    public static (Guid? Id, string? Number) TakeDetailingContext()
    {
        var id = DetailingInvoiceId;
        var number = DetailingInvoiceNumber;
        DetailingInvoiceId = null;
        DetailingInvoiceNumber = null;
        return (id, number);
    }

    public static (Guid? Id, string? Number) TakeReturnsFilter()
    {
        var id = ReturnsFilterInvoiceId;
        var number = ReturnsFilterInvoiceNumber;
        ReturnsFilterInvoiceId = null;
        ReturnsFilterInvoiceNumber = null;
        return (id, number);
    }
}

public static class SalesListRefreshHub
{
    public static event EventHandler? RefreshRequested;

    public static void RequestRefresh() => RefreshRequested?.Invoke(null, EventArgs.Empty);
}

public static class DetailingQueueRefreshHub
{
    public static event EventHandler? RefreshRequested;

    public static void RequestRefresh() => RefreshRequested?.Invoke(null, EventArgs.Empty);
}

public static class SalesCatalogDefaults
{
    public static readonly Guid FabricItemId = Guid.Parse("88888888-8888-8888-8888-888888888888");
    public static readonly Guid FabricColorId = Guid.Parse("99999999-9999-9999-9999-999999999999");
}

public sealed class SalesUiService
{
    private static readonly ConcurrentDictionary<Guid, byte> ApproveInFlight = new();
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ICurrentBranchService _branch;

    public SalesUiService(IServiceScopeFactory scopeFactory, ICurrentBranchService branch)
    {
        _scopeFactory = scopeFactory;
        _branch = branch;
    }

    public static SalesUiService Instance =>
        AppServices.GetRequiredService<SalesUiService>();

    private Guid CompanyId =>
        _branch.CompanyId ?? throw new InvalidOperationException("Company context is not set.");

    private Guid BranchId =>
        _branch.BranchId ?? throw new InvalidOperationException("Branch context is not set.");

    public async Task<ApplicationResult<PagedResult<SalesInvoiceDto>>> GetListAsync(
        string? search,
        SalesInvoiceStatus? status = null,
        int page = 1,
        int pageSize = 100,
        Guid? customerId = null,
        CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetSalesInvoiceListHandler>();
        var result = await handler.HandleAsync(new GetSalesInvoiceListQuery
        {
            CompanyId = CompanyId,
            BranchId = BranchId,
            Status = status,
            CustomerId = customerId,
            Page = page,
            PageSize = pageSize
        }, cancellationToken);

        if (!result.IsSuccess || result.Value is null || string.IsNullOrWhiteSpace(search))
            return result;

        var term = search.Trim();
        var filtered = result.Value.Items
            .Where(i =>
                i.InvoiceNumber.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                i.CustomerName.Contains(term, StringComparison.OrdinalIgnoreCase))
            .ToList();

        return ApplicationResult<PagedResult<SalesInvoiceDto>>.Success(new PagedResult<SalesInvoiceDto>
        {
            Items = filtered,
            TotalCount = filtered.Count,
            Page = page,
            PageSize = pageSize
        });
    }

    public async Task<ApplicationResult<SalesInvoiceOperationsCenterDto>> GetOperationsCenterAsync(
        Guid invoiceId,
        CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetSalesInvoiceOperationsCenterHandler>();
        return await handler.HandleAsync(new GetSalesInvoiceOperationsCenterQuery { InvoiceId = invoiceId }, cancellationToken);
    }

    public async Task<ApplicationResult<IReadOnlyList<WarehouseListDto>>> GetWarehousesAsync(
        CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetWarehouseListHandler>();
        return await handler.HandleAsync(new GetWarehouseListQuery { BranchId = BranchId }, cancellationToken);
    }

    public async Task<ApplicationResult<Guid>> CreateDraftAsync(
        Guid customerId,
        Guid warehouseId,
        Guid chinaContainerId,
        PaymentType paymentType,
        IReadOnlyList<SalesInvoiceLineCommand> lines,
        string? invoiceNumber = null,
        decimal discountAmount = 0,
        Guid? cashboxId = null,
        decimal? partialPaymentAmount = null,
        CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<ICommandHandler<CreateSalesInvoiceDraftCommand, ApplicationResult<Guid>>>();
        return await handler.HandleAsync(new CreateSalesInvoiceDraftCommand
        {
            CompanyId = CompanyId,
            BranchId = BranchId,
            InvoiceNumber = invoiceNumber,
            CustomerId = customerId,
            WarehouseId = warehouseId,
            ChinaContainerId = chinaContainerId,
            PaymentType = paymentType,
            DiscountAmount = discountAmount,
            CashboxId = cashboxId,
            PartialPaymentAmount = partialPaymentAmount,
            Lines = lines
        }, cancellationToken);
    }

    public async Task<ApplicationResult> UpdateDraftAsync(
        Guid invoiceId,
        Guid customerId,
        Guid warehouseId,
        Guid chinaContainerId,
        PaymentType paymentType,
        IReadOnlyList<SalesInvoiceLineCommand> lines,
        decimal discountAmount = 0,
        Guid? cashboxId = null,
        decimal? partialPaymentAmount = null,
        CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<ICommandHandler<UpdateSalesInvoiceDraftCommand, ApplicationResult>>();
        return await handler.HandleAsync(new UpdateSalesInvoiceDraftCommand
        {
            InvoiceId = invoiceId,
            CustomerId = customerId,
            WarehouseId = warehouseId,
            ChinaContainerId = chinaContainerId,
            PaymentType = paymentType,
            DiscountAmount = discountAmount,
            CashboxId = cashboxId,
            PartialPaymentAmount = partialPaymentAmount,
            Lines = lines
        }, cancellationToken);
    }

    public async Task<ApplicationResult> ApplyDiscountAsync(
        Guid invoiceId,
        decimal discountAmount,
        CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<ICommandHandler<UpdateSalesInvoiceDiscountCommand, ApplicationResult>>();
        return await handler.HandleAsync(new UpdateSalesInvoiceDiscountCommand
        {
            InvoiceId = invoiceId,
            DiscountAmount = discountAmount
        }, cancellationToken);
    }

    public async Task<ApplicationResult> SendToWarehouseAsync(
        Guid invoiceId,
        CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<ICommandHandler<SendSalesInvoiceToWarehouseCommand, ApplicationResult>>();
        return await handler.HandleAsync(new SendSalesInvoiceToWarehouseCommand { InvoiceId = invoiceId }, cancellationToken);
    }

    public async Task<ApplicationResult> ApproveAsync(
        Guid invoiceId,
        CancellationToken cancellationToken = default)
    {
        if (!ApproveInFlight.TryAdd(invoiceId, 0))
            return ApplicationResult.Conflict("جاري تنفيذ اعتماد الفاتورة. يرجى الانتظار.");

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var handler = scope.ServiceProvider.GetRequiredService<ICommandHandler<ApproveSalesInvoiceCommand, ApplicationResult>>();
            return await handler.HandleAsync(new ApproveSalesInvoiceCommand { InvoiceId = invoiceId }, cancellationToken);
        }
        finally
        {
            ApproveInFlight.TryRemove(invoiceId, out _);
        }
    }

    public async Task<ApplicationResult<IReadOnlyList<SalesInvoiceBelowCostLineDto>>> CheckBelowCostAsync(
        Guid invoiceId,
        CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<CheckSalesInvoiceBelowCostHandler>();
        return await handler.HandleAsync(new CheckSalesInvoiceBelowCostQuery { InvoiceId = invoiceId }, cancellationToken);
    }

    public async Task<ApplicationResult> CancelAsync(
        Guid invoiceId,
        string reason,
        CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<ICommandHandler<CancelSalesInvoiceCommand, ApplicationResult>>();
        return await handler.HandleAsync(new CancelSalesInvoiceCommand
        {
            InvoiceId = invoiceId,
            Reason = reason
        }, cancellationToken);
    }

    public async Task<ApplicationResult> ApproveAndDeliverAsync(
        Guid invoiceId,
        string? receivedByName = null,
        CancellationToken cancellationToken = default)
    {
        var approve = await ApproveAsync(invoiceId, cancellationToken);
        if (!approve.IsSuccess)
            return approve;

        return await DeliverAsync(
            invoiceId,
            DateTime.UtcNow,
            receivedByName,
            driverName: null,
            notes: null,
            cancellationToken);
    }

    public async Task<ApplicationResult> DeliverAsync(
        Guid invoiceId,
        DateTime deliveryDate,
        string? receivedByName,
        string? driverName,
        string? notes,
        CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<ICommandHandler<ConfirmSalesInvoiceDeliveryCommand, ApplicationResult>>();
        return await handler.HandleAsync(new ConfirmSalesInvoiceDeliveryCommand
        {
            InvoiceId = invoiceId,
            DeliveryDate = deliveryDate,
            ReceivedByName = receivedByName,
            DriverName = driverName,
            Notes = notes
        }, cancellationToken);
    }

    public async Task<ApplicationResult> UpdateWarehouseAsync(
        Guid invoiceId,
        Guid warehouseId,
        CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<ICommandHandler<UpdateSalesInvoiceWarehouseCommand, ApplicationResult>>();
        return await handler.HandleAsync(new UpdateSalesInvoiceWarehouseCommand
        {
            InvoiceId = invoiceId,
            WarehouseId = warehouseId
        }, cancellationToken);
    }

    public async Task<ApplicationResult<IReadOnlyList<SalesInvoiceDto>>> GetDeliveryQueueAsync(
        bool includeDelivered = true,
        CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetDeliveryQueueHandler>();
        return await handler.HandleAsync(new GetDeliveryQueueQuery
        {
            CompanyId = CompanyId,
            BranchId = BranchId,
            IncludeDelivered = includeDelivered
        }, cancellationToken);
    }

    public async Task<ApplicationResult<IReadOnlyList<ReceiptInvoicePaymentDto>>> GetInvoicePaymentsAsync(
        Guid invoiceId,
        CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetInvoicePaymentHistoryHandler>();
        return await handler.HandleAsync(new GetInvoicePaymentHistoryQuery { InvoiceId = invoiceId }, cancellationToken);
    }

    public async Task<bool> CanDeliverAsync(CancellationToken cancellationToken = default) =>
        await CanAsync("sales.deliver", cancellationToken);

    public async Task<bool> CanReturnAsync(CancellationToken cancellationToken = default) =>
        await CanAsync("sales.return", cancellationToken);

    public async Task<bool> CanCreateAsync(CancellationToken cancellationToken = default) =>
        await CanAsync("sales.create", cancellationToken);

    public async Task<bool> CanApproveAsync(CancellationToken cancellationToken = default) =>
        await CanAsync("sales.approve", cancellationToken);

    public async Task<bool> CanCancelAsync(CancellationToken cancellationToken = default) =>
        await CanAsync("sales.cancel", cancellationToken);

    public async Task<bool> CanSendToWarehouseAsync(CancellationToken cancellationToken = default) =>
        await CanAsync("sales.send-to-warehouse", cancellationToken);

    public async Task<ApplicationResult<IReadOnlyList<SalesWarehouseStockOptionDto>>> GetWarehouseStockAsync(
        Guid containerId,
        Guid warehouseId,
        CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetSalesWarehouseStockHandler>();
        return await handler.HandleAsync(new GetSalesWarehouseStockQuery
        {
            ContainerId = containerId,
            WarehouseId = warehouseId
        }, cancellationToken);
    }

    public async Task<ApplicationResult<IReadOnlyList<WarehouseDetailingDto>>> GetDetailingQueueAsync(
        Guid warehouseId,
        CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetWarehouseDetailingQueueHandler>();
        return await handler.HandleAsync(new GetWarehouseDetailingQueueQuery { WarehouseId = warehouseId }, cancellationToken);
    }

    public async Task<ApplicationResult> CompleteDetailingAsync(
        Guid invoiceId,
        IReadOnlyList<RollLengthEntryCommand> rollEntries,
        CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<ICommandHandler<CompleteWarehouseDetailingCommand, ApplicationResult>>();
        return await handler.HandleAsync(new CompleteWarehouseDetailingCommand
        {
            InvoiceId = invoiceId,
            RollEntries = rollEntries
        }, cancellationToken);
    }

    public async Task<bool> CanCompleteDetailingAsync(CancellationToken cancellationToken = default) =>
        await CanAsync("warehouse.detailing", cancellationToken);

    public async Task<ApplicationResult> SaveDetailingDraftAsync(
        Guid invoiceId,
        IReadOnlyList<RollDraftEntryCommand> rollEntries,
        CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<ICommandHandler<SaveWarehouseDetailingDraftCommand, ApplicationResult>>();
        return await handler.HandleAsync(new SaveWarehouseDetailingDraftCommand
        {
            InvoiceId = invoiceId,
            RollEntries = rollEntries
        }, cancellationToken);
    }

    private async Task<bool> CanAsync(string permission, CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var permissions = scope.ServiceProvider.GetRequiredService<IPermissionService>();
        return await permissions.CanAsync(permission, cancellationToken);
    }
}
