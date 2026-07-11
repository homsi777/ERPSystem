using ERPSystem.Core;
using ERPSystem.Application.DependencyInjection;
using ERPSystem.Application.Abstractions.Services;
using ERPSystem.Diagnostics.Performance;
using ERPSystem.Infrastructure.DependencyInjection;
using ERPSystem.Services;
using ERPSystem.Services.Customers;
using ERPSystem.Services.Suppliers;
using ERPSystem.Services.Purchases;
using ERPSystem.Services.Sales;
using ERPSystem.Services.China;
using ERPSystem.Services.Expenses;
using ERPSystem.Services.Inventory;
using ERPSystem.Services.Capital;
using ERPSystem.Services.Accounting;
using ERPSystem.Services.Finance;
using ERPSystem.Services.Reports;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Windows;

namespace ERPSystem;

public partial class App : System.Windows.Application
{
    private SshTunnelService? _sshTunnel;

    protected override async void OnStartup(StartupEventArgs e)
    {
        AppCulture.ConfigureWpfPresentation();
        AppCulture.Apply();
        LocalizationManager.Instance.LanguageChanged += (_, _) =>
            AppCulture.ApplyForLanguage(LocalizationManager.Instance.CurrentLanguage);

        base.OnStartup(e);
        ShutdownMode = ShutdownMode.OnExplicitShutdown;

        try
        {
            var services = new ServiceCollection();
            var configuration = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                // Optional per-machine override (e.g. cloud DB connection string).
                // Not tracked in git; place it next to ERPSystem.exe to point this
                // installation at the shared cloud database.
                .AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables()
                .Build();

            // Bring up the optional SSH tunnel without blocking the dispatcher thread.
            var connectionStatus = CreateConnectionStatusWindow();
            connectionStatus.Show();
            try
            {
                _sshTunnel = await SshTunnelService.StartIfConfiguredAsync(configuration);
            }
            finally
            {
                connectionStatus.Close();
            }

            // WPF Performance Rescue — Phase A instrumentation. Purely observational (counts EF Core
            // round-trips via the standard DiagnosticListener feed); never changes query behavior or results.
            EfQueryTelemetry.EnsureStarted();

            services.AddSingleton<IConfiguration>(configuration);
            services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Information));
            services.AddSingleton<IWpfPerformanceProfiler, WpfPerformanceProfiler>();

            services.AddSingleton<ICurrentUserService, WpfCurrentUserService>();
            services.AddSingleton<ICurrentBranchService, WpfCurrentBranchService>();

            services.AddInfrastructure(configuration);
            services.AddApplication();

            foreach (var descriptor in services.Where(d => d.ServiceType == typeof(INotificationService)).ToList())
                services.Remove(descriptor);
            services.AddSingleton<INotificationService, WpfNotificationService>();

            services.AddScoped<IPermissionService, WpfPermissionService>();
            services.AddSingleton<CustomerUiService>();
            services.AddSingleton<SupplierUiService>();
            services.AddSingleton<PurchaseUiService>();
            services.AddSingleton<SalesUiService>();
            services.AddSingleton<SalesReturnUiService>();
            services.AddSingleton<ContainerUiService>();
            services.AddSingleton<ContainerDocumentationService>();
            services.AddSingleton<ExpenseUiService>();
            services.AddSingleton<InventoryUiService>();
            services.AddSingleton<InventoryCatalogUiService>();
            services.AddSingleton<CapitalPartnerUiService>();
            services.AddSingleton<AccountingUiService>();
            services.AddSingleton<FinanceUiService>();
            services.AddSingleton<OpeningBalanceUiService>();
            services.AddSingleton<ModuleReportUiService>();
            services.AddSingleton<ERPSystem.Services.Settings.SettingsUiService>();
            services.AddSingleton<ERPSystem.Services.Search.GlobalSearchUiService>();
            services.AddSingleton<ERPSystem.Services.Hr.HrUiService>();

            var provider = services.BuildServiceProvider();
            AppServices.Initialize(provider);

            var profiler = provider.GetRequiredService<IWpfPerformanceProfiler>();
            using (var startupScope = profiler.BeginScreenLoad("App.Startup"))
            {
                using (startupScope.MeasureDataLoad())
                    await provider.MigrateAndSeedAsync();

                await ERPSystem.Services.Settings.CurrencyCatalog.RefreshAsync();
            }

            using (var windowScope = profiler.BeginScreenLoad("App.MainWindowConstruction"))
            {
                MainWindow window;
                using (windowScope.MeasureMapping())
                    window = new MainWindow();
                using (windowScope.MeasureRendering())
                    window.Show();
                MainWindow = window;
                ShutdownMode = ShutdownMode.OnMainWindowClose;
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"تعذّر تشغيل الأمل.AB:\n\n{ex.Message}",
                "خطأ في التشغيل",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            Shutdown(-1);
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _sshTunnel?.Dispose();
        _sshTunnel = null;
        base.OnExit(e);
    }

    private static Window CreateConnectionStatusWindow() => new()
    {
        Title = "الأمل.AB",
        Width = 360,
        Height = 110,
        WindowStartupLocation = WindowStartupLocation.CenterScreen,
        ResizeMode = ResizeMode.NoResize,
        WindowStyle = WindowStyle.ToolWindow,
        ShowInTaskbar = false,
        Content = new System.Windows.Controls.TextBlock
        {
            Text = "جاري الاتصال...",
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            FontSize = 16
        }
    };
}
