using ERPSystem.Application.Abstractions.Repositories;
using ERPSystem.Application.DTOs.Capital;
using ERPSystem.Core;
using ERPSystem.Domain.Enums;
using ERPSystem.Helpers;
using ERPSystem.Services;
using ERPSystem.Services.Capital;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using WpfApplication = System.Windows.Application;
using ERPSystem.Diagnostics.Performance;

namespace ERPSystem.Controls.Capital;

public sealed class CapitalPartnerListPageControl : UserControl
{
    private readonly TextBox _search = new() { Style = S("EnterpriseSearchInputStyle"), Tag = "بحث بالاسم أو الكود..." };
    private readonly ComboBox _statusFilter = ErpUiFactory.FilterCombo(["الكل", "نشط", "غير نشط", "مؤرشف"]);
    private readonly ComboBox _scopeFilter = ErpUiFactory.FilterCombo(["الكل", "شركة", "مشروع", "حاوية"]);
    private readonly Button _btnPrimary = new() { Style = S("PrimaryButtonStyle"), Height = 32, Padding = new Thickness(12, 0, 12, 0) };
    private readonly WrapPanel _cardsHost = new() { Margin = new Thickness(16) };
    private readonly TextBlock _recordCount = new() { FontSize = 11, Foreground = Br("TextMutedBrush") };
    private readonly Border _emptyState = new() { Visibility = Visibility.Collapsed, Background = Brushes.White };
    private readonly DispatcherTimer _searchTimer = new() { Interval = TimeSpan.FromMilliseconds(350) };
    private bool _isLoading;
    private IReadOnlyList<CapitalPartnerListDto> _allPartners = [];

    public CapitalPartnerListPageControl()
    {
        Background = Br("AppBgBrush");
        Content = BuildLayout();
        Loaded += OnLoaded;
        Unloaded += (_, _) => CapitalListRefreshHub.RefreshRequested -= OnRefreshRequested;
        CapitalListRefreshHub.RefreshRequested += OnRefreshRequested;
        _searchTimer.Tick += async (_, _) => { _searchTimer.Stop(); await LoadAsync(); };
        _search.TextChanged += (_, _) =>
        {
            if (_allPartners.Count > 0)
                RenderCards(_allPartners);
            else
            {
                _searchTimer.Stop();
                _searchTimer.Start();
            }
        };
        foreach (var c in new[] { _statusFilter, _scopeFilter })
            c.SelectionChanged += (_, _) => _ = LoadAsync();
        _btnPrimary.Click += (_, _) => CapitalPartnerPopupService.ShowCreate();
    }

    private UIElement BuildLayout()
    {
        var root = new Grid();
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        var exportBtn = new Button
        {
            Content = "تصدير",
            Style = S("GhostButtonStyle"),
            Height = 32,
            Margin = new Thickness(6, 0, 0, 0)
        };
        exportBtn.Click += (_, _) => ERPSystem.Services.Documents.ListExportService.ExportRecords(
            _allPartners, "قائمة الشركاء",
            ("الكود", p => p.Code),
            ("الاسم", p => p.FullName),
            ("الحالة", p => p.StatusDisplay),
            ("العملة", p => p.DefaultCurrency),
            ("رأس المال الحالي", p => p.CurrentCapitalBase),
            ("إجمالي الاستثمارات", p => p.TotalInvestmentsBase),
            ("إجمالي السحوبات", p => p.TotalWithdrawalsBase),
            ("عدد المشاركات", p => p.ParticipationsCount),
            ("الهاتف", p => p.Phone));

        var toolbar = new Border
        {
            Background = Br("SurfaceBrush"),
            BorderBrush = Br("BorderBrush"),
            BorderThickness = new Thickness(0, 0, 0, 1),
            Padding = new Thickness(16, 12, 16, 12),
            Child = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Children = { BuildPrimaryButton(), exportBtn }
            }
        };
        Grid.SetRow(toolbar, 0);

        var filterRow = new StackPanel { Orientation = Orientation.Horizontal };
        filterRow.Children.Add(new Border
        {
            Width = 220,
            Background = Br("SurfaceBrush"),
            BorderBrush = Br("BorderBrush"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(6),
            Padding = new Thickness(8, 4, 8, 4),
            Child = _search
        });
        _statusFilter.Margin = new Thickness(8, 0, 0, 0);
        _scopeFilter.Margin = new Thickness(8, 0, 0, 0);
        filterRow.Children.Add(_statusFilter);
        filterRow.Children.Add(_scopeFilter);

        var filters = new Border
        {
            Background = Br("SurfaceBrush"),
            BorderBrush = Br("BorderBrush"),
            BorderThickness = new Thickness(0, 0, 0, 1),
            Padding = new Thickness(16, 10, 16, 10),
            Child = filterRow
        };
        Grid.SetRow(filters, 1);

        var body = new Border { Background = Brushes.White };
        var bodyGrid = new Grid();
        bodyGrid.Children.Add(new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            Content = _cardsHost
        });

