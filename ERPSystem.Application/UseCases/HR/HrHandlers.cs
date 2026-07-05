using ERPSystem.Application.Abstractions;
using ERPSystem.Application.Abstractions.Repositories;
using ERPSystem.Application.Abstractions.Services;
using ERPSystem.Application.Commands.HR;
using ERPSystem.Application.Common;
using ERPSystem.Application.DTOs.HR;
using ERPSystem.Application.Queries.HR;
using ERPSystem.Application.Results;
using ERPSystem.Domain.Entities.HR;

namespace ERPSystem.Application.UseCases.HR;

public sealed class CreateEmployeeHandler(
    IEmployeeRepository employeeRepository,
    IUnitOfWork unitOfWork,
    IPermissionService permissionService)
    : ICommandHandler<CreateEmployeeCommand, ApplicationResult<Guid>>
{
    public async Task<ApplicationResult<Guid>> HandleAsync(
        CreateEmployeeCommand command, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command.FullName))
            return ApplicationResult<Guid>.ValidationFailed(nameof(command.FullName), "اسم الموظف مطلوب.");
        if (!await permissionService.CanAsync("hr.employee.manage", cancellationToken))
            return ApplicationResult<Guid>.PermissionDenied("Not allowed to manage employees.");

        try
        {
            var employee = Employee.Create(
                command.CompanyId,
                command.EmployeeCode,
                command.FullName,
                command.DepartmentId,
                command.JobTitle,
                command.HireDate,
                command.BasicSalary,
                command.Phone,
                command.Email,
                command.Notes);

            await employeeRepository.AddAsync(employee, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return ApplicationResult<Guid>.Success(employee.Id);
        }
        catch (Exception ex)
        {
            return ex.ToFailureResult<Guid>();
        }
    }
}

public sealed class UpdateEmployeeHandler(
    IEmployeeRepository employeeRepository,
    IUnitOfWork unitOfWork,
    IPermissionService permissionService)
    : ICommandHandler<UpdateEmployeeCommand, ApplicationResult>
{
    public async Task<ApplicationResult> HandleAsync(
        UpdateEmployeeCommand command, CancellationToken cancellationToken = default)
    {
        if (command.EmployeeId == Guid.Empty)
            return ApplicationResult.ValidationFailed(nameof(command.EmployeeId), "الموظف مطلوب.");
        if (string.IsNullOrWhiteSpace(command.FullName))
            return ApplicationResult.ValidationFailed(nameof(command.FullName), "اسم الموظف مطلوب.");
        if (!await permissionService.CanAsync("hr.employee.manage", cancellationToken))
            return ApplicationResult.PermissionDenied("Not allowed to manage employees.");

        var employee = await employeeRepository.GetByIdAsync(command.EmployeeId, cancellationToken);
        if (employee is null)
            return ApplicationResult.NotFound("Employee not found.");

        try
        {
            employee.UpdateProfile(
                command.FullName,
                command.DepartmentId,
                command.JobTitle,
                command.HireDate,
                command.BasicSalary,
                command.Phone,
                command.Email,
                command.Notes);
            if (command.IsActive) employee.Activate(); else employee.Deactivate();

            await employeeRepository.UpdateAsync(employee, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return ApplicationResult.Success();
        }
        catch (Exception ex)
        {
            return ex.ToFailureResult();
        }
    }
}

public sealed class GetEmployeeListHandler(
    IEmployeeRepository employeeRepository,
    IDepartmentRepository departmentRepository)
    : IQueryHandler<GetEmployeeListQuery, ApplicationResult<IReadOnlyList<EmployeeListDto>>>
{
    public async Task<ApplicationResult<IReadOnlyList<EmployeeListDto>>> HandleAsync(
        GetEmployeeListQuery query, CancellationToken cancellationToken = default)
    {
        var employees = await employeeRepository.GetListAsync(query.CompanyId, query.Search, cancellationToken);
        var departments = await departmentRepository.GetListAsync(query.CompanyId, cancellationToken);
        var deptMap = departments.ToDictionary(d => d.Id, d => d.Name);

        var list = employees.Select(e => new EmployeeListDto
        {
            Id = e.Id,
            EmployeeCode = e.EmployeeCode,
            FullName = e.FullName,
            DepartmentId = e.DepartmentId,
            DepartmentName = e.DepartmentId is Guid did && deptMap.TryGetValue(did, out var n) ? n : "—",
            JobTitle = e.JobTitle,
            Phone = e.Phone,
            HireDate = e.HireDate,
            BasicSalary = e.BasicSalary,
            IsActive = e.IsActive
        }).ToList();

        return ApplicationResult<IReadOnlyList<EmployeeListDto>>.Success(list);
    }
}

public sealed class GetEmployeeDetailsHandler(
    IEmployeeRepository employeeRepository,
    IDepartmentRepository departmentRepository)
    : IQueryHandler<GetEmployeeDetailsQuery, ApplicationResult<EmployeeDetailsDto>>
{
    public async Task<ApplicationResult<EmployeeDetailsDto>> HandleAsync(
        GetEmployeeDetailsQuery query, CancellationToken cancellationToken = default)
    {
        var e = await employeeRepository.GetByIdAsync(query.EmployeeId, cancellationToken);
        if (e is null)
            return ApplicationResult<EmployeeDetailsDto>.NotFound("Employee not found.");

        var deptName = "—";
        if (e.DepartmentId is Guid did)
        {
            var dept = await departmentRepository.GetByIdAsync(did, cancellationToken);
            deptName = dept?.Name ?? "—";
        }

        return ApplicationResult<EmployeeDetailsDto>.Success(new EmployeeDetailsDto
        {
            Id = e.Id,
            EmployeeCode = e.EmployeeCode,
            FullName = e.FullName,
            DepartmentId = e.DepartmentId,
            DepartmentName = deptName,
            JobTitle = e.JobTitle,
            Phone = e.Phone,
            Email = e.Email,
            Notes = e.Notes,
            HireDate = e.HireDate,
            BasicSalary = e.BasicSalary,
            IsActive = e.IsActive
        });
    }
}

public sealed class CreateDepartmentHandler(
    IDepartmentRepository departmentRepository,
    IUnitOfWork unitOfWork,
    IPermissionService permissionService)
    : ICommandHandler<CreateDepartmentCommand, ApplicationResult<Guid>>
{
    public async Task<ApplicationResult<Guid>> HandleAsync(
        CreateDepartmentCommand command, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command.Name))
            return ApplicationResult<Guid>.ValidationFailed(nameof(command.Name), "اسم القسم مطلوب.");
        if (!await permissionService.CanAsync("hr.department.manage", cancellationToken))
            return ApplicationResult<Guid>.PermissionDenied("Not allowed to manage departments.");

        try
        {
            var department = Department.Create(command.CompanyId, command.Code, command.Name);
            await departmentRepository.AddAsync(department, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return ApplicationResult<Guid>.Success(department.Id);
        }
        catch (Exception ex)
        {
            return ex.ToFailureResult<Guid>();
        }
    }
}

