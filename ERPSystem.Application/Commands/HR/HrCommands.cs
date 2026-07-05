namespace ERPSystem.Application.Commands.HR;

public sealed class CreateEmployeeCommand
{
    public Guid CompanyId { get; init; }
    public string EmployeeCode { get; init; } = "";
    public string FullName { get; init; } = "";
    public Guid? DepartmentId { get; init; }
    public string JobTitle { get; init; } = "";
    public string? Phone { get; init; }
    public string? Email { get; init; }
    public string? Notes { get; init; }
    public DateTime HireDate { get; init; }
    public decimal BasicSalary { get; init; }
}

public sealed class UpdateEmployeeCommand
{
    public Guid EmployeeId { get; init; }
    public string FullName { get; init; } = "";
    public Guid? DepartmentId { get; init; }
    public string JobTitle { get; init; } = "";
    public string? Phone { get; init; }
    public string? Email { get; init; }
    public string? Notes { get; init; }
    public DateTime HireDate { get; init; }
    public decimal BasicSalary { get; init; }
    public bool IsActive { get; init; } = true;
}

public sealed class CreateDepartmentCommand
{
    public Guid CompanyId { get; init; }
    public string Code { get; init; } = "";
    public string Name { get; init; } = "";
}

public sealed class UpdateDepartmentCommand
{
    public Guid DepartmentId { get; init; }
    public string Name { get; init; } = "";
}
