using ERPSystem.Application.DTOs.Capital;
using ERPSystem.Controls;
using ERPSystem.Core;
using ERPSystem.Helpers;
using ERPSystem.Services;
using ERPSystem.Services.Capital;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using WpfApplication = System.Windows.Application;

namespace ERPSystem.Controls.Capital;

public sealed class CapitalDashboardControl : UserControl
{
    private readonly StackPanel _root = new();
    private readonly StackPanel _kpiRow = new() { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 16) };
    private readonly StackPanel _chartsHost = new();

    public CapitalDashboardControl()
    {
        Content = new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            Padding = new Thickness(16),
            Content = _root
        };
        Background = (SolidColorBrush)WpfApplication.Current.Resources["AppBgBrush"]!;

        _root.Children.Add(ErpUiFactory.SectionTitle("لوحة رأس المال والشركاء"));
        _root.Children.Add(new TextBlock
        {
            Text = "نظرة تنفيذية على رأس المال والاستثمارات والشركاء",
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
        if (!AppServices.IsInitialized) return;
        var result = await CapitalPartnerUiService.Instance.GetDashboardAsync();
        if (!ApplicationResultPresenter.Present(result) || result.Value is null) return;
        Render(result.Value);
    }

    private void Render(CapitalDashboardDto d)
    {
        _kpiRow.Children.Clear();
        _chartsHost.Children.Clear();

        var cards = new (string title, string value, string icon, string brush)[]
        {
            ("إجمالي رأس المال", $"{d.TotalCapitalBase:N0} {d.BaseCurrency}", "\uE8C1", "PrimaryBrush"),
            ("شركاء نشطون", d.ActivePartnersCount.ToString(), "\uE716", "SuccessBrush"),
            ("مشاركات نشطة", d.ActiveParticipationsCount.ToString(), "\uE8AB", "InfoBrush"),
            ("توزيع أرباح الشهر", $"{d.MonthlyDistributedProfit:N0}", "\uE9D2", "AccentSalesBrush"),
            ("تسويات معلقة", $"{d.PendingSettlementsBase:N0}", "\uE823", "WarningBrush"),
            ("أكبر مستثمر", d.LargestInvestorBase > 0 ? d.LargestInvestorName : "—", "\uE8F1", "AccentPayableBrush")
        };

        ErpUiFactory.SetSummaryCards(_kpiRow, cards.Select(c => (c.title, c.value, c.icon, Br(c.brush))).ToArray());

        if (d.TopInvestors.Count > 0)
        {
            _chartsHost.Children.Add(ErpUiFactory.SectionTitle("أكبر المستثمرين"));
            var grid = new DataGrid { AutoGenerateColumns = false, IsReadOnly = true, Margin = new Thickness(0, 8, 0, 16) };
            ErpUiFactory.AddGridColumn(grid, "الشريك", nameof(CapitalTopInvestorDto.PartnerName), "*");
            ErpUiFactory.AddGridColumn(grid, "رأس المال", nameof(CapitalTopInvestorDto.CapitalBase), 140, "N2");
            grid.ItemsSource = d.TopInvestors;
            _chartsHost.Children.Add(ErpUiFactory.Card(grid));
        }

        var actions = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 16, 0, 0) };
        var txBtn = new Button { Content = "حركات رأس المال", Style = S("SecondaryButtonStyle"), Margin = new Thickness(0, 0, 8, 0) };
        txBtn.Click += (_, _) => MockInteractionService.Navigate(AppModule.CapitalPartners, "Transactions");
        var listBtn = new Button { Content = "سجل الشركاء", Style = S("SecondaryButtonStyle"), Margin = new Thickness(0, 0, 8, 0) };
        listBtn.Click += (_, _) => MockInteractionService.Navigate(AppModule.CapitalPartners, "List");
        var newBtn = new Button { Content = "شريك جديد", Style = S("PrimaryButtonStyle") };
        newBtn.Click += (_, _) => CapitalPartnerPopupService.ShowCreate();
        actions.Children.Add(txBtn);
        actions.Children.Add(listBtn);
        actions.Children.Add(newBtn);
        _chartsHost.Children.Add(actions);
    }

    private static SolidColorBrush Br(string key) =>
        (SolidColorBrush)WpfApplication.Current.Resources[key]!;

    private static Style S(string key) =>
        (Style)WpfApplication.Current.Resources[key]!;
}