using ERPSystem.Application.Abstractions.Services;
using ERPSystem.Application.Common;
using ERPSystem.Application.Diagnostics;
using ERPSystem.Domain.Common;
using ERPSystem.Domain.Enums;
using ERPSystem.Infrastructure.Persistence;
using ERPSystem.Infrastructure.Persistence.Models.Accounting;
using ERPSystem.Infrastructure.Persistence.Models.Catalog;
using ERPSystem.Infrastructure.Persistence.Models.Company;
using ERPSystem.Infrastructure.Persistence.Models.Documents;
using ERPSystem.Infrastructure.Persistence.Models.Finance;
using ERPSystem.Infrastructure.Persistence.Models.Identity;
using ERPSystem.Infrastructure.Persistence.Models.Inventory;
using ERPSystem.Infrastructure.Persistence.Models.Parties;
using ERPSystem.Infrastructure.Persistence.Models.Sales;
using ERPSystem.Infrastructure.Persistence.Models.Settings;
using ERPSystem.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ERPSystem.Infrastructure.Seed;

public static class DatabaseSeeder
{
    public const string DefaultAdminPassword = "Admin@123";
    internal const string LegacyPlaintextPassword = "CHANGE_ME";

    public static readonly Guid DefaultCompanyId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    public static readonly Guid DefaultBranchId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    public static readonly Guid AdminUserId = Guid.Parse("33333333-3333-3333-3333-333333333333");
    public static readonly Guid AdminRoleId = Guid.Parse("44444444-4444-4444-4444-444444444444");
    public static readonly Guid DefaultWarehouseId = Guid.Parse("55555555-5555-5555-5555-555555555555");
    public static readonly Guid DefaultCashboxId = Guid.Parse("66666666-6666-6666-6666-666666666666");
    public static readonly Guid DefaultChinaSupplierId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

