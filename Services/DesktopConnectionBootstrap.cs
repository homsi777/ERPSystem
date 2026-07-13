using Microsoft.Extensions.Configuration;
using Npgsql;

namespace ERPSystem.Services;

/// <summary>
/// Resolves how the desktop client reaches PostgreSQL and validates connectivity before startup continues.
/// Production installs connect directly to the VPS IP — never alamal-ab.org (Cloudflare blocks 5432).
/// </summary>
public static class DesktopConnectionBootstrap
{
    public static bool RequiresSshTunnel(IConfiguration configuration)
    {
        var tunnel = configuration.GetSection("SshTunnel");
        if (!tunnel.Exists() || !tunnel.GetValue<bool>("Enabled"))
            return false;

        var host = ReadDatabaseHost(configuration);
        return IsLoopbackHost(host);
    }

    public static string ReadDatabaseHost(IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString(
            Infrastructure.DependencyInjection.InfrastructureServiceCollectionExtensions.ConnectionStringName);
        if (string.IsNullOrWhiteSpace(connectionString))
            return "";

        return new NpgsqlConnectionStringBuilder(connectionString).Host ?? "";
    }

    public static async Task ValidateDatabaseAsync(IConfiguration configuration, CancellationToken cancellationToken = default)
    {
        var connectionString = configuration.GetConnectionString(
            Infrastructure.DependencyInjection.InfrastructureServiceCollectionExtensions.ConnectionStringName)
            ?? throw new InvalidOperationException("سلسلة الاتصال DefaultConnection غير معرّفة في appsettings.");

        var builder = new NpgsqlConnectionStringBuilder(connectionString);
        if (string.IsNullOrWhiteSpace(builder.Host))
            throw new InvalidOperationException("عنوان خادم قاعدة البيانات غير معرّف.");

        if (string.IsNullOrWhiteSpace(builder.Password))
            throw new InvalidOperationException(
                "كلمة مرور قاعدة البيانات غير معرّفة.\n\n" +
                "أعد تشغيل المثبّت أو عدّل appsettings.Local.json بجوار ERPSystem.exe.");

        if (DesktopDatabaseEndpoint.IsCloudflareOrWrongHost(builder.Host))
            throw new InvalidOperationException(
                "عنوان قاعدة البيانات خاطئ.\n\n" +
                "alamal-ab.org يمر عبر Cloudflare ولا يعمل مع PostgreSQL (المنفذ 5432).\n" +
                $"استخدم IP السيرفر: {DesktopDatabaseEndpoint.ProductionHost}\n\n" +
                "عدّل appsettings.Local.json أو أعد التثبيت.");

        try
        {
            await using var connection = new NpgsqlConnection(connectionString);
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(TimeSpan.FromSeconds(15));
            await connection.OpenAsync(timeout.Token);
            await using var cmd = new NpgsqlCommand("SELECT 1", connection);
            await cmd.ExecuteScalarAsync(timeout.Token);
        }
        catch (OperationCanceledException)
        {
            throw new InvalidOperationException(
                $"انتهت مهلة الاتصال بقاعدة البيانات على {builder.Host}:{builder.Port}.\n" +
                "تأكد من اتصال الإنترنت وأن المنفذ 5432 مسموح من شبكة الشركة.");
        }
        catch (PostgresException ex)
        {
            throw new InvalidOperationException(
                $"رفض الخادم الاتصال ({ex.SqlState}): {ex.MessageText}\n" +
                "تحقق من اسم المستخدم وكلمة المرور في appsettings.Local.json.");
        }
        catch (Exception ex) when (ex is NpgsqlException or TimeoutException)
        {
            throw new InvalidOperationException(
                $"تعذّر الاتصال بقاعدة البيانات على {builder.Host}:{builder.Port}.\n\n" +
                $"التفاصيل: {ex.Message}\n\n" +
                $"• تأكد أن Host = {DesktopDatabaseEndpoint.ProductionHost} (وليس alamal-ab.org)\n" +
                "• افتح المنفذ 5432 في جدار Hetzner Cloud Firewall للسيرفر\n" +
                "• تأكد أن شبكة الشركة لا تحجب المنفذ 5432",
                ex);
        }
    }

    private static bool IsLoopbackHost(string host) =>
        host.Equals("localhost", StringComparison.OrdinalIgnoreCase)
        || host.Equals("127.0.0.1", StringComparison.OrdinalIgnoreCase)
        || host.Equals("::1", StringComparison.OrdinalIgnoreCase);
}
