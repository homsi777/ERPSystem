namespace ERPSystem.Infrastructure.Persistence.Models.Hr;

public class DepartmentEntity : PersistenceEntity
{
    public Guid CompanyId { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
}

public class EmployeeEntity : PersistenceEntity
{
    public Guid CompanyId { get; set; }
    public string EmployeeCode { get; set; } = "";
    public string FullName { get; set; } = "";
    public Guid? DepartmentId { get; set; }
    public string JobTitle { get; set; } = "";
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Notes { get; set; }
    public DateTime HireDate { get; set; }
    public decimal BasicSalary { get; set; }
}
