using ERPSystem.Core;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ERPSystem.Controls
{
    public partial class PlaceholderWorkspaceControl : UserControl
    {
        public static readonly DependencyProperty ModuleTitleProperty =
            DependencyProperty.Register(nameof(ModuleTitle), typeof(string), typeof(PlaceholderWorkspaceControl),
                new PropertyMetadata("Module", OnModuleInfoChanged));

        public static readonly DependencyProperty ModuleSubtitleProperty =
            DependencyProperty.Register(nameof(ModuleSubtitle), typeof(string), typeof(PlaceholderWorkspaceControl),
                new PropertyMetadata("", OnModuleInfoChanged));

        public static readonly DependencyProperty ModuleIconCodeProperty =
            DependencyProperty.Register(nameof(ModuleIconCode), typeof(string), typeof(PlaceholderWorkspaceControl),
                new PropertyMetadata("\uE80F", OnModuleInfoChanged));

        public static readonly DependencyProperty ModuleAccentColorProperty =
            DependencyProperty.Register(nameof(ModuleAccentColor), typeof(Brush), typeof(PlaceholderWorkspaceControl),
                new PropertyMetadata(new SolidColorBrush(Color.FromRgb(37, 99, 235)), OnModuleInfoChanged));

        public string ModuleTitle { get => (string)GetValue(ModuleTitleProperty); set => SetValue(ModuleTitleProperty, value); }
        public string ModuleSubtitle { get => (string)GetValue(ModuleSubtitleProperty); set => SetValue(ModuleSubtitleProperty, value); }
        public string ModuleIconCode { get => (string)GetValue(ModuleIconCodeProperty); set => SetValue(ModuleIconCodeProperty, value); }
        public Brush ModuleAccentColor { get => (Brush)GetValue(ModuleAccentColorProperty); set => SetValue(ModuleAccentColorProperty, value); }

        public DataGrid DataGrid => ContentGrid;

        public PlaceholderWorkspaceControl()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            LocalizationManager.Instance.LanguageChanged += (_, _) => UpdateLabels();
            UpdateLabels();
        }

        private void UpdateLabels()
        {
            var loc = LocalizationManager.Instance;
            TxtBtnNew.Text = loc["New"];
            TxtExport.Text = loc["Export"];
        }

        private static void OnModuleInfoChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is PlaceholderWorkspaceControl ctrl)
                ctrl.RefreshModuleInfo();
        }

        private void RefreshModuleInfo()
        {
            TxtPageTitle.Text = ModuleTitle;
            TxtPageSubtitle.Text = ModuleSubtitle;
            TxtModuleIcon.Text = ModuleIconCode;
            TxtModuleIcon.Foreground = ModuleAccentColor;

            // Light background from accent
            if (ModuleAccentColor is SolidColorBrush solid)
            {
                var c = solid.Color;
                var light = Color.FromRgb(
                    (byte)(c.R + (255 - c.R) * 0.88),
                    (byte)(c.G + (255 - c.G) * 0.88),
                    (byte)(c.B + (255 - c.B) * 0.88));
                ModuleIconBadge.Background = new SolidColorBrush(light);
            }
        }

        public void SetSummaryCards(List<(string title, string value, string icon, Brush color)> cards)
        {
            SummaryGrid.Children.Clear();
            foreach (var (title, value, icon, color) in cards)
            {
                var card = new MetricCardControl
                {
                    CardTitle = title,
                    CardValue = value,
                    CardIcon = icon,
                    AccentColor = color,
                    Margin = new Thickness(0, 0, 12, 0)
                };
                SummaryGrid.Children.Add(card);
            }
        }
    }
}
