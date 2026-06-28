using ERPSystem.Application.Abstractions.Services;
using ERPSystem.Application.Queries.Dashboard;
using ERPSystem.Application.UseCases.Queries;
using ERPSystem.Core;
using ERPSystem.Core.Actions;
using ERPSystem.Core.ChinaImport;
using ERPSystem.Core.Customers;
using ERPSystem.Core.Workspace;
using ERPSystem.Helpers;
using ERPSystem.Core.Sales;
using ERPSystem.Services;
using ERPSystem.Services.Customers;
using ERPSystem.Views.Sales;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;

namespace ERPSystem.Modules
{
    public partial class DashboardModule : UserControl
    {
        public event EventHandler<AppModule>? NavigationRequested;
        public event EventHandler<DashboardActionRequest>? ActionRequested;

        private bool _cardsWired;
        private bool _languageSubscribed;

        public DashboardModule()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (!_languageSubscribed)
            {
                LocalizationManager.Instance.LanguageChanged += OnLanguageChanged;
                _languageSubscribed = true;
            }
            ApplyOperationalDashboard();
        }

        private void OnLanguageChanged(object? sender, EventArgs e) => ApplyOperationalDashboard();

        private void ApplyOperationalDashboard()
        {
            TxtPageTitle.Text = "لوحة التحكم";
            TxtPageSubtitle.Text = "نظرة عامة على أداء أعمالك لهذا اليوم";
            TxtBtnNewInvoice.Text = "فاتورة جديدة";

            CardSales.CardTitle = "مبيعات اليوم";
            CardSales.CardDescription = "مقارنة بالأمس";
            CardSales.TrendValue = "↑ 12.5%";

            CardOrders.CardTitle = "بانتظار التفصيل";
            CardOrders.CardDescription = "فواتير تحتاج تفصيل أثواب";
            CardOrders.TrendValue = "3 عاجلة";

            CardInventory.CardTitle = "تكلفة استيراد معلّقة";
            CardInventory.CardDescription = "حاويات بانتظار Landing Cost";
            CardInventory.TrendValue = "CN-2026-001";

            CardReceivables.CardTitle = "التحصيل / الذمم";
            CardReceivables.CardDescription = "عملاء يحتاجون تحصيل";
            CardReceivables.TrendValue = "↑ 5.2%";

            CardPayables.CardTitle = "الذمم الدائنة";
            CardPayables.CardDescription = "مستحقة للموردين";
            CardPayables.TrendValue = "↓ 3.8%";
            CardPayables.TrendDirection = Controls.MetricTrend.Down;
            // TODO: not supported by handler yet — supplier payables balance
            CardPayables.CardValue = "32,100 ر.س";

            CardCustomers.CardTitle = "العملاء النشطون";
            CardCustomers.CardDescription = "خلال هذا الشهر";
            CardCustomers.TrendValue = "↑ 34 جديد";
            // TODO: not supported by handler yet — active customers count
            CardCustomers.CardValue = "2,847";

            CardSales.CardValue = "—";
            CardOrders.CardValue = "—";
            CardInventory.CardValue = "—";
            CardReceivables.CardValue = "—";

            TxtQA_Invoice.Text = "فاتورة بيع جديدة";
            TxtQA_Container.Text = "استيراد حاوية";
            TxtQA_Customer.Text = "مركز عمليات عميل";
            TxtQA_Product.Text = "تقرير مخزون";
            TxtQA_Purchase.Text = "سند قبض";
            BtnInventoryReport.Tag = "InventoryReport";
            BtnNewPurchase.Tag = "ReceiptVoucher";
            BtnNewCustomer.Tag = "CustomerOpsCenter";
            BtnNewReport.Visibility = Visibility.Collapsed;

            ActivityHeader.Title = "آخر النشاط";
            PendingHeader.Title = "فواتير بانتظار المتابعة";
            ChartHeader.Title = "نظرة عامة على الأعمال";
            ChartHeader.Subtitle = "حاويات قادمة — انقر لفتح مركز الحاوية";
            ProductsHeader.Title = "عملاء يحتاجون تحصيل";

            LoadOperationalTables();
            WireCardClicks();
            _ = LoadKpiCardsAsync();
        }

        private void WireCardClicks()
        {
            if (_cardsWired) return;
            _cardsWired = true;

            CardInventory.MouseLeftButtonUp += (_, _) => MockInteractionService.OpenLandingCostWorkspace();
            CardReceivables.MouseLeftButtonUp += (_, _) =>
                MockInteractionService.Navigate(AppModule.Customers, "List");
            CardPayables.MouseLeftButtonUp += (_, _) =>
            {
                MockInteractionService.Navigate(AppModule.Suppliers, "Statement");
            };
            CardCustomers.MouseLeftButtonUp += (_, _) => MockInteractionService.Navigate(AppModule.Customers, "List");
            CardSales.MouseLeftButtonUp += (_, _) => MockInteractionService.Navigate(AppModule.Sales, "Invoices");
            CardOrders.MouseLeftButtonUp += (_, _) =>
                MockInteractionService.NavigateToWarehouseDetailing();
            ContainersChartPanel.Cursor = Cursors.Hand;
            ContainersChartPanel.MouseLeftButtonUp += (_, _) => MockInteractionService.OpenContainerOperationsCenter();
        }

        private void BtnHeaderRefresh_Click(object sender, RoutedEventArgs e)
        {
            LoadOperationalTables();
            _ = LoadKpiCardsAsync();
            MockInteractionService.ShowSuccess("تم تحديث بيانات لوحة التحكم.", "تحديث");
        }

        private async Task LoadKpiCardsAsync()
        {
            if (!AppServices.IsInitialized)
                return;

            try
            {
                using var scope = AppServices.CreateScope();
                var branch = scope.ServiceProvider.GetRequiredService<ICurrentBranchService>();
                if (branch.CompanyId is not Guid companyId || branch.BranchId is not Guid branchId)
                    return;

                var handler = scope.ServiceProvider.GetRequiredService<GetDashboardSummaryHandler>();
                var result = await handler.HandleAsync(new GetDashboardSummaryQuery
                {
                    CompanyId = companyId,
                    BranchId = branchId
                });

                if (!result.IsSuccess || result.Value is null)
                    return;

                var dto = result.Value;
                CardSales.CardValue = $"{dto.TodaySalesTotal:N0} ر.س";
                CardOrders.CardValue = dto.AwaitingDetailingCount.ToString();
                CardInventory.CardValue = dto.PendingContainersCount.ToString();
                CardReceivables.CardValue = $"{dto.TotalCustomerOutstanding:N0} ر.س";
            }
            catch
            {
                // Keep fallback/placeholder values if the summary query fails.
            }
        }

        private void BtnHeaderNewInvoice_Click(object sender, RoutedEventArgs e)
        {
            NavigationRequested?.Invoke(this, AppModule.Sales);
            ActionRequested?.Invoke(this, new DashboardActionRequest(AppModule.Sales, "NewInvoice"));
        }

        private void LoadOperationalTables()
        {
            _ = LoadDebtCustomersAsync();
            LoadContainersChart();
            LoadPendingWarehouseTasks();
        }

        private async Task LoadDebtCustomersAsync()
        {
            DebtCustomersPanel.Children.Clear();

            if (!AppServices.IsInitialized)
                return;

            var result = await CustomerUiService.Instance.GetListAsync(null, 1, 50);
            if (!result.IsSuccess || result.Value is null)
                return;

            var customers = result.Value.Items
                .Where(c => c.Balance > 0)
                .OrderByDescending(c => c.Balance)
                .Take(5)
                .Select(CustomerListRow.FromDto)
                .ToList();

            foreach (var c in customers)
            {
                var row = new Grid { Margin = new Thickness(0, 0, 0, ErpDesignTokens.SpaceMd), Cursor = Cursors.Hand };
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                var name = new TextBlock
                {
                    Text = c.NameAr, FontSize = 12, Foreground = Br("TextPrimaryBrush"),
                    FontFamily = Ff(), VerticalAlignment = VerticalAlignment.Center
                };
                Grid.SetColumn(name, 0);
                row.Children.Add(name);
                var bal = new TextBlock
                {
                    Text = $"{c.Balance:N0} ر.س", FontSize = 12, FontWeight = FontWeights.SemiBold,
                    Foreground = Br("DangerBrush"), FontFamily = Ff()
                };
                Grid.SetColumn(bal, 1);
                row.Children.Add(bal);

                row.MouseLeftButtonUp += (_, _) => MockInteractionService.OpenCustomerStatement(c);

                DebtCustomersPanel.Children.Add(row);
            }
        }

        private void LoadContainersChart()
        {
            ContainersChartPanel.Child = null;
            var stack = new StackPanel { Margin = new Thickness(12, 10, 12, 10) };
            var containers = ChinaImportSampleData.Generate(5).Take(4).ToList();
            foreach (var c in containers)
            {
                var row = new Grid { Margin = new Thickness(0, 0, 0, 8), Cursor = Cursors.Hand };
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                var dotColor = c.StatusDisplay switch
                {
                    "واصلة" => Br("PrimaryBrush"),
                    "قيد المراجعة" => Br("AccentReceivableBrush"),
                    "معتمدة" => Br("SuccessBrush"),
                    _ => Br("AccentOrdersBrush")
                };
                row.Children.Add(new Border
                {
                    Width = 8, Height = 8, CornerRadius = new CornerRadius(4),
                    Background = dotColor, Margin = new Thickness(0, 0, 10, 0),
                    VerticalAlignment = VerticalAlignment.Center
                });
                row.Children.Add(new TextBlock
                {
                    Text = $"{c.ContainerNumber} — {c.SupplierName}",
                    FontSize = 12, Foreground = Br("TextPrimaryBrush"), FontFamily = Ff()
                });
                Grid.SetColumn(row.Children[1], 1);
                row.Children.Add(new TextBlock
                {
                    Text = c.StatusDisplay, FontSize = 11, Foreground = Br("TextSecondaryBrush"), FontFamily = Ff()
                });
                Grid.SetColumn(row.Children[2], 2);
                var captured = c;
                row.MouseLeftButtonUp += (_, _) => MockInteractionService.OpenContainerOperationsCenter(captured);
                stack.Children.Add(row);
            }
            ContainersChartPanel.Child = stack;
        }

