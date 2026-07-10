using ERPSystem.Application.Abstractions.Services;
using ERPSystem.Application.Common;
using ERPSystem.Application.DTOs.Accounting;
using ERPSystem.Domain.Enums;
using ERPSystem.Infrastructure.Persistence;
using ERPSystem.Infrastructure.Persistence.Models.Accounting;
using Microsoft.EntityFrameworkCore;

namespace ERPSystem.Infrastructure.Services;

/// <summary>
/// Shared read-only queries for accounting baseline and health checks.
/// </summary>
internal static class AccountingBaselineReadService
{
    private const decimal BalanceTolerance = 0.01m;
    private static readonly int PostedJournalStatus = (int)JournalEntryStatus.Posted;
    private static readonly int PostedVoucherStatus = (int)VoucherStatus.Posted;
    private static readonly int AvailableRollStatus = (int)FabricRollStatus.Available;
    private static readonly int SalesReturnDocumentType = (int)DocumentType.SalesReturn;

    public static async Task<(Guid CompanyId, string CompanyName)> ResolveCompanyAsync(
        ErpDbContext context,
        Guid? companyId,
        CancellationToken cancellationToken)
    {
        if (companyId is { } explicitId)
        {
            var company = await context.Companies.AsNoTracking()
                .Where(c => c.Id == explicitId)
                .Select(c => new { c.Id, c.NameAr })
                .FirstOrDefaultAsync(cancellationToken)
                ?? throw new InvalidOperationException($"Company '{explicitId}' was not found.");

            return (company.Id, company.NameAr);
        }

        var first = await context.Companies.AsNoTracking()
            .OrderBy(c => c.Code)
            .Select(c => new { c.Id, c.NameAr })
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new InvalidOperationException("No company records exist in the database.");

        return (first.Id, first.NameAr);
    }

    public static async Task<AccountingBaselineReportDto> GenerateBaselineAsync(
        ErpDbContext context,
        Guid companyId,
        string companyName,
        CancellationToken cancellationToken)
    {
        var generatedAt = DateTime.UtcNow;
        var invoiceCounts = await GetInvoiceCountsByStatusAsync(context, companyId, cancellationToken);
        var summaryCore = await BuildSummaryCoreAsync(context, companyId, cancellationToken);
        var negativeOpen = await GetInvoicesWithNegativeOpenAmountAsync(context, companyId, cancellationToken);
        var invoicesOverAllocated = await GetInvoicesOverAllocatedAsync(context, companyId, cancellationToken);
        var receiptsOverAllocated = await GetReceiptsOverAllocatedAsync(context, companyId, cancellationToken);
        var duplicateJournals = await GetDuplicateJournalEntriesAsync(context, companyId, cancellationToken);
        var unbalancedJournals = await GetUnbalancedJournalEntriesAsync(context, companyId, cancellationToken);
        var journalsWithoutSource = await GetJournalEntriesWithoutSourceAsync(context, companyId, cancellationToken);
        var orphanAllocations = await GetOrphanAllocationsAsync(context, companyId, cancellationToken);
        var customerDiffs = await GetCustomerBalanceDifferencesAsync(context, companyId, cancellationToken);
        var cashboxDiffs = await GetCashboxBalanceDifferencesAsync(context, companyId, cancellationToken);
        var returnsWithoutCost = await GetReturnsWithoutCostTraceAsync(context, companyId, cancellationToken);

        var issueCount =
            negativeOpen.Count +
            invoicesOverAllocated.Count +
            receiptsOverAllocated.Count +
            duplicateJournals.Count +
            unbalancedJournals.Count +
            journalsWithoutSource.Count +
            orphanAllocations.Count +
            customerDiffs.Count +
            cashboxDiffs.Count(d => d.Difference is not null && Math.Abs(d.Difference.Value) > BalanceTolerance) +
            returnsWithoutCost.Count;

        var summary = new AccountingBaselineSummaryDto
        {
            TotalInvoices = summaryCore.TotalInvoices,
            ApprovedInvoicesGrandTotal = summaryCore.ApprovedInvoicesGrandTotal,
            ApprovedInvoiceCount = summaryCore.ApprovedInvoiceCount,
            PostedReceiptsTotal = summaryCore.PostedReceiptsTotal,
            PostedReceiptCount = summaryCore.PostedReceiptCount,
            TotalAllocationsAmount = summaryCore.TotalAllocationsAmount,
            AllocationCount = summaryCore.AllocationCount,
            PostedAllocationsAmount = summaryCore.PostedAllocationsAmount,
            StoredCustomerBalancesTotal = summaryCore.StoredCustomerBalancesTotal,
            CustomerCount = summaryCore.CustomerCount,
            AccountsReceivableGlBalance = summaryCore.AccountsReceivableGlBalance,
            OperationalCashboxBalancesTotal = summaryCore.OperationalCashboxBalancesTotal,
            CashUsdGlBalance = summaryCore.CashUsdGlBalance,
            LinkedCashboxGlBalancesTotal = summaryCore.LinkedCashboxGlBalancesTotal,
            InventoryOperationalValue = summaryCore.InventoryOperationalValue,
            InventoryAssetGlBalance = summaryCore.InventoryAssetGlBalance,
            CostOfGoodsSoldGlTotal = summaryCore.CostOfGoodsSoldGlTotal,
            PostedSalesReturnsTotal = summaryCore.PostedSalesReturnsTotal,
            IssueCount = issueCount
        };

        return new AccountingBaselineReportDto
        {
            GeneratedAtUtc = generatedAt.ToString("O"),
            CompanyId = companyId,
            CompanyName = companyName,
            Summary = summary,
            InvoiceCountsByStatus = invoiceCounts,
            InvoicesWithNegativeOpenAmount = negativeOpen,
            InvoicesOverAllocated = invoicesOverAllocated,
            ReceiptsOverAllocated = receiptsOverAllocated,
            DuplicateJournalEntries = duplicateJournals,
            UnbalancedJournalEntries = unbalancedJournals,
            JournalEntriesWithoutSource = journalsWithoutSource,
            OrphanAllocations = orphanAllocations,
            CustomerBalanceDifferences = customerDiffs,
            CashboxBalanceDifferences = cashboxDiffs,
            ReturnsWithoutCostTrace = returnsWithoutCost
        };
    }

