using System.Diagnostics;
using System.Net.Sockets;
using System.Threading;
using Microsoft.Extensions.Configuration;

namespace ERPSystem.Services;

/// <summary>
/// Optionally opens an SSH port-forward (local -> remote PostgreSQL) so the desktop
/// app can reach a cloud database that is only reachable through the SSH port.
/// Driven by the "SshTunnel" section in appsettings(.Local).json. No-ops when disabled.
/// </summary>
public sealed class SshTunnelService : IDisposable
{
    private Process? _process;

    private sealed record TunnelOptions(
        string SshHost,
        int SshPort,
        string SshUser,
        int LocalPort,
        string RemoteHost,
        int RemotePort);

    /// <summary>Starts the tunnel if configured and enabled; returns null otherwise.</summary>
    public static SshTunnelService? StartIfConfigured(IConfiguration configuration)
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
            section.GetValue("RemotePort", 5432));

        if (string.IsNullOrWhiteSpace(options.SshHost) || string.IsNullOrWhiteSpace(options.SshUser))
            return null;

        var service = new SshTunnelService();
        service.Start(options);
        return service;
    }

    private void Start(TunnelOptions options)
    {
        // A tunnel (or another forwarder) is already listening — reuse it.
        if (IsPortOpen(options.LocalPort))
            return;

        var args =
            $"-p {options.SshPort} -N " +
            "-o StrictHostKeyChecking=accept-new -o ServerAliveInterval=30 -o ExitOnForwardFailure=yes " +
            $"-L {options.LocalPort}:{options.RemoteHost}:{options.RemotePort} " +
            $"{options.SshUser}@{options.SshHost}";

        _process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "ssh",
                Arguments = args,
                // Visible window so a one-time password/host-key prompt can be answered.
                UseShellExecute = true,
                CreateNoWindow = false,
                WindowStyle = ProcessWindowStyle.Minimized
            }
        };

        _process.Start();

        // Wait up to ~20s for the forwarded port to accept connections.
        for (var attempt = 0; attempt < 40 && !IsPortOpen(options.LocalPort); attempt++)
            Thread.Sleep(500);
    }

    private static bool IsPortOpen(int port)
    {
        try
        {
            using var client = new TcpClient();
            var result = client.BeginConnect("127.0.0.1", port, null, null);
            var connected = result.AsyncWaitHandle.WaitOne(TimeSpan.FromMilliseconds(600));
            if (connected)
                client.EndConnect(result);
            return connected && client.Connected;
        }
        catch
        {
            return false;
        }
    }

    public void Dispose()
    {
        try
        {
            if (_process is { HasExited: false })
                _process.Kill(entireProcessTree: true);
        }
        catch
        {
            // Best-effort cleanup.
        }

        _process?.Dispose();
        _process = null;
    }
}
