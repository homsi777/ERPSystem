using ERPSystem.Application.Abstractions.Repositories;
using ERPSystem.Domain.Entities.Catalog;

namespace ERPSystem.Application.Common;

/// <summary>
/// Resolves manually typed opening-stock fabric code/name/color to catalog IDs within the same unit of work.
/// Newly typed items are grouped under a dedicated opening-balance category.
/// </summary>
public sealed class OpeningBalanceFabricCatalogBatch
{
    public const string CategoryCode = "OPENING_BALANCE";

    private readonly Dictionary<string, FabricItem> _itemsByCode = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, FabricColor> _colorsByKey = new(StringComparer.OrdinalIgnoreCase);
    private Guid? _categoryId;

    public async Task<(FabricItem Item, FabricColor Color)> EnsureAsync(
        IFabricCatalogRepository repository,
        Guid companyId,
        string fabricCode,
        string itemName,
        string colorName,
        CancellationToken cancellationToken = default)
    {
        var code = PackingListCatalogNormalizer.NormalizeFabricCode(fabricCode);
        var normalizedName = Normalize(itemName);
        var normalizedColor = Normalize(colorName);
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Fabric code is required.", nameof(fabricCode));
        if (string.IsNullOrWhiteSpace(normalizedColor))
            throw new ArgumentException("Color name is required.", nameof(colorName));

        var displayName = string.IsNullOrWhiteSpace(normalizedName) ? code : normalizedName;
        var colorCacheKey = BuildColorCacheKey(code, normalizedColor);

        if (_colorsByKey.TryGetValue(colorCacheKey, out var cachedColor) &&
            _itemsByCode.TryGetValue(code, out var cachedItem))
            return (cachedItem, cachedColor);

        FabricItem item;
        if (_itemsByCode.TryGetValue(code, out var memoryItem))
        {
            item = memoryItem;
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
                var categoryId = await EnsureCategoryAsync(repository, companyId, cancellationToken);
                item = FabricItem.Create(categoryId, code, displayName, displayName);
                await repository.AddItemAsync(item, companyId, cancellationToken);
            }

            _itemsByCode[code] = item;
        }

        if (_colorsByKey.TryGetValue(colorCacheKey, out var memoryColor))
            return (item, memoryColor);

        var fabricColor = await repository.GetColorForItemAsync(item.Id, normalizedColor, cancellationToken);
        if (fabricColor is null)
        {
            fabricColor = FabricColor.Create(item.Id, normalizedColor, normalizedColor, normalizedColor);
            await repository.AddColorAsync(fabricColor, cancellationToken);
        }

        _colorsByKey[colorCacheKey] = fabricColor;
        return (item, fabricColor);
    }

    private async Task<Guid> EnsureCategoryAsync(
        IFabricCatalogRepository repository,
        Guid companyId,
        CancellationToken cancellationToken)
    {
        if (_categoryId.HasValue)
            return _categoryId.Value;

        var existing = (await repository.GetCategoriesAsync(companyId, cancellationToken))
            .FirstOrDefault(c => c.Code.Equals(CategoryCode, StringComparison.OrdinalIgnoreCase));
        if (existing is not null)
        {
            _categoryId = existing.Id;
            return existing.Id;
        }

        var category = FabricCategory.Create(CategoryCode, "مواد أول المدة", "Opening balance materials");
        await repository.AddCategoryAsync(category, companyId, cancellationToken);
        _categoryId = category.Id;
        return category.Id;
    }

    private static string BuildColorCacheKey(string fabricCode, string color) =>
        $"{fabricCode.Trim().ToUpperInvariant()}|{color.Trim().ToUpperInvariant()}";

    private static string Normalize(string value) =>
        PackingListCatalogNormalizer.CollapseWhitespace(value);
}
