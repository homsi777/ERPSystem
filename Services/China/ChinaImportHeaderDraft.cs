namespace ERPSystem.Services.China;

public sealed class ChinaImportHeaderDraft
{
    public string ContainerNumber { get; init; } = "";
    public Guid SupplierId { get; init; }
    public DateTime ShipmentDate { get; init; }
    public DateTime? ExpectedArrival { get; init; }
    public decimal ExchangeRateToLocalCurrency { get; init; } = 1m;
    public string? Notes { get; init; }
}
