using ERPSystem.Application.Abstractions.Repositories;
using ERPSystem.Application.DTOs.Sales;
using ERPSystem.Domain.Aggregates;
using ERPSystem.Domain.Entities.Catalog;

namespace ERPSystem.Application.Common;

public static class SalesInvoiceCatalogEnricher
{
    public static async Task<IReadOnlyList<SalesInvoiceLineDto>> EnrichLinesAsync(
        IReadOnlyList<SalesInvoiceLineDto> lines,
        IFabricCatalogRepository catalog,
        CancellationToken cancellationToken = default)
    {
        if (lines.Count == 0)
            return lines;

        var fabrics = await catalog.GetItemsByIdsAsync(lines.Select(l => l.FabricItemId), cancellationToken);
        var colors = await catalog.GetColorsByIdsAsync(lines.Select(l => l.FabricColorId), cancellationToken);
        return EnrichLines(lines, fabrics, colors);
    }

    public static IReadOnlyList<SalesInvoiceLineDto> EnrichLines(
        IReadOnlyList<SalesInvoiceLineDto> lines,
        IReadOnlyDictionary<Guid, FabricItem> fabrics,
        IReadOnlyDictionary<Guid, FabricColor> colors)
    {
        if (lines.Count == 0)
            return lines;

        var enriched = new List<SalesInvoiceLineDto>(lines.Count);
        foreach (var line in lines)
        {
            fabrics.TryGetValue(line.FabricItemId, out var fabric);
            colors.TryGetValue(line.FabricColorId, out var color);
            enriched.Add(new SalesInvoiceLineDto
            {
                Id = line.Id,
                LineNumber = line.LineNumber,
                ChinaContainerId = line.ChinaContainerId,
                FabricItemId = line.FabricItemId,
                FabricColorId = line.FabricColorId,
                FabricDisplayName = fabric?.NameAr ?? "—",
                FabricCode = fabric?.Code ?? "—",
                ColorDisplayName = color?.NameAr ?? "—",
                RollCount = line.RollCount,
                UnitPrice = line.UnitPrice,
                OriginalUnitPrice = line.OriginalUnitPrice,
                Unit = line.Unit,
                TotalLengthMeters = line.TotalLengthMeters,
                LineTotal = line.LineTotal,
                DiscountAmount = line.DiscountAmount,
                DiscountReason = line.DiscountReason,
                TaxCodeId = line.TaxCodeId,
                TaxCode = line.TaxCode,
                TaxName = line.TaxName,
                TaxRate = line.TaxRate,
                TaxCategory = line.TaxCategory,
                IsTaxInclusive = line.IsTaxInclusive,
                TaxableAmount = line.TaxableAmount,
                TaxAmount = line.TaxAmount,
                Notes = line.Notes,
                RollLengths = line.RollLengths
            });
        }

        return enriched;
    }

    public static async Task<IReadOnlyList<WarehouseDetailingRollDto>> EnrichRollsAsync(
        SalesInvoiceAggregate aggregate,
        IReadOnlyList<WarehouseDetailingRollDto> rolls,
        IFabricCatalogRepository catalog,
        IChinaContainerRepository containerRepository,
        CancellationToken cancellationToken = default)
    {
        if (rolls.Count == 0)
            return rolls;

        var fabrics = await catalog.GetItemsByIdsAsync(aggregate.Items.Select(i => i.FabricItemId), cancellationToken);
        var colors = await catalog.GetColorsByIdsAsync(aggregate.Items.Select(i => i.FabricColorId), cancellationToken);
        var containerIds = aggregate.Items
            .Select(i => i.ChinaContainerId)
            .Concat(rolls.Select(r => r.ChinaContainerId))
            .Where(id => id != Guid.Empty)
            .Distinct();
        var containerLookup = await containerRepository.GetNumberLookupAsync(
            aggregate.CompanyId,
            containerIds,
            cancellationToken);
        return EnrichRolls(aggregate, rolls, fabrics, colors, containerLookup);
    }

