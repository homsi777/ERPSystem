using ERPSystem.Application.Abstractions.Repositories;
using ERPSystem.Application.DTOs.Sales;
using ERPSystem.Domain.Aggregates;

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

        var enriched = new List<SalesInvoiceLineDto>(lines.Count);
        foreach (var line in lines)
        {
            var fabric = await catalog.GetItemByIdAsync(line.FabricItemId, cancellationToken);
            var color = await catalog.GetColorByIdAsync(line.FabricColorId, cancellationToken);
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
                TotalLengthMeters = line.TotalLengthMeters,
                LineTotal = line.LineTotal,
                DiscountAmount = line.DiscountAmount,
                DiscountReason = line.DiscountReason,
                Notes = line.Notes
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

        var itemsById = aggregate.Items.ToDictionary(i => i.Id);
        var enriched = new List<WarehouseDetailingRollDto>(rolls.Count);
        var containerDisplayCache = new Dictionary<Guid, string>();

        async Task<string> ResolveContainerDisplayAsync(Guid containerId)
        {
            if (containerId == Guid.Empty)
                return "—";
            if (containerDisplayCache.TryGetValue(containerId, out var cached))
                return cached;

            var container = await containerRepository.GetByIdAsync(containerId, cancellationToken);
            var display = container?.ContainerNumber.Value ?? "—";
            containerDisplayCache[containerId] = display;
            return display;
        }

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
                    ChinaContainerId = roll.ChinaContainerId,
                    ContainerDisplay = await ResolveContainerDisplayAsync(roll.ChinaContainerId),
                    DraftRollNumber = roll.DraftRollNumber,
                    DraftLengthMeters = roll.DraftLengthMeters
                });
                continue;
            }

            var fabric = await catalog.GetItemByIdAsync(item.FabricItemId, cancellationToken);
            var color = await catalog.GetColorByIdAsync(item.FabricColorId, cancellationToken);
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
                ChinaContainerId = item.ChinaContainerId,
                ContainerDisplay = await ResolveContainerDisplayAsync(item.ChinaContainerId),
                DraftRollNumber = roll.DraftRollNumber,
                DraftLengthMeters = roll.DraftLengthMeters
            });
        }

        return enriched;
    }

    public static SalesInvoiceDto WithEnrichedLines(SalesInvoiceDto invoice, IReadOnlyList<SalesInvoiceLineDto> lines) =>
        new()
        {
            Id = invoice.Id,
            InvoiceNumber = invoice.InvoiceNumber,
            Status = invoice.Status,
            CustomerId = invoice.CustomerId,
            CustomerName = invoice.CustomerName,
            WarehouseId = invoice.WarehouseId,
            ChinaContainerId = invoice.ChinaContainerId,
            InvoiceDate = invoice.InvoiceDate,
            PaymentType = invoice.PaymentType,
            PartialPaymentAmount = invoice.PartialPaymentAmount,
            SubTotal = invoice.SubTotal,
            DiscountTotal = invoice.DiscountTotal,
            TaxTotal = invoice.TaxTotal,
            GrandTotal = invoice.GrandTotal,
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
            ChinaContainerId = detailing.ChinaContainerId,
            SentToWarehouseAt = detailing.SentToWarehouseAt,
            RepresentativeUnitPrice = detailing.RepresentativeUnitPrice,
            Status = detailing.Status,
            Rolls = rolls
        };
}
