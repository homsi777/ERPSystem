using MudBlazor;

namespace ERPSystem.Web.Client.Theme;

public static class ErpTheme
{
    public static MudTheme Default { get; } = new()
    {
        PaletteLight = new PaletteLight
        {
            Primary = "#2563EB",
            Secondary = "#64748B",
            Success = "#16A34A",
            Warning = "#EA580C",
            Error = "#DC2626",
            AppbarBackground = "#FFFFFF",
            AppbarText = "#0F172A",
            Background = "#F8FAFC",
            Surface = "#FFFFFF",
            TextPrimary = "#0F172A",
            TextSecondary = "#64748B",
            Divider = "#E2E8F0",
            ActionDefault = "#64748B"
        },
        LayoutProperties = new LayoutProperties
        {
            DefaultBorderRadius = "8px"
        },
        Typography = new Typography
        {
            Default = new DefaultTypography
            {
                FontFamily = ["Segoe UI", "Tahoma", "Arial", "sans-serif"]
            }
        }
    };
}
