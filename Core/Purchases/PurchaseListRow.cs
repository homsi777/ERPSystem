using ERPSystem.Application.DTOs.Purchases;
using ERPSystem.Application.Common;

namespace ERPSystem.Core.Purchases;

public sealed class PurchaseListRow
{
    public Guid Id { get; init; }
    public string InvoiceNumber { get; init; } = "";
    public DateTime InvoiceDate { get; init; }
    public DateTime DueDate { get; init; }
    public string SupplierName { get; init; } = "";
    public decimal TotalAmount { get; init; }
    public decimal PaidAmount { get; init; }
    public decimal RemainingAmount { get; init; }
    public string StatusDisplay { get; init; } = "";
    public bool IsOverdue { get; init; }
    public Guid? SourceContainerId { get; init; }
    public string? SourceContainerNumber { get; init; }
    public string SourceDisplay { get; init; } = "محلي";

    public static PurchaseListRow FromDto(PurchaseInvoiceListDto dto) => new()
    {
        Id = dto.Id,
        InvoiceNumber = dto.InvoiceNumber,
        InvoiceDate = dto.InvoiceDate,
        DueDate = dto.DueDate,
        SupplierName = dto.SupplierName,
        TotalAmount = dto.TotalAmount,
        PaidAmount = dto.PaidAmount,
        RemainingAmount = dto.RemainingAmount,
        StatusDisplay = dto.StatusDisplay,
        IsOverdue = dto.IsOverdue,
        SourceContainerId = dto.SourceContainerId,
        SourceContainerNumber = dto.SourceContainerNumber,
        SourceDisplay = dto.SourceDisplay
    };
}
