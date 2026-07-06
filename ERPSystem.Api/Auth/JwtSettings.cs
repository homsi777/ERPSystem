namespace ERPSystem.Api.Auth;

public sealed class JwtSettings
{
    public const string SectionName = "Jwt";

    public string Issuer { get; init; } = "ERPSystem.Api";
    public string Audience { get; init; } = "ERPSystem.Web";
    public string? SecretKey { get; set; }
    public int AccessTokenMinutes { get; init; } = 30;
    public int RefreshTokenDays { get; init; } = 7;
}
