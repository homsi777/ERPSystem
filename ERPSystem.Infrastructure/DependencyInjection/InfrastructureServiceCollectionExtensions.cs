using ERPSystem.Application.Abstractions;
using ERPSystem.Application.Abstractions.Repositories;
using ERPSystem.Application.Diagnostics;
using ERPSystem.Application.Abstractions.Services;
using ERPSystem.Infrastructure.Audit;
using ERPSystem.Infrastructure.Notifications;
using ERPSystem.Infrastructure.Numbering;
using ERPSystem.Infrastructure.Persistence;
using ERPSystem.Infrastructure.Repositories;
using ERPSystem.Infrastructure.Security;
using ERPSystem.Infrastructure.Services;
using ERPSystem.Infrastructure.UnitOfWork;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ERPSystem.Infrastructure.DependencyInjection;

public static class InfrastructureServiceCollectionExtensions
{
    public const string ConnectionStringName = "DefaultConnection";

    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString(ConnectionStringName)
            ?? throw new InvalidOperationException($"Connection string '{ConnectionStringName}' is not configured.");

        services.AddDbContext<ErpDbContext>((sp, options) =>
        {
            options.UseNpgsql(connectionString, npgsql =>
            {
                npgsql.MigrationsAssembly(typeof(ErpDbContext).Assembly.FullName);
                npgsql.MigrationsHistoryTable("__ef_migrations_history", Schemas.Settings);
            });
            options.ConfigureWarnings(w =>
                w.Ignore(RelationalEventId.PendingModelChangesWarning));
            options.AddInterceptors(
                sp.GetRequiredService<UtcDateTimeSaveChangesInterceptor>(),
                sp.GetRequiredService<AuditSaveChangesInterceptor>());
        });

        services.AddDbContextFactory<ErpDbContext>((sp, options) =>
        {
            options.UseNpgsql(connectionString, npgsql =>
            {
                npgsql.MigrationsAssembly(typeof(ErpDbContext).Assembly.FullName);
                npgsql.MigrationsHistoryTable("__ef_migrations_history", Schemas.Settings);
            });
            options.ConfigureWarnings(w =>
                w.Ignore(RelationalEventId.PendingModelChangesWarning));
        });

        services.AddScoped<ISalesInvoiceListLookupLoader, SalesInvoiceListLookupLoader>();

        services.AddSingleton<UtcDateTimeSaveChangesInterceptor>();
        services.AddSingleton<AuditSaveChangesInterceptor>();
        services.AddMemoryCache();
        services.AddSingleton<ICacheService, InMemoryCacheService>();

        services.AddScoped<IUnitOfWork, EfUnitOfWork>();
        services.AddScoped<ICustomerRepository, CustomerRepository>();
        services.AddScoped<IDepartmentRepository, DepartmentRepository>();
        services.AddScoped<IEmployeeRepository, EmployeeRepository>();
        services.AddScoped<IExpenseRepository, ExpenseRepository>();
        services.AddScoped<ICapitalPartnerRepository, CapitalPartnerRepository>();
        services.AddScoped<ICostCenterRepository, CostCenterRepository>();
        services.AddScoped<ISupplierRepository, SupplierRepository>();
        services.AddScoped<IChinaContainerRepository, ChinaContainerRepository>();
        services.AddScoped<IFabricTypeAliasRepository, FabricTypeAliasRepository>();
        services.AddScoped<IFabricCatalogRepository, FabricCatalogRepository>();
        services.AddScoped<IWarehouseRepository, WarehouseRepository>();
        services.AddScoped<ISalesInvoiceRepository, SalesInvoiceRepository>();
        services.AddScoped<ISalesReturnRepository, SalesReturnRepository>();
        services.AddScoped<ITaxCodeRepository, TaxCodeRepository>();
        services.AddScoped<ISalesPostingProfileRepository, SalesPostingProfileRepository>();
        services.AddScoped<ISalesTaxReportRepository, SalesTaxReportRepository>();
        services.AddScoped<IReceiptInvoicePaymentRepository, ReceiptInvoicePaymentRepository>();
        services.AddScoped<IReceiptVoucherRepository, ReceiptVoucherRepository>();
        services.AddScoped<IPurchaseInvoiceRepository, PurchaseInvoiceRepository>();
        services.AddScoped<IPurchaseOrderRepository, PurchaseOrderRepository>();
        services.AddScoped<IPurchaseReturnRepository, PurchaseReturnRepository>();
        services.AddScoped<IPurchaseInvoicePaymentRepository, PurchaseInvoicePaymentRepository>();
        services.AddScoped<IPaymentMethodRepository, PaymentMethodRepository>();
        services.AddScoped<IBankAccountRepository, BankAccountRepository>();
        services.AddScoped<IPaymentVoucherRepository, PaymentVoucherRepository>();
        services.AddScoped<ICashboxRepository, CashboxRepository>();
        services.AddScoped<ICashboxTransferRepository, CashboxTransferRepository>();
        services.AddScoped<IJournalEntryRepository, JournalEntryRepository>();
        services.AddScoped<IAccountRepository, AccountRepository>();
        services.AddScoped<IJournalBookRepository, JournalBookRepository>();
        services.AddScoped<IAccountingReportRepository, AccountingReportRepository>();
        services.AddScoped<IAuditLogRepository, AuditLogRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IUserSessionRepository, UserSessionRepository>();
        services.AddScoped<IIdentityAdminRepository, IdentityAdminRepository>();
        services.AddScoped<IInventoryRepository, InventoryRepository>();
        services.AddScoped<IInventoryManagementRepository, InventoryManagementRepository>();
        services.AddScoped<IModuleReportRepository, ModuleReportRepository>();
        services.AddScoped<ISystemSettingsRepository, SystemSettingsRepository>();
        services.AddScoped<IBranchRepository, BranchRepository>();
        services.AddScoped<IOpeningBalanceRepository, OpeningBalanceRepository>();
        services.AddScoped<IOpeningBalanceLookupService, OpeningBalanceLookupService>();
        services.AddScoped<IOpeningBalanceEngine, OpeningBalanceEngine>();

