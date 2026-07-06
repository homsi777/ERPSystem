namespace ERPSystem.Web.Client.Services;

/// <summary>
/// Development-only helper that signs in automatically.
/// Replace with a real login screen before production.
/// </summary>
public sealed class DevAuthInitializer(ApiClient apiClient, IConfiguration configuration)
{
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (!configuration.GetValue("DevAutoLogin", false))
            return;

        var username = configuration["DevLogin:Username"] ?? "admin";
        var password = configuration["DevLogin:Password"] ?? "Admin@123";

        await apiClient.LoginAsync(username, password, cancellationToken);
    }
}
