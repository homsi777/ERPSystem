namespace ERPSystem.Application.DTOs.Catalog;

public sealed class ImportedFabricClassificationDto
{
    public Guid ContainerId { get; init; }
    public string ContainerNumber { get; init; } = "";
    public Guid FabricItemId { get; init; }
    public Guid CategoryId { get; init; }
    public string FabricCode { get; init; } = "";
    public string NameAr { get; init; } = "";
    public string? NameEn { get; init; }
    public Guid FabricColorId { get; init; }
    public string ColorCode { get; init; } = "";
    public string ColorNameAr { get; init; } = "";
    public string? TypeDisplayName { get; init; }
    public int RollCount { get; init; }
    public decimal LengthMeters { get; init; }

    public string DisplayLabel => string.IsNullOrWhiteSpace(TypeDisplayName)
        ? $"{FabricCode} / {ColorNameAr}"
        : TypeDisplayName;
}

public sealed class ImportedFabricContainerFilterDto
{
    public Guid Id { get; init; }
    public string ContainerNumber { get; init; } = "";
    public int FabricTypeCount { get; init; }
}
