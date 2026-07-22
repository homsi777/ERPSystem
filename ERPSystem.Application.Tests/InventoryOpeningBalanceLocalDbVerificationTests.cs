using ERPSystem.Application.Abstractions;
using ERPSystem.Application.Abstractions.Repositories;
using ERPSystem.Application.Abstractions.Services;
using ERPSystem.Application.Common;
using ERPSystem.Application.Commands.Finance;
using ERPSystem.Domain.Entities.Finance;
using ERPSystem.Infrastructure.DependencyInjection;
using ERPSystem.Infrastructure.Persistence;
using ERPSystem.Infrastructure.Persistence.Models.Catalog;
using ERPSystem.Infrastructure.Persistence.Models.Documents;
using ERPSystem.Infrastructure.Persistence.Models.Finance;
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

    [Fact]
    public async Task Manually_typed_opening_stock_names_receive_catalog_ids_then_roll_back()
    {
        if (Environment.GetEnvironmentVariable("ERP_LOCAL_DB_TESTS") != "1")
            return;

        await using var provider = BuildProvider();
        using var scope = provider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ErpDbContext>();
        await using var tx = await db.Database.BeginTransactionAsync();

        var warehouse = await db.Warehouses.AsNoTracking()
            .FirstAsync(w => w.BranchId == DatabaseSeeder.DefaultBranchId);
        var suffix = Guid.NewGuid().ToString("N");
        var itemCode = $"OB-{suffix}";
        var itemName = $"قماش أول مدة {suffix}";
        var colorName = $"لون أول مدة {suffix}";
        var engine = scope.ServiceProvider.GetRequiredService<IOpeningBalanceEngine>();

        var result = await engine.CreateAsync(new CreateOpeningBalanceCommand
        {
            Type = OpeningBalanceType.OpeningStock,
            Source = OpeningBalanceSource.Manual,
            OpeningDate = DateTime.UtcNow,
            CurrencyCode = "USD",
            ExchangeRate = 1m,
            Reference = $"LOCAL-MANUAL-{suffix}",
            Lines =
            [
                new OpeningBalanceLineInput
                {
                    WarehouseId = warehouse.Id,
                    WarehouseName = warehouse.NameAr,
                    ItemCode = itemCode,
                    ItemName = itemName,
                    ColorName = colorName,
                    ContainerNumber = $"OB-{suffix}",
                    Quantity = 100m,
                    RollCount = 5,
                    UnitCost = 2m,
                    Debit = 200m
                }
            ]
        });

        Assert.True(result.IsSuccess, result.ErrorMessage);
        Assert.NotNull(result.Value);
        var documentId = result.Value!.Id;
        var storedLine = await db.OpeningBalanceLines.AsNoTracking()
            .SingleAsync(l => l.DocumentId == documentId);
        Assert.NotNull(storedLine.FabricItemId);
        Assert.NotEqual(Guid.Empty, storedLine.FabricItemId);
        Assert.NotNull(storedLine.FabricColorId);
        Assert.NotEqual(Guid.Empty, storedLine.FabricColorId);
        Assert.Equal(itemCode, storedLine.ItemCode);
        Assert.Equal(itemName, storedLine.ItemName);
        Assert.Equal(colorName, storedLine.ColorName);

        Assert.True(await db.FabricItems.AsNoTracking()
            .AnyAsync(i => i.Id == storedLine.FabricItemId && i.Code == itemCode && i.NameAr == itemName));
        Assert.True(await db.FabricColors.AsNoTracking()
            .AnyAsync(c => c.Id == storedLine.FabricColorId && c.NameAr == colorName));

        var submit = await engine.SubmitAsync(documentId);
        Assert.True(submit.IsSuccess, submit.ErrorMessage);
        var approve = await engine.ApproveAsync(documentId, "local manual-entry verification");
        Assert.True(approve.IsSuccess, approve.ErrorMessage);
        var post = await engine.PostAsync(documentId, lockAfterPost: true);
        Assert.True(post.IsSuccess, post.ErrorMessage);
        Assert.Equal(5, await db.FabricRolls.AsNoTracking().CountAsync(r =>
            r.FabricItemId == storedLine.FabricItemId &&
            r.FabricColorId == storedLine.FabricColorId &&
            r.IsLegacyOpeningBalance));

        await tx.RollbackAsync();

        Assert.False(await db.OpeningBalanceDocuments.AsNoTracking().AnyAsync(d => d.Id == documentId));
        Assert.False(await db.FabricItems.AsNoTracking().AnyAsync(i => i.NameAr == itemName));
        Assert.False(await db.FabricColors.AsNoTracking().AnyAsync(c => c.NameAr == colorName));
        Assert.False(await db.FabricRolls.AsNoTracking().AnyAsync(r =>
            r.FabricItemId == storedLine.FabricItemId && r.FabricColorId == storedLine.FabricColorId));
    }

    [Fact]
    public async Task Opening_balance_numbering_skips_existing_number_when_counter_is_behind()
    {
        if (Environment.GetEnvironmentVariable("ERP_LOCAL_DB_TESTS") != "1")
            return;

        await using var provider = BuildProvider();
        using var scope = provider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ErpDbContext>();
        await using var tx = await db.Database.BeginTransactionAsync();

        var branch = await db.Branches.AsNoTracking()
            .SingleAsync(b => b.Id == DatabaseSeeder.DefaultBranchId);
        var prefix = $"OBL-{branch.Code}-";
        var existingNumbers = await db.OpeningBalanceDocuments.AsNoTracking()
            .Where(d => d.CompanyId == DatabaseSeeder.DefaultCompanyId && d.Number.StartsWith(prefix))
            .Select(d => d.Number)
            .ToListAsync();
        var currentMaximum = existingNumbers
            .Select(number => long.TryParse(number.AsSpan(prefix.Length), out var value) ? value : 0L)
            .DefaultIfEmpty(0L)
            .Max();
        var forcedDuplicate = currentMaximum + 1;
        var duplicateNumber = $"{prefix}{forcedDuplicate:D6}";

        db.OpeningBalanceDocuments.Add(new OpeningBalanceDocumentEntity
        {
            Id = Guid.NewGuid(),
            CompanyId = DatabaseSeeder.DefaultCompanyId,
            BranchId = DatabaseSeeder.DefaultBranchId,
            Number = duplicateNumber,
            Type = (int)OpeningBalanceType.OpeningStock,
            Status = (int)OpeningBalanceStatus.Draft,
            Source = (int)OpeningBalanceSource.Manual,
            OpeningDate = DateTime.UtcNow,
            CurrencyCode = "USD",
            ExchangeRate = 1m,
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        });

        var counter = await db.DocumentCounters.SingleOrDefaultAsync(c =>
            c.BranchId == DatabaseSeeder.DefaultBranchId && c.DocumentType == "OpeningBalance");
        if (counter is null)
        {
            counter = new DocumentCounterEntity
            {
                Id = Guid.NewGuid(),
                BranchId = DatabaseSeeder.DefaultBranchId,
                DocumentType = "OpeningBalance",
                Prefix = "OBL",
                LastNumber = forcedDuplicate - 1,
                RowVersion = [0, 0, 0, 0, 0, 0, 0, 1]
            };
            db.DocumentCounters.Add(counter);
        }
        else
        {
            counter.Prefix = "OBL";
            counter.LastNumber = forcedDuplicate - 1;
        }
        await db.SaveChangesAsync();

        var warehouse = await db.Warehouses.AsNoTracking()
            .FirstAsync(w => w.BranchId == DatabaseSeeder.DefaultBranchId);
        var suffix = Guid.NewGuid().ToString("N");
        var engine = scope.ServiceProvider.GetRequiredService<IOpeningBalanceEngine>();
        var result = await engine.CreateAsync(new CreateOpeningBalanceCommand
        {
            Type = OpeningBalanceType.OpeningStock,
            Source = OpeningBalanceSource.Manual,
            CurrencyCode = "USD",
            ExchangeRate = 1m,
            Lines =
            [
                new OpeningBalanceLineInput
                {
                    WarehouseId = warehouse.Id,
                    WarehouseName = warehouse.NameAr,
                    ItemCode = $"CNT-{suffix}",
                    ItemName = $"قماش عداد {suffix}",
                    ColorName = $"لون عداد {suffix}",
                    ContainerNumber = $"CNT-OB-{suffix}",
                    Quantity = 10m,
                    RollCount = 1,
                    UnitCost = 1m,
                    Debit = 10m
                }
            ]
        });

        Assert.True(result.IsSuccess, result.ErrorMessage);
        Assert.Equal($"{prefix}{forcedDuplicate + 1:D6}", result.Value!.Number);

        await tx.RollbackAsync();
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
        services.AddLogging();
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
