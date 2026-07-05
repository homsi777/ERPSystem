namespace ERPSystem.Application.DTOs.HR;

public sealed class EmployeeListDto
{
    public Guid Id { get; init; }
    public string EmployeeCode { get; init; } = "";
    public string FullName { get; init; } = "";
    public Guid? DepartmentId { get; init; }
    public string DepartmentName { get; init; } = "";
    public string JobTitle { get; init; } = "";
    public string? Phone { get; init; }
    public DateTime HireDate { get; init; }
    public decimal BasicSalary { get; init; }
    public bool IsActive { get; init; }
    public string StatusDisplay => IsActive ? "نشط" : "غير نشط";
}

public sealed class EmployeeDetailsDto
{
    public Guid Id { get; init; }
    public string EmployeeCode { get; init; } = "";
    public string FullName { get; init; } = "";
    public Guid? DepartmentId { get; init; }
    public string DepartmentName { get; init; } = "";
    public string JobTitle { get; init; } = "";
    public string? Phone { get; init; }
    public string? Email { get; init; }
    public string? Notes { get; init; }
    public DateTime HireDate { get; init; }
    public decimal BasicSalary { get; init; }
    public bool IsActive { get; init; }
}

public sealed class DepartmentListDto
{
    public Guid Id { get; init; }
    public string Code { get; init; } = "";
    public string Name { get; init; } = "";
    public bool IsActive { get; init; }
}
