using ERPSystem.Application.Abstractions;
using ERPSystem.Application.Abstractions.Services;
using ERPSystem.Application.DTOs.Accounting;
using ERPSystem.Application.DependencyInjection;
using ERPSystem.Infrastructure.DependencyInjection;
using ERPSystem.Infrastructure.E2E;
using ERPSystem.Infrastructure.Persistence;
using ERPSystem.Infrastructure.Seed;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace ERPSystem.Application.Tests.E2E;

/// <summary>Live DB Phase 2 tax E2E integration tests — isolated test company only.</summary>
public sealed class Phase2TaxE2EIntegrationTests
{
    private const string DefaultConnection =
        "Host=localhost;Port=5433;Database=erp_pro;Username=erp_app;Password=12345678;SSL Mode=Disable";

    private static async Task<bool> CanConnectAsync()
    {
        try
        {
            await using var conn = new NpgsqlConnection(DefaultConnection);
            await conn.OpenAsync();
            return true;
        }
        catch { return false; }
    }

    [Fact]
    public async Task E2E_All_scenarios_full_certification_run()
    {
        if (!await CanConnectAsync()) return;

        await using var sp = BuildProvider();

        AccountingBaselineReportDto preBaseline;
        AccountingHealthCheckResultDto preHealth;
        using (var preScope = sp.CreateScope())
        {
            var baselineService = preScope.ServiceProvider.GetRequiredService<IAccountingBaselineReportService>();
            var healthService = preScope.ServiceProvider.GetRequiredService<IAccountingHealthCheckService>();
            preBaseline = await baselineService.GenerateAsync(DatabaseSeeder.DefaultCompanyId);
            preHealth = await healthService.RunAsync(DatabaseSeeder.DefaultCompanyId);
        }
        await Phase2E2ECertificationArtifacts.WriteBaselineAsync("phase2-e2e-pre", preBaseline, preHealth);

        await SeedAsync(sp);
        using var scope = sp.CreateScope();
        var runner = scope.ServiceProvider.GetRequiredService<Phase2TaxE2ECertificationRunner>();
        var result = await runner.RunAllScenariosAsync();
        await Phase2E2ECertificationArtifacts.WriteRunResultAsync(result);

        if (result.ScenarioA.InvoiceId != Guid.Empty)
        {
            var proof = await runner.BuildCrossLayerProofAsync(result.ScenarioA.InvoiceId);
            await Phase2E2ECertificationArtifacts.WriteCrossLayerProofAsync(proof);
            Assert.True(proof.AllMatch);
        }

        AccountingBaselineReportDto postBaseline;
        AccountingHealthCheckResultDto postHealth;
        using (var postScope = sp.CreateScope())
        {
            var baselineService = postScope.ServiceProvider.GetRequiredService<IAccountingBaselineReportService>();
            var healthService = postScope.ServiceProvider.GetRequiredService<IAccountingHealthCheckService>();
            postBaseline = await baselineService.GenerateAsync(DatabaseSeeder.DefaultCompanyId);
            postHealth = await healthService.RunAsync(DatabaseSeeder.DefaultCompanyId);
        }
        await Phase2E2ECertificationArtifacts.WriteBaselineAsync("phase2-e2e-post", postBaseline, postHealth);
        await Phase2E2ECertificationArtifacts.WriteBaselineDiffAsync(preBaseline, postBaseline);

        Assert.True(result.AllPassed, string.Join("; ", result.Scenarios.Where(s => !s.Passed).Select(s => $"{s.Name}:{s.Details}")));
        Assert.True(result.Concurrency.Passed);
        Assert.Equal(1, result.Concurrency.JournalEntryCount);

        Assert.Equal(320.00m, preBaseline.Summary.AccountsReceivableGlBalance);
        Assert.Equal(preBaseline.Summary.AccountsReceivableGlBalance, postBaseline.Summary.AccountsReceivableGlBalance);
        Assert.Equal(preBaseline.Summary.InventoryOperationalValue, postBaseline.Summary.InventoryOperationalValue);
        Assert.Equal(preBaseline.Summary.InventoryAssetGlBalance, postBaseline.Summary.InventoryAssetGlBalance);
    }

    [Fact]
    public async Task Matrix_25_Legacy_journal_unchanged_read_only()
    {
        if (!await CanConnectAsync()) return;
        await using var sp = BuildProvider();
        using var scope = sp.CreateScope();
        var runner = scope.ServiceProvider.GetRequiredService<Phase2TaxE2ECertificationRunner>();
        var result = await runner.RunLegacyReadOnlyScenarioAsync();
        Assert.True(result.Passed, result.Details);
    }

    [Fact]
    public async Task E2E_ScenarioA_Exclusive_VAT_full_flow()
    {
        if (!await CanConnectAsync()) return;
        await using var sp = BuildProvider();
        await SeedAsync(sp);
        using var scope = sp.CreateScope();
        var runner = scope.ServiceProvider.GetRequiredService<Phase2TaxE2ECertificationRunner>();
        var result = await runner.RunExclusiveScenarioAsync();
        Assert.True(result.Passed, result.Details);
    }

