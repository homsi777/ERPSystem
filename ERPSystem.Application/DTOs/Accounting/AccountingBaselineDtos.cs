namespace ERPSystem.Application.DTOs.Accounting;

public sealed class AccountingBaselineReportDto
{
    public required string GeneratedAtUtc { get; init; }
    public required Guid CompanyId { get; init; }
    public required string CompanyName { get; init; }
    public required AccountingBaselineSummaryDto Summary { get; init; }
    public required IReadOnlyList<AccountingBaselineCountByStatusDto> InvoiceCountsByStatus { get; init; }
    public required IReadOnlyList<AccountingBaselineIssueRowDto> InvoicesWithNegativeOpenAmount { get; init; }
    public required IReadOnlyList<AccountingBaselineIssueRowDto> InvoicesOverAllocated { get; init; }
    public required IReadOnlyList<AccountingBaselineIssueRowDto> ReceiptsOverAllocated { get; init; }
    public required IReadOnlyList<AccountingBaselineDuplicateJournalDto> DuplicateJournalEntries { get; init; }
    public required IReadOnlyList<AccountingBaselineUnbalancedJournalDto> UnbalancedJournalEntries { get; init; }
    public required IReadOnlyList<AccountingBaselineIssueRowDto> JournalEntriesWithoutSource { get; init; }
    public required IReadOnlyList<AccountingBaselineOrphanAllocationDto> OrphanAllocations { get; init; }
    public required IReadOnlyList<AccountingBaselineCustomerBalanceDiffDto> CustomerBalanceDifferences { get; init; }
    public required IReadOnlyList<AccountingBaselineCashboxDiffDto> CashboxBalanceDifferences { get; init; }
    public required IReadOnlyList<AccountingBaselineIssueRowDto> ReturnsWithoutCostTrace { get; init; }
}

public sealed class AccountingBaselineSummaryDto
{
    public int TotalInvoices { get; init; }
    public decimal ApprovedInvoicesGrandTotal { get; init; }
    public int ApprovedInvoiceCount { get; init; }
    public decimal PostedReceiptsTotal { get; init; }
    public int PostedReceiptCount { get; init; }
    public decimal TotalAllocationsAmount { get; init; }
    public int AllocationCount { get; init; }
    public decimal PostedAllocationsAmount { get; init; }
    public decimal StoredCustomerBalancesTotal { get; init; }
    public int CustomerCount { get; init; }
    public decimal AccountsReceivableGlBalance { get; init; }
    public decimal OperationalCashboxBalancesTotal { get; init; }
    public decimal CashUsdGlBalance { get; init; }
    public decimal LinkedCashboxGlBalancesTotal { get; init; }
    public decimal InventoryOperationalValue { get; init; }
    public decimal InventoryAssetGlBalance { get; init; }
    public decimal CostOfGoodsSoldGlTotal { get; init; }
    public decimal PostedSalesReturnsTotal { get; init; }
    public int IssueCount { get; init; }
}

public sealed class AccountingBaselineCountByStatusDto
{
    public required string StatusName { get; init; }
    public int StatusValue { get; init; }
    public int Count { get; init; }
    public decimal GrandTotalSum { get; init; }
}

public sealed class AccountingBaselineIssueRowDto
{
    public required string Id { get; init; }
    public required string Reference { get; init; }
    public required string Detail { get; init; }
    public decimal? Amount { get; init; }
}

public sealed class AccountingBaselineDuplicateJournalDto
{
    public int? SourceType { get; init; }
    public required string SourceTypeName { get; init; }
    public Guid? SourceId { get; init; }
    public int DuplicateCount { get; init; }
    public required IReadOnlyList<string> JournalEntryNumbers { get; init; }
}

public sealed class AccountingBaselineUnbalancedJournalDto
{
    public Guid JournalEntryId { get; init; }
    public required string EntryNumber { get; init; }
    public decimal DebitTotal { get; init; }
    public decimal CreditTotal { get; init; }
    public decimal Difference { get; init; }
    public required string StatusName { get; init; }
}

public sealed class AccountingBaselineOrphanAllocationDto
{
    public Guid AllocationId { get; init; }
    public Guid? SalesInvoiceId { get; init; }
    public Guid? ReceiptVoucherId { get; init; }
    public decimal Amount { get; init; }
    public required string Reason { get; init; }
}

public sealed class AccountingBaselineCustomerBalanceDiffDto
{
    public Guid CustomerId { get; init; }
    public required string CustomerCode { get; init; }
    public required string CustomerName { get; init; }
    public decimal StoredBalance { get; init; }
    public decimal SubledgerBalance { get; init; }
    public decimal Difference { get; init; }
}

public sealed class AccountingBaselineCashboxDiffDto
{
    public Guid CashboxId { get; init; }
    public required string CashboxCode { get; init; }
    public required string CashboxName { get; init; }
    public Guid? AccountId { get; init; }
    public decimal OperationalBalance { get; init; }
    public decimal? GlBalance { get; init; }
    public decimal? Difference { get; init; }
    public required string Notes { get; init; }
}

public enum AccountingHealthSeverity
{
    Info = 0,
    Warning = 1,
    Critical = 2
}

public enum AccountingHealthStatus
{
    Pass = 0,
    Fail = 1,
    Skipped = 2
}

public sealed class AccountingHealthCheckResultDto
{
    public required string GeneratedAtUtc { get; init; }
    public required Guid CompanyId { get; init; }
    public required string CompanyName { get; init; }
    public required IReadOnlyList<AccountingHealthCheckItemDto> Checks { get; init; }
    public int PassCount { get; init; }
    public int FailCount { get; init; }
    public int CriticalFailCount { get; init; }
}

public sealed class AccountingHealthCheckItemDto
{
    public required string CheckId { get; init; }
    public required string Title { get; init; }
    public AccountingHealthSeverity Severity { get; init; }
    public AccountingHealthStatus Status { get; init; }
    public int IssueCount { get; init; }
    public required string Message { get; init; }
    public required IReadOnlyList<string> SampleDetails { get; init; }
}
