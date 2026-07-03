using ERPSystem.Application.Abstractions;
using ERPSystem.Application.Abstractions.Repositories;
using ERPSystem.Application.Abstractions.Services;
using ERPSystem.Application.Commands.Purchases;
using ERPSystem.Application.Commands.Finance;
using ERPSystem.Application.DTOs.Purchases;
using ERPSystem.Application.Queries.Purchases;
using ERPSystem.Application.Results;
using ERPSystem.Application.UseCases.Purchases;
using ERPSystem.Application.UseCases.Queries;
using ERPSystem.Core;
using ERPSystem.Core.Actions;
using ERPSystem.Core.Purchases;
using ERPSystem.Core.Workspace;
using ERPSystem.Domain.Enums;
using ERPSystem.Services.Accounting;
using ERPSystem.Services;
using Microsoft.Extensions.DependencyInjection;
namespace ERPSystem.Services.Purchases;

public static class PurchaseNavigationContext
{
    public static Guid? EditInvoiceId { get; set; }
    public static Guid? EditOrderId { get; set; }
    public static Guid? EditReturnId { get; set; }
    public static Guid? ReturnSourceInvoiceId { get; set; }
    public static Guid? PaymentInvoiceId { get; set; }
    public static Guid? PaymentSupplierId { get; set; }
    public static decimal? PaymentAmount { get; set; }
    public static string? PaymentReference { get; set; }
    public static Guid? ConvertOrderId { get; set; }
    public static Guid? ReturnInvoiceId { get; set; }

    public static void BeginCreate() => EditInvoiceId = null;
    public static void BeginEdit(Guid id) => EditInvoiceId = id;
    public static void BeginOrderCreate() => EditOrderId = null;
    public static void BeginOrderEdit(Guid id) => EditOrderId = id;
    public static void BeginReturnCreate(Guid? sourceInvoiceId = null)
    {
        EditReturnId = null;
        ReturnSourceInvoiceId = sourceInvoiceId;
    }
    public static void BeginReturnEdit(Guid id) => EditReturnId = id;

    public static void BeginPayment(Guid invoiceId, Guid supplierId, decimal amount, string reference)
    {
        PaymentInvoiceId = invoiceId;
        PaymentSupplierId = supplierId;
        PaymentAmount = amount;
        PaymentReference = reference;
    }

    public static (Guid? InvoiceId, Guid? SupplierId, decimal? Amount, string? Reference) TakePaymentContext()
    {
        var ctx = (PaymentInvoiceId, PaymentSupplierId, PaymentAmount, PaymentReference);
        PaymentInvoiceId = null;
        PaymentSupplierId = null;
        PaymentAmount = null;
        PaymentReference = null;
        return ctx;
    }
}

