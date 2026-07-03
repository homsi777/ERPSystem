using ERPSystem.Application.Abstractions;
using ERPSystem.Application.Abstractions.Services;
using ERPSystem.Application.Commands.Suppliers;
using ERPSystem.Application.Common;
using ERPSystem.Application.DTOs.Suppliers;
using ERPSystem.Application.Queries.Suppliers;
using ERPSystem.Application.Results;
using ERPSystem.Application.UseCases.Queries;
using ERPSystem.Application.UseCases.Suppliers;
using ERPSystem.Core;
using ERPSystem.Core.Actions;
using ERPSystem.Core.Suppliers;
using ERPSystem.Core.Workspace;
using ERPSystem.Services;
using Microsoft.Extensions.DependencyInjection;

namespace ERPSystem.Services.Suppliers;

public static class SupplierNavigationContext
{
    public static Guid? EditSupplierId { get; set; }
    public static Guid? StatementSupplierId { get; set; }
    public static string? StatementSupplierName { get; set; }

    public static void BeginCreate() => EditSupplierId = null;
    public static void BeginEdit(Guid supplierId) => EditSupplierId = supplierId;

    public static void BeginStatement(Guid supplierId, string supplierName)
    {
        StatementSupplierId = supplierId;
        StatementSupplierName = supplierName;
    }

    public static (Guid? Id, string? Name) TakeStatementContext()
    {
        var id = StatementSupplierId;
        var name = StatementSupplierName;
        StatementSupplierId = null;
        StatementSupplierName = null;
        return (id, name);
    }
}

public sealed class SupplierUiService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ICurrentBranchService _branch;

    public SupplierUiService(IServiceScopeFactory scopeFactory, ICurrentBranchService branch)
    {
        _scopeFactory = scopeFactory;
        _branch = branch;
    }

    public static SupplierUiService Instance => AppServices.GetRequiredService<SupplierUiService>();

    private Guid CompanyId =>
        _branch.CompanyId ?? throw new InvalidOperationException("Company context is not set.");

    public async Task<ApplicationResult<PagedResult<SupplierListDto>>> GetListAsync(
        string? search,
        string? country = null,
        int? paymentTermsDays = null,
        bool? hasBalance = null,
        int page = 1,
        int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetSupplierListHandler>();
        return await handler.HandleAsync(new GetSupplierListQuery
        {
            CompanyId = CompanyId,
            Search = search,
            Country = country,
            PaymentTermsDays = paymentTermsDays,
            HasBalance = hasBalance,
            Page = page,
            PageSize = pageSize
        }, cancellationToken);
    }

    public async Task<ApplicationResult<SupplierDetailsDto>> GetDetailsAsync(
        Guid supplierId, CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetSupplierDetailsHandler>();
        return await handler.HandleAsync(new GetSupplierDetailsQuery { SupplierId = supplierId }, cancellationToken);
    }

    public async Task<ApplicationResult<SupplierOperationsCenterDto>> GetOperationsCenterAsync(
        Guid supplierId, CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetSupplierOperationsCenterHandler>();
        return await handler.HandleAsync(new GetSupplierOperationsCenterQuery { SupplierId = supplierId }, cancellationToken);
    }

    public async Task<ApplicationResult<SupplierStatementDto>> GetStatementAsync(
        Guid supplierId, DateTime? from, DateTime? to, CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetSupplierStatementHandler>();
        return await handler.HandleAsync(new GetSupplierStatementQuery
        {
            SupplierId = supplierId,
            FromDate = from,
            ToDate = to
        }, cancellationToken);
    }

    public async Task<ApplicationResult<IReadOnlyList<SupplierInvoiceListDto>>> GetInvoicesAsync(
        Guid supplierId, CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetSupplierInvoiceListHandler>();
        return await handler.HandleAsync(new GetSupplierInvoiceListQuery { SupplierId = supplierId }, cancellationToken);
    }

    public async Task<string> NextSupplierCodeAsync(CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var numbering = scope.ServiceProvider.GetRequiredService<INumberingService>();
        var branchId = _branch.BranchId ?? throw new InvalidOperationException("Branch context is not set.");
        return await numbering.NextSupplierCodeAsync(branchId, cancellationToken);
    }

    public async Task<ApplicationResult<Guid>> CreateAsync(CreateSupplierCommand command, CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<ICommandHandler<CreateSupplierCommand, ApplicationResult<Guid>>>();
        return await handler.HandleAsync(new CreateSupplierCommand
        {
            CompanyId = CompanyId,
            Code = command.Code,
            NameAr = command.NameAr,
            NameEn = command.NameEn,
            Phone = command.Phone,
            Email = command.Email,
            Address = command.Address,
            Country = command.Country,
            City = command.City,
            CurrencyCode = command.CurrencyCode,
            PaymentTermsDays = command.PaymentTermsDays,
            CreditLimit = command.CreditLimit,
            TaxNumber = command.TaxNumber,
            PayablesAccountId = command.PayablesAccountId,
            Notes = command.Notes
        }, cancellationToken);
    }

    public async Task<ApplicationResult> UpdateAsync(UpdateSupplierCommand command, CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<ICommandHandler<UpdateSupplierCommand, ApplicationResult>>();
        return await handler.HandleAsync(command, cancellationToken);
    }

    public async Task<ApplicationResult> DeactivateAsync(Guid supplierId, CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<ICommandHandler<DeactivateSupplierCommand, ApplicationResult>>();
        return await handler.HandleAsync(new DeactivateSupplierCommand { SupplierId = supplierId }, cancellationToken);
    }

    public async Task<ApplicationResult<SupplierOpeningBalanceResultDto>> PostOpeningBalanceAsync(
        PostSupplierOpeningBalanceCommand command, CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<ICommandHandler<PostSupplierOpeningBalanceCommand, ApplicationResult<SupplierOpeningBalanceResultDto>>>();
        return await handler.HandleAsync(command, cancellationToken);
    }

    public async Task<bool> CanCreateAsync(CancellationToken cancellationToken = default) =>
        await CanAsync("suppliers.create", cancellationToken);

    public async Task<bool> CanDeactivateAsync(CancellationToken cancellationToken = default) =>
        await CanAsync("suppliers.deactivate", cancellationToken);

    public async Task<bool> CanPostOpeningBalanceAsync(CancellationToken cancellationToken = default) =>
        await CanAsync("suppliers.opening-balance", cancellationToken);

    private async Task<bool> CanAsync(string permission, CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var permissions = scope.ServiceProvider.GetRequiredService<IPermissionService>();
        return await permissions.CanAsync(permission, cancellationToken);
    }
}

