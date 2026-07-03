using ERPSystem.Infrastructure.Persistence;
using ERPSystem.Infrastructure.Persistence.Models.Catalog;
using Microsoft.EntityFrameworkCore;

namespace ERPSystem.Infrastructure.Seed;

/// <summary>Idempotent fabric catalog entries required by real supplier packing lists.</summary>
public static class ChinaImportFabricCatalogSeeder
{
    private static readonly Guid CategoryId = Guid.Parse("77777777-7777-7777-7777-777777777777");

    private sealed record FabricSeed(string Code, string NameAr, (string Code, string NameAr)[] Colors);

    private static readonly FabricSeed[] PackingListFabrics =
    [
        new("1#", "كولومبيا", [("DEFAULT", "افتراضي")]),
        new("TR3663", "TR3663", [("DARK BLUE", "كحلي"), ("BLACK", "أسود")]),
        new("S30061", "S30061", [("BLUE BLACK", "أزرق أسود"), ("BLACK", "أسود")]),
        new("Q5775", "Q5775", [("DARK BLUE", "كحلي"), ("BLACK", "أسود")]),
        new("TR5419", "TR5419", [("DARK BLUE", "كحلي")]),
        new("42332", "42332", [("DARK BLUE", "كحلي")])
    ];

    public static async Task EnsureAsync(ErpDbContext context, Guid companyId, CancellationToken cancellationToken = default)
    {
        if (!await context.Companies.AnyAsync(c => c.Id == companyId, cancellationToken))
            return;

        if (!await context.FabricCategories.AnyAsync(c => c.Id == CategoryId, cancellationToken))
        {
            context.FabricCategories.Add(new FabricCategoryEntity
            {
                Id = CategoryId,
                CompanyId = companyId,
                Code = "FAB",
                NameAr = "أقمشة",
                NameEn = "Fabrics"
            });
        }

        foreach (var fabric in PackingListFabrics)
        {
            var item = await context.FabricItems
                .FirstOrDefaultAsync(i => i.CompanyId == companyId && i.Code == fabric.Code, cancellationToken);

            if (item is null)
            {
                item = new FabricItemEntity
                {
                    Id = CreateDeterministicId($"fabric:{fabric.Code}"),
                    CompanyId = companyId,
                    CategoryId = CategoryId,
                    Code = fabric.Code,
                    NameAr = fabric.NameAr,
                    NameEn = fabric.Code
                };
                context.FabricItems.Add(item);
            }

            foreach (var color in fabric.Colors)
            {
                var exists = await context.FabricColors.AnyAsync(
                    c => c.FabricItemId == item.Id && c.Code == color.Code,
                    cancellationToken);
                if (exists)
                    continue;

                context.FabricColors.Add(new FabricColorEntity
                {
                    Id = CreateDeterministicId($"color:{fabric.Code}:{color.Code}"),
                    FabricItemId = item.Id,
                    Code = color.Code,
                    NameAr = color.NameAr,
                    NameEn = color.Code
                });
            }
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    private static Guid CreateDeterministicId(string key)
    {
        var bytes = System.Security.Cryptography.MD5.HashData(System.Text.Encoding.UTF8.GetBytes(key));
        return new Guid(bytes);
    }
}
