using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using ERPSystem.Application.Abstractions.Repositories;
using ERPSystem.Application.Abstractions.Services;
using ERPSystem.Application.DTOs.Identity;
using ERPSystem.Application.Queries.Identity;
using ERPSystem.Application.UseCases.Identity;
using ERPSystem.Infrastructure.Seed;
using Microsoft.AspNetCore.Mvc;

namespace ERPSystem.Api.Services;

internal sealed class HttpContextCurrentUserService(IHttpContextAccessor httpContextAccessor) : ICurrentUserService
{
    public Guid? UserId
    {
        get
        {
            var user = httpContextAccessor.HttpContext?.User;
            if (user?.Identity?.IsAuthenticated != true)
                return null;

            var raw = user.FindFirstValue(JwtRegisteredClaimNames.Sub)
                ?? user.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(raw, out var id) ? id : null;
        }
    }

    public string? Username
    {
        get
        {
            var user = httpContextAccessor.HttpContext?.User;
            return user?.FindFirstValue(JwtRegisteredClaimNames.UniqueName)
                ?? user?.FindFirstValue(ClaimTypes.Name);
        }
    }

    public bool IsAuthenticated =>
        httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated == true;
}

internal sealed class HttpContextBranchService(ICurrentUserService currentUser) : ICurrentBranchService
{
    public Guid? CompanyId =>
        currentUser.IsAuthenticated ? DatabaseSeeder.DefaultCompanyId : null;

    public Guid? BranchId =>
        currentUser.IsAuthenticated ? DatabaseSeeder.DefaultBranchId : null;
}
