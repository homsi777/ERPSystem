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
                FabricItemId = line.FabricItemId,
                FabricColorId = line.FabricColorId,
                FabricDisplayName = fabric?.NameAr ?? "—",
                FabricCode = fabric?.Code ?? "—",
                ColorDisplayName = color?.NameAr ?? "—",
                RollCount = line.RollCount,
                UnitPrice = line.UnitPrice,
                LineTotal = line.LineTotal
            });
        }

        return enriched;
    }

    public static async Task<IReadOnlyList<WarehouseDetailingRollDto>> EnrichRollsAsync(
        SalesInvoiceAggregate aggregate,
        IReadOnlyList<WarehouseDetailingRollDto> rolls,
        IFabricCatalogRepository catalog,
        CancellationToken cancellationToken = default)
    {
        if (rolls.Count == 0)
            return rolls;

        var itemsById = aggregate.Items.ToDictionary(i => i.Id);
        var enriched = new List<WarehouseDetailingRollDto>(rolls.Count);

        foreach (var roll in rolls)
        {
            if (!itemsById.TryGetValue(roll.SalesInvoiceItemId, out var item))
            {
                enriched.Add(new WarehouseDetailingRollDto
                {
                    RollDetailId = roll.RollDetailId,
                    SalesInvoiceItemId = roll.SalesInvoiceItemId,
                    RollSequence = roll.RollSequence,
                    FabricDisplayName = "—",
                    FabricCode = "—",
                    ColorDisplayName = "—",
                    LengthMeters = roll.LengthMeters,
                    HasValidLength = roll.HasValidLength
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
                FabricDisplayName = fabric?.NameAr ?? "—",
                FabricCode = fabric?.Code ?? "—",
                ColorDisplayName = color?.NameAr ?? "—",
                LengthMeters = roll.LengthMeters,
                HasValidLength = roll.HasValidLength
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
            SubTotal = invoice.SubTotal,
            DiscountTotal = invoice.DiscountTotal,
            TaxTotal = invoice.TaxTotal,
            GrandTotal = invoice.GrandTotal,
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
