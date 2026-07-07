using ERPSystem.Application.Abstractions.Services;
using ERPSystem.Application.Common;
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
        await EnsureSchemasAsync(context, cancellationToken);
        await EnsureAdminPasswordAsync(context, passwordHasher, logger, cancellationToken);
        await EnsureChinaImportReferenceDataAsync(context, cancellationToken);
        await EnsureIntegratedAccountingAccountsAsync(context, cancellationToken);
        await EnsureJournalBooksAsync(context, cancellationToken);
        await EnsureAccountingPermissionsAsync(context, cancellationToken);
        await EnsureSupplierPermissionsAsync(context, cancellationToken);
        await EnsurePurchasePermissionsAsync(context, cancellationToken);
        await EnsureCustomerPermissionsAsync(context, cancellationToken);
        await EnsureSalesPermissionsAsync(context, cancellationToken);
        await EnsureFinancePermissionsAsync(context, cancellationToken);
        await EnsureOpeningBalancePermissionsAsync(context, cancellationToken);
        await EnsureContainerPermissionsAsync(context, cancellationToken);
        await EnsureWarehousePermissionsAsync(context, cancellationToken);
        await EnsureHrPermissionsAsync(context, cancellationToken);
        await EnsureDefaultCurrencyAsync(context, cancellationToken);
        await ExpenseModuleSeeder.EnsureAsync(context, DefaultCompanyId, AdminRoleId, cancellationToken);
        await CapitalModuleSeeder.EnsureAsync(context, AdminRoleId, cancellationToken);

        if (await context.Companies.AnyAsync(cancellationToken))
        {
            logger.LogInformation("Database already seeded.");
            return;
        }

        logger.LogInformation("Seeding ERP PRO database...");

        context.Companies.Add(new CompanyEntity
        {
            Id = DefaultCompanyId,
            Code = "ERP",
            NameAr = "شركة ERP PRO",
            NameEn = "ERP PRO Company",
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
            Currency = "USD"
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
            new SystemSettingEntity { Id = Guid.NewGuid(), Key = "CompanyName", Value = "ERP PRO", CompanyId = DefaultCompanyId }
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
        logger.LogInformation("ERP PRO seed completed.");
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

        foreach (var (id, code, nameAr, nameEn, type, postable) in roots)
        {
            if (await context.Accounts.AnyAsync(a => a.Id == id, cancellationToken))
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
            (AccountingAccountIds.PartnerCapital, "3200", "رأس مال الشركاء", "Partner Capital", "Equity", AccountingAccountIds.RootEquity)
        };

        foreach (var (id, code, nameAr, nameEn, type, parentId) in accounts)
        {
            var existing = await context.Accounts.FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
            if (existing is null)
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
            else if (existing.ParentId != parentId)
            {
                existing.ParentId = parentId;
            }
        }

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

        foreach (var (id, code, nameAr, nameEn, type) in books)
        {
            if (await context.JournalBooks.AnyAsync(b => b.Id == id, cancellationToken))
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

    private static async Task EnsurePermissionsAsync(
        ErpDbContext context,
        IEnumerable<(string Code, string Module, string Action)> permissions,
        CancellationToken cancellationToken)
    {
        foreach (var (code, module, action) in permissions)
            await EnsurePermissionAsync(context, code, module, action, cancellationToken);

        await context.SaveChangesAsync(cancellationToken);
    }

    private static async Task EnsureAccountingPermissionsAsync(ErpDbContext context, CancellationToken cancellationToken)
    {
        await EnsurePermissionsAsync(context,
        [
            ("accounting.account.create", "accounting", "account-create"),
            ("accounting.account.edit", "accounting", "account-edit"),
            ("accounting.account.deactivate", "accounting", "account-deactivate"),
            ("accounting.account.view", "accounting", "account-view"),
            ("accounting.journal.create", "accounting", "journal-create"),
            ("accounting.journal.post", "accounting", "journal-post"),
            ("accounting.journal.reverse", "accounting", "journal-reverse")
        ], cancellationToken);
    }

    private static async Task EnsureSupplierPermissionsAsync(ErpDbContext context, CancellationToken cancellationToken)
    {
        await EnsurePermissionsAsync(context,
        [
            ("suppliers.create", "suppliers", "create"),
            ("suppliers.deactivate", "suppliers", "deactivate"),
            ("suppliers.opening-balance", "suppliers", "opening-balance")
        ], cancellationToken);
    }

    private static async Task EnsureCustomerPermissionsAsync(ErpDbContext context, CancellationToken cancellationToken)
    {
        await EnsurePermissionsAsync(context,
        [
            ("customers.create", "customers", "create"),
            ("customers.deactivate", "customers", "deactivate"),
            ("customers.opening-balance", "customers", "opening-balance")
        ], cancellationToken);
    }

    private static async Task EnsureSalesPermissionsAsync(ErpDbContext context, CancellationToken cancellationToken)
    {
        await EnsurePermissionsAsync(context,
        [
            ("sales.create", "sales", "create"),
            ("sales.approve", "sales", "approve"),
            ("sales.send-to-warehouse", "sales", "send-to-warehouse"),
            ("sales.cancel", "sales", "cancel"),
            ("sales.deliver", "sales", "deliver"),
            ("sales.return", "sales", "return")
        ], cancellationToken);
    }

    private static async Task EnsureFinancePermissionsAsync(ErpDbContext context, CancellationToken cancellationToken)
    {
        await EnsurePermissionsAsync(context,
        [
            ("finance.receipt.create", "finance", "receipt-create"),
            ("finance.receipt.post", "finance", "receipt-post"),
            ("finance.payment.create", "finance", "payment-create"),
            ("finance.payment.post", "finance", "payment-post"),
            ("finance.cashbox.create", "finance", "cashbox-create"),
            ("finance.cashbox.edit", "finance", "cashbox-edit"),
            ("finance.cashbox.transfer", "finance", "cashbox-transfer")
        ], cancellationToken);
    }

    private static async Task EnsureOpeningBalancePermissionsAsync(ErpDbContext context, CancellationToken cancellationToken)
    {
        await EnsurePermissionsAsync(context,
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
        ], cancellationToken);
    }

    private static async Task EnsureContainerPermissionsAsync(ErpDbContext context, CancellationToken cancellationToken)
    {
        await EnsurePermissionsAsync(context,
        [
            ("containers.create", "containers", "create"),
            ("containers.approve", "containers", "approve"),
            ("containers.landing-cost", "containers", "landing-cost"),
            ("containers.move-to-warehouse", "containers", "move-to-warehouse")
        ], cancellationToken);
    }

    private static async Task EnsureWarehousePermissionsAsync(ErpDbContext context, CancellationToken cancellationToken)
    {
        await EnsurePermissionsAsync(context,
        [
            ("warehouse.detailing", "warehouse", "detailing")
        ], cancellationToken);
    }

    private static async Task EnsurePurchasePermissionsAsync(ErpDbContext context, CancellationToken cancellationToken)
    {
        await EnsurePermissionsAsync(context,
        [
            ("purchases.create", "purchases", "create"),
            ("purchases.post", "purchases", "post")
        ], cancellationToken);
    }

    private static async Task EnsureHrPermissionsAsync(ErpDbContext context, CancellationToken cancellationToken)
    {
        await EnsurePermissionsAsync(context,
        [
            ("hr.employee.manage", "hr", "employee.manage"),
            ("hr.department.manage", "hr", "department.manage")
        ], cancellationToken);
    }

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
}
