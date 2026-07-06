namespace ERPSystem.Web.Client.Models.Auth;

public sealed class LoginResponse
{
    public string AccessToken { get; init; } = "";
    public string RefreshToken { get; init; } = "";
}

public sealed class LoginRequest
{
    public string Username { get; init; } = "";
    public string Password { get; init; } = "";
}
