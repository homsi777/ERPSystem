using ERPSystem.Infrastructure.Persistence;
using ERPSystem.Infrastructure.Persistence.Models.Sales;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ERPSystem.Infrastructure.Configurations;

internal sealed class TaxCodeConfiguration : IEntityTypeConfiguration<TaxCodeEntity>
{
    public void Configure(EntityTypeBuilder<TaxCodeEntity> builder)
    {
        builder.ToTable("tax_codes", Schemas.Sales);
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Code).HasMaxLength(20).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Rate).HasPrecision(9, 6);
        builder.HasIndex(x => new { x.CompanyId, x.Code }).IsUnique();
    }
}

internal sealed class SalesPostingProfileConfiguration : IEntityTypeConfiguration<SalesPostingProfileEntity>
{
    public void Configure(EntityTypeBuilder<SalesPostingProfileEntity> builder)
    {
        builder.ToTable("sales_posting_profiles", Schemas.Sales);
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => x.CompanyId).IsUnique();
    }
}

internal sealed class SalesInvoiceItemTaxConfiguration : IEntityTypeConfiguration<SalesInvoiceItemTaxEntity>
{
    public void Configure(EntityTypeBuilder<SalesInvoiceItemTaxEntity> builder)
    {
        builder.ToTable("sales_invoice_item_taxes", Schemas.Sales);
        builder.HasKey(x => x.Id);
        builder.Property(x => x.TaxCode).HasMaxLength(20);
        builder.Property(x => x.TaxName).HasMaxLength(200);
        builder.Property(x => x.TaxRate).HasPrecision(9, 6);
        builder.Property(x => x.TaxableAmount).HasPrecision(18, 2);
        builder.Property(x => x.TaxAmount).HasPrecision(18, 2);
        builder.HasIndex(x => x.SalesInvoiceItemId).IsUnique();
        builder.HasIndex(x => x.SalesInvoiceId);
    }
}

internal sealed class SalesReturnLineTaxConfiguration : IEntityTypeConfiguration<SalesReturnLineTaxEntity>
{
    public void Configure(EntityTypeBuilder<SalesReturnLineTaxEntity> builder)
    {
        builder.ToTable("sales_return_line_taxes", Schemas.Sales);
        builder.HasKey(x => x.Id);
        builder.Property(x => x.TaxCode).HasMaxLength(20);
        builder.Property(x => x.TaxRate).HasPrecision(9, 6);
        builder.Property(x => x.TaxableAmount).HasPrecision(18, 2);
        builder.Property(x => x.TaxAmount).HasPrecision(18, 2);
        builder.HasIndex(x => x.SalesReturnLineId).IsUnique();
    }
}
