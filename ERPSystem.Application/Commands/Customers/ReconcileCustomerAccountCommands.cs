namespace ERPSystem.Application.Commands.Customers;

public sealed class ReconcileCustomerAccountCommand
{
    public Guid CustomerId { get; init; }
    public DateTime ReconciliationDate { get; init; }
    public Guid DocumentId { get; init; }
    public decimal BalanceAtReconciliation { get; init; }
}
