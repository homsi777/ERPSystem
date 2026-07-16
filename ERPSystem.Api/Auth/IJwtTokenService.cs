using ERPSystem.Application.DTOs.Identity;
using System.Security.Claims;

namespace ERPSystem.Api.Auth;

public interface IJwtTokenService
{
    (string Token, DateTime ExpiresAt) CreateAccessToken(AuthenticatedUserDto user, Guid? sessionId = null);
    string CreateRefreshToken();
    ClaimsPrincipal? ValidateAccessToken(string token);
}