    public static AccountingHealthCheckResultDto BuildHealthCheck(
        AccountingBaselineReportDto baseline)
    {
        var checks = new List<AccountingHealthCheckItemDto>
        {
            BuildCheck(
                "duplicate_journal_entries",
                "قيود محاسبية مكررة Legacy (SourceType + SourceId — ما قبل PostingKind)",
                AccountingHealthSeverity.Critical,
                baseline.DuplicateJournalEntries.Count,
                baseline.DuplicateJournalEntries.Take(5).Select(d =>
                    $"{d.SourceTypeName} / {d.SourceId} × {d.DuplicateCount}: {string.Join(", ", d.JournalEntryNumbers)}")),

            BuildCheck(
                "unbalanced_journal_entries",
                "قيود غير متوازنة",
                AccountingHealthSeverity.Critical,
                baseline.UnbalancedJournalEntries.Count,
                baseline.UnbalancedJournalEntries.Take(5).Select(u =>
                    $"{u.EntryNumber}: Dr {u.DebitTotal:N2} / Cr {u.CreditTotal:N2} (Δ {u.Difference:N2})")),

            BuildCheck(
                "journal_entries_without_source",
                "قيود آلية بلا مصدر (SourceType/SourceId)",
                AccountingHealthSeverity.Warning,
                baseline.JournalEntriesWithoutSource.Count,
                baseline.JournalEntriesWithoutSource.Take(5).Select(j => j.Reference)),

            BuildCheck(
                "invoices_negative_open_amount",
                "فواتير ذات متبقي سالب",
                AccountingHealthSeverity.Critical,
                baseline.InvoicesWithNegativeOpenAmount.Count,
                baseline.InvoicesWithNegativeOpenAmount.Take(5).Select(i => i.Detail)),

            BuildCheck(
                "invoices_over_allocated",
                "فواتير تخصيصاتها أكبر من إجماليها",
                AccountingHealthSeverity.Critical,
                baseline.InvoicesOverAllocated.Count,
                baseline.InvoicesOverAllocated.Take(5).Select(i => i.Detail)),

            BuildCheck(
                "receipts_over_allocated",
                "سندات قبض تخصيصاتها أكبر من قيمتها",
                AccountingHealthSeverity.Critical,
                baseline.ReceiptsOverAllocated.Count,
                baseline.ReceiptsOverAllocated.Take(5).Select(i => i.Detail)),

            BuildCheck(
                "orphan_allocations",
                "تخصيصات دون فاتورة أو سند صالح",
                AccountingHealthSeverity.Critical,
                baseline.OrphanAllocations.Count,
                baseline.OrphanAllocations.Take(5).Select(o => $"{o.Reason}: {o.Amount:N2} USD")),

            BuildCheck(
                "customer_balance_mismatch",
                "عملاء رصيدهم المخزّن ≠ أستاذ AR",
                AccountingHealthSeverity.Critical,
                baseline.CustomerBalanceDifferences.Count,
                baseline.CustomerBalanceDifferences.Take(5).Select(c =>
                    $"{c.CustomerCode} {c.CustomerName}: stored {c.StoredBalance:N2} vs GL {c.SubledgerBalance:N2} (Δ {c.Difference:N2})")),

            BuildCheck(
                "cashbox_gl_mismatch",
                "صناديق مختلفة عن حسابات GL المرتبطة",
                AccountingHealthSeverity.Critical,
                baseline.CashboxBalanceDifferences.Count(d =>
                    d.Difference is not null && Math.Abs(d.Difference.Value) > BalanceTolerance),
                baseline.CashboxBalanceDifferences
                    .Where(d => d.Difference is not null && Math.Abs(d.Difference.Value) > BalanceTolerance)
                    .Take(5)
                    .Select(c => $"{c.CashboxCode}: ops {c.OperationalBalance:N2} vs GL {c.GlBalance:N2}")),

            BuildCheck(
                "inventory_gl_mismatch",
                "قيمة المخزون التشغيلية ≠ حساب المخزون في GL",
                AccountingHealthSeverity.Warning,
                Math.Abs(baseline.Summary.InventoryOperationalValue - baseline.Summary.InventoryAssetGlBalance) > BalanceTolerance ? 1 : 0,
                [
                    $"Operational {baseline.Summary.InventoryOperationalValue:N2} USD vs GL {baseline.Summary.InventoryAssetGlBalance:N2} USD"
                ]),

            BuildCheck(
                "ar_control_vs_stored_customers",
                "مجموع أرصدة العملاء المخزنة ≠ حساب AR في GL",
                AccountingHealthSeverity.Warning,
                Math.Abs(baseline.Summary.StoredCustomerBalancesTotal - baseline.Summary.AccountsReceivableGlBalance) > BalanceTolerance ? 1 : 0,
                [
                    $"Stored customers {baseline.Summary.StoredCustomerBalancesTotal:N2} vs AR GL {baseline.Summary.AccountsReceivableGlBalance:N2}"
                ]),

            BuildCheck(
                "returns_without_cost_trace",
                "مرتجعات دون أثر تكلفة/حركة مخزون واضح",
                AccountingHealthSeverity.Warning,
                baseline.ReturnsWithoutCostTrace.Count,
                baseline.ReturnsWithoutCostTrace.Take(5).Select(r => r.Detail))
        };

        return new AccountingHealthCheckResultDto
        {
            GeneratedAtUtc = baseline.GeneratedAtUtc,
            CompanyId = baseline.CompanyId,
            CompanyName = baseline.CompanyName,
            Checks = checks,
            PassCount = checks.Count(c => c.Status == AccountingHealthStatus.Pass),
            FailCount = checks.Count(c => c.Status == AccountingHealthStatus.Fail),
            CriticalFailCount = checks.Count(c =>
                c.Status == AccountingHealthStatus.Fail && c.Severity == AccountingHealthSeverity.Critical)
        };
    }

