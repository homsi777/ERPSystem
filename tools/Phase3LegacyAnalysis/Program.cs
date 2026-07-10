using System.Text;
using ERPSystem.Application.Common;
using ERPSystem.Domain.Enums;
using ERPSystem.Infrastructure.DependencyInjection;
using ERPSystem.Infrastructure.Persistence;
using ERPSystem.Infrastructure.Seed;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

var repoRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
var mode = args.FirstOrDefault(a => a.StartsWith("--")) ?? "--all";

var configuration = new ConfigurationBuilder()
    .SetBasePath(repoRoot)
    .AddJsonFile("appsettings.json", optional: false)
    .AddJsonFile("appsettings.Local.json", optional: true)
    .AddEnvironmentVariables()
    .Build();

using var tunnel = ERPSystem.Services.SshTunnelService.StartIfConfigured(configuration);

var services = new ServiceCollection();
services.AddInfrastructure(configuration);
await using var provider = services.BuildServiceProvider();

using var scope = provider.CreateScope();
var context = scope.ServiceProvider.GetRequiredService<ErpDbContext>();

var artifactsDir = Path.Combine(repoRoot, "artifacts");
Directory.CreateDirectory(artifactsDir);

return mode switch
{
    "--cashbox-mapping" => await WriteCashboxMappingAsync(context, artifactsDir),
    "--legacy-receipts" => await WriteLegacyReceiptsAsync(context, artifactsDir),
    "--all" => await RunAllAsync(context, artifactsDir),
    _ => PrintHelp()
};

static int PrintHelp()
{
    Console.WriteLine("Phase 3 Legacy Analysis (read-only)");
    Console.WriteLine("  --cashbox-mapping");
    Console.WriteLine("  --legacy-receipts");
    Console.WriteLine("  --all");
    return 0;
}

static async Task<int> RunAllAsync(ErpDbContext context, string artifactsDir)
{
    await WriteCashboxMappingAsync(context, artifactsDir);
    await WriteLegacyReceiptsAsync(context, artifactsDir);
    return 0;
}

static async Task<int> WriteCashboxMappingAsync(ErpDbContext context, string artifactsDir)
{
    var cashboxes = await context.Cashboxes.AsNoTracking()
        .Join(context.Branches.AsNoTracking(), c => c.BranchId, b => b.Id, (c, b) => new { c, b.CompanyId })
        .Join(context.Companies.AsNoTracking(), x => x.CompanyId, co => co.Id, (x, co) => new { x.c, co.NameEn })
        .OrderBy(x => x.NameEn).ThenBy(x => x.c.Code)
        .ToListAsync();

    var accounts = await context.Accounts.AsNoTracking()
        .ToDictionaryAsync(a => a.Id, a => new { a.Code, a.NameEn, a.CompanyId, a.IsActive });

    var sb = new StringBuilder();
    sb.AppendLine("# Phase 3 — Cashbox Account Mapping Required");
    sb.AppendLine();
    sb.AppendLine("| Cashbox | Company | Active | Currency | Operational balance | AccountId | GL account | Classification |");
    sb.AppendLine("| ------- | ------- | ------ | -------- | ------------------: | --------- | ---------- | -------------- |");

    foreach (var row in cashboxes)
    {
        var c = row.c;
        string classification;
        string gl = "—";

        if (!c.IsActive)
            classification = "Inactive";
        else if (c.AccountId is not Guid accountId)
            classification = "MissingAccount";
        else if (!accounts.TryGetValue(accountId, out var acc))
            classification = "InvalidAccount";
        else
        {
            gl = $"{acc.Code} — {acc.NameEn}";
            if (acc.CompanyId != row.CompanyId)
                classification = "CrossCompanyAccount";
            else if (!acc.IsActive)
                classification = "InvalidAccount";
            else if (!string.Equals(c.Currency, "USD", StringComparison.OrdinalIgnoreCase))
                classification = "CurrencyMismatch";
            else
                classification = "Mapped";
        }

        sb.AppendLine($"| {c.Name} | {row.NameEn} | {c.IsActive} | {c.Currency} | {c.Balance:N2} | {c.AccountId?.ToString() ?? "null"} | {gl} | {classification} |");
    }

    var path = Path.Combine(artifactsDir, "phase3-cashbox-account-mapping-required.md");
    await File.WriteAllTextAsync(path, sb.ToString());
    Console.WriteLine($"Wrote {path}");
    return 0;
}

static async Task<int> WriteLegacyReceiptsAsync(ErpDbContext context, string artifactsDir)
{
    var posted = (int)VoucherStatus.Posted;
    var reversed = (int)VoucherStatus.Reversed;
    var receipts = await context.ReceiptVouchers.AsNoTracking()
        .Where(v => v.Status == posted || v.Status == reversed)
        .Join(context.Cashboxes.AsNoTracking(), v => v.CashboxId, c => c.Id, (v, c) => new { v, c })
        .OrderBy(x => x.v.VoucherDate)
        .ToListAsync();

    var journals = await context.JournalEntries.AsNoTracking()
        .Where(j => j.SourceType == (int)DocumentType.ReceiptVoucher
                    || j.PostingKind == (int)PostingKind.ReceiptVoucherCollection
                    || j.PostingKind == (int)PostingKind.ReceiptVoucherReversal)
        .ToListAsync();
    var journalBySource = journals.ToLookup(j => j.SourceId);

    var lines = await context.JournalEntryLines.AsNoTracking().ToListAsync();
    var linesByJe = lines.ToLookup(l => l.JournalEntryId);

    var sb = new StringBuilder();
    sb.AppendLine("# Phase 3 — Legacy Receipt Posting Analysis");
    sb.AppendLine();
    sb.AppendLine("**Read-only — no modifications applied.**");
    sb.AppendLine();
    sb.AppendLine("| Receipt | Cashbox | Amount | Actual GL account | Expected account | Journal | Classification |");
    sb.AppendLine("| ------- | ------- | -----: | ----------------- | ---------------- | ------- | -------------- |");

    foreach (var row in receipts)
    {
        var v = row.v;
        var je = journalBySource[v.Id].FirstOrDefault();
        var actualGl = "—";
        var classification = "Unknown";

        if (je is null)
            classification = "MissingJournal";
        else
        {
            var debitLine = linesByJe[je.Id].FirstOrDefault(l => l.Debit > 0);
            if (debitLine is not null)
            {
                actualGl = debitLine.AccountId.ToString();
                if (debitLine.AccountId == AccountingAccountIds.CashUsd)
                    classification = "PostedToLegacyCashUsd";
                else if (row.c.AccountId is Guid expected && debitLine.AccountId == expected)
                    classification = "Correct";
                else if (row.c.AccountId is null)
                    classification = "MissingCashbox";
                else
                    classification = "Unknown";
            }
        }

        var expected = row.c.AccountId?.ToString() ?? "null";
        sb.AppendLine($"| {v.VoucherNumber} | {row.c.Name} | {v.Amount:N2} | {actualGl} | {expected} | {je?.EntryNumber ?? "—"} | {classification} |");
    }

    var path = Path.Combine(artifactsDir, "phase3-legacy-receipt-posting-analysis.md");
    await File.WriteAllTextAsync(path, sb.ToString());
    Console.WriteLine($"Wrote {path}");
    return 0;
}
