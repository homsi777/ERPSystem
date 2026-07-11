using ERPSystem.Application.Abstractions.Repositories;
using ERPSystem.Domain.Aggregates;
using ERPSystem.Domain.Entities.Sales;
using ERPSystem.Domain.Enums;
using ERPSystem.Domain.ValueObjects;
using ERPSystem.Infrastructure.Persistence;
using ERPSystem.Infrastructure.Persistence.Mapping;
using ERPSystem.Infrastructure.Persistence.Models.ChinaImport;
using ERPSystem.Infrastructure.Persistence.Models.Sales;
using Microsoft.EntityFrameworkCore;

namespace ERPSystem.Infrastructure.Repositories;

internal sealed class ChinaContainerRepository(ErpDbContext context) : IChinaContainerRepository
{
    public async Task<ContainerAggregate?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await LoadAggregateAsync(id, null, cancellationToken);

    public async Task<ContainerAggregate?> GetByNumberAsync(string containerNumber, CancellationToken cancellationToken = default) =>
        await LoadAggregateAsync(null, containerNumber, cancellationToken);

    public async Task<IReadOnlyList<ContainerAggregate>> GetListAsync(
        Guid companyId,
        Guid? branchId = null,
        ChinaContainerStatus? status = null,
        CancellationToken cancellationToken = default)
    {
        var query = context.Containers.AsNoTracking().Where(c => c.CompanyId == companyId);
        if (branchId.HasValue)
            query = query.Where(c => c.BranchId == branchId.Value);
        if (status.HasValue)
            query = query.Where(c => c.Status == (int)status.Value);

        var headers = await query.OrderByDescending(c => c.ShipmentDate).ToListAsync(cancellationToken);
        return headers
            .Select(h => ContainerMapper.ToAggregate(h, [], null, []))
            .ToList();
    }

    public async Task<IReadOnlyDictionary<Guid, string>> GetNumberLookupAsync(
        Guid companyId,
        IEnumerable<Guid> containerIds,
        CancellationToken cancellationToken = default)
    {
        var ids = containerIds.Distinct().ToList();
        if (ids.Count == 0)
            return new Dictionary<Guid, string>();

        return await context.Containers.AsNoTracking()
            .Where(c => c.CompanyId == companyId && ids.Contains(c.Id))
            .ToDictionaryAsync(c => c.Id, c => c.ContainerNumber, cancellationToken);
    }

    public async Task AddAsync(ContainerAggregate aggregate, CancellationToken cancellationToken = default)
    {
        await context.Containers.AddAsync(ContainerMapper.ToHeaderEntity(aggregate), cancellationToken);
        await SyncItemsAsync(aggregate, cancellationToken);
        await SyncImportBatchesAsync(aggregate, cancellationToken);
        await SyncLandingCostAsync(aggregate, cancellationToken);
        await SyncFabricTypeLinesAsync(aggregate, cancellationToken);
    }

    public async Task UpdateAsync(ContainerAggregate aggregate, CancellationToken cancellationToken = default)
    {
        var header = await context.Containers.FirstOrDefaultAsync(c => c.Id == aggregate.Id, cancellationToken)
            ?? throw new InvalidOperationException("Container not found.");

        var mapped = ContainerMapper.ToHeaderEntity(aggregate);
        header.Status = mapped.Status;
        header.TotalRolls = mapped.TotalRolls;
        header.TotalMeters = mapped.TotalMeters;
        header.TotalWeightKg = mapped.TotalWeightKg;
        header.ApprovedAt = mapped.ApprovedAt;
        header.ApprovedByUserId = mapped.ApprovedByUserId;
        header.ExchangeRateToLocalCurrency = mapped.ExchangeRateToLocalCurrency;
        header.ChinaInvoiceAmountUsd = mapped.ChinaInvoiceAmountUsd;
        header.FinancialTaxReservePostedLocal = mapped.FinancialTaxReservePostedLocal;
        header.ExpectedArrival = mapped.ExpectedArrival;
        header.Notes = mapped.Notes;
        header.IsArchived = mapped.IsArchived;
        header.UpdatedAt = DateTime.UtcNow;

        await SyncItemsAsync(aggregate, cancellationToken);
        await SyncLandingCostAsync(aggregate, cancellationToken);
        await SyncFabricTypeLinesAsync(aggregate, cancellationToken);
    }

