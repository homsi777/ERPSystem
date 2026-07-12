using ERPSystem.Application.DTOs.Expenses;
using ERPSystem.Controls;
using ERPSystem.Core;
using ERPSystem.Helpers;
using ERPSystem.Services;
using ERPSystem.Services.Expenses;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using WpfApplication = System.Windows.Application;

namespace ERPSystem.Controls.Expenses;

public sealed class ExpenseDashboardControl : UserControl
{
    private readonly StackPanel _root = new();
    private readonly StackPanel _kpiRow = new() { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 16) };
    private readonly StackPanel _chartsHost = new();

    public ExpenseDashboardControl()
    {
        var scroll = new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            Padding = new Thickness(16),
            Content = _root
        };
        Content = scroll;
        Background = (SolidColorBrush)WpfApplication.Current.Resources["AppBgBrush"]!;

        _root.Children.Add(ErpUiFactory.SectionTitle("لوحة المصاريف التنفيذية"));
        _root.Children.Add(new TextBlock
        {
            Text = "نظرة شاملة على مصاريف الشركة — بيانات حية من PostgreSQL",
            Foreground = Br("TextSecondaryBrush"),
            Margin = new Thickness(0, 0, 0, 16)
        });
        _root.Children.Add(_kpiRow);
        _root.Children.Add(_chartsHost);

        Loaded += async (_, _) => await LoadAsync();
        ErpDataRefreshHub.DataChanged += OnDataChanged;
        Unloaded += (_, _) => ErpDataRefreshHub.DataChanged -= OnDataChanged;
    }

    private void OnDataChanged(ErpDataRefreshScope scope)
    {
        if ((scope & ErpDataRefreshScope.All) != 0 && IsLoaded)
            _ = LoadAsync();
    }

    private async Task LoadAsync()
    {
        if (!AppServices.IsInitialized)
            return;

        var result = await ExpenseUiService.Instance.GetDashboardAsync();
        if (!ApplicationResultPresenter.Present(result) || result.Value is null)
            return;

        Render(result.Value);
    }

    private void Render(ExpenseDashboardDto d)
    {
        var cards = new (string title, string value, string icon, SolidColorBrush color)[]
        {
            ("إجمالي المصاريف", $"{d.TotalExpensesBase:N0} {d.BaseCurrency}", "\uE9D9", B("PrimaryBrush")),
            ("مصاريف الشهر", $"{d.MonthlyExpensesBase:N0} {d.BaseCurrency}", "\uE8C1", B("AccentSalesBrush")),
            ("رأسمالية", $"{d.CapitalExpensesBase:N0}", "\uE8B7", B("AccentOrdersBrush")),
            ("شخصية", $"{d.PersonalExpensesBase:N0}", "\uE716", B("InfoBrush")),
            ("تشغيلية", $"{d.OperatingExpensesBase:N0}", "\uE821", B("AccentInventoryBrush")),
            ("نشطة", d.ActiveCount.ToString(), "\uE73E", B("SuccessBrush")),
            ("بانتظار الاعتماد", d.PendingApprovalCount.ToString(), "\uE823", B("WarningBrush")),
            ("معدل الحرق الشهري", $"{d.BurnRateMonthly:N0}", "\uE9D2", B("AccentPayableBrush")),
            ("سنوي", $"{d.YearlyExpensesBase:N0}", "\uE8C1", B("InfoBrush")),
            ("دفعات قادمة", d.UpcomingPaymentsCount.ToString(), "\uE787", B("WarningBrush")),
            ("متأخرة", d.OverdueCount.ToString(), "\uE783", B("DangerBrush")),
            ("أكبر مصروف", d.LargestExpenseBase > 0 ? $"{d.LargestExpenseBase:N0}" : "—", "\uE9D2", B("AccentPayableBrush"))
        };
        ErpUiFactory.SetSummaryCards(_kpiRow, cards);

        _chartsHost.Children.Clear();

        var grid = new Grid { Margin = new Thickness(0, 8, 0, 0) };
        grid.ColumnDefinitions.Add(new ColumnDefinition());
        grid.ColumnDefinitions.Add(new ColumnDefinition());

        var left = new StackPanel { Margin = new Thickness(0, 0, 8, 0) };
        left.Children.Add(ErpUiFactory.SectionTitle("الاتجاه الشهري"));
        left.Children.Add(ErpUiFactory.Card(BuildTrendChart(d.MonthlyTrend)));

        var right = new StackPanel { Margin = new Thickness(8, 0, 0, 0) };
        right.Children.Add(ErpUiFactory.SectionTitle("توزيع الفئات"));
        right.Children.Add(ErpUiFactory.Card(BuildCategoryChart(d.CategoryBreakdown)));

        Grid.SetColumn(left, 0);
        Grid.SetColumn(right, 1);
        grid.Children.Add(left);
        grid.Children.Add(right);
        _chartsHost.Children.Add(grid);

        var grid2 = new Grid { Margin = new Thickness(0, 16, 0, 0) };
        grid2.ColumnDefinitions.Add(new ColumnDefinition());
        grid2.ColumnDefinitions.Add(new ColumnDefinition());

        var curr = new StackPanel { Margin = new Thickness(0, 0, 8, 0) };
        curr.Children.Add(ErpUiFactory.SectionTitle("توزيع العملات"));
        curr.Children.Add(ErpUiFactory.Card(BuildCurrencyPanel(d.CurrencyBreakdown)));

        var dept = new StackPanel { Margin = new Thickness(8, 0, 0, 0) };
        dept.Children.Add(ErpUiFactory.SectionTitle("الأقسام ومراكز التكلفة"));
        dept.Children.Add(ErpUiFactory.Card(BuildDeptCostPanel(d)));

        Grid.SetColumn(curr, 0);
        Grid.SetColumn(dept, 1);
        grid2.Children.Add(curr);
        grid2.Children.Add(dept);
        _chartsHost.Children.Add(grid2);

        if (!string.IsNullOrWhiteSpace(d.LargestExpenseName))
        {
            _root.Children.Add(ErpUxFactory.InfoBanner(
                $"أكبر مصروف: {d.LargestExpenseName} — {d.LargestExpenseBase:N0} {d.BaseCurrency}", "info"));
        }

        var actions = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 16, 0, 0) };
        var listBtn = new Button
        {
            Content = "سجل القيود",
            Style = S("PrimaryButtonStyle"),
            Margin = new Thickness(0, 0, 8, 0)
        };
        listBtn.Click += (_, _) => MockInteractionService.Navigate(AppModule.Expenses, "Entries");
        var newBtn = new Button { Content = "قيد مصروف جديد", Style = S("SecondaryButtonStyle") };
        newBtn.Click += (_, _) => ExpensePopupService.ShowEntry();
        actions.Children.Add(listBtn);
        actions.Children.Add(newBtn);
        _chartsHost.Children.Add(actions);
    }

    private static UIElement BuildTrendChart(IReadOnlyList<ExpenseMonthlyTrendDto> points)
    {
        if (points.Count == 0)
            return EmptyChart("لا توجد بيانات كافية");

        var max = points.Max(p => p.AmountBase);
        if (max <= 0) max = 1;

        var row = new StackPanel { Orientation = Orientation.Horizontal, Height = 140, VerticalAlignment = VerticalAlignment.Bottom };
        foreach (var p in points)
        {
            var h = Math.Max(4, (double)(p.AmountBase / max) * 120);
            var col = new StackPanel { Width = 36, Margin = new Thickness(4, 0, 4, 0), VerticalAlignment = VerticalAlignment.Bottom };
            col.Children.Add(new Border
            {
                Height = h,
                Background = B("PrimaryBrush"),
                CornerRadius = new CornerRadius(4, 4, 0, 0),
                ToolTip = $"{p.Label}: {p.AmountBase:N0}"
            });
            col.Children.Add(new TextBlock
            {
                Text = p.Label.Length > 7 ? p.Label[^5..] : p.Label,
                FontSize = 9,
                Foreground = Br("TextMutedBrush"),
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 4, 0, 0)
            });
            row.Children.Add(col);
        }
        return row;
    }

    private static UIElement BuildCategoryChart(IReadOnlyList<ExpenseCategoryBreakdownDto> points)
    {
        if (points.Count == 0)
            return EmptyChart("لا توجد بيانات");

        var stack = new StackPanel();
        foreach (var p in points.OrderByDescending(x => x.AmountBase))
        {
            var row = new Grid { Margin = new Thickness(0, 6, 0, 6) };
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(100) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(70) });

            var label = new TextBlock { Text = p.Label, FontSize = 12, Foreground = Br("TextSecondaryBrush") };
            var barBg = new Border
            {
                Background = Br("SurfaceAltBrush"),
                CornerRadius = new CornerRadius(4),
                Height = 10,
                Margin = new Thickness(8, 0, 8, 0)
            };
            var barFg = new Border
            {
                Background = B("AccentPayableBrush"),
                CornerRadius = new CornerRadius(4),
                HorizontalAlignment = HorizontalAlignment.Left,
                Width = Math.Max(4, (double)p.Percentage * 1.5),
                Height = 10
            };
            barBg.Child = barFg;

            var val = new TextBlock
            {
                Text = $"{p.AmountBase:N0}",
                FontSize = 11,
                Foreground = Br("TextPrimaryBrush"),
                HorizontalAlignment = HorizontalAlignment.Right
            };

            Grid.SetColumn(label, 0);
            Grid.SetColumn(barBg, 1);
            Grid.SetColumn(val, 2);
            row.Children.Add(label);
            row.Children.Add(barBg);
            row.Children.Add(val);
            stack.Children.Add(row);
        }
        return stack;
    }

    private static UIElement BuildCurrencyPanel(IReadOnlyList<ExpenseCurrencyBreakdownDto> points)
    {
        if (points.Count == 0)
            return EmptyChart("لا توجد عملات");

        return ErpUiFactory.BuildGrid(points.Select(p => new
        {
            العملة = p.Currency,
            الأصلي = AppFormats.Amount(p.AmountOriginal),
            بالأساس = AppFormats.Amount(p.AmountBase)
        }).ToList(), false);
    }

    private static UIElement BuildDeptCostPanel(ExpenseDashboardDto d)
    {
        var stack = new StackPanel();
        stack.Children.Add(new TextBlock
        {
            Text = "الأقسام",
            FontWeight = FontWeights.SemiBold,
            Margin = new Thickness(0, 0, 0, 6)
        });
        if (d.DepartmentBreakdown.Count == 0)
            stack.Children.Add(new TextBlock { Text = "—", Foreground = Br("TextMutedBrush") });
        else
            foreach (var item in d.DepartmentBreakdown)
                stack.Children.Add(new TextBlock
                {
                    Text = $"{item.Department}: {item.AmountBase:N0}",
                    Margin = new Thickness(0, 2, 0, 2)
                });

        stack.Children.Add(new TextBlock
        {
            Text = "مراكز التكلفة",
            FontWeight = FontWeights.SemiBold,
            Margin = new Thickness(0, 12, 0, 6)
        });
        if (d.CostCenterBreakdown.Count == 0)
            stack.Children.Add(new TextBlock { Text = "—", Foreground = Br("TextMutedBrush") });
        else
            foreach (var item in d.CostCenterBreakdown)
                stack.Children.Add(new TextBlock
                {
                    Text = $"{item.CostCenter}: {item.AmountBase:N0}",
                    Margin = new Thickness(0, 2, 0, 2)
                });
        return stack;
    }

    private static TextBlock EmptyChart(string msg) => new()
    {
        Text = msg,
        Foreground = Br("TextMutedBrush"),
        Margin = new Thickness(0, 24, 0, 24),
        HorizontalAlignment = HorizontalAlignment.Center
    };

    private static SolidColorBrush B(string k) => (SolidColorBrush)WpfApplication.Current.Resources[k]!;
    private static Brush Br(string k) => (Brush)WpfApplication.Current.Resources[k]!;
    private static Style S(string k) => (Style)WpfApplication.Current.Resources[k]!;
}