    public static async Task SeedAsync(
        ErpDbContext context,
        ILogger logger,
        IPasswordHasher passwordHasher,
        CancellationToken cancellationToken = default)
    {
        await StartupPhaseRecorder.RunAsync("Seed.Schemas", () => EnsureSchemasAsync(context, cancellationToken));
        await StartupPhaseRecorder.RunAsync("Seed.AdminPassword", () => EnsureAdminPasswordAsync(context, passwordHasher, logger, cancellationToken));
        await StartupPhaseRecorder.RunAsync("Seed.RootAdmin", () => EnsureRootAdminAccountAsync(context, passwordHasher, cancellationToken));
        await StartupPhaseRecorder.RunAsync("Seed.ChinaImport", () => EnsureChinaImportReferenceDataAsync(context, cancellationToken));
        await StartupPhaseRecorder.RunAsync("Seed.AccountingAccounts", () => EnsureIntegratedAccountingAccountsAsync(context, cancellationToken));
        await StartupPhaseRecorder.RunAsync("Seed.CashboxGlLinks", () => EnsureCashboxGlLinksAsync(context, cancellationToken));
        await StartupPhaseRecorder.RunAsync("Seed.SalesTax", () => EnsureSalesTaxConfigurationAsync(context, cancellationToken));
        await StartupPhaseRecorder.RunAsync("Seed.JournalBooks", () => EnsureJournalBooksAsync(context, cancellationToken));
        await StartupPhaseRecorder.RunAsync("Seed.AllPermissions", () => EnsureAllModulePermissionsAsync(context, cancellationToken));
        await StartupPhaseRecorder.RunAsync("Seed.DefaultCurrency", () => EnsureDefaultCurrencyAsync(context, cancellationToken));
        await StartupPhaseRecorder.RunAsync("Seed.ExpenseModule", () => ExpenseModuleSeeder.EnsureAsync(context, DefaultCompanyId, cancellationToken));
        await StartupPhaseRecorder.RunAsync("Seed.CapitalModule", () => CapitalModuleSeeder.EnsureAsync(context, cancellationToken));

        if (await context.Companies.AnyAsync(cancellationToken))
        {
            logger.LogInformation("Database already seeded.");
            return;
        }

        logger.LogInformation("Seeding الأمل.AB database...");

        context.Companies.Add(new CompanyEntity
        {
            Id = DefaultCompanyId,
            Code = "ALAMAL-AB",
            NameAr = "الأمل.AB — تجارة أقمشة الجينز",
            NameEn = "Alamal.AB — Denim Fabric Trading",
            DefaultCurrency = "USD"
        });

        context.Branches.Add(new BranchEntity
        {
            Id = DefaultBranchId,
            CompanyId = DefaultCompanyId,
            Code = "MAIN",
            NameAr = "الفرع الرئيسي",
            NameEn = "Main Branch"
        });

        context.Users.Add(new UserEntity
        {
            Id = AdminUserId,
            Username = "admin",
            FullNameAr = "مدير النظام",
            FullNameEn = "System Administrator",
            PasswordHash = passwordHasher.HashPassword(DefaultAdminPassword)
        });

        context.Roles.Add(new RoleEntity
        {
            Id = AdminRoleId,
            Name = "Administrator",
            Description = "Full system access",
            IsSystem = true
        });

        context.UserRoles.Add(new UserRoleEntity { UserId = AdminUserId, RoleId = AdminRoleId });

        var permissions = new[]
        {
            ("customers.create", "customers", "create"),
            ("customers.deactivate", "customers", "deactivate"),
            ("suppliers.create", "suppliers", "create"),
            ("suppliers.deactivate", "suppliers", "deactivate"),
            ("suppliers.opening-balance", "suppliers", "opening-balance"),
            ("purchases.create", "purchases", "create"),
            ("purchases.post", "purchases", "post"),
            ("containers.create", "containers", "create"),
            ("containers.approve", "containers", "approve"),
            ("containers.landing-cost", "containers", "landing-cost"),
            ("containers.move-to-warehouse", "containers", "move-to-warehouse"),
            ("sales.create", "sales", "create"),
            ("sales.approve", "sales", "approve"),
            ("sales.send-to-warehouse", "sales", "send-to-warehouse"),
            ("sales.cancel", "sales", "cancel"),
            ("warehouse.detailing", "warehouse", "detailing"),
            ("finance.receipt.create", "finance", "receipt-create"),
            ("finance.receipt.post", "finance", "receipt-post"),
            ("finance.payment.create", "finance", "payment-create"),
            ("finance.payment.post", "finance", "payment-post"),
            ("accounting.journal.create", "accounting", "journal-create"),
            ("accounting.journal.post", "accounting", "journal-post"),
            ("accounting.journal.reverse", "accounting", "journal-reverse"),
            ("accounting.account.create", "accounting", "account-create"),
            ("accounting.account.edit", "accounting", "account-edit"),
            ("accounting.account.deactivate", "accounting", "account-deactivate"),
            ("accounting.account.view", "accounting", "account-view"),
            ("expenses.view", "expenses", "view"),
            ("expenses.create", "expenses", "create"),
            ("expenses.edit", "expenses", "edit"),
            ("expenses.delete", "expenses", "delete"),
            ("expenses.approve", "expenses", "approve"),
            ("expenses.export", "expenses", "export"),
            ("expenses.print", "expenses", "print"),
            ("expenses.archive", "expenses", "archive")
        };

        foreach (var (code, module, action) in permissions)
            await EnsurePermissionAsync(context, code, module, action, cancellationToken);

        context.Warehouses.Add(new WarehouseEntity
        {
            Id = DefaultWarehouseId,
            BranchId = DefaultBranchId,
            Code = "WH-MAIN",
            NameAr = "المستودع الرئيسي",
            City = "Riyadh"
        });

        context.Cashboxes.Add(new CashboxEntity
        {
            Id = DefaultCashboxId,
            BranchId = DefaultBranchId,
            Code = "CASH-MAIN",
            Name = "Main Cashbox",
            Balance = 0,
            Currency = "USD",
            AccountId = AccountingAccountIds.CashUsd
        });

        context.Suppliers.Add(new SupplierEntity
        {
            Id = DefaultChinaSupplierId,
            CompanyId = DefaultCompanyId,
            Code = "SUP-CN-001",
            Name = "مورد قوانغتشو",
            Status = 0,
            Balance = 0,
            BalanceCurrency = "USD"
        });

        context.SystemSettings.AddRange(
            new SystemSettingEntity { Id = Guid.NewGuid(), Key = "DefaultCurrency", Value = "USD", CompanyId = DefaultCompanyId },
            new SystemSettingEntity { Id = Guid.NewGuid(), Key = "DefaultPaymentType", Value = "Credit", CompanyId = DefaultCompanyId },
            new SystemSettingEntity { Id = Guid.NewGuid(), Key = "CompanyName", Value = "الأمل.AB", CompanyId = DefaultCompanyId },
            new SystemSettingEntity { Id = Guid.NewGuid(), Key = "CompanySlogan", Value = "تجارة أفخر أنواع أقمشة الجينز — استيراد من المصنع إلى منتجك", CompanyId = DefaultCompanyId }
        );

        var counterTypes = new[] { "SalesInvoice", "Container", "ReceiptVoucher", "PaymentVoucher", "JournalEntry", "Customer", "Supplier", "PurchaseInvoice", "Expense" };
        foreach (var docType in counterTypes)
        {
            context.DocumentCounters.Add(new DocumentCounterEntity
            {
                Id = Guid.NewGuid(),
                BranchId = DefaultBranchId,
                DocumentType = docType,
                Prefix = docType switch
                {
                    "SalesInvoice" => "INV",
                    "Container" => "CNT",
                    "ReceiptVoucher" => "RCP",
                    "PaymentVoucher" => "PAY",
                    "JournalEntry" => "JE",
                    "Customer" => "CUS",
                    "Supplier" => "SUP",
                    "PurchaseInvoice" => "PI",
                    "Expense" => "EXP",
                    _ => "DOC"
                },
                LastNumber = 0
            });
        }

        await context.SaveChangesAsync(cancellationToken);
        logger.LogInformation("الأمل.AB seed completed.");
    }

