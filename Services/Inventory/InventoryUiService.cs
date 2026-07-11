using ERPSystem.Application.Abstractions;
using ERPSystem.Application.Abstractions.Repositories;
using ERPSystem.Application.Abstractions.Services;
using ERPSystem.Application.Commands.Inventory;
using ERPSystem.Application.DTOs.Inventory;
using ERPSystem.Application.Queries.Inventory;
using ERPSystem.Application.Results;
using ERPSystem.Application.UseCases.Inventory;
using ERPSystem.Domain.Entities.Catalog;
using Microsoft.Extensions.DependencyInjection;

namespace ERPSystem.Services.Inventory;

public static class InventoryNavigationContext
{
    public static Guid? EditWarehouseId { get; set; }
    public static Guid? WorkspaceWarehouseId { get; set; }
    public static string? WorkspaceInitialTab { get; set; }
    public static Guid? EditTransferId { get; set; }
    public static Guid? EditStocktakeId { get; set; }
    public static Guid? PreselectedFromWarehouseId { get; set; }
    public static Guid? PreselectedStocktakeWarehouseId { get; set; }
    public static Guid? EditOpeningStockId { get; set; }
    public static Guid? PreselectedOpeningWarehouseId { get; set; }

    public static void BeginCreate() => EditWarehouseId = null;
    public static void BeginEdit(Guid warehouseId) => EditWarehouseId = warehouseId;

    public static void BeginWorkspace(Guid warehouseId, string? tab = null)
    {
        WorkspaceWarehouseId = warehouseId;
        WorkspaceInitialTab = tab;
    }

    public static void BeginCreateTransfer(Guid? fromWarehouseId = null)
    {
        EditTransferId = null;
        PreselectedFromWarehouseId = fromWarehouseId;
    }

    public static void BeginEditTransfer(Guid transferId) => EditTransferId = transferId;

    public static void BeginEditStocktake(Guid sessionId) => EditStocktakeId = sessionId;

    public static void BeginCreateStocktake(Guid? warehouseId = null)
    {
        PreselectedStocktakeWarehouseId = warehouseId;
    }

    public static void BeginCreateOpeningStock(Guid? warehouseId = null)
    {
        EditOpeningStockId = null;
        PreselectedOpeningWarehouseId = warehouseId;
    }

    public static (Guid? Id, string? Tab) TakeWorkspaceContext()
    {
        var id = WorkspaceWarehouseId;
        var tab = WorkspaceInitialTab;
        WorkspaceWarehouseId = null;
        WorkspaceInitialTab = null;
        return (id, tab);
    }

    public static Guid? TakePreselectedFromWarehouse() =>
        PreselectedFromWarehouseId is Guid id ? (PreselectedFromWarehouseId = null, id).id : null;

    public static Guid? TakePreselectedStocktakeWarehouse() =>
        PreselectedStocktakeWarehouseId is Guid id ? (PreselectedStocktakeWarehouseId = null, id).id : null;

    public static Guid? TakePreselectedOpeningWarehouse() =>
        PreselectedOpeningWarehouseId is Guid id ? (PreselectedOpeningWarehouseId = null, id).id : null;
}

public static class InventoryListRefreshHub
{
    public static event EventHandler? RefreshRequested;
    public static void RequestRefresh() => RefreshRequested?.Invoke(null, EventArgs.Empty);
}

