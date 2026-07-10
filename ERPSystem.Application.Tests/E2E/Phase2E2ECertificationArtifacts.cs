using System.Text;
using System.Text.Json;
using ERPSystem.Application.Abstractions.Services;
using ERPSystem.Application.DTOs.Accounting;
using ERPSystem.Infrastructure.E2E;
using ERPSystem.Infrastructure.Seed;

namespace ERPSystem.Application.Tests.E2E;

internal static class Phase2E2ECertificationArtifacts
{
    private static string RepoRoot =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));

    public static string ArtifactsDir
    {
        get
        {
            var dir = Path.Combine(RepoRoot, "artifacts");
            Directory.CreateDirectory(dir);
            return dir;
        }
    }

    public static async Task WriteCrossLayerProofAsync(CrossLayerProof proof, CancellationToken ct = default)
    {
        var path = Path.Combine(ArtifactsDir, "phase2-e2e-cross-layer-proof.md");
        var md = $"""
            # Phase 2 E2E Cross-Layer Proof

            **Invoice:** {proof.InvoiceNumber} (`{proof.InvoiceId}`)
            **Company:** {Phase2E2ETestCompanyIds.CompanyNameEn}

            ## Totals parity

            | Field | DB | Journal (AR Dr) | PDF | Tax Report | Match |
            |-------|---:|----------------:|----:|-------------:|-------|
            | Grand Total | {proof.DbGrandTotal:N2} | {proof.JournalArDebit:N2} | {proof.PdfGrandTotal:N2} | — | {(Math.Abs(proof.DbGrandTotal - proof.PdfGrandTotal) < 0.01m && Math.Abs(proof.DbGrandTotal - proof.JournalArDebit) < 0.01m ? "PASS" : "FAIL")} |
            | Tax Total | {proof.DbTaxTotal:N2} | — | — | {proof.ReportTaxAmount:N2} | {(Math.Abs(proof.DbTaxTotal - proof.ReportTaxAmount) < 0.01m ? "PASS" : "FAIL")} |

            **Overall:** {(proof.AllMatch ? "PASS" : "FAIL")}
            """;
        await File.WriteAllTextAsync(path, md, ct);
    }

    public static async Task WriteRunResultAsync(Phase2E2ERunResult result, CancellationToken ct = default)
    {
        await File.WriteAllTextAsync(
            Path.Combine(ArtifactsDir, "phase2-e2e-last-run-id.txt"),
            result.RunId,
            ct);

        var jsonPath = Path.Combine(ArtifactsDir, "phase2-e2e-run-result.json");
        var payload = new
        {
            result.RunId,
            result.AllPassed,
            Scenarios = result.Scenarios.Select(s => new { s.Name, s.Passed, s.Details, s.InvoiceId, s.ReturnId }),
            Concurrency = new
            {
                result.Concurrency.Passed,
                result.Concurrency.ParallelRequests,
                result.Concurrency.SuccessResponses,
                result.Concurrency.JournalEntryCount,
                result.Concurrency.TaxSnapshotCount
            }
        };
        await File.WriteAllTextAsync(jsonPath, JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true }), ct);
    }

    public static async Task WriteBaselineAsync(
        string prefix,
        AccountingBaselineReportDto baseline,
        AccountingHealthCheckResultDto health,
        CancellationToken ct = default)
    {
        var s = baseline.Summary;
        var md = new StringBuilder();
        md.AppendLine($"# Accounting Baseline — {prefix}");
        md.AppendLine();
        md.AppendLine($"- Generated (UTC): {baseline.GeneratedAtUtc}");
        md.AppendLine($"- Company: {baseline.CompanyName} (`{baseline.CompanyId}`)");
        md.AppendLine();
        md.AppendLine("| Metric | Value |");
        md.AppendLine("|--------|------:|");
        md.AppendLine($"| AR GL | {s.AccountsReceivableGlBalance:N2} |");
        md.AppendLine($"| Stored customer balances | {s.StoredCustomerBalancesTotal:N2} |");
        md.AppendLine($"| Operational inventory | {s.InventoryOperationalValue:N2} |");
        md.AppendLine($"| Inventory GL | {s.InventoryAssetGlBalance:N2} |");
        md.AppendLine($"| Legacy duplicate journal groups | {baseline.DuplicateJournalEntries.Count} |");
        md.AppendLine($"| Unbalanced journals | {baseline.UnbalancedJournalEntries.Count} |");
        md.AppendLine($"| Stuck posting attempts | {health.Checks.Count(c => c.CheckId.Contains("posting", StringComparison.OrdinalIgnoreCase) && c.Status == AccountingHealthStatus.Fail)} |");
        md.AppendLine($"| Health failures | {health.FailCount} |");

        await File.WriteAllTextAsync(Path.Combine(ArtifactsDir, $"{prefix}.md"), md.ToString(), ct);
        await File.WriteAllTextAsync(
            Path.Combine(ArtifactsDir, $"{prefix}.json"),
            JsonSerializer.Serialize(new { baseline, health }, new JsonSerializerOptions { WriteIndented = true }),
            ct);
    }

    public static async Task WriteBaselineDiffAsync(
        AccountingBaselineReportDto pre,
        AccountingBaselineReportDto post,
        CancellationToken ct = default)
    {
        var prodId = DatabaseSeeder.DefaultCompanyId;
        var testId = Phase2E2ETestCompanyIds.CompanyId;
        var md = new StringBuilder();
        md.AppendLine("# Phase 2 E2E Baseline Diff");
        md.AppendLine();
        md.AppendLine("## Production company (must be unchanged)");
        md.AppendLine();
        md.AppendLine("| Metric | Pre | Post | Delta |");
        md.AppendLine("|--------|----:|-----:|------:|");
        AppendDiff(md, "AR GL", pre.Summary.AccountsReceivableGlBalance, post.Summary.AccountsReceivableGlBalance);
        AppendDiff(md, "Operational inventory", pre.Summary.InventoryOperationalValue, post.Summary.InventoryOperationalValue);
        AppendDiff(md, "Inventory GL", pre.Summary.InventoryAssetGlBalance, post.Summary.InventoryAssetGlBalance);
        AppendDiff(md, "Stored customer balances", pre.Summary.StoredCustomerBalancesTotal, post.Summary.StoredCustomerBalancesTotal);
        md.AppendLine();
        md.AppendLine($"Production CompanyId: `{prodId}`");
        md.AppendLine($"Test CompanyId: `{testId}` (`{Phase2E2ETestCompanyIds.CompanyNameEn}`)");
        md.AppendLine();
        md.AppendLine("## Test company");
        md.AppendLine();
        md.AppendLine("Test company metrics are isolated; E2E documents use `E2E-TAX-{RunId}-*` invoice numbers only under the test company.");

        await File.WriteAllTextAsync(Path.Combine(ArtifactsDir, "phase2-e2e-baseline-diff.md"), md.ToString(), ct);
    }

    private static void AppendDiff(StringBuilder sb, string label, decimal pre, decimal post)
    {
        var delta = post - pre;
        var status = Math.Abs(delta) < 0.01m ? "OK" : "**DRIFT**";
        sb.AppendLine($"| {label} | {pre:N2} | {post:N2} | {delta:N2} {status} |");
    }
}
