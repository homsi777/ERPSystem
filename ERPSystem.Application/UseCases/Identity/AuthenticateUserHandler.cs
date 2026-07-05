using ERPSystem.Application.Abstractions;
using ERPSystem.Application.Abstractions.Repositories;
using ERPSystem.Application.Abstractions.Services;
using ERPSystem.Application.DTOs.Identity;
using ERPSystem.Application.Queries.Identity;
using ERPSystem.Application.Results;

namespace ERPSystem.Application.UseCases.Identity;

public sealed class AuthenticateUserHandler(
    IUserRepository userRepository,
    IPasswordHasher passwordHasher)
    : IQueryHandler<AuthenticateUserQuery, ApplicationResult<AuthenticatedUserDto>>
{
    private const string InvalidCredentialsMessage = "Invalid username or password.";

    public async Task<ApplicationResult<AuthenticatedUserDto>> HandleAsync(
        AuthenticateUserQuery query,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query.Username) || string.IsNullOrWhiteSpace(query.Password))
            return ApplicationResult<AuthenticatedUserDto>.Failure(InvalidCredentialsMessage);

        var credential = await userRepository.GetCredentialByUsernameAsync(query.Username.Trim(), cancellationToken);
        if (credential is null || !credential.IsActive)
            return ApplicationResult<AuthenticatedUserDto>.Failure(InvalidCredentialsMessage);

        if (!passwordHasher.VerifyPassword(query.Password, credential.PasswordHash))
            return ApplicationResult<AuthenticatedUserDto>.Failure(InvalidCredentialsMessage);

        var roles = await userRepository.GetRolesForUserAsync(credential.UserId, cancellationToken);
        var permissions = await userRepository.GetPermissionCodesForUserAsync(credential.UserId, cancellationToken);

        return ApplicationResult<AuthenticatedUserDto>.Success(new AuthenticatedUserDto
        {
            UserId = credential.UserId,
            Username = credential.Username,
            FullNameAr = credential.FullNameAr,
            Roles = roles.Select(r => r.Name).ToList(),
            Permissions = permissions
        });
    }
}