public sealed class InventoryUiService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ICurrentBranchService _branch;

    public InventoryUiService(IServiceScopeFactory scopeFactory, ICurrentBranchService branch)
    {
        _scopeFactory = scopeFactory;
        _branch = branch;
    }

    public static InventoryUiService Instance => AppServices.GetRequiredService<InventoryUiService>();

    private Guid BranchId =>
        _branch.BranchId ?? throw new InvalidOperationException("Branch context is not set.");

    public async Task<ApplicationResult<IReadOnlyList<WarehouseListExtendedDto>>> GetWarehousesAsync(
        CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetInventoryWarehouseListHandler>();
        return await handler.HandleAsync(new GetInventoryWarehouseListQuery(BranchId), cancellationToken);
    }

    public async Task<ApplicationResult<InventoryOperationsCenterDto>> GetOperationsCenterAsync(
        Guid warehouseId, CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetInventoryOperationsCenterHandler>();
        return await handler.HandleAsync(new GetInventoryWarehouseOperationsCenterQuery(warehouseId), cancellationToken);
    }

    public async Task<ApplicationResult<InventoryDashboardDto>> GetDashboardAsync(
        CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetInventoryDashboardHandler>();
        return await handler.HandleAsync(new GetInventoryDashboardQuery(BranchId), cancellationToken);
    }

    public async Task<ApplicationResult<IReadOnlyList<FabricStockBalanceDto>>> GetFabricStockAsync(
        Guid? warehouseId = null,
        string? search = null,
        CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetFabricStockBalancesHandler>();
        return await handler.HandleAsync(
            new GetFabricStockBalancesQuery(BranchId, warehouseId, search),
            cancellationToken);
    }

    public async Task<ApplicationResult<IReadOnlyList<FabricRollListDto>>> GetFabricRollsByStockAsync(
        Guid warehouseId, Guid containerId, Guid fabricItemId, Guid fabricColorId,
        CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetFabricRollsByStockHandler>();
        return await handler.HandleAsync(
            new GetFabricRollsByStockQuery(warehouseId, containerId, fabricItemId, fabricColorId), cancellationToken);
    }

    public async Task<ApplicationResult<PaginatedFabricRollDto>> GetFabricRollsPageAsync(
        Guid warehouseId,
        int pageNumber = 1,
        int pageSize = 50,
        int? status = null,
        string? search = null,
        CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetFabricRollsPageHandler>();
        return await handler.HandleAsync(
            new GetFabricRollsPageQuery(warehouseId, pageNumber, pageSize, status, search),
            cancellationToken);
    }

    public async Task<ApplicationResult<IReadOnlyList<StockTransferListDto>>> GetTransfersAsync(
        CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetStockTransfersHandler>();
        return await handler.HandleAsync(new GetStockTransfersQuery(BranchId), cancellationToken);
    }

    public async Task<ApplicationResult<IReadOnlyList<StocktakeListDto>>> GetStocktakeSessionsAsync(
        CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetStocktakeSessionsHandler>();
        return await handler.HandleAsync(new GetStocktakeSessionsQuery(BranchId), cancellationToken);
    }

    public async Task<ApplicationResult<IReadOnlyList<OpeningStockListDto>>> GetOpeningStockDocumentsAsync(
        CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetOpeningStockDocumentsHandler>();
        return await handler.HandleAsync(new GetOpeningStockDocumentsQuery(BranchId), cancellationToken);
    }

    public async Task<ApplicationResult<Guid>> CreateWarehouseAsync(
        CreateWarehouseCommand command, CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<ICommandHandler<CreateWarehouseCommand, ApplicationResult<Guid>>>();
        var result = await handler.HandleAsync(command with { BranchId = BranchId }, cancellationToken);
        if (result.IsSuccess)
            ERPSystem.Services.Settings.ReferenceDataCatalog.InvalidateWarehouses();
        return result;
    }

    public async Task<ApplicationResult> UpdateWarehouseAsync(
        UpdateWarehouseCommand command, CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<ICommandHandler<UpdateWarehouseCommand, ApplicationResult>>();
        var result = await handler.HandleAsync(command, cancellationToken);
        if (result.IsSuccess)
            ERPSystem.Services.Settings.ReferenceDataCatalog.InvalidateWarehouses();
        return result;
    }

    public async Task<ApplicationResult> DeactivateWarehouseAsync(
        Guid warehouseId, CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<ICommandHandler<DeactivateWarehouseCommand, ApplicationResult>>();
        return await handler.HandleAsync(new DeactivateWarehouseCommand(warehouseId), cancellationToken);
    }

    public async Task<ApplicationResult> ActivateWarehouseAsync(
        Guid warehouseId, CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<ICommandHandler<ActivateWarehouseCommand, ApplicationResult>>();
        return await handler.HandleAsync(new ActivateWarehouseCommand(warehouseId), cancellationToken);
    }

    public async Task<ApplicationResult> ArchiveWarehouseAsync(
        Guid warehouseId, CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<ICommandHandler<ArchiveWarehouseCommand, ApplicationResult>>();
        return await handler.HandleAsync(new ArchiveWarehouseCommand(warehouseId), cancellationToken);
    }

    public async Task<ApplicationResult<Guid>> DuplicateWarehouseAsync(
        Guid warehouseId, CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<ICommandHandler<DuplicateWarehouseCommand, ApplicationResult<Guid>>>();
        return await handler.HandleAsync(new DuplicateWarehouseCommand(warehouseId), cancellationToken);
    }

    public async Task<ApplicationResult<WarehouseDetailDto>> GetWarehouseDetailAsync(
        Guid warehouseId, CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetInventoryWarehouseDetailHandler>();
        return await handler.HandleAsync(new GetInventoryWarehouseDetailQuery(warehouseId), cancellationToken);
    }

    public async Task<ApplicationResult<IReadOnlyList<FabricItem>>> GetFabricCatalogAsync(
        string? search = null, CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var branch = scope.ServiceProvider.GetRequiredService<ICurrentBranchService>();
        var catalog = scope.ServiceProvider.GetRequiredService<IFabricCatalogRepository>();
        var companyId = branch.CompanyId ?? Guid.Empty;
        var items = await catalog.GetItemsAsync(companyId, search, cancellationToken);
        return ApplicationResult<IReadOnlyList<FabricItem>>.Success(items);
    }

    public async Task<ApplicationResult<IReadOnlyList<FabricColor>>> GetFabricColorsAsync(
        Guid fabricItemId, CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var catalog = scope.ServiceProvider.GetRequiredService<IFabricCatalogRepository>();
        var colors = await catalog.GetColorsForItemAsync(fabricItemId, cancellationToken);
        return ApplicationResult<IReadOnlyList<FabricColor>>.Success(colors);
    }

    public async Task<ApplicationResult<IReadOnlyList<WarehouseTransferRollDto>>> GetTransferableRollsAsync(
        Guid warehouseId, CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetWarehouseTransferRollsHandler>();
        return await handler.HandleAsync(new GetWarehouseTransferRollsQuery(warehouseId), cancellationToken);
    }

    public async Task<ApplicationResult<StockTransferDetailDto>> GetTransferDetailAsync(
        Guid transferId, CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetStockTransferDetailHandler>();
        return await handler.HandleAsync(new GetStockTransferDetailQuery(transferId), cancellationToken);
    }

    public async Task<ApplicationResult> ApproveTransferAsync(
        Guid transferId, CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<ICommandHandler<ApproveStockTransferCommand, ApplicationResult>>();
        return await handler.HandleAsync(new ApproveStockTransferCommand(transferId), cancellationToken);
    }

    public async Task<ApplicationResult<StocktakeDetailDto>> GetStocktakeDetailAsync(
        Guid sessionId, CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetStocktakeDetailHandler>();
        return await handler.HandleAsync(new GetStocktakeDetailQuery(sessionId), cancellationToken);
    }

    public async Task<ApplicationResult> UpdateStocktakeLinesAsync(
        UpdateStocktakeLinesCommand command, CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<ICommandHandler<UpdateStocktakeLinesCommand, ApplicationResult>>();
        return await handler.HandleAsync(command, cancellationToken);
    }

    public async Task<ApplicationResult<Guid>> CreateTransferAsync(
        CreateStockTransferCommand command, CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<ICommandHandler<CreateStockTransferCommand, ApplicationResult<Guid>>>();
        return await handler.HandleAsync(command with { BranchId = BranchId }, cancellationToken);
    }

    public async Task<ApplicationResult> CompleteTransferAsync(
        Guid transferId, CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<ICommandHandler<CompleteStockTransferCommand, ApplicationResult>>();
        return await handler.HandleAsync(new CompleteStockTransferCommand(transferId), cancellationToken);
    }

    public async Task<ApplicationResult<Guid>> CreateStocktakeAsync(
        CreateStocktakeCommand command, CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<ICommandHandler<CreateStocktakeCommand, ApplicationResult<Guid>>>();
        return await handler.HandleAsync(command with { BranchId = BranchId }, cancellationToken);
    }

    public async Task<ApplicationResult> PostStocktakeAsync(
        Guid sessionId, CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<ICommandHandler<PostStocktakeCommand, ApplicationResult>>();
        return await handler.HandleAsync(new PostStocktakeCommand(sessionId), cancellationToken);
    }

    public async Task<ApplicationResult<Guid>> CreateOpeningStockAsync(
        CreateOpeningStockCommand command, CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<ICommandHandler<CreateOpeningStockCommand, ApplicationResult<Guid>>>();
        return await handler.HandleAsync(command with { BranchId = BranchId }, cancellationToken);
    }

    public async Task<ApplicationResult> PostOpeningStockAsync(
        Guid documentId, CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<ICommandHandler<PostOpeningStockCommand, ApplicationResult>>();
        return await handler.HandleAsync(new PostOpeningStockCommand(documentId), cancellationToken);
    }
}
