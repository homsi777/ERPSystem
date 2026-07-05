using ERPSystem.Application.Abstractions.Repositories;
using ERPSystem.Application.Abstractions.Services;

namespace ERPSystem.Application.Services;

public sealed class PermissionService(
    ICurrentUserService currentUser,
    IUserRepository userRepository) : IPermissionService
{
    public async Task<bool> CanAsync(string permissionCode, CancellationToken cancellationToken = default)
    {
        if (currentUser.UserId is not Guid userId)
            return false;

        return await userRepository.HasPermissionAsync(userId, permissionCode, cancellationToken);
    }

    public async Task EnsureCanAsync(string permissionCode, CancellationToken cancellationToken = default)
    {
        if (!await CanAsync(permissionCode, cancellationToken))
            throw new UnauthorizedAccessException($"Permission denied: {permissionCode}");
    }
}
