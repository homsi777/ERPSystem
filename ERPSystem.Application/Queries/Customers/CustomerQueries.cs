namespace ERPSystem.Application.Queries.Customers;

public sealed class GetCustomerListQuery
{
    public Guid CompanyId { get; init; }
    public string? Search { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 50;
}

public sealed class GetCustomerOperationsCenterQuery
{
    public Guid CustomerId { get; init; }
}

public sealed class GetCustomerStatementQuery
{
    public Guid CustomerId { get; init; }
    public DateTime? FromDate { get; init; }
    public DateTime? ToDate { get; init; }
}
