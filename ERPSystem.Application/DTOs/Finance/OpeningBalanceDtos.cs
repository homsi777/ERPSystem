using ERPSystem.Domain.Entities.Finance;

namespace ERPSystem.Application.DTOs.Finance;

/// <summary>Row shown in the opening balances list / dashboard drill-down.</summary>
public sealed class OpeningBalanceListDto
{
    public Guid Id { get; init; }
    public string Number { get; init; } = "";
    public OpeningBalanceType Type { get; init; }
    public string TypeDisplay { get; init; } = "";
    public OpeningBalanceStatus Status { get; init; }
    public string StatusDisplay { get; init; } = "";
    public OpeningBalanceSource Source { get; init; }
    public string SourceDisplay { get; init; } = "";
    public DateTime OpeningDate { get; init; }
    public string CurrencyCode { get; init; } = "USD";
    public decimal ExchangeRate { get; init; } = 1m;
    public decimal TotalDebit { get; init; }
    public decimal TotalCredit { get; init; }
    public decimal TotalBaseAmount { get; init; }
    public int LineCount { get; init; }
    public string? Reference { get; init; }
    public string? Description { get; init; }
    public string? JournalEntryNumber { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
    public DateTime? PostedAt { get; init; }
    public string? PrimaryPartyDisplay { get; init; }
    public string? DisplayNotes { get; init; }
    /// <summary>Opening stock: summarized fabric/item names from document lines.</summary>
    public string? StockItemsSummary { get; init; }
    /// <summary>Opening stock: total roll count across all lines.</summary>
    public int TotalRollCount { get; init; }
    public decimal NetBalance => TotalDebit - TotalCredit;
}

/// <summary>KPI strip for customer opening balance submodule.</summary>
public sealed class CustomerOpeningBalanceSummaryDto
{
    public int TotalCount { get; init; }
    public decimal TotalDebit { get; init; }
    public decimal TotalCredit { get; init; }
    public decimal NetBalance { get; init; }
    public int PendingApprovalCount { get; init; }
    public int PostedCount { get; init; }
}

/// <summary>One line of an opening balance document.</summary>
public sealed class OpeningBalanceLineDto
{
    public Guid Id { get; init; }
    public int LineNumber { get; init; }
    public Guid? PartyId { get; init; }
    public string? PartyName { get; init; }
    public Guid? AccountId { get; init; }
    public string? AccountName { get; init; }
    public Guid? WarehouseId { get; init; }
    public string? WarehouseName { get; init; }
    public Guid? FabricItemId { get; init; }
    public Guid? FabricColorId { get; init; }
    public string? ItemName { get; init; }
    public string? ColorName { get; init; }
    public string? BatchNumber { get; init; }
    public string? LocationCode { get; init; }
    public decimal? RollCount { get; init; }
    public decimal? Quantity { get; init; }
    public decimal? UnitCost { get; init; }
    public string? BankName { get; init; }
    public string? BankAccountNumber { get; init; }
    public string? InvestmentScope { get; init; }
    public decimal Debit { get; init; }
    public decimal Credit { get; init; }
    public decimal Amount { get; init; }
    public string? Reference { get; init; }
    public string? Description { get; init; }
    public string? Notes { get; init; }
}

/// <summary>Audit / timeline event row.</summary>
public sealed class OpeningBalanceEventDto
{
    public DateTime OccurredAt { get; init; }
    public string UserName { get; init; } = "";
    public string Action { get; init; } = "";
    public string? OldValues { get; init; }
    public string? NewValues { get; init; }
    public string? Notes { get; init; }
    public string? MachineName { get; init; }
    public string? IpAddress { get; init; }
}

/// <summary>Full document with lines + audit trail + timeline (Operations Center).</summary>
public sealed class OpeningBalanceDetailsDto
{
    public OpeningBalanceListDto Header { get; init; } = new();
    public IReadOnlyList<OpeningBalanceLineDto> Lines { get; init; } = [];
    public IReadOnlyList<OpeningBalanceEventDto> Events { get; init; } = [];
    public string? ApprovalNotes { get; init; }
    public string? RejectionReason { get; init; }
    public DateTime? ApprovedAt { get; init; }
    public DateTime? LockedAt { get; init; }
    public DateTime? ArchivedAt { get; init; }
    public IReadOnlyList<OpeningBalanceJournalLineDto> JournalLines { get; init; } = [];
}

/// <summary>A journal entry line generated for the document (Accounting tab).</summary>
public sealed class OpeningBalanceJournalLineDto
{
    public string EntryNumber { get; init; } = "";
    public DateTime EntryDate { get; init; }
    public string AccountCode { get; init; } = "";
    public string AccountName { get; init; } = "";
    public decimal Debit { get; init; }
    public decimal Credit { get; init; }
    public string? Narrative { get; init; }
}

/// <summary>Dashboard KPIs for the module landing page.</summary>
public sealed class OpeningBalanceDashboardDto
{
    public int TotalDocuments { get; init; }
    public int DraftCount { get; init; }
    public int PendingApprovalCount { get; init; }
    public int ApprovedCount { get; init; }
    public int PostedCount { get; init; }
    public int LockedCount { get; init; }
    public int ArchivedCount { get; init; }
    public decimal TotalPostedBaseAmount { get; init; }
    public decimal TotalDraftBaseAmount { get; init; }
    public IReadOnlyList<OpeningBalanceTypeSummaryDto> ByType { get; init; } = [];
}

public sealed class OpeningBalanceTypeSummaryDto
{
    public OpeningBalanceType Type { get; init; }
    public string TypeDisplay { get; init; } = "";
    public int DocumentCount { get; init; }
    public int PostedCount { get; init; }
    public decimal TotalBaseAmount { get; init; }
}

/// <summary>One validation finding (error or warning) with row context.</summary>
public sealed class OpeningBalanceValidationIssueDto
{
    public int RowNumber { get; init; }
    public string Field { get; init; } = "";
    public string Message { get; init; } = "";
    public bool IsWarning { get; init; }
}

/// <summary>Result of running the validation engine against a draft / import.</summary>
public sealed class OpeningBalanceValidationReportDto
{
    public bool IsValid { get; init; }
    public int TotalRows { get; init; }
    public int ValidRows { get; init; }
    public int DuplicateRows { get; init; }
    public IReadOnlyList<OpeningBalanceValidationIssueDto> Errors { get; init; } = [];
    public IReadOnlyList<OpeningBalanceValidationIssueDto> Warnings { get; init; } = [];
}

/// <summary>Import summary displayed after an Excel import completes.</summary>
public sealed class OpeningBalanceImportResultDto
{
    public Guid? DocumentId { get; init; }
    public string? DocumentNumber { get; init; }
    public int TotalRows { get; init; }
    public int ImportedRows { get; init; }
    public int SkippedRows { get; init; }
    public int DuplicateRows { get; init; }
    public int WarningCount { get; init; }
    public int ErrorCount { get; init; }
    public long ExecutionMs { get; init; }
    public string UserName { get; init; } = "";
    public DateTime ImportDate { get; init; }
    public string FileName { get; init; } = "";
    public OpeningBalanceValidationReportDto Validation { get; init; } = new();
}

/// <summary>Result of posting a document to the general ledger.</summary>
public sealed class OpeningBalancePostResultDto
{
    public Guid DocumentId { get; init; }
    public string DocumentNumber { get; init; } = "";
    public string JournalEntryNumber { get; init; } = "";
    public DateTime PostedAt { get; init; }
    public decimal TotalBaseAmount { get; init; }
}

/// <summary>Generic lookup item used by manual entry forms and import matching.</summary>
public sealed class OpeningBalanceLookupItemDto
{
    public Guid Id { get; init; }
    public string Code { get; init; } = "";
    public string Name { get; init; } = "";
    public string? Extra { get; init; }
}

/// <summary>All lookups the opening balance UI needs, loaded in one round-trip.</summary>
public sealed class OpeningBalanceLookupsDto
{
    public IReadOnlyList<OpeningBalanceLookupItemDto> Customers { get; init; } = [];
    public IReadOnlyList<OpeningBalanceLookupItemDto> Suppliers { get; init; } = [];
    public IReadOnlyList<OpeningBalanceLookupItemDto> Partners { get; init; } = [];
    public IReadOnlyList<OpeningBalanceLookupItemDto> Cashboxes { get; init; } = [];
    public IReadOnlyList<OpeningBalanceLookupItemDto> Warehouses { get; init; } = [];
    public IReadOnlyList<OpeningBalanceLookupItemDto> Accounts { get; init; } = [];
}

/// <summary>Display-name helpers shared by handlers, UI and reports.</summary>
public static class OpeningBalanceDisplay
{
    public static string TypeName(OpeningBalanceType type) => type switch
    {
        OpeningBalanceType.OpeningStock => "مخزون افتتاحي",
        OpeningBalanceType.CustomerReceivable => "ذمم عملاء افتتاحية",
        OpeningBalanceType.SupplierPayable => "ذمم موردين افتتاحية",
        OpeningBalanceType.Cash => "نقدية افتتاحية",
        OpeningBalanceType.Bank => "حسابات بنكية افتتاحية",
        OpeningBalanceType.Capital => "رأس مال افتتاحي",
        OpeningBalanceType.GeneralLedger => "أرصدة دفتر الأستاذ",
        OpeningBalanceType.FixedAsset => "أصول ثابتة افتتاحية",
        OpeningBalanceType.Loan => "قروض افتتاحية",
        OpeningBalanceType.EmployeeAdvance => "سلف موظفين افتتاحية",
        OpeningBalanceType.PettyCash => "عهدة نقدية افتتاحية",
        OpeningBalanceType.BranchOpening => "أرصدة فرع افتتاحية",
        _ => type.ToString()
    };

    public static string StatusName(OpeningBalanceStatus status) => status switch
    {
        OpeningBalanceStatus.Draft => "مسودة",
        OpeningBalanceStatus.PendingApproval => "بانتظار الاعتماد",
        OpeningBalanceStatus.Approved => "معتمد",
        OpeningBalanceStatus.Posted => "مرحّل",
        OpeningBalanceStatus.Locked => "مقفل",
        OpeningBalanceStatus.Archived => "مؤرشف",
        OpeningBalanceStatus.Rejected => "مرفوض",
        _ => status.ToString()
    };

    public static string SourceName(OpeningBalanceSource source) => source switch
    {
        OpeningBalanceSource.ExcelImport => "استيراد Excel",
        _ => "إدخال يدوي"
    };
}
