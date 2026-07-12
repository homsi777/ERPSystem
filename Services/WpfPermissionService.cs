using ERPSystem.Application.Abstractions.Services;
using ERPSystem.Application.Services;

namespace ERPSystem.Services;

public sealed class WpfPermissionService(PermissionService permissionService) : IPermissionService
{
    // TEMP (testing): full-access mode — every permission check passes.
    // To restore real role-based permissions, set BypassAllPermissions = false.
    private const bool BypassAllPermissions = false;

    public Task<bool> CanAsync(string permissionCode, CancellationToken cancellationToken = default) =>
        BypassAllPermissions
            ? Task.FromResult(true)
            : permissionService.CanAsync(permissionCode, cancellationToken);

    public Task EnsureCanAsync(string permissionCode, CancellationToken cancellationToken = default) =>
        BypassAllPermissions
            ? Task.CompletedTask
            : permissionService.EnsureCanAsync(permissionCode, cancellationToken);
}
