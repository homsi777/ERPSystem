using System.IdentityModel.Tokens.Jwt;
using ERPSystem.Application.Abstractions.Repositories;

namespace ERPSystem.Api.Auth;

internal sealed class SessionValidationMiddleware(RequestDelegate next)
{
    public const string SessionClaimType = "sid";

    public async Task InvokeAsync(HttpContext context, IUserSessionRepository sessions)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var raw = context.User.FindFirst(SessionClaimType)?.Value
                ?? context.User.FindFirst(JwtRegisteredClaimNames.Sid)?.Value;

            if (Guid.TryParse(raw, out var sessionId))
            {
                var active = await sessions.IsSessionActiveAsync(sessionId, context.RequestAborted);
                if (!active)
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    await context.Response.WriteAsJsonAsync(new
                    {
                        code = "SessionRevoked",
                        message = "تم تسجيل الدخول من جهاز آخر. يرجى تسجيل الدخول مجدداً.",
                        validationErrors = Array.Empty<object>()
                    });
                    return;
                }

                await sessions.TouchSessionAsync(sessionId, context.RequestAborted);
            }
        }

        await next(context);
    }
}

public static class SessionValidationMiddlewareExtensions
{
    public static IApplicationBuilder UseSessionValidation(this IApplicationBuilder app)
        => app.UseMiddleware<SessionValidationMiddleware>();
}
