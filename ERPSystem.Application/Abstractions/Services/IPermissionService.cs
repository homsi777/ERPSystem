namespace ERPSystem.Application.Abstractions.Services;

public interface IPermissionService
{
    Task<bool> CanAsync(string permissionCode, CancellationToken cancellationToken = default);
    Task EnsureCanAsync(string permissionCode, CancellationToken cancellationToken = default);
}
