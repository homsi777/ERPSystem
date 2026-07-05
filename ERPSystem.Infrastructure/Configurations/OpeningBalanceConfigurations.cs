using ERPSystem.Infrastructure.Persistence;
using ERPSystem.Infrastructure.Persistence.Models.Finance;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ERPSystem.Infrastructure.Configurations;

internal sealed class OpeningBalanceDocumentConfiguration : IEntityTypeConfiguration<OpeningBalanceDocumentEntity>
{
    public void Configure(EntityTypeBuilder<OpeningBalanceDocumentEntity> builder)
    {
        builder.ToTable("opening_balance_documents", Schemas.Finance);
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Number).HasMaxLength(40).IsRequired();
        builder.Property(x => x.CurrencyCode).HasMaxLength(8).IsRequired();
        builder.Property(x => x.ExchangeRate).HasPrecision(18, 6);
        builder.Property(x => x.TotalDebit).HasPrecision(18, 4);
        builder.Property(x => x.TotalCredit).HasPrecision(18, 4);
        builder.Property(x => x.TotalBaseAmount).HasPrecision(18, 4);
        builder.HasIndex(x => new { x.CompanyId, x.Number }).IsUnique();
        builder.HasIndex(x => new { x.CompanyId, x.Type, x.Status });
        builder.HasIndex(x => x.OpeningDate);
        builder.HasMany(x => x.Lines).WithOne(x => x.Document).HasForeignKey(x => x.DocumentId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(x => x.Events).WithOne(x => x.Document).HasForeignKey(x => x.DocumentId).OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class OpeningBalanceLineConfiguration : IEntityTypeConfiguration<OpeningBalanceLineEntity>
{
    public void Configure(EntityTypeBuilder<OpeningBalanceLineEntity> builder)
    {
        builder.ToTable("opening_balance_lines", Schemas.Finance);
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Debit).HasPrecision(18, 4);
        builder.Property(x => x.Credit).HasPrecision(18, 4);
        builder.Property(x => x.RollCount).HasPrecision(18, 4);
        builder.Property(x => x.Quantity).HasPrecision(18, 4);
        builder.Property(x => x.UnitCost).HasPrecision(18, 4);
        builder.HasIndex(x => new { x.DocumentId, x.LineNumber }).IsUnique();
        builder.HasIndex(x => x.PartyId);
        builder.HasIndex(x => x.AccountId);
        builder.HasIndex(x => x.WarehouseId);
    }
}

internal sealed class OpeningBalanceEventConfiguration : IEntityTypeConfiguration<OpeningBalanceEventEntity>
{
    public void Configure(EntityTypeBuilder<OpeningBalanceEventEntity> builder)
    {
        builder.ToTable("opening_balance_events", Schemas.Finance);
        builder.HasKey(x => x.Id);
        builder.Property(x => x.UserName).HasMaxLength(120).IsRequired();
        builder.Property(x => x.Action).HasMaxLength(80).IsRequired();
        builder.HasIndex(x => new { x.DocumentId, x.OccurredAt });
    }
}
