namespace ERPSystem.Application.DTOs.Reports;

public sealed class ModuleReportColumnDto
{
    public string Key { get; init; } = "";
    public string HeaderAr { get; init; } = "";
    public string? Format { get; init; }
    public double Width { get; init; } = 100;
    public bool IsStar { get; init; }
}

public sealed class ModuleReportKpiDto
{
    public string Label { get; init; } = "";
    public string Value { get; init; } = "";
    public string IconGlyph { get; init; } = "\uE9D2";
}

public sealed class ModuleReportResultDto
{
    public string ReportKey { get; init; } = "";
    public string Title { get; init; } = "";
    public string Description { get; init; } = "";
    public DateTime GeneratedAt { get; init; }
    public DateTime? FromDate { get; init; }
    public DateTime? ToDate { get; init; }
    public IReadOnlyList<ModuleReportColumnDto> Columns { get; init; } = [];
    public IReadOnlyList<Dictionary<string, object?>> Rows { get; init; } = [];
    public IReadOnlyList<ModuleReportKpiDto> Kpis { get; init; } = [];
}

public sealed class GetModuleReportQuery
{
    public Guid CompanyId { get; init; }
    public Guid BranchId { get; init; }
    public string ReportKey { get; init; } = "";
    public DateTime? FromDate { get; init; }
    public DateTime? ToDate { get; init; }
}
