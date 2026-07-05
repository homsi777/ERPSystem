using ERPSystem.Application.Abstractions.Services;
using ERPSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ERPSystem.Infrastructure.Services;

/// <summary>
/// Minimal cross-entity search over customers, suppliers, sales/purchase
/// invoices, China containers and the chart of accounts using PostgreSQL ILIKE.
/// </summary>
internal sealed class GlobalSearchService(ErpDbContext context) : IGlobalSearchService
{
    public async Task<IReadOnlyList<GlobalSearchResult>> SearchAsync(
        string query,
        int limit = 20,
        CancellationToken cancellationToken = default)
    {
        var term = query?.Trim() ?? "";
        if (term.Length < 2)
            return [];

        var pattern = $"%{term}%";
        var perType = Math.Max(3, limit / 4);
        var results = new List<GlobalSearchResult>();

        var customers = await context.Customers.AsNoTracking()
            .Where(c => EF.Functions.ILike(c.NameAr, pattern) || EF.Functions.ILike(c.Code, pattern))
            .OrderBy(c => c.NameAr)
            .Take(perType)
            .Select(c => new { c.Id, c.NameAr, c.Code })
            .ToListAsync(cancellationToken);
        results.AddRange(customers.Select(c =>
            new GlobalSearchResult("Customer", c.Id, c.NameAr, $"عميل — {c.Code}", "\uE77B")));

        var suppliers = await context.Suppliers.AsNoTracking()
            .Where(s => EF.Functions.ILike(s.NameAr, pattern) || EF.Functions.ILike(s.Code, pattern))
            .OrderBy(s => s.NameAr)
            .Take(perType)
            .Select(s => new { s.Id, s.NameAr, s.Code })
            .ToListAsync(cancellationToken);
        results.AddRange(suppliers.Select(s =>
            new GlobalSearchResult("Supplier", s.Id, s.NameAr, $"مورد — {s.Code}", "\uE7EE")));

        var salesInvoices = await context.SalesInvoices.AsNoTracking()
            .Where(i => EF.Functions.ILike(i.InvoiceNumber, pattern))
            .OrderByDescending(i => i.InvoiceDate)
            .Take(perType)
            .Select(i => new { i.Id, i.InvoiceNumber })
            .ToListAsync(cancellationToken);
        results.AddRange(salesInvoices.Select(i =>
            new GlobalSearchResult("SalesInvoice", i.Id, i.InvoiceNumber, "فاتورة بيع", "\uE9F9")));

        var purchaseInvoices = await context.PurchaseInvoices.AsNoTracking()
            .Where(i => EF.Functions.ILike(i.InvoiceNumber, pattern))
            .OrderByDescending(i => i.InvoiceDate)
            .Take(perType)
            .Select(i => new { i.Id, i.InvoiceNumber })
            .ToListAsync(cancellationToken);
        results.AddRange(purchaseInvoices.Select(i =>
            new GlobalSearchResult("PurchaseInvoice", i.Id, i.InvoiceNumber, "فاتورة شراء", "\uE7BF")));

        var containers = await context.Containers.AsNoTracking()
            .Where(c => EF.Functions.ILike(c.ContainerNumber, pattern))
            .OrderByDescending(c => c.ShipmentDate)
            .Take(perType)
            .Select(c => new { c.Id, c.ContainerNumber })
            .ToListAsync(cancellationToken);
        results.AddRange(containers.Select(c =>
            new GlobalSearchResult("Container", c.Id, c.ContainerNumber, "حاوية استيراد", "\uE7B8")));

        var accounts = await context.Accounts.AsNoTracking()
            .Where(a => EF.Functions.ILike(a.NameAr, pattern) || EF.Functions.ILike(a.Code, pattern))
            .OrderBy(a => a.Code)
            .Take(perType)
            .Select(a => new { a.Id, a.NameAr, a.Code })
            .ToListAsync(cancellationToken);
        results.AddRange(accounts.Select(a =>
            new GlobalSearchResult("Account", a.Id, $"{a.Code} — {a.NameAr}", "حساب", "\uE8C7")));

        return results.Take(limit).ToList();
    }
}
