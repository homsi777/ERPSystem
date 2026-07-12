using ERPSystem.Domain.Aggregates;

namespace ERPSystem.Application.Abstractions.Services;

/// <summary>
/// Creates and posts purchase invoices from approved China import containers (financial mirror).
/// </summary>
public interface IChinaContainerPurchaseBridgeService
{
    /// <summary>
    /// Idempotent: returns existing linked invoice id or creates + posts a new purchase invoice.
    /// When <paramref name="skipGeneralLedger"/> is true, only supplier balance + document are updated (backfill).
    /// </summary>
    Task<Guid?> EnsurePostedPurchaseInvoiceAsync(
        ContainerAggregate container,
        Guid userId,
        bool skipGeneralLedger = false,
        CancellationToken cancellationToken = default);

    /// <summary>Backfill purchase invoices for approved containers missing a linked invoice.</summary>
    Task<ChinaContainerPurchaseBridgeBackfillResult> BackfillApprovedContainersAsync(
        Guid companyId,
        Guid userId,
        CancellationToken cancellationToken = default);
}

public sealed class ChinaContainerPurchaseBridgeBackfillResult
{
    public int Processed { get; init; }
    public int Created { get; init; }
    public int SkippedExisting { get; init; }
    public int SkippedNoAmount { get; init; }
    public IReadOnlyList<string> Messages { get; init; } = [];
}
