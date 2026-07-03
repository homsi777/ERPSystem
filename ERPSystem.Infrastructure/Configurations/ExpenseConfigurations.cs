using ERPSystem.Infrastructure.Persistence;
using ERPSystem.Infrastructure.Persistence.Models.Expenses;
using ERPSystem.Infrastructure.Persistence.Models.Finance;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ERPSystem.Infrastructure.Configurations;

internal sealed class CostCenterConfiguration : IEntityTypeConfiguration<CostCenterEntity>
{
    public void Configure(EntityTypeBuilder<CostCenterEntity> builder)
    {
        builder.ToTable("cost_centers", Schemas.Finance);
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Code).HasMaxLength(32).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.HasIndex(x => new { x.CompanyId, x.Code }).IsUnique();
        builder.HasOne(x => x.Parent)
            .WithMany()
            .HasForeignKey(x => x.ParentCostCenterId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class ExpenseCategoryConfiguration : IEntityTypeConfiguration<ExpenseCategoryEntity>
{
    public void Configure(EntityTypeBuilder<ExpenseCategoryEntity> builder)
    {
        builder.ToTable("expense_categories", "expenses");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Code).HasMaxLength(32).IsRequired();
        builder.Property(x => x.NameAr).HasMaxLength(200).IsRequired();
        builder.Property(x => x.NameEn).HasMaxLength(200).IsRequired();
        builder.HasIndex(x => new { x.CompanyId, x.Code }).IsUnique();
    }
}

internal sealed class ExpenseConfiguration : IEntityTypeConfiguration<ExpenseEntity>
{
    public void Configure(EntityTypeBuilder<ExpenseEntity> builder)
    {
        builder.ToTable("expenses", "expenses");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Code).HasMaxLength(40).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(300).IsRequired();
        builder.Property(x => x.OriginalCurrency).HasMaxLength(8).IsRequired();
        builder.Property(x => x.BaseCurrency).HasMaxLength(8).IsRequired();
        builder.Property(x => x.OriginalAmount).HasPrecision(18, 4);
        builder.Property(x => x.BaseAmount).HasPrecision(18, 4);
        builder.Property(x => x.ExchangeRate).HasPrecision(18, 6);
        builder.Property(x => x.Department).HasMaxLength(100);
        builder.Property(x => x.ProjectCode).HasMaxLength(50);
        builder.Property(x => x.PayeeName).HasMaxLength(200);
        builder.HasIndex(x => new { x.CompanyId, x.Code }).IsUnique();
        builder.HasIndex(x => x.CompanyId);
        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => x.CategoryKind);
        builder.HasIndex(x => x.NextDueDate);
        builder.HasIndex(x => x.CostCenterId);
        builder.HasOne(x => x.CostCenter).WithMany().HasForeignKey(x => x.CostCenterId);
        builder.HasMany(x => x.Payments).WithOne(x => x.Expense).HasForeignKey(x => x.ExpenseId);
        builder.HasMany(x => x.Attachments).WithOne(x => x.Expense).HasForeignKey(x => x.ExpenseId);
        builder.HasMany(x => x.Installments).WithOne(x => x.Expense).HasForeignKey(x => x.ExpenseId);
    }
}

internal sealed class ExpensePaymentConfiguration : IEntityTypeConfiguration<ExpensePaymentEntity>
{
    public void Configure(EntityTypeBuilder<ExpensePaymentEntity> builder)
    {
        builder.ToTable("expense_payments", "expenses");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Currency).HasMaxLength(8).IsRequired();
        builder.Property(x => x.AmountOriginal).HasPrecision(18, 4);
        builder.Property(x => x.AmountBase).HasPrecision(18, 4);
        builder.Property(x => x.ExchangeRateSnapshot).HasPrecision(18, 6);
        builder.Property(x => x.ReferenceNumber).HasMaxLength(100);
        builder.HasIndex(x => x.DueDate);
        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => x.CashboxId);
    }
}

internal sealed class ExpenseInstallmentConfiguration : IEntityTypeConfiguration<ExpenseInstallmentEntity>
{
    public void Configure(EntityTypeBuilder<ExpenseInstallmentEntity> builder)
    {
        builder.ToTable("expense_installments", "expenses");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Currency).HasMaxLength(8).IsRequired();
        builder.Property(x => x.AmountOriginal).HasPrecision(18, 4);
        builder.Property(x => x.AmountBase).HasPrecision(18, 4);
        builder.HasIndex(x => x.DueDate);
        builder.HasIndex(x => x.Status);
    }
}

internal sealed class ExpenseAttachmentConfiguration : IEntityTypeConfiguration<ExpenseAttachmentEntity>
{
    public void Configure(EntityTypeBuilder<ExpenseAttachmentEntity> builder)
    {
        builder.ToTable("expense_attachments", "expenses");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.FileName).HasMaxLength(260).IsRequired();
        builder.Property(x => x.StoragePath).HasMaxLength(500).IsRequired();
        builder.Property(x => x.ContentType).HasMaxLength(120).IsRequired();
    }
}

internal sealed class ExpenseAuditLogConfiguration : IEntityTypeConfiguration<ExpenseAuditLogEntity>
{
    public void Configure(EntityTypeBuilder<ExpenseAuditLogEntity> builder)
    {
        builder.ToTable("expense_audit_logs", "expenses");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Action).HasMaxLength(80).IsRequired();
        builder.Property(x => x.FieldName).HasMaxLength(80);
        builder.Property(x => x.UserName).HasMaxLength(120).IsRequired();
        builder.HasIndex(x => x.ExpenseId);
        builder.HasIndex(x => x.Timestamp);
    }
}

internal sealed class ExpenseTimelineEventConfiguration : IEntityTypeConfiguration<ExpenseTimelineEventEntity>
{
    public void Configure(EntityTypeBuilder<ExpenseTimelineEventEntity> builder)
    {
        builder.ToTable("expense_timeline_events", "expenses");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.EventType).HasMaxLength(80).IsRequired();
        builder.Property(x => x.Title).HasMaxLength(200).IsRequired();
        builder.Property(x => x.UserName).HasMaxLength(120).IsRequired();
        builder.HasIndex(x => x.ExpenseId);
        builder.HasIndex(x => x.Timestamp);
    }
}