    private async Task<ContainerAggregate?> LoadAggregateAsync(Guid? id, string? number, CancellationToken ct)
    {
        var header = id.HasValue
            ? await context.Containers.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id.Value, ct)
            : await context.Containers.AsNoTracking().FirstOrDefaultAsync(c => c.ContainerNumber == number, ct);
        return header is null ? null : await LoadAggregateFromHeaderAsync(header, ct);
    }

    private async Task<ContainerAggregate> LoadAggregateFromHeaderAsync(ContainerEntity header, CancellationToken ct)
    {
        var items = await context.ContainerItems.AsNoTracking()
            .Where(i => i.ContainerId == header.Id).OrderBy(i => i.LineNumber).ToListAsync(ct);
        var landingCost = await context.LandingCosts.AsNoTracking()
            .FirstOrDefaultAsync(l => l.ContainerId == header.Id, ct);
        var typeLines = await context.ContainerFabricTypeLines.AsNoTracking()
            .Where(t => t.ContainerId == header.Id).OrderBy(t => t.LineNumber).ToListAsync(ct);
        return ContainerMapper.ToAggregate(header, items, landingCost, typeLines);
    }

    private async Task SyncItemsAsync(ContainerAggregate aggregate, CancellationToken ct)
    {
        var existing = await context.ContainerItems.Where(i => i.ContainerId == aggregate.Id).ToListAsync(ct);
        context.ContainerItems.RemoveRange(existing);
        await context.ContainerItems.AddRangeAsync(aggregate.Items.Select(i => new ContainerItemEntity
        {
            Id = i.Id,
            ContainerId = aggregate.Id,
            LineNumber = i.LineNumber,
            FabricItemId = i.FabricItemId,
            FabricColorId = i.FabricColorId,
            RollCount = i.RollCount,
            LengthMeters = i.LengthMeters.Value,
            WeightKg = i.WeightKg?.Value,
            LotCode = i.LotCode,
            BuyerCustomerId = i.BuyerCustomerId,
            SupplierRollNumber = i.SupplierRollNumber,
            RowStatus = i.RowStatus
        }), ct);
    }

    private async Task SyncImportBatchesAsync(ContainerAggregate aggregate, CancellationToken ct)
    {
        foreach (var batch in aggregate.ImportBatches)
        {
            await context.ImportBatches.AddAsync(new ImportBatchEntity
            {
                Id = batch.Id,
                ContainerId = aggregate.Id,
                BatchNumber = batch.BatchNumber,
                FileName = batch.FileName,
                ImportedAt = batch.ImportedAt,
                ImportedByUserId = batch.ImportedByUserId,
                ValidRowCount = batch.ValidRowCount,
                ErrorRowCount = batch.ErrorRowCount
            }, ct);
        }
    }

    private async Task SyncLandingCostAsync(ContainerAggregate aggregate, CancellationToken ct)
    {
        if (aggregate.LandingCost is null)
            return;

        var existing = await context.LandingCosts.FirstOrDefaultAsync(l => l.ContainerId == aggregate.Id, ct);
        var lc = aggregate.LandingCost;
        if (existing is null)
        {
            await context.LandingCosts.AddAsync(new LandingCostEntity
            {
                Id = lc.Id,
                ContainerId = aggregate.Id,
                TotalLengthMeters = lc.TotalLengthFromInvoice.Value,
                ContainerWeightKg = lc.ContainerWeight.Value,
                CustomsAmount = lc.CustomsAmountPaid.Amount,
                Shipping = lc.Shipping.Amount,
                Insurance = lc.Insurance.Amount,
                Clearance = lc.Clearance.Amount,
                OtherExpenses = lc.OtherExpenses.Amount,
                OtherExpense1 = lc.OtherExpense1.Amount,
                OtherExpense2 = lc.OtherExpense2.Amount,
                OtherExpense3 = lc.OtherExpense3.Amount,
                OtherExpense4 = lc.OtherExpense4.Amount,
                UsesWeightedAllocation = lc.UsesWeightedAllocation,
                Status = (int)lc.Status,
                CalculatedAt = lc.CalculatedAt,
                CalculatedByUserId = lc.CalculatedByUserId
            }, ct);
        }
        else
        {
            existing.TotalLengthMeters = lc.TotalLengthFromInvoice.Value;
            existing.ContainerWeightKg = lc.ContainerWeight.Value;
            existing.CustomsAmount = lc.CustomsAmountPaid.Amount;
            existing.Shipping = lc.Shipping.Amount;
            existing.Insurance = lc.Insurance.Amount;
            existing.Clearance = lc.Clearance.Amount;
            existing.OtherExpenses = lc.OtherExpenses.Amount;
            existing.OtherExpense1 = lc.OtherExpense1.Amount;
            existing.OtherExpense2 = lc.OtherExpense2.Amount;
            existing.OtherExpense3 = lc.OtherExpense3.Amount;
            existing.OtherExpense4 = lc.OtherExpense4.Amount;
            existing.UsesWeightedAllocation = lc.UsesWeightedAllocation;
            existing.Status = (int)lc.Status;
            existing.CalculatedAt = lc.CalculatedAt;
            existing.CalculatedByUserId = lc.CalculatedByUserId;
            existing.UpdatedAt = DateTime.UtcNow;
        }
    }

    private async Task SyncFabricTypeLinesAsync(ContainerAggregate aggregate, CancellationToken ct)
    {
        var existing = await context.ContainerFabricTypeLines
            .Where(t => t.ContainerId == aggregate.Id).ToListAsync(ct);
        context.ContainerFabricTypeLines.RemoveRange(existing);

        if (aggregate.FabricTypeLines.Count == 0)
            return;

        await context.ContainerFabricTypeLines.AddRangeAsync(aggregate.FabricTypeLines.Select(t =>
            new ContainerFabricTypeLineEntity
            {
                Id = t.Id,
                ContainerId = aggregate.Id,
                LineNumber = t.LineNumber,
                TypeDisplayName = t.TypeDisplayName,
                MatchKey = t.MatchKey,
                FabricItemId = t.FabricItemId,
                FabricColorId = t.FabricColorId,
                LengthMeters = t.LengthMeters,
                RollCount = t.RollCount,
                NetWeightKg = t.NetWeightKg,
                Cbm = t.Cbm,
                ChinaUnitPriceUsd = t.ChinaUnitPriceUsd,
                InvoiceLineAmountUsd = t.InvoiceLineAmountUsd,
                ExpenseShareUsd = t.ExpenseShareUsd,
                LandedCostPerMeterUsd = t.LandedCostPerMeterUsd,
                MarginPerMeterUsd = t.MarginPerMeterUsd,
                SalePricePerMeterUsd = t.SalePricePerMeterUsd,
                HasInvoiceMatch = t.HasInvoiceMatch,
                HasPlMatch = t.HasPlMatch,
                HasDplMatch = t.HasDplMatch,
                MatchWarnings = t.MatchWarnings,
                UsesWeightedAllocation = t.UsesWeightedAllocation
            }), ct);
    }
}

