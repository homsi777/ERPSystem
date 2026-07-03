using ERPSystem.Infrastructure.Persistence;
using ERPSystem.Infrastructure.Persistence.Models.Accounting;
using ERPSystem.Infrastructure.Persistence.Models.Audit;
using ERPSystem.Infrastructure.Persistence.Models.Catalog;
using ERPSystem.Infrastructure.Persistence.Models.Company;
using ERPSystem.Infrastructure.Persistence.Models.Documents;
using ERPSystem.Infrastructure.Persistence.Models.Finance;
using ERPSystem.Infrastructure.Persistence.Models.Hr;
using ERPSystem.Infrastructure.Persistence.Models.Identity;
using ERPSystem.Infrastructure.Persistence.Models.Inventory;
using ERPSystem.Infrastructure.Persistence.Models.Purchasing;
using ERPSystem.Infrastructure.Persistence.Models.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ERPSystem.Infrastructure.Configurations;

internal sealed class CompanyConfiguration : IEntityTypeConfiguration<CompanyEntity>
{
    public void Configure(EntityTypeBuilder<CompanyEntity> builder)
    {
        builder.ToTable("companies", Schemas.Company);
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Code).HasMaxLength(20).IsRequired();
        builder.HasIndex(x => x.Code).IsUnique();
    }
}

internal sealed class BranchConfiguration : IEntityTypeConfiguration<BranchEntity>
{
    public void Configure(EntityTypeBuilder<BranchEntity> builder)
    {
        builder.ToTable("branches", Schemas.Company);
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Code).HasMaxLength(20).IsRequired();
        builder.HasIndex(x => new { x.CompanyId, x.Code }).IsUnique();
    }
}

internal sealed class UserConfiguration : IEntityTypeConfiguration<UserEntity>
{
    public void Configure(EntityTypeBuilder<UserEntity> builder)
    {
        builder.ToTable("users", Schemas.Identity);
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Username).HasMaxLength(100).IsRequired();
        builder.HasIndex(x => x.Username).IsUnique();
    }
}

internal sealed class RoleConfiguration : IEntityTypeConfiguration<RoleEntity>
{
    public void Configure(EntityTypeBuilder<RoleEntity> builder)
    {
        builder.ToTable("roles", Schemas.Identity);
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(100).IsRequired();
        builder.HasIndex(x => x.Name).IsUnique();
    }
}

internal sealed class PermissionConfiguration : IEntityTypeConfiguration<PermissionEntity>
{
    public void Configure(EntityTypeBuilder<PermissionEntity> builder)
    {
        builder.ToTable("permissions", Schemas.Identity);
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Code).HasMaxLength(100).IsRequired();
        builder.HasIndex(x => x.Code).IsUnique();
    }
}

internal sealed class UserRoleConfiguration : IEntityTypeConfiguration<UserRoleEntity>
{
    public void Configure(EntityTypeBuilder<UserRoleEntity> builder)
    {
        builder.ToTable("user_roles", Schemas.Identity);
        builder.HasKey(x => new { x.UserId, x.RoleId });
    }
}

internal sealed class RolePermissionConfiguration : IEntityTypeConfiguration<RolePermissionEntity>
{
    public void Configure(EntityTypeBuilder<RolePermissionEntity> builder)
    {
        builder.ToTable("role_permissions", Schemas.Identity);
        builder.HasKey(x => new { x.RoleId, x.PermissionId });
    }
}

internal sealed class FabricCategoryConfiguration : IEntityTypeConfiguration<FabricCategoryEntity>
{
    public void Configure(EntityTypeBuilder<FabricCategoryEntity> builder)
    {
        builder.ToTable("fabric_categories", Schemas.Catalog);
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => new { x.CompanyId, x.Code }).IsUnique();
    }
}

internal sealed class FabricItemConfiguration : IEntityTypeConfiguration<FabricItemEntity>
{
    public void Configure(EntityTypeBuilder<FabricItemEntity> builder)
    {
        builder.ToTable("fabric_items", Schemas.Catalog);
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => new { x.CompanyId, x.Code }).IsUnique();
    }
}

internal sealed class FabricColorConfiguration : IEntityTypeConfiguration<FabricColorEntity>
{
    public void Configure(EntityTypeBuilder<FabricColorEntity> builder)
    {
        builder.ToTable("fabric_colors", Schemas.Catalog);
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => new { x.FabricItemId, x.Code }).IsUnique();
    }
}

