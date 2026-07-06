using ERPSystem.Application.DTOs.Identity;

namespace ERPSystem.Api.Contracts;

public sealed record LoginRequest(string Username, string Password);

public sealed record RefreshTokenRequest(string RefreshToken);

public sealed record LogoutRequest(string RefreshToken);

public sealed record AuthTokenResponse(
    string AccessToken,
    string RefreshToken,
    DateTime AccessTokenExpiresAt,
    DateTime RefreshTokenExpiresAt,
    AuthenticatedUserDto User);

public sealed record RefreshTokenResponse(
    string AccessToken,
    DateTime AccessTokenExpiresAt);

public sealed record MeResponse(
    Guid UserId,
    string Username,
    string FullNameAr,
    IReadOnlyList<string> Roles,
    IReadOnlyList<string> Permissions);
