using System.Windows;
using System.Windows.Media;

namespace ERPSystem.Core
{
    public enum AppTheme { Light, Dark }

    public class ThemeManager
    {
        private static ThemeManager? _instance;
        public static ThemeManager Instance => _instance ??= new ThemeManager();

        public AppTheme CurrentTheme { get; private set; } = AppTheme.Light;

        public event EventHandler? ThemeChanged;

        public void ApplyTheme(AppTheme theme)
        {
            CurrentTheme = theme;
            ThemeChanged?.Invoke(this, EventArgs.Empty);
        }

        public void ToggleTheme() =>
            ApplyTheme(CurrentTheme == AppTheme.Light ? AppTheme.Dark : AppTheme.Light);

        // Spacing constants
        public static class Spacing
        {
            public const double XS = 4;
            public const double SM = 8;
            public const double MD = 12;
            public const double Base = 16;
            public const double LG = 24;
            public const double XL = 32;
            public const double XXL = 48;
        }

        // Font families
        public static class Fonts
        {
            public const string Primary = "Segoe UI, Tahoma, Arial";
            public const string Arabic = "Segoe UI, Tahoma, Arial";
            public const string Monospace = "Consolas, Courier New";
        }

        // Helper to get a color brush from the application resources
        public static SolidColorBrush GetBrush(string key)
        {
            if (Application.Current.Resources.Contains(key))
                return (SolidColorBrush)Application.Current.Resources[key]!;
            return new SolidColorBrush(Colors.Gray);
        }

        public static Color GetColor(string key)
        {
            if (Application.Current.Resources.Contains(key))
                return (Color)Application.Current.Resources[key]!;
            return Colors.Gray;
        }
    }
}
