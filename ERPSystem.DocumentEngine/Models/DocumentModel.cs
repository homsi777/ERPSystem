namespace ERPSystem.DocumentEngine.Models;

/// <summary>
/// The single, transport-agnostic DTO the engine consumes. It describes the
/// anatomy of any business document in neutral terms; each template picks the
/// sections it needs. This is the ONLY input contract of the engine — no WPF,
/// EF, or domain types are referenced.
/// </summary>
public sealed class DocumentModel
{
    public DocumentType Type { get; set; }

    /// <summary>Main document title, e.g. "Sales Invoice" / "فاتورة مبيعات".</summary>
    public string Title { get; set; } = string.Empty;
    public string? Subtitle { get; set; }

    /// <summary>Human document number, e.g. "INV-2026-000123".</summary>
    public string? Number { get; set; }

    /// <summary>Header status badge.</summary>
    public DocumentStatus Status { get; set; } = DocumentStatus.None;
    public string? StatusLabel { get; set; }

    /// <summary>Primary party (bill-to customer / supplier / partner).</summary>
    public PartyInfo? PrimaryParty { get; set; }

    /// <summary>Optional secondary party (ship-to, issuing branch, ...).</summary>
    public PartyInfo? SecondaryParty { get; set; }

    /// <summary>Header meta fields (date, due date, reference, currency, ...).</summary>
    public List<InfoField> HeaderFields { get; set; } = new();

    /// <summary>KPI cards shown above the body (used by reports / statements).</summary>
    public List<SummaryCard> SummaryCards { get; set; } = new();

    /// <summary>One or more data tables (line items, ledger rows, ...).</summary>
    public List<DocumentTable> Tables { get; set; } = new();

    public TotalsModel? Totals { get; set; }
    public List<TaxLine> TaxLines { get; set; } = new();

    public List<TimelineEntry> Timeline { get; set; } = new();
    public List<SignatureSlot> Signatures { get; set; } = new();
    public List<AttachmentItem> Attachments { get; set; } = new();

    public string? Notes { get; set; }
    public string? Terms { get; set; }
    public ApprovalInfo? Approval { get; set; }

    /// <summary>Overrides branding watermark for this specific document.</summary>
    public string? WatermarkText { get; set; }

    /// <summary>Show a signature/stamp block region at the end of the document.</summary>
    public bool ShowSignatures => Signatures.Count > 0;
}
