using ERPSystem.Application.Abstractions;
using ERPSystem.Application.Abstractions.Repositories;
using ERPSystem.Application.DTOs.Sales;
using ERPSystem.Application.Queries.Sales;
using ERPSystem.Application.Results;

namespace ERPSystem.Application.UseCases.Sales;

/// <summary>
/// Read-only preview used before approval to warn the manager about lines whose
/// applied sale price is below the weighted-average roll cost. It never blocks the sale.
/// </summary>
public sealed class CheckSalesInvoiceBelowCostHandler(
    ISalesInvoiceRepository invoiceRepository,
    IInventoryRepository inventoryRepository)
    : IQueryHandler<CheckSalesInvoiceBelowCostQuery, ApplicationResult<IReadOnlyList<SalesInvoiceBelowCostLineDto>>>
{
    public async Task<ApplicationResult<IReadOnlyList<SalesInvoiceBelowCostLineDto>>> HandleAsync(
        CheckSalesInvoiceBelowCostQuery query,
        CancellationToken cancellationToken = default)
    {
        var aggregate = await invoiceRepository.GetByIdAsync(query.InvoiceId, cancellationToken);
        if (aggregate is null)
            return ApplicationResult<IReadOnlyList<SalesInvoiceBelowCostLineDto>>.NotFound("Invoice not found.");

        var rollIds = aggregate.RollDetails
            .Where(d => d.FabricRollId.HasValue && d.HasValidLength)
            .Select(d => d.FabricRollId!.Value)
            .Distinct()
            .ToList();

        var costs = await inventoryRepository.GetRollCostsAsync(rollIds, cancellationToken);

        var below = new List<SalesInvoiceBelowCostLineDto>();
        foreach (var item in aggregate.Items)
        {
            var lineDetails = aggregate.RollDetails
                .Where(d => d.SalesInvoiceItemId == item.Id && d.HasValidLength)
                .ToList();

            var meters = lineDetails.Sum(d => d.LengthMeters.Value);
            if (meters <= 0)
                continue;

            decimal weightedCost = 0m;
            foreach (var detail in lineDetails)
            {
                var costPerMeter = detail.FabricRollId.HasValue
                    && costs.TryGetValue(detail.FabricRollId.Value, out var c)
                    ? c
                    : 0m;
                weightedCost += detail.LengthMeters.Value * costPerMeter;
            }

            var avgCost = weightedCost / meters;
            if (avgCost > 0 && item.UnitPrice.Amount < avgCost)
            {
                below.Add(new SalesInvoiceBelowCostLineDto
                {
                    LineNumber = item.LineNumber,
                    FabricDisplayName = "",
                    ColorDisplayName = "",
                    AppliedPrice = item.UnitPrice.Amount,
                    CostPerMeter = decimal.Round(avgCost, 2),
                    Meters = meters
                });
            }
        }

        return ApplicationResult<IReadOnlyList<SalesInvoiceBelowCostLineDto>>.Success(below);
    }
}