        private void LoadPendingWarehouseTasks()
        {
            var data = new List<WarehouseTaskRow>
            {
                new("INV-2026-0088", "أحمد الحمصي", "CN-2026-001", "5 أثواب", "بانتظار التفصيل"),
                new("INV-2026-0085", "مؤسسة النسيج", "CN-2026-012", "12 توب", "بانتظار التفصيل"),
                new("INV-2026-0082", "فهد الغامدي", "CN-2026-008", "3 أثواب", "بانتظار التفصيل"),
            };
            RecentGrid.ItemsSource = data;
            RecentGrid.Columns.Clear();
            RecentGrid.Columns.Add(Col("رقم الفاتورة", nameof(WarehouseTaskRow.Invoice), 120));
            RecentGrid.Columns.Add(Col("العميل", nameof(WarehouseTaskRow.Customer), "*"));
            RecentGrid.Columns.Add(Col("الحاوية", nameof(WarehouseTaskRow.Container), 110));
            RecentGrid.Columns.Add(Col("الأثواب", nameof(WarehouseTaskRow.Rolls), 80));
            RecentGrid.Columns.Add(StatusCol("الحالة", nameof(WarehouseTaskRow.Status)));
            TxtTableCount.Text = $"عرض 1 إلى {data.Count} من {data.Count} سجل";
            RecentGrid.MouseDoubleClick -= RecentGrid_OnDoubleClick;
            RecentGrid.MouseDoubleClick += RecentGrid_OnDoubleClick;
        }

        private void RecentGrid_OnDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (RecentGrid.SelectedItem is WarehouseTaskRow task)
                MockInteractionService.OpenDetailingWorkspace(task.Invoice);
        }

        private static DataGridTextColumn Col(string h, string p, object w) => new()
        {
            Header = h,
            Binding = new Binding(p),
            Width = w is string ? new DataGridLength(1, DataGridLengthUnitType.Star) : new DataGridLength(Convert.ToDouble(w))
        };

        private static DataGridTemplateColumn StatusCol(string header, string path)
        {
            var col = new DataGridTemplateColumn { Header = header, Width = new DataGridLength(130) };
            var factory = new FrameworkElementFactory(typeof(Border));
            factory.SetValue(Border.CornerRadiusProperty, new CornerRadius(100));
            factory.SetValue(Border.PaddingProperty, new Thickness(8, 3, 8, 3));
            factory.SetValue(Border.BackgroundProperty, Br("PrimaryVeryLightBrush"));
            factory.SetValue(Border.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            factory.SetValue(Border.MarginProperty, new Thickness(0, 4, 0, 4));
            var tb = new FrameworkElementFactory(typeof(TextBlock));
            tb.SetBinding(TextBlock.TextProperty, new Binding(path));
            tb.SetValue(TextBlock.FontSizeProperty, 11.0);
            tb.SetValue(TextBlock.ForegroundProperty, Br("PrimaryBrush"));
            tb.SetValue(TextBlock.FontWeightProperty, FontWeights.SemiBold);
            tb.SetValue(TextBlock.FontFamilyProperty, Ff());
            factory.AppendChild(tb);
            col.CellTemplate = new DataTemplate { VisualTree = factory };
            return col;
        }

        private void QuickAction_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn) return;
            switch (btn.Tag?.ToString())
            {
                case "NewInvoice":
                    NavigationRequested?.Invoke(this, AppModule.Sales);
                    ActionRequested?.Invoke(this, new DashboardActionRequest(AppModule.Sales, "NewInvoice"));
                    break;
                case "NewContainer":
                    NavigationRequested?.Invoke(this, AppModule.ChinaImport);
                    ActionRequested?.Invoke(this, new DashboardActionRequest(AppModule.ChinaImport, "NewImport"));
                    break;
                case "CustomerOpsCenter":
                    MockInteractionService.OpenCustomerOperationsCenter();
                    break;
                case "InventoryReport":
                    NavigationRequested?.Invoke(this, AppModule.Reports);
                    ActionRequested?.Invoke(this, new DashboardActionRequest(AppModule.Reports, "Inventory"));
                    break;
                case "ReceiptVoucher":
                    NavigationRequested?.Invoke(this, AppModule.Accounting);
                    ActionRequested?.Invoke(this, new DashboardActionRequest(AppModule.Accounting, "Receipts"));
                    break;
            }
        }

        private static Brush Br(string k) => (Brush)System.Windows.Application.Current.Resources[k]!;
        private static FontFamily Ff() => ErpDesignTokens.UiFont;

        public record TransactionRow(string Number, string Name, string Date, string Amount, string Status);
        public record WarehouseTaskRow(string Invoice, string Customer, string Container, string Rolls, string Status);
        public record DashboardActionRequest(AppModule Module, string SubPage);
    }
}
