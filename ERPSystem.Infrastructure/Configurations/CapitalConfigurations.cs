using ERPSystem.Infrastructure.Persistence.Models.Capital;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ERPSystem.Infrastructure.Configurations;

internal sealed class CapitalPartnerConfiguration : IEntityTypeConfiguration<CapitalPartnerEntity>
{
    public void Configure(EntityTypeBuilder<CapitalPartnerEntity> builder)
    {
        builder.ToTable("partners", "capital");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Code).HasMaxLength(40).IsRequired();
        builder.Property(x => x.FullName).HasMaxLength(300).IsRequired();
        builder.Property(x => x.DefaultCurrency).HasMaxLength(8).IsRequired();
        builder.HasIndex(x => new { x.CompanyId, x.Code }).IsUnique();
        builder.HasIndex(x => x.CompanyId);
        builder.HasIndex(x => x.Status);
        builder.HasMany(x => x.Participations).WithOne(x => x.Partner).HasForeignKey(x => x.PartnerId);
        builder.HasMany(x => x.BankAccounts).WithOne(x => x.Partner).HasForeignKey(x => x.PartnerId);
        builder.HasMany(x => x.Transactions).WithOne(x => x.Partner).HasForeignKey(x => x.PartnerId);
    }
}

internal sealed class PartnerParticipationConfiguration : IEntityTypeConfiguration<PartnerParticipationEntity>
{
    public void Configure(EntityTypeBuilder<PartnerParticipationEntity> builder)
    {
        builder.ToTable("partner_participations", "capital");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.OwnershipPercentage).HasPrecision(18, 4);
        builder.Property(x => x.ProjectCode).HasMaxLength(50);
        builder.Property(x => x.ContainerNumber).HasMaxLength(50);
        builder.HasIndex(x => x.PartnerId);
        builder.HasIndex(x => x.Scope);
    }
}

internal sealed class PartnerBankAccountConfiguration : IEntityTypeConfiguration<PartnerBankAccountEntity>
{
    public void Configure(EntityTypeBuilder<PartnerBankAccountEntity> builder)
    {
        builder.ToTable("partner_bank_accounts", "capital");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.BankName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.AccountNumber).HasMaxLength(50).IsRequired();
        builder.Property(x => x.Iban).HasMaxLength(50);
        builder.Property(x => x.Currency).HasMaxLength(8).IsRequired();
    }
}

internal sealed class CapitalTransactionConfiguration : IEntityTypeConfiguration<CapitalTransactionEntity>
{
    public void Configure(EntityTypeBuilder<CapitalTransactionEntity> builder)
    {
        builder.ToTable("capital_transactions", "capital");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.AmountOriginal).HasPrecision(18, 4);
        builder.Property(x => x.AmountBase).HasPrecision(18, 4);
        builder.Property(x => x.ExchangeRate).HasPrecision(18, 6);
        builder.Property(x => x.Currency).HasMaxLength(8).IsRequired();
        builder.Property(x => x.BaseCurrency).HasMaxLength(8).IsRequired();
        builder.Property(x => x.ProjectCode).HasMaxLength(50);
        builder.Property(x => x.ReferenceNumber).HasMaxLength(100);
        builder.HasIndex(x => x.PartnerId);
        builder.HasIndex(x => x.TransactionDate);
        builder.HasIndex(x => x.Scope);
    }
}

internal sealed class ProfitDistributionConfiguration : IEntityTypeConfiguration<ProfitDistributionEntity>
{
    public void Configure(EntityTypeBuilder<ProfitDistributionEntity> builder)
    {
        builder.ToTable("profit_distributions", "capital");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Code).HasMaxLength(40).IsRequired();
        builder.Property(x => x.GrossRevenue).HasPrecision(18, 4);
        builder.Property(x => x.TotalCosts).HasPrecision(18, 4);
        builder.Property(x => x.NetProfit).HasPrecision(18, 4);
        builder.Property(x => x.NetLoss).HasPrecision(18, 4);
        builder.Property(x => x.BaseCurrency).HasMaxLength(8).IsRequired();
        builder.Property(x => x.ProjectCode).HasMaxLength(50);
        builder.HasIndex(x => new { x.CompanyId, x.Code }).IsUnique();
        builder.HasIndex(x => x.Status);
        builder.HasMany(x => x.Lines).WithOne(x => x.Distribution).HasForeignKey(x => x.DistributionId);
    }
}

internal sealed class ProfitDistributionLineConfiguration : IEntityTypeConfiguration<ProfitDistributionLineEntity>
{
    public void Configure(EntityTypeBuilder<ProfitDistributionLineEntity> builder)
    {
        builder.ToTable("profit_distribution_lines", "capital");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.OwnershipPercentage).HasPrecision(18, 4);
        builder.Property(x => x.PartnerShare).HasPrecision(18, 4);
        builder.Property(x => x.CompanyShare).HasPrecision(18, 4);
        builder.HasIndex(x => x.DistributionId);
        builder.HasIndex(x => x.PartnerId);
    }
}

internal sealed class PartnerAuditLogConfiguration : IEntityTypeConfiguration<PartnerAuditLogEntity>
{
    public void Configure(EntityTypeBuilder<PartnerAuditLogEntity> builder)
    {
        builder.ToTable("partner_audit_logs", "capital");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Action).HasMaxLength(100).IsRequired();
        builder.Property(x => x.UserName).HasMaxLength(200).IsRequired();
        builder.HasIndex(x => x.PartnerId);
        builder.HasIndex(x => x.Timestamp);
    }
}

internal sealed class PartnerTimelineEventConfiguration : IEntityTypeConfiguration<PartnerTimelineEventEntity>
{
    public void Configure(EntityTypeBuilder<PartnerTimelineEventEntity> builder)
    {
        builder.ToTable("partner_timeline_events", "capital");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.EventType).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Title).HasMaxLength(300).IsRequired();
        builder.Property(x => x.UserName).HasMaxLength(200).IsRequired();
        builder.HasIndex(x => x.PartnerId);
        builder.HasIndex(x => x.Timestamp);
    }
}
