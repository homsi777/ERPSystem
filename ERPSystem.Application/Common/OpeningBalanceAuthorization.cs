using ERPSystem.Application.Abstractions.Repositories;
using ERPSystem.Application.Abstractions.Services;
using ERPSystem.Domain.Entities.Finance;

namespace ERPSystem.Application.Common;

/// <summary>
/// Opening-stock documents in Inventory may be authorized via finance opening-balance
/// permissions or warehouse opening-stock permissions.
/// </summary>
public static class OpeningBalanceAuthorization
{
    public const string InventoryOpeningStockPermission = "warehouse.opening-stock";
    private const string LegacyInventoryOpeningStockPermission = "warehouse.detailing";

    public static Task<bool> CanCreateAsync(
        IPermissionService permissions,
        OpeningBalanceType type,
        CancellationToken cancellationToken = default) =>
        type == OpeningBalanceType.OpeningStock
            ? HasInventoryOpeningStockAccessAsync(permissions, "openingbalances.create", cancellationToken)
            : permissions.CanAsync("openingbalances.create", cancellationToken);

    public static Task<bool> CanEditAsync(
        IPermissionService permissions,
        OpeningBalanceType type,
        CancellationToken cancellationToken = default) =>
        type == OpeningBalanceType.OpeningStock
            ? HasInventoryOpeningStockAccessAsync(permissions, "openingbalances.edit", cancellationToken)
            : permissions.CanAsync("openingbalances.edit", cancellationToken);

    public static async Task<bool> CanWorkflowAsync(
        IPermissionService permissions,
        IOpeningBalanceRepository repository,
        Guid documentId,
        string openingBalancePermission,
        CancellationToken cancellationToken = default)
    {
        var doc = await repository.GetAsync(documentId, cancellationToken);
        if (doc?.Type == OpeningBalanceType.OpeningStock)
            return await HasInventoryOpeningStockAccessAsync(permissions, openingBalancePermission, cancellationToken);

        return await permissions.CanAsync(openingBalancePermission, cancellationToken);
    }

    private static async Task<bool> HasInventoryOpeningStockAccessAsync(
        IPermissionService permissions,
        string openingBalancePermission,
        CancellationToken cancellationToken) =>
        await permissions.CanAsync(openingBalancePermission, cancellationToken)
        || await permissions.CanAsync(InventoryOpeningStockPermission, cancellationToken)
        || await permissions.CanAsync(LegacyInventoryOpeningStockPermission, cancellationToken);
}
