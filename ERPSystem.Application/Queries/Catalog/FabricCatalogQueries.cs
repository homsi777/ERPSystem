namespace ERPSystem.Application.Queries.Catalog;

public sealed record GetFabricCategoryListQuery(Guid CompanyId);
public sealed record GetFabricItemListQuery(Guid CompanyId, Guid? CategoryId = null, string? Search = null);
public sealed record GetFabricColorListQuery(Guid FabricItemId);
public sealed record GetImportedFabricClassificationListQuery(Guid CompanyId, Guid? ContainerId = null);
public sealed record GetImportedFabricContainerFiltersQuery(Guid CompanyId);
