using System.Text;
using ERPSystem.Api.Auth;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace ERPSystem.Api.Auth;

public static class JwtAuthenticationExtensions
{
    public const string SecretKeyEnvironmentVariable = "JWT_SECRET";

    public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.SectionName));

        var jwtSettings = configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>() ?? new JwtSettings();
        var secretKey = ResolveSecretKey(configuration, jwtSettings);

        services.PostConfigure<JwtSettings>(options => options.SecretKey = secretKey);

        services.AddSingleton<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IRefreshTokenStore, DbRefreshTokenStore>();

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtSettings.Issuer,
                    ValidAudience = jwtSettings.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
                    ClockSkew = TimeSpan.FromMinutes(1)
                };
            });

        services.AddAuthorization();
        return services;
    }

    private static string ResolveSecretKey(IConfiguration configuration, JwtSettings jwtSettings)
    {
        var secret = configuration[SecretKeyEnvironmentVariable]
            ?? configuration[$"{JwtSettings.SectionName}:SecretKey"];

        if (string.IsNullOrWhiteSpace(secret))
            throw new InvalidOperationException(
                $"JWT secret is not configured. Set environment variable '{SecretKeyEnvironmentVariable}' or Jwt:SecretKey.");

        return secret;
    }
}