internal sealed class SalesInvoiceRepository(ErpDbContext context) : ISalesInvoiceRepository
{
    public async Task<SalesInvoiceAggregate?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await LoadAsync(id, null, cancellationToken);

    public async Task<SalesInvoiceAggregate?> GetByNumberAsync(string invoiceNumber, CancellationToken cancellationToken = default) =>
        await LoadAsync(null, invoiceNumber, cancellationToken);

    public async Task<IReadOnlyList<SalesInvoiceAggregate>> GetListAsync(
        Guid companyId,
        Guid? branchId = null,
        SalesInvoiceStatus? status = null,
        Guid? customerId = null,
        CancellationToken cancellationToken = default)
    {
        var query = context.SalesInvoices.AsNoTracking().Where(i => i.CompanyId == companyId);
        if (branchId.HasValue) query = query.Where(i => i.BranchId == branchId.Value);
        if (status.HasValue) query = query.Where(i => i.Status == (int)status.Value);
        if (customerId.HasValue) query = query.Where(i => i.CustomerId == customerId.Value);

        var headers = await query.OrderByDescending(i => i.InvoiceDate).ToListAsync(cancellationToken);
        return await MapHeadersToListAggregatesAsync(headers, cancellationToken);
    }

    public async Task<(IReadOnlyList<SalesInvoiceAggregate> Items, int TotalCount)> GetPagedListAsync(
        Guid companyId,
        Guid? branchId,
        SalesInvoiceStatus? status,
        Guid? customerId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = BuildListQuery(companyId, branchId, status, customerId);
        var total = await query.CountAsync(cancellationToken);
        var headers = await query
            .OrderByDescending(i => i.InvoiceDate)
            .Skip(Math.Max(0, page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var items = await MapHeadersToListAggregatesAsync(headers, cancellationToken);
        return (items, total);
    }

    public async Task<IReadOnlyList<SalesInvoiceAggregate>> GetDetailingQueueAsync(
        Guid warehouseId,
        CancellationToken cancellationToken = default)
    {
        var headers = await context.SalesInvoices.AsNoTracking()
            .Where(i => i.WarehouseId == warehouseId && i.Status == (int)SalesInvoiceStatus.AwaitingDetailing)
            .OrderBy(i => i.SentToWarehouseAt)
            .ToListAsync(cancellationToken);

        return await MapHeadersToListAggregatesAsync(headers, cancellationToken);
    }

    public async Task<IReadOnlyDictionary<Guid, SalesInvoiceCustomerAgingAggregate>> GetReceivablesAgingAggregatesAsync(
        Guid companyId,
        CancellationToken cancellationToken = default)
    {
        var minStatus = (int)SalesInvoiceStatus.AwaitingDetailing;
        var cancelled = (int)SalesInvoiceStatus.Cancelled;

        var rows = await context.SalesInvoices.AsNoTracking()
            .Where(i => i.CompanyId == companyId && i.Status >= minStatus && i.Status != cancelled)
            .GroupBy(i => i.CustomerId)
            .Select(g => new
            {
                CustomerId = g.Key,
                TotalInvoiced = g.Sum(i => i.GrandTotal),
                OldestInvoiceDate = g.Min(i => i.InvoiceDate)
            })
            .ToListAsync(cancellationToken);

        return rows.ToDictionary(
            r => r.CustomerId,
            r => new SalesInvoiceCustomerAgingAggregate(r.TotalInvoiced, r.OldestInvoiceDate));
    }

    public async Task AddAsync(SalesInvoiceAggregate aggregate, CancellationToken cancellationToken = default)
    {
        await context.SalesInvoices.AddAsync(SalesInvoiceMapper.ToHeaderEntity(aggregate), cancellationToken);
        await SyncChildrenAsync(aggregate, cancellationToken);
    }

    public async Task UpdateAsync(SalesInvoiceAggregate aggregate, CancellationToken cancellationToken = default)
    {
        var header = await context.SalesInvoices.FirstOrDefaultAsync(i => i.Id == aggregate.Id, cancellationToken)
            ?? throw new InvalidOperationException("Sales invoice not found.");

        var mapped = SalesInvoiceMapper.ToHeaderEntity(aggregate);
        header.CustomerId = mapped.CustomerId;
        header.WarehouseId = mapped.WarehouseId;
        header.ChinaContainerId = mapped.ChinaContainerId;
        header.PaymentType = mapped.PaymentType;
        header.PartialPaymentAmount = mapped.PartialPaymentAmount;
        header.CashboxId = mapped.CashboxId;
        header.Status = mapped.Status;
        header.SubTotal = mapped.SubTotal;
        header.DiscountTotal = mapped.DiscountTotal;
        header.TaxTotal = mapped.TaxTotal;
        header.GrandTotal = mapped.GrandTotal;
        header.RoundingDifference = mapped.RoundingDifference;
        header.IsLegacyUntaxed = mapped.IsLegacyUntaxed;
        header.ApprovedByUserId = mapped.ApprovedByUserId;
        header.SentToWarehouseAt = mapped.SentToWarehouseAt;
        header.DetailedAt = mapped.DetailedAt;
        header.ApprovedAt = mapped.ApprovedAt;
        header.PrintedAt = mapped.PrintedAt;
        header.DeliveredAt = mapped.DeliveredAt;
        header.DeliveredToName = mapped.DeliveredToName;
        header.DeliveryDriverName = mapped.DeliveryDriverName;
        header.DeliveryNotes = mapped.DeliveryNotes;
        header.CancelledAt = mapped.CancelledAt;
        header.CancelReason = mapped.CancelReason;
        header.UpdatedAt = DateTime.UtcNow;

        await SyncChildrenAsync(aggregate, cancellationToken);
    }

    private IQueryable<SalesInvoiceEntity> BuildListQuery(
        Guid companyId,
        Guid? branchId,
        SalesInvoiceStatus? status,
        Guid? customerId)
    {
        var query = context.SalesInvoices.AsNoTracking().Where(i => i.CompanyId == companyId);
        if (branchId.HasValue) query = query.Where(i => i.BranchId == branchId.Value);
        if (status.HasValue) query = query.Where(i => i.Status == (int)status.Value);
        if (customerId.HasValue) query = query.Where(i => i.CustomerId == customerId.Value);
        return query;
    }

    private async Task<IReadOnlyList<SalesInvoiceAggregate>> MapHeadersToListAggregatesAsync(
        IReadOnlyList<SalesInvoiceEntity> headers,
        CancellationToken cancellationToken)
    {
        if (headers.Count == 0)
            return [];

        var invoiceIds = headers.Select(h => h.Id).ToList();
        var allItems = await context.SalesInvoiceItems.AsNoTracking()
            .Where(i => invoiceIds.Contains(i.SalesInvoiceId))
            .OrderBy(i => i.LineNumber)
            .ToListAsync(cancellationToken);
        var itemsByInvoice = allItems
            .GroupBy(i => i.SalesInvoiceId)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<SalesInvoiceItemEntity>)g.ToList());

        return headers
            .Select(h => SalesInvoiceMapper.ToAggregate(
                h,
                itemsByInvoice.GetValueOrDefault(h.Id) ?? [],
                [],
                null,
                []))
            .ToList();
    }

