namespace ERPSystem.Application.DTOs.Identity;

public sealed class UserCredentialDto
{
    public Guid UserId { get; init; }
    public string Username { get; init; } = "";
    public string FullNameAr { get; init; } = "";
    public string PasswordHash { get; init; } = "";
    public bool IsActive { get; init; }
}

public sealed class AuthenticatedUserDto
{
    public Guid UserId { get; init; }
    public string Username { get; init; } = "";
    public string FullNameAr { get; init; } = "";
    public IReadOnlyList<string> Roles { get; init; } = [];
    public IReadOnlyList<string> Permissions { get; init; } = [];
}
