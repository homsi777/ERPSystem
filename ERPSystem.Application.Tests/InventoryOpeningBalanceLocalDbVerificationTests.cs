using ERPSystem.Application.Abstractions;
using ERPSystem.Application.Abstractions.Repositories;
using ERPSystem.Application.Abstractions.Services;
using ERPSystem.Application.Common;
using ERPSystem.Infrastructure.DependencyInjection;
using ERPSystem.Infrastructure.Persistence;
using ERPSystem.Infrastructure.Persistence.Models.Catalog;
using ERPSystem.Infrastructure.Seed;
using ERPSystem.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ERPSystem.Application.Tests;

public sealed class InventoryOpeningBalanceLocalDbVerificationTests
{
    private const string ConnectionString =
        "Host=localhost;Port=5432;Database=erp_pro;Username=postgres;Password=12345678";

    [Fact]
    public async Task Local_db_source_type_and_legacy_length_are_verified_then_rolled_back()
    {
        if (Environment.GetEnvironmentVariable("ERP_LOCAL_DB_TESTS") != "1")
            return;

        await using var provider = BuildProvider();
        using var scope = provider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ErpDbContext>();
        await using var tx = await db.Database.BeginTransactionAsync();

        var accounting = scope.ServiceProvider.GetRequiredService<IIntegratedAccountingService>();
        var reports = scope.ServiceProvider.GetRequiredService<IAccountingReportRepository>();
        var documentId = Guid.NewGuid();
        var partyId = Guid.NewGuid();
        const decimal amount = 12.34m;

        await accounting.PostOpeningBalanceDocumentAsync(
            documentId,
            $"TEST-{documentId:N}",
            "Local rollback-only opening balance verification",
            DateTime.UtcNow,
            [
                new JournalLineSpec(AccountingAccountIds.AccountsReceivable, amount, 0m, "test", partyId),
                new JournalLineSpec(AccountingAccountIds.OpeningBalanceEquity, 0m, amount, "test", null)
            ]);
        await db.SaveChangesAsync();

        var visibleOpening = await reports.GetPartyOpeningBalanceAsync(
            partyId, OpeningBalanceDocumentTypePolicy.SourceType);
        Assert.Equal(amount, visibleOpening);

        var template = await db.FabricRolls.AsNoTracking().FirstAsync();
        var legacy = Clone(template, Guid.NewGuid(), isLegacy: true, confirmed: false, length: 20m);
        var china = Clone(template, Guid.NewGuid(), isLegacy: false, confirmed: true, length: 20m);
        db.FabricRolls.AddRange(legacy, china);
        await db.SaveChangesAsync();

        Assert.Equal(27.5m,
            LegacyOpeningBalanceRollLengthPolicy.ResolveAndValidateSaleLength(legacy, 27.5m));
        Assert.Throws<ERPSystem.Domain.Exceptions.InventoryException>(() =>
            LegacyOpeningBalanceRollLengthPolicy.ResolveAndValidateSaleLength(china, 27.5m));
        await db.SaveChangesAsync();

        var storedLegacy = await db.FabricRolls.AsNoTracking().SingleAsync(r => r.Id == legacy.Id);
        Assert.Equal(27.5m, storedLegacy.LengthMeters);
        Assert.Equal(27.5m, storedLegacy.RemainingLengthMeters);
        Assert.True(storedLegacy.LegacyLengthConfirmed);

        await tx.RollbackAsync();

        Assert.False(await db.JournalEntries.AsNoTracking().AnyAsync(j => j.SourceId == documentId));
        Assert.False(await db.FabricRolls.AsNoTracking().AnyAsync(r => r.Id == legacy.Id || r.Id == china.Id));
    }

    private static FabricRollEntity Clone(
        FabricRollEntity source,
        Guid id,
        bool isLegacy,
        bool confirmed,
        decimal length) => new()
    {
        Id = id,
        ContainerId = isLegacy ? Guid.Empty : source.ContainerId,
        ContainerItemId = isLegacy ? null : source.ContainerItemId,
        FabricBatchId = isLegacy ? null : source.FabricBatchId,
        FabricItemId = source.FabricItemId,
        FabricColorId = source.FabricColorId,
        WarehouseId = source.WarehouseId,
        RollNumber = 900000 + Random.Shared.Next(1, 99999),
        LengthMeters = length,
        RemainingLengthMeters = length,
        CostPerMeter = source.CostPerMeter,
        Status = source.Status,
        QualityStatus = source.QualityStatus,
        ReservationStatus = source.ReservationStatus,
        IsLegacyOpeningBalance = isLegacy,
        LegacyLengthConfirmed = confirmed,
        CreatedAt = DateTime.UtcNow,
        IsActive = true
    };

    private static ServiceProvider BuildProvider()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = ConnectionString
            })
            .Build();
        var services = new ServiceCollection();
        services.AddSingleton<ICurrentUserService, LocalUserService>();
        services.AddSingleton<ICurrentBranchService, LocalBranchService>();
        services.AddInfrastructure(configuration);
        return services.BuildServiceProvider();
    }

    private sealed class LocalUserService : ICurrentUserService
    {
        public Guid? UserId => DatabaseSeeder.AdminUserId;
        public string? Username => "local-verification";
        public bool IsAuthenticated => true;
    }

    private sealed class LocalBranchService : ICurrentBranchService
    {
        public Guid? CompanyId => DatabaseSeeder.DefaultCompanyId;
        public Guid? BranchId => DatabaseSeeder.DefaultBranchId;
    }
}