    internal static AccountingHealthCheckItemDto BuildCheck(
        string checkId,
        string title,
        AccountingHealthSeverity severity,
        int issueCount,
        IEnumerable<string> sampleDetails)
    {
        var samples = sampleDetails.ToList();
        return new AccountingHealthCheckItemDto
        {
            CheckId = checkId,
            Title = title,
            Severity = severity,
            Status = issueCount > 0 ? AccountingHealthStatus.Fail : AccountingHealthStatus.Pass,
            IssueCount = issueCount,
            Message = issueCount > 0
                ? $"Detected {issueCount} issue(s)."
                : "No issues detected.",
            SampleDetails = samples
        };
    }

    private static async Task<AccountingBaselineSummaryDto> BuildSummaryCoreAsync(
        ErpDbContext context,
        Guid companyId,
        CancellationToken cancellationToken)
    {
        var approvedMin = (int)SalesInvoiceStatus.Approved;
        var cancelled = (int)SalesInvoiceStatus.Cancelled;

        var invoiceQuery = context.SalesInvoices.AsNoTracking()
            .Where(i => i.CompanyId == companyId && !i.IsArchived);

        var totalInvoices = await invoiceQuery.CountAsync(cancellationToken);

        var approvedInvoices = await invoiceQuery
            .Where(i => i.Status >= approvedMin && i.Status != cancelled)
            .Select(i => new { i.GrandTotal })
            .ToListAsync(cancellationToken);

        var postedReceipts = await context.ReceiptVouchers.AsNoTracking()
            .Where(r => r.CompanyId == companyId && r.Status == PostedVoucherStatus && !r.IsArchived)
            .Select(r => new { r.Amount })
            .ToListAsync(cancellationToken);

        var allocations = await context.ReceiptInvoicePayments.AsNoTracking().ToListAsync(cancellationToken);
        var postedReceiptIds = await context.ReceiptVouchers.AsNoTracking()
            .Where(r => r.CompanyId == companyId && r.Status == PostedVoucherStatus && !r.IsArchived)
            .Select(r => r.Id)
            .ToListAsync(cancellationToken);
        var postedReceiptIdSet = postedReceiptIds.ToHashSet();
        var postedAllocationsAmount = allocations
            .Where(a => postedReceiptIdSet.Contains(a.ReceiptVoucherId))
            .Sum(a => a.Amount);

        var customers = await context.Customers.AsNoTracking()
            .Where(c => c.CompanyId == companyId && c.IsActive)
            .Select(c => c.Balance)
            .ToListAsync(cancellationToken);

        var cashboxes = await context.Cashboxes.AsNoTracking()
            .Where(c => c.IsActive)
            .Select(c => new { c.Balance, c.AccountId })
            .ToListAsync(cancellationToken);

        var inventoryValue = await context.FabricRolls.AsNoTracking()
            .Where(r => r.Status == AvailableRollStatus && r.RemainingLengthMeters > 0)
            .SumAsync(r => r.RemainingLengthMeters * r.CostPerMeter, cancellationToken);

        var arBalance = await GetAssetAccountBalanceAsync(
            context, companyId, AccountingAccountIds.AccountsReceivable, cancellationToken);
        var cashUsdBalance = await GetAssetAccountBalanceAsync(
            context, companyId, AccountingAccountIds.CashUsd, cancellationToken);
        var inventoryGlBalance = await GetAssetAccountBalanceAsync(
            context, companyId, AccountingAccountIds.InventoryAsset, cancellationToken);
        var cogsTotal = await GetExpenseAccountDebitTotalAsync(
            context, companyId, AccountingAccountIds.CostOfGoodsSold, cancellationToken);

        var linkedCashboxAccountIds = cashboxes
            .Where(c => c.AccountId.HasValue)
            .Select(c => c.AccountId!.Value)
            .Distinct()
            .ToList();

        decimal linkedCashboxGlTotal = 0m;
        foreach (var accountId in linkedCashboxAccountIds)
        {
            linkedCashboxGlTotal += await GetAssetAccountBalanceAsync(
                context, companyId, accountId, cancellationToken);
        }

        var postedReturnsTotal = await context.SalesReturns.AsNoTracking()
            .Where(r => r.CompanyId == companyId && r.Status == PostedVoucherStatus && !r.IsArchived)
            .SumAsync(r => r.TotalAmount, cancellationToken);

        return new AccountingBaselineSummaryDto
        {
            TotalInvoices = totalInvoices,
            ApprovedInvoicesGrandTotal = approvedInvoices.Sum(i => i.GrandTotal),
            ApprovedInvoiceCount = approvedInvoices.Count,
            PostedReceiptsTotal = postedReceipts.Sum(r => r.Amount),
            PostedReceiptCount = postedReceipts.Count,
            TotalAllocationsAmount = allocations.Sum(a => a.Amount),
            AllocationCount = allocations.Count,
            PostedAllocationsAmount = postedAllocationsAmount,
            StoredCustomerBalancesTotal = customers.Sum(c => c),
            CustomerCount = customers.Count,
            AccountsReceivableGlBalance = arBalance,
            OperationalCashboxBalancesTotal = cashboxes.Sum(c => c.Balance),
            CashUsdGlBalance = cashUsdBalance,
            LinkedCashboxGlBalancesTotal = linkedCashboxGlTotal,
            InventoryOperationalValue = inventoryValue,
            InventoryAssetGlBalance = inventoryGlBalance,
            CostOfGoodsSoldGlTotal = cogsTotal,
            PostedSalesReturnsTotal = postedReturnsTotal,
            IssueCount = 0
        };
    }

