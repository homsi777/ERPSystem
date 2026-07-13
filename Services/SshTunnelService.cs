using System.Net.Sockets;
using System.IO;
using System.Threading;
using Microsoft.Extensions.Configuration;
using Renci.SshNet;

namespace ERPSystem.Services;

/// <summary>
/// Opens an in-process SSH port-forward to PostgreSQL.
/// SSH.NET is packaged with the desktop app, so no external ssh.exe is required.
/// </summary>
public sealed class SshTunnelService : IDisposable
{
    private SshClient? _client;
    private ForwardedPortLocal? _forwardedPort;

    private sealed record TunnelOptions(
        string SshHost,
        int SshPort,
        string SshUser,
        int LocalPort,
        string RemoteHost,
        int RemotePort,
        string? IdentityFile);

    /// <summary>Starts the tunnel if configured and enabled; returns null otherwise.</summary>
    public static SshTunnelService? StartIfConfigured(IConfiguration configuration) =>
        StartIfConfiguredAsync(configuration).GetAwaiter().GetResult();

    public static async Task<SshTunnelService?> StartIfConfiguredAsync(
        IConfiguration configuration,
        CancellationToken cancellationToken = default)
    {
        var section = configuration.GetSection("SshTunnel");
        if (!section.Exists() || !section.GetValue<bool>("Enabled"))
            return null;

        var options = new TunnelOptions(
            section.GetValue<string>("SshHost") ?? "",
            section.GetValue("SshPort", 22),
            section.GetValue<string>("SshUser") ?? "",
            section.GetValue("LocalPort", 5433),
            section.GetValue<string>("RemoteHost") ?? "localhost",
            section.GetValue("RemotePort", 5432),
            section.GetValue<string>("IdentityFile"));

        if (string.IsNullOrWhiteSpace(options.SshHost) || string.IsNullOrWhiteSpace(options.SshUser))
            return null;

        if (string.IsNullOrWhiteSpace(options.IdentityFile))
            throw new InvalidOperationException("مسار مفتاح SSH غير معرّف في إعدادات التطبيق.");

        var identityFile = ResolveIdentityFile(options.IdentityFile);
        if (!File.Exists(identityFile))
            throw new FileNotFoundException(
                $"مفتاح نفق SSH غير موجود:\n{identityFile}\n\nأعد تثبيت التطبيق لإصلاح ملفات الاتصال.",
                identityFile);

        options = options with { IdentityFile = identityFile };
        var service = new SshTunnelService();
        await service.StartAsync(options, cancellationToken);
        return service;
    }

    private async Task StartAsync(TunnelOptions options, CancellationToken cancellationToken)
    {
        // A tunnel (or another forwarder) is already listening — reuse it.
        if (await IsPortOpenAsync(options.LocalPort, cancellationToken))
            return;

        var key = new PrivateKeyFile(options.IdentityFile!);
        var connectionInfo = new ConnectionInfo(
            options.SshHost,
            options.SshPort,
            options.SshUser,
            new PrivateKeyAuthenticationMethod(options.SshUser, key))
        {
            Timeout = TimeSpan.FromSeconds(15)
        };

        _client = new SshClient(connectionInfo)
        {
            KeepAliveInterval = TimeSpan.FromSeconds(30)
        };

        await Task.Run(_client.Connect, cancellationToken);
        if (!_client.IsConnected)
            throw new InvalidOperationException("تعذّر إنشاء اتصال SSH بالخادم.");

        _forwardedPort = new ForwardedPortLocal(
            "127.0.0.1",
            (uint)options.LocalPort,
            options.RemoteHost,
            (uint)options.RemotePort);
        _client.AddForwardedPort(_forwardedPort);
        _forwardedPort.Start();

        // Wait up to 10 seconds for the forwarded port to accept connections.
        for (var attempt = 0; attempt < 20; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (await IsPortOpenAsync(options.LocalPort, cancellationToken))
                return;
            await Task.Delay(500, cancellationToken);
        }

        throw new TimeoutException($"SSH tunnel did not become ready on local port {options.LocalPort}.");
    }

    private static async Task<bool> IsPortOpenAsync(int port, CancellationToken cancellationToken)
    {
        try
        {
            using var client = new TcpClient();
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(TimeSpan.FromMilliseconds(600));
            await client.ConnectAsync("127.0.0.1", port, timeout.Token);
            return client.Connected;
        }
        catch
        {
            return false;
        }
    }

    private static string ResolveIdentityFile(string configuredPath)
    {
        var expanded = Environment.ExpandEnvironmentVariables(configuredPath.Trim());
        return Path.IsPathRooted(expanded)
            ? expanded
            : Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, expanded));
    }

    public void Dispose()
    {
        try
        {
            if (_forwardedPort?.IsStarted == true)
                _forwardedPort.Stop();
            _forwardedPort?.Dispose();
            _forwardedPort = null;

            if (_client?.IsConnected == true)
                _client.Disconnect();
            _client?.Dispose();
            _client = null;
        }
        catch
        {
            // Best-effort cleanup.
        }
    }
}
