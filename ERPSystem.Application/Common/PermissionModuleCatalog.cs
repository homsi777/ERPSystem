namespace ERPSystem.Application.Common;

/// <summary>
/// Predefined ERP modules shown when assigning role permissions and when filtering navigation.
/// </summary>
public static class PermissionModuleCatalog
{
    public static readonly IReadOnlyList<string> AssignableModuleOrder =
    [
        "security",
        "sales",
        "customers",
        "purchases",
        "suppliers",
        "warehouse",
        "accounting",
        "finance",
        "openingbalances",
        "expenses",
        "capital",
        "hr",
        "settings"
    ];

    private static readonly Dictionary<string, int> OrderIndex =
        AssignableModuleOrder
            .Select((module, index) => new { module, index })
            .ToDictionary(x => x.module, x => x.index, StringComparer.OrdinalIgnoreCase);

    public static bool IsAssignableModule(string moduleKey) =>
        OrderIndex.ContainsKey(moduleKey);

    public static int GetModuleSortOrder(string moduleKey) =>
        OrderIndex.GetValueOrDefault(moduleKey, int.MaxValue);

    public static bool HasModuleAccess(IReadOnlyCollection<string> permissionCodes, string moduleKey)
    {
        if (permissionCodes.Count == 0)
            return false;

        var prefix = moduleKey + ".";
        return permissionCodes.Any(code =>
            code.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
    }

    public static bool HasAnyModuleAccess(
        IReadOnlyCollection<string> permissionCodes,
        IEnumerable<string> moduleKeys) =>
        moduleKeys.Any(module => HasModuleAccess(permissionCodes, module));

    /// <summary>True when the user holds every seeded permission (Administrator/system role).</summary>
    public static bool HasFullAccess(IReadOnlyCollection<string> permissionCodes, int totalPermissionCount) =>
        totalPermissionCount > 0 && permissionCodes.Count >= totalPermissionCount;
}
