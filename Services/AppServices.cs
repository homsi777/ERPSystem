using Microsoft.Extensions.DependencyInjection;

namespace ERPSystem.Services;

public static class AppServices
{
    public static IServiceProvider Provider { get; private set; } = null!;

    public static bool IsInitialized => Provider is not null;

    public static void Initialize(IServiceProvider provider) => Provider = provider;

    public static IServiceScope CreateScope() => Provider.CreateScope();

    public static T GetRequiredService<T>() where T : notnull =>
        Provider.GetRequiredService<T>();
}