public static class SupplierActionRouter
{
    public static bool TryHandle(EntityActionId actionId, EntityType entityType, object entityRow, AppModule sourceModule)
    {
        if (entityType != EntityType.Supplier || entityRow is not SupplierListRow row)
            return false;

        switch (actionId)
        {
            case EntityActionId.OpenOperationsCenter:
                OpenOperationsCenter(row);
                return true;
            case EntityActionId.SupplierEdit:
                SupplierNavigationContext.BeginEdit(row.Id);
                MockInteractionService.Navigate(AppModule.Suppliers, "Form");
                return true;
            case EntityActionId.SupplierStatement:
                SupplierNavigationContext.BeginStatement(row.Id, row.NameAr);
                MockInteractionService.Navigate(AppModule.Suppliers, "Statement");
                return true;
            case EntityActionId.SupplierInvoices:
                SupplierNavigationContext.BeginStatement(row.Id, row.NameAr);
                MockInteractionService.Navigate(AppModule.Suppliers, "Invoices");
                return true;
            case EntityActionId.SupplierPayment:
                MockInteractionService.Navigate(AppModule.Accounting, "Payments");
                return true;
            case EntityActionId.SupplierDeactivate:
                _ = DeactivateAsync(row);
                return true;
            default:
                return false;
        }
    }

    public static bool TryHandleQuickAction(string? actionKey, OperationsCenterContext ctx)
    {
        if (ctx.EntityRow is not SupplierListRow row)
            return false;

        switch (actionKey)
        {
            case "form:EditSupplier":
                SupplierNavigationContext.BeginEdit(row.Id);
                MockInteractionService.Navigate(AppModule.Suppliers, "Form");
                return true;
            case "nav:Accounting:Payments":
                MockInteractionService.Navigate(AppModule.Accounting, "Payments");
                return true;
            case "ws:DeactivateSupplier":
                _ = DeactivateAsync(row);
                return true;
            default:
                return false;
        }
    }

    public static void OpenOperationsCenter(SupplierListRow row) =>
        WorkspaceWindowManager.Instance.OpenAction(
            EntityActionId.OpenOperationsCenter, EntityType.Supplier, row, AppModule.Suppliers);

    private static async Task DeactivateAsync(SupplierListRow row)
    {
        if (!await SupplierUiService.Instance.CanDeactivateAsync())
        {
            ApplicationResultPresenter.Present(ApplicationResult.PermissionDenied("Not allowed."));
            return;
        }

        var result = await SupplierUiService.Instance.DeactivateAsync(row.Id);
        if (ApplicationResultPresenter.Present(result))
            SupplierListRefreshHub.RequestRefresh();
    }
}

public static class SupplierListRefreshHub
{
    public static event EventHandler? RefreshRequested;
    public static void RequestRefresh() => RefreshRequested?.Invoke(null, EventArgs.Empty);
}
