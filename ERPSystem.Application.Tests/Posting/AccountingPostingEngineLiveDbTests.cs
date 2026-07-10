using ERPSystem.Application.Abstractions.Services;
using ERPSystem.Application.Common;
using ERPSystem.Application.Posting;
using ERPSystem.Domain.Enums;
using ERPSystem.Infrastructure.DependencyInjection;
using ERPSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace ERPSystem.Application.Tests.Posting;

public sealed class AccountingPostingEngineLiveDbTests
{
    private const string DefaultConnection =
        "Host=localhost;Port=5433;Database=erp_pro;Username=erp_app;Password=12345678;SSL Mode=Disable";

    [Fact]
    public async Task PostAsync_rejects_unbalanced_lines_before_save()
    {
        var connected = await CanConnectAsync();
        if (!connected)
            return;

        await using var provider = BuildProvider();
        using var scope = provider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ErpDbContext>();
        var engine = scope.ServiceProvider.GetRequiredService<IAccountingPostingEngine>();

        var companyId = await context.Companies.AsNoTracking().Select(c => c.Id).FirstAsync();
        var branchId = await context.Branches.AsNoTracking()
            .Where(b => b.CompanyId == companyId)
            .Select(b => b.Id)
            .FirstAsync();

        var request = new PostingRequest
        {
            CompanyId = companyId,
            BranchId = branchId,
            SourceType = DocumentType.ChinaContainer,
            SourceId = Guid.NewGuid(),
            PostingKind = PostingKind.ChinaContainerLandingCost,
            PostingDate = DateTime.UtcNow,
            UserId = Guid.Parse("33333333-3333-3333-3333-333333333333"),
            Description = "Phase1 test unbalanced",
            JournalBookId = JournalBookIds.Purchase,
            Lines =
            [
                new PostingLineRequest
                {
                    AccountId = AccountingAccountIds.LandingCostClearing,
                    Debit = 100m,
                    Credit = 0m,
                    Narrative = "test"
                },
                new PostingLineRequest
                {
                    AccountId = AccountingAccountIds.AccountsPayable,
                    Debit = 0m,
                    Credit = 50m,
                    Narrative = "test"
                }
            ]
        };

        var result = await engine.PostAsync(request);
        Assert.False(result.Success);
        Assert.Equal("posting_failed", result.ErrorCode);
    }

    [Fact]
    public async Task Parallel_posting_same_identity_yields_single_journal_entry()
    {
        var connected = await CanConnectAsync();
        if (!connected)
            return;

        await using var provider = BuildProvider();
        using var scope = provider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ErpDbContext>();
        var engine = scope.ServiceProvider.GetRequiredService<IAccountingPostingEngine>();
        var coordinator = scope.ServiceProvider.GetRequiredService<IPostingSaveCoordinator>();

        var companyId = await context.Companies.AsNoTracking().Select(c => c.Id).FirstAsync();
        var branchId = await context.Branches.AsNoTracking()
            .Where(b => b.CompanyId == companyId)
            .Select(b => b.Id)
            .FirstAsync();

        var sourceId = Guid.NewGuid();
        var amount = 12.34m;
        var request = new PostingRequest
        {
            CompanyId = companyId,
            BranchId = branchId,
            SourceType = DocumentType.ChinaContainer,
            SourceId = sourceId,
            PostingKind = PostingKind.ChinaContainerLandingCost,
            PostingDate = DateTime.UtcNow,
            UserId = Guid.Parse("33333333-3333-3333-3333-333333333333"),
            Description = $"Phase1 concurrency test {sourceId:N}",
            JournalBookId = JournalBookIds.Purchase,
            Lines =
            [
                new PostingLineRequest
                {
                    AccountId = AccountingAccountIds.LandingCostClearing,
                    Debit = amount,
                    Credit = 0m,
                    Narrative = "concurrency test"
                },
                new PostingLineRequest
                {
                    AccountId = AccountingAccountIds.AccountsPayable,
                    Debit = 0m,
                    Credit = amount,
                    Narrative = "concurrency test"
                }
            ]
        };

        async Task<PostingResult> PostOnceAsync()
        {
            await using var innerProvider = BuildProvider();
            using var innerScope = innerProvider.CreateScope();
            var innerEngine = innerScope.ServiceProvider.GetRequiredService<IAccountingPostingEngine>();
            var innerCoordinator = innerScope.ServiceProvider.GetRequiredService<IPostingSaveCoordinator>();
            var result = await innerEngine.PostAsync(request);
            if (result.Success && !result.AlreadyPosted)
                await innerCoordinator.SaveChangesWithPostingRecoveryAsync([request]);
            return result;
        }

        var tasks = Enumerable.Range(0, 20).Select(_ => PostOnceAsync()).ToArray();
        var results = await Task.WhenAll(tasks);

        Assert.All(results, r => Assert.True(r.Success));
        Assert.Equal(1, results.Select(r => r.JournalEntryId).Distinct().Count());

        var count = await context.JournalEntries.AsNoTracking()
            .Where(j => j.CompanyId == companyId
                        && j.SourceId == sourceId
                        && j.PostingKind == (int)PostingKind.ChinaContainerLandingCost
                        && j.PostingIdentityVersion == 2)
            .CountAsync();

        Assert.Equal(1, count);

        // Cleanup test journal only (not legacy data).
        var testEntry = await context.JournalEntries
            .FirstAsync(j => j.SourceId == sourceId && j.PostingIdentityVersion == 2);
        var lines = await context.JournalEntryLines.Where(l => l.JournalEntryId == testEntry.Id).ToListAsync();
        context.JournalEntryLines.RemoveRange(lines);
        context.JournalEntries.Remove(testEntry);
        await context.SaveChangesAsync();
    }

    private static ServiceProvider BuildProvider()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = DefaultConnection
            })
            .Build();

        var services = new ServiceCollection();
        services.AddInfrastructure(configuration);
        services.AddLogging();
        return services.BuildServiceProvider();
    }

    private static async Task<bool> CanConnectAsync()
    {
        try
        {
            await using var conn = new NpgsqlConnection(DefaultConnection);
            await conn.OpenAsync();
            return true;
        }
        catch
        {
            return false;
        }
    }
}