public sealed class UpdateDepartmentHandler(
    IDepartmentRepository departmentRepository,
    IUnitOfWork unitOfWork,
    IPermissionService permissionService)
    : ICommandHandler<UpdateDepartmentCommand, ApplicationResult>
{
    public async Task<ApplicationResult> HandleAsync(
        UpdateDepartmentCommand command, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command.Name))
            return ApplicationResult.ValidationFailed(nameof(command.Name), "اسم القسم مطلوب.");
        if (!await permissionService.CanAsync("hr.department.manage", cancellationToken))
            return ApplicationResult.PermissionDenied("Not allowed to manage departments.");

        var department = await departmentRepository.GetByIdAsync(command.DepartmentId, cancellationToken);
        if (department is null)
            return ApplicationResult.NotFound("Department not found.");

        try
        {
            department.Rename(command.Name);
            await departmentRepository.UpdateAsync(department, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return ApplicationResult.Success();
        }
        catch (Exception ex)
        {
            return ex.ToFailureResult();
        }
    }
}

public sealed class GetDepartmentListHandler(IDepartmentRepository departmentRepository)
    : IQueryHandler<GetDepartmentListQuery, ApplicationResult<IReadOnlyList<DepartmentListDto>>>
{
    public async Task<ApplicationResult<IReadOnlyList<DepartmentListDto>>> HandleAsync(
        GetDepartmentListQuery query, CancellationToken cancellationToken = default)
    {
        var departments = await departmentRepository.GetListAsync(query.CompanyId, cancellationToken);
        var list = departments.Select(d => new DepartmentListDto
        {
            Id = d.Id,
            Code = d.Code,
            Name = d.Name,
            IsActive = d.IsActive
        }).ToList();
        return ApplicationResult<IReadOnlyList<DepartmentListDto>>.Success(list);
    }
}