    public static IReadOnlyList<WarehouseDetailingRollDto> EnrichRolls(
        SalesInvoiceAggregate aggregate,
        IReadOnlyList<WarehouseDetailingRollDto> rolls,
        IReadOnlyDictionary<Guid, FabricItem> fabrics,
        IReadOnlyDictionary<Guid, FabricColor> colors,
        IReadOnlyDictionary<Guid, string> containerLookup)
    {
        if (rolls.Count == 0)
            return rolls;

        var itemsById = aggregate.Items.ToDictionary(i => i.Id);
        var enriched = new List<WarehouseDetailingRollDto>(rolls.Count);

        string ResolveContainerDisplay(Guid containerId) =>
            containerId == Guid.Empty
                ? "—"
                : containerLookup.TryGetValue(containerId, out var number) ? number : "—";

        foreach (var roll in rolls)
        {
            if (!itemsById.TryGetValue(roll.SalesInvoiceItemId, out var item))
            {
                enriched.Add(new WarehouseDetailingRollDto
                {
                    RollDetailId = roll.RollDetailId,
                    SalesInvoiceItemId = roll.SalesInvoiceItemId,
                    RollSequence = roll.RollSequence,
                    FabricItemId = roll.FabricItemId,
                    FabricColorId = roll.FabricColorId,
                    FabricDisplayName = "—",
                    FabricCode = "—",
                    ColorDisplayName = "—",
                    LengthMeters = roll.LengthMeters,
                    HasValidLength = roll.HasValidLength,
                    Unit = roll.Unit,
                    ChinaContainerId = roll.ChinaContainerId,
                    ContainerDisplay = ResolveContainerDisplay(roll.ChinaContainerId),
                    DraftRollNumber = roll.DraftRollNumber,
                    DraftLengthMeters = roll.DraftLengthMeters
                });
                continue;
            }

            fabrics.TryGetValue(item.FabricItemId, out var fabric);
            colors.TryGetValue(item.FabricColorId, out var color);
            enriched.Add(new WarehouseDetailingRollDto
            {
                RollDetailId = roll.RollDetailId,
                SalesInvoiceItemId = roll.SalesInvoiceItemId,
                RollSequence = roll.RollSequence,
                FabricItemId = item.FabricItemId,
                FabricColorId = item.FabricColorId,
                FabricDisplayName = fabric?.NameAr ?? "—",
                FabricCode = fabric?.Code ?? "—",
                ColorDisplayName = color?.NameAr ?? "—",
                LengthMeters = roll.LengthMeters,
                HasValidLength = roll.HasValidLength,
                Unit = roll.Unit,
                ChinaContainerId = item.ChinaContainerId,
                ContainerDisplay = ResolveContainerDisplay(item.ChinaContainerId),
                DraftRollNumber = roll.DraftRollNumber,
                DraftLengthMeters = roll.DraftLengthMeters
            });
        }

        return enriched;
    }

    public static SalesInvoiceDto WithEnrichedLines(
        SalesInvoiceDto invoice,
        IReadOnlyList<SalesInvoiceLineDto> lines,
        string? warehouseName = null) =>
        new()
        {
            Id = invoice.Id,
            InvoiceNumber = invoice.InvoiceNumber,
            Status = invoice.Status,
            CustomerId = invoice.CustomerId,
            CustomerName = invoice.CustomerName,
            WarehouseId = invoice.WarehouseId,
            WarehouseName = string.IsNullOrWhiteSpace(warehouseName) ? invoice.WarehouseName : warehouseName,
            ChinaContainerId = invoice.ChinaContainerId,
            ContainerNumber = invoice.ContainerNumber,
            InvoiceDate = invoice.InvoiceDate,
            PaymentType = invoice.PaymentType,
            PartialPaymentAmount = invoice.PartialPaymentAmount,
            CashboxId = invoice.CashboxId,
            SubTotal = invoice.SubTotal,
            DiscountTotal = invoice.DiscountTotal,
            TaxTotal = invoice.TaxTotal,
            GrandTotal = invoice.GrandTotal,
            RoundingDifference = invoice.RoundingDifference,
            IsLegacyUntaxed = invoice.IsLegacyUntaxed,
            SentToWarehouseAt = invoice.SentToWarehouseAt,
            DetailedAt = invoice.DetailedAt,
            ApprovedAt = invoice.ApprovedAt,
            PrintedAt = invoice.PrintedAt,
            DeliveredAt = invoice.DeliveredAt,
            CancelledAt = invoice.CancelledAt,
            DeliveredToName = invoice.DeliveredToName,
            DeliveryDriverName = invoice.DeliveryDriverName,
            DeliveryNotes = invoice.DeliveryNotes,
            CancelReason = invoice.CancelReason,
            Lines = lines
        };

    public static WarehouseDetailingDto WithEnrichedRolls(
        WarehouseDetailingDto detailing,
        IReadOnlyList<WarehouseDetailingRollDto> rolls) =>
        new()
        {
            InvoiceId = detailing.InvoiceId,
            InvoiceNumber = detailing.InvoiceNumber,
            CustomerName = detailing.CustomerName,
            WarehouseId = detailing.WarehouseId,
            ChinaContainerId = detailing.ChinaContainerId,
            SentToWarehouseAt = detailing.SentToWarehouseAt,
            RepresentativeUnitPrice = detailing.RepresentativeUnitPrice,
            Status = detailing.Status,
            Rolls = rolls
        };
}
