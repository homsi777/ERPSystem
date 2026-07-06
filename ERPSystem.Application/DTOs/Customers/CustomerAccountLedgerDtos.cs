using ERPSystem.Domain.Enums;

namespace ERPSystem.Application.DTOs.Customers;

public sealed class CustomerAccountLedgerLineDto
{
    public CustomerAccountMovementType MovementType { get; init; }
    public Guid DocumentId { get; init; }
    public Guid EntryId { get; init; }
    public string DocumentNumber { get; init; } = "";
    public DateTime TransactionDate { get; init; }
    public string FabricDescription { get; init; } = "";
    public int? RollCount { get; init; }
    public decimal? TotalMeters { get; init; }
    public decimal? UnitPrice { get; init; }
    public decimal LineAmount { get; init; }
    public string? Notes { get; init; }
    public decimal RunningBalance { get; init; }
}

public sealed class CustomerAccountLedgerDto
{
    public Guid CustomerId { get; init; }
    public string CustomerName { get; init; } = "";
    public decimal OpeningBalance { get; init; }
    public decimal ClosingBalance { get; init; }
    public DateTime? LastReconciliationDate { get; init; }
    public decimal? LastReconciliationBalance { get; init; }
    public Guid? LastReconciliationDocumentId { get; init; }
    public IReadOnlyList<CustomerAccountLedgerLineDto> Lines { get; init; } = [];
}