        var emptyAddBtn = new Button
        {
            Content = "إضافة شريك",
            Style = S("PrimaryButtonStyle"),
            Height = 32,
            Margin = new Thickness(0, 10, 0, 0)
        };
        emptyAddBtn.Click += (_, _) => CapitalPartnerPopupService.ShowCreate();

        _emptyState.VerticalAlignment = VerticalAlignment.Center;
        _emptyState.HorizontalAlignment = HorizontalAlignment.Center;
        _emptyState.Child = new StackPanel
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            Children =
            {
                new TextBlock
                {
                    Text = "\uE8A5",
                    FontFamily = new FontFamily("Segoe MDL2 Assets"),
                    FontSize = 28,
                    Foreground = Br("TextMutedBrush"),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 0, 0, 6)
                },
                new TextBlock
                {
                    Text = "لا يوجد شركاء مسجّلون",
                    FontSize = 12,
                    Foreground = Br("TextSecondaryBrush"),
                    HorizontalAlignment = HorizontalAlignment.Center
                },
                emptyAddBtn
            }
        };
        bodyGrid.Children.Add(_emptyState);
        body.Child = bodyGrid;
        Grid.SetRow(body, 2);

        var footer = new Border
        {
            Background = Br("SurfaceAltBrush"),
            BorderBrush = Br("BorderBrush"),
            BorderThickness = new Thickness(0, 1, 0, 0),
            Padding = new Thickness(16, 8, 16, 8),
            Child = _recordCount
        };
        Grid.SetRow(footer, 3);

        root.Children.Add(toolbar);
        root.Children.Add(filters);
        root.Children.Add(body);
        root.Children.Add(footer);
        return root;
    }

    private UIElement BuildPrimaryButton()
    {
        var sp = new StackPanel { Orientation = Orientation.Horizontal };
        sp.Children.Add(new TextBlock
        {
            Text = "\uE710",
            FontFamily = new FontFamily("Segoe MDL2 Assets"),
            FontSize = 12,
            Margin = new Thickness(0, 0, 5, 0),
            VerticalAlignment = VerticalAlignment.Center
        });
        sp.Children.Add(new TextBlock { Text = "شريك جديد", FontSize = 12, VerticalAlignment = VerticalAlignment.Center });
        _btnPrimary.Content = sp;
        return _btnPrimary;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        var canCreate = await CapitalPartnerUiService.Instance.CanCreateAsync();
        _btnPrimary.IsEnabled = canCreate;
        await LoadAsync();
    }

    private void OnRefreshRequested(object? sender, EventArgs e) => _ = LoadAsync();

    private async Task LoadAsync()
    {
        using var perfScope = ScreenLoadProfiler.Begin("Capital.Partners");
        if (_isLoading || !AppServices.IsInitialized) return;
        _isLoading = true;
        try
        {
            var filter = BuildFilter();
            var result = await ScreenLoadProfiler.MeasureLoadAsync(perfScope, () => CapitalPartnerUiService.Instance.GetListAsync(filter, 1, 500));
        perfScope?.IncrementServiceCalls();
            if (!ApplicationResultPresenter.Present(result)) return;
            _allPartners = result.Value?.Items ?? [];
            RenderCards(_allPartners);
        }
        finally { _isLoading = false; }
    }

    private void RenderCards(IReadOnlyList<CapitalPartnerListDto> partners)
    {
        _cardsHost.Children.Clear();
        var term = _search.Text.Trim();
        var filtered = partners.AsEnumerable();

        if (!string.IsNullOrEmpty(term))
        {
            filtered = filtered.Where(p =>
                p.FullName.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                p.Code.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                (p.Phone?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false));
        }

        var list = filtered.ToList();
        foreach (var partner in list)
            _cardsHost.Children.Add(new CapitalPartnerCardControl(partner));

        _emptyState.Visibility = list.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        _recordCount.Text = $"عرض {list.Count} من {partners.Count} شريك";
    }

    private CapitalPartnerListFilter BuildFilter()
    {
        PartnerStatus? status = _statusFilter.SelectedIndex switch
        {
            1 => PartnerStatus.Active,
            2 => PartnerStatus.Inactive,
            3 => PartnerStatus.Archived,
            _ => null
        };

        PartnershipScope? scope = _scopeFilter.SelectedIndex switch
        {
            1 => PartnershipScope.Company,
            2 => PartnershipScope.Project,
            3 => PartnershipScope.Container,
            _ => null
        };

        return new CapitalPartnerListFilter
        {
            Status = status,
            Scope = scope,
            IncludeArchived = status == PartnerStatus.Archived
        };
    }

    private static Brush Br(string key) => (Brush)WpfApplication.Current.Resources[key]!;
    private static Style S(string key) => (Style)WpfApplication.Current.Resources[key]!;
}
