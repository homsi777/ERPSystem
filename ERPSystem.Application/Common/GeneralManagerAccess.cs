namespace ERPSystem.Application.Common;

/// <summary>
/// General manager (مدير عام) — exclusive access to import/cost pricing and the China module.
/// </summary>
public static class GeneralManagerAccess
{
    public const string PermissionCode = "security.general-manager";

    public static bool IsGeneralManager(IReadOnlyCollection<string> permissionCodes) =>
        permissionCodes.Any(code => code.Equals(PermissionCode, StringComparison.OrdinalIgnoreCase));
}
