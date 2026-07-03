using ERPSystem.Application.Abstractions.Repositories;
using ERPSystem.Application.DTOs.Containers;
using ERPSystem.Infrastructure.Persistence;
using ERPSystem.Infrastructure.Persistence.Models.ChinaImport;
using Microsoft.EntityFrameworkCore;

namespace ERPSystem.Infrastructure.Repositories;

internal sealed class FabricTypeAliasRepository(ErpDbContext context) : IFabricTypeAliasRepository
{
    public async Task<IReadOnlyList<FabricTypeAliasDto>> GetBySupplierAsync(
        Guid companyId,
        Guid supplierId,
        CancellationToken cancellationToken = default)
    {
        var rows = await context.FabricTypeAliases.AsNoTracking()
            .Where(a => a.CompanyId == companyId && a.SupplierId == supplierId && a.IsActive)
            .ToListAsync(cancellationToken);

        return rows.Select(ToDto).ToList();
    }

    public async Task UpsertAsync(
        Guid companyId,
        Guid supplierId,
        Guid fabricItemId,
        Guid fabricColorId,
        string dplMatchKey,
        string invoiceDescriptionMatchKey,
        string invoiceDescription,
        CancellationToken cancellationToken = default)
    {
        var existing = await context.FabricTypeAliases
            .FirstOrDefaultAsync(a =>
                a.CompanyId == companyId &&
                a.SupplierId == supplierId &&
                a.FabricItemId == fabricItemId &&
                a.FabricColorId == fabricColorId,
                cancellationToken);

        if (existing is null && !string.IsNullOrWhiteSpace(dplMatchKey))
        {
            existing = await context.FabricTypeAliases
                .FirstOrDefaultAsync(a =>
                    a.CompanyId == companyId &&
                    a.SupplierId == supplierId &&
                    a.DplMatchKey == dplMatchKey,
                    cancellationToken);
        }

        if (existing is null)
        {
            await context.FabricTypeAliases.AddAsync(new FabricTypeAliasEntity
            {
                Id = Guid.NewGuid(),
                CompanyId = companyId,
                SupplierId = supplierId,
                FabricItemId = fabricItemId,
                FabricColorId = fabricColorId,
                DplMatchKey = dplMatchKey,
                InvoiceDescriptionMatchKey = invoiceDescriptionMatchKey,
                InvoiceDescription = invoiceDescription,
                CreatedAt = DateTime.UtcNow
            }, cancellationToken);
            return;
        }

        existing.FabricItemId = fabricItemId != Guid.Empty ? fabricItemId : existing.FabricItemId;
        existing.FabricColorId = fabricColorId != Guid.Empty ? fabricColorId : existing.FabricColorId;
        existing.DplMatchKey = dplMatchKey;
        existing.InvoiceDescriptionMatchKey = invoiceDescriptionMatchKey;
        existing.InvoiceDescription = invoiceDescription;
        existing.UpdatedAt = DateTime.UtcNow;
    }

    private static FabricTypeAliasDto ToDto(FabricTypeAliasEntity e) => new()
    {
        Id = e.Id,
        SupplierId = e.SupplierId,
        FabricItemId = e.FabricItemId,
        FabricColorId = e.FabricColorId,
        DplMatchKey = e.DplMatchKey,
        InvoiceDescriptionMatchKey = e.InvoiceDescriptionMatchKey,
        InvoiceDescription = e.InvoiceDescription
    };
}
