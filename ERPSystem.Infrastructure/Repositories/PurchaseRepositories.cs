using ERPSystem.Application.Abstractions.Repositories;
using ERPSystem.Domain.Entities.Purchasing;
using ERPSystem.Domain.Enums;
using ERPSystem.Domain.ValueObjects;
using ERPSystem.Infrastructure.Persistence;
using ERPSystem.Infrastructure.Persistence.Mapping;
using ERPSystem.Infrastructure.Persistence.Models.Purchasing;
using Microsoft.EntityFrameworkCore;

namespace ERPSystem.Infrastructure.Repositories;

internal sealed class PurchaseInvoiceRepository(ErpDbContext context) : IPurchaseInvoiceRepository
{
    public async Task<PurchaseInvoice?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var header = await context.PurchaseInvoices.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        if (header is null) return null;
        var items = await context.PurchaseInvoiceItems.AsNoTracking()
            .Where(i => i.PurchaseInvoiceId == id).ToListAsync(cancellationToken);
        return PurchaseMapper.ToDomain(header, items);
    }

    public async Task<PurchaseInvoice?> GetByNumberAsync(Guid companyId, string invoiceNumber, CancellationToken cancellationToken = default)
    {
        var header = await context.PurchaseInvoices.AsNoTracking()
            .FirstOrDefaultAsync(p => p.CompanyId == companyId && p.InvoiceNumber == invoiceNumber, cancellationToken);
        if (header is null) return null;
        var items = await context.PurchaseInvoiceItems.AsNoTracking()
            .Where(i => i.PurchaseInvoiceId == header.Id).ToListAsync(cancellationToken);
        return PurchaseMapper.ToDomain(header, items);
    }

    public async Task<(IReadOnlyList<PurchaseInvoice> Items, int TotalCount)> GetPagedAsync(
        Guid companyId,
        string? search = null,
        PurchaseInvoiceStatus? status = null,
        Guid? supplierId = null,
        CancellationToken cancellationToken = default)
    {
        var query = context.PurchaseInvoices.AsNoTracking().Where(p => p.CompanyId == companyId && !p.IsArchived);
        if (status.HasValue)
            query = query.Where(p => p.Status == (int)status.Value);
        if (supplierId.HasValue)
            query = query.Where(p => p.SupplierId == supplierId.Value);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            var supplierIds = await context.Suppliers.AsNoTracking()
                .Where(s => s.CompanyId == companyId &&
                            (s.NameAr.Contains(term) || s.Code.Contains(term)))
                .Select(s => s.Id).ToListAsync(cancellationToken);
            query = query.Where(p =>
                p.InvoiceNumber.Contains(term) ||
                (p.SupplierReference != null && p.SupplierReference.Contains(term)) ||
                supplierIds.Contains(p.SupplierId));
        }

        var total = await query.CountAsync(cancellationToken);
        var headers = await query.OrderByDescending(p => p.InvoiceDate).ThenByDescending(p => p.InvoiceNumber)
            .Take(500).ToListAsync(cancellationToken);
        var list = await MapHeadersAsync(headers, cancellationToken);
        return (list, total);
    }

    public async Task<IReadOnlyList<PurchaseInvoice>> GetListAsync(
        Guid companyId,
        PurchaseInvoiceStatus? status = null,
        Guid? supplierId = null,
        CancellationToken cancellationToken = default)
    {
        var (items, _) = await GetPagedAsync(companyId, null, status, supplierId, cancellationToken);
        return items;
    }

    public async Task AddAsync(PurchaseInvoice invoice, CancellationToken cancellationToken = default)
    {
        await context.PurchaseInvoices.AddAsync(PurchaseMapper.ToEntity(invoice), cancellationToken);
        foreach (var item in invoice.Items)
            await context.PurchaseInvoiceItems.AddAsync(PurchaseMapper.ToItemEntity(invoice.Id, item), cancellationToken);
    }

    public async Task<IReadOnlyDictionary<Guid, string>> GetInvoiceNumberLookupAsync(
        IEnumerable<Guid> invoiceIds,
        CancellationToken cancellationToken = default)
    {
        var ids = invoiceIds.Distinct().ToList();
        if (ids.Count == 0)
            return new Dictionary<Guid, string>();

        return await context.PurchaseInvoices.AsNoTracking()
            .Where(p => ids.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, p => p.InvoiceNumber, cancellationToken);
    }

    public async Task<IReadOnlyList<PurchasePayablesAgingAggregate>> GetPayablesAgingAsync(
        Guid companyId,
        CancellationToken cancellationToken = default)
    {
        var cancelled = (int)PurchaseInvoiceStatus.Cancelled;

        var rows = await context.PurchaseInvoices.AsNoTracking()
            .Where(p => p.CompanyId == companyId && !p.IsArchived && p.Status != cancelled)
            .Join(
                context.Suppliers.AsNoTracking(),
                invoice => invoice.SupplierId,
                supplier => supplier.Id,
                (invoice, supplier) => new { invoice, supplier.NameAr })
            .GroupBy(x => new { x.invoice.SupplierId, x.NameAr })
            .Select(g => new
            {
                g.Key.SupplierId,
                g.Key.NameAr,
                TotalInvoiced = g.Sum(x => x.invoice.TotalAmount),
                Paid = g.Sum(x => x.invoice.PaidAmount),
                Outstanding = g.Sum(x => x.invoice.TotalAmount - x.invoice.PaidAmount),
                OldestInvoiceDate = g.Where(x => x.invoice.TotalAmount - x.invoice.PaidAmount > 0)
                    .Min(x => (DateTime?)x.invoice.InvoiceDate)
            })
            .Where(x => x.Outstanding > 0)
            .OrderByDescending(x => x.Outstanding)
            .ToListAsync(cancellationToken);

        return rows.Select(r => new PurchasePayablesAgingAggregate(
            r.SupplierId,
            r.NameAr,
            r.TotalInvoiced,
            r.Paid,
            r.Outstanding,
            r.OldestInvoiceDate)).ToList();
    }

    public async Task UpdateAsync(PurchaseInvoice invoice, CancellationToken cancellationToken = default)
    {
        var entity = await context.PurchaseInvoices.FirstOrDefaultAsync(p => p.Id == invoice.Id, cancellationToken)
            ?? throw new InvalidOperationException("Purchase invoice not found.");
        PurchaseMapper.UpdateEntity(entity, invoice);
        var existing = await context.PurchaseInvoiceItems.Where(i => i.PurchaseInvoiceId == invoice.Id).ToListAsync(cancellationToken);
        context.PurchaseInvoiceItems.RemoveRange(existing);
        foreach (var item in invoice.Items)
            await context.PurchaseInvoiceItems.AddAsync(PurchaseMapper.ToItemEntity(invoice.Id, item), cancellationToken);
    }

    private async Task<List<PurchaseInvoice>> MapHeadersAsync(
        List<PurchaseInvoiceEntity> headers,
        CancellationToken cancellationToken)
    {
        var ids = headers.Select(h => h.Id).ToList();
        var allItems = await context.PurchaseInvoiceItems.AsNoTracking()
            .Where(i => ids.Contains(i.PurchaseInvoiceId)).ToListAsync(cancellationToken);
        return headers.Select(h => PurchaseMapper.ToDomain(h, allItems.Where(i => i.PurchaseInvoiceId == h.Id).ToList())).ToList();
    }
}

