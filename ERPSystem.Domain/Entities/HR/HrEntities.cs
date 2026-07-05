using ERPSystem.Domain.Enums;

namespace ERPSystem.Domain.Entities.HR;

public class Department
{
    public Guid Id { get; private set; }
    public Guid CompanyId { get; private set; }
    public string Code { get; private set; } = "";
    public string Name { get; private set; } = "";
    public bool IsActive { get; private set; } = true;

    private Department() { }

    public static Department Create(Guid companyId, string code, string name) => new()
    {
        Id = Guid.NewGuid(),
        CompanyId = companyId,
        Code = code,
        Name = name
    };

    public void Rename(string name) => Name = name;
    public void Deactivate() => IsActive = false;
}

public class Employee
{
    public Guid Id { get; private set; }
    public Guid CompanyId { get; private set; }
    public string EmployeeCode { get; private set; } = "";
    public string FullName { get; private set; } = "";
    public Guid? DepartmentId { get; private set; }
    public string JobTitle { get; private set; } = "";
    public string? Phone { get; private set; }
    public string? Email { get; private set; }
    public string? Notes { get; private set; }
    public DateTime HireDate { get; private set; }
    public decimal BasicSalary { get; private set; }
    public bool IsActive { get; private set; } = true;

    private Employee() { }

    public static Employee Create(
        Guid companyId,
        string employeeCode,
        string fullName,
        Guid? departmentId,
        string jobTitle,
        DateTime hireDate,
        decimal basicSalary,
        string? phone = null,
        string? email = null,
        string? notes = null) => new()
    {
        Id = Guid.NewGuid(),
        CompanyId = companyId,
        EmployeeCode = employeeCode,
        FullName = fullName,
        DepartmentId = departmentId,
        JobTitle = jobTitle,
        HireDate = hireDate,
        BasicSalary = basicSalary,
        Phone = phone,
        Email = email,
        Notes = notes
    };

    public void UpdateProfile(
        string fullName,
        Guid? departmentId,
        string jobTitle,
        DateTime hireDate,
        decimal basicSalary,
        string? phone,
        string? email,
        string? notes)
    {
        FullName = fullName;
        DepartmentId = departmentId;
        JobTitle = jobTitle;
        HireDate = hireDate;
        BasicSalary = basicSalary;
        Phone = phone;
        Email = email;
        Notes = notes;
    }

    public void Activate() => IsActive = true;
    public void Deactivate() => IsActive = false;
}

public class Shift
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = "";
    public TimeSpan StartTime { get; private set; }
    public TimeSpan EndTime { get; private set; }

    private Shift() { }

    public static Shift Create(string name, TimeSpan start, TimeSpan end) => new()
    {
        Id = Guid.NewGuid(),
        Name = name,
        StartTime = start,
        EndTime = end
    };
}

public class AttendanceRecord
{
    public Guid Id { get; private set; }
    public Guid EmployeeId { get; private set; }
    public DateTime Date { get; private set; }
    public TimeSpan? CheckIn { get; private set; }
    public TimeSpan? CheckOut { get; private set; }

    private AttendanceRecord() { }

    public static AttendanceRecord Create(Guid employeeId, DateTime date) => new()
    {
        Id = Guid.NewGuid(),
        EmployeeId = employeeId,
        Date = date.Date
    };
}

public class LeaveRequest
{
    public Guid Id { get; private set; }
    public Guid EmployeeId { get; private set; }
    public DateTime StartDate { get; private set; }
    public DateTime EndDate { get; private set; }
    public string LeaveType { get; private set; } = "";
    public ApprovalStatus Status { get; private set; }

    private LeaveRequest() { }

    public static LeaveRequest Create(
        Guid employeeId,
        DateTime start,
        DateTime end,
        string leaveType) => new()
    {
        Id = Guid.NewGuid(),
        EmployeeId = employeeId,
        StartDate = start,
        EndDate = end,
        LeaveType = leaveType,
        Status = ApprovalStatus.Pending
    };
}
