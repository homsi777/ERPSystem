namespace ERPSystem.Application.Queries.Customers;

public sealed class GetCustomerAccountLedgerQuery
{
    public Guid CustomerId { get; init; }
    public DateTime? FromDate { get; init; }
    public DateTime? ToDate { get; init; }
}
