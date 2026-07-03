using ERPSystem.Application.Abstractions;
using ERPSystem.Application.Abstractions.Repositories;
using ERPSystem.Application.DTOs.Sales;
using ERPSystem.Application.Queries.Sales;
using ERPSystem.Application.Results;

namespace ERPSystem.Application.UseCases.Sales;

public sealed class GetSalesWarehouseStockHandler(
    IInventoryRepository inventoryRepository)
    : IQueryHandler<GetSalesWarehouseStockQuery, ApplicationResult<IReadOnlyList<SalesWarehouseStockOptionDto>>>
{
    public async Task<ApplicationResult<IReadOnlyList<SalesWarehouseStockOptionDto>>> HandleAsync(
        GetSalesWarehouseStockQuery query,
        CancellationToken cancellationToken = default)
    {
        var rolls = await inventoryRepository.GetAvailableRollsForContainerAsync(
            query.ContainerId,
            query.WarehouseId,
            cancellationToken);

        var options = rolls
            .GroupBy(r => (r.FabricItemId, r.FabricColorId))
            .Select(g =>
            {
                var first = g.First();
                var salePrice = g
                    .Select(r => r.SalePricePerMeter)
                    .FirstOrDefault(p => p is > 0);

                return new SalesWarehouseStockOptionDto
                {
                    FabricItemId = g.Key.FabricItemId,
                    FabricColorId = g.Key.FabricColorId,
                    FabricDisplayName = first.FabricName,
                    FabricCode = first.FabricCode,
                    ColorDisplayName = first.ColorName,
                    AvailableRollCount = g.Count(),
                    AvailableMeters = g.Sum(r => r.RemainingLengthMeters),
                    SalePricePerMeter = salePrice
                };
            })
            .OrderBy(o => o.FabricDisplayName)
            .ThenBy(o => o.ColorDisplayName)
            .ToList();

        return ApplicationResult<IReadOnlyList<SalesWarehouseStockOptionDto>>.Success(options);
    }
}
