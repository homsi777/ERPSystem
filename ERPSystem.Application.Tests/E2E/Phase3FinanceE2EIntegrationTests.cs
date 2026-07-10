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

/// <summary>Live DB Phase 3 finance E2E — isolated test company on erp_pro_phase3_e2e only.</summary>
public sealed class Phase3FinanceE2EIntegrationTests
{
    private const string DefaultConnection =
        "Host=localhost;Port=5432;Database=erp_pro_phase3_e2e;Username=postgres;Password=12345678";

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
    public async Task E2E_28_matrix_full_certification_run()
    {
        if (!await CanConnectAsync()) return;

        await using var sp = BuildProvider();
        await SeedAsync(sp);

        AccountingBaselineReportDto preProdBaseline;
        using (var preScope = sp.CreateScope())
        {
            var baseline = preScope.ServiceProvider.GetRequiredService<IAccountingBaselineReportService>();
            preProdBaseline = await baseline.GenerateAsync(DatabaseSeeder.DefaultCompanyId);
        }

        using var scope = sp.CreateScope();
        var runner = scope.ServiceProvider.GetRequiredService<Phase3FinanceE2ECertificationRunner>();
        var result = await runner.RunAllMatrixAsync();
        await Phase3FinanceE2ECertificationArtifacts.WriteMatrixResultAsync(result);

        if (result.ProofReceiptId is Guid proofId && proofId != Guid.Empty)
        {
            var proof = await runner.BuildCrossLayerProofAsync(proofId);
            await Phase3FinanceE2ECertificationArtifacts.WriteCrossLayerProofAsync(proof);
            Assert.True(proof.AllMatch);
        }

        AccountingBaselineReportDto postProdBaseline;
        AccountingBaselineReportDto postTestBaseline;
        using (var postScope = sp.CreateScope())
        {
            var baseline = postScope.ServiceProvider.GetRequiredService<IAccountingBaselineReportService>();
            postProdBaseline = await baseline.GenerateAsync(DatabaseSeeder.DefaultCompanyId);
            postTestBaseline = await baseline.GenerateAsync(Phase3FinanceE2ETestCompanyIds.CompanyId);
        }

        await Phase3FinanceE2ECertificationArtifacts.WriteBaselineDiffAsync(preProdBaseline, postTestBaseline);

        Assert.Equal(28, result.Matrix.Count);
        Assert.Equal(0, result.FailedCount);
        Assert.True(result.AllPassed, string.Join("; ",
            result.Matrix.Where(m => !m.Passed).Select(m => $"{m.Index}:{m.Name}:{m.Details}")));

        Assert.Equal(preProdBaseline.Summary.AccountsReceivableGlBalance,
            postProdBaseline.Summary.AccountsReceivableGlBalance);
        Assert.Equal(preProdBaseline.Summary.InventoryOperationalValue,
            postProdBaseline.Summary.InventoryOperationalValue);
        Assert.Equal(preProdBaseline.Summary.InventoryAssetGlBalance,
            postProdBaseline.Summary.InventoryAssetGlBalance);
    }

    [Fact]
    public void E2E_production_guard_blocks_erp_pro_writes()
    {
        Assert.Throws<InvalidOperationException>(() =>
            E2EProductionGuard.GuardWritableE2E(
                "Host=localhost;Port=5432;Database=erp_pro;Username=postgres;Password=x"));
    }

    private static async Task SeedAsync(ServiceProvider sp)
    {
        using var scope = sp.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ErpDbContext>();
        await Phase3FinanceE2ETestCompanySeeder.SeedAsync(context);
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
        services.AddSingleton<Phase3E2EPermissionGate>();
        services.AddSingleton<IPermissionService>(sp => sp.GetRequiredService<Phase3E2EPermissionGate>());
        services.AddInfrastructure(configuration);
        services.AddApplication();
        services.AddScoped<Phase3FinanceE2ECertificationRunner>();
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
        public Guid? CompanyId => Phase3FinanceE2ETestCompanyIds.CompanyId;
        public Guid? BranchId => Phase3FinanceE2ETestCompanyIds.BranchId;
    }
}
