namespace ERPSystem.Application.Services;

/// <summary>
/// Marker for application-layer orchestration services.
/// Concrete implementations will be registered in Infrastructure or WPF composition root.
/// </summary>
public static class ApplicationServiceRegistration
{
    public const string ApplicationAssemblyName = "ERPSystem.Application";
}
