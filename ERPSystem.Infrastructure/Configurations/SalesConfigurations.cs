using ERPSystem.Infrastructure.Persistence;
using ERPSystem.Infrastructure.Persistence.Models.Sales;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ERPSystem.Infrastructure.Configurations;

internal sealed class SalesInvoiceConfiguration : IEntityTypeConfiguration<SalesInvoiceEntity>
{
    public void Configure(EntityTypeBuilder<SalesInvoiceEntity> builder)
    {
        builder.ToTable("sales_invoices", Schemas.Sales);
        builder.HasKey(x => x.Id);
        builder.Property(x => x.InvoiceNumber).HasMaxLength(50).IsRequired();
        builder.Property(x => x.SubTotal).HasPrecision(18, 2);
        builder.Property(x => x.DiscountTotal).HasPrecision(18, 2);
        builder.Property(x => x.TaxTotal).HasPrecision(18, 2);
        builder.Property(x => x.GrandTotal).HasPrecision(18, 2);
        builder.HasIndex(x => new { x.CompanyId, x.InvoiceNumber }).IsUnique();
        builder.HasQueryFilter(x => x.IsActive && !x.IsArchived);
    }
}

internal sealed class SalesInvoiceItemConfiguration : IEntityTypeConfiguration<SalesInvoiceItemEntity>
{
    public void Configure(EntityTypeBuilder<SalesInvoiceItemEntity> builder)
    {
        builder.ToTable("sales_invoice_items", Schemas.Sales);
        builder.HasKey(x => x.Id);
        builder.Property(x => x.UnitPrice).HasPrecision(18, 2);
        builder.Property(x => x.LineTotal).HasPrecision(18, 2);
        builder.HasIndex(x => new { x.SalesInvoiceId, x.LineNumber }).IsUnique();
    }
}

internal sealed class SalesInvoiceRollDetailConfiguration : IEntityTypeConfiguration<SalesInvoiceRollDetailEntity>
{
    public void Configure(EntityTypeBuilder<SalesInvoiceRollDetailEntity> builder)
    {
        builder.ToTable("sales_invoice_roll_details", Schemas.Sales);
        builder.HasKey(x => x.Id);
        builder.Property(x => x.LengthMeters).HasPrecision(18, 4);
        builder.HasIndex(x => new { x.SalesInvoiceItemId, x.RollSequence }).IsUnique();
    }
}

internal sealed class WarehouseDetailingSessionConfiguration : IEntityTypeConfiguration<WarehouseDetailingSessionEntity>
{
    public void Configure(EntityTypeBuilder<WarehouseDetailingSessionEntity> builder)
    {
        builder.ToTable("warehouse_detailing_sessions", Schemas.Sales);
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => x.SalesInvoiceId).IsUnique();
    }
}
