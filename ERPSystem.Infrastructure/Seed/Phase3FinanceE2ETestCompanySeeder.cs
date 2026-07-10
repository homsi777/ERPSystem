using ERPSystem.Application.Common;
using ERPSystem.Domain.Enums;
using ERPSystem.Infrastructure.E2E;
using ERPSystem.Infrastructure.Persistence;
using ERPSystem.Infrastructure.Persistence.Models.Accounting;
using ERPSystem.Infrastructure.Persistence.Models.Catalog;
using ERPSystem.Infrastructure.Persistence.Models.ChinaImport;
using ERPSystem.Infrastructure.Persistence.Models.Company;
using ERPSystem.Infrastructure.Persistence.Models.Documents;
using ERPSystem.Infrastructure.Persistence.Models.Finance;
using ERPSystem.Infrastructure.Persistence.Models.Inventory;
using ERPSystem.Infrastructure.Persistence.Models.Parties;
using ERPSystem.Infrastructure.Persistence.Models.Sales;
using Microsoft.EntityFrameworkCore;

namespace ERPSystem.Infrastructure.Seed;

/// <summary>Idempotent seeder for the isolated Phase 3 finance E2E test company.</summary>
public static class Phase3FinanceE2ETestCompanySeeder
{
    public static async Task<Phase3FinanceE2ESeedResult> SeedAsync(
        ErpDbContext context,
        CancellationToken cancellationToken = default)
    {
        await E2EProductionGuard.GuardWritableE2EAsync(context, Phase3FinanceE2ETestCompanyIds.CompanyId, cancellationToken);

        var existing = await context.Companies.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == Phase3FinanceE2ETestCompanyIds.CompanyId, cancellationToken);
        if (existing is not null && !Phase3FinanceE2ETestCompanyIds.IsTestCompanyName(existing.NameEn))
            throw new InvalidOperationException("Company ID collision — refusing to seed non-TEST company.");

        await EnsureCompanyAsync(context, cancellationToken);
        await EnsureAccountsAsync(context, cancellationToken);
        await EnsureTaxCodesAndProfileAsync(context, cancellationToken);
        await EnsurePaymentMethodsAsync(context, cancellationToken);
        await EnsureCashboxesAsync(context, cancellationToken);
        await EnsureBankAccountAsync(context, cancellationToken);
        await EnsurePartiesAsync(context, cancellationToken);
        await EnsureCatalogAndInventoryAsync(context, cancellationToken);
        await EnsureCreditInvoiceAsync(context, cancellationToken);
        await EnsureDocumentCountersAsync(context, cancellationToken);

        await context.SaveChangesAsync(cancellationToken);

