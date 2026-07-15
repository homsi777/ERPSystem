using ERPSystem.Core;
using ERPSystem.Application.DependencyInjection;
using ERPSystem.Application.Abstractions.Services;
using ERPSystem.Application.Diagnostics;
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
using ERPSystem.Helpers;
using ERPSystem.Views.Auth;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Windows;
using System.Windows.Controls;

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

        EventManager.RegisterClassHandler(
            typeof(DatePicker),
            FrameworkElement.LoadedEvent,
            new RoutedEventHandler((sender, _) =>
            {
                if (sender is DatePicker picker)
                    LatinDigitDatePickerHelper.Enable(picker);
            }));

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

            // SSH tunnel is for developer machines only (local host + tunnel enabled).
            // Company installs connect directly to alamal-ab.org over SSL.
            var connectionStatus = CreateConnectionStatusWindow();
            connectionStatus.Show();
            try
            {
                if (DesktopConnectionBootstrap.RequiresSshTunnel(configuration))
                    _sshTunnel = await SshTunnelService.StartIfConfiguredAsync(configuration);

                await DesktopConnectionBootstrap.ValidateDatabaseAsync(configuration);
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
            services.AddSingleton<ERPSystem.Services.Identity.IdentityUiService>();
            services.AddSingleton<ERPSystem.Services.Identity.AuthUiService>();

            var provider = services.BuildServiceProvider();
            AppServices.Initialize(provider);

            var profiler = provider.GetRequiredService<IWpfPerformanceProfiler>();

            using (var migrateScope = profiler.BeginScreenLoad("App.Startup.Migrate"))
            {
                using (migrateScope.MeasureDataLoad())
                    await provider.MigrateAsync();
            }

            using (var seedScope = profiler.BeginScreenLoad("App.Startup.Seed"))
            {
                using (seedScope.MeasureDataLoad())
                    await provider.SeedOnlyAsync();
            }

            using (var healthScope = profiler.BeginScreenLoad("App.Startup.AccountingHealth"))
            {
                using (healthScope.MeasureDataLoad())
                {
                    using var scope = provider.CreateScope();
                    var context = scope.ServiceProvider.GetRequiredService<ERPSystem.Infrastructure.Persistence.ErpDbContext>();
                    await ERPSystem.Infrastructure.Services.AccountingHealth.ValidateAsync(context);
                }
            }

            using (var currencyScope = profiler.BeginScreenLoad("App.Startup.CurrencyCatalog"))
            {
                using (currencyScope.MeasureDataLoad())
                    await ERPSystem.Services.Settings.CurrencyCatalog.RefreshAsync();
            }

            using (var referenceScope = profiler.BeginScreenLoad("App.Startup.ReferenceDataCatalog"))
            {
                using (referenceScope.MeasureDataLoad())
                    await ERPSystem.Services.Settings.ReferenceDataCatalog.RefreshAsync();
            }

            using (var windowScope = profiler.BeginScreenLoad("App.LoginWindow"))
            {
                LoginWindow loginWindow;
                using (windowScope.MeasureMapping())
                    loginWindow = new LoginWindow();
                using (windowScope.MeasureRendering())
                {
                    if (loginWindow.ShowDialog() != true)
                    {
                        Shutdown();
                        return;
                    }
                }
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
        string? sessionLog = null;
        if (AppServices.IsInitialized)
        {
            try
            {
                sessionLog = AppServices.GetRequiredService<IWpfPerformanceProfiler>().SessionLogFilePath;
            }
            catch
            {
                // Best-effort — never block shutdown.
            }
        }

        _sshTunnel?.Dispose();
        _sshTunnel = null;

        if (!string.IsNullOrWhiteSpace(sessionLog))
        {
            WpfSessionSummaryAnalyzer.TryWriteSummary(sessionLog);
            WriteStartupPhaseBreakdown(sessionLog);
        }

        base.OnExit(e);
    }

    private static void WriteStartupPhaseBreakdown(string sessionLogPath)
    {
        try
        {
            var dir = Path.GetDirectoryName(sessionLogPath);
            if (string.IsNullOrWhiteSpace(dir))
                return;

            var stamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
            var path = Path.Combine(dir, $"startup-phase-breakdown-{stamp}.json");
            var payload = new
            {
                SessionLog = sessionLogPath,
                GeneratedUtc = DateTime.UtcNow,
                Phases = StartupPhaseRecorder.GetTimings().Select(p => new
                {
                    p.Phase,
                    TotalMs = Math.Round(p.TotalMs, 1)
                })
            };
            File.WriteAllText(path, System.Text.Json.JsonSerializer.Serialize(payload, new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
        }
        catch
        {
            // Best-effort only.
        }
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
            Text = "جاري الاتصال بقاعدة البيانات...",
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            FontSize = 16
        }
    };
}