    private static async Task<IReadOnlyList<AccountingBaselineCountByStatusDto>> GetInvoiceCountsByStatusAsync(
        ErpDbContext context,
        Guid companyId,
        CancellationToken cancellationToken)
    {
        var rows = await context.SalesInvoices.AsNoTracking()
            .Where(i => i.CompanyId == companyId && !i.IsArchived)
            .GroupBy(i => i.Status)
            .Select(g => new
            {
                Status = g.Key,
                Count = g.Count(),
                Total = g.Sum(x => x.GrandTotal)
            })
            .OrderBy(x => x.Status)
            .ToListAsync(cancellationToken);

        return rows.Select(r => new AccountingBaselineCountByStatusDto
        {
            StatusName = Enum.IsDefined(typeof(SalesInvoiceStatus), r.Status)
                ? ((SalesInvoiceStatus)r.Status).ToString()
                : r.Status.ToString(),
            StatusValue = r.Status,
            Count = r.Count,
            GrandTotalSum = r.Total
        }).ToList();
    }

    private static async Task<IReadOnlyDictionary<Guid, decimal>> GetPostedAllocationsByInvoiceAsync(
        ErpDbContext context,
        Guid companyId,
        CancellationToken cancellationToken)
    {
        var rows = await (
            from payment in context.ReceiptInvoicePayments.AsNoTracking()
            join receipt in context.ReceiptVouchers.AsNoTracking()
                on payment.ReceiptVoucherId equals receipt.Id
            where receipt.CompanyId == companyId
                  && receipt.Status == PostedVoucherStatus
                  && !receipt.IsArchived
            group payment by payment.SalesInvoiceId
            into g
            select new { InvoiceId = g.Key, Total = g.Sum(x => x.Amount) }
        ).ToListAsync(cancellationToken);

        return rows.ToDictionary(x => x.InvoiceId, x => x.Total);
    }

    private static async Task<IReadOnlyDictionary<Guid, decimal>> GetPostedReturnsByInvoiceAsync(
        ErpDbContext context,
        Guid companyId,
        CancellationToken cancellationToken)
    {
        var rows = await context.SalesReturns.AsNoTracking()
            .Where(r => r.CompanyId == companyId && r.Status == PostedVoucherStatus && !r.IsArchived)
            .GroupBy(r => r.OriginalInvoiceId)
            .Select(g => new { InvoiceId = g.Key, Total = g.Sum(x => x.TotalAmount) })
            .ToListAsync(cancellationToken);

        return rows.ToDictionary(x => x.InvoiceId, x => x.Total);
    }

    private static async Task<IReadOnlyList<AccountingBaselineIssueRowDto>> GetInvoicesWithNegativeOpenAmountAsync(
        ErpDbContext context,
        Guid companyId,
        CancellationToken cancellationToken)
    {
        var approvedMin = (int)SalesInvoiceStatus.Approved;
        var cancelled = (int)SalesInvoiceStatus.Cancelled;
        var allocations = await GetPostedAllocationsByInvoiceAsync(context, companyId, cancellationToken);
        var returns = await GetPostedReturnsByInvoiceAsync(context, companyId, cancellationToken);

        var invoices = await context.SalesInvoices.AsNoTracking()
            .Where(i => i.CompanyId == companyId && !i.IsArchived && i.Status >= approvedMin && i.Status != cancelled)
            .Select(i => new { i.Id, i.InvoiceNumber, i.GrandTotal })
            .ToListAsync(cancellationToken);

        return invoices
            .Select(i =>
            {
                allocations.TryGetValue(i.Id, out var allocated);
                returns.TryGetValue(i.Id, out var returned);
                var open = i.GrandTotal - allocated - returned;
                return new { i.Id, i.InvoiceNumber, i.GrandTotal, allocated, returned, open };
            })
            .Where(x => x.open < -BalanceTolerance)
            .Select(x => new AccountingBaselineIssueRowDto
            {
                Id = x.Id.ToString(),
                Reference = x.InvoiceNumber,
                Detail = $"Open {x.open:N2} (Total {x.GrandTotal:N2}, Allocated {x.allocated:N2}, Returns {x.returned:N2})",
                Amount = x.open
            })
            .OrderBy(x => x.Amount)
            .ToList();
    }