internal sealed class PurchaseOrderRepository(ErpDbContext context) : IPurchaseOrderRepository
{
    public async Task<PurchaseOrder?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var header = await context.PurchaseOrders.AsNoTracking().FirstOrDefaultAsync(o => o.Id == id, cancellationToken);
        if (header is null) return null;
        var lines = await context.PurchaseOrderLines.AsNoTracking()
            .Where(l => l.PurchaseOrderId == id).ToListAsync(cancellationToken);
        return PurchaseMapper.ToOrderDomain(header, lines);
    }

    public async Task<IReadOnlyList<PurchaseOrder>> GetListAsync(
        Guid companyId,
        PurchaseOrderStatus? status = null,
        CancellationToken cancellationToken = default)
    {
        var query = context.PurchaseOrders.AsNoTracking().Where(o => o.CompanyId == companyId && !o.IsArchived);
        if (status.HasValue)
            query = query.Where(o => o.Status == (int)status.Value);
        var headers = await query.OrderByDescending(o => o.OrderDate).ToListAsync(cancellationToken);
        var list = new List<PurchaseOrder>();
        foreach (var h in headers)
        {
            var lines = await context.PurchaseOrderLines.AsNoTracking()
                .Where(l => l.PurchaseOrderId == h.Id).ToListAsync(cancellationToken);
            list.Add(PurchaseMapper.ToOrderDomain(h, lines));
        }
        return list;
    }

    public async Task AddAsync(PurchaseOrder order, CancellationToken cancellationToken = default)
    {
        await context.PurchaseOrders.AddAsync(PurchaseMapper.ToOrderEntity(order), cancellationToken);
        foreach (var line in order.Lines)
            await context.PurchaseOrderLines.AddAsync(PurchaseMapper.ToOrderLineEntity(order.Id, line), cancellationToken);
    }

    public async Task UpdateAsync(PurchaseOrder order, CancellationToken cancellationToken = default)
    {
        var entity = await context.PurchaseOrders.FirstOrDefaultAsync(o => o.Id == order.Id, cancellationToken)
            ?? throw new InvalidOperationException("Purchase order not found.");
        PurchaseMapper.UpdateOrderEntity(entity, order);
        var existing = await context.PurchaseOrderLines.Where(l => l.PurchaseOrderId == order.Id).ToListAsync(cancellationToken);
        context.PurchaseOrderLines.RemoveRange(existing);
        foreach (var line in order.Lines)
            await context.PurchaseOrderLines.AddAsync(PurchaseMapper.ToOrderLineEntity(order.Id, line), cancellationToken);
    }
}

