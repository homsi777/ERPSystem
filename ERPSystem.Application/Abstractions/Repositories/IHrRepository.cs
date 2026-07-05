using ERPSystem.Domain.Entities.HR;

namespace ERPSystem.Application.Abstractions.Repositories;

public interface IDepartmentRepository
{
    Task<IReadOnlyList<Department>> GetListAsync(Guid companyId, CancellationToken cancellationToken = default);
    Task<Department?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddAsync(Department department, CancellationToken cancellationToken = default);
    Task UpdateAsync(Department department, CancellationToken cancellationToken = default);
}

public interface IEmployeeRepository
{
    Task<IReadOnlyList<Employee>> GetListAsync(Guid companyId, string? search = null, CancellationToken cancellationToken = default);
    Task<Employee?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddAsync(Employee employee, CancellationToken cancellationToken = default);
    Task UpdateAsync(Employee employee, CancellationToken cancellationToken = default);
}
