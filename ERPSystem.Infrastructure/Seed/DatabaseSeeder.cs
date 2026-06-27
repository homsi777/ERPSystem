using ERPSystem.Infrastructure.Persistence;
using ERPSystem.Infrastructure.Persistence.Models.Catalog;
using ERPSystem.Infrastructure.Persistence.Models.Company;
using ERPSystem.Infrastructure.Persistence.Models.Documents;
using ERPSystem.Infrastructure.Persistence.Models.Finance;
using ERPSystem.Infrastructure.Persistence.Models.Identity;
using ERPSystem.Infrastructure.Persistence.Models.Inventory;
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

    public static async Task SeedAsync(ErpDbContext context, ILogger logger, CancellationToken cancellationToken = default)
    {
        await EnsureSchemasAsync(context, cancellationToken);

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
            ("accounting.journal.reverse", "accounting", "journal-reverse")
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

        var counterTypes = new[] { "SalesInvoice", "Container", "ReceiptVoucher", "PaymentVoucher", "JournalEntry", "Customer", "Supplier", "PurchaseInvoice" };
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
                    _ => "DOC"
                },
                LastNumber = 0
            });
        }

        await context.SaveChangesAsync(cancellationToken);
        logger.LogInformation("ERP PRO seed completed.");
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
    }
}
