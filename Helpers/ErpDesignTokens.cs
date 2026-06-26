using System.Windows;
using System.Windows.Media;

namespace ERPSystem.Helpers
{
    /// <summary>Single source of truth for ERP PRO spacing, sizing, and typography.</summary>
    public static class ErpDesignTokens
    {
        // Spacing scale (4px base)
        public const double SpaceXs = 4;
        public const double SpaceSm = 8;
        public const double SpaceMd = 12;
        public const double SpaceLg = 16;
        public const double SpaceXl = 24;

        // Layout
        public const double CardRadius = 8;
        public const double CardGap = 12;
        public const double IconBadgeSize = 40;
        public const double IconBadgeSizeLg = 44;
        public const double IconBadgeRadius = 8;

        // Controls
        public const double ControlHeight = 32;
        public const double ToolbarHeight = 36;
        public const double GridRowHeight = 34;
        public const double GridHeaderHeight = 32;

        // Typography
        public const double FontDashboardTitle = 22;
        public const double FontPageTitle = 15;
        public const double FontSectionTitle = 14;
        public const double FontBody = 13;
        public const double FontCaption = 11;
        public const double FontKpiValue = 18;
        public const double FontKpiLabel = 11;

        public static Thickness PagePadding => new(SpaceLg, SpaceMd, SpaceLg, SpaceLg);
        public static Thickness CardPadding => new(SpaceMd);
        public static Thickness CardPaddingCompact => new(SpaceMd, SpaceSm, SpaceMd, SpaceSm);
        public static Thickness ToolbarPadding => new(SpaceLg, SpaceSm, SpaceLg, SpaceSm);
        public static Thickness SectionBottom => new(0, 0, 0, SpaceMd);
        public static Thickness CardBottomMargin => new(0, 0, 0, CardGap);
        public static Thickness GridColumnGap => new(CardGap, 0, 0, 0);

        public static CornerRadius Radius => new(CardRadius);
        public static FontFamily UiFont => new("Segoe UI, Tahoma, Arial");
        public static FontFamily IconFont => new("Segoe MDL2 Assets");
    }
}
