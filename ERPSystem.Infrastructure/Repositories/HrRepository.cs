using ERPSystem.Application.Abstractions.Repositories;
using ERPSystem.Domain.Entities.HR;
using ERPSystem.Infrastructure.Persistence;
using ERPSystem.Infrastructure.Persistence.Mapping;
using ERPSystem.Infrastructure.Persistence.Models.Hr;
using Microsoft.EntityFrameworkCore;

namespace ERPSystem.Infrastructure.Repositories;

internal sealed class DepartmentRepository(ErpDbContext context) : IDepartmentRepository
{
    public async Task<IReadOnlyList<Department>> GetListAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        var rows = await context.Departments.AsNoTracking()
            .Where(d => d.CompanyId == companyId && d.IsActive && !d.IsArchived)
            .OrderBy(d => d.Name)
            .ToListAsync(cancellationToken);
        return rows.Select(ToDomain).ToList();
    }

    public async Task<Department?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await context.Departments.AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == id, cancellationToken);
        return entity is null ? null : ToDomain(entity);
    }

    public async Task AddAsync(Department department, CancellationToken cancellationToken = default) =>
        await context.Departments.AddAsync(new DepartmentEntity
        {
            Id = department.Id,
            CompanyId = department.CompanyId,
            Code = department.Code,
            Name = department.Name,
            IsActive = department.IsActive
        }, cancellationToken);

    public async Task UpdateAsync(Department department, CancellationToken cancellationToken = default)
    {
        var entity = await context.Departments.FirstOrDefaultAsync(d => d.Id == department.Id, cancellationToken)
            ?? throw new InvalidOperationException("Department not found.");
        entity.Name = department.Name;
        entity.IsActive = department.IsActive;
        entity.UpdatedAt = DateTime.UtcNow;
    }

    private static Department ToDomain(DepartmentEntity e)
    {
        var d = DomainHydrator.Create<Department>();
        DomainHydrator.Set(d, nameof(Department.Id), e.Id);
        DomainHydrator.Set(d, nameof(Department.CompanyId), e.CompanyId);
        DomainHydrator.Set(d, nameof(Department.Code), e.Code);
        DomainHydrator.Set(d, nameof(Department.Name), e.Name);
        DomainHydrator.Set(d, nameof(Department.IsActive), e.IsActive);
        return d;
    }
}

internal sealed class EmployeeRepository(ErpDbContext context) : IEmployeeRepository
{
    public async Task<IReadOnlyList<Employee>> GetListAsync(Guid companyId, string? search = null, CancellationToken cancellationToken = default)
    {
        var query = context.Employees.AsNoTracking()
            .Where(e => e.CompanyId == companyId && !e.IsArchived);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(e =>
                EF.Functions.ILike(e.FullName, $"%{term}%") ||
                EF.Functions.ILike(e.EmployeeCode, $"%{term}%") ||
                EF.Functions.ILike(e.JobTitle, $"%{term}%"));
        }

        var rows = await query.OrderBy(e => e.FullName).ToListAsync(cancellationToken);
        return rows.Select(ToDomain).ToList();
    }

    public async Task<Employee?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await context.Employees.AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
        return entity is null ? null : ToDomain(entity);
    }

    public async Task AddAsync(Employee employee, CancellationToken cancellationToken = default) =>
        await context.Employees.AddAsync(ToEntity(employee), cancellationToken);

    public async Task UpdateAsync(Employee employee, CancellationToken cancellationToken = default)
    {
        var entity = await context.Employees.FirstOrDefaultAsync(e => e.Id == employee.Id, cancellationToken)
            ?? throw new InvalidOperationException("Employee not found.");
        entity.FullName = employee.FullName;
        entity.DepartmentId = employee.DepartmentId;
        entity.JobTitle = employee.JobTitle;
        entity.Phone = employee.Phone;
        entity.Email = employee.Email;
        entity.Notes = employee.Notes;
        entity.HireDate = UtcDateTimeNormalizer.ToUtc(employee.HireDate);
        entity.BasicSalary = employee.BasicSalary;
        entity.IsActive = employee.IsActive;
        entity.UpdatedAt = DateTime.UtcNow;
    }

    private static EmployeeEntity ToEntity(Employee e) => new()
    {
        Id = e.Id,
        CompanyId = e.CompanyId,
        EmployeeCode = e.EmployeeCode,
        FullName = e.FullName,
        DepartmentId = e.DepartmentId,
        JobTitle = e.JobTitle,
        Phone = e.Phone,
        Email = e.Email,
        Notes = e.Notes,
        HireDate = UtcDateTimeNormalizer.ToUtc(e.HireDate),
        BasicSalary = e.BasicSalary,
        IsActive = e.IsActive
    };

    private static Employee ToDomain(EmployeeEntity e)
    {
        var emp = DomainHydrator.Create<Employee>();
        DomainHydrator.Set(emp, nameof(Employee.Id), e.Id);
        DomainHydrator.Set(emp, nameof(Employee.CompanyId), e.CompanyId);
        DomainHydrator.Set(emp, nameof(Employee.EmployeeCode), e.EmployeeCode);
        DomainHydrator.Set(emp, nameof(Employee.FullName), e.FullName);
        DomainHydrator.Set(emp, nameof(Employee.DepartmentId), e.DepartmentId);
        DomainHydrator.Set(emp, nameof(Employee.JobTitle), e.JobTitle);
        DomainHydrator.Set(emp, nameof(Employee.Phone), e.Phone);
        DomainHydrator.Set(emp, nameof(Employee.Email), e.Email);
        DomainHydrator.Set(emp, nameof(Employee.Notes), e.Notes);
        DomainHydrator.Set(emp, nameof(Employee.HireDate), e.HireDate);
        DomainHydrator.Set(emp, nameof(Employee.BasicSalary), e.BasicSalary);
        DomainHydrator.Set(emp, nameof(Employee.IsActive), e.IsActive);
        return emp;
    }
}
