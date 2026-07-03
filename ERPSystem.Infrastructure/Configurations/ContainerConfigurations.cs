using ERPSystem.Infrastructure.Persistence;
using ERPSystem.Infrastructure.Persistence.Models.ChinaImport;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ERPSystem.Infrastructure.Configurations;

internal sealed class ContainerConfiguration : IEntityTypeConfiguration<ContainerEntity>
{
    public void Configure(EntityTypeBuilder<ContainerEntity> builder)
    {
        builder.ToTable("containers", Schemas.ChinaImport);
        builder.HasKey(x => x.Id);
        builder.Property(x => x.ContainerNumber).HasMaxLength(50).IsRequired();
        builder.Property(x => x.TotalMeters).HasPrecision(18, 4);
        builder.Property(x => x.TotalWeightKg).HasPrecision(18, 4);
        builder.Property(x => x.ExchangeRateToLocalCurrency).HasPrecision(18, 6);
        builder.Property(x => x.ChinaInvoiceAmountUsd).HasPrecision(18, 2);
        builder.Property(x => x.FinancialTaxReservePostedLocal).HasPrecision(18, 2);
        builder.HasIndex(x => new { x.CompanyId, x.ContainerNumber }).IsUnique();
        builder.HasQueryFilter(x => x.IsActive && !x.IsArchived);
    }
}

internal sealed class ContainerItemConfiguration : IEntityTypeConfiguration<ContainerItemEntity>
{
    public void Configure(EntityTypeBuilder<ContainerItemEntity> builder)
    {
        builder.ToTable("container_items", Schemas.ChinaImport);
        builder.HasKey(x => x.Id);
        builder.Property(x => x.LengthMeters).HasPrecision(18, 4);
        builder.Property(x => x.LotCode).HasMaxLength(20);
        builder.HasIndex(x => new { x.ContainerId, x.LineNumber }).IsUnique();
    }
}

internal sealed class LandingCostConfiguration : IEntityTypeConfiguration<LandingCostEntity>
{
    public void Configure(EntityTypeBuilder<LandingCostEntity> builder)
    {
        builder.ToTable("landing_costs", Schemas.ChinaImport);
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => x.ContainerId).IsUnique();
        builder.Property(x => x.CustomsAmount).HasPrecision(18, 2);
        builder.Property(x => x.Shipping).HasPrecision(18, 2);
        builder.Property(x => x.Insurance).HasPrecision(18, 2);
        builder.Property(x => x.Clearance).HasPrecision(18, 2);
        builder.Property(x => x.OtherExpenses).HasPrecision(18, 2);
        builder.Property(x => x.OtherExpense1).HasPrecision(18, 2);
        builder.Property(x => x.OtherExpense2).HasPrecision(18, 2);
        builder.Property(x => x.OtherExpense3).HasPrecision(18, 2);
        builder.Property(x => x.OtherExpense4).HasPrecision(18, 2);
    }
}

internal sealed class ContainerFabricTypeLineConfiguration : IEntityTypeConfiguration<ContainerFabricTypeLineEntity>
{
    public void Configure(EntityTypeBuilder<ContainerFabricTypeLineEntity> builder)
    {
        builder.ToTable("container_fabric_type_lines", Schemas.ChinaImport);
        builder.HasKey(x => x.Id);
        builder.Property(x => x.TypeDisplayName).HasMaxLength(300).IsRequired();
        builder.Property(x => x.MatchKey).HasMaxLength(300).IsRequired();
        builder.Property(x => x.LengthMeters).HasPrecision(18, 4);
        builder.Property(x => x.NetWeightKg).HasPrecision(18, 4);
        builder.Property(x => x.Cbm).HasPrecision(18, 4);
        builder.Property(x => x.ChinaUnitPriceUsd).HasPrecision(18, 4);
        builder.Property(x => x.LandedCostPerMeterUsd).HasPrecision(18, 6);
        builder.Property(x => x.SalePricePerMeterUsd).HasPrecision(18, 6);
        builder.HasIndex(x => new { x.ContainerId, x.LineNumber }).IsUnique();
    }
}

internal sealed class FabricTypeAliasConfiguration : IEntityTypeConfiguration<FabricTypeAliasEntity>
{
    public void Configure(EntityTypeBuilder<FabricTypeAliasEntity> builder)
    {
        builder.ToTable("fabric_type_aliases", Schemas.ChinaImport);
        builder.HasKey(x => x.Id);
        builder.Property(x => x.DplMatchKey).HasMaxLength(300).IsRequired();
        builder.Property(x => x.InvoiceDescriptionMatchKey).HasMaxLength(300).IsRequired();
        builder.Property(x => x.InvoiceDescription).HasMaxLength(500).IsRequired();
        builder.HasIndex(x => new { x.CompanyId, x.SupplierId, x.DplMatchKey }).IsUnique();
    }
}
