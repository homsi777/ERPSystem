using ERPSystem.Application.Abstractions.Repositories;
using ERPSystem.Application.DTOs.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace ERPSystem.Services.Identity;

public sealed class UserSessionUiService
{
    private readonly IServiceScopeFactory _scopeFactory;

    public UserSessionUiService(IServiceScopeFactory scopeFactory) => _scopeFactory = scopeFactory;

    public static UserSessionUiService Instance => AppServices.GetRequiredService<UserSessionUiService>();

    public async Task<IReadOnlyList<UserSessionStatusDto>> GetHistoryAsync(
        int limit = 200,
        CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IUserSessionRepository>();
        return await repository.GetHistoryAsync(limit, cancellationToken);
    }
}
