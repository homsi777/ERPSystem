using ERPSystem.Infrastructure.Persistence.Models.Accounting;
using ERPSystem.Infrastructure.Persistence.Models.Audit;
using ERPSystem.Infrastructure.Persistence.Models.Catalog;
using ERPSystem.Infrastructure.Persistence.Models.ChinaImport;
using ERPSystem.Infrastructure.Persistence.Models.Company;
using ERPSystem.Infrastructure.Persistence.Models.Documents;
using ERPSystem.Infrastructure.Persistence.Models.Capital;
using ERPSystem.Infrastructure.Persistence.Models.Expenses;
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
    public DbSet<ContainerFabricTypeLineEntity> ContainerFabricTypeLines => Set<ContainerFabricTypeLineEntity>();
    public DbSet<FabricTypeAliasEntity> FabricTypeAliases => Set<FabricTypeAliasEntity>();
    public DbSet<LandingCostEntity> LandingCosts => Set<LandingCostEntity>();
    public DbSet<LandingCostExpenseEntity> LandingCostExpenses => Set<LandingCostExpenseEntity>();
    public DbSet<ImportBatchEntity> ImportBatches => Set<ImportBatchEntity>();
    public DbSet<ContainerDistributionEntity> ContainerDistributions => Set<ContainerDistributionEntity>();

    // inventory
    public DbSet<WarehouseEntity> Warehouses => Set<WarehouseEntity>();
    public DbSet<WarehouseLocationEntity> WarehouseLocations => Set<WarehouseLocationEntity>();
    public DbSet<WarehouseStockEntity> WarehouseStocks => Set<WarehouseStockEntity>();
    public DbSet<StockMovementEntity> StockMovements => Set<StockMovementEntity>();
    public DbSet<StockMovementLineEntity> StockMovementLines => Set<StockMovementLineEntity>();
    public DbSet<FabricBatchEntity> FabricBatches => Set<FabricBatchEntity>();
    public DbSet<InventoryReservationEntity> InventoryReservations => Set<InventoryReservationEntity>();
    public DbSet<StockTransferDocumentEntity> StockTransfers => Set<StockTransferDocumentEntity>();
    public DbSet<StockTransferLineEntity> StockTransferLines => Set<StockTransferLineEntity>();
    public DbSet<StocktakeSessionEntity> StocktakeSessions => Set<StocktakeSessionEntity>();
    public DbSet<StocktakeLineEntity> StocktakeLines => Set<StocktakeLineEntity>();
    public DbSet<OpeningStockDocumentEntity> OpeningStockDocuments => Set<OpeningStockDocumentEntity>();
    public DbSet<OpeningStockLineEntity> OpeningStockLines => Set<OpeningStockLineEntity>();
    public DbSet<InventoryRuleEntity> InventoryRules => Set<InventoryRuleEntity>();
    public DbSet<InventoryAlertEntity> InventoryAlerts => Set<InventoryAlertEntity>();
    public DbSet<InventoryAuditEntryEntity> InventoryAuditLogs => Set<InventoryAuditEntryEntity>();
    public DbSet<InventoryTimelineEventEntity> InventoryTimelineEvents => Set<InventoryTimelineEventEntity>();
    public DbSet<InventoryValuationSnapshotEntity> InventoryValuationSnapshots => Set<InventoryValuationSnapshotEntity>();

    // sales
    public DbSet<SalesInvoiceEntity> SalesInvoices => Set<SalesInvoiceEntity>();
    public DbSet<SalesInvoiceItemEntity> SalesInvoiceItems => Set<SalesInvoiceItemEntity>();
    public DbSet<SalesInvoiceRollDetailEntity> SalesInvoiceRollDetails => Set<SalesInvoiceRollDetailEntity>();
    public DbSet<WarehouseDetailingSessionEntity> WarehouseDetailingSessions => Set<WarehouseDetailingSessionEntity>();
    public DbSet<SalesReturnEntity> SalesReturns => Set<SalesReturnEntity>();
    public DbSet<SalesReturnLineEntity> SalesReturnLines => Set<SalesReturnLineEntity>();
    public DbSet<ReceiptInvoicePaymentEntity> ReceiptInvoicePayments => Set<ReceiptInvoicePaymentEntity>();

    // purchasing
    public DbSet<PurchaseInvoiceEntity> PurchaseInvoices => Set<PurchaseInvoiceEntity>();
    public DbSet<PurchaseInvoiceItemEntity> PurchaseInvoiceItems => Set<PurchaseInvoiceItemEntity>();
    public DbSet<PurchaseOrderEntity> PurchaseOrders => Set<PurchaseOrderEntity>();
    public DbSet<PurchaseOrderLineEntity> PurchaseOrderLines => Set<PurchaseOrderLineEntity>();
    public DbSet<PurchaseReturnEntity> PurchaseReturns => Set<PurchaseReturnEntity>();
    public DbSet<PurchaseReturnLineEntity> PurchaseReturnLines => Set<PurchaseReturnLineEntity>();
    public DbSet<PurchaseInvoicePaymentEntity> PurchaseInvoicePayments => Set<PurchaseInvoicePaymentEntity>();

    // finance
    public DbSet<ReceiptVoucherEntity> ReceiptVouchers => Set<ReceiptVoucherEntity>();
    public DbSet<PaymentVoucherEntity> PaymentVouchers => Set<PaymentVoucherEntity>();
    public DbSet<CashboxEntity> Cashboxes => Set<CashboxEntity>();
    public DbSet<CashboxTransferEntity> CashboxTransfers => Set<CashboxTransferEntity>();
    public DbSet<CostCenterEntity> CostCenters => Set<CostCenterEntity>();
    public DbSet<OpeningBalanceDocumentEntity> OpeningBalanceDocuments => Set<OpeningBalanceDocumentEntity>();
    public DbSet<OpeningBalanceLineEntity> OpeningBalanceLines => Set<OpeningBalanceLineEntity>();
    public DbSet<OpeningBalanceEventEntity> OpeningBalanceEvents => Set<OpeningBalanceEventEntity>();

    // accounting
    public DbSet<AccountEntity> Accounts => Set<AccountEntity>();
    public DbSet<JournalEntryEntity> JournalEntries => Set<JournalEntryEntity>();
    public DbSet<JournalEntryLineEntity> JournalEntryLines => Set<JournalEntryLineEntity>();
    public DbSet<JournalBookEntity> JournalBooks => Set<JournalBookEntity>();

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

    // expenses
    public DbSet<ExpenseCategoryEntity> ExpenseCategories => Set<ExpenseCategoryEntity>();
    public DbSet<ExpenseEntity> Expenses => Set<ExpenseEntity>();
    public DbSet<ExpensePaymentEntity> ExpensePayments => Set<ExpensePaymentEntity>();
    public DbSet<ExpenseInstallmentEntity> ExpenseInstallments => Set<ExpenseInstallmentEntity>();
    public DbSet<ExpenseAttachmentEntity> ExpenseAttachments => Set<ExpenseAttachmentEntity>();
    public DbSet<ExpenseAuditLogEntity> ExpenseAuditLogs => Set<ExpenseAuditLogEntity>();
    public DbSet<ExpenseTimelineEventEntity> ExpenseTimelineEvents => Set<ExpenseTimelineEventEntity>();

    // capital
    public DbSet<CapitalPartnerEntity> CapitalPartners => Set<CapitalPartnerEntity>();
    public DbSet<PartnerParticipationEntity> PartnerParticipations => Set<PartnerParticipationEntity>();
    public DbSet<PartnerBankAccountEntity> PartnerBankAccounts => Set<PartnerBankAccountEntity>();
    public DbSet<CapitalTransactionEntity> CapitalTransactions => Set<CapitalTransactionEntity>();
    public DbSet<ProfitDistributionEntity> ProfitDistributions => Set<ProfitDistributionEntity>();
    public DbSet<ProfitDistributionLineEntity> ProfitDistributionLines => Set<ProfitDistributionLineEntity>();
    public DbSet<PartnerAuditLogEntity> PartnerAuditLogs => Set<PartnerAuditLogEntity>();
    public DbSet<PartnerTimelineEventEntity> PartnerTimelineEvents => Set<PartnerTimelineEventEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ErpDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }

    public override int SaveChanges()
    {
        UtcDateTimeNormalizer.NormalizeTrackedEntities(this);
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        UtcDateTimeNormalizer.NormalizeTrackedEntities(this);
        return base.SaveChangesAsync(cancellationToken);
    }
}
