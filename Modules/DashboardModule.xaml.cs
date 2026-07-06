using ERPSystem.Application.Abstractions.Services;
using ERPSystem.Application.Queries.Dashboard;
using ERPSystem.Application.UseCases.Queries;
using ERPSystem.Core;
using ERPSystem.Core.Actions;
using ERPSystem.Controls.China;
using ERPSystem.Core.ChinaImport;
using ERPSystem.Core.Customers;
using ERPSystem.Core.Workspace;
using ERPSystem.Helpers;
using ERPSystem.Core.Sales;
using ERPSystem.Services;
using ERPSystem.Domain.Enums;
using ERPSystem.Services.China;
using ERPSystem.Services.Customers;
using ERPSystem.Services.Sales;
using ERPSystem.Infrastructure.Seed;
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
            Unloaded += OnUnloaded;
            ErpDataRefreshHub.DataChanged += OnDataRefreshRequested;
        }

        private void OnUnloaded(object sender, RoutedEventArgs e) =>
            ErpDataRefreshHub.DataChanged -= OnDataRefreshRequested;

        private void OnDataRefreshRequested(ErpDataRefreshScope scope)
        {
            if ((scope & (ErpDataRefreshScope.Dashboard | ErpDataRefreshScope.All)) == 0)
                return;

            if (!IsLoaded)
                return;

            LoadOperationalTables();
            _ = LoadKpiCardsAsync();
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
            CardSales.TrendValue = "—";

            CardOrders.CardTitle = "بانتظار التفصيل";
            CardOrders.CardDescription = "فواتير تحتاج تفصيل أثواب";
            CardOrders.TrendValue = "—";

            CardInventory.CardTitle = "تكلفة استيراد معلّقة";
            CardInventory.CardDescription = "حاويات بانتظار Landing Cost";
            CardInventory.TrendValue = "—";

            CardReceivables.CardTitle = "التحصيل / الذمم";
            CardReceivables.CardDescription = "عملاء يحتاجون تحصيل";
            CardReceivables.TrendValue = "—";

            CardPayables.CardTitle = "الذمم الدائنة";
            CardPayables.CardDescription = "مستحقة للموردين";
            CardPayables.TrendValue = "—";
            CardPayables.TrendDirection = Controls.MetricTrend.Down;
            CardPayables.CardValue = "—";

            CardCustomers.CardTitle = "العملاء النشطون";
            CardCustomers.CardDescription = "خلال هذا الشهر";
            CardCustomers.TrendValue = "—";
            CardCustomers.CardValue = "—";

            CardSales.CardValue = "—";
            CardOrders.CardValue = "—";
            CardInventory.CardValue = "—";
            CardReceivables.CardValue = "—";

            InsightCardsRow.Visibility = Visibility.Collapsed;

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

            CardInventory.MouseLeftButtonUp += async (_, _) => await OpenPendingLandingCostAsync();
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
            ContainersChartPanel.MouseLeftButtonUp += (_, _) =>
                MockInteractionService.Navigate(AppModule.ChinaImport, "Containers");
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
                CardSales.CardValue = AppFormats.CurrencyUsd(dto.TodaySalesTotal);
                CardOrders.CardValue = AppFormats.Number(dto.AwaitingDetailingCount);
                CardInventory.CardValue = AppFormats.Number(dto.PendingContainersCount);
                CardReceivables.CardValue = AppFormats.CurrencyUsd(dto.TotalCustomerOutstanding);
                CardPayables.CardValue = AppFormats.CurrencyUsd(dto.TotalSupplierPayables);
                CardCustomers.CardValue = AppFormats.Number(dto.ActiveCustomersCount);

                PopulateActivityFeed(dto.RecentActivity);
            }
            catch
            {
                // Keep fallback/placeholder values if the summary query fails.
            }
        }

        private void PopulateActivityFeed(IReadOnlyList<ERPSystem.Application.DTOs.Dashboard.DashboardActivityDto> activity)
        {
            ActivityList.Children.Clear();

            if (ERPSystem.Infrastructure.Services.AccountingHealth.HasMissingAccounts)
            {
                var names = string.Join("، ", ERPSystem.Infrastructure.Services.AccountingHealth.MissingRequiredAccounts);
                ActivityList.Children.Add(new Border
                {
                    Background = Br("DangerBgBrush"),
                    CornerRadius = new CornerRadius(8),
                    Padding = new Thickness(12),
                    Margin = new Thickness(0, 0, 0, 8),
                    Child = new TextBlock
                    {
                        Text = $"تحذير: بعض الحسابات الأساسية غير مكوّنة ({names}) — راجع إعدادات المحاسبة.",
                        Foreground = Br("DangerBrush"),
                        TextWrapping = TextWrapping.Wrap,
                        FontFamily = Ff()
                    }
                });
            }

            if (activity.Count == 0)
            {
                ActivityList.Children.Add(new TextBlock
                {
                    Text = "لا يوجد نشاط حديث",
                    Foreground = Br("TextMutedBrush"),
                    FontSize = 12,
                    FontFamily = Ff()
                });
                return;
            }

            foreach (var a in activity)
            {
                var row = new Grid { Margin = new Thickness(0, 0, 0, 8), Cursor = Cursors.Hand };
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                var desc = new TextBlock
                {
                    Text = a.Description,
                    FontSize = 12,
                    Foreground = Br("TextPrimaryBrush"),
                    FontFamily = Ff(),
                    VerticalAlignment = VerticalAlignment.Center
                };
                Grid.SetColumn(desc, 0);
                row.Children.Add(desc);

                var when = new TextBlock
                {
                    Text = a.OccurredAt.ToLocalTime().ToString("MM/dd HH:mm"),
                    FontSize = 11,
                    Foreground = Br("TextMutedBrush"),
                    FontFamily = Ff()
                };
                Grid.SetColumn(when, 1);
                row.Children.Add(when);

                ActivityList.Children.Add(row);
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
            _ = LoadContainersChartAsync();
            _ = LoadPendingWarehouseTasksAsync();
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
                    Text = $"{c.Balance:N0} $", FontSize = 12, FontWeight = FontWeights.SemiBold,
                    Foreground = Br("DangerBrush"), FontFamily = Ff()
                };
                Grid.SetColumn(bal, 1);
                row.Children.Add(bal);

                row.MouseLeftButtonUp += (_, _) => MockInteractionService.OpenCustomerStatement(c);

                DebtCustomersPanel.Children.Add(row);
            }
        }

        private async Task LoadContainersChartAsync()
        {
            ContainersChartPanel.Child = null;
            var stack = new StackPanel { Margin = new Thickness(12, 10, 12, 10) };

            if (!AppServices.IsInitialized)
            {
                stack.Children.Add(new TextBlock
                {
                    Text = "لا توجد حاويات لعرضها.",
                    FontSize = 12,
                    Foreground = Br("TextSecondaryBrush"),
                    FontFamily = Ff()
                });
                ContainersChartPanel.Child = stack;
                return;
            }

            var result = await ContainerUiService.Instance.GetListAsync(null, null, 1, 4);
            if (!result.IsSuccess || result.Value?.Items.Count == 0)
            {
                stack.Children.Add(new TextBlock
                {
                    Text = "لا توجد حاويات مستوردة.",
                    FontSize = 12,
                    Foreground = Br("TextSecondaryBrush"),
                    FontFamily = Ff()
                });
                ContainersChartPanel.Child = stack;
                return;
            }

            foreach (var dto in result.Value!.Items)
            {
                var row = ContainerListRow.FromDto(dto);
                var grid = new Grid { Margin = new Thickness(0, 0, 0, 8), Cursor = Cursors.Hand };
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                var dotColor = row.StatusDisplay switch
                {
                    "واصلة" => Br("PrimaryBrush"),
                    "قيد المراجعة" or "مراجعة التكلفة" => Br("AccentReceivableBrush"),
                    "معتمدة" or "في المخزن" => Br("SuccessBrush"),
                    _ => Br("AccentOrdersBrush")
                };
                grid.Children.Add(new Border
                {
                    Width = 8, Height = 8, CornerRadius = new CornerRadius(4),
                    Background = dotColor, Margin = new Thickness(0, 0, 10, 0),
                    VerticalAlignment = VerticalAlignment.Center
                });
                grid.Children.Add(new TextBlock
                {
                    Text = $"{row.ContainerNumber} — {row.SupplierName}",
                    FontSize = 12, Foreground = Br("TextPrimaryBrush"), FontFamily = Ff()
                });
                Grid.SetColumn(grid.Children[1], 1);
                grid.Children.Add(new TextBlock
                {
                    Text = row.StatusDisplay, FontSize = 11, Foreground = Br("TextSecondaryBrush"), FontFamily = Ff()
                });
                Grid.SetColumn(grid.Children[2], 2);
                var captured = row;
                grid.MouseLeftButtonUp += (_, _) => ChinaImportNavigation.OpenOperationsCenter(captured);
                stack.Children.Add(grid);
            }

            ContainersChartPanel.Child = stack;
        }

        private static async Task OpenPendingLandingCostAsync()
        {
            if (!AppServices.IsInitialized)
            {
                MockInteractionService.Navigate(AppModule.ChinaImport, "Containers");
                return;
            }

            var result = await ContainerUiService.Instance.GetListAsync(
                null, ChinaContainerStatus.LandingCostReviewed, 1, 1);
            if (result.IsSuccess && result.Value?.Items.Count > 0)
            {
                ChinaImportNavigation.OpenLandingCostWorkspace(ContainerListRow.FromDto(result.Value.Items[0]));
                return;
            }

            MockInteractionService.Navigate(AppModule.ChinaImport, "Containers");
        }

        private async Task LoadPendingWarehouseTasksAsync()
        {
            RecentGrid.Columns.Clear();
            RecentGrid.Columns.Add(Col("رقم الفاتورة", nameof(DashboardDetailingQueueRow.Invoice), 120));
            RecentGrid.Columns.Add(Col("العميل", nameof(DashboardDetailingQueueRow.Customer), "*"));
            RecentGrid.Columns.Add(Col("تاريخ الإرسال", nameof(DashboardDetailingQueueRow.SentDate), 110));
            RecentGrid.Columns.Add(Col("الأثواب", nameof(DashboardDetailingQueueRow.Rolls), 80));
            RecentGrid.Columns.Add(StatusCol("الحالة", nameof(DashboardDetailingQueueRow.Status)));
            RecentGrid.MouseDoubleClick -= RecentGrid_OnDoubleClick;
            RecentGrid.MouseDoubleClick += RecentGrid_OnDoubleClick;

            if (!AppServices.IsInitialized)
            {
                RecentGrid.ItemsSource = Array.Empty<DashboardDetailingQueueRow>();
                TxtTableCount.Text = "لا توجد فواتير بانتظار التفصيل";
                return;
            }

            var result = await SalesUiService.Instance.GetDetailingQueueAsync(DatabaseSeeder.DefaultWarehouseId);
            if (!result.IsSuccess || result.Value is null)
            {
                RecentGrid.ItemsSource = Array.Empty<DashboardDetailingQueueRow>();
                TxtTableCount.Text = "لا توجد فواتير بانتظار التفصيل";
                return;
            }

            var rows = result.Value.Select(dto => new DashboardDetailingQueueRow(
                dto.InvoiceId,
                dto.InvoiceNumber,
                dto.CustomerName,
                dto.SentToWarehouseAt?.ToString("yyyy/MM/dd") ?? "—",
                $"{dto.Rolls.Count} توب",
                "بانتظار التفصيل")).ToList();

            RecentGrid.ItemsSource = rows;
            TxtTableCount.Text = rows.Count == 0
                ? "لا توجد فواتير بانتظار التفصيل"
                : $"عرض 1 إلى {rows.Count} من {rows.Count} سجل";
        }

        private void RecentGrid_OnDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (RecentGrid.SelectedItem is DashboardDetailingQueueRow task)
            {
                SalesNavigationContext.BeginDetailing(task.InvoiceId, task.Invoice);
                MockInteractionService.NavigateToWarehouseDetailing(task.Invoice);
            }
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
        public record DashboardDetailingQueueRow(
            Guid InvoiceId,
            string Invoice,
            string Customer,
            string SentDate,
            string Rolls,
            string Status);
        public record DashboardActionRequest(AppModule Module, string SubPage);
    }
}
