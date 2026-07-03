namespace ERPSystem.Application.DTOs.Containers;

public sealed class FabricTypeAliasDto
{
    public Guid Id { get; init; }
    public Guid SupplierId { get; init; }
    public Guid FabricItemId { get; init; }
    public Guid FabricColorId { get; init; }
    public string DplMatchKey { get; init; } = "";
    public string InvoiceDescriptionMatchKey { get; init; } = "";
    public string InvoiceDescription { get; init; } = "";
}

public sealed class ChinaImportUnmatchedDplGroupDto
{
    public string DplMatchKey { get; init; } = "";
    public int GroupIndex { get; init; }
    public string FabricCode { get; init; } = "";
    public string Color { get; init; } = "";
    public Guid? FabricItemId { get; init; }
    public Guid? FabricColorId { get; init; }
    public int RollCount { get; init; }
    public decimal LengthMeters { get; init; }
    public string? SuggestedInvoiceMatchKey { get; init; }
    public string? SuggestedInvoiceDescription { get; init; }
    public int SuggestionScore { get; init; }

    public string DisplayLabel => $"{FabricCode} / {Color} ({RollCount} توب، {LengthMeters:N0} م)";
    public bool HasSuggestion => !string.IsNullOrWhiteSpace(SuggestedInvoiceMatchKey);
}

public sealed class ChinaImportInvoiceLinkOptionDto
{
    public string MatchKey { get; init; } = "";
    public string Description { get; init; } = "";
    public int RollCount { get; init; }
    public decimal LengthMeters { get; init; }

    public string Display => $"{Description} ({RollCount} توب، {LengthMeters:N0} م)";
}

public sealed class ChinaImportMatchContext
{
    public Guid SupplierId { get; init; }
    public IReadOnlyList<FabricTypeAliasDto> PersistedAliases { get; init; } = [];
    public IReadOnlyDictionary<string, string> SessionDplToInvoiceKeys { get; init; }
        = new Dictionary<string, string>();
}
