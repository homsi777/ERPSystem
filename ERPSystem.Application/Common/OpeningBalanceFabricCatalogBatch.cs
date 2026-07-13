using ERPSystem.Application.Abstractions.Repositories;
using ERPSystem.Domain.Entities.Catalog;

namespace ERPSystem.Application.Common;

/// <summary>
/// Resolves manually typed opening-stock names to catalog IDs within the same unit of work.
/// Newly typed names are grouped under a dedicated opening-balance category.
/// </summary>
public sealed class OpeningBalanceFabricCatalogBatch
{
    public const string CategoryCode = "OPENING_BALANCE";

    private readonly Dictionary<string, FabricItem> _items = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<Guid, Dictionary<string, FabricColor>> _colors = [];
    private Guid? _categoryId;
    private bool _itemsLoaded;

    public async Task<(FabricItem Item, FabricColor Color)> EnsureAsync(
        IFabricCatalogRepository repository,
        Guid companyId,
        string itemName,
        string colorName,
        CancellationToken cancellationToken = default)
    {
        var normalizedItem = Normalize(itemName);
        var normalizedColor = Normalize(colorName);
        if (string.IsNullOrWhiteSpace(normalizedItem))
            throw new ArgumentException("Fabric name is required.", nameof(itemName));
        if (string.IsNullOrWhiteSpace(normalizedColor))
            throw new ArgumentException("Color name is required.", nameof(colorName));

        await LoadItemsAsync(repository, companyId, cancellationToken);

        if (!_items.TryGetValue(normalizedItem, out var item))
        {
            var categoryId = await EnsureCategoryAsync(repository, companyId, cancellationToken);
            item = FabricItem.Create(categoryId, normalizedItem, normalizedItem, normalizedItem);
            await repository.AddItemAsync(item, companyId, cancellationToken);
            IndexItem(item);
        }

        if (!_colors.TryGetValue(item.Id, out var colors))
        {
            var existing = await repository.GetColorsForItemAsync(item.Id, cancellationToken);
            colors = new Dictionary<string, FabricColor>(StringComparer.OrdinalIgnoreCase);
            foreach (var color in existing)
                IndexColor(colors, color);
            _colors[item.Id] = colors;
        }

        if (!colors.TryGetValue(normalizedColor, out var fabricColor))
        {
            fabricColor = FabricColor.Create(item.Id, normalizedColor, normalizedColor, normalizedColor);
            await repository.AddColorAsync(fabricColor, cancellationToken);
            IndexColor(colors, fabricColor);
        }

        return (item, fabricColor);
    }

    private async Task LoadItemsAsync(
        IFabricCatalogRepository repository,
        Guid companyId,
        CancellationToken cancellationToken)
    {
        if (_itemsLoaded)
            return;

        foreach (var item in await repository.GetItemsAsync(companyId, cancellationToken: cancellationToken))
            IndexItem(item);
        _itemsLoaded = true;
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

    private void IndexItem(FabricItem item)
    {
        _items[Normalize(item.Code)] = item;
        _items[Normalize(item.NameAr)] = item;
        if (!string.IsNullOrWhiteSpace(item.NameEn))
            _items[Normalize(item.NameEn)] = item;
    }

    private static void IndexColor(Dictionary<string, FabricColor> colors, FabricColor color)
    {
        colors[Normalize(color.ColorCode)] = color;
        colors[Normalize(color.NameAr)] = color;
        if (!string.IsNullOrWhiteSpace(color.NameEn))
            colors[Normalize(color.NameEn)] = color;
    }

    private static string Normalize(string value) =>
        PackingListCatalogNormalizer.CollapseWhitespace(value);
}