    private static async Task<IReadOnlyList<AccountingBaselineIssueRowDto>> GetInvoicesOverAllocatedAsync(
        ErpDbContext context,
        Guid companyId,
        CancellationToken cancellationToken)
    {
        var approvedMin = (int)SalesInvoiceStatus.Approved;
        var cancelled = (int)SalesInvoiceStatus.Cancelled;
        var allocations = await GetPostedAllocationsByInvoiceAsync(context, companyId, cancellationToken);

        var invoices = await context.SalesInvoices.AsNoTracking()
            .Where(i => i.CompanyId == companyId && !i.IsArchived && i.Status >= approvedMin && i.Status != cancelled)
            .Select(i => new { i.Id, i.InvoiceNumber, i.GrandTotal })
            .ToListAsync(cancellationToken);

        return invoices
            .Select(i =>
            {
                allocations.TryGetValue(i.Id, out var allocated);
                return new { i.Id, i.InvoiceNumber, i.GrandTotal, allocated };
            })
            .Where(x => x.allocated > x.GrandTotal + BalanceTolerance)
            .Select(x => new AccountingBaselineIssueRowDto
            {
                Id = x.Id.ToString(),
                Reference = x.InvoiceNumber,
                Detail = $"Allocated {x.allocated:N2} > GrandTotal {x.GrandTotal:N2}",
                Amount = x.allocated - x.GrandTotal
            })
            .ToList();
    }

    private static async Task<IReadOnlyList<AccountingBaselineIssueRowDto>> GetReceiptsOverAllocatedAsync(
        ErpDbContext context,
        Guid companyId,
        CancellationToken cancellationToken)
    {
        var rows = await (
            from payment in context.ReceiptInvoicePayments.AsNoTracking()
            join receipt in context.ReceiptVouchers.AsNoTracking()
                on payment.ReceiptVoucherId equals receipt.Id
            where receipt.CompanyId == companyId && !receipt.IsArchived
            group payment by new { receipt.Id, receipt.VoucherNumber, receipt.Amount, receipt.Status }
            into g
            select new
            {
                g.Key.Id,
                g.Key.VoucherNumber,
                g.Key.Amount,
                g.Key.Status,
                Allocated = g.Sum(x => x.Amount)
            }).ToListAsync(cancellationToken);

        return rows
            .Where(r => r.Allocated > r.Amount + BalanceTolerance)
            .Select(r => new AccountingBaselineIssueRowDto
            {
                Id = r.Id.ToString(),
                Reference = r.VoucherNumber,
                Detail = $"Allocated {r.Allocated:N2} > Receipt {r.Amount:N2} (Status {(VoucherStatus)r.Status})",
                Amount = r.Allocated - r.Amount
            })
            .ToList();
    }

    private static async Task<IReadOnlyList<AccountingBaselineDuplicateJournalDto>> GetDuplicateJournalEntriesAsync(
        ErpDbContext context,
        Guid companyId,
        CancellationToken cancellationToken)
    {
        var duplicateKeys = await context.JournalEntries.AsNoTracking()
            .Where(e => e.CompanyId == companyId && e.SourceType != null && e.SourceId != null)
            .GroupBy(e => new { e.SourceType, e.SourceId })
            .Where(g => g.Count() > 1)
            .Select(g => new
            {
                g.Key.SourceType,
                g.Key.SourceId,
                Count = g.Count()
            })
            .ToListAsync(cancellationToken);

        if (duplicateKeys.Count == 0)
            return [];

        var sourceIds = duplicateKeys.Where(d => d.SourceId.HasValue).Select(d => d.SourceId!.Value).ToList();
        var entries = await context.JournalEntries.AsNoTracking()
            .Where(e => e.CompanyId == companyId && e.SourceId != null && sourceIds.Contains(e.SourceId.Value))
            .Select(e => new { e.SourceType, e.SourceId, e.EntryNumber })
            .ToListAsync(cancellationToken);

        return duplicateKeys.Select(d =>
        {
            var numbers = entries
                .Where(e => e.SourceType == d.SourceType && e.SourceId == d.SourceId)
                .Select(e => e.EntryNumber)
                .ToList();

            return new AccountingBaselineDuplicateJournalDto
            {
                SourceType = d.SourceType,
                SourceTypeName = d.SourceType.HasValue && Enum.IsDefined(typeof(DocumentType), d.SourceType.Value)
                    ? ((DocumentType)d.SourceType.Value).ToString()
                    : d.SourceType?.ToString() ?? "Unknown",
                SourceId = d.SourceId,
                DuplicateCount = d.Count,
                JournalEntryNumbers = numbers
            };
        }).ToList();
    }

