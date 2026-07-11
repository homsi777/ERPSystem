namespace ERPSystem.Application.DTOs.Accounting;

public sealed class ReceivablesAgingRowDto
{
    public Guid CustomerId { get; init; }
    public string CustomerCode { get; init; } = "";
    public string CustomerName { get; init; } = "";
    public decimal TotalInvoiced { get; init; }
    public decimal Collected { get; init; }
    public decimal Outstanding { get; init; }
    public DateTime? OldestInvoiceDate { get; init; }
    public int DaysOverdue { get; init; }
}
