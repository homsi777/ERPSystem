namespace ERPSystem.Application.Commands.Catalog;

public sealed record CreateFabricCategoryCommand(Guid CompanyId, string Code, string NameAr, string? NameEn);
public sealed record UpdateFabricCategoryCommand(Guid CategoryId, string NameAr, string? NameEn);
public sealed record DeactivateFabricCategoryCommand(Guid CategoryId);

public sealed record CreateFabricItemCommand(Guid CompanyId, Guid CategoryId, string Code, string NameAr, string? NameEn);
public sealed record UpdateFabricItemCommand(Guid ItemId, Guid CategoryId, string NameAr, string? NameEn);
public sealed record DeactivateFabricItemCommand(Guid ItemId);

public sealed record CreateFabricColorCommand(Guid FabricItemId, string Code, string NameAr, string? NameEn);
public sealed record UpdateFabricColorCommand(Guid ColorId, string NameAr, string? NameEn);
public sealed record DeactivateFabricColorCommand(Guid ColorId);
