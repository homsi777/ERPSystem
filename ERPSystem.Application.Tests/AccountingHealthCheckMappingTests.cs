using ERPSystem.Application.DTOs.Accounting;
using ERPSystem.Infrastructure.Services;

namespace ERPSystem.Application.Tests;

public class AccountingHealthCheckMappingTests
{
    [Fact]
    public void BuildHealthCheck_Passes_When_No_Issues()
    {
        var baseline = CreateBaseline();

        var health = AccountingBaselineReadService.BuildHealthCheck(baseline);

        Assert.Equal(baseline.CompanyId, health.CompanyId);
        Assert.Equal(0, health.FailCount);
        Assert.True(health.Checks.All(c => c.Status == AccountingHealthStatus.Pass));
    }

    [Fact]
    public void BuildHealthCheck_Fails_Critical_When_Duplicate_Journals_Present()
    {
        var baseline = CreateBaseline();
        baseline = new AccountingBaselineReportDto
        {
            GeneratedAtUtc = baseline.GeneratedAtUtc,
            CompanyId = baseline.CompanyId,
            CompanyName = baseline.CompanyName,
            Summary = baseline.Summary,
            InvoiceCountsByStatus = baseline.InvoiceCountsByStatus,
            InvoicesWithNegativeOpenAmount = baseline.InvoicesWithNegativeOpenAmount,
            InvoicesOverAllocated = baseline.InvoicesOverAllocated,
            ReceiptsOverAllocated = baseline.ReceiptsOverAllocated,
            DuplicateJournalEntries =
            [
                new AccountingBaselineDuplicateJournalDto
                {
                    SourceType = 0,
                    SourceTypeName = "SalesInvoice",
                    SourceId = Guid.NewGuid(),
                    DuplicateCount = 2,
                    JournalEntryNumbers = ["JE-1", "JE-2"]
                }
            ],
            UnbalancedJournalEntries = baseline.UnbalancedJournalEntries,
            JournalEntriesWithoutSource = baseline.JournalEntriesWithoutSource,
            OrphanAllocations = baseline.OrphanAllocations,
            CustomerBalanceDifferences = baseline.CustomerBalanceDifferences,
            CashboxBalanceDifferences = baseline.CashboxBalanceDifferences,
            ReturnsWithoutCostTrace = baseline.ReturnsWithoutCostTrace
        };

        var health = AccountingBaselineReadService.BuildHealthCheck(baseline);
        var duplicateCheck = health.Checks.Single(c => c.CheckId == "duplicate_journal_entries");

        Assert.Equal(AccountingHealthStatus.Fail, duplicateCheck.Status);
        Assert.Equal(AccountingHealthSeverity.Critical, duplicateCheck.Severity);
        Assert.Equal(1, duplicateCheck.IssueCount);
    }

    private static AccountingBaselineReportDto CreateBaseline()
    {
        var summary = new AccountingBaselineSummaryDto
        {
            TotalInvoices = 0,
            ApprovedInvoicesGrandTotal = 0,
            ApprovedInvoiceCount = 0,
            PostedReceiptsTotal = 0,
            PostedReceiptCount = 0,
            TotalAllocationsAmount = 0,
            AllocationCount = 0,
            PostedAllocationsAmount = 0,
            StoredCustomerBalancesTotal = 0,
            CustomerCount = 0,
            AccountsReceivableGlBalance = 0,
            OperationalCashboxBalancesTotal = 0,
            CashUsdGlBalance = 0,
            LinkedCashboxGlBalancesTotal = 0,
            InventoryOperationalValue = 100m,
            InventoryAssetGlBalance = 100m,
            CostOfGoodsSoldGlTotal = 0,
            PostedSalesReturnsTotal = 0,
            IssueCount = 0
        };

        return new AccountingBaselineReportDto
        {
            GeneratedAtUtc = DateTime.UtcNow.ToString("O"),
            CompanyId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
            CompanyName = "Test Co",
            Summary = summary,
            InvoiceCountsByStatus = [],
            InvoicesWithNegativeOpenAmount = [],
            InvoicesOverAllocated = [],
            ReceiptsOverAllocated = [],
            DuplicateJournalEntries = [],
            UnbalancedJournalEntries = [],
            JournalEntriesWithoutSource = [],
            OrphanAllocations = [],
            CustomerBalanceDifferences = [],
            CashboxBalanceDifferences = [],
            ReturnsWithoutCostTrace = []
        };
    }
}
