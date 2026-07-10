using ERPSystem.Domain.Enums;
using ERPSystem.Infrastructure.Persistence;
using ERPSystem.Infrastructure.Persistence.Models.Accounting;
using ERPSystem.Infrastructure.Persistence.Models.Catalog;
using ERPSystem.Infrastructure.Persistence.Models.ChinaImport;
using ERPSystem.Infrastructure.Persistence.Models.Company;
using ERPSystem.Infrastructure.Persistence.Models.Documents;
using ERPSystem.Infrastructure.Persistence.Models.Inventory;
using ERPSystem.Infrastructure.Persistence.Models.Parties;
using ERPSystem.Infrastructure.Persistence.Models.Sales;
using ERPSystem.Infrastructure.Seed;
using Microsoft.EntityFrameworkCore;

namespace ERPSystem.Infrastructure.Seed;

/// <summary>Idempotent seeder for the isolated Phase 2 tax E2E test company.</summary>
public static class Phase2E2ETestCompanySeeder
{
    public static async Task<Phase2E2ESeedResult> SeedAsync(ErpDbContext context, CancellationToken cancellationToken = default)
    {
        GuardNotProduction();

        var existing = await context.Companies.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == Phase2E2ETestCompanyIds.CompanyId, cancellationToken);

        if (existing is not null && !Phase2E2ETestCompanyIds.IsTestCompanyName(existing.NameEn))
            throw new InvalidOperationException("Company ID collision — refusing to seed non-TEST company.");

        await EnsureCompanyAsync(context, cancellationToken);
        await EnsureAccountsAsync(context, cancellationToken);
        await EnsureTaxCodesAndProfileAsync(context, cancellationToken);
        await EnsureSupplierAsync(context, cancellationToken);
        await EnsureCustomerAsync(context, cancellationToken);
        await EnsureCatalogAndInventoryAsync(context, cancellationToken);
        await EnsureDocumentCountersAsync(context, cancellationToken);

        await context.SaveChangesAsync(cancellationToken);

