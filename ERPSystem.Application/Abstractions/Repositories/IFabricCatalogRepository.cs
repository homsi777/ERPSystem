using ERPSystem.Domain.Entities.Catalog;

namespace ERPSystem.Application.Abstractions.Repositories;

public interface IFabricCatalogRepository
{
    Task<FabricItem?> GetItemByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<FabricColor?> GetColorByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<FabricItem>> GetItemsAsync(
        Guid companyId,
        string? search = null,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<FabricCategory>> GetCategoriesAsync(
        Guid companyId,
        CancellationToken cancellationToken = default);
    Task<FabricItem?> GetItemByCodeAsync(
        Guid companyId,
        string code,
        CancellationToken cancellationToken = default);
    Task<FabricColor?> GetColorForItemAsync(
        Guid fabricItemId,
        string colorCodeOrName,
        CancellationToken cancellationToken = default);
}
