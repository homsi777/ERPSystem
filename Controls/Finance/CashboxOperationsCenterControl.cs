using ERPSystem.Application.DTOs.Finance;
using ERPSystem.Core;
using ERPSystem.Helpers;
using ERPSystem.Services;
using ERPSystem.Services.Finance;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ERPSystem.Controls.Finance;

public sealed class CashboxOperationsCenterControl : UserControl
{
    private CashboxOperationsCenterDto? _data;
    private TabControl? _tabs;
    private Guid? _popupId;
    private string? _popupTab;

    public CashboxOperationsCenterControl()
    {
        Content = Loading();
        Loaded += OnLoaded;
    }

    public void InitializeForPopup(Guid cashboxId, string? initialTab = null)
    {
        _popupId = cashboxId;
        _popupTab = initialTab;
        if (IsLoaded)
            _ = LoadAsync(cashboxId, initialTab);
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        Loaded -= OnLoaded;
        if (!_popupId.HasValue) return;
        var id = _popupId.Value;
        var tab = _popupTab;
        _popupId = null;
        _popupTab = null;
        await LoadAsync(id, tab);
    }

    private async Task LoadAsync(Guid cashboxId, string? initialTab)
    {
        if (!AppServices.IsInitialized)
        {
            Content = Empty("غير متصل", "تعذر الاتصال بخدمات التطبيق.");
            return;
        }

        Content = Loading();
        var result = await FinanceUiService.Instance.GetCashboxOperationsCenterAsync(cashboxId);
        if (!ApplicationResultPresenter.Present(result) || result.Value is null)
        {
            Content = Empty("تعذر التحميل", "لم يتم العثور على بيانات الصندوق.");
            return;
        }

        _data = result.Value;
        Render(initialTab);
    }

    private void Render(string? initialTab)
    {
        if (_data is null) return;
        var c = _data.Cashbox;

        var root = new StackPanel { Margin = new Thickness(20) };
        root.Children.Add(BuildHeader(c));
        root.Children.Add(BuildKpis(c));
        root.Children.Add(BuildQuickActions(c));
        _tabs = BuildTabs();
        root.Children.Add(_tabs);

        Content = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto, Content = root };

