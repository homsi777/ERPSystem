using ERPSystem.Application.Common;
using ERPSystem.Core;

namespace ERPSystem.Core.Navigation;

public static class AppModuleAccess
{
    private static readonly IReadOnlyDictionary<AppModule, string[]> ModuleKeys = new Dictionary<AppModule, string[]>
    {
        [AppModule.Dashboard] = [],
        [AppModule.ChinaImport] = ["containers"],
        [AppModule.Inventory] = ["warehouse", "containers"],
        [AppModule.Sales] = ["sales"],
        [AppModule.Purchases] = ["purchases"],
        [AppModule.Customers] = ["customers"],
        [AppModule.Suppliers] = ["suppliers"],
        [AppModule.Accounting] = ["accounting", "finance", "openingbalances"],
        [AppModule.Expenses] = ["expenses"],
        [AppModule.CapitalPartners] = ["capital"],
        [AppModule.HR] = ["hr"],
        [AppModule.Settings] = ["settings"],
        [AppModule.Reports] = ["sales", "accounting", "finance", "expenses", "capital", "purchases", "containers"]
    };

    public static bool CanAccess(AppModule module, IReadOnlyCollection<string> permissionCodes)
    {
        if (module == AppModule.Dashboard)
            return true;

        if (!ModuleKeys.TryGetValue(module, out var keys) || keys.Length == 0)
            return false;

        return PermissionModuleCatalog.HasAnyModuleAccess(permissionCodes, keys);
    }
}
