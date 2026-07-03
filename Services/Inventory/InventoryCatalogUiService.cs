using ERPSystem.Application.Abstractions;
using ERPSystem.Application.Abstractions.Services;
using ERPSystem.Application.Commands.Catalog;
using ERPSystem.Application.DTOs.Catalog;
using ERPSystem.Application.Queries.Catalog;
using ERPSystem.Application.Results;
using ERPSystem.Application.UseCases.Catalog;
using Microsoft.Extensions.DependencyInjection;

namespace ERPSystem.Services.Inventory;

public static class InventoryCatalogNavigationContext
{
    public static ImportedFabricClassificationDto? EditClassification { get; set; }

    public static void BeginEditClassification(ImportedFabricClassificationDto row) =>
        EditClassification = row;
}

public static class InventoryCatalogListRefreshHub
{
    public static event EventHandler? RefreshRequested;
    public static void RequestRefresh() => RefreshRequested?.Invoke(null, EventArgs.Empty);
}

public sealed class InventoryCatalogUiService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ICurrentBranchService _branch;

    public InventoryCatalogUiService(IServiceScopeFactory scopeFactory, ICurrentBranchService branch)
    {
        _scopeFactory = scopeFactory;
        _branch = branch;
    }

    public static InventoryCatalogUiService Instance => AppServices.GetRequiredService<InventoryCatalogUiService>();

    private Guid CompanyId =>
        _branch.CompanyId ?? throw new InvalidOperationException("Company context is not set.");

    public async Task<ApplicationResult<IReadOnlyList<FabricCategoryListDto>>> GetCategoriesAsync(
        CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetFabricCategoryListHandler>();
        return await handler.HandleAsync(new GetFabricCategoryListQuery(CompanyId), cancellationToken);
    }

    public async Task<ApplicationResult<IReadOnlyList<FabricItemListDto>>> GetItemsAsync(
        Guid? categoryId = null, string? search = null, CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetFabricItemListHandler>();
        return await handler.HandleAsync(new GetFabricItemListQuery(CompanyId, categoryId, search), cancellationToken);
    }

    public async Task<ApplicationResult<IReadOnlyList<FabricColorListDto>>> GetColorsAsync(
        Guid fabricItemId, CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetFabricColorListHandler>();
        return await handler.HandleAsync(new GetFabricColorListQuery(fabricItemId), cancellationToken);
    }

    public async Task<ApplicationResult<IReadOnlyList<ImportedFabricClassificationDto>>> GetImportedClassificationsAsync(
        Guid? containerId = null, CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetImportedFabricClassificationListHandler>();
        return await handler.HandleAsync(new GetImportedFabricClassificationListQuery(CompanyId, containerId), cancellationToken);
    }

    public async Task<ApplicationResult<IReadOnlyList<ImportedFabricContainerFilterDto>>> GetImportedContainerFiltersAsync(
        CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetImportedFabricContainerFiltersHandler>();
        return await handler.HandleAsync(new GetImportedFabricContainerFiltersQuery(CompanyId), cancellationToken);
    }

    public async Task<ApplicationResult> UpdateItemAsync(
        UpdateFabricItemCommand command, CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<ICommandHandler<UpdateFabricItemCommand, ApplicationResult>>();
        return await handler.HandleAsync(command, cancellationToken);
    }

    public async Task<ApplicationResult> UpdateColorAsync(
        UpdateFabricColorCommand command, CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<ICommandHandler<UpdateFabricColorCommand, ApplicationResult>>();
        return await handler.HandleAsync(command, cancellationToken);
    }
}
