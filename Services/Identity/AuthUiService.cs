using ERPSystem.Application.DTOs.Identity;
using ERPSystem.Application.Queries.Identity;
using ERPSystem.Application.Results;
using ERPSystem.Application.UseCases.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace ERPSystem.Services.Identity;

public sealed class AuthUiService
{
    private readonly IServiceScopeFactory _scopeFactory;

    public AuthUiService(IServiceScopeFactory scopeFactory) => _scopeFactory = scopeFactory;

    public static AuthUiService Instance => AppServices.GetRequiredService<AuthUiService>();

    public async Task<ApplicationResult<AuthenticatedUserDto>> LoginAsync(
        string username,
        string password,
        CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<AuthenticateUserHandler>();
        return await handler.HandleAsync(new AuthenticateUserQuery
        {
            Username = username,
            Password = password
        }, cancellationToken);
    }
}
