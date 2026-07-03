using ERPSystem.Application.DTOs.Containers;
using ERPSystem.Application.UseCases.Containers;

namespace ERPSystem.Services.China;

public static class ChinaImportNavigationContext
{
    private static readonly Dictionary<string, string> SessionDplLinks = new(StringComparer.OrdinalIgnoreCase);

    public static ContainerExcelParseResultDto? LastParseResult { get; private set; }
    public static ChinaInvoiceParseResultDto? LastInvoiceParse { get; private set; }
    public static ChinaPackingSummaryParseResultDto? LastPackingSummaryParse { get; private set; }
    public static ChinaImportMultiFileSessionDto? MultiFileSession { get; private set; }
    public static ChinaImportHeaderDraft? HeaderDraft { get; private set; }
    public static string? LastRollDetailFileName { get; private set; }
    public static string? LastFileName { get; private set; }
    public static ChinaImportCostPreviewDto? CostPreview { get; private set; }
    public static Guid? ActiveContainerId { get; private set; }
    public static Guid? CreatedContainerId { get; private set; }

    public static void SetParseSession(
        ContainerExcelParseResultDto parseResult,
        ChinaImportHeaderDraft headerDraft,
        string rollDetailFileName)
    {
        LastParseResult = parseResult;
        HeaderDraft = headerDraft;
        LastRollDetailFileName = rollDetailFileName;
        LastFileName = rollDetailFileName;
        RebuildMultiFileSessionLocal();
    }

    public static void SetInvoiceParse(ChinaInvoiceParseResultDto? invoice, string? fileName = null)
    {
        LastInvoiceParse = invoice;
        if (!string.IsNullOrWhiteSpace(fileName))
            LastFileName = fileName;
        RebuildMultiFileSessionLocal();
    }

    public static void SetPackingSummaryParse(ChinaPackingSummaryParseResultDto? packingSummary, string? fileName = null)
    {
        LastPackingSummaryParse = packingSummary;
        if (!string.IsNullOrWhiteSpace(fileName))
            LastFileName = fileName;
        RebuildMultiFileSessionLocal();
    }

    public static void SetMultiFileSession(ChinaImportMultiFileSessionDto session) =>
        MultiFileSession = session;

    public static IReadOnlyDictionary<string, string> GetSessionDplLinks() => SessionDplLinks;

    public static void SetSessionDplLink(string dplMatchKey, string invoiceMatchKey)
    {
        SessionDplLinks[dplMatchKey] = invoiceMatchKey;
        RebuildMultiFileSessionLocal();
    }

    private static void RebuildMultiFileSessionLocal() =>
        MultiFileSession = ChinaImportCrossFileMatcher.BuildSession(
            LastParseResult,
            LastInvoiceParse,
            LastPackingSummaryParse,
            new ChinaImportMatchContext
            {
                SupplierId = HeaderDraft?.SupplierId ?? Guid.Empty,
                SessionDplToInvoiceKeys = SessionDplLinks
            });

    public static ContainerExcelParseResultDto? GetParseResult() => LastParseResult;

    public static ChinaImportMultiFileSessionDto? GetMultiFileSession() => MultiFileSession;

    public static void SetCostPreview(ChinaImportCostPreviewDto preview) => CostPreview = preview;

    public static void SetCreatedContainer(Guid containerId) => CreatedContainerId = containerId;

    public static void SetActiveContainer(Guid containerId) => ActiveContainerId = containerId;

    public static Guid? ResolveContainerId() => ActiveContainerId ?? CreatedContainerId;

    public static void Clear()
    {
        LastParseResult = null;
        LastInvoiceParse = null;
        LastPackingSummaryParse = null;
        MultiFileSession = null;
        HeaderDraft = null;
        LastRollDetailFileName = null;
        LastFileName = null;
        CostPreview = null;
        ActiveContainerId = null;
        CreatedContainerId = null;
        SessionDplLinks.Clear();
    }
}
