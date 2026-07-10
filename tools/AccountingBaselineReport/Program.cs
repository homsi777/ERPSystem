using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using ERPSystem.Application.Abstractions.Services;
using ERPSystem.Application.DTOs.Accounting;
using ERPSystem.Infrastructure.DependencyInjection;
using ERPSystem.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

var repoRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
var jsonOptions = new JsonSerializerOptions
{
    WriteIndented = true,
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
};

Guid? companyId = null;
var outputPrefix = "accounting-baseline-before";
for (var i = 0; i < args.Length; i++)
{
    if (args[i] is "--company-id" or "-c" && i + 1 < args.Length && Guid.TryParse(args[i + 1], out var parsed))
    {
        companyId = parsed;
        i++;
        continue;
    }

    if (args[i] is "--output-prefix" or "-o" && i + 1 < args.Length)
    {
        outputPrefix = args[i + 1];
        i++;
    }
}

var configuration = new ConfigurationBuilder()
    .SetBasePath(repoRoot)
    .AddJsonFile("appsettings.json", optional: false)
    .AddJsonFile("appsettings.Local.json", optional: true)
    .AddEnvironmentVariables()
    .Build();

using var tunnel = SshTunnelService.StartIfConfigured(configuration);

var services = new ServiceCollection();
services.AddLogging(b => b.AddConsole().SetMinimumLevel(LogLevel.Information));
services.AddInfrastructure(configuration);

await using var provider = services.BuildServiceProvider();
using var scope = provider.CreateScope();
var baselineService = scope.ServiceProvider.GetRequiredService<IAccountingBaselineReportService>();
var healthService = scope.ServiceProvider.GetRequiredService<IAccountingHealthCheckService>();

Console.WriteLine("Generating accounting baseline (read-only)...");

var baseline = await baselineService.GenerateAsync(companyId, CancellationToken.None);
var health = await healthService.RunAsync(companyId, CancellationToken.None);

var artifactsDir = Path.Combine(repoRoot, "artifacts");
Directory.CreateDirectory(artifactsDir);

var jsonPath = Path.Combine(artifactsDir, $"{outputPrefix}.json");
var mdPath = Path.Combine(artifactsDir, $"{outputPrefix}.md");
var healthJsonPath = Path.Combine(artifactsDir, $"{outputPrefix}-health.json");
var healthMdPath = Path.Combine(artifactsDir, $"{outputPrefix}-health.md");

var baselinePayload = new
{
    baseline,
    health
};

await File.WriteAllTextAsync(jsonPath, JsonSerializer.Serialize(baselinePayload, jsonOptions), Encoding.UTF8);
await File.WriteAllTextAsync(mdPath, BuildBaselineMarkdown(baseline, health), Encoding.UTF8);
await File.WriteAllTextAsync(healthJsonPath, JsonSerializer.Serialize(health, jsonOptions), Encoding.UTF8);
await File.WriteAllTextAsync(healthMdPath, BuildHealthMarkdown(health), Encoding.UTF8);

Console.WriteLine($"Baseline JSON : {jsonPath}");
Console.WriteLine($"Baseline MD   : {mdPath}");
Console.WriteLine($"Health JSON   : {healthJsonPath}");
Console.WriteLine($"Health MD     : {healthMdPath}");
Console.WriteLine($"Issues found  : {baseline.Summary.IssueCount}");
Console.WriteLine($"Health fails  : {health.FailCount} ({health.CriticalFailCount} critical)");