    private async Task<SalesInvoiceAggregate?> LoadAsync(Guid? id, string? number, CancellationToken ct)
    {
        var header = id.HasValue
            ? await context.SalesInvoices.AsNoTracking().FirstOrDefaultAsync(i => i.Id == id.Value, ct)
            : await context.SalesInvoices.AsNoTracking().FirstOrDefaultAsync(i => i.InvoiceNumber == number, ct);
        return header is null ? null : await LoadFromHeaderAsync(header, ct);
    }

    private async Task<SalesInvoiceAggregate> LoadFromHeaderAsync(SalesInvoiceEntity header, CancellationToken ct)
    {
        var items = await context.SalesInvoiceItems.AsNoTracking()
            .Where(i => i.SalesInvoiceId == header.Id).OrderBy(i => i.LineNumber).ToListAsync(ct);
        var rolls = await context.SalesInvoiceRollDetails.AsNoTracking()
            .Where(r => r.SalesInvoiceId == header.Id).ToListAsync(ct);
        var itemTaxes = await context.SalesInvoiceItemTaxes.AsNoTracking()
            .Where(t => t.SalesInvoiceId == header.Id).ToListAsync(ct);
        var session = await context.WarehouseDetailingSessions.AsNoTracking()
            .FirstOrDefaultAsync(s => s.SalesInvoiceId == header.Id, ct);
        return SalesInvoiceMapper.ToAggregate(header, items, rolls, session, itemTaxes);
    }

