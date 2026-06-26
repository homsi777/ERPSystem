using ERPSystem.Core;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace ERPSystem.Shell
{
    public partial class SidebarNavigation : UserControl
    {
        public event EventHandler<AppModule>? ModuleRequested;

        private bool _isCollapsed = false;
        private const double ExpandedWidth = 220;
        private const double CollapsedWidth = 60;

        private Button? _activeButton;

        private readonly Dictionary<string, AppModule> _tagToModule = new()
        {
            ["Dashboard"] = AppModule.Dashboard,
            ["Sales"] = AppModule.Sales,
            ["Purchases"] = AppModule.Purchases,
            ["Inventory"] = AppModule.Inventory,
            ["Customers"] = AppModule.Customers,
            ["Suppliers"] = AppModule.Suppliers,
            ["Accounting"] = AppModule.Accounting,
            ["Reports"] = AppModule.Reports,
            ["ChinaImport"] = AppModule.ChinaImport,
            ["HR"] = AppModule.HR,
            ["Settings"] = AppModule.Settings,
        };

        public SidebarNavigation()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            LocalizationManager.Instance.LanguageChanged += OnLanguageChanged;
            UpdateLabels();
            SetActiveButton(BtnDashboard);
        }

        private void OnLanguageChanged(object? sender, EventArgs e) => UpdateLabels();

        private void UpdateLabels()
        {
            var loc = LocalizationManager.Instance;
            TxtDashboard.Text = loc["Nav_Dashboard"];
            TxtSales.Text = loc["Nav_Sales"];
            TxtPurchases.Text = loc["Nav_Purchases"];
            TxtInventory.Text = loc["Nav_Inventory"];
            TxtCustomers.Text = loc["Nav_Customers"];
            TxtSuppliers.Text = loc["Nav_Suppliers"];
            TxtAccounting.Text = loc["Nav_Accounting"];
            TxtReports.Text = loc["Nav_Reports"];
            TxtPOS.Text = loc["Nav_ChinaImport"];
            TxtSettings.Text = loc["Nav_Settings"];
            TxtUserName.Text = loc["AdminUser"];
            TxtUserRole.Text = loc.IsArabic ? "المسؤول الرئيسي" : "Super Admin";
            TxtAppName.Text = loc.IsArabic ? "ERP Pro" : "ERP Pro";

            // Section headers
            TxtSectionMain.Text = loc.IsArabic ? "الرئيسية" : "MAIN";
            TxtSectionOps.Text = loc.IsArabic ? "العمليات" : "OPERATIONS";
            TxtSectionPeople.Text = loc.IsArabic ? "الأشخاص" : "PEOPLE";
            TxtSectionFinance.Text = loc.IsArabic ? "المالية" : "FINANCE";
        }

        private void NavItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string tag && _tagToModule.TryGetValue(tag, out var module))
            {
                SetActiveButton(btn);
                ModuleRequested?.Invoke(this, module);
            }
        }

        private void SetActiveButton(Button btn)
        {
            // Reset previous
            if (_activeButton != null)
                _activeButton.Tag = _activeButton.Tag; // trigger style re-evaluation

            _activeButton = btn;
            HighlightActiveButton();
        }

        private void HighlightActiveButton()
        {
            // Reset all buttons to normal state
            var allButtons = new[] { BtnDashboard, BtnSales, BtnPurchases, BtnInventory,
                BtnCustomers, BtnSuppliers, BtnAccounting, BtnReports, BtnPOS, BtnSettings };

            foreach (var b in allButtons)
            {
                b.Background = Brushes.Transparent;
                b.Foreground = (SolidColorBrush)Application.Current.Resources["SidebarTextBrush"]!;
            }

            // Highlight active
            if (_activeButton != null)
            {
                _activeButton.Background = (SolidColorBrush)Application.Current.Resources["SidebarActiveBgBrush"]!;
                _activeButton.Foreground = Brushes.White;
            }
        }

        public void SetActiveModule(AppModule module)
        {
            var moduleToButton = new Dictionary<AppModule, Button>
            {
                [AppModule.Dashboard] = BtnDashboard,
                [AppModule.Sales] = BtnSales,
                [AppModule.Purchases] = BtnPurchases,
                [AppModule.Inventory] = BtnInventory,
                [AppModule.Customers] = BtnCustomers,
                [AppModule.Suppliers] = BtnSuppliers,
                [AppModule.Accounting] = BtnAccounting,
                [AppModule.Reports] = BtnReports,
                [AppModule.ChinaImport] = BtnPOS,
                [AppModule.HR] = BtnPOS,
                [AppModule.Settings] = BtnSettings,
            };

            if (moduleToButton.TryGetValue(module, out var btn))
                SetActiveButton(btn);
        }

        private void BtnToggle_Click(object sender, RoutedEventArgs e)
        {
            _isCollapsed = !_isCollapsed;

            var targetWidth = _isCollapsed ? CollapsedWidth : ExpandedWidth;
            var animation = new DoubleAnimation(targetWidth, TimeSpan.FromMilliseconds(200))
            {
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };
            this.BeginAnimation(WidthProperty, animation);

            // Hide/show text labels
            var visibility = _isCollapsed ? Visibility.Collapsed : Visibility.Visible;
            AppNamePanel.Visibility = visibility;
            UserInfoPanel.Visibility = visibility;
            TxtSectionMain.Visibility = visibility;
            TxtSectionOps.Visibility = visibility;
            TxtSectionPeople.Visibility = visibility;
            TxtSectionFinance.Visibility = visibility;

            // Update toggle icon
            var toggleBtn = BtnToggle;
            var iconText = toggleBtn.Content as TextBlock ??
                           (toggleBtn.Content as Grid)?.Children.OfType<TextBlock>().FirstOrDefault();
        }
    }
}
