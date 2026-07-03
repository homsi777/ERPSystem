using ERPSystem.Application.Abstractions.Repositories;
using ERPSystem.Domain.Entities.Catalog;

namespace ERPSystem.Application.Common;

/// <summary>Creates fabric catalog entries from import files — no manual catalog seeding.</summary>
public static class FabricCatalogImportProvisioner
{
    public const string ImportCategoryCode = "IMPORT";

    public static async Task<(FabricItem Item, FabricColor Color, bool Created)> EnsureAsync(
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

        var (categoryId, categoryCreated) = await EnsureImportCategoryAsync(repository, companyId, cancellationToken);
        var created = categoryCreated;

        var item = await repository.GetItemByCodeAsync(companyId, code, cancellationToken);
        if (item is null)
        {
            item = FabricItem.Create(categoryId, code, code, code);
            await repository.AddItemAsync(item, companyId, cancellationToken);
            created = true;
        }

        var fabricColor = await repository.GetColorForItemAsync(item.Id, color, cancellationToken);
        if (fabricColor is null)
        {
            fabricColor = FabricColor.Create(item.Id, color, color, color);
            await repository.AddColorAsync(fabricColor, cancellationToken);
            created = true;
        }

        return (item, fabricColor, created);
    }

    private static async Task<(Guid CategoryId, bool Created)> EnsureImportCategoryAsync(
        IFabricCatalogRepository repository,
        Guid companyId,
        CancellationToken cancellationToken)
    {
        var categories = await repository.GetCategoriesAsync(companyId, cancellationToken);
        var existing = categories.FirstOrDefault(c =>
            c.Code.Equals(ImportCategoryCode, StringComparison.OrdinalIgnoreCase));
        if (existing is not null)
            return (existing.Id, false);

        var category = FabricCategory.Create(ImportCategoryCode, "مستورد من الصين", "China Import");
        await repository.AddCategoryAsync(category, companyId, cancellationToken);
        return (category.Id, true);
    }
}