    private static async Task<IReadOnlyList<AccountingBaselineUnbalancedJournalDto>> GetUnbalancedJournalEntriesAsync(
        ErpDbContext context,
        Guid companyId,
        CancellationToken cancellationToken)
    {
        var entries = await (
            from entry in context.JournalEntries.AsNoTracking()
            where entry.CompanyId == companyId
            join line in context.JournalEntryLines.AsNoTracking()
                on entry.Id equals line.JournalEntryId into lines
            select new
            {
                entry.Id,
                entry.EntryNumber,
                entry.Status,
                DebitTotal = lines.Sum(l => (decimal?)l.Debit) ?? 0m,
                CreditTotal = lines.Sum(l => (decimal?)l.Credit) ?? 0m
            }).ToListAsync(cancellationToken);

        return entries
            .Select(e => new
            {
                e.Id,
                e.EntryNumber,
                e.Status,
                e.DebitTotal,
                e.CreditTotal,
                Difference = e.DebitTotal - e.CreditTotal
            })
            .Where(e => Math.Abs(e.Difference) > BalanceTolerance)
            .Select(e => new AccountingBaselineUnbalancedJournalDto
            {
                JournalEntryId = e.Id,
                EntryNumber = e.EntryNumber,
                DebitTotal = e.DebitTotal,
                CreditTotal = e.CreditTotal,
                Difference = e.Difference,
                StatusName = Enum.IsDefined(typeof(JournalEntryStatus), e.Status)
                    ? ((JournalEntryStatus)e.Status).ToString()
                    : e.Status.ToString()
            })
            .OrderByDescending(e => Math.Abs(e.Difference))
            .ToList();
    }

    private static async Task<IReadOnlyList<AccountingBaselineIssueRowDto>> GetJournalEntriesWithoutSourceAsync(
        ErpDbContext context,
        Guid companyId,
        CancellationToken cancellationToken)
    {
        var rows = await context.JournalEntries.AsNoTracking()
            .Where(e => e.CompanyId == companyId
                        && (e.SourceType == null || e.SourceId == null)
                        && e.Status == PostedJournalStatus)
            .OrderByDescending(e => e.EntryDate)
            .Select(e => new { e.Id, e.EntryNumber, e.Description })
            .Take(500)
            .ToListAsync(cancellationToken);

        return rows.Select(r => new AccountingBaselineIssueRowDto
        {
            Id = r.Id.ToString(),
            Reference = r.EntryNumber,
            Detail = r.Description,
            Amount = null
        }).ToList();
    }

    private static async Task<IReadOnlyList<AccountingBaselineOrphanAllocationDto>> GetOrphanAllocationsAsync(
        ErpDbContext context,
        Guid companyId,
        CancellationToken cancellationToken)
    {
        var payments = await context.ReceiptInvoicePayments.AsNoTracking().ToListAsync(cancellationToken);
        var invoiceIds = await context.SalesInvoices.AsNoTracking()
            .Where(i => i.CompanyId == companyId)
            .Select(i => i.Id)
            .ToListAsync(cancellationToken);
        var receiptIds = await context.ReceiptVouchers.AsNoTracking()
            .Where(r => r.CompanyId == companyId)
            .Select(r => new { r.Id, r.Status, r.IsArchived })
            .ToListAsync(cancellationToken);

        var invoiceSet = invoiceIds.ToHashSet();
        var receiptMap = receiptIds.ToDictionary(r => r.Id);

        var orphans = new List<AccountingBaselineOrphanAllocationDto>();
        foreach (var payment in payments)
        {
            string? reason = null;
            if (!invoiceSet.Contains(payment.SalesInvoiceId))
                reason = "Missing sales invoice";
            else if (!receiptMap.TryGetValue(payment.ReceiptVoucherId, out var receipt))
                reason = "Missing receipt voucher";
            else if (receipt.IsArchived)
                reason = "Receipt is archived";
            else if (receipt.Status == (int)VoucherStatus.Cancelled)
                reason = "Receipt is cancelled";

            if (reason is not null)
            {
                orphans.Add(new AccountingBaselineOrphanAllocationDto
                {
                    AllocationId = payment.Id,
                    SalesInvoiceId = payment.SalesInvoiceId,
                    ReceiptVoucherId = payment.ReceiptVoucherId,
                    Amount = payment.Amount,
                    Reason = reason
                });
            }
        }

        return orphans;
    }

    private static async Task<IReadOnlyList<AccountingBaselineCustomerBalanceDiffDto>> GetCustomerBalanceDifferencesAsync(
        ErpDbContext context,
        Guid companyId,
        CancellationToken cancellationToken)
    {
        var customers = await context.Customers.AsNoTracking()
            .Where(c => c.CompanyId == companyId && c.IsActive)
            .Select(c => new { c.Id, c.Code, c.NameAr, c.Balance })
            .ToListAsync(cancellationToken);

        var subledgerRows = await (
            from line in context.JournalEntryLines.AsNoTracking()
            join entry in context.JournalEntries.AsNoTracking() on line.JournalEntryId equals entry.Id
            where entry.CompanyId == companyId
                  && entry.Status == PostedJournalStatus
                  && line.AccountId == AccountingAccountIds.AccountsReceivable
                  && line.PartyId != null
            group line by line.PartyId
            into g
            select new
            {
                PartyId = g.Key!.Value,
                Balance = g.Sum(x => x.Debit - x.Credit)
            }).ToListAsync(cancellationToken);

        var subledgerMap = subledgerRows.ToDictionary(x => x.PartyId, x => x.Balance);
        var diffs = new List<AccountingBaselineCustomerBalanceDiffDto>();

        foreach (var customer in customers)
        {
            subledgerMap.TryGetValue(customer.Id, out var subledger);
            var difference = customer.Balance - subledger;
            if (Math.Abs(difference) <= BalanceTolerance)
                continue;

            diffs.Add(new AccountingBaselineCustomerBalanceDiffDto
            {
                CustomerId = customer.Id,
                CustomerCode = customer.Code,
                CustomerName = customer.NameAr,
                StoredBalance = customer.Balance,
                SubledgerBalance = subledger,
                Difference = difference
            });
        }

        foreach (var extra in subledgerMap.Where(x => customers.All(c => c.Id != x.Key)))
        {
            diffs.Add(new AccountingBaselineCustomerBalanceDiffDto
            {
                CustomerId = extra.Key,
                CustomerCode = "—",
                CustomerName = "(Party in GL without active customer row)",
                StoredBalance = 0m,
                SubledgerBalance = extra.Value,
                Difference = -extra.Value
            });
        }

        return diffs.OrderByDescending(d => Math.Abs(d.Difference)).ToList();
    }

