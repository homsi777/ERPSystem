using ERPSystem.Application.Abstractions;
using ERPSystem.Application.Abstractions.Repositories;
using ERPSystem.Application.Abstractions.Services;
using ERPSystem.Application.Results;

namespace ERPSystem.Application.Commands.Identity;

public sealed class UpdateRolePermissionsCommand
{
    public Guid RoleId { get; init; }
    public IReadOnlyList<string> PermissionCodes { get; init; } = [];
}

public sealed class CreateIdentityRoleCommand
{
    public string Name { get; init; } = "";
    public string Description { get; init; } = "";
}

public sealed class CreateIdentityUserCommand
{
    public string Username { get; init; } = "";
    public string Password { get; init; } = "";
    public string FullNameAr { get; init; } = "";
    public string FullNameEn { get; init; } = "";
    public IReadOnlyList<Guid> RoleIds { get; init; } = [];
}

public sealed class UpdateRolePermissionsHandler(
    IIdentityAdminRepository repository,
    IUnitOfWork unitOfWork)
    : ICommandHandler<UpdateRolePermissionsCommand, ApplicationResult>
{
    public async Task<ApplicationResult> HandleAsync(
        UpdateRolePermissionsCommand command,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await repository.ReplaceRolePermissionsAsync(command.RoleId, command.PermissionCodes, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return ApplicationResult.Success();
        }
        catch (Exception ex)
        {
            return ApplicationResult.Failure(ex.Message);
        }
    }
}

public sealed class CreateIdentityRoleHandler(
    IIdentityAdminRepository repository,
    IUnitOfWork unitOfWork)
    : ICommandHandler<CreateIdentityRoleCommand, ApplicationResult<Guid>>
{
    public async Task<ApplicationResult<Guid>> HandleAsync(
        CreateIdentityRoleCommand command,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var id = await repository.CreateRoleAsync(command.Name, command.Description, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return ApplicationResult<Guid>.Success(id);
        }
        catch (Exception ex)
        {
            return ApplicationResult<Guid>.Failure(ex.Message);
        }
    }
}

public sealed class CreateIdentityUserHandler(
    IIdentityAdminRepository repository,
    IPasswordHasher passwordHasher,
    IUnitOfWork unitOfWork)
    : ICommandHandler<CreateIdentityUserCommand, ApplicationResult<Guid>>
{
    public async Task<ApplicationResult<Guid>> HandleAsync(
        CreateIdentityUserCommand command,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command.Username) || string.IsNullOrWhiteSpace(command.Password))
            return ApplicationResult<Guid>.Failure("اسم المستخدم وكلمة المرور مطلوبان.");

        if (string.IsNullOrWhiteSpace(command.FullNameAr))
            return ApplicationResult<Guid>.Failure("الاسم بالعربي مطلوب.");

        try
        {
            var hash = passwordHasher.HashPassword(command.Password);
            var userId = await repository.CreateUserAsync(
                command.Username,
                hash,
                command.FullNameAr,
                command.FullNameEn,
                cancellationToken);

            if (command.RoleIds.Count > 0)
                await repository.SetUserRolesAsync(userId, command.RoleIds, cancellationToken);

            await unitOfWork.SaveChangesAsync(cancellationToken);
            return ApplicationResult<Guid>.Success(userId);
        }
        catch (Exception ex)
        {
            return ApplicationResult<Guid>.Failure(ex.Message);
        }
    }
}