        return new Phase2E2ESeedResult
        {
            CompanyId = Phase2E2ETestCompanyIds.CompanyId,
            BranchId = Phase2E2ETestCompanyIds.BranchId,
            WarehouseId = Phase2E2ETestCompanyIds.WarehouseId,
            CustomerId = Phase2E2ETestCompanyIds.CustomerId,
            ContainerId = Phase2E2ETestCompanyIds.ContainerId,
            FabricItemId = Phase2E2ETestCompanyIds.FabricItemId,
            FabricColorId = Phase2E2ETestCompanyIds.FabricColorId,
            AvailableRolls = await context.FabricRolls.CountAsync(
                r => r.ContainerId == Phase2E2ETestCompanyIds.ContainerId
                     && r.Status == (int)FabricRollStatus.Available, cancellationToken)
        };
    }

    public static void GuardNotProduction(Guid? companyId = null)
    {
        if (companyId is Guid id && id == DatabaseSeeder.DefaultCompanyId)
            throw new InvalidOperationException("Refusing E2E operations on production company.");
    }

    private static async Task EnsureCompanyAsync(ErpDbContext context, CancellationToken ct)
    {
        if (!await context.Companies.AnyAsync(c => c.Id == Phase2E2ETestCompanyIds.CompanyId, ct))
        {
            context.Companies.Add(new CompanyEntity
            {
                Id = Phase2E2ETestCompanyIds.CompanyId,
                Code = Phase2E2ETestCompanyIds.CompanyCode,
                NameAr = "شركة اختبار ضريبة E2E",
                NameEn = Phase2E2ETestCompanyIds.CompanyNameEn,
                DefaultCurrency = "USD"
            });
        }

        if (!await context.Branches.AnyAsync(b => b.Id == Phase2E2ETestCompanyIds.BranchId, ct))
        {
            context.Branches.Add(new BranchEntity
            {
                Id = Phase2E2ETestCompanyIds.BranchId,
                CompanyId = Phase2E2ETestCompanyIds.CompanyId,
                Code = "E2E-MAIN",
                NameAr = "فرع اختبار E2E",
                NameEn = "E2E Test Branch"
            });
        }

        if (!await context.Warehouses.AnyAsync(w => w.Id == Phase2E2ETestCompanyIds.WarehouseId, ct))
        {
            context.Warehouses.Add(new WarehouseEntity
            {
                Id = Phase2E2ETestCompanyIds.WarehouseId,
                BranchId = Phase2E2ETestCompanyIds.BranchId,
                Code = "E2E-WH",
                NameAr = "مستودع اختبار E2E",
                NameEn = "E2E Test Warehouse",
                City = "Test",
                IsDefault = true
            });
        }
    }

    private static async Task EnsureAccountsAsync(ErpDbContext context, CancellationToken ct)
    {
        var companyId = Phase2E2ETestCompanyIds.CompanyId;
        var accounts = new (Guid Id, string Code, string NameAr, string NameEn, string Type, Guid? Parent, bool Postable)[]
        {
            (Phase2E2ETestCompanyIds.RootAssets, "E2E-1000", "أصول اختبار", "E2E Assets", "Asset", null, false),
            (Phase2E2ETestCompanyIds.RootLiabilities, "E2E-2000", "خصوم اختبار", "E2E Liabilities", "Liability", null, false),
            (Phase2E2ETestCompanyIds.RootRevenue, "E2E-4000", "إيرادات اختبار", "E2E Revenue", "Revenue", null, false),
            (Phase2E2ETestCompanyIds.RootExpense, "E2E-5000", "مصروفات اختبار", "E2E Expenses", "Expense", null, false),
            (Phase2E2ETestCompanyIds.AccountsReceivable, "E2E-1100", "ذمم عملاء اختبار", "E2E AR", "Asset", Phase2E2ETestCompanyIds.RootAssets, true),
            (Phase2E2ETestCompanyIds.SalesRevenue, "E2E-4100", "إيراد مبيعات اختبار", "E2E Sales Revenue", "Revenue", Phase2E2ETestCompanyIds.RootRevenue, true),
            (Phase2E2ETestCompanyIds.SalesDiscounts, "E2E-4200", "خصم مبيعات اختبار", "E2E Sales Discounts", "Revenue", Phase2E2ETestCompanyIds.RootRevenue, true),
            (Phase2E2ETestCompanyIds.VatPayable, "E2E-2200", "ضريبة مستحقة اختبار", "E2E VAT Payable", "Liability", Phase2E2ETestCompanyIds.RootLiabilities, true),
            (Phase2E2ETestCompanyIds.InventoryAsset, "E2E-1200", "مخزون اختبار", "E2E Inventory", "Asset", Phase2E2ETestCompanyIds.RootAssets, true),
            (Phase2E2ETestCompanyIds.CostOfGoodsSold, "E2E-5100", "تكلفة مبيعات اختبار", "E2E COGS", "Expense", Phase2E2ETestCompanyIds.RootExpense, true),
            (Phase2E2ETestCompanyIds.RoundingDifference, "E2E-5290", "تقريب اختبار", "E2E Rounding", "Expense", Phase2E2ETestCompanyIds.RootExpense, true),
            (Phase2E2ETestCompanyIds.CashTest, "E2E-1010", "صندوق اختبار", "E2E Cash Test", "Asset", Phase2E2ETestCompanyIds.RootAssets, true),
            (Phase2E2ETestCompanyIds.AccountsPayable, "E2E-2100", "ذمم موردين اختبار", "E2E AP", "Liability", Phase2E2ETestCompanyIds.RootLiabilities, true),
        };

        foreach (var (id, code, nameAr, nameEn, type, parent, postable) in accounts)
        {
            if (await context.Accounts.AnyAsync(a => a.Id == id, ct))
                continue;
            context.Accounts.Add(new AccountEntity
            {
                Id = id,
                CompanyId = companyId,
                Code = code,
                NameAr = nameAr,
                NameEn = nameEn,
                AccountType = type,
                ParentId = parent,
                IsPostable = postable
            });
        }
    }

    private static async Task EnsureTaxCodesAndProfileAsync(ErpDbContext context, CancellationToken ct)
    {
        var companyId = Phase2E2ETestCompanyIds.CompanyId;
        var vatAccount = Phase2E2ETestCompanyIds.VatPayable;
        var effectiveFrom = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        var taxCodes = new (Guid Id, string Code, string Name, decimal Rate, TaxPriceMode Mode, TaxCategory Cat, Guid? Account)[]
        {
            (Phase2E2ETestCompanyIds.Vat15Exclusive, "E2E-VAT15-EX", "E2E VAT 15% Exclusive", 0.15m, TaxPriceMode.Exclusive, TaxCategory.Standard, vatAccount),
            (Phase2E2ETestCompanyIds.Vat15Inclusive, "E2E-VAT15-IN", "E2E VAT 15% Inclusive", 0.15m, TaxPriceMode.Inclusive, TaxCategory.Standard, vatAccount),
            (Phase2E2ETestCompanyIds.ZeroRated, "E2E-ZERO", "E2E Zero Rated", 0m, TaxPriceMode.Exclusive, TaxCategory.ZeroRated, vatAccount),
            (Phase2E2ETestCompanyIds.Exempt, "E2E-EXEMPT", "E2E Exempt", 0m, TaxPriceMode.Exclusive, TaxCategory.Exempt, null),
        };

        foreach (var (id, code, name, rate, mode, cat, account) in taxCodes)
        {
            if (await context.TaxCodes.AnyAsync(t => t.Id == id, ct))
                continue;
            context.TaxCodes.Add(new TaxCodeEntity
            {
                Id = id,
                CompanyId = companyId,
                Code = code,
                Name = name,
                Rate = rate,
                PriceMode = (int)mode,
                Category = (int)cat,
                SalesTaxAccountId = account,
                EffectiveFrom = effectiveFrom,
                IsActive = true
            });
        }

        if (!await context.SalesPostingProfiles.AnyAsync(p => p.Id == Phase2E2ETestCompanyIds.PostingProfileId, ct))
        {
            context.SalesPostingProfiles.Add(new SalesPostingProfileEntity
            {
                Id = Phase2E2ETestCompanyIds.PostingProfileId,
                CompanyId = companyId,
                AccountsReceivableAccountId = Phase2E2ETestCompanyIds.AccountsReceivable,
                SalesRevenueAccountId = Phase2E2ETestCompanyIds.SalesRevenue,
                SalesDiscountAccountId = Phase2E2ETestCompanyIds.SalesDiscounts,
                VatPayableAccountId = Phase2E2ETestCompanyIds.VatPayable,
                InventoryAccountId = Phase2E2ETestCompanyIds.InventoryAsset,
                CogsAccountId = Phase2E2ETestCompanyIds.CostOfGoodsSold,
                RoundingAccountId = Phase2E2ETestCompanyIds.RoundingDifference
            });
        }
    }

    private static async Task EnsureSupplierAsync(ErpDbContext context, CancellationToken ct)
    {
        if (!await context.Suppliers.AnyAsync(s => s.Id == Phase2E2ETestCompanyIds.SupplierId, ct))
        {
            context.Suppliers.Add(new SupplierEntity
            {
                Id = Phase2E2ETestCompanyIds.SupplierId,
                CompanyId = Phase2E2ETestCompanyIds.CompanyId,
                Code = "E2E-SUP",
                Name = "E2E Test Supplier",
                NameAr = "مورد اختبار",
                NameEn = "E2E Test Supplier",
                Status = (int)SupplierStatus.Active,
                PayablesAccountId = Phase2E2ETestCompanyIds.AccountsPayable
            });
        }
    }

    private static async Task EnsureCustomerAsync(ErpDbContext context, CancellationToken ct)
    {
        if (!await context.Customers.AnyAsync(c => c.Id == Phase2E2ETestCompanyIds.CustomerId, ct))
        {
            context.Customers.Add(new CustomerEntity
            {
                Id = Phase2E2ETestCompanyIds.CustomerId,
                CompanyId = Phase2E2ETestCompanyIds.CompanyId,
                Code = "E2E-CUST",
                NameAr = "عميل اختبار ضريبة",
                NameEn = "E2E Tax Test Customer",
                Type = (int)CustomerType.Credit,
                Status = (int)CustomerStatus.Active,
                CreditLimit = 10_000_000m,
                CreditLimitEnabled = true,
                CreditLimitCurrency = "USD",
                Balance = 0m,
                BalanceCurrency = "USD"
            });
        }
    }

    private static async Task EnsureCatalogAndInventoryAsync(ErpDbContext context, CancellationToken ct)
    {
        if (!await context.FabricCategories.AnyAsync(c => c.Id == Phase2E2ETestCompanyIds.FabricCategoryId, ct))
        {
            context.FabricCategories.Add(new FabricCategoryEntity
            {
                Id = Phase2E2ETestCompanyIds.FabricCategoryId,
                CompanyId = Phase2E2ETestCompanyIds.CompanyId,
                Code = "E2E-CAT",
                NameAr = "قماش اختبار",
                NameEn = "E2E Test Fabric"
            });
        }

        if (!await context.FabricItems.AnyAsync(f => f.Id == Phase2E2ETestCompanyIds.FabricItemId, ct))
        {
            context.FabricItems.Add(new FabricItemEntity
            {
                Id = Phase2E2ETestCompanyIds.FabricItemId,
                CompanyId = Phase2E2ETestCompanyIds.CompanyId,
                CategoryId = Phase2E2ETestCompanyIds.FabricCategoryId,
                Code = "E2E-FAB",
                NameAr = "ثوب اختبار E2E",
                NameEn = "E2E Test Fabric Roll"
            });
        }

        if (!await context.FabricColors.AnyAsync(c => c.Id == Phase2E2ETestCompanyIds.FabricColorId, ct))
        {
            context.FabricColors.Add(new FabricColorEntity
            {
                Id = Phase2E2ETestCompanyIds.FabricColorId,
                FabricItemId = Phase2E2ETestCompanyIds.FabricItemId,
                Code = "E2E-BLU",
                NameAr = "أزرق اختبار",
                NameEn = "E2E Test Blue"
            });
        }

        if (!await context.Containers.AnyAsync(c => c.Id == Phase2E2ETestCompanyIds.ContainerId, ct))
        {
            var totalMeters = Phase2E2ETestCompanyIds.SeedRollCount * Phase2E2ETestCompanyIds.DefaultRollMeters;
            context.Containers.Add(new ContainerEntity
            {
                Id = Phase2E2ETestCompanyIds.ContainerId,
                CompanyId = Phase2E2ETestCompanyIds.CompanyId,
                BranchId = Phase2E2ETestCompanyIds.BranchId,
                SupplierId = Phase2E2ETestCompanyIds.SupplierId,
                ContainerNumber = "E2E-TAX-CNT-001",
                Status = (int)ChinaContainerStatus.InWarehouse,
                ShipmentDate = DateTime.UtcNow.AddMonths(-1),
                TotalRolls = Phase2E2ETestCompanyIds.SeedRollCount,
                TotalMeters = totalMeters,
                ExchangeRateToLocalCurrency = 1m,
                ApprovedAt = DateTime.UtcNow.AddMonths(-1)
            });
        }

        if (!await context.ContainerItems.AnyAsync(i => i.Id == Phase2E2ETestCompanyIds.ContainerItemId, ct))
        {
            context.ContainerItems.Add(new ContainerItemEntity
            {
                Id = Phase2E2ETestCompanyIds.ContainerItemId,
                ContainerId = Phase2E2ETestCompanyIds.ContainerId,
                LineNumber = 1,
                FabricItemId = Phase2E2ETestCompanyIds.FabricItemId,
                FabricColorId = Phase2E2ETestCompanyIds.FabricColorId,
                RollCount = Phase2E2ETestCompanyIds.SeedRollCount,
                LengthMeters = Phase2E2ETestCompanyIds.SeedRollCount * Phase2E2ETestCompanyIds.DefaultRollMeters
            });
        }

        if (!await context.WarehouseStocks.AnyAsync(
                s => s.ContainerId == Phase2E2ETestCompanyIds.ContainerId, ct))
        {
            var totalMeters = Phase2E2ETestCompanyIds.SeedRollCount * Phase2E2ETestCompanyIds.DefaultRollMeters;
            context.WarehouseStocks.Add(new WarehouseStockEntity
            {
                Id = Guid.NewGuid(),
                WarehouseId = Phase2E2ETestCompanyIds.WarehouseId,
                FabricItemId = Phase2E2ETestCompanyIds.FabricItemId,
                FabricColorId = Phase2E2ETestCompanyIds.FabricColorId,
                ContainerId = Phase2E2ETestCompanyIds.ContainerId,
                RollCount = Phase2E2ETestCompanyIds.SeedRollCount,
                TotalMeters = totalMeters,
                ReservedMeters = 0m,
                AvailableMeters = totalMeters
            });
        }

        var existingRolls = await context.FabricRolls.CountAsync(
            r => r.ContainerId == Phase2E2ETestCompanyIds.ContainerId, ct);
        for (var i = existingRolls + 1; i <= Phase2E2ETestCompanyIds.SeedRollCount; i++)
        {
            context.FabricRolls.Add(new FabricRollEntity
            {
                Id = Guid.NewGuid(),
                ContainerId = Phase2E2ETestCompanyIds.ContainerId,
                ContainerItemId = Phase2E2ETestCompanyIds.ContainerItemId,
                FabricItemId = Phase2E2ETestCompanyIds.FabricItemId,
                FabricColorId = Phase2E2ETestCompanyIds.FabricColorId,
                WarehouseId = Phase2E2ETestCompanyIds.WarehouseId,
                RollNumber = i,
                LengthMeters = Phase2E2ETestCompanyIds.DefaultRollMeters,
                RemainingLengthMeters = Phase2E2ETestCompanyIds.DefaultRollMeters,
                CostPerMeter = Phase2E2ETestCompanyIds.CostPerMeter,
                Status = (int)FabricRollStatus.Available,
                ReservationStatus = 0
            });
        }

        var stock = await context.WarehouseStocks.FirstAsync(
            s => s.ContainerId == Phase2E2ETestCompanyIds.ContainerId, ct);
        if (stock.AvailableMeters < Phase2E2ETestCompanyIds.MinimumAvailableMetersPerRun)
        {
            var missingMeters =
                Phase2E2ETestCompanyIds.MinimumAvailableMetersPerRun - stock.AvailableMeters;
            var topUpRolls = (int)Math.Ceiling(
                missingMeters / Phase2E2ETestCompanyIds.DefaultRollMeters);
            var maxRollNumber = await context.FabricRolls
                .Where(r => r.ContainerId == Phase2E2ETestCompanyIds.ContainerId)
                .MaxAsync(r => (int?)r.RollNumber, ct) ?? 0;

            for (var i = 1; i <= topUpRolls; i++)
            {
                context.FabricRolls.Add(new FabricRollEntity
                {
                    Id = Guid.NewGuid(),
                    ContainerId = Phase2E2ETestCompanyIds.ContainerId,
                    ContainerItemId = Phase2E2ETestCompanyIds.ContainerItemId,
                    FabricItemId = Phase2E2ETestCompanyIds.FabricItemId,
                    FabricColorId = Phase2E2ETestCompanyIds.FabricColorId,
                    WarehouseId = Phase2E2ETestCompanyIds.WarehouseId,
                    RollNumber = maxRollNumber + i,
                    LengthMeters = Phase2E2ETestCompanyIds.DefaultRollMeters,
                    RemainingLengthMeters = Phase2E2ETestCompanyIds.DefaultRollMeters,
                    CostPerMeter = Phase2E2ETestCompanyIds.CostPerMeter,
                    Status = (int)FabricRollStatus.Available,
                    ReservationStatus = 0
                });
            }

            var addedMeters = topUpRolls * Phase2E2ETestCompanyIds.DefaultRollMeters;
            stock.RollCount += topUpRolls;
            stock.TotalMeters += addedMeters;
            stock.AvailableMeters += addedMeters;

            var container = await context.Containers.FirstAsync(
                c => c.Id == Phase2E2ETestCompanyIds.ContainerId, ct);
            container.TotalRolls += topUpRolls;
            container.TotalMeters += addedMeters;

            var containerItem = await context.ContainerItems.FirstAsync(
                i => i.Id == Phase2E2ETestCompanyIds.ContainerItemId, ct);
            containerItem.RollCount += topUpRolls;
            containerItem.LengthMeters += addedMeters;
        }
    }

    private static async Task EnsureDocumentCountersAsync(ErpDbContext context, CancellationToken ct)
    {
        var types = new[] { "SalesInvoice", "SalesReturn", "JournalEntry" };
        foreach (var docType in types)
        {
            if (await context.DocumentCounters.AnyAsync(
                    d => d.BranchId == Phase2E2ETestCompanyIds.BranchId && d.DocumentType == docType, ct))
                continue;

            context.DocumentCounters.Add(new DocumentCounterEntity
            {
                Id = Guid.NewGuid(),
                BranchId = Phase2E2ETestCompanyIds.BranchId,
                DocumentType = docType,
                Prefix = docType switch
                {
                    "SalesInvoice" => "E2E-INV",
                    "SalesReturn" => "E2E-RET",
                    _ => "E2E-JE"
                },
                LastNumber = 0
            });
        }
    }
}

public sealed class Phase2E2ESeedResult
{
    public Guid CompanyId { get; init; }
    public Guid BranchId { get; init; }
    public Guid WarehouseId { get; init; }
    public Guid CustomerId { get; init; }
    public Guid ContainerId { get; init; }
    public Guid FabricItemId { get; init; }
    public Guid FabricColorId { get; init; }
    public int AvailableRolls { get; init; }
}