    private async Task SyncChildrenAsync(SalesInvoiceAggregate aggregate, CancellationToken ct)
    {
        var existingItems = await context.SalesInvoiceItems.Where(i => i.SalesInvoiceId == aggregate.Id).ToListAsync(ct);
        context.SalesInvoiceItems.RemoveRange(existingItems);
        await context.SalesInvoiceItems.AddRangeAsync(aggregate.Items.Select(i => new SalesInvoiceItemEntity
        {
            Id = i.Id,
            SalesInvoiceId = aggregate.Id,
            LineNumber = i.LineNumber,
            ChinaContainerId = i.ChinaContainerId,
            FabricItemId = i.FabricItemId,
            FabricColorId = i.FabricColorId,
            RollCount = i.RollCount,
            UnitPrice = i.UnitPrice.Amount,
            OriginalUnitPrice = i.OriginalUnitPrice.Amount,
            Unit = i.Unit,
            LineTotal = i.LineTotal.Amount,
            DiscountAmount = i.DiscountAmount.Amount,
            DiscountReason = i.DiscountReason,
            PriceModifiedByUserId = i.PriceModifiedByUserId,
            PriceModifiedAt = i.PriceModifiedAt,
            Notes = i.Notes,
            TaxCodeId = i.TaxCodeId
        }), ct);

        var existingTaxes = await context.SalesInvoiceItemTaxes.Where(t => t.SalesInvoiceId == aggregate.Id).ToListAsync(ct);
        context.SalesInvoiceItemTaxes.RemoveRange(existingTaxes);
        await context.SalesInvoiceItemTaxes.AddRangeAsync(aggregate.ItemTaxSnapshots.Select(t => new SalesInvoiceItemTaxEntity
        {
            Id = t.Id,
            SalesInvoiceId = aggregate.Id,
            SalesInvoiceItemId = t.SalesInvoiceItemId,
            TaxCodeId = t.TaxCodeId,
            TaxCode = t.TaxCode,
            TaxName = t.TaxName,
            TaxRate = t.TaxRate,
            TaxableAmount = t.TaxableAmount.Amount,
            TaxAmount = t.TaxAmount.Amount,
            IsInclusive = t.IsInclusive,
            SalesTaxAccountId = t.SalesTaxAccountId,
            IsFrozen = t.IsFrozen
        }), ct);

        var existingRolls = await context.SalesInvoiceRollDetails.Where(r => r.SalesInvoiceId == aggregate.Id).ToListAsync(ct);
        context.SalesInvoiceRollDetails.RemoveRange(existingRolls);
        await context.SalesInvoiceRollDetails.AddRangeAsync(aggregate.RollDetails.Select(r => new SalesInvoiceRollDetailEntity
        {
            Id = r.Id,
            SalesInvoiceId = aggregate.Id,
            SalesInvoiceItemId = r.SalesInvoiceItemId,
            RollSequence = r.RollSequence.Value,
            FabricRollId = r.FabricRollId,
            LengthMeters = r.LengthMeters.Value,
            EnteredByUserId = r.EnteredByUserId,
            EnteredAt = r.EnteredAt,
            DraftRollNumber = r.DraftRollNumber,
            DraftLengthMeters = r.DraftLengthMeters
        }), ct);

        var existingSession = await context.WarehouseDetailingSessions
            .FirstOrDefaultAsync(s => s.SalesInvoiceId == aggregate.Id, ct);
        if (aggregate.DetailingSession is null)
        {
            if (existingSession is not null)
                context.WarehouseDetailingSessions.Remove(existingSession);
            return;
        }

        if (existingSession is null)
        {
            await context.WarehouseDetailingSessions.AddAsync(new WarehouseDetailingSessionEntity
            {
                Id = aggregate.DetailingSession.Id,
                SalesInvoiceId = aggregate.Id,
                Status = (int)aggregate.DetailingSession.Status,
                AssignedOfficerUserId = aggregate.DetailingSession.AssignedOfficerUserId,
                StartedAt = aggregate.DetailingSession.StartedAt,
                CompletedAt = aggregate.DetailingSession.CompletedAt,
                RejectionReason = aggregate.DetailingSession.RejectionReason
            }, ct);
        }
        else
        {
            existingSession.Status = (int)aggregate.DetailingSession.Status;
            existingSession.AssignedOfficerUserId = aggregate.DetailingSession.AssignedOfficerUserId;
            existingSession.StartedAt = aggregate.DetailingSession.StartedAt;
            existingSession.CompletedAt = aggregate.DetailingSession.CompletedAt;
            existingSession.RejectionReason = aggregate.DetailingSession.RejectionReason;
            existingSession.UpdatedAt = DateTime.UtcNow;
        }
    }
}

