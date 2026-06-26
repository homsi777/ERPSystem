using ERPSystem.Core;

namespace ERPSystem.Core.Navigation
{
    public sealed record NavMenuItem(string LabelKey, string Icon, AppModule Module, bool Direct = false);

    public sealed record NavSubItem(string Label, string Icon, AppModule Module, string SubPage);

    /// <summary>Single source for top navigation — driven by SubmoduleRegistry.</summary>
    public static class NavigationCatalog
    {
        public static IReadOnlyList<NavMenuItem> TopLevel { get; } =
        [
            new("Nav_Dashboard", "\uE80F", AppModule.Dashboard, Direct: true),
            new("Nav_ChinaImport", "\uE7BF", AppModule.ChinaImport),
            new("Nav_Inventory", "\uE821", AppModule.Inventory),
            new("Nav_Sales", "\uE8F1", AppModule.Sales),
            new("Nav_Purchases", "\uE7BF", AppModule.Purchases),
            new("Nav_Customers", "\uE716", AppModule.Customers),
            new("Nav_Suppliers", "\uE779", AppModule.Suppliers),
            new("Nav_Accounting", "\uE8C1", AppModule.Accounting),
            new("Nav_Reports", "\uE9D2", AppModule.Reports),
            new("Nav_HR", "\uE716", AppModule.HR),
            new("Nav_Settings", "\uE713", AppModule.Settings),
        ];

        public static IReadOnlyList<NavSubItem> GetSubItems(AppModule module)
        {
            var subs = SubmoduleRegistry.Get(module);
            if (subs.Count == 0) return Array.Empty<NavSubItem>();

            return subs.Select(s => new NavSubItem(s.LabelAr, s.IconGlyph, module, s.Key)).ToList();
        }

        public static string GetModuleLabelKey(AppModule module) =>
            TopLevel.FirstOrDefault(m => m.Module == module)?.LabelKey ?? module.ToString();

        public static SubmoduleDef? GetActiveSub(AppModule module, string? key)
        {
            var resolved = SubmoduleRegistry.ResolveKey(module, key);
            return SubmoduleRegistry.Get(module).FirstOrDefault(s => s.Key == resolved);
        }

        public static string BuildBreadcrumb(AppModule module, string? subKey)
        {
            var modLabel = GetModuleLabelKey(module);
            var sub = GetActiveSub(module, subKey);
            if (sub == null) return modLabel;
            return $"{modLabel} › {sub.LabelAr}";
        }
    }
}
