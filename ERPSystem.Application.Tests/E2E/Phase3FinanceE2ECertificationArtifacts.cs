using System.Text;
using System.Text.Json;
using ERPSystem.Application.Abstractions.Services;
using ERPSystem.Application.DTOs.Accounting;
using ERPSystem.Infrastructure.E2E;
using ERPSystem.Infrastructure.Seed;

namespace ERPSystem.Application.Tests.E2E;

internal static class Phase3FinanceE2ECertificationArtifacts
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

    public static async Task WriteMatrixResultAsync(Phase3FinanceE2ERunResult result, CancellationToken ct = default)
    {
        await File.WriteAllTextAsync(
            Path.Combine(ArtifactsDir, "phase3-e2e-last-run-id.txt"),
            result.RunId,
            ct);

        var json = JsonSerializer.Serialize(new
        {
            result.RunId,
            Passed = result.PassedCount,
            Failed = result.FailedCount,
            result.AllPassed,
            Matrix = result.Matrix.Select(m => new { m.Index, m.Name, m.Passed, m.Details })
        }, new JsonSerializerOptions { WriteIndented = true });

        await File.WriteAllTextAsync(Path.Combine(ArtifactsDir, "phase3-e2e-matrix.json"), json, ct);

        var md = new StringBuilder();
        md.AppendLine("# Phase 3 Finance E2E — 28 Test Matrix");
        md.AppendLine();
        md.AppendLine($"**RunId:** {result.RunId}");
        md.AppendLine($"**Passed:** {result.PassedCount} / **Failed:** {result.FailedCount}");
        md.AppendLine();
        md.AppendLine("| # | Test | Result | Details |");
        md.AppendLine("|---:|------|--------|---------|");
        foreach (var m in result.Matrix)
            md.AppendLine($"| {m.Index} | {m.Name} | {(m.Passed ? "PASS" : "FAIL")} | {m.Details} |");
        await File.WriteAllTextAsync(Path.Combine(ArtifactsDir, "phase3-e2e-matrix.md"), md.ToString(), ct);
    }

    public static async Task WriteCrossLayerProofAsync(Phase3ReceiptCrossLayerProof proof, CancellationToken ct = default)
    {
        var md = new StringBuilder();
        md.AppendLine("# Phase 3 Receipt Cross-Layer Proof");
        md.AppendLine();
        md.AppendLine($"**Receipt:** {proof.VoucherNumber} (`{proof.ReceiptId}`)");
        md.AppendLine($"**Company:** {Phase3FinanceE2ETestCompanyIds.CompanyNameEn}");
        md.AppendLine();
        md.AppendLine("| Field | WPF | API | DB | Journal | PDF | Reconciliation | Match |");
        md.AppendLine("|-------|-----|-----|----|---------|----:|----------------|-------|");
        void Row(string field, string wpf, string api, string db, string journal, string pdf, string recon, bool match) =>
            md.AppendLine($"| {field} | {wpf} | {api} | {db} | {journal} | {pdf} | {recon} | {(match ? "PASS" : "FAIL")} |");

        Row("Receipt number", proof.DtoVoucherNumber, proof.DtoVoucherNumber, proof.VoucherNumber,
            proof.JournalEntryId?.ToString() ?? "—", proof.PdfVoucherNumber, "—",
            string.Equals(proof.DtoVoucherNumber, proof.VoucherNumber, StringComparison.Ordinal));
        Row("Amount", proof.DtoAmount.ToString("N2"), proof.DtoAmount.ToString("N2"), proof.DbAmount.ToString("N2"),
            proof.JournalCashDebit.ToString("N2"), proof.PdfAmount.ToString("N2"), "—",
            Math.Abs(proof.DbAmount - proof.JournalCashDebit) < 0.01m);
        Row("AR credit", "—", "—", "—", proof.JournalArOrAdvanceCredit.ToString("N2"), "—", "—",
            Math.Abs(proof.DbAmount - proof.JournalArOrAdvanceCredit) < 0.01m);
        Row("Journal source", "—", proof.JournalSourceId?.ToString() ?? "—", proof.ReceiptId.ToString(),
            proof.JournalSourceId?.ToString() ?? "—", "—", "—",
            proof.JournalSourceId == proof.ReceiptId);
        Row("Posting kind", "—", "—", "—",
            proof.JournalPostingKind?.ToString() ?? "—", "—", "—",
            proof.JournalPostingKind == (int)ERPSystem.Domain.Enums.PostingKind.ReceiptVoucherCollection);

        md.AppendLine();
        md.AppendLine($"**Overall:** {(proof.AllMatch ? "PASS" : "FAIL")}");
        await File.WriteAllTextAsync(Path.Combine(ArtifactsDir, "phase3-receipt-cross-layer-proof.md"), md.ToString(), ct);
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
        md.AppendLine($"**CompanyId:** `{baseline.CompanyId}`");
        md.AppendLine($"**Generated (UTC):** {baseline.GeneratedAtUtc}");
        md.AppendLine();
        md.AppendLine("| Metric | Value |");
        md.AppendLine("|--------|------:|");
        md.AppendLine($"| AR GL | {s.AccountsReceivableGlBalance:N2} |");
        md.AppendLine($"| Inventory operational | {s.InventoryOperationalValue:N2} |");
        md.AppendLine($"| Inventory GL | {s.InventoryAssetGlBalance:N2} |");
        md.AppendLine($"| CashUsd GL | {s.CashUsdGlBalance:N2} |");
        md.AppendLine($"| Unbalanced journals | {baseline.UnbalancedJournalEntries.Count} |");
        await File.WriteAllTextAsync(Path.Combine(ArtifactsDir, $"{prefix}.md"), md.ToString(), ct);
        await File.WriteAllTextAsync(
            Path.Combine(ArtifactsDir, $"{prefix}.json"),
            JsonSerializer.Serialize(baseline, new JsonSerializerOptions { WriteIndented = true }),
            ct);
    }

    public static async Task WriteBaselineDiffAsync(
        AccountingBaselineReportDto production,
        AccountingBaselineReportDto testCompany,
        CancellationToken ct = default)
    {
        var md = new StringBuilder();
        md.AppendLine("# Phase 3 Baseline Diff");
        md.AppendLine();
        md.AppendLine("## Production company (must be unchanged)");
        md.AppendLine($"CompanyId: `{production.CompanyId}`");
        md.AppendLine($"AR: {production.Summary.AccountsReceivableGlBalance:N2}");
        md.AppendLine($"Operational inventory: {production.Summary.InventoryOperationalValue:N2}");
        md.AppendLine($"Inventory GL: {production.Summary.InventoryAssetGlBalance:N2}");
        md.AppendLine();
        md.AppendLine("## Test company");
        md.AppendLine($"CompanyId: `{testCompany.CompanyId}`");
        md.AppendLine($"AR: {testCompany.Summary.AccountsReceivableGlBalance:N2}");
        md.AppendLine($"Operational inventory: {testCompany.Summary.InventoryOperationalValue:N2}");
        await File.WriteAllTextAsync(Path.Combine(ArtifactsDir, "phase3-baseline-diff.md"), md.ToString(), ct);
    }
}
