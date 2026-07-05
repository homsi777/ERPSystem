using ERPSystem.Application.Abstractions.Repositories;
using ERPSystem.Application.Abstractions.Services;
using ERPSystem.Application.DTOs.Identity;
using ERPSystem.Application.Queries.Identity;
using ERPSystem.Application.UseCases.Identity;
using ERPSystem.Domain.Entities.Identity;
using ERPSystem.Infrastructure.Security;

namespace ERPSystem.Application.Tests.UseCases.Identity;

public sealed class AuthenticateUserHandlerTests
{
    private readonly BcryptPasswordHasher _passwordHasher = new();
    private const string Username = "admin";
    private const string Password = "Admin@123";

    [Fact]
    public async Task HandleAsync_WithValidPassword_ReturnsSuccess()
    {
        var repository = CreateRepository(_passwordHasher.HashPassword(Password));
        var handler = new AuthenticateUserHandler(repository, _passwordHasher);

        var result = await handler.HandleAsync(new AuthenticateUserQuery
        {
            Username = Username,
            Password = Password
        });

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(Username, result.Value!.Username);
    }

    [Fact]
    public async Task HandleAsync_WithInvalidPassword_ReturnsFailure()
    {
        var repository = CreateRepository(_passwordHasher.HashPassword(Password));
        var handler = new AuthenticateUserHandler(repository, _passwordHasher);

        var result = await handler.HandleAsync(new AuthenticateUserQuery
        {
            Username = Username,
            Password = "WrongPassword!"
        });

        Assert.False(result.IsSuccess);
        Assert.Equal("Invalid username or password.", result.ErrorMessage);
    }

    private static FakeUserRepository CreateRepository(string passwordHash) =>
        new(new UserCredentialDto
        {
            UserId = Guid.Parse("33333333-3333-3333-3333-333333333333"),
            Username = Username,
            FullNameAr = "مدير النظام",
            PasswordHash = passwordHash,
            IsActive = true
        });

    private sealed class FakeUserRepository(UserCredentialDto credential) : IUserRepository
    {
        public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult<User?>(null);

        public Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default) =>
            Task.FromResult<User?>(null);

        public Task<UserCredentialDto?> GetCredentialByUsernameAsync(string username, CancellationToken cancellationToken = default) =>
            Task.FromResult(username == credential.Username ? credential : (UserCredentialDto?)null);

        public Task<IReadOnlyList<Role>> GetRolesForUserAsync(Guid userId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Role>>([Role.Create("Administrator", "Full system access", true)]);

        public Task<IReadOnlyList<string>> GetPermissionCodesForUserAsync(Guid userId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<string>>(["sales.create"]);

        public Task<bool> HasPermissionAsync(Guid userId, string permissionCode, CancellationToken cancellationToken = default) =>
            Task.FromResult(false);
    }
}