public sealed class PurchaseUiService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ICurrentBranchService _branch;

    public PurchaseUiService(IServiceScopeFactory scopeFactory, ICurrentBranchService branch)
    {
        _scopeFactory = scopeFactory;
        _branch = branch;
    }

    public static PurchaseUiService Instance => AppServices.GetRequiredService<PurchaseUiService>();

    private Guid CompanyId => _branch.CompanyId ?? throw new InvalidOperationException("Company context is not set.");
    private Guid BranchId => _branch.BranchId ?? throw new InvalidOperationException("Branch context is not set.");

    public async Task<ApplicationResult<IReadOnlyList<PurchaseInvoiceListDto>>> GetInvoiceListAsync(
        string? search, PurchaseInvoiceStatus? status = null, CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetPurchaseInvoiceListHandler>();
        return await handler.HandleAsync(new GetPurchaseInvoiceListQuery
        {
            CompanyId = CompanyId,
            Search = search,
            Status = status
        }, cancellationToken);
    }

    public async Task<ApplicationResult<PurchaseInvoiceDetailsDto>> GetInvoiceDetailsAsync(
        Guid id, CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetPurchaseInvoiceDetailsHandler>();
        return await handler.HandleAsync(new GetPurchaseInvoiceDetailsQuery { InvoiceId = id }, cancellationToken);
    }

    public async Task<ApplicationResult<PurchaseOperationsCenterDto>> GetOperationsCenterAsync(
        Guid id, CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetPurchaseInvoiceOperationsCenterHandler>();
        return await handler.HandleAsync(new GetPurchaseInvoiceOperationsCenterQuery { InvoiceId = id }, cancellationToken);
    }

    public async Task<ApplicationResult<IReadOnlyList<PurchaseOrderListDto>>> GetOrderListAsync(
        PurchaseOrderStatus? status = null, CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetPurchaseOrderListHandler>();
        return await handler.HandleAsync(new GetPurchaseOrderListQuery { CompanyId = CompanyId, Status = status }, cancellationToken);
    }

    public async Task<ApplicationResult<IReadOnlyList<PurchaseReturnListDto>>> GetReturnListAsync(
        CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetPurchaseReturnListHandler>();
        return await handler.HandleAsync(new GetPurchaseReturnListQuery { CompanyId = CompanyId }, cancellationToken);
    }

    public async Task<string> NextInvoiceNumberAsync(CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var numbering = scope.ServiceProvider.GetRequiredService<INumberingService>();
        return await numbering.NextPurchaseInvoiceNumberAsync(BranchId, cancellationToken);
    }

    public async Task<ApplicationResult<Guid>> CreateDraftAsync(CreatePurchaseInvoiceDraftCommand command, CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<ICommandHandler<CreatePurchaseInvoiceDraftCommand, ApplicationResult<Guid>>>();
        return await handler.HandleAsync(new CreatePurchaseInvoiceDraftCommand
        {
            CompanyId = CompanyId,
            BranchId = BranchId,
            InvoiceNumber = command.InvoiceNumber,
            SupplierId = command.SupplierId,
            SupplierReference = command.SupplierReference,
            InvoiceDate = command.InvoiceDate,
            DueDate = command.DueDate,
            WarehouseId = command.WarehouseId,
            CurrencyCode = command.CurrencyCode,
            DiscountAmount = command.DiscountAmount,
            TaxAmount = command.TaxAmount,
            PurchaseOrderId = command.PurchaseOrderId,
            Notes = command.Notes,
            Lines = command.Lines
        }, cancellationToken);
    }

    public async Task<ApplicationResult> UpdateDraftAsync(UpdatePurchaseInvoiceDraftCommand command, CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<ICommandHandler<UpdatePurchaseInvoiceDraftCommand, ApplicationResult>>();
        return await handler.HandleAsync(command, cancellationToken);
    }

    public async Task<ApplicationResult<string>> PostInvoiceAsync(Guid invoiceId, Guid userId, CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<ICommandHandler<PostPurchaseInvoiceCommand, ApplicationResult<string>>>();
        return await handler.HandleAsync(new PostPurchaseInvoiceCommand { InvoiceId = invoiceId, UserId = userId }, cancellationToken);
    }

    public async Task<ApplicationResult> CancelInvoiceAsync(Guid invoiceId, CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<ICommandHandler<CancelPurchaseInvoiceCommand, ApplicationResult>>();
        return await handler.HandleAsync(new CancelPurchaseInvoiceCommand { InvoiceId = invoiceId }, cancellationToken);
    }

    public async Task<ApplicationResult<Guid>> CreateOrderAsync(CreatePurchaseOrderCommand command, CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<ICommandHandler<CreatePurchaseOrderCommand, ApplicationResult<Guid>>>();
        return await handler.HandleAsync(new CreatePurchaseOrderCommand
        {
            CompanyId = CompanyId,
            BranchId = BranchId,
            SupplierId = command.SupplierId,
            OrderDate = command.OrderDate,
            ExpectedDeliveryDate = command.ExpectedDeliveryDate,
            Notes = command.Notes,
            Lines = command.Lines
        }, cancellationToken);
    }

    public async Task<ApplicationResult<Guid>> ConvertOrderToInvoiceAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<ICommandHandler<ConvertPurchaseOrderToInvoiceCommand, ApplicationResult<Guid>>>();
        return await handler.HandleAsync(new ConvertPurchaseOrderToInvoiceCommand
        {
            OrderId = orderId,
            CompanyId = CompanyId,
            BranchId = BranchId
        }, cancellationToken);
    }

    public async Task<ApplicationResult<Guid>> CreateReturnAsync(CreatePurchaseReturnCommand command, CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<ICommandHandler<CreatePurchaseReturnCommand, ApplicationResult<Guid>>>();
        return await handler.HandleAsync(new CreatePurchaseReturnCommand
        {
            CompanyId = CompanyId,
            BranchId = BranchId,
            OriginalInvoiceId = command.OriginalInvoiceId,
            ReturnDate = command.ReturnDate,
            Notes = command.Notes,
            Lines = command.Lines
        }, cancellationToken);
    }

    public async Task<ApplicationResult<string>> PostReturnAsync(Guid returnId, CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<ICommandHandler<PostPurchaseReturnCommand, ApplicationResult<string>>>();
        return await handler.HandleAsync(new PostPurchaseReturnCommand { ReturnId = returnId }, cancellationToken);
    }

    public async Task<ApplicationResult<IReadOnlyList<PurchaseFabricPickDto>>> GetFabricItemsAsync(
        CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var catalog = scope.ServiceProvider.GetRequiredService<IFabricCatalogRepository>();
        var items = await catalog.GetItemsAsync(CompanyId, cancellationToken: cancellationToken);
        return ApplicationResult<IReadOnlyList<PurchaseFabricPickDto>>.Success(
            items.Where(i => i.IsActive).Select(i => new PurchaseFabricPickDto
            {
                Id = i.Id,
                Code = i.Code,
                NameAr = i.NameAr
            }).ToList());
    }

    public async Task<IReadOnlyList<PurchaseFabricColorPickDto>> GetFabricColorsAsync(
        Guid fabricItemId, CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var catalog = scope.ServiceProvider.GetRequiredService<IFabricCatalogRepository>();
        var colors = await catalog.GetColorsForItemAsync(fabricItemId, cancellationToken);
        return colors.Select(c => new PurchaseFabricColorPickDto
        {
            Id = c.Id,
            ColorCode = c.ColorCode,
            NameAr = c.NameAr
        }).ToList();
    }

    public async Task<ApplicationResult<IReadOnlyList<ERPSystem.Application.DTOs.Accounting.AccountListDto>>> GetExpenseAccountsAsync(
        CancellationToken cancellationToken = default)
    {
        return await AccountingUiService.Instance.GetAccountsAsync(accountType: GlAccountType.Expense, cancellationToken: cancellationToken);
    }

    public async Task<ApplicationResult<PurchaseOrderDetailsDto>> GetOrderDetailsAsync(
        Guid id, CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetPurchaseOrderDetailsHandler>();
        return await handler.HandleAsync(new GetPurchaseOrderDetailsQuery { OrderId = id }, cancellationToken);
    }

    public async Task<ApplicationResult<PurchaseReturnDetailsDto>> GetReturnDetailsAsync(
        Guid id, CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetPurchaseReturnDetailsHandler>();
        return await handler.HandleAsync(new GetPurchaseReturnDetailsQuery { ReturnId = id }, cancellationToken);
    }

    public async Task<ApplicationResult<IReadOnlyList<PurchaseInvoicePickDto>>> GetPostedInvoicesForSupplierAsync(
        Guid supplierId, CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetPostedPurchaseInvoicesForSupplierHandler>();
        return await handler.HandleAsync(new GetPostedPurchaseInvoicesForSupplierQuery
        {
            CompanyId = CompanyId,
            SupplierId = supplierId
        }, cancellationToken);
    }

    public async Task<string> NextOrderNumberAsync(CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var numbering = scope.ServiceProvider.GetRequiredService<INumberingService>();
        return await numbering.NextPurchaseOrderNumberAsync(BranchId, cancellationToken);
    }

    public async Task<string> NextReturnNumberAsync(CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var numbering = scope.ServiceProvider.GetRequiredService<INumberingService>();
        return await numbering.NextPurchaseReturnNumberAsync(BranchId, cancellationToken);
    }

    public async Task<ApplicationResult> UpdateOrderAsync(UpdatePurchaseOrderCommand command, CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<ICommandHandler<UpdatePurchaseOrderCommand, ApplicationResult>>();
        return await handler.HandleAsync(command, cancellationToken);
    }

    public async Task<ApplicationResult> SendOrderAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<ICommandHandler<SendPurchaseOrderCommand, ApplicationResult>>();
        return await handler.HandleAsync(new SendPurchaseOrderCommand { OrderId = orderId }, cancellationToken);
    }

    public async Task<ApplicationResult> UpdateReturnDraftAsync(UpdatePurchaseReturnDraftCommand command, CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<ICommandHandler<UpdatePurchaseReturnDraftCommand, ApplicationResult>>();
        return await handler.HandleAsync(command, cancellationToken);
    }

    public async Task<ApplicationResult> CancelOrderAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<ICommandHandler<CancelPurchaseOrderCommand, ApplicationResult>>();
        return await handler.HandleAsync(new CancelPurchaseOrderCommand { OrderId = orderId }, cancellationToken);
    }
}

public static class PurchaseActionRouter
{
    public static void OpenOperationsCenter(PurchaseListRow row) =>
        WorkspaceWindowManager.Instance.OpenAction(
            EntityActionId.OpenOperationsCenter, EntityType.PurchaseInvoice, row, AppModule.Purchases);

    public static void OpenPayment(PurchaseInvoiceDetailsDto invoice)
    {
        PurchaseNavigationContext.BeginPayment(
            invoice.Id, invoice.SupplierId, invoice.RemainingAmount, invoice.InvoiceNumber);
        MockInteractionService.Navigate(AppModule.Accounting, "Payments");
    }
}

public static class PurchaseListRefreshHub
{
    public static event EventHandler? RefreshRequested;
    public static void RequestRefresh() => RefreshRequested?.Invoke(null, EventArgs.Empty);
}
