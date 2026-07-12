using ERPSystem.Core;
using ERPSystem.Core.Navigation;
using ERPSystem.Modules;
using ERPSystem.Services;
using System.Windows;

namespace ERPSystem
{
    public partial class MainWindow : Window
    {
        private readonly DashboardModule _dashboard;
        private readonly Dictionary<AppModule, FrameworkElement> _moduleCache = [];
        private bool _languageSubscribed;
        private bool _navigationSubscribed;

        public MainWindow()
        {
            InitializeComponent();

            _dashboard = new DashboardModule();
            _moduleCache[AppModule.Dashboard] = _dashboard;

            _dashboard.NavigationRequested += (_, m) => NavigateTo(new NavigationRequest(m));
            _dashboard.ActionRequested += (_, req) => NavigateTo(new NavigationRequest(req.Module, req.SubPage));
            Loaded += OnLoaded;
            Closing += OnClosing;
        }

        private void OnClosing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            if (!UnsavedWorkGuard.TryConfirmLeave())
                e.Cancel = true;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (!_languageSubscribed)
            {
                LocalizationManager.Instance.LanguageChanged += OnLanguageChanged;
                _languageSubscribed = true;
            }
            if (!_navigationSubscribed)
            {
                NavigationStateManager.Instance.Navigated += (_, req) => NavigateTo(req);
                _navigationSubscribed = true;
            }
            ApplyFlowDirection();
            NavigateTo(new NavigationRequest(AppModule.Dashboard));
        }

        private void OnLanguageChanged(object? sender, EventArgs e)
        {
            ApplyFlowDirection();
            UpdateStatusBar();
        }

        private void ApplyFlowDirection()
        {
            FlowDirection = LocalizationManager.Instance.FlowDir;
        }

        private void NavigateTo(NavigationRequest req)
        {
            if (req.Module == AppModule.ChinaImport && !WpfGeneralManagerAccess.IsGeneralManager)
            {
                MockInteractionService.ShowWarning(
                    "قسم الصين وأسعار الاستيراد/التكلفة متاحان لحساب المدير العام فقط.",
                    "صلاحية محدودة");
                return;
            }

            var currentModule = ResolveActiveModule();
            if (currentModule.HasValue
                && currentModule.Value != req.Module
                && !UnsavedWorkGuard.TryConfirmLeave())
                return;

            WorkspaceHost.Content = GetOrCreateModule(req.Module);

            if (WorkspaceHost.Content is ISubpageNavigator nav && !string.IsNullOrEmpty(req.SubPage))
                nav.NavigateSubpage(req.SubPage);

            NavBar.SetActiveModule(req.Module);
            Title = $"{GetModuleTitle(req.Module)} — الأمل.AB";
            UpdateStatusBar(req);
        }

        private FrameworkElement GetOrCreateModule(AppModule module)
        {
            if (_moduleCache.TryGetValue(module, out var existing))
                return existing;

            FrameworkElement created = module switch
            {
                AppModule.Dashboard   => _dashboard,
                AppModule.ChinaImport => new ChinaImportModule(),
                AppModule.Sales       => new SalesModule(),
                AppModule.Inventory   => new InventoryModule(),
                AppModule.Customers   => new CustomersModule(),
                AppModule.Suppliers   => new SuppliersModule(),
                AppModule.Accounting  => new AccountingModule(),
                AppModule.Expenses    => new ExpensesModule(),
                AppModule.CapitalPartners => new CapitalPartnersModule(),
                AppModule.Reports     => new ReportsModule(),
                AppModule.Purchases   => new PurchasesModule(),
                AppModule.HR          => new HRModule(),
                AppModule.Settings    => new SettingsModule(),
                _                     => _dashboard
            };
            _moduleCache[module] = created;
            return created;
        }

        private static string GetModuleTitle(AppModule module) => module switch
        {
            AppModule.Dashboard   => "الرئيسية",
            AppModule.ChinaImport => "طلبات الصين",
            AppModule.Sales       => "المبيعات",
            AppModule.Purchases   => "المشتريات",
            AppModule.Inventory   => "المخزون",
            AppModule.Customers   => "العملاء",
            AppModule.Suppliers   => "الموردون",
            AppModule.Accounting  => "المالية",
            AppModule.Expenses    => "المصاريف",
            AppModule.CapitalPartners => "رأس المال والشركاء",
            AppModule.Reports     => "التقارير",
            AppModule.HR          => "الموارد البشرية",
            AppModule.Settings    => "الإعدادات",
            _                     => "الأمل.AB"
        };

        private void UpdateStatusBar(NavigationRequest? req = null)
        {
            TxtStatusMessage.Text = "جاهز للعمل";
            TxtDbStatus.Text = "متصل بـ PostgreSQL ✓";

            if (req != null && !string.IsNullOrEmpty(req.SubPage))
            {
                var moduleTitle = GetModuleTitle(req.Module);
                TxtNavBreadcrumb.Text = $"{moduleTitle} › {req.SubPage}";
            }
            else
            {
                TxtNavBreadcrumb.Text = req != null ? GetModuleTitle(req.Module) : "";
            }
        }

        private void NavBar_NavigationRequested(object? sender, NavigationRequest req) =>
            NavigateTo(req);

        private void TopBar_SettingsRequested(object? sender, EventArgs e) =>
            NavigateTo(new NavigationRequest(AppModule.Settings, "Company"));

        private AppModule? ResolveActiveModule() => WorkspaceHost.Content switch
        {
            DashboardModule => AppModule.Dashboard,
            ChinaImportModule => AppModule.ChinaImport,
            SalesModule => AppModule.Sales,
            InventoryModule => AppModule.Inventory,
            CustomersModule => AppModule.Customers,
            SuppliersModule => AppModule.Suppliers,
            AccountingModule => AppModule.Accounting,
            ExpensesModule => AppModule.Expenses,
            CapitalPartnersModule => AppModule.CapitalPartners,
            ReportsModule => AppModule.Reports,
            PurchasesModule => AppModule.Purchases,
            HRModule => AppModule.HR,
            SettingsModule => AppModule.Settings,
            _ => null
        };
    }
}