internal sealed class SalesReturnRepository(ErpDbContext context) : ISalesReturnRepository
{
    public async Task<SalesReturnAggregate?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var header = await context.SalesReturns.AsNoTracking().FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
        if (header is null) return null;
        var lines = await context.SalesReturnLines.AsNoTracking()
            .Where(l => l.SalesReturnId == id)
            .OrderBy(l => l.LineNumber)
            .ToListAsync(cancellationToken);
        return SalesReturnMapper.ToAggregate(header, lines);
    }

    public async Task<IReadOnlyList<SalesReturnAggregate>> GetListAsync(
        Guid companyId,
        Guid? branchId = null,
        VoucherStatus? status = null,
        Guid? customerId = null,
        Guid? originalInvoiceId = null,
        CancellationToken cancellationToken = default)
    {
        var query = context.SalesReturns.AsNoTracking().Where(r => r.CompanyId == companyId);
        if (branchId.HasValue) query = query.Where(r => r.BranchId == branchId.Value);
        if (status.HasValue) query = query.Where(r => r.Status == (int)status.Value);
        if (customerId.HasValue) query = query.Where(r => r.CustomerId == customerId.Value);
        if (originalInvoiceId.HasValue) query = query.Where(r => r.OriginalInvoiceId == originalInvoiceId.Value);

        var headers = await query.OrderByDescending(r => r.ReturnDate).ToListAsync(cancellationToken);
        var result = new List<SalesReturnAggregate>();
        foreach (var header in headers)
        {
            var lines = await context.SalesReturnLines.AsNoTracking()
                .Where(l => l.SalesReturnId == header.Id)
                .OrderBy(l => l.LineNumber)
                .ToListAsync(cancellationToken);
            result.Add(SalesReturnMapper.ToAggregate(header, lines));
        }
        return result;
    }

    public async Task AddAsync(SalesReturnAggregate aggregate, CancellationToken cancellationToken = default)
    {
        await context.SalesReturns.AddAsync(SalesReturnMapper.ToHeaderEntity(aggregate), cancellationToken);
        await SyncLinesAsync(aggregate, cancellationToken);
    }

    public async Task UpdateAsync(SalesReturnAggregate aggregate, CancellationToken cancellationToken = default)
    {
        var header = await context.SalesReturns.FirstOrDefaultAsync(r => r.Id == aggregate.Id, cancellationToken)
            ?? throw new InvalidOperationException("Sales return not found.");
        var mapped = SalesReturnMapper.ToHeaderEntity(aggregate);
        header.Status = mapped.Status;
        header.TotalAmount = mapped.TotalAmount;
        header.TaxTotal = mapped.TaxTotal;
        header.IsLegacyUntaxedReturn = mapped.IsLegacyUntaxedReturn;
        header.Reason = mapped.Reason;
        header.ReasonNotes = mapped.ReasonNotes;
        header.Notes = mapped.Notes;
        header.PostedByUserId = mapped.PostedByUserId;
        header.PostedAt = mapped.PostedAt;
        header.JournalEntryNumber = mapped.JournalEntryNumber;
        header.UpdatedAt = DateTime.UtcNow;
        await SyncLinesAsync(aggregate, cancellationToken);
    }

    private async Task SyncLinesAsync(SalesReturnAggregate aggregate, CancellationToken ct)
    {
        var existing = await context.SalesReturnLines.Where(l => l.SalesReturnId == aggregate.Id).ToListAsync(ct);
        context.SalesReturnLines.RemoveRange(existing);
        await context.SalesReturnLines.AddRangeAsync(aggregate.Lines.Select(l => new SalesReturnLineEntity
        {
            Id = l.Id,
            SalesReturnId = aggregate.Id,
            LineNumber = l.LineNumber,
            OriginalInvoiceItemId = l.OriginalInvoiceItemId,
            FabricItemId = l.FabricItemId,
            FabricColorId = l.FabricColorId,
            OriginalMeters = l.OriginalMeters,
            ReturnMeters = l.ReturnMeters,
            UnitPrice = l.UnitPrice.Amount,
            LineTotal = l.LineTotal.Amount
        }), ct);
    }
}