        if (!string.IsNullOrWhiteSpace(initialTab))
            SelectTab(initialTab);
    }

    private UIElement BuildHeader(CashboxDetailsDto c)
    {
        var sp = new StackPanel { Margin = new Thickness(0, 0, 0, 16) };
        sp.Children.Add(new TextBlock
        {
            Text = c.Name,
            FontSize = 22,
            FontWeight = FontWeights.SemiBold,
            Foreground = Br("TextPrimaryBrush")
        });
        sp.Children.Add(new TextBlock
        {
            Text = $"{c.Code} • {(c.IsActive ? "نشط" : "معطل")} • {c.Currency}",
            FontSize = 13,
            Foreground = Br("TextSecondaryBrush"),
            Margin = new Thickness(0, 4, 0, 0)
        });
        return sp;
    }

    private UIElement BuildKpis(CashboxDetailsDto c)
    {
        var panel = new WrapPanel { Margin = new Thickness(0, 0, 0, 16) };
        panel.Children.Add(KpiCard("الرصيد", $"{c.Balance:N2} {c.Currency}", "\uE8C1", "PrimaryBrush"));
        panel.Children.Add(KpiCard("قبض اليوم", $"{c.TodayReceipts:N2}", "\uE7BF", "AccentReceivableBrush"));
        panel.Children.Add(KpiCard("صرف اليوم", $"{c.TodayPayments:N2}", "\uE719", "AccentPayableBrush"));
        return panel;
    }

    private Border KpiCard(string label, string value, string icon, string accentKey)
    {
        var sp = new StackPanel();
        sp.Children.Add(new TextBlock
        {
            Text = icon,
            FontFamily = new FontFamily("Segoe MDL2 Assets"),
            FontSize = 18,
            Foreground = Br(accentKey)
        });
        sp.Children.Add(new TextBlock
        {
            Text = value,
            FontSize = 18,
            FontWeight = FontWeights.SemiBold,
            Margin = new Thickness(0, 6, 0, 0)
        });
        sp.Children.Add(new TextBlock
        {
            Text = label,
            FontSize = 11,
            Foreground = Br("TextMutedBrush"),
            Margin = new Thickness(0, 2, 0, 0)
        });

        return new Border
        {
            Background = Br("SurfaceBrush"),
            BorderBrush = Br("BorderBrush"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(10),
            Padding = new Thickness(16, 12, 16, 12),
            Margin = new Thickness(0, 0, 12, 12),
            MinWidth = 160,
            Child = sp
        };
    }

    private UIElement BuildQuickActions(CashboxDetailsDto c)
    {
        var panel = new WrapPanel { Margin = new Thickness(0, 0, 0, 12) };
        panel.Children.Add(QuickBtn("تعديل", "\uE70F", () => CashboxPopupService.ShowEdit(c.Id)));
        panel.Children.Add(QuickBtn("تحويل", "\uE8AB", () => CashboxPopupService.ShowTransfer(c.Id)));
        panel.Children.Add(QuickBtn("سند قبض", "\uE7BF", () => MockInteractionService.Navigate(AppModule.Accounting, "Receipts")));
        panel.Children.Add(QuickBtn("سند دفع", "\uE719", () => MockInteractionService.Navigate(AppModule.Accounting, "Payments")));
        return panel;
    }

    private Button QuickBtn(string label, string icon, Action onClick)
    {
        var btn = new Button
        {
            Content = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Children =
                {
                    new TextBlock { Text = icon, FontFamily = new FontFamily("Segoe MDL2 Assets"), Margin = new Thickness(0, 0, 8, 0) },
                    new TextBlock { Text = label }
                }
            },
            Style = (Style)System.Windows.Application.Current.Resources["SecondaryButtonStyle"]!,
            Height = 36,
            MinWidth = 110,
            Margin = new Thickness(0, 0, 8, 8)
        };
        btn.Click += (_, _) => onClick();
        return btn;
    }

    private TabControl BuildTabs()
    {
        var tc = new TabControl { Margin = new Thickness(0, 8, 0, 0) };
        tc.Items.Add(MovementsTab());
        tc.Items.Add(TransfersTab());
        return tc;
    }

    private TabItem MovementsTab()
    {
        var grid = ErpUiFactory.BuildGrid(autoColumns: false);
        ErpUiFactory.AddGridColumn(grid, "التاريخ", nameof(CashboxMovementDto.MovementDate), 110, "yyyy/MM/dd");
        ErpUiFactory.AddGridColumn(grid, "النوع", nameof(CashboxMovementDto.ReferenceType), 100);
        ErpUiFactory.AddGridColumn(grid, "الرقم", nameof(CashboxMovementDto.ReferenceNumber), 100);
        ErpUiFactory.AddGridColumn(grid, "الوصف", nameof(CashboxMovementDto.Description), "*");
        ErpUiFactory.AddGridColumn(grid, "الاتجاه", nameof(CashboxMovementDto.DirectionDisplay), 70);
        ErpUiFactory.AddGridColumn(grid, "المبلغ", nameof(CashboxMovementDto.Amount), 100, "N2");

        grid.ItemsSource = _data?.RecentMovements ?? [];
        return new TabItem { Header = "الحركات", Tag = "Movements", Content = grid };
    }

    private TabItem TransfersTab()
    {
        var grid = ErpUiFactory.BuildGrid(autoColumns: false);
        ErpUiFactory.AddGridColumn(grid, "الرقم", nameof(CashboxTransferListDto.TransferNumber), 100);
        ErpUiFactory.AddGridColumn(grid, "من", nameof(CashboxTransferListDto.FromCashboxName), "*");
        ErpUiFactory.AddGridColumn(grid, "إلى", nameof(CashboxTransferListDto.ToCashboxName), "*");
        ErpUiFactory.AddGridColumn(grid, "التاريخ", nameof(CashboxTransferListDto.TransferDate), 110, "yyyy/MM/dd");
        ErpUiFactory.AddGridColumn(grid, "المبلغ", nameof(CashboxTransferListDto.Amount), 100, "N2");
        ErpUiFactory.AddGridColumn(grid, "الحالة", nameof(CashboxTransferListDto.StatusDisplay), 80);

        grid.ItemsSource = _data?.RecentTransfers ?? [];
        return new TabItem { Header = "التحويلات", Tag = "Transfers", Content = grid };
    }

    private void SelectTab(string key)
    {
        if (_tabs is null) return;
        foreach (TabItem item in _tabs.Items)
        {
            if (item.Tag is string k && k.Equals(key, StringComparison.OrdinalIgnoreCase))
            {
                _tabs.SelectedItem = item;
                return;
            }
        }
    }

    private static UIElement Loading() => new TextBlock
    {
        Text = "جاري تحميل مركز عمل الصندوق...",
        Margin = new Thickness(24),
        FontSize = 15,
        Foreground = Br("TextSecondaryBrush")
    };

    private static UIElement Empty(string title, string msg)
    {
        var sp = new StackPanel { Margin = new Thickness(24) };
        sp.Children.Add(new TextBlock { Text = title, FontSize = 18, FontWeight = FontWeights.SemiBold });
        sp.Children.Add(new TextBlock { Text = msg, Margin = new Thickness(0, 8, 0, 0), Foreground = Br("TextSecondaryBrush") });
        return sp;
    }

    private static Brush Br(string key) => (Brush)System.Windows.Application.Current.Resources[key]!;
}