static string BuildBaselineMarkdown(AccountingBaselineReportDto baseline, AccountingHealthCheckResultDto health)
{
    var sb = new StringBuilder();
    var s = baseline.Summary;

    sb.AppendLine("# Accounting Baseline Report");
    sb.AppendLine();
    sb.AppendLine($"- Generated (UTC): {baseline.GeneratedAtUtc}");
    sb.AppendLine($"- Company: {baseline.CompanyName} (`{baseline.CompanyId}`)");
    sb.AppendLine($"- Total issues flagged: **{s.IssueCount}**");
    sb.AppendLine($"- Health checks failed: **{health.FailCount}** ({health.CriticalFailCount} critical)");
    sb.AppendLine();
    sb.AppendLine("## Summary");
    sb.AppendLine();
    sb.AppendLine("| Metric | Value |");
    sb.AppendLine("|--------|------:|");
    AppendRow(sb, "Total invoices", s.TotalInvoices.ToString());
    AppendRow(sb, "Approved invoices (count)", s.ApprovedInvoiceCount.ToString());
    AppendRow(sb, "Approved invoices grand total (USD)", s.ApprovedInvoicesGrandTotal.ToString("N2"));
    AppendRow(sb, "Posted receipts total (USD)", s.PostedReceiptsTotal.ToString("N2"));
    AppendRow(sb, "Posted receipts (count)", s.PostedReceiptCount.ToString());
    AppendRow(sb, "All allocations total (USD)", s.TotalAllocationsAmount.ToString("N2"));
    AppendRow(sb, "Posted allocations total (USD)", s.PostedAllocationsAmount.ToString("N2"));
    AppendRow(sb, "Stored customer balances total (USD)", s.StoredCustomerBalancesTotal.ToString("N2"));
    AppendRow(sb, "AR GL balance (USD)", s.AccountsReceivableGlBalance.ToString("N2"));
    AppendRow(sb, "Operational cashbox balances total (USD)", s.OperationalCashboxBalancesTotal.ToString("N2"));
    AppendRow(sb, "CashUsd GL balance (USD)", s.CashUsdGlBalance.ToString("N2"));
    AppendRow(sb, "Linked cashbox GL balances total (USD)", s.LinkedCashboxGlBalancesTotal.ToString("N2"));
    AppendRow(sb, "Inventory operational value (USD)", s.InventoryOperationalValue.ToString("N2"));
    AppendRow(sb, "Inventory GL balance (USD)", s.InventoryAssetGlBalance.ToString("N2"));
    AppendRow(sb, "COGS GL debit total (USD)", s.CostOfGoodsSoldGlTotal.ToString("N2"));
    AppendRow(sb, "Posted sales returns total (USD)", s.PostedSalesReturnsTotal.ToString("N2"));
    sb.AppendLine();
    sb.AppendLine("## Invoice counts by status");
    sb.AppendLine();
    sb.AppendLine("| Status | Count | Grand total sum |");
    sb.AppendLine("|--------|------:|----------------:|");
    foreach (var row in baseline.InvoiceCountsByStatus)
        sb.AppendLine($"| {row.StatusName} ({row.StatusValue}) | {row.Count} | {row.GrandTotalSum:N2} |");

    AppendIssueSection(sb, "Invoices with negative open amount", baseline.InvoicesWithNegativeOpenAmount);
    AppendIssueSection(sb, "Invoices over-allocated", baseline.InvoicesOverAllocated);
    AppendIssueSection(sb, "Receipts over-allocated", baseline.ReceiptsOverAllocated);
    AppendIssueSection(sb, "Journal entries without source", baseline.JournalEntriesWithoutSource);
    AppendIssueSection(sb, "Returns without cost / movement trace", baseline.ReturnsWithoutCostTrace);

    if (baseline.DuplicateJournalEntries.Count > 0)
    {
        sb.AppendLine("## Duplicate journal entries");
        sb.AppendLine();
        foreach (var dup in baseline.DuplicateJournalEntries)
        {
            sb.AppendLine($"- **{dup.SourceTypeName}** `{dup.SourceId}` × {dup.DuplicateCount}: {string.Join(", ", dup.JournalEntryNumbers)}");
        }
        sb.AppendLine();
    }

    if (baseline.UnbalancedJournalEntries.Count > 0)
    {
        sb.AppendLine("## Unbalanced journal entries");
        sb.AppendLine();
        foreach (var row in baseline.UnbalancedJournalEntries.Take(50))
            sb.AppendLine($"- {row.EntryNumber}: Dr {row.DebitTotal:N2} / Cr {row.CreditTotal:N2} (Δ {row.Difference:N2}) [{row.StatusName}]");
        sb.AppendLine();
    }

    if (baseline.CustomerBalanceDifferences.Count > 0)
    {
        sb.AppendLine("## Customer balance differences (stored vs AR subledger)");
        sb.AppendLine();
        foreach (var row in baseline.CustomerBalanceDifferences.Take(50))
            sb.AppendLine($"- {row.CustomerCode} {row.CustomerName}: stored {row.StoredBalance:N2}, GL {row.SubledgerBalance:N2}, Δ {row.Difference:N2}");
        sb.AppendLine();
    }

    if (baseline.CashboxBalanceDifferences.Count > 0)
    {
        sb.AppendLine("## Cashbox balance differences");
        sb.AppendLine();
        foreach (var row in baseline.CashboxBalanceDifferences)
        {
            var gl = row.GlBalance?.ToString("N2") ?? "n/a";
            var diff = row.Difference?.ToString("N2") ?? "n/a";
            sb.AppendLine($"- {row.CashboxCode} {row.CashboxName}: ops {row.OperationalBalance:N2}, GL {gl}, Δ {diff} — {row.Notes}");
        }
        sb.AppendLine();
    }

    sb.AppendLine("---");
    sb.AppendLine("*Read-only baseline — no financial data was modified.*");
    return sb.ToString();
}

static string BuildHealthMarkdown(AccountingHealthCheckResultDto health)
{
    var sb = new StringBuilder();
    sb.AppendLine("# Accounting Health Check");
    sb.AppendLine();
    sb.AppendLine($"- Generated (UTC): {health.GeneratedAtUtc}");
    sb.AppendLine($"- Company: {health.CompanyName}");
    sb.AppendLine($"- Pass: {health.PassCount} | Fail: {health.FailCount} | Critical fails: {health.CriticalFailCount}");
    sb.AppendLine();
    sb.AppendLine("| Check | Severity | Status | Issues | Message |");
    sb.AppendLine("|-------|----------|--------|-------:|---------|");
    foreach (var check in health.Checks)
    {
        sb.AppendLine($"| {check.Title} | {check.Severity} | {check.Status} | {check.IssueCount} | {check.Message} |");
        foreach (var sample in check.SampleDetails)
            sb.AppendLine($"| ↳ | | | | {sample} |");
    }

    return sb.ToString();
}

static void AppendRow(StringBuilder sb, string label, string value)
{
    sb.AppendLine($"| {label} | {value} |");
}

static void AppendIssueSection(StringBuilder sb, string title, IReadOnlyList<AccountingBaselineIssueRowDto> rows)
{
    if (rows.Count == 0)
        return;

    sb.AppendLine($"## {title} ({rows.Count})");
    sb.AppendLine();
    foreach (var row in rows.Take(50))
        sb.AppendLine($"- **{row.Reference}**: {row.Detail}");
    if (rows.Count > 50)
        sb.AppendLine($"- … and {rows.Count - 50} more");
    sb.AppendLine();
}
