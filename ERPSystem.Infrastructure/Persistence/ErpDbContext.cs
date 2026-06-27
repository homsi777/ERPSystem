using ERPSystem.Infrastructure.Persistence.Models.Accounting;
using ERPSystem.Infrastructure.Persistence.Models.Audit;
using ERPSystem.Infrastructure.Persistence.Models.Catalog;
using ERPSystem.Infrastructure.Persistence.Models.ChinaImport;
using ERPSystem.Infrastructure.Persistence.Models.Company;
using ERPSystem.Infrastructure.Persistence.Models.Documents;
using ERPSystem.Infrastructure.Persistence.Models.Finance;
using ERPSystem.Infrastructure.Persistence.Models.Hr;
using ERPSystem.Infrastructure.Persistence.Models.Identity;
using ERPSystem.Infrastructure.Persistence.Models.Inventory;
using ERPSystem.Infrastructure.Persistence.Models.Parties;
using ERPSystem.Infrastructure.Persistence.Models.Purchasing;
using ERPSystem.Infrastructure.Persistence.Models.Sales;
using ERPSystem.Infrastructure.Persistence.Models.Settings;
using Microsoft.EntityFrameworkCore;

namespace ERPSystem.Infrastructure.Persistence;

public sealed class ErpDbContext(DbContextOptions<ErpDbContext> options) : DbContext(options)
{
    // company
    public DbSet<CompanyEntity> Companies => Set<CompanyEntity>();
    public DbSet<BranchEntity> Branches => Set<BranchEntity>();

    // identity
    public DbSet<UserEntity> Users => Set<UserEntity>();
    public DbSet<RoleEntity> Roles => Set<RoleEntity>();
    public DbSet<PermissionEntity> Permissions => Set<PermissionEntity>();
    public DbSet<UserRoleEntity> UserRoles => Set<UserRoleEntity>();
    public DbSet<RolePermissionEntity> RolePermissions => Set<RolePermissionEntity>();

    // parties
    public DbSet<CustomerEntity> Customers => Set<CustomerEntity>();
    public DbSet<SupplierEntity> Suppliers => Set<SupplierEntity>();
    public DbSet<ChinaSupplierEntity> ChinaSuppliers => Set<ChinaSupplierEntity>();

    // catalog
    public DbSet<FabricCategoryEntity> FabricCategories => Set<FabricCategoryEntity>();
    public DbSet<FabricItemEntity> FabricItems => Set<FabricItemEntity>();
    public DbSet<FabricColorEntity> FabricColors => Set<FabricColorEntity>();
    public DbSet<FabricRollEntity> FabricRolls => Set<FabricRollEntity>();

    // china_import
    public DbSet<ContainerEntity> Containers => Set<ContainerEntity>();
    public DbSet<ContainerItemEntity> ContainerItems => Set<ContainerItemEntity>();
    public DbSet<LandingCostEntity> LandingCosts => Set<LandingCostEntity>();
    public DbSet<LandingCostExpenseEntity> LandingCostExpenses => Set<LandingCostExpenseEntity>();
    public DbSet<ImportBatchEntity> ImportBatches => Set<ImportBatchEntity>();
    public DbSet<ContainerDistributionEntity> ContainerDistributions => Set<ContainerDistributionEntity>();

    // inventory
    public DbSet<WarehouseEntity> Warehouses => Set<WarehouseEntity>();
    public DbSet<WarehouseLocationEntity> WarehouseLocations => Set<WarehouseLocationEntity>();
    public DbSet<WarehouseStockEntity> WarehouseStocks => Set<WarehouseStockEntity>();
    public DbSet<StockMovementEntity> StockMovements => Set<StockMovementEntity>();

    // sales
    public DbSet<SalesInvoiceEntity> SalesInvoices => Set<SalesInvoiceEntity>();
    public DbSet<SalesInvoiceItemEntity> SalesInvoiceItems => Set<SalesInvoiceItemEntity>();
    public DbSet<SalesInvoiceRollDetailEntity> SalesInvoiceRollDetails => Set<SalesInvoiceRollDetailEntity>();
    public DbSet<WarehouseDetailingSessionEntity> WarehouseDetailingSessions => Set<WarehouseDetailingSessionEntity>();

    // purchasing
    public DbSet<PurchaseInvoiceEntity> PurchaseInvoices => Set<PurchaseInvoiceEntity>();
    public DbSet<PurchaseInvoiceItemEntity> PurchaseInvoiceItems => Set<PurchaseInvoiceItemEntity>();

    // finance
    public DbSet<ReceiptVoucherEntity> ReceiptVouchers => Set<ReceiptVoucherEntity>();
    public DbSet<PaymentVoucherEntity> PaymentVouchers => Set<PaymentVoucherEntity>();
    public DbSet<CashboxEntity> Cashboxes => Set<CashboxEntity>();

    // accounting
    public DbSet<AccountEntity> Accounts => Set<AccountEntity>();
    public DbSet<JournalEntryEntity> JournalEntries => Set<JournalEntryEntity>();
    public DbSet<JournalEntryLineEntity> JournalEntryLines => Set<JournalEntryLineEntity>();

    // settings
    public DbSet<SystemSettingEntity> SystemSettings => Set<SystemSettingEntity>();
    public DbSet<DocumentTemplateEntity> DocumentTemplates => Set<DocumentTemplateEntity>();

    // audit
    public DbSet<AuditLogEntity> AuditLogs => Set<AuditLogEntity>();

    // documents
    public DbSet<DocumentCounterEntity> DocumentCounters => Set<DocumentCounterEntity>();

    // hr
    public DbSet<DepartmentEntity> Departments => Set<DepartmentEntity>();
    public DbSet<EmployeeEntity> Employees => Set<EmployeeEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ErpDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