internal sealed class ReceiptInvoicePaymentRepository(ErpDbContext context) : IReceiptInvoicePaymentRepository
{
    public async Task AddAsync(ReceiptInvoicePayment payment, CancellationToken cancellationToken = default) =>
        await context.ReceiptInvoicePayments.AddAsync(new ReceiptInvoicePaymentEntity
        {
            Id = payment.Id,
            SalesInvoiceId = payment.SalesInvoiceId,
            ReceiptVoucherId = payment.ReceiptVoucherId,
            Amount = payment.Amount.Amount,
            AppliedAt = payment.AppliedAt,
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        }, cancellationToken);

    public async Task<IReadOnlyList<ReceiptInvoicePayment>> GetByInvoiceIdAsync(Guid invoiceId, CancellationToken cancellationToken = default)
    {
        var rows = await context.ReceiptInvoicePayments.AsNoTracking()
            .Where(p => p.SalesInvoiceId == invoiceId).ToListAsync(cancellationToken);
        return rows.Select(r => ReceiptInvoicePayment.Create(r.SalesInvoiceId, r.ReceiptVoucherId, new Money(r.Amount))).ToList();
    }

    public async Task<IReadOnlyList<ReceiptInvoicePayment>> GetByVoucherIdAsync(Guid voucherId, CancellationToken cancellationToken = default)
    {
        var rows = await context.ReceiptInvoicePayments.AsNoTracking()
            .Where(p => p.ReceiptVoucherId == voucherId).ToListAsync(cancellationToken);
        return rows.Select(r => ReceiptInvoicePayment.Create(r.SalesInvoiceId, r.ReceiptVoucherId, new Money(r.Amount))).ToList();
    }