internal sealed class WarehouseConfiguration : IEntityTypeConfiguration<WarehouseEntity>
{
    public void Configure(EntityTypeBuilder<WarehouseEntity> builder)
    {
        builder.ToTable("warehouses", Schemas.Inventory);
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => new { x.BranchId, x.Code }).IsUnique();
        builder.HasQueryFilter(x => x.IsActive && !x.IsArchived);
    }
}

internal sealed class WarehouseStockConfiguration : IEntityTypeConfiguration<WarehouseStockEntity>
{
    public void Configure(EntityTypeBuilder<WarehouseStockEntity> builder)
    {
        builder.ToTable("warehouse_stocks", Schemas.Inventory);
        builder.HasKey(x => x.Id);
        builder.Property(x => x.TotalMeters).HasPrecision(18, 4);
        builder.Property(x => x.ReservedMeters).HasPrecision(18, 4);
        builder.Property(x => x.AvailableMeters).HasPrecision(18, 4);
        builder.HasIndex(x => new { x.WarehouseId, x.FabricItemId, x.FabricColorId, x.ContainerId }).IsUnique();
    }
}

internal sealed class JournalEntryConfiguration : IEntityTypeConfiguration<JournalEntryEntity>
{
    public void Configure(EntityTypeBuilder<JournalEntryEntity> builder)
    {
        builder.ToTable("journal_entries", Schemas.Accounting);
        builder.HasKey(x => x.Id);
        builder.Property(x => x.EntryNumber).HasMaxLength(50).IsRequired();
        builder.HasIndex(x => new { x.CompanyId, x.EntryNumber }).IsUnique();
        builder.HasIndex(x => x.JournalBookId);
    }
}

internal sealed class JournalBookConfiguration : IEntityTypeConfiguration<JournalBookEntity>
{
    public void Configure(EntityTypeBuilder<JournalBookEntity> builder)
    {
        builder.ToTable("journal_books", Schemas.Accounting);
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => new { x.CompanyId, x.Code }).IsUnique();
    }
}

internal sealed class JournalEntryLineConfiguration : IEntityTypeConfiguration<JournalEntryLineEntity>
{
    public void Configure(EntityTypeBuilder<JournalEntryLineEntity> builder)
    {
        builder.ToTable("journal_entry_lines", Schemas.Accounting);
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Debit).HasPrecision(18, 2);
        builder.Property(x => x.Credit).HasPrecision(18, 2);
    }
}

internal sealed class ReceiptVoucherConfiguration : IEntityTypeConfiguration<ReceiptVoucherEntity>
{
    public void Configure(EntityTypeBuilder<ReceiptVoucherEntity> builder)
    {
        builder.ToTable("receipt_vouchers", Schemas.Finance);
        builder.HasKey(x => x.Id);
        builder.Property(x => x.VoucherNumber).HasMaxLength(50).IsRequired();
        builder.Property(x => x.Amount).HasPrecision(18, 2);
        builder.HasIndex(x => new { x.CompanyId, x.VoucherNumber }).IsUnique();
    }
}

internal sealed class PaymentVoucherConfiguration : IEntityTypeConfiguration<PaymentVoucherEntity>
{
    public void Configure(EntityTypeBuilder<PaymentVoucherEntity> builder)
    {
        builder.ToTable("payment_vouchers", Schemas.Finance);
        builder.HasKey(x => x.Id);
        builder.Property(x => x.VoucherNumber).HasMaxLength(50).IsRequired();
        builder.Property(x => x.Amount).HasPrecision(18, 2);
        builder.HasIndex(x => new { x.CompanyId, x.VoucherNumber }).IsUnique();
    }
}

internal sealed class CashboxConfiguration : IEntityTypeConfiguration<CashboxEntity>
{
    public void Configure(EntityTypeBuilder<CashboxEntity> builder)
    {
        builder.ToTable("cashboxes", Schemas.Finance);
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Balance).HasPrecision(18, 2);
        builder.HasIndex(x => new { x.BranchId, x.Code }).IsUnique();
    }
}

internal sealed class PurchaseInvoiceConfiguration : IEntityTypeConfiguration<PurchaseInvoiceEntity>
{
    public void Configure(EntityTypeBuilder<PurchaseInvoiceEntity> builder)
    {
        builder.ToTable("purchase_invoices", Schemas.Purchasing);
        builder.HasKey(x => x.Id);
        builder.Property(x => x.InvoiceNumber).HasMaxLength(50).IsRequired();
        builder.HasIndex(x => new { x.CompanyId, x.InvoiceNumber }).IsUnique();
    }
}