internal sealed class PurchaseReturnRepository(ErpDbContext context) : IPurchaseReturnRepository
{
    public async Task<PurchaseReturn?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var header = await context.PurchaseReturns.AsNoTracking().FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
        if (header is null) return null;
        var lines = await context.PurchaseReturnLines.AsNoTracking()
            .Where(l => l.PurchaseReturnId == id).ToListAsync(cancellationToken);
        return PurchaseMapper.ToReturnDomain(header, lines);
    }

    public async Task<IReadOnlyList<PurchaseReturn>> GetListAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        var headers = await context.PurchaseReturns.AsNoTracking()
            .Where(r => r.CompanyId == companyId && !r.IsArchived)
            .OrderByDescending(r => r.ReturnDate).ToListAsync(cancellationToken);
        var list = new List<PurchaseReturn>();
        foreach (var h in headers)
        {
            var lines = await context.PurchaseReturnLines.AsNoTracking()
                .Where(l => l.PurchaseReturnId == h.Id).ToListAsync(cancellationToken);
            list.Add(PurchaseMapper.ToReturnDomain(h, lines));
        }
        return list;
    }

    public async Task AddAsync(PurchaseReturn purchaseReturn, CancellationToken cancellationToken = default)
    {
        await context.PurchaseReturns.AddAsync(PurchaseMapper.ToReturnEntity(purchaseReturn), cancellationToken);
        foreach (var line in purchaseReturn.Lines)
            await context.PurchaseReturnLines.AddAsync(PurchaseMapper.ToReturnLineEntity(purchaseReturn.Id, line), cancellationToken);
    }

    public async Task UpdateAsync(PurchaseReturn purchaseReturn, CancellationToken cancellationToken = default)
    {
        var entity = await context.PurchaseReturns.FirstOrDefaultAsync(r => r.Id == purchaseReturn.Id, cancellationToken)
            ?? throw new InvalidOperationException("Purchase return not found.");
        PurchaseMapper.UpdateReturnEntity(entity, purchaseReturn);
        var existing = await context.PurchaseReturnLines.Where(l => l.PurchaseReturnId == purchaseReturn.Id).ToListAsync(cancellationToken);
        context.PurchaseReturnLines.RemoveRange(existing);
        foreach (var line in purchaseReturn.Lines)
            await context.PurchaseReturnLines.AddAsync(PurchaseMapper.ToReturnLineEntity(purchaseReturn.Id, line), cancellationToken);
    }
}

internal sealed class PurchaseInvoicePaymentRepository(ErpDbContext context) : IPurchaseInvoicePaymentRepository
{
    public async Task AddAsync(PurchaseInvoicePayment payment, CancellationToken cancellationToken = default) =>
        await context.PurchaseInvoicePayments.AddAsync(new PurchaseInvoicePaymentEntity
        {
            Id = payment.Id,
            PurchaseInvoiceId = payment.PurchaseInvoiceId,
            PaymentVoucherId = payment.PaymentVoucherId,
            Amount = payment.Amount.Amount,
            AppliedAt = payment.AppliedAt,
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        }, cancellationToken);

    public async Task<IReadOnlyList<PurchaseInvoicePayment>> GetByInvoiceIdAsync(Guid invoiceId, CancellationToken cancellationToken = default)
    {
        var rows = await context.PurchaseInvoicePayments.AsNoTracking()
            .Where(p => p.PurchaseInvoiceId == invoiceId).ToListAsync(cancellationToken);
        return rows.Select(r => PurchaseInvoicePayment.Create(r.PurchaseInvoiceId, r.PaymentVoucherId, new Money(r.Amount))).ToList();
    }

    public async Task<IReadOnlyList<PurchaseInvoicePayment>> GetByVoucherIdAsync(Guid voucherId, CancellationToken cancellationToken = default)
    {
        var rows = await context.PurchaseInvoicePayments.AsNoTracking()
            .Where(p => p.PaymentVoucherId == voucherId).ToListAsync(cancellationToken);
        return rows.Select(r => PurchaseInvoicePayment.Create(r.PurchaseInvoiceId, r.PaymentVoucherId, new Money(r.Amount))).ToList();
    }
}
