using ERPSystem.Application.Common;
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
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ERPSystem.Infrastructure.Seed;

public static class DatabaseSeeder
{
    public static readonly Guid DefaultCompanyId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    public static readonly Guid DefaultBranchId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    public static readonly Guid AdminUserId = Guid.Parse("33333333-3333-3333-3333-333333333333");
    public static readonly Guid AdminRoleId = Guid.Parse("44444444-4444-4444-4444-444444444444");
    public static readonly Guid DefaultWarehouseId = Guid.Parse("55555555-5555-5555-5555-555555555555");
    public static readonly Guid DefaultCashboxId = Guid.Parse("66666666-6666-6666-6666-666666666666");
    public static readonly Guid DefaultChinaSupplierId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

    public static async Task SeedAsync(ErpDbContext context, ILogger logger, CancellationToken cancellationToken = default)
    {
        await EnsureSchemasAsync(context, cancellationToken);
        await EnsureChinaImportReferenceDataAsync(context, cancellationToken);
        await EnsureIntegratedAccountingAccountsAsync(context, cancellationToken);
        await EnsureJournalBooksAsync(context, cancellationToken);
        await EnsureAccountingPermissionsAsync(context, cancellationToken);
        await ChinaImportFabricCatalogSeeder.EnsureAsync(context, DefaultCompanyId, cancellationToken);
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
            DefaultCurrency = "SAR"
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
            PasswordHash = "CHANGE_ME"
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
        {
            var permissionId = Guid.NewGuid();
            context.Permissions.Add(new PermissionEntity
            {
                Id = permissionId,
                Code = code,
                Module = module,
                Action = action
            });
            context.RolePermissions.Add(new RolePermissionEntity { RoleId = AdminRoleId, PermissionId = permissionId });
        }

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
            Currency = "SAR"
        });

        context.Suppliers.Add(new SupplierEntity
        {
            Id = DefaultChinaSupplierId,
            CompanyId = DefaultCompanyId,
            Code = "SUP-CN-001",
            Name = "مورد قوانغتشو",
            Status = 0,
            Balance = 0,
            BalanceCurrency = "SAR"
        });

        var categoryId = Guid.Parse("77777777-7777-7777-7777-777777777777");
        context.FabricCategories.Add(new FabricCategoryEntity
        {
            Id = categoryId,
            CompanyId = DefaultCompanyId,
            Code = "FAB",
            NameAr = "أقمشة",
            NameEn = "Fabrics"
        });

        context.FabricItems.Add(new FabricItemEntity
        {
            Id = Guid.Parse("88888888-8888-8888-8888-888888888888"),
            CompanyId = DefaultCompanyId,
            CategoryId = categoryId,
            Code = "FAB-001",
            NameAr = "قماش قطن",
            NameEn = "Cotton Fabric"
        });

        context.FabricColors.Add(new FabricColorEntity
        {
            Id = Guid.Parse("99999999-9999-9999-9999-999999999999"),
            FabricItemId = Guid.Parse("88888888-8888-8888-8888-888888888888"),
            Code = "WHITE",
            NameAr = "أبيض",
            NameEn = "White"
        });

        context.SystemSettings.AddRange(
            new SystemSettingEntity { Id = Guid.NewGuid(), Key = "DefaultCurrency", Value = "SAR", CompanyId = DefaultCompanyId },
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
                Status = 0,
                Balance = 0,
                BalanceCurrency = "SAR"
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
            (AccountingAccountIds.CostOfGoodsSold, "5100", "تكلفة مبيعات", "Cost of Goods Sold", "Expense", AccountingAccountIds.RootExpense),
            (AccountingAccountIds.OperatingExpenses, "5210", "مصاريف تشغيل", "Operating Expenses", "Expense", AccountingAccountIds.RootExpense)
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

    private static async Task EnsureAccountingPermissionsAsync(ErpDbContext context, CancellationToken cancellationToken)
    {
        var codes = new[]
        {
            "accounting.account.create",
            "accounting.account.edit",
            "accounting.account.deactivate",
            "accounting.account.view"
        };

        foreach (var code in codes)
        {
            if (await context.Permissions.AnyAsync(p => p.Code == code, cancellationToken))
                continue;

            var permissionId = Guid.NewGuid();
            context.Permissions.Add(new PermissionEntity
            {
                Id = permissionId,
                Code = code,
                Module = "accounting",
                Action = code.Split('.').Last()
            });

            if (await context.Roles.AnyAsync(r => r.Id == AdminRoleId, cancellationToken))
            {
                context.RolePermissions.Add(new RolePermissionEntity
                {
                    RoleId = AdminRoleId,
                    PermissionId = permissionId
                });
            }
        }

        await context.SaveChangesAsync(cancellationToken);
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
    }
}
