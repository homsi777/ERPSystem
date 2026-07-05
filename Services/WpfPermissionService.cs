using ERPSystem.Application.Abstractions.Services;
using ERPSystem.Application.Services;

namespace ERPSystem.Services;

public sealed class WpfPermissionService(PermissionService permissionService) : IPermissionService
{
    public Task<bool> CanAsync(string permissionCode, CancellationToken cancellationToken = default) =>
        permissionService.CanAsync(permissionCode, cancellationToken);

    public Task EnsureCanAsync(string permissionCode, CancellationToken cancellationToken = default) =>
        permissionService.EnsureCanAsync(permissionCode, cancellationToken);
}
