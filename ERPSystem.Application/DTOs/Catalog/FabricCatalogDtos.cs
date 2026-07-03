namespace ERPSystem.Application.DTOs.Catalog;

public sealed class FabricCategoryListDto
{
    public Guid Id { get; init; }
    public string Code { get; init; } = "";
    public string NameAr { get; init; } = "";
    public string? NameEn { get; init; }
    public int ItemCount { get; init; }
    public bool IsActive { get; init; }
    public string StatusDisplay => IsActive ? "نشط" : "معطل";
}

public sealed class FabricItemListDto
{
    public Guid Id { get; init; }
    public Guid CategoryId { get; init; }
    public string CategoryName { get; init; } = "";
    public string Code { get; init; } = "";
    public string NameAr { get; init; } = "";
    public string? NameEn { get; init; }
    public int ColorCount { get; init; }
    public bool IsActive { get; init; }
    public string StatusDisplay => IsActive ? "نشط" : "معطل";
}

public sealed class FabricColorListDto
{
    public Guid Id { get; init; }
    public Guid FabricItemId { get; init; }
    public string Code { get; init; } = "";
    public string NameAr { get; init; } = "";
    public string? NameEn { get; init; }
    public bool IsActive { get; init; }
}
