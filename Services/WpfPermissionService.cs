using ERPSystem.Application.Abstractions.Repositories;
using ERPSystem.Application.Abstractions.Services;
using Microsoft.Extensions.DependencyInjection;

namespace ERPSystem.Services;

public sealed class WpfPermissionService(
    ICurrentUserService currentUser,
    IServiceScopeFactory scopeFactory) : IPermissionService
{
    public async Task<bool> CanAsync(string permissionCode, CancellationToken cancellationToken = default)
    {
        if (currentUser.UserId is not Guid userId)
            return false;

        using var scope = scopeFactory.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<IUserRepository>();
        return await users.HasPermissionAsync(userId, permissionCode, cancellationToken);
    }

    public async Task EnsureCanAsync(string permissionCode, CancellationToken cancellationToken = default)
    {
        if (!await CanAsync(permissionCode, cancellationToken))
            throw new UnauthorizedAccessException($"Permission denied: {permissionCode}");
    }
}
