using ERPSystem.Core;
using ERPSystem.Core.Navigation;
using ERPSystem.Modules;
using System.Windows;

namespace ERPSystem
{
    public partial class MainWindow : Window
    {
        private readonly DashboardModule _dashboard;
        private readonly ChinaImportModule _chinaImport;
        private readonly SalesModule _sales;
        private readonly InventoryModule _inventory;
        private readonly CustomersModule _customers;
        private readonly SuppliersModule _suppliers;
        private readonly AccountingModule _accounting;
        private readonly ExpensesModule _expenses;
        private readonly CapitalPartnersModule _capitalPartners;
        private readonly ReportsModule _reports;
        private readonly PurchasesModule _purchases;
        private readonly HRModule _hr;
        private readonly SettingsModule _settings;
        private bool _languageSubscribed;
        private bool _navigationSubscribed;

        public MainWindow()
        {
            InitializeComponent();

            _dashboard = new DashboardModule();
            _chinaImport = new ChinaImportModule();
            _sales = new SalesModule();
            _inventory = new InventoryModule();
            _customers = new CustomersModule();
            _suppliers = new SuppliersModule();
            _accounting = new AccountingModule();
            _expenses = new ExpensesModule();
            _capitalPartners = new CapitalPartnersModule();
            _reports = new ReportsModule();
            _purchases = new PurchasesModule();
            _hr = new HRModule();
            _settings = new SettingsModule();

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
            var currentModule = ResolveActiveModule();
            if (currentModule.HasValue
                && currentModule.Value != req.Module
                && !UnsavedWorkGuard.TryConfirmLeave())
                return;

            WorkspaceHost.Content = req.Module switch
            {
                AppModule.Dashboard   => _dashboard,
                AppModule.ChinaImport => _chinaImport,
                AppModule.Sales       => _sales,
                AppModule.Inventory   => _inventory,
                AppModule.Customers   => _customers,
                AppModule.Suppliers   => _suppliers,
                AppModule.Accounting  => _accounting,
                AppModule.Expenses    => _expenses,
                AppModule.CapitalPartners => _capitalPartners,
                AppModule.Reports     => _reports,
                AppModule.Purchases   => _purchases,
                AppModule.HR          => _hr,
                AppModule.Settings    => _settings,
                _                     => _dashboard
            };

            if (WorkspaceHost.Content is ISubpageNavigator nav && !string.IsNullOrEmpty(req.SubPage))
                nav.NavigateSubpage(req.SubPage);

            NavBar.SetActiveModule(req.Module);
            Title = $"{GetModuleTitle(req.Module)} — ERP PRO";
            UpdateStatusBar(req);
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
            _                     => "ERP PRO"
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
