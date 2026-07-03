namespace ERPSystem.Application.DTOs.Containers;

public sealed class ChinaInvoiceParseResultDto
{
    public string FileName { get; init; } = "";
    public IReadOnlyList<ChinaInvoiceLineDto> Lines { get; init; } = [];
    public decimal SeaFreightUsd { get; init; }
    public decimal InsuranceUsd { get; init; }
    public decimal GrandTotalUsd { get; init; }
    public decimal DeclaredTotalMeters { get; init; }
    public int DeclaredTotalRolls { get; init; }
    public bool LineAmountsMatchTotal { get; init; }
    public string? TotalValidationWarning { get; init; }
}

public sealed class ChinaInvoiceLineDto
{
    public int LineIndex { get; init; }
    public string Description { get; init; } = "";
    public string MatchKey { get; init; } = "";
    public decimal LengthMeters { get; init; }
    public int RollCount { get; init; }
    public decimal UnitPriceUsd { get; init; }
    public decimal LineAmountUsd { get; init; }
}

public sealed class ChinaPackingSummaryParseResultDto
{
    public string FileName { get; init; } = "";
    public IReadOnlyList<ChinaPackingSummaryLineDto> Lines { get; init; } = [];
    public decimal DeclaredTotalMeters { get; init; }
    public int DeclaredTotalRolls { get; init; }
    public decimal TotalCbm { get; init; }
    public decimal TotalGrossWeightKg { get; init; }
    public decimal TotalNetWeightKg { get; init; }
}

public sealed class ChinaPackingSummaryLineDto
{
    public int LineIndex { get; init; }
    public string Description { get; init; } = "";
    public string MatchKey { get; init; } = "";
    public int RollCount { get; init; }
    public decimal LengthMeters { get; init; }
    public decimal Cbm { get; init; }
    public decimal GrossWeightKg { get; init; }
    public decimal NetWeightKg { get; init; }
}

public sealed class ChinaImportTypeLineDto
{
    public int LineIndex { get; init; }
    public string TypeDisplayName { get; init; } = "";
    public string MatchKey { get; init; } = "";
    public string? FabricCode { get; init; }
    public string? Color { get; init; }
    public Guid? FabricItemId { get; init; }
    public Guid? FabricColorId { get; init; }

    public bool HasInvoice { get; init; }
    public bool HasPackingSummary { get; init; }
    public bool HasDpl { get; init; }
    public IReadOnlyList<string> MissingSources { get; init; } = [];
    public IReadOnlyList<string> MismatchWarnings { get; init; } = [];

    public decimal LengthMeters { get; init; }
    public int RollCount { get; init; }
    public decimal NetWeightKg { get; init; }
    public decimal Cbm { get; init; }
    public decimal ChinaUnitPriceUsd { get; init; }
    public decimal InvoiceLineAmountUsd { get; init; }

    public decimal ExpenseShareUsd { get; init; }
    public decimal LandedCostPerMeterUsd { get; init; }
    public decimal MarginPerMeterUsd { get; init; }
    public decimal SalePricePerMeterUsd { get; init; }

    public string? DplMatchKey { get; init; }
    public bool LinkedViaAlias { get; init; }

    public string MatchStatusDisplay =>
        MissingSources.Count == 0 && MismatchWarnings.Count == 0
            ? (LinkedViaAlias ? "✅ متطابق (ربط محفوظ)" : "✅ متطابق")
            : MissingSources.Count > 0 ? $"⚠️ ناقص: {string.Join("، ", MissingSources)}" :
        $"⚠️ {string.Join("؛ ", MismatchWarnings)}";
}

public sealed class ChinaImportMultiFileSessionDto
{
    public ContainerExcelParseResultDto? RollDetail { get; init; }
    public ChinaInvoiceParseResultDto? Invoice { get; init; }
    public ChinaPackingSummaryParseResultDto? PackingSummary { get; init; }
    public IReadOnlyList<ChinaImportTypeLineDto> TypeLines { get; init; } = [];
    public IReadOnlyList<ChinaImportUnmatchedDplGroupDto> UnmatchedDplGroups { get; init; } = [];
    public IReadOnlyList<ChinaImportInvoiceLinkOptionDto> InvoiceLinkOptions { get; init; } = [];
    public bool UsesWeightedAllocation => Invoice is not null && PackingSummary is not null;
    public bool RequiresDplLinking => UsesWeightedAllocation && UnmatchedDplGroups.Count > 0;
    public string CostingModeDisplay => UsesWeightedAllocation
        ? "تخصيص بالوزن (فاتورة + PL)"
        : "معدّل مسطح (DPL فقط — يتطلب فاتورة + PL للتكلفة حسب النوع)";
}

public sealed class ContainerFabricTypeLineDto
{
    public Guid Id { get; init; }
    public int LineNumber { get; init; }
    public string TypeDisplayName { get; init; } = "";
    public Guid? FabricItemId { get; init; }
    public Guid? FabricColorId { get; init; }
    public decimal LengthMeters { get; init; }
    public int RollCount { get; init; }
    public decimal NetWeightKg { get; init; }
    public decimal ChinaUnitPriceUsd { get; init; }
    public decimal ExpenseShareUsd { get; init; }
    public decimal LandedCostPerMeterUsd { get; init; }
    public decimal MarginPerMeterUsd { get; init; }
    public decimal SalePricePerMeterUsd { get; init; }
    public bool HasSalePrice => SalePricePerMeterUsd > 0;
}
