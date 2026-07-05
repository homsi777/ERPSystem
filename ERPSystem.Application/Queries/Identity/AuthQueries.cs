namespace ERPSystem.Application.Queries.Identity;

public sealed class AuthenticateUserQuery
{
    public string Username { get; init; } = "";
    public string Password { get; init; } = "";
}
