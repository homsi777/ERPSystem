using ERPSystem.Application.Abstractions.Services;

namespace ERPSystem.Api.Services;

internal sealed class ApiCurrentUserService : ICurrentUserService
{
    public Guid? UserId => null;
    public string? Username => null;
    public bool IsAuthenticated => false;
}

internal sealed class ApiCurrentBranchService : ICurrentBranchService
{
    public Guid? CompanyId => null;
    public Guid? BranchId => null;
}

internal sealed class ApiPermissionService : IPermissionService
{
    public Task<bool> CanAsync(string permissionCode, CancellationToken cancellationToken = default) =>
        Task.FromResult(false);

    public Task EnsureCanAsync(string permissionCode, CancellationToken cancellationToken = default) =>
        throw new UnauthorizedAccessException($"Permission denied: {permissionCode}");
}
