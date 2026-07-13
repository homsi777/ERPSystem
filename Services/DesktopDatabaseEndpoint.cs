namespace ERPSystem.Services;

/// <summary>
/// Production PostgreSQL endpoint for desktop clients.
/// Do NOT use alamal-ab.org — it resolves to Cloudflare and blocks port 5432.
/// </summary>
public static class DesktopDatabaseEndpoint
{
    public const string ProductionHost = "65.21.136.217";
    public const int ProductionPort = 5432;
    public const string Database = "erp_pro";
    public const string Username = "erp_app";

    public static string BuildConnectionString(string password) =>
        $"Host={ProductionHost};Port={ProductionPort};Database={Database};Username={Username};Password={password};SSL Mode=Prefer;Trust Server Certificate=true";

    public static bool IsCloudflareOrWrongHost(string? host)
    {
        if (string.IsNullOrWhiteSpace(host))
            return false;

        if (host.Contains("alamal-ab.org", StringComparison.OrdinalIgnoreCase))
            return true;

        if (!System.Net.IPAddress.TryParse(host, out var ip))
            return false;

        var bytes = ip.GetAddressBytes();
        if (bytes.Length != 4)
            return false;

        // Common Cloudflare anycast ranges used when the domain is proxied (orange cloud).
        return bytes[0] == 188 && bytes[1] == 114
            || bytes[0] == 188 && bytes[1] == 96
            || bytes[0] == 104 && bytes[1] == 16;
    }
}
