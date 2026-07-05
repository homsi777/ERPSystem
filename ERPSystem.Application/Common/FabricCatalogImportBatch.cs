using ERPSystem.Application.Abstractions.Repositories;
using ERPSystem.Domain.Entities.Catalog;

namespace ERPSystem.Application.Common;

/// <summary>
/// Resolves fabric catalog entries during a single DPL import without duplicate inserts
/// before SaveChanges (repository lookups use AsNoTracking and cannot see pending rows).
/// </summary>
public sealed class FabricCatalogImportBatch
{
    private readonly Dictionary<string, FabricItem> _itemsByCode = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, FabricColor> _colorsByKey = new(StringComparer.OrdinalIgnoreCase);
    private Guid? _categoryId;
    private bool _hasPendingChanges;

    public bool HasPendingChanges => _hasPendingChanges;

    public async Task<(FabricItem Item, FabricColor Color, bool Created)> EnsureAsync(
        IFabricCatalogRepository repository,
        Guid companyId,
        string fabricCode,
        string colorKey,
        CancellationToken cancellationToken = default)
    {
        var code = fabricCode.Trim();
        var color = colorKey.Trim();
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Fabric code is required.", nameof(fabricCode));
        if (string.IsNullOrWhiteSpace(color))
            throw new ArgumentException("Color is required.", nameof(colorKey));

        var colorCacheKey = BuildColorCacheKey(code, color);
        if (_colorsByKey.TryGetValue(colorCacheKey, out var cachedColor) &&
            _itemsByCode.TryGetValue(code, out var cachedItem))
            return (cachedItem, cachedColor, false);

        var created = false;
        var categoryId = await EnsureImportCategoryAsync(repository, companyId, cancellationToken);

        FabricItem item;
        if (_itemsByCode.TryGetValue(code, out var cachedFabric))
        {
            item = cachedFabric;
        }
        else
        {
            var fromDb = await repository.GetItemByCodeAsync(companyId, code, cancellationToken);
            if (fromDb is not null)
            {
                item = fromDb;
            }
            else
            {
                item = FabricItem.Create(categoryId, code, code, code);
                await repository.AddItemAsync(item, companyId, cancellationToken);
                created = true;
                _hasPendingChanges = true;
            }

            _itemsByCode[code] = item;
        }

        if (_colorsByKey.TryGetValue(colorCacheKey, out var memoryColor))
            return (item, memoryColor, created);

        var fabricColor = await repository.GetColorForItemAsync(item.Id, color, cancellationToken);
        if (fabricColor is null)
        {
            fabricColor = FabricColor.Create(item.Id, color, color, color);
            await repository.AddColorAsync(fabricColor, cancellationToken);
            created = true;
            _hasPendingChanges = true;
        }

        _colorsByKey[colorCacheKey] = fabricColor;
        return (item, fabricColor, created);
    }

    private async Task<Guid> EnsureImportCategoryAsync(
        IFabricCatalogRepository repository,
        Guid companyId,
        CancellationToken cancellationToken)
    {
        if (_categoryId.HasValue)
            return _categoryId.Value;

        var categories = await repository.GetCategoriesAsync(companyId, cancellationToken);
        var existing = categories.FirstOrDefault(c =>
            c.Code.Equals(FabricCatalogImportProvisioner.ImportCategoryCode, StringComparison.OrdinalIgnoreCase));
        if (existing is not null)
        {
            _categoryId = existing.Id;
            return existing.Id;
        }

        var category = FabricCategory.Create(
            FabricCatalogImportProvisioner.ImportCategoryCode,
            "مستورد من الصين",
            "China Import");
        await repository.AddCategoryAsync(category, companyId, cancellationToken);
        _categoryId = category.Id;
        _hasPendingChanges = true;
        return category.Id;
    }

    private static string BuildColorCacheKey(string fabricCode, string color) =>
        $"{fabricCode.Trim().ToUpperInvariant()}|{color.Trim().ToUpperInvariant()}";
}