        return new Phase3FinanceE2ESeedResult
        {
            CompanyId = Phase3FinanceE2ETestCompanyIds.CompanyId,
            BranchId = Phase3FinanceE2ETestCompanyIds.BranchId,
            CustomerId = Phase3FinanceE2ETestCompanyIds.CustomerId,
            CashboxAId = Phase3FinanceE2ETestCompanyIds.CashboxA,
            CashboxBId = Phase3FinanceE2ETestCompanyIds.CashboxB,
            BankAccountId = Phase3FinanceE2ETestCompanyIds.BankAccount,
            CreditInvoiceId = Phase3FinanceE2ETestCompanyIds.CreditInvoiceId
        };
    }

    private static async Task EnsureCompanyAsync(ErpDbContext context, CancellationToken ct)
    {
        if (!await context.Companies.AnyAsync(c => c.Id == Phase3FinanceE2ETestCompanyIds.CompanyId, ct))
        {
            context.Companies.Add(new CompanyEntity
            {
                Id = Phase3FinanceE2ETestCompanyIds.CompanyId,
                Code = Phase3FinanceE2ETestCompanyIds.CompanyCode,
                NameAr = "شركة اختبار مالية E2E",
                NameEn = Phase3FinanceE2ETestCompanyIds.CompanyNameEn,
                DefaultCurrency = "USD"
            });
        }

        if (!await context.Branches.AnyAsync(b => b.Id == Phase3FinanceE2ETestCompanyIds.BranchId, ct))
        {
            context.Branches.Add(new BranchEntity
            {
                Id = Phase3FinanceE2ETestCompanyIds.BranchId,
                CompanyId = Phase3FinanceE2ETestCompanyIds.CompanyId,
                Code = "E2E-FIN",
                NameAr = "فرع اختبار مالية",
                NameEn = "E2E Finance Branch"
            });
        }

        if (!await context.Warehouses.AnyAsync(w => w.Id == Phase3FinanceE2ETestCompanyIds.WarehouseId, ct))
        {
            context.Warehouses.Add(new WarehouseEntity
            {
                Id = Phase3FinanceE2ETestCompanyIds.WarehouseId,
                BranchId = Phase3FinanceE2ETestCompanyIds.BranchId,
                Code = "E2E-FIN-WH",
                NameAr = "مستودع اختبار مالية",
                NameEn = "E2E Finance Warehouse",
                City = "Test",
                IsDefault = true
            });
        }
    }

    private static async Task EnsureAccountsAsync(ErpDbContext context, CancellationToken ct)
    {
        var companyId = Phase3FinanceE2ETestCompanyIds.CompanyId;
        var accounts = new (Guid Id, string Code, string NameAr, string NameEn, string Type, Guid? Parent, bool Postable)[]
        {
            (Phase3FinanceE2ETestCompanyIds.RootAssets, "E3F-1000", "أصول اختبار", "E2E Assets", "Asset", null, false),
            (Phase3FinanceE2ETestCompanyIds.RootLiabilities, "E3F-2000", "خصوم اختبار", "E2E Liabilities", "Liability", null, false),
            (Phase3FinanceE2ETestCompanyIds.RootRevenue, "E3F-4000", "إيرادات اختبار", "E2E Revenue", "Revenue", null, false),
            (Phase3FinanceE2ETestCompanyIds.RootExpense, "E3F-5000", "مصروفات اختبار", "E2E Expenses", "Expense", null, false),
            (Phase3FinanceE2ETestCompanyIds.AccountsReceivable, "E3F-1100", "ذمم عملاء اختبار", "Test Accounts Receivable", "Asset", Phase3FinanceE2ETestCompanyIds.RootAssets, true),
            (Phase3FinanceE2ETestCompanyIds.CashAccountA, "E3F-1010", "صندوق A", "Test Cash Account A", "Asset", Phase3FinanceE2ETestCompanyIds.RootAssets, true),
            (Phase3FinanceE2ETestCompanyIds.CashAccountB, "E3F-1011", "صندوق B", "Test Cash Account B", "Asset", Phase3FinanceE2ETestCompanyIds.RootAssets, true),
            (Phase3FinanceE2ETestCompanyIds.BankGlAccount, "E3F-1020", "بنك اختبار", "Test Bank Account", "Asset", Phase3FinanceE2ETestCompanyIds.RootAssets, true),
            (Phase3FinanceE2ETestCompanyIds.CustomerAdvances, "E3F-2150", "دفعات مقدمة", "Test Customer Advances", "Liability", Phase3FinanceE2ETestCompanyIds.RootLiabilities, true),
            (Phase3FinanceE2ETestCompanyIds.SalesRevenue, "E3F-4100", "إيراد مبيعات", "Test Revenue", "Revenue", Phase3FinanceE2ETestCompanyIds.RootRevenue, true),
            (Phase3FinanceE2ETestCompanyIds.VatPayable, "E3F-2200", "ضريبة", "Test VAT", "Liability", Phase3FinanceE2ETestCompanyIds.RootLiabilities, true),
            (Phase3FinanceE2ETestCompanyIds.InventoryAsset, "E3F-1200", "مخزون", "Test Inventory", "Asset", Phase3FinanceE2ETestCompanyIds.RootAssets, true),
            (Phase3FinanceE2ETestCompanyIds.CostOfGoodsSold, "E3F-5100", "تكلفة مبيعات", "Test COGS", "Expense", Phase3FinanceE2ETestCompanyIds.RootExpense, true),
            (Phase3FinanceE2ETestCompanyIds.RoundingDifference, "E3F-5290", "تقريب", "E2E Rounding", "Expense", Phase3FinanceE2ETestCompanyIds.RootExpense, true),
        };

        foreach (var (id, code, nameAr, nameEn, type, parent, postable) in accounts)
        {
            if (await context.Accounts.AnyAsync(a => a.Id == id, ct)) continue;
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
        var companyId = Phase3FinanceE2ETestCompanyIds.CompanyId;
        if (!await context.TaxCodes.AnyAsync(t => t.Id == Phase3FinanceE2ETestCompanyIds.Vat15Exclusive, ct))
        {
            context.TaxCodes.Add(new TaxCodeEntity
            {
                Id = Phase3FinanceE2ETestCompanyIds.Vat15Exclusive,
                CompanyId = companyId,
                Code = "E3F-VAT15",
                Name = "E2E VAT 15%",
                Rate = 0.15m,
                PriceMode = (int)TaxPriceMode.Exclusive,
                Category = (int)TaxCategory.Standard,
                SalesTaxAccountId = Phase3FinanceE2ETestCompanyIds.VatPayable,
                EffectiveFrom = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                IsActive = true
            });
        }

        if (!await context.SalesPostingProfiles.AnyAsync(p => p.Id == Phase3FinanceE2ETestCompanyIds.PostingProfileId, ct))
        {
            context.SalesPostingProfiles.Add(new SalesPostingProfileEntity
            {
                Id = Phase3FinanceE2ETestCompanyIds.PostingProfileId,
                CompanyId = companyId,
                AccountsReceivableAccountId = Phase3FinanceE2ETestCompanyIds.AccountsReceivable,
                SalesRevenueAccountId = Phase3FinanceE2ETestCompanyIds.SalesRevenue,
                SalesDiscountAccountId = Phase3FinanceE2ETestCompanyIds.SalesRevenue,
                VatPayableAccountId = Phase3FinanceE2ETestCompanyIds.VatPayable,
                InventoryAccountId = Phase3FinanceE2ETestCompanyIds.InventoryAsset,
                CogsAccountId = Phase3FinanceE2ETestCompanyIds.CostOfGoodsSold,
                RoundingAccountId = Phase3FinanceE2ETestCompanyIds.RoundingDifference
            });
        }
    }

    private static async Task EnsurePaymentMethodsAsync(ErpDbContext context, CancellationToken ct)
    {
        var companyId = Phase3FinanceE2ETestCompanyIds.CompanyId;
        var methods = new (Guid Id, int Kind, string Code, string Name, bool Cashbox, bool Bank, bool Ref)[]
        {
            (PaymentMethodIds.Cash, 0, "CASH", "نقدي", true, false, false),
            (PaymentMethodIds.BankTransfer, 1, "BANK", "تحويل بنكي", false, true, true),
        };

        foreach (var (id, kind, code, name, cashbox, bank, reference) in methods)
        {
            if (await context.PaymentMethods.AnyAsync(m => m.Id == id && m.CompanyId == companyId, ct))
                continue;
            context.PaymentMethods.Add(new PaymentMethodEntity
            {
                Id = id,
                CompanyId = companyId,
                Kind = kind,
                Code = code,
                Name = name,
                RequiresCashbox = cashbox,
                RequiresBankAccount = bank,
                RequiresReference = reference
            });
        }
    }

    private static async Task EnsureCashboxesAsync(ErpDbContext context, CancellationToken ct)
    {
        var branchId = Phase3FinanceE2ETestCompanyIds.BranchId;
        var companyId = Phase3FinanceE2ETestCompanyIds.CompanyId;
        var boxes = new (Guid Id, string Code, string Name, Guid? AccountId, bool Active)[]
        {
            (Phase3FinanceE2ETestCompanyIds.CashboxA, "E2F-CBX-A", "TEST CASHBOX A", Phase3FinanceE2ETestCompanyIds.CashAccountA, true),
            (Phase3FinanceE2ETestCompanyIds.CashboxB, "E2F-CBX-B", "TEST CASHBOX B", Phase3FinanceE2ETestCompanyIds.CashAccountB, true),
            (Phase3FinanceE2ETestCompanyIds.CashboxNoAccount, "E2F-CBX-NA", "TEST CASHBOX NO ACCOUNT", null, true),
            (Phase3FinanceE2ETestCompanyIds.CashboxInactive, "E2F-CBX-IN", "TEST CASHBOX INACTIVE", Phase3FinanceE2ETestCompanyIds.CashAccountA, false),
        };

        foreach (var (id, code, name, accountId, active) in boxes)
        {
            if (await context.Cashboxes.AnyAsync(c => c.Id == id, ct)) continue;
            context.Cashboxes.Add(new CashboxEntity
            {
                Id = id,
                CompanyId = companyId,
                BranchId = branchId,
                Code = code,
                Name = name,
                Currency = "USD",
                Balance = 0m,
                AccountId = accountId,
                IsActive = active,
                OpeningDate = DateTime.UtcNow.Date
            });
        }
    }

    private static async Task EnsureBankAccountAsync(ErpDbContext context, CancellationToken ct)
    {
        if (await context.BankAccounts.AnyAsync(b => b.Id == Phase3FinanceE2ETestCompanyIds.BankAccount, ct))
            return;

        context.BankAccounts.Add(new BankAccountEntity
        {
            Id = Phase3FinanceE2ETestCompanyIds.BankAccount,
            CompanyId = Phase3FinanceE2ETestCompanyIds.CompanyId,
            BranchId = Phase3FinanceE2ETestCompanyIds.BranchId,
            Code = "E2F-BANK",
            Name = "TEST BANK ACCOUNT",
            BankName = "E2E Test Bank",
            GlAccountId = Phase3FinanceE2ETestCompanyIds.BankGlAccount,
            Currency = "USD"
        });
    }

    private static async Task EnsurePartiesAsync(ErpDbContext context, CancellationToken ct)
    {
        if (!await context.Customers.AnyAsync(c => c.Id == Phase3FinanceE2ETestCompanyIds.CustomerId, ct))
        {
            context.Customers.Add(new CustomerEntity
            {
                Id = Phase3FinanceE2ETestCompanyIds.CustomerId,
                CompanyId = Phase3FinanceE2ETestCompanyIds.CompanyId,
                Code = "E2F-CR",
                NameAr = "عميل اختبار ائتمان",
                NameEn = "Test credit customer",
                Type = (int)CustomerType.Credit,
                Status = (int)CustomerStatus.Active,
                CreditLimit = 1_000_000m,
                CreditLimitEnabled = true,
                CreditLimitCurrency = "USD",
                Balance = Phase3FinanceE2ETestCompanyIds.CreditInvoiceTotal,
                BalanceCurrency = "USD"
            });
        }

        if (!await context.Customers.AnyAsync(c => c.Id == Phase3FinanceE2ETestCompanyIds.CashCustomerId, ct))
        {
            context.Customers.Add(new CustomerEntity
            {
                Id = Phase3FinanceE2ETestCompanyIds.CashCustomerId,
                CompanyId = Phase3FinanceE2ETestCompanyIds.CompanyId,
                Code = "E2F-CA",
                NameAr = "عميل اختبار نقدي",
                NameEn = "Test cash customer",
                Type = (int)CustomerType.Cash,
                Status = (int)CustomerStatus.Active
            });
        }

        if (!await context.Suppliers.AnyAsync(s => s.Id == Phase3FinanceE2ETestCompanyIds.SupplierId, ct))
        {
            context.Suppliers.Add(new SupplierEntity
            {
                Id = Phase3FinanceE2ETestCompanyIds.SupplierId,
                CompanyId = Phase3FinanceE2ETestCompanyIds.CompanyId,
                Code = "E2F-SUP",
                Name = "E2E Finance Supplier",
                NameAr = "مورد اختبار",
                NameEn = "E2E Finance Supplier",
                Status = (int)SupplierStatus.Active
            });
        }
    }

    private static async Task EnsureCatalogAndInventoryAsync(ErpDbContext context, CancellationToken ct)
    {
        if (!await context.FabricCategories.AnyAsync(c => c.Id == Phase3FinanceE2ETestCompanyIds.FabricCategoryId, ct))
        {
            context.FabricCategories.Add(new FabricCategoryEntity
            {
                Id = Phase3FinanceE2ETestCompanyIds.FabricCategoryId,
                CompanyId = Phase3FinanceE2ETestCompanyIds.CompanyId,
                Code = "E3F-CAT",
                NameAr = "قماش اختبار",
                NameEn = "E2E Test Fabric"
            });
        }

        if (!await context.FabricItems.AnyAsync(f => f.Id == Phase3FinanceE2ETestCompanyIds.FabricItemId, ct))
        {
            context.FabricItems.Add(new FabricItemEntity
            {
                Id = Phase3FinanceE2ETestCompanyIds.FabricItemId,
                CompanyId = Phase3FinanceE2ETestCompanyIds.CompanyId,
                CategoryId = Phase3FinanceE2ETestCompanyIds.FabricCategoryId,
                Code = "E3F-FAB",
                NameAr = "ثوب اختبار",
                NameEn = "Test fabric roll"
            });
        }

        if (!await context.FabricColors.AnyAsync(c => c.Id == Phase3FinanceE2ETestCompanyIds.FabricColorId, ct))
        {
            context.FabricColors.Add(new FabricColorEntity
            {
                Id = Phase3FinanceE2ETestCompanyIds.FabricColorId,
                FabricItemId = Phase3FinanceE2ETestCompanyIds.FabricItemId,
                Code = "E3F-BLU",
                NameAr = "أزرق",
                NameEn = "Test Blue"
            });
        }

        if (!await context.Containers.AnyAsync(c => c.Id == Phase3FinanceE2ETestCompanyIds.ContainerId, ct))
        {
            var totalMeters = Phase3FinanceE2ETestCompanyIds.SeedRollCount * Phase3FinanceE2ETestCompanyIds.DefaultRollMeters;
            context.Containers.Add(new ContainerEntity
            {
                Id = Phase3FinanceE2ETestCompanyIds.ContainerId,
                CompanyId = Phase3FinanceE2ETestCompanyIds.CompanyId,
                BranchId = Phase3FinanceE2ETestCompanyIds.BranchId,
                SupplierId = Phase3FinanceE2ETestCompanyIds.SupplierId,
                ContainerNumber = "E3F-CNT-001",
                Status = (int)ChinaContainerStatus.InWarehouse,
                ShipmentDate = DateTime.UtcNow.AddMonths(-1),
                TotalRolls = Phase3FinanceE2ETestCompanyIds.SeedRollCount,
                TotalMeters = totalMeters,
                ExchangeRateToLocalCurrency = 1m,
                ApprovedAt = DateTime.UtcNow.AddMonths(-1)
            });
        }

        if (!await context.ContainerItems.AnyAsync(i => i.Id == Phase3FinanceE2ETestCompanyIds.ContainerItemId, ct))
        {
            context.ContainerItems.Add(new ContainerItemEntity
            {
                Id = Phase3FinanceE2ETestCompanyIds.ContainerItemId,
                ContainerId = Phase3FinanceE2ETestCompanyIds.ContainerId,
                LineNumber = 1,
                FabricItemId = Phase3FinanceE2ETestCompanyIds.FabricItemId,
                FabricColorId = Phase3FinanceE2ETestCompanyIds.FabricColorId,
                RollCount = Phase3FinanceE2ETestCompanyIds.SeedRollCount,
                LengthMeters = Phase3FinanceE2ETestCompanyIds.SeedRollCount * Phase3FinanceE2ETestCompanyIds.DefaultRollMeters
            });
        }

        if (!await context.WarehouseStocks.AnyAsync(
                s => s.ContainerId == Phase3FinanceE2ETestCompanyIds.ContainerId, ct))
        {
            var totalMeters = Phase3FinanceE2ETestCompanyIds.SeedRollCount * Phase3FinanceE2ETestCompanyIds.DefaultRollMeters;
            context.WarehouseStocks.Add(new WarehouseStockEntity
            {
                Id = Guid.NewGuid(),
                WarehouseId = Phase3FinanceE2ETestCompanyIds.WarehouseId,
                FabricItemId = Phase3FinanceE2ETestCompanyIds.FabricItemId,
                FabricColorId = Phase3FinanceE2ETestCompanyIds.FabricColorId,
                ContainerId = Phase3FinanceE2ETestCompanyIds.ContainerId,
                RollCount = Phase3FinanceE2ETestCompanyIds.SeedRollCount,
                TotalMeters = totalMeters,
                ReservedMeters = 0m,
                AvailableMeters = totalMeters
            });
        }

        var existingRolls = await context.FabricRolls.CountAsync(
            r => r.ContainerId == Phase3FinanceE2ETestCompanyIds.ContainerId, ct);
        for (var i = existingRolls + 1; i <= Phase3FinanceE2ETestCompanyIds.SeedRollCount; i++)
        {
            context.FabricRolls.Add(new FabricRollEntity
            {
                Id = Guid.NewGuid(),
                ContainerId = Phase3FinanceE2ETestCompanyIds.ContainerId,
                ContainerItemId = Phase3FinanceE2ETestCompanyIds.ContainerItemId,
                FabricItemId = Phase3FinanceE2ETestCompanyIds.FabricItemId,
                FabricColorId = Phase3FinanceE2ETestCompanyIds.FabricColorId,
                WarehouseId = Phase3FinanceE2ETestCompanyIds.WarehouseId,
                RollNumber = i,
                LengthMeters = Phase3FinanceE2ETestCompanyIds.DefaultRollMeters,
                RemainingLengthMeters = Phase3FinanceE2ETestCompanyIds.DefaultRollMeters,
                CostPerMeter = Phase3FinanceE2ETestCompanyIds.CostPerMeter,
                Status = (int)FabricRollStatus.Available,
                ReservationStatus = 0
            });
        }
    }

    private static async Task EnsureCreditInvoiceAsync(ErpDbContext context, CancellationToken ct)
    {
        if (await context.SalesInvoices.AnyAsync(i => i.Id == Phase3FinanceE2ETestCompanyIds.CreditInvoiceId, ct))
            return;

        var total = Phase3FinanceE2ETestCompanyIds.CreditInvoiceTotal;
        context.SalesInvoices.Add(new SalesInvoiceEntity
        {
            Id = Phase3FinanceE2ETestCompanyIds.CreditInvoiceId,
            CompanyId = Phase3FinanceE2ETestCompanyIds.CompanyId,
            BranchId = Phase3FinanceE2ETestCompanyIds.BranchId,
            InvoiceNumber = "E3F-INV-OPEN-001",
            CustomerId = Phase3FinanceE2ETestCompanyIds.CustomerId,
            WarehouseId = Phase3FinanceE2ETestCompanyIds.WarehouseId,
            ChinaContainerId = Phase3FinanceE2ETestCompanyIds.ContainerId,
            InvoiceDate = DateTime.UtcNow.AddDays(-7),
            PaymentType = (int)PaymentType.Credit,
            Status = (int)SalesInvoiceStatus.Approved,
            SubTotal = total,
            TaxTotal = 0m,
            DiscountTotal = 0m,
            GrandTotal = total,
            ApprovedAt = DateTime.UtcNow.AddDays(-7)
        });

        var jeId = Guid.Parse("e3f00051-0051-0051-0051-000000005051");
        if (!await context.JournalEntries.AnyAsync(j => j.Id == jeId, ct))
        {
            context.JournalEntries.Add(new JournalEntryEntity
            {
                Id = jeId,
                CompanyId = Phase3FinanceE2ETestCompanyIds.CompanyId,
                BranchId = Phase3FinanceE2ETestCompanyIds.BranchId,
                EntryNumber = "E3F-JE-AR-001",
                EntryDate = DateTime.UtcNow.AddDays(-7),
                Description = "E2E seeded credit invoice AR",
                Status = (int)JournalEntryStatus.Posted,
                JournalBookId = Guid.Parse("c1000001-0001-0001-0001-000000000001"),
                SourceType = (int)DocumentType.SalesInvoice,
                SourceId = Phase3FinanceE2ETestCompanyIds.CreditInvoiceId,
                PostingKind = (int)PostingKind.SalesInvoicePosting,
                PostedAt = DateTime.UtcNow.AddDays(-7)
            });
            context.JournalEntryLines.Add(new JournalEntryLineEntity
            {
                Id = Guid.NewGuid(),
                JournalEntryId = jeId,
                AccountId = Phase3FinanceE2ETestCompanyIds.AccountsReceivable,
                Debit = total,
                Credit = 0m,
                Narrative = "Test AR open invoice",
                PartyId = Phase3FinanceE2ETestCompanyIds.CustomerId
            });
            context.JournalEntryLines.Add(new JournalEntryLineEntity
            {
                Id = Guid.NewGuid(),
                JournalEntryId = jeId,
                AccountId = Phase3FinanceE2ETestCompanyIds.SalesRevenue,
                Debit = 0m,
                Credit = total,
                Narrative = "Test revenue"
            });
        }
    }

    private static async Task EnsureDocumentCountersAsync(ErpDbContext context, CancellationToken ct)
    {
        foreach (var docType in new[] { "SalesInvoice", "ReceiptVoucher", "JournalEntry" })
        {
            if (await context.DocumentCounters.AnyAsync(
                    d => d.BranchId == Phase3FinanceE2ETestCompanyIds.BranchId && d.DocumentType == docType, ct))
                continue;

            context.DocumentCounters.Add(new DocumentCounterEntity
            {
                Id = Guid.NewGuid(),
                BranchId = Phase3FinanceE2ETestCompanyIds.BranchId,
                DocumentType = docType,
                Prefix = docType switch
                {
                    "SalesInvoice" => "E3F-INV",
                    "ReceiptVoucher" => "E3F-RV",
                    _ => "E3F-JE"
                },
                LastNumber = 1
            });
        }
    }
}

public sealed class Phase3FinanceE2ESeedResult
{
    public Guid CompanyId { get; init; }
    public Guid BranchId { get; init; }
    public Guid CustomerId { get; init; }
    public Guid CashboxAId { get; init; }
    public Guid CashboxBId { get; init; }
    public Guid BankAccountId { get; init; }
    public Guid CreditInvoiceId { get; init; }
}