    private static async Task<IReadOnlyList<AccountingBaselineCashboxDiffDto>> GetCashboxBalanceDifferencesAsync(
        ErpDbContext context,
        Guid companyId,
        CancellationToken cancellationToken)
    {
        var cashboxes = await context.Cashboxes.AsNoTracking()
            .Where(c => c.IsActive)
            .Select(c => new { c.Id, c.Code, c.Name, c.Balance, c.AccountId })
            .ToListAsync(cancellationToken);

        var results = new List<AccountingBaselineCashboxDiffDto>();
        foreach (var cashbox in cashboxes)
        {
            if (cashbox.AccountId is null)
            {
                results.Add(new AccountingBaselineCashboxDiffDto
                {
                    CashboxId = cashbox.Id,
                    CashboxCode = cashbox.Code,
                    CashboxName = cashbox.Name,
                    AccountId = null,
                    OperationalBalance = cashbox.Balance,
                    GlBalance = null,
                    Difference = null,
                    Notes = "No GL AccountId linked — compare manually with CashUsd aggregate."
                });
                continue;
            }

            var glBalance = await GetAssetAccountBalanceAsync(
                context, companyId, cashbox.AccountId.Value, cancellationToken);
            var difference = cashbox.Balance - glBalance;
            results.Add(new AccountingBaselineCashboxDiffDto
            {
                CashboxId = cashbox.Id,
                CashboxCode = cashbox.Code,
                CashboxName = cashbox.Name,
                AccountId = cashbox.AccountId,
                OperationalBalance = cashbox.Balance,
                GlBalance = glBalance,
                Difference = difference,
                Notes = Math.Abs(difference) <= BalanceTolerance ? "Matched" : "Operational vs linked GL account"
            });
        }

        return results;
    }

    private static async Task<IReadOnlyList<AccountingBaselineIssueRowDto>> GetReturnsWithoutCostTraceAsync(
        ErpDbContext context,
        Guid companyId,
        CancellationToken cancellationToken)
    {
        var postedReturns = await context.SalesReturns.AsNoTracking()
            .Where(r => r.CompanyId == companyId && r.Status == PostedVoucherStatus && !r.IsArchived)
            .Select(r => new { r.Id, r.ReturnNumber, r.TotalAmount })
            .ToListAsync(cancellationToken);

        var journalReturnIds = await context.JournalEntries.AsNoTracking()
            .Where(e => e.CompanyId == companyId
                        && e.SourceType == SalesReturnDocumentType
                        && e.SourceId != null)
            .Select(e => e.SourceId!.Value)
            .ToListAsync(cancellationToken);
        var journalSet = journalReturnIds.ToHashSet();

        var movementReturnIds = await context.StockMovements.AsNoTracking()
            .Where(m => m.ReferenceType == SalesReturnDocumentType && m.ReferenceId != null)
            .Select(m => m.ReferenceId!.Value)
            .ToListAsync(cancellationToken);
        var movementSet = movementReturnIds.ToHashSet();

        return postedReturns
            .Where(r => !journalSet.Contains(r.Id) || !movementSet.Contains(r.Id))
            .Select(r => new AccountingBaselineIssueRowDto
            {
                Id = r.Id.ToString(),
                Reference = r.ReturnNumber,
                Detail = $"Posted return {r.TotalAmount:N2} USD — journal={(journalSet.Contains(r.Id) ? "yes" : "no")}, stock movement={(movementSet.Contains(r.Id) ? "yes" : "no")}",
                Amount = r.TotalAmount
            })
            .ToList();
    }

    private static async Task<decimal> GetAssetAccountBalanceAsync(
        ErpDbContext context,
        Guid companyId,
        Guid accountId,
        CancellationToken cancellationToken)
    {
        var totals = await (
            from line in context.JournalEntryLines.AsNoTracking()
            join entry in context.JournalEntries.AsNoTracking() on line.JournalEntryId equals entry.Id
            where entry.CompanyId == companyId
                  && entry.Status == PostedJournalStatus
                  && line.AccountId == accountId
            group line by 1
            into g
            select new
            {
                Debit = g.Sum(x => x.Debit),
                Credit = g.Sum(x => x.Credit)
            }).FirstOrDefaultAsync(cancellationToken);

        if (totals is null)
            return 0m;

        return totals.Debit - totals.Credit;
    }

    private static async Task<decimal> GetExpenseAccountDebitTotalAsync(
        ErpDbContext context,
        Guid companyId,
        Guid accountId,
        CancellationToken cancellationToken)
    {
        var total = await (
            from line in context.JournalEntryLines.AsNoTracking()
            join entry in context.JournalEntries.AsNoTracking() on line.JournalEntryId equals entry.Id
            where entry.CompanyId == companyId
                  && entry.Status == PostedJournalStatus
                  && line.AccountId == accountId
            select (decimal?)line.Debit).SumAsync(cancellationToken);

        return total ?? 0m;
    }
}

