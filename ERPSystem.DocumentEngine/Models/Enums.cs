namespace ERPSystem.DocumentEngine.Models;

/// <summary>
/// Every document type the engine can render. New business documents are added
/// here and mapped to a template in the <c>TemplateRegistry</c>.
/// </summary>
public enum DocumentType
{
    SalesInvoice,
    PurchaseInvoice,
    PurchaseOrder,
    Quotation,
    CustomerStatement,
    SupplierStatement,
    ReceiptVoucher,
    PaymentVoucher,
    ExpenseVoucher,
    InventoryTransfer,
    OpeningStock,
    Stocktake,
    InventoryReport,
    ContainerReport,
    PartnerStatement,
    ExecutiveDashboardReport,
    TrialBalance,
    BalanceSheet,
    IncomeStatement,
    CashFlow,
    GeneralLedger,
    JournalVoucher
}

/// <summary>
/// The output mode. The SAME HTML template is used for all modes; only a body
/// class and small CSS rules differ. HTML is always the single source of truth.
/// </summary>
public enum RenderMode
{
    /// <summary>On-screen preview / web display.</summary>
    Web,
    /// <summary>Print preview / browser print pipeline.</summary>
    Print,
    /// <summary>PDF generation (same HTML fed to a headless converter).</summary>
    Pdf
}

/// <summary>Text / layout direction.</summary>
public enum TextDirection
{
    Rtl,
    Ltr
}

/// <summary>Semantic accent used by badges, cards, timeline dots, etc.</summary>
public enum Accent
{
    Neutral,
    Primary,
    Secondary,
    Success,
    Danger,
    Warning,
    Info
}

/// <summary>Horizontal cell / text alignment.</summary>
public enum TextAlign
{
    Start,
    Center,
    End
}

/// <summary>High-level document status for the header status badge.</summary>
public enum DocumentStatus
{
    None,
    Draft,
    Pending,
    Approved,
    Posted,
    Paid,
    PartiallyPaid,
    Overdue,
    Cancelled,
    Rejected,
    Completed
}

/// <summary>Approval state for the approval badge component.</summary>
public enum ApprovalState
{
    Approved,
    Pending,
    Rejected
}

/// <summary>Printable page geometry.</summary>
public enum PageSize
{
    A4,
    A5,
    Letter
}
