using ERPSystem.Infrastructure.Persistence;
using ERPSystem.Infrastructure.Persistence.Models.Parties;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ERPSystem.Infrastructure.Configurations;

internal sealed class CustomerConfiguration : IEntityTypeConfiguration<CustomerEntity>
{
    public void Configure(EntityTypeBuilder<CustomerEntity> builder)
    {
        builder.ToTable("customers", Schemas.Parties);
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Code).HasMaxLength(50).IsRequired();
        builder.Property(x => x.NameAr).HasMaxLength(200).IsRequired();
        builder.Property(x => x.NameEn).HasMaxLength(200).IsRequired();
        builder.Property(x => x.CreditLimit).HasPrecision(18, 2);
        builder.Property(x => x.Balance).HasPrecision(18, 2);
        builder.HasIndex(x => new { x.CompanyId, x.Code }).IsUnique();
        builder.HasQueryFilter(x => x.IsActive && !x.IsArchived);
    }
}

internal sealed class SupplierConfiguration : IEntityTypeConfiguration<SupplierEntity>
{
    public void Configure(EntityTypeBuilder<SupplierEntity> builder)
    {
        builder.ToTable("suppliers", Schemas.Parties);
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Code).HasMaxLength(50).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.NameAr).HasMaxLength(200).IsRequired();
        builder.Property(x => x.NameEn).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Balance).HasPrecision(18, 2);
        builder.Property(x => x.CreditLimit).HasPrecision(18, 2);
        builder.Property(x => x.CurrencyCode).HasMaxLength(10);
        builder.Property(x => x.Phone).HasMaxLength(50);
        builder.Property(x => x.Email).HasMaxLength(200);
        builder.Property(x => x.Address).HasMaxLength(500);
        builder.Property(x => x.Country).HasMaxLength(100);
        builder.Property(x => x.City).HasMaxLength(100);
        builder.Property(x => x.TaxNumber).HasMaxLength(50);
        builder.Property(x => x.Notes).HasMaxLength(2000);
        builder.HasIndex(x => new { x.CompanyId, x.Code }).IsUnique();
        builder.HasQueryFilter(x => x.IsActive && !x.IsArchived);
    }
}

internal sealed class ChinaSupplierConfiguration : IEntityTypeConfiguration<ChinaSupplierEntity>
{
    public void Configure(EntityTypeBuilder<ChinaSupplierEntity> builder)
    {
        builder.ToTable("china_suppliers", Schemas.Parties);
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => x.SupplierId).IsUnique();
    }
}
