using ERPSystem.Application.DTOs.Containers;

namespace ERPSystem.Services.China;

public sealed class ChinaImportHeaderDraft
{
    public string ContainerNumber { get; init; } = "";
    public Guid SupplierId { get; init; }
    public DateTime ShipmentDate { get; init; }
    public DateTime? ExpectedArrival { get; init; }
    public decimal ExchangeRateToLocalCurrency { get; init; } = 1m;
    public string? Notes { get; init; }
}

public static class ChinaImportNavigationContext
{
    public static ContainerExcelParseResultDto? LastParseResult { get; private set; }
    public static ChinaImportHeaderDraft? HeaderDraft { get; private set; }
    public static string? LastFileName { get; private set; }

    public static void SetParseSession(
        ContainerExcelParseResultDto parseResult,
        ChinaImportHeaderDraft headerDraft,
        string fileName)
    {
        LastParseResult = parseResult;
        HeaderDraft = headerDraft;
        LastFileName = fileName;
    }

    public static ContainerExcelParseResultDto? GetParseResult() => LastParseResult;

    public static void Clear()
    {
        LastParseResult = null;
        HeaderDraft = null;
        LastFileName = null;
    }
}
