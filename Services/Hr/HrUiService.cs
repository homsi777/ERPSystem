using ERPSystem.Application.Abstractions;
using ERPSystem.Application.Abstractions.Services;
using ERPSystem.Application.Commands.HR;
using ERPSystem.Application.DTOs.HR;
using ERPSystem.Application.Queries.HR;
using ERPSystem.Application.Results;
using ERPSystem.Application.UseCases.HR;
using Microsoft.Extensions.DependencyInjection;

namespace ERPSystem.Services.Hr;

public static class HrNavigationContext
{
    public static Guid? EditEmployeeId { get; set; }
    public static Guid? EditDepartmentId { get; set; }

    public static void BeginCreateEmployee() => EditEmployeeId = null;
    public static void BeginEditEmployee(Guid id) => EditEmployeeId = id;
    public static void BeginCreateDepartment() => EditDepartmentId = null;
    public static void BeginEditDepartment(Guid id) => EditDepartmentId = id;
}

public static class HrListRefreshHub
{
    public static event EventHandler? RefreshRequested;
    public static void RequestRefresh() => RefreshRequested?.Invoke(null, EventArgs.Empty);
}

public sealed class HrUiService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ICurrentBranchService _branch;

    public HrUiService(IServiceScopeFactory scopeFactory, ICurrentBranchService branch)
    {
        _scopeFactory = scopeFactory;
        _branch = branch;
    }

    public static HrUiService Instance => AppServices.GetRequiredService<HrUiService>();

    private Guid CompanyId =>
        _branch.CompanyId ?? throw new InvalidOperationException("Company context is not set.");

    private Guid BranchId =>
        _branch.BranchId ?? throw new InvalidOperationException("Branch context is not set.");

    public async Task<ApplicationResult<IReadOnlyList<EmployeeListDto>>> GetEmployeesAsync(
        string? search = null, CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetEmployeeListHandler>();
        return await handler.HandleAsync(new GetEmployeeListQuery { CompanyId = CompanyId, Search = search }, cancellationToken);
    }

    public async Task<ApplicationResult<EmployeeDetailsDto>> GetEmployeeAsync(
        Guid employeeId, CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetEmployeeDetailsHandler>();
        return await handler.HandleAsync(new GetEmployeeDetailsQuery { EmployeeId = employeeId }, cancellationToken);
    }

    public async Task<ApplicationResult<IReadOnlyList<DepartmentListDto>>> GetDepartmentsAsync(
        CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetDepartmentListHandler>();
        return await handler.HandleAsync(new GetDepartmentListQuery { CompanyId = CompanyId }, cancellationToken);
    }

    public async Task<string> NextEmployeeCodeAsync(CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var numbering = scope.ServiceProvider.GetRequiredService<INumberingService>();
        return await numbering.NextEmployeeCodeAsync(BranchId, cancellationToken);
    }

    public async Task<string> NextDepartmentCodeAsync(CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var numbering = scope.ServiceProvider.GetRequiredService<INumberingService>();
        return await numbering.NextDepartmentCodeAsync(BranchId, cancellationToken);
    }

    public async Task<ApplicationResult<Guid>> CreateEmployeeAsync(
        string employeeCode,
        string fullName,
        Guid? departmentId,
        string jobTitle,
        string? phone,
        string? email,
        string? notes,
        DateTime hireDate,
        decimal basicSalary,
        CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<ICommandHandler<CreateEmployeeCommand, ApplicationResult<Guid>>>();
        return await handler.HandleAsync(new CreateEmployeeCommand
        {
            CompanyId = CompanyId,
            EmployeeCode = employeeCode,
            FullName = fullName,
            DepartmentId = departmentId,
            JobTitle = jobTitle,
            Phone = phone,
            Email = email,
            Notes = notes,
            HireDate = hireDate,
            BasicSalary = basicSalary
        }, cancellationToken);
    }

    public async Task<ApplicationResult> UpdateEmployeeAsync(UpdateEmployeeCommand command, CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<ICommandHandler<UpdateEmployeeCommand, ApplicationResult>>();
        return await handler.HandleAsync(command, cancellationToken);
    }

    public async Task<ApplicationResult<Guid>> CreateDepartmentAsync(string code, string name, CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<ICommandHandler<CreateDepartmentCommand, ApplicationResult<Guid>>>();
        return await handler.HandleAsync(new CreateDepartmentCommand { CompanyId = CompanyId, Code = code, Name = name }, cancellationToken);
    }

    public async Task<ApplicationResult> UpdateDepartmentAsync(Guid departmentId, string name, CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<ICommandHandler<UpdateDepartmentCommand, ApplicationResult>>();
        return await handler.HandleAsync(new UpdateDepartmentCommand { DepartmentId = departmentId, Name = name }, cancellationToken);
    }

    public Guid ResolveCompanyId() => CompanyId;
}