internal sealed class PurchaseInvoiceItemConfiguration : IEntityTypeConfiguration<PurchaseInvoiceItemEntity>
{
    public void Configure(EntityTypeBuilder<PurchaseInvoiceItemEntity> builder)
    {
        builder.ToTable("purchase_invoice_items", Schemas.Purchasing);
        builder.HasKey(x => x.Id);
    }
}

internal sealed class PurchaseOrderConfiguration : IEntityTypeConfiguration<PurchaseOrderEntity>
{
    public void Configure(EntityTypeBuilder<PurchaseOrderEntity> builder)
    {
        builder.ToTable("purchase_orders", Schemas.Purchasing);
        builder.HasKey(x => x.Id);
        builder.Property(x => x.OrderNumber).HasMaxLength(50).IsRequired();
        builder.HasIndex(x => new { x.CompanyId, x.OrderNumber }).IsUnique();
    }
}

internal sealed class PurchaseOrderLineConfiguration : IEntityTypeConfiguration<PurchaseOrderLineEntity>
{
    public void Configure(EntityTypeBuilder<PurchaseOrderLineEntity> builder)
    {
        builder.ToTable("purchase_order_lines", Schemas.Purchasing);
        builder.HasKey(x => x.Id);
    }
}

internal sealed class PurchaseReturnConfiguration : IEntityTypeConfiguration<PurchaseReturnEntity>
{
    public void Configure(EntityTypeBuilder<PurchaseReturnEntity> builder)
    {
        builder.ToTable("purchase_returns", Schemas.Purchasing);
        builder.HasKey(x => x.Id);
        builder.Property(x => x.ReturnNumber).HasMaxLength(50).IsRequired();
        builder.HasIndex(x => new { x.CompanyId, x.ReturnNumber }).IsUnique();
    }
}

internal sealed class PurchaseReturnLineConfiguration : IEntityTypeConfiguration<PurchaseReturnLineEntity>
{
    public void Configure(EntityTypeBuilder<PurchaseReturnLineEntity> builder)
    {
        builder.ToTable("purchase_return_lines", Schemas.Purchasing);
        builder.HasKey(x => x.Id);
    }
}

internal sealed class PurchaseInvoicePaymentConfiguration : IEntityTypeConfiguration<PurchaseInvoicePaymentEntity>
{
    public void Configure(EntityTypeBuilder<PurchaseInvoicePaymentEntity> builder)
    {
        builder.ToTable("purchase_invoice_payments", Schemas.Purchasing);
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => x.PurchaseInvoiceId);
    }
}

internal sealed class AuditLogConfiguration : IEntityTypeConfiguration<AuditLogEntity>
{
    public void Configure(EntityTypeBuilder<AuditLogEntity> builder)
    {
        builder.ToTable("audit_logs", Schemas.Audit);
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Action).HasMaxLength(100).IsRequired();
        builder.Property(x => x.EntityType).HasMaxLength(100).IsRequired();
        builder.HasIndex(x => new { x.EntityType, x.EntityId });
        builder.HasIndex(x => x.OccurredAt);
    }
}

internal sealed class DocumentCounterConfiguration : IEntityTypeConfiguration<DocumentCounterEntity>
{
    public void Configure(EntityTypeBuilder<DocumentCounterEntity> builder)
    {
        builder.ToTable("document_counters", Schemas.Documents);
        builder.HasKey(x => x.Id);
        builder.Property(x => x.DocumentType).HasMaxLength(50).IsRequired();
        builder.Property(x => x.Prefix).HasMaxLength(20).IsRequired();
        builder.Property(x => x.RowVersion).IsConcurrencyToken();
        builder.HasIndex(x => new { x.BranchId, x.DocumentType }).IsUnique();
    }
}

internal sealed class SystemSettingConfiguration : IEntityTypeConfiguration<SystemSettingEntity>
{
    public void Configure(EntityTypeBuilder<SystemSettingEntity> builder)
    {
        builder.ToTable("system_settings", Schemas.Settings);
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Key).HasMaxLength(100).IsRequired();
        builder.HasIndex(x => new { x.CompanyId, x.BranchId, x.Key }).IsUnique();
    }
}

internal sealed class DepartmentConfiguration : IEntityTypeConfiguration<DepartmentEntity>
{
    public void Configure(EntityTypeBuilder<DepartmentEntity> builder)
    {
        builder.ToTable("departments", Schemas.Hr);
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => new { x.CompanyId, x.Code }).IsUnique();
    }
}
