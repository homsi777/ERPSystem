using ERPSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ERPSystem.Infrastructure.Seed;

/// <summary>
/// Removes seeded/imported fabric catalog, containers, and dependent inventory/sales rows
/// so import workflows can be tested from a clean slate.
/// </summary>
public static class ImportCatalogDevelopmentCleanup
{
    public static async Task RunAsync(
        ErpDbContext context,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        logger.LogWarning("Starting import/catalog development cleanup...");

        await context.SalesInvoiceRollDetails.ExecuteDeleteAsync(cancellationToken);
        await context.SalesInvoiceItems.ExecuteDeleteAsync(cancellationToken);
        await context.WarehouseDetailingSessions.ExecuteDeleteAsync(cancellationToken);
        await context.SalesInvoices.IgnoreQueryFilters().ExecuteDeleteAsync(cancellationToken);

        await context.PurchaseReturnLines.ExecuteDeleteAsync(cancellationToken);
        await context.PurchaseReturns.ExecuteDeleteAsync(cancellationToken);
        await context.PurchaseInvoicePayments.ExecuteDeleteAsync(cancellationToken);
        await context.PurchaseInvoiceItems.ExecuteDeleteAsync(cancellationToken);
        await context.PurchaseInvoices.ExecuteDeleteAsync(cancellationToken);
        await context.PurchaseOrderLines.ExecuteDeleteAsync(cancellationToken);
        await context.PurchaseOrders.ExecuteDeleteAsync(cancellationToken);

        await context.InventoryReservations.ExecuteDeleteAsync(cancellationToken);
        await context.InventoryAlerts.ExecuteDeleteAsync(cancellationToken);
        await context.InventoryAuditLogs.ExecuteDeleteAsync(cancellationToken);
        await context.InventoryTimelineEvents.ExecuteDeleteAsync(cancellationToken);
        await context.InventoryValuationSnapshots.ExecuteDeleteAsync(cancellationToken);
        await context.OpeningStockLines.ExecuteDeleteAsync(cancellationToken);
        await context.OpeningStockDocuments.ExecuteDeleteAsync(cancellationToken);
        await context.StocktakeLines.ExecuteDeleteAsync(cancellationToken);
        await context.StocktakeSessions.ExecuteDeleteAsync(cancellationToken);
        await context.StockTransferLines.ExecuteDeleteAsync(cancellationToken);
        await context.StockTransfers.ExecuteDeleteAsync(cancellationToken);
        await context.StockMovementLines.ExecuteDeleteAsync(cancellationToken);
        await context.StockMovements.ExecuteDeleteAsync(cancellationToken);
        await context.WarehouseStocks.ExecuteDeleteAsync(cancellationToken);
        await context.FabricRolls.ExecuteDeleteAsync(cancellationToken);
        await context.FabricBatches.ExecuteDeleteAsync(cancellationToken);

        await context.ContainerDistributions.ExecuteDeleteAsync(cancellationToken);
        await context.ContainerFabricTypeLines.ExecuteDeleteAsync(cancellationToken);
        await context.ContainerItems.ExecuteDeleteAsync(cancellationToken);
        await context.LandingCostExpenses.ExecuteDeleteAsync(cancellationToken);
        await context.LandingCosts.ExecuteDeleteAsync(cancellationToken);
        await context.ImportBatches.ExecuteDeleteAsync(cancellationToken);
        await context.Containers.IgnoreQueryFilters().ExecuteDeleteAsync(cancellationToken);
        await context.FabricTypeAliases.ExecuteDeleteAsync(cancellationToken);

        await context.FabricColors.ExecuteDeleteAsync(cancellationToken);
        await context.FabricItems.ExecuteDeleteAsync(cancellationToken);
        await context.FabricCategories.ExecuteDeleteAsync(cancellationToken);

        var containerCounters = await context.DocumentCounters
            .Where(c => c.DocumentType == "Container")
            .ToListAsync(cancellationToken);
        foreach (var counter in containerCounters)
            counter.LastNumber = 0;

        await context.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Import/catalog cleanup completed — fabric catalog, containers, inventory fabric rows, and related sales/purchases removed.");
    }
}