        services.AddScoped<IInventoryEngine, InventoryEngine>();
        services.AddScoped<IInventoryOperationsService, InventoryOperationsService>();
        services.AddScoped<IPurchaseInventoryService, PurchaseInventoryService>();
        services.AddScoped<IIntegratedAccountingService, IntegratedAccountingService>();
        services.AddScoped<IPostingSaveCoordinator, PostingSaveCoordinator>();
        services.AddScoped<IAccountingPostingEngine, AccountingPostingEngine>();
        services.AddScoped<IAccountingIdempotencyService, AccountingIdempotencyService>();
        services.AddScoped<ICashboxPostingValidator, CashboxPostingValidator>();
        services.AddScoped<IBankAccountPostingValidator, BankAccountPostingValidator>();
        services.AddScoped<IReceiptTenderResolver, ReceiptTenderResolver>();
        services.AddScoped<IReceiptPostingService, ReceiptPostingService>();
        services.AddScoped<ICashboxBalanceService, CashboxBalanceService>();
        services.AddScoped<ICashboxReconciliationService, CashboxReconciliationService>();
        services.AddScoped<IAccountingBaselineReportService, AccountingBaselineReportService>();
        services.AddScoped<IAccountingHealthCheckService, AccountingHealthCheckService>();
        services.AddScoped<IContainerWarehouseImportService, ContainerWarehouseImportService>();
        services.AddScoped<IGlobalSearchService, GlobalSearchService>();

        services.AddScoped<INumberingService, PostgreSqlNumberingService>();
        services.AddSingleton<INotificationService, InMemoryNotificationService>();
        services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();
        services.AddSingleton<IDocumentPreviewService, NullDocumentPreviewService>();
        services.AddSingleton<IPasswordHasher, BcryptPasswordHasher>();

        return services;
    }

    public static async Task MigrateAndSeedAsync(this IServiceProvider serviceProvider, CancellationToken cancellationToken = default)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ErpDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<ErpDbContext>>();
        var configuration = scope.ServiceProvider.GetService<IConfiguration>();

        await StartupPhaseRecorder.RunAsync("Startup.Migrate",
            () => context.Database.MigrateAsync(cancellationToken));

        if (configuration?.GetValue<bool>("Development:CleanupImportCatalogOnStartup") == true)
        {
            await StartupPhaseRecorder.RunAsync("Startup.ImportCatalogCleanup",
                () => Seed.ImportCatalogDevelopmentCleanup.RunAsync(context, logger, cancellationToken));
        }

        await StartupPhaseRecorder.RunAsync("Startup.Seed",
            () => Seed.DatabaseSeeder.SeedAsync(
                context,
                logger,
                scope.ServiceProvider.GetRequiredService<IPasswordHasher>(),
                cancellationToken));

        await StartupPhaseRecorder.RunAsync("Startup.AccountingHealth",
            () => Services.AccountingHealth.ValidateAsync(context, cancellationToken));
    }

    public static async Task MigrateAsync(this IServiceProvider serviceProvider, CancellationToken cancellationToken = default)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ErpDbContext>();
        await context.Database.MigrateAsync(cancellationToken);
    }

    public static async Task SeedOnlyAsync(this IServiceProvider serviceProvider, CancellationToken cancellationToken = default)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ErpDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<ErpDbContext>>();
        await Seed.DatabaseSeeder.SeedAsync(
            context,
            logger,
            scope.ServiceProvider.GetRequiredService<IPasswordHasher>(),
            cancellationToken);
    }
}