    [Fact]
    public async Task E2E_ScenarioB_Inclusive_VAT_full_flow()
    {
        if (!await CanConnectAsync()) return;
        await using var sp = BuildProvider();
        await SeedAsync(sp);
        using var scope = sp.CreateScope();
        var runner = scope.ServiceProvider.GetRequiredService<Phase2TaxE2ECertificationRunner>();
        var result = await runner.RunInclusiveScenarioAsync();
        Assert.True(result.Passed, result.Details);
    }

    [Fact]
    public async Task Matrix_21_22_Concurrent_taxed_approval_single_journal()
    {
        if (!await CanConnectAsync()) return;
        await using var sp = BuildProvider();
        await SeedAsync(sp);
        using var scope = sp.CreateScope();
        var runner = scope.ServiceProvider.GetRequiredService<Phase2TaxE2ECertificationRunner>();
        var result = await runner.RunConcurrencyTestAsync();
        Assert.True(result.Passed);
        Assert.Equal(1, result.JournalEntryCount);
    }

    [Fact]
    public async Task Matrix_23_Rollback_on_approval_failure()
    {
        if (!await CanConnectAsync()) return;
        await using var sp = BuildProvider();
        await SeedAsync(sp);
        using var scope = sp.CreateScope();
        var runner = scope.ServiceProvider.GetRequiredService<Phase2TaxE2ECertificationRunner>();
        var result = await runner.RunRollbackTestAsync();
        Assert.True(result.Passed, result.Details);
    }

    [Fact]
    public async Task Matrix_32_33_Cross_layer_and_tax_report_parity()
    {
        if (!await CanConnectAsync()) return;
        await using var sp = BuildProvider();
        await SeedAsync(sp);
        using var scope = sp.CreateScope();
        var runner = scope.ServiceProvider.GetRequiredService<Phase2TaxE2ECertificationRunner>();
        var scenario = await runner.RunExclusiveScenarioAsync();
        Assert.True(scenario.Passed, scenario.Details);
        var proof = await runner.BuildCrossLayerProofAsync(scenario.InvoiceId);
        Assert.True(proof.AllMatch);
        Assert.Equal(proof.DbTaxTotal, proof.ReportTaxAmount);
    }

    [Fact]
    public async Task Matrix_34_Company_isolation_no_test_data_in_production()
    {
        if (!await CanConnectAsync()) return;
        await using var sp = BuildProvider();
        await SeedAsync(sp);
        using var scope = sp.CreateScope();
        var runner = scope.ServiceProvider.GetRequiredService<Phase2TaxE2ECertificationRunner>();
        var result = await runner.RunCompanyIsolationTestAsync();
        Assert.True(result.Passed, result.Details);
    }

    [Fact]
    public async Task Production_company_invoice_count_unchanged_after_test_operations()
    {
        if (!await CanConnectAsync()) return;
        await using var sp = BuildProvider();
        using var scope1 = sp.CreateScope();
        var context = scope1.ServiceProvider.GetRequiredService<ErpDbContext>();
        var countBefore = await context.Set<ERPSystem.Infrastructure.Persistence.Models.Sales.SalesInvoiceEntity>()
            .CountAsync(i => i.CompanyId == DatabaseSeeder.DefaultCompanyId);

        await SeedAsync(sp);
        using var scope2 = sp.CreateScope();
        var runner = scope2.ServiceProvider.GetRequiredService<Phase2TaxE2ECertificationRunner>();
        await runner.RunExclusiveScenarioAsync();

        using var scope3 = sp.CreateScope();
        var ctx2 = scope3.ServiceProvider.GetRequiredService<ErpDbContext>();
        var countAfter = await ctx2.Set<ERPSystem.Infrastructure.Persistence.Models.Sales.SalesInvoiceEntity>()
            .CountAsync(i => i.CompanyId == DatabaseSeeder.DefaultCompanyId);
        Assert.Equal(countBefore, countAfter);
    }

    private static async Task SeedAsync(ServiceProvider sp)
    {
        using var scope = sp.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ErpDbContext>();
        await Phase2E2ETestCompanySeeder.SeedAsync(context);
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
        services.AddSingleton<ICurrentUserService, E2ETestUserService>();
        services.AddSingleton<ICurrentBranchService, E2ETestBranchService>();
        services.AddSingleton<IPermissionService, E2EAllowAllPermissions>();
        services.AddInfrastructure(configuration);
        services.AddApplication();
        services.AddScoped<Phase2TaxE2ECertificationRunner>();
        return services.BuildServiceProvider();
    }

    private sealed class E2ETestUserService : ICurrentUserService
    {
        public Guid? UserId => DatabaseSeeder.AdminUserId;
        public string? Username => "admin";
        public bool IsAuthenticated => true;
    }

    private sealed class E2ETestBranchService : ICurrentBranchService
    {
        public Guid? CompanyId => Phase2E2ETestCompanyIds.CompanyId;
        public Guid? BranchId => Phase2E2ETestCompanyIds.BranchId;
    }

    private sealed class E2EAllowAllPermissions : IPermissionService
    {
        public Task<bool> CanAsync(string permissionCode, CancellationToken cancellationToken = default) =>
            Task.FromResult(true);
        public Task EnsureCanAsync(string permissionCode, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }
}