    public async Task<decimal> GetCollectedTotalAsync(Guid invoiceId, CancellationToken cancellationToken = default)
    {
        return await context.ReceiptInvoicePayments.AsNoTracking()
            .Where(p => p.SalesInvoiceId == invoiceId)
            .SumAsync(p => (decimal?)p.Amount, cancellationToken) ?? 0m;
    }

    public async Task<IReadOnlyDictionary<Guid, decimal>> GetCollectedTotalsAsync(
        IEnumerable<Guid> invoiceIds, CancellationToken cancellationToken = default)
    {
        var ids = invoiceIds.Distinct().ToArray();
        if (ids.Length == 0) return new Dictionary<Guid, decimal>();
        return await context.ReceiptInvoicePayments.AsNoTracking()
            .Where(p => ids.Contains(p.SalesInvoiceId))
            .GroupBy(p => p.SalesInvoiceId)
            .ToDictionaryAsync(g => g.Key, g => g.Sum(p => p.Amount), cancellationToken);
    }

    public async Task<IReadOnlyList<(ReceiptInvoicePayment Payment, string VoucherNumber)>> GetByInvoiceWithVoucherAsync(
        Guid invoiceId, CancellationToken cancellationToken = default)
    {
        var rows = await (from p in context.ReceiptInvoicePayments.AsNoTracking()
                          join r in context.ReceiptVouchers.AsNoTracking() on p.ReceiptVoucherId equals r.Id
                          where p.SalesInvoiceId == invoiceId
                          orderby p.AppliedAt descending
                          select new { p, r.VoucherNumber }).ToListAsync(cancellationToken);
        return rows.Select(x =>
            (ReceiptInvoicePayment.Create(x.p.SalesInvoiceId, x.p.ReceiptVoucherId, new Money(x.p.Amount)), x.VoucherNumber)).ToList();
    }
}
