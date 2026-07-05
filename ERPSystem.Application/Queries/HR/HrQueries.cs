namespace ERPSystem.Application.Queries.HR;

public sealed class GetEmployeeListQuery
{
    public Guid CompanyId { get; init; }
    public string? Search { get; init; }
}

public sealed class GetEmployeeDetailsQuery
{
    public Guid EmployeeId { get; init; }
}

public sealed class GetDepartmentListQuery
{
    public Guid CompanyId { get; init; }
}