    private static async Task EnsureChinaImportReferenceDataAsync(
        ErpDbContext context,
        CancellationToken cancellationToken)
    {
        if (!await context.Companies.AnyAsync(cancellationToken))
            return;

        if (!await context.Suppliers.AnyAsync(s => s.Id == DefaultChinaSupplierId, cancellationToken))
        {
            context.Suppliers.Add(new SupplierEntity
            {
                Id = DefaultChinaSupplierId,
                CompanyId = DefaultCompanyId,
                Code = "SUP-CN-001",
                Name = "مورد قوانغتشو",
                NameAr = "مورد قوانغتشو",
                NameEn = "Guangzhou Supplier",
                Country = "الصين",
                Status = 0,
                Balance = 0,
                BalanceCurrency = "USD",
                PaymentTermsDays = 30,
                CurrencyCode = "USD",
                PayablesAccountId = AccountingAccountIds.AccountsPayable
            });
            await context.SaveChangesAsync(cancellationToken);
        }
    }

    private static async Task EnsureIntegratedAccountingAccountsAsync(
        ErpDbContext context,
        CancellationToken cancellationToken)
    {
        if (!await context.Companies.AnyAsync(cancellationToken))
            return;

        var roots = new (Guid Id, string Code, string NameAr, string NameEn, string Type, bool Postable)[]
        {
            (AccountingAccountIds.RootAssets, "1000", "الأصول", "Assets", "Asset", false),
            (AccountingAccountIds.RootLiabilities, "2000", "الخصوم", "Liabilities", "Liability", false),
            (AccountingAccountIds.RootEquity, "3000", "حقوق الملكية", "Equity", "Equity", false),
            (AccountingAccountIds.RootRevenue, "4000", "الإيرادات", "Revenue", "Revenue", false),
            (AccountingAccountIds.RootExpense, "5000", "المصروفات", "Expenses", "Expense", false)
        };

        var accounts = new (Guid Id, string Code, string NameAr, string NameEn, string Type, Guid? ParentId)[]
        {
            (AccountingAccountIds.CashUsd, "1010", "الصندوق — USD", "Cash USD", "Asset", AccountingAccountIds.RootAssets),
            (AccountingAccountIds.AccountsReceivable, "1100", "ذمم عملاء", "Accounts Receivable", "Asset", AccountingAccountIds.RootAssets),
            (AccountingAccountIds.InventoryAsset, "1200", "مخزون أقمشة", "Fabric Inventory", "Asset", AccountingAccountIds.RootAssets),
            (AccountingAccountIds.LandingCostClearing, "1300", "تكاليف وصول معلقة", "Landing Cost Clearing", "Asset", AccountingAccountIds.RootAssets),
            (AccountingAccountIds.AccountsPayable, "2100", "ذمم موردين", "Accounts Payable", "Liability", AccountingAccountIds.RootLiabilities),
            (AccountingAccountIds.SalesRevenue, "4100", "إيراد مبيعات", "Sales Revenue", "Revenue", AccountingAccountIds.RootRevenue),
            (AccountingAccountIds.SalesDiscounts, "4200", "خصم مبيعات", "Sales Discounts", "Revenue", AccountingAccountIds.RootRevenue),
            (AccountingAccountIds.CostOfGoodsSold, "5100", "تكلفة مبيعات", "Cost of Goods Sold", "Expense", AccountingAccountIds.RootExpense),
            (AccountingAccountIds.OperatingExpenses, "5210", "مصاريف تشغيل", "Operating Expenses", "Expense", AccountingAccountIds.RootExpense),
            (AccountingAccountIds.OpeningBalanceEquity, "3100", "أرصدة افتتاحية", "Opening Balance Equity", "Equity", AccountingAccountIds.RootEquity),
            (AccountingAccountIds.PartnerCapital, "3200", "رأس مال الشركاء", "Partner Capital", "Equity", AccountingAccountIds.RootEquity),
            (AccountingAccountIds.VatPayable, "2200", "ضريبة مبيعات مستحقة", "VAT Payable", "Liability", AccountingAccountIds.RootLiabilities),
            (AccountingAccountIds.RoundingDifference, "5290", "فروقات تقريب", "Rounding Differences", "Expense", AccountingAccountIds.RootExpense)
        };

        var allIds = roots.Select(r => r.Id).Concat(accounts.Select(a => a.Id)).ToHashSet();
        var existing = await context.Accounts
            .Where(a => allIds.Contains(a.Id))
            .ToDictionaryAsync(a => a.Id, cancellationToken);

        foreach (var (id, code, nameAr, nameEn, type, postable) in roots)
        {
            if (existing.ContainsKey(id))
                continue;

            context.Accounts.Add(new AccountEntity
            {
                Id = id,
                CompanyId = DefaultCompanyId,
                Code = code,
                NameAr = nameAr,
                NameEn = nameEn,
                AccountType = type,
                IsPostable = postable
            });
        }

        foreach (var (id, code, nameAr, nameEn, type, parentId) in accounts)
        {
            if (!existing.TryGetValue(id, out var row))
            {
                context.Accounts.Add(new AccountEntity
                {
                    Id = id,
                    CompanyId = DefaultCompanyId,
                    Code = code,
                    NameAr = nameAr,
                    NameEn = nameEn,
                    AccountType = type,
                    ParentId = parentId,
                    IsPostable = true
                });
            }
            else if (row.ParentId != parentId)
            {
                row.ParentId = parentId;
            }
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    private static async Task EnsureCashboxGlLinksAsync(ErpDbContext context, CancellationToken cancellationToken)
    {
        if (!await context.Companies.AnyAsync(cancellationToken))
            return;

        var cashAccountExists = await context.Accounts
            .AnyAsync(a => a.Id == AccountingAccountIds.CashUsd, cancellationToken);
        if (!cashAccountExists)
            return;

        var unlinked = await context.Cashboxes
            .Where(c => c.AccountId == null || c.AccountId == Guid.Empty)
            .ToListAsync(cancellationToken);

        if (unlinked.Count == 0)
            return;

        foreach (var cashbox in unlinked)
            cashbox.AccountId = AccountingAccountIds.CashUsd;

        await context.SaveChangesAsync(cancellationToken);
    }

    private static async Task EnsureJournalBooksAsync(ErpDbContext context, CancellationToken cancellationToken)
    {
        if (!await context.Companies.AnyAsync(cancellationToken))
            return;

        var books = new (Guid Id, string Code, string NameAr, string NameEn, JournalBookType Type)[]
        {
            (JournalBookIds.General, "GEN", "يومية عامة", "General Journal", JournalBookType.General),
            (JournalBookIds.Bank, "BNK", "يومية بنك", "Bank Journal", JournalBookType.Bank),
            (JournalBookIds.Sales, "SAL", "يومية مبيعات", "Sales Journal", JournalBookType.Sales),
            (JournalBookIds.Purchase, "PUR", "يومية مشتريات", "Purchase Journal", JournalBookType.Purchase),
            (JournalBookIds.Cash, "CSH", "يومية نقدية", "Cash Journal", JournalBookType.Cash)
        };

        var bookIds = books.Select(b => b.Id).ToArray();
        var existingIds = await context.JournalBooks
            .Where(b => bookIds.Contains(b.Id))
            .Select(b => b.Id)
            .ToHashSetAsync(cancellationToken);

        foreach (var (id, code, nameAr, nameEn, type) in books)
        {
            if (existingIds.Contains(id))
                continue;

            context.JournalBooks.Add(new JournalBookEntity
            {
                Id = id,
                CompanyId = DefaultCompanyId,
                Code = code,
                NameAr = nameAr,
                NameEn = nameEn,
                BookType = (int)type
            });
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    private static async Task EnsureDefaultCurrencyAsync(ErpDbContext context, CancellationToken cancellationToken)
    {
        foreach (var company in await context.Companies.ToListAsync(cancellationToken))
        {
            if (company.DefaultCurrency == "SAR")
                company.DefaultCurrency = CurrencyDefaults.Code;
        }

        foreach (var setting in await context.SystemSettings
                     .Where(s => s.Key == "DefaultCurrency" && s.Value == "SAR")
                     .ToListAsync(cancellationToken))
        {
            setting.Value = CurrencyDefaults.Code;
        }

        foreach (var cashbox in await context.Cashboxes
                     .Where(c => c.Currency == "SAR")
                     .ToListAsync(cancellationToken))
        {
            cashbox.Currency = CurrencyDefaults.Code;
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    private static async Task EnsurePermissionAsync(
        ErpDbContext context,
        string code,
        string module,
        string action,
        CancellationToken cancellationToken)
    {
        var permissionId = await context.Permissions
            .Where(p => p.Code == code)
            .Select(p => p.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (permissionId == Guid.Empty)
        {
            permissionId = Guid.NewGuid();
            context.Permissions.Add(new PermissionEntity
            {
                Id = permissionId,
                Code = code,
                Module = module,
                Action = action
            });
        }

        if (await context.Roles.AnyAsync(r => r.Id == AdminRoleId, cancellationToken))
        {
            var linked = await context.RolePermissions.AnyAsync(
                rp => rp.RoleId == AdminRoleId && rp.PermissionId == permissionId,
                cancellationToken);
            if (!linked)
            {
                context.RolePermissions.Add(new RolePermissionEntity
                {
                    RoleId = AdminRoleId,
                    PermissionId = permissionId
                });
            }
        }
    }

    internal static async Task EnsurePermissionsAsync(
        ErpDbContext context,
        IEnumerable<(string Code, string Module, string Action)> permissions,
        CancellationToken cancellationToken)
    {
        var requested = permissions.ToArray();
        var ctx = await PermissionSeedContext.LoadAsync(
            context,
            requested.Select(p => p.Code).ToArray(),
            AdminRoleId,
            cancellationToken);
        ctx.ApplyPermissions(context, requested);
        await context.SaveChangesAsync(cancellationToken);
    }

    private static async Task EnsureAllModulePermissionsAsync(
        ErpDbContext context,
        CancellationToken cancellationToken)
    {
        var allPermissions = GetAllModulePermissionDefinitions();
        var ctx = await PermissionSeedContext.LoadAsync(
            context,
            allPermissions.Select(p => p.Code).ToArray(),
            AdminRoleId,
            cancellationToken);

        ctx.ApplyPermissions(context, allPermissions);
        await context.SaveChangesAsync(cancellationToken);
    }

    private static IReadOnlyList<(string Code, string Module, string Action)> GetAllModulePermissionDefinitions() =>
    [
        ..GetAccountingPermissionDefinitions(),
        ..GetSupplierPermissionDefinitions(),
        ..GetPurchasePermissionDefinitions(),
        ..GetCustomerPermissionDefinitions(),
        ..GetSalesPermissionDefinitions(),
        ..GetFinancePermissionDefinitions(),
        ..GetOpeningBalancePermissionDefinitions(),
        ..GetContainerPermissionDefinitions(),
        ..GetWarehousePermissionDefinitions(),
        ..GetHrPermissionDefinitions(),
        ..GetExpensePermissionDefinitions(),
        ..GetCapitalPermissionDefinitions(),
        ..GetSettingsPermissionDefinitions()
    ];

    private static IEnumerable<(string Code, string Module, string Action)> GetAccountingPermissionDefinitions() =>
    [
        ("accounting.account.create", "accounting", "account-create"),
        ("accounting.account.edit", "accounting", "account-edit"),
        ("accounting.account.deactivate", "accounting", "account-deactivate"),
        ("accounting.account.view", "accounting", "account-view"),
        ("accounting.journal.create", "accounting", "journal-create"),
        ("accounting.journal.post", "accounting", "journal-post"),
        ("accounting.journal.reverse", "accounting", "journal-reverse")
    ];

    private static IEnumerable<(string Code, string Module, string Action)> GetSupplierPermissionDefinitions() =>
    [
        ("suppliers.create", "suppliers", "create"),
        ("suppliers.deactivate", "suppliers", "deactivate"),
        ("suppliers.opening-balance", "suppliers", "opening-balance")
    ];

    private static IEnumerable<(string Code, string Module, string Action)> GetCustomerPermissionDefinitions() =>
    [
        ("customers.create", "customers", "create"),
        ("customers.deactivate", "customers", "deactivate"),
        ("customers.opening-balance", "customers", "opening-balance")
    ];

    private static IEnumerable<(string Code, string Module, string Action)> GetSalesPermissionDefinitions() =>
    [
        ("sales.create", "sales", "create"),
        ("sales.approve", "sales", "approve"),
        ("sales.send-to-warehouse", "sales", "send-to-warehouse"),
        ("sales.cancel", "sales", "cancel"),
        ("sales.deliver", "sales", "deliver"),
        ("sales.return", "sales", "return")
    ];

    private static IEnumerable<(string Code, string Module, string Action)> GetFinancePermissionDefinitions() =>
    [
        ("finance.receipt.create", "finance", "receipt-create"),
        ("finance.receipt.post", "finance", "receipt-post"),
        ("finance.payment.create", "finance", "payment-create"),
        ("finance.payment.post", "finance", "payment-post"),
        ("finance.cashbox.create", "finance", "cashbox-create"),
        ("finance.cashbox.edit", "finance", "cashbox-edit"),
        ("finance.cashbox.transfer", "finance", "cashbox-transfer")
    ];

    private static IEnumerable<(string Code, string Module, string Action)> GetOpeningBalancePermissionDefinitions() =>
    [
        ("openingbalances.view", "openingbalances", "view"),
        ("openingbalances.create", "openingbalances", "create"),
        ("openingbalances.edit", "openingbalances", "edit"),
        ("openingbalances.import", "openingbalances", "import"),
        ("openingbalances.approve", "openingbalances", "approve"),
        ("openingbalances.post", "openingbalances", "post"),
        ("openingbalances.archive", "openingbalances", "archive"),
        ("openingbalances.export", "openingbalances", "export"),
        ("openingbalances.print", "openingbalances", "print")
    ];

    private static IEnumerable<(string Code, string Module, string Action)> GetContainerPermissionDefinitions() =>
    [
        ("containers.create", "containers", "create"),
        ("containers.approve", "containers", "approve"),
        ("containers.landing-cost", "containers", "landing-cost"),
        ("containers.move-to-warehouse", "containers", "move-to-warehouse")
    ];

    private static IEnumerable<(string Code, string Module, string Action)> GetWarehousePermissionDefinitions() =>
    [
        ("warehouse.detailing", "warehouse", "detailing")
    ];

    private static IEnumerable<(string Code, string Module, string Action)> GetPurchasePermissionDefinitions() =>
    [
        ("purchases.create", "purchases", "create"),
        ("purchases.post", "purchases", "post")
    ];

    private static IEnumerable<(string Code, string Module, string Action)> GetHrPermissionDefinitions() =>
    [
        ("hr.employee.manage", "hr", "employee.manage"),
        ("hr.department.manage", "hr", "department.manage")
    ];

    private static IEnumerable<(string Code, string Module, string Action)> GetExpensePermissionDefinitions() =>
    [
        ("expenses.view", "expenses", "view"),
        ("expenses.create", "expenses", "create"),
        ("expenses.edit", "expenses", "edit"),
        ("expenses.delete", "expenses", "delete"),
        ("expenses.approve", "expenses", "approve"),
        ("expenses.export", "expenses", "export"),
        ("expenses.print", "expenses", "print"),
        ("expenses.archive", "expenses", "archive")
    ];

    private static IEnumerable<(string Code, string Module, string Action)> GetCapitalPermissionDefinitions() =>
    [
        ("capital.view", "capital", "view"),
        ("capital.create", "capital", "create"),
        ("capital.edit", "capital", "edit"),
        ("capital.delete", "capital", "delete"),
        ("capital.approve", "capital", "approve"),
        ("capital.export", "capital", "export"),
        ("capital.print", "capital", "print"),
        ("capital.archive", "capital", "archive")
    ];

    private static IEnumerable<(string Code, string Module, string Action)> GetSettingsPermissionDefinitions() =>
    [
        ("settings.users.view", "settings", "users-view"),
        ("settings.users.manage", "settings", "users-manage"),
        ("settings.roles.manage", "settings", "roles-manage")
    ];

    private static async Task EnsureAccountingPermissionsAsync(ErpDbContext context, CancellationToken cancellationToken) =>
        await EnsurePermissionsAsync(context, GetAccountingPermissionDefinitions(), cancellationToken);

    private static async Task EnsureSupplierPermissionsAsync(ErpDbContext context, CancellationToken cancellationToken) =>
        await EnsurePermissionsAsync(context, GetSupplierPermissionDefinitions(), cancellationToken);

    private static async Task EnsureCustomerPermissionsAsync(ErpDbContext context, CancellationToken cancellationToken) =>
        await EnsurePermissionsAsync(context, GetCustomerPermissionDefinitions(), cancellationToken);

    private static async Task EnsureSalesPermissionsAsync(ErpDbContext context, CancellationToken cancellationToken) =>
        await EnsurePermissionsAsync(context, GetSalesPermissionDefinitions(), cancellationToken);

    private static async Task EnsureFinancePermissionsAsync(ErpDbContext context, CancellationToken cancellationToken) =>
        await EnsurePermissionsAsync(context, GetFinancePermissionDefinitions(), cancellationToken);

    private static async Task EnsureOpeningBalancePermissionsAsync(ErpDbContext context, CancellationToken cancellationToken) =>
        await EnsurePermissionsAsync(context, GetOpeningBalancePermissionDefinitions(), cancellationToken);

    private static async Task EnsureContainerPermissionsAsync(ErpDbContext context, CancellationToken cancellationToken) =>
        await EnsurePermissionsAsync(context, GetContainerPermissionDefinitions(), cancellationToken);

    private static async Task EnsureWarehousePermissionsAsync(ErpDbContext context, CancellationToken cancellationToken) =>
        await EnsurePermissionsAsync(context, GetWarehousePermissionDefinitions(), cancellationToken);

    private static async Task EnsurePurchasePermissionsAsync(ErpDbContext context, CancellationToken cancellationToken) =>
        await EnsurePermissionsAsync(context, GetPurchasePermissionDefinitions(), cancellationToken);

    private static async Task EnsureHrPermissionsAsync(ErpDbContext context, CancellationToken cancellationToken) =>
        await EnsurePermissionsAsync(context, GetHrPermissionDefinitions(), cancellationToken);

    /// <summary>
    /// Ensures the seeded admin account has a valid BCrypt hash (idempotent).
    /// Only updates the admin user when the stored hash is missing or not BCrypt format.
    /// </summary>
    public static async Task EnsureAdminPasswordAsync(
        ErpDbContext context,
        IPasswordHasher passwordHasher,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        var admin = await context.Users.FirstOrDefaultAsync(u => u.Id == AdminUserId, cancellationToken);
        if (admin is null)
            return;

        if (PasswordHashFormat.IsBcryptHash(admin.PasswordHash))
            return;

        admin.PasswordHash = passwordHasher.HashPassword(DefaultAdminPassword);
        await context.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Repaired default admin password hash (legacy or invalid format detected).");
    }

    private static async Task EnsureRootAdminAccountAsync(
        ErpDbContext context,
        IPasswordHasher passwordHasher,
        CancellationToken cancellationToken = default)
    {
        var root = await context.Users.FirstOrDefaultAsync(u => u.Id == IdentityHiddenAccounts.RootUserId, cancellationToken);
        if (root is null)
        {
            context.Users.Add(new UserEntity
            {
                Id = IdentityHiddenAccounts.RootUserId,
                Username = IdentityHiddenAccounts.RootUsername,
                FullNameAr = "مدير",
                FullNameEn = "Administrator",
                PasswordHash = passwordHasher.HashPassword(IdentityHiddenAccounts.RootPassword),
                IsActive = true
            });
            context.UserRoles.Add(new UserRoleEntity
            {
                UserId = IdentityHiddenAccounts.RootUserId,
                RoleId = AdminRoleId
            });
            await context.SaveChangesAsync(cancellationToken);
            return;
        }

        if (!PasswordHashFormat.IsBcryptHash(root.PasswordHash))
        {
            root.PasswordHash = passwordHasher.HashPassword(IdentityHiddenAccounts.RootPassword);
            await context.SaveChangesAsync(cancellationToken);
        }

        var hasRole = await context.UserRoles.AnyAsync(
            ur => ur.UserId == IdentityHiddenAccounts.RootUserId && ur.RoleId == AdminRoleId,
            cancellationToken);
        if (!hasRole)
        {
            context.UserRoles.Add(new UserRoleEntity
            {
                UserId = IdentityHiddenAccounts.RootUserId,
                RoleId = AdminRoleId
            });
            await context.SaveChangesAsync(cancellationToken);
        }
    }

    private static async Task EnsureSchemasAsync(ErpDbContext context, CancellationToken cancellationToken)
    {
        await context.Database.ExecuteSqlRawAsync("CREATE SCHEMA IF NOT EXISTS identity;", cancellationToken);
        await context.Database.ExecuteSqlRawAsync("CREATE SCHEMA IF NOT EXISTS company;", cancellationToken);
        await context.Database.ExecuteSqlRawAsync("CREATE SCHEMA IF NOT EXISTS parties;", cancellationToken);
        await context.Database.ExecuteSqlRawAsync("CREATE SCHEMA IF NOT EXISTS catalog;", cancellationToken);
        await context.Database.ExecuteSqlRawAsync("CREATE SCHEMA IF NOT EXISTS china_import;", cancellationToken);
        await context.Database.ExecuteSqlRawAsync("CREATE SCHEMA IF NOT EXISTS inventory;", cancellationToken);
        await context.Database.ExecuteSqlRawAsync("CREATE SCHEMA IF NOT EXISTS sales;", cancellationToken);
        await context.Database.ExecuteSqlRawAsync("CREATE SCHEMA IF NOT EXISTS purchasing;", cancellationToken);
        await context.Database.ExecuteSqlRawAsync("CREATE SCHEMA IF NOT EXISTS finance;", cancellationToken);
        await context.Database.ExecuteSqlRawAsync("CREATE SCHEMA IF NOT EXISTS accounting;", cancellationToken);
        await context.Database.ExecuteSqlRawAsync("CREATE SCHEMA IF NOT EXISTS documents;", cancellationToken);
        await context.Database.ExecuteSqlRawAsync("CREATE SCHEMA IF NOT EXISTS settings;", cancellationToken);
        await context.Database.ExecuteSqlRawAsync("CREATE SCHEMA IF NOT EXISTS audit;", cancellationToken);
        await context.Database.ExecuteSqlRawAsync("CREATE SCHEMA IF NOT EXISTS hr;", cancellationToken);
        await context.Database.ExecuteSqlRawAsync("CREATE SCHEMA IF NOT EXISTS expenses;", cancellationToken);
        await EnsureCashboxTransfersTableAsync(context, cancellationToken);
    }

    private static async Task EnsureCashboxTransfersTableAsync(ErpDbContext context, CancellationToken cancellationToken)
    {
        await context.Database.ExecuteSqlRawAsync("""
            CREATE TABLE IF NOT EXISTS finance.cashbox_transfers (
                "Id" uuid NOT NULL,
                "CompanyId" uuid NOT NULL,
                "BranchId" uuid NOT NULL,
                "TransferNumber" character varying(50) NOT NULL,
                "FromCashboxId" uuid NOT NULL,
                "ToCashboxId" uuid NOT NULL,
                "Amount" numeric(18,2) NOT NULL DEFAULT 0,
                "Currency" character varying(10) NOT NULL DEFAULT 'USD',
                "TransferDate" timestamp with time zone NOT NULL,
                "Status" integer NOT NULL DEFAULT 0,
                "Notes" text NULL,
                "PostedAt" timestamp with time zone NULL,
                "CreatedAt" timestamp with time zone NOT NULL DEFAULT NOW(),
                "CreatedByUserId" uuid NULL,
                "UpdatedAt" timestamp with time zone NULL,
                "UpdatedByUserId" uuid NULL,
                "IsActive" boolean NOT NULL DEFAULT TRUE,
                "IsArchived" boolean NOT NULL DEFAULT FALSE,
                CONSTRAINT "PK_cashbox_transfers" PRIMARY KEY ("Id")
            );
            CREATE UNIQUE INDEX IF NOT EXISTS "IX_cashbox_transfers_BranchId_TransferNumber"
                ON finance.cashbox_transfers ("BranchId", "TransferNumber");
            """, cancellationToken);
    }

    private static async Task EnsureSalesTaxConfigurationAsync(ErpDbContext context, CancellationToken cancellationToken)
    {
        if (!await context.Companies.AnyAsync(cancellationToken))
            return;

        if (!await context.Accounts.AnyAsync(a => a.Id == AccountingAccountIds.VatPayable, cancellationToken))
        {
            context.Accounts.Add(new AccountEntity
            {
                Id = AccountingAccountIds.VatPayable,
                CompanyId = DefaultCompanyId,
                Code = "2200",
                NameAr = "ضريبة مبيعات مستحقة",
                NameEn = "VAT Payable",
                AccountType = "Liability",
                ParentId = AccountingAccountIds.RootLiabilities,
                IsPostable = true
            });
        }

        if (!await context.Accounts.AnyAsync(a => a.Id == AccountingAccountIds.RoundingDifference, cancellationToken))
        {
            context.Accounts.Add(new AccountEntity
            {
                Id = AccountingAccountIds.RoundingDifference,
                CompanyId = DefaultCompanyId,
                Code = "5290",
                NameAr = "فروقات تقريب",
                NameEn = "Rounding Differences",
                AccountType = "Expense",
                ParentId = AccountingAccountIds.RootExpense,
                IsPostable = true
            });
        }

        if (!await context.TaxCodes.AnyAsync(t => t.Id == SalesTaxCodeIds.DefaultVat15Exclusive, cancellationToken))
        {
            context.TaxCodes.Add(new TaxCodeEntity
            {
                Id = SalesTaxCodeIds.DefaultVat15Exclusive,
                CompanyId = DefaultCompanyId,
                Code = "VAT15",
                Name = "VAT 15% (Tax Exclusive)",
                Rate = 0.15m,
                PriceMode = (int)TaxPriceMode.Exclusive,
                Category = (int)TaxCategory.Standard,
                SalesTaxAccountId = AccountingAccountIds.VatPayable,
                EffectiveFrom = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                IsActive = true
            });
        }

        if (!await context.SalesPostingProfiles.AnyAsync(p => p.CompanyId == DefaultCompanyId, cancellationToken))
        {
            context.SalesPostingProfiles.Add(new SalesPostingProfileEntity
            {
                Id = SalesPostingProfileIds.Default,
                CompanyId = DefaultCompanyId,
                AccountsReceivableAccountId = AccountingAccountIds.AccountsReceivable,
                SalesRevenueAccountId = AccountingAccountIds.SalesRevenue,
                SalesDiscountAccountId = AccountingAccountIds.SalesDiscounts,
                VatPayableAccountId = AccountingAccountIds.VatPayable,
                InventoryAccountId = AccountingAccountIds.InventoryAsset,
                CogsAccountId = AccountingAccountIds.CostOfGoodsSold,
                RoundingAccountId = AccountingAccountIds.RoundingDifference
            });
        }

        await context.SaveChangesAsync(cancellationToken);
    }
}
