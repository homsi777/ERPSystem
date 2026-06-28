using ERPSystem.Application.DTOs.Sales;

namespace ERPSystem.Core.Sales;

public sealed class DetailingQueueRow
{
    public Guid InvoiceId { get; init; }
    public string InvoiceNumber { get; init; } = "";
    public string CustomerName { get; init; } = "";
    public string Container { get; init; } = "";
    public int RollCount { get; init; }
    public decimal UnitPrice { get; init; }
    public WarehouseDetailingDto Dto { get; init; } = null!;

    public string StatusDisplay => "بانتظار التفصيل";

    public static DetailingQueueRow FromDto(
        WarehouseDetailingDto dto,
        string containerLabel,
        decimal unitPrice) => new()
    {
        InvoiceId = dto.InvoiceId,
        InvoiceNumber = dto.InvoiceNumber,
        CustomerName = dto.CustomerName,
        Container = containerLabel,
        RollCount = dto.Rolls.Count,
        UnitPrice = unitPrice,
        Dto = dto
    };
}