internal sealed class AccountingBaselineReportService(ErpDbContext context)
    : IAccountingBaselineReportService
{
    public async Task<AccountingBaselineReportDto> GenerateAsync(
        Guid? companyId = null,
        CancellationToken cancellationToken = default)
    {
        var (resolvedCompanyId, companyName) = await AccountingBaselineReadService.ResolveCompanyAsync(
            context, companyId, cancellationToken);

        return await AccountingBaselineReadService.GenerateBaselineAsync(
            context, resolvedCompanyId, companyName, cancellationToken);
    }
}

internal sealed class AccountingHealthCheckService(ErpDbContext context)
    : IAccountingHealthCheckService
{
    private static readonly TimeSpan StuckPostingThreshold = TimeSpan.FromMinutes(15);

    public async Task<AccountingHealthCheckResultDto> RunAsync(
        Guid? companyId = null,
        CancellationToken cancellationToken = default)
    {
        var baseline = await new AccountingBaselineReportService(context)
            .GenerateAsync(companyId, cancellationToken);

        var checks = AccountingBaselineReadService.BuildHealthCheck(baseline).Checks.ToList();
        var resolvedCompanyId = baseline.CompanyId;

        checks.AddRange(await BuildPostingProtectionChecksAsync(resolvedCompanyId, cancellationToken));

        return new AccountingHealthCheckResultDto
        {
            GeneratedAtUtc = baseline.GeneratedAtUtc,
            CompanyId = baseline.CompanyId,
            CompanyName = baseline.CompanyName,
            Checks = checks,
            PassCount = checks.Count(c => c.Status == AccountingHealthStatus.Pass),
            FailCount = checks.Count(c => c.Status == AccountingHealthStatus.Fail),
            CriticalFailCount = checks.Count(c =>
                c.Status == AccountingHealthStatus.Fail && c.Severity == AccountingHealthSeverity.Critical)
        };
    }

    private async Task<IReadOnlyList<AccountingHealthCheckItemDto>> BuildPostingProtectionChecksAsync(
        Guid companyId,
        CancellationToken cancellationToken)
    {
        var stuckCutoff = DateTime.UtcNow.Subtract(StuckPostingThreshold);

        var protectedDuplicateGroups = await context.JournalEntries.AsNoTracking()
            .Where(j => j.CompanyId == companyId
                        && j.PostingIdentityVersion == 2
                        && j.SourceType != null
                        && j.SourceId != null
                        && j.PostingKind != null
                        && j.IsActive)
            .GroupBy(j => new { j.SourceType, j.SourceId, j.PostingKind })
            .Where(g => g.Count() > 1)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        var legacyDuplicateCount = await context.JournalEntries.AsNoTracking()
            .Where(j => j.CompanyId == companyId && j.SourceType != null && j.SourceId != null && j.IsActive)
            .GroupBy(j => new { j.SourceType, j.SourceId })
            .Where(g => g.Count() > 1)
            .CountAsync(cancellationToken);

        var stuckAttempts = await context.AccountingPostingAttempts.AsNoTracking()
            .Where(a => a.CompanyId == companyId
                        && a.Status == (int)PostingAttemptStatus.Posting
                        && a.StartedAt < stuckCutoff)
            .CountAsync(cancellationToken);

        var failedAttempts = await context.AccountingPostingAttempts.AsNoTracking()
            .Where(a => a.CompanyId == companyId && a.Status == (int)PostingAttemptStatus.PostingFailed)
            .CountAsync(cancellationToken);

        var v2WithoutKind = await context.JournalEntries.AsNoTracking()
            .Where(j => j.CompanyId == companyId && j.PostingIdentityVersion == 2 && j.PostingKind == null)
            .CountAsync(cancellationToken);

        return
        [
            AccountingBaselineReadService.BuildCheck(
                "duplicate_protected_posting_identities",
                "تكرار هوية ترحيل محمية (v2: Company+Source+PostingKind)",
                AccountingHealthSeverity.Critical,
                protectedDuplicateGroups.Count,
                protectedDuplicateGroups.Select(d =>
                    $"{d.Key.SourceType}/{d.Key.SourceId}/kind={d.Key.PostingKind} × {d.Count}")),

            AccountingBaselineReadService.BuildCheck(
                "legacy_critical_duplicate_evidence",
                "دليل Legacy Critical — تكرار تاريخي محفوظ (Phase 2)",
                AccountingHealthSeverity.Critical,
                legacyDuplicateCount,
                legacyDuplicateCount > 0
                    ? ["Historical duplicate preserved — e.g. JE-MAIN-000001 / JE-MAIN-000002 for ChinaContainer b9e96735"]
                    : []),

            AccountingBaselineReadService.BuildCheck(
                "stuck_posting_attempts",
                "محاولات ترحيل عالقة في حالة Posting",
                AccountingHealthSeverity.Critical,
                stuckAttempts,
                stuckAttempts > 0 ? [$"{stuckAttempts} attempt(s) older than {StuckPostingThreshold.TotalMinutes:N0} minutes"] : []),

            AccountingBaselineReadService.BuildCheck(
                "failed_posting_attempts",
                "محاولات ترحيل فاشلة",
                AccountingHealthSeverity.Warning,
                failedAttempts,
                failedAttempts > 0 ? [$"{failedAttempts} failed posting attempt(s)"] : []),

            AccountingBaselineReadService.BuildCheck(
                "journal_entries_v2_without_posting_kind",
                "قيود v2 بدون PostingKind",
                AccountingHealthSeverity.Critical,
                v2WithoutKind,
                v2WithoutKind > 0 ? [$"{v2WithoutKind} protected entry(ies) missing PostingKind"] : [])
        ];
    }
}
