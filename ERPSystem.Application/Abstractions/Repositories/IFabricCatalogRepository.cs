using ERPSystem.Application.DTOs.Catalog;
using ERPSystem.Domain.Entities.Catalog;

namespace ERPSystem.Application.Abstractions.Repositories;

public interface IFabricCatalogRepository
{
    Task<FabricItem?> GetItemByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<FabricColor?> GetColorByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyDictionary<Guid, FabricItem>> GetItemsByIdsAsync(
        IEnumerable<Guid> ids, CancellationToken cancellationToken = default);
    Task<IReadOnlyDictionary<Guid, FabricColor>> GetColorsByIdsAsync(
        IEnumerable<Guid> ids, CancellationToken cancellationToken = default);
    Task<FabricCategory?> GetCategoryByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<FabricItem>> GetItemsAsync(
        Guid companyId,
        string? search = null,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<FabricCategory>> GetCategoriesAsync(
        Guid companyId,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<FabricCategoryListDto>> GetCategoryListAsync(
        Guid companyId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<FabricItemListDto>> GetItemListAsync(
        Guid companyId, Guid? categoryId = null, string? search = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<FabricColorListDto>> GetColorListAsync(
        Guid fabricItemId, CancellationToken cancellationToken = default);
    Task<FabricItem?> GetItemByCodeAsync(
        Guid companyId,
        string code,
        CancellationToken cancellationToken = default);
    Task<FabricColor?> GetColorForItemAsync(
        Guid fabricItemId,
        string colorCodeOrName,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<FabricColor>> GetColorsForItemAsync(
        Guid fabricItemId,
        CancellationToken cancellationToken = default);
    Task<bool> CategoryCodeExistsAsync(
        Guid companyId, string code, Guid? excludeId = null, CancellationToken cancellationToken = default);
    Task<bool> ItemCodeExistsAsync(
        Guid companyId, string code, Guid? excludeId = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ImportedFabricClassificationDto>> GetImportedClassificationsAsync(
        Guid companyId, Guid? containerId = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ImportedFabricContainerFilterDto>> GetImportedFabricContainerFiltersAsync(
        Guid companyId, CancellationToken cancellationToken = default);
    Task AddCategoryAsync(FabricCategory category, Guid companyId, CancellationToken cancellationToken = default);
    Task UpdateCategoryAsync(FabricCategory category, CancellationToken cancellationToken = default);
    Task AddItemAsync(FabricItem item, Guid companyId, CancellationToken cancellationToken = default);
    Task UpdateItemAsync(FabricItem item, CancellationToken cancellationToken = default);
    Task AddColorAsync(FabricColor color, CancellationToken cancellationToken = default);
    Task UpdateColorAsync(FabricColor color, CancellationToken cancellationToken = default);
}
