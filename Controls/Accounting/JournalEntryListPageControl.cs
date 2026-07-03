using ERPSystem.Application.Abstractions.Repositories;
using ERPSystem.Application.Common;
using ERPSystem.Application.DTOs.Accounting;
using ERPSystem.Application.Queries.Accounting;
using ERPSystem.Domain.Enums;
using ERPSystem.Helpers;
using ERPSystem.Services;
using ERPSystem.Services.Accounting;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using WpfApplication = System.Windows.Application;

namespace ERPSystem.Controls.Accounting;

public sealed class JournalEntryListPageControl : UserControl
{
    private sealed record StatusFilterOption(JournalEntryStatus? Status, string Label);

    private readonly TextBox _search = new() { Style = S("EnterpriseSearchInputStyle"), Tag = "بحث برقم القيد أو البيان..." };
    private readonly ComboBox _statusFilter = new() { MinWidth = 140, Style = S("EnterpriseComboBoxStyle") };
    private readonly DatePicker _fromDate = ErpUiFactory.FormDate(DateTime.Today.AddMonths(-1));
    private readonly DatePicker _toDate = ErpUiFactory.FormDate(DateTime.Today);
    private readonly Button _btnPrimary = new() { Style = S("PrimaryButtonStyle"), Height = 32, Padding = new Thickness(12, 0, 12, 0) };
    private readonly WrapPanel _cardsHost = new() { Margin = new Thickness(16) };
    private readonly TextBlock _recordCount = new() { FontSize = 11, Foreground = Br("TextMutedBrush") };
    private readonly Border _emptyState = new() { Visibility = Visibility.Collapsed, Background = Brushes.White };
    private readonly DispatcherTimer _searchTimer = new() { Interval = TimeSpan.FromMilliseconds(350) };
    private bool _isLoading;

    public JournalEntryListPageControl()
    {
        Background = Br("AppBgBrush");
        Content = BuildLayout();
        Loaded += OnLoaded;
        Unloaded += (_, _) => AccountingListRefreshHub.RefreshRequested -= OnRefreshRequested;
        AccountingListRefreshHub.RefreshRequested += OnRefreshRequested;
        _searchTimer.Tick += async (_, _) => { _searchTimer.Stop(); await LoadAsync(); };
        _search.TextChanged += (_, _) => { _searchTimer.Stop(); _searchTimer.Start(); };
        _statusFilter.SelectionChanged += (_, _) => _ = LoadAsync();
        _fromDate.SelectedDateChanged += (_, _) => _ = LoadAsync();
        _toDate.SelectedDateChanged += (_, _) => _ = LoadAsync();
        _btnPrimary.Click += (_, _) => AccountingPopupService.ShowCreateJournal();
    }

    private UIElement BuildLayout()
    {
        var root = new Grid();
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        var toolbar = new Border
        {
            Background = Br("SurfaceBrush"),
            BorderBrush = Br("BorderBrush"),
            BorderThickness = new Thickness(0, 0, 0, 1),
            Padding = new Thickness(16, 12, 16, 12),
            Child = BuildPrimaryButton()
        };
        Grid.SetRow(toolbar, 0);

        var filterRow = new StackPanel { Orientation = Orientation.Horizontal };
        filterRow.Children.Add(Labeled("من", _fromDate));
        filterRow.Children.Add(Labeled("إلى", _toDate));
        filterRow.Children.Add(Labeled("الحالة", _statusFilter));
        filterRow.Children.Add(new Border
        {
            Width = 220,
            Margin = new Thickness(12, 0, 0, 0),
            Background = Br("SurfaceBrush"),
            BorderBrush = Br("BorderBrush"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(6),
            Padding = new Thickness(8, 4, 8, 4),
            Child = _search
        });

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

        var emptyBtn = new Button
        {
            Content = "قيد جديد",
            Style = S("PrimaryButtonStyle"),
            Height = 32,
            Margin = new Thickness(0, 10, 0, 0)
        };
        emptyBtn.Click += (_, _) => AccountingPopupService.ShowCreateJournal();

        _emptyState.VerticalAlignment = VerticalAlignment.Center;
        _emptyState.HorizontalAlignment = HorizontalAlignment.Center;
        _emptyState.Child = new StackPanel
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            Children =
            {
                new TextBlock
                {
                    Text = "\uE8C1",
                    FontFamily = new FontFamily("Segoe MDL2 Assets"),
                    FontSize = 28,
                    Foreground = Br("TextMutedBrush"),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 0, 0, 6)
                },
                new TextBlock
                {
                    Text = "لا توجد قيود يومية",
                    FontSize = 12,
                    Foreground = Br("TextSecondaryBrush"),
                    HorizontalAlignment = HorizontalAlignment.Center
                },
                emptyBtn
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
        sp.Children.Add(new TextBlock { Text = "قيد جديد", FontSize = 12, VerticalAlignment = VerticalAlignment.Center });
        _btnPrimary.Content = sp;
        return _btnPrimary;
    }

    private static UIElement Labeled(string label, UIElement control)
    {
        var sp = new StackPanel { Margin = new Thickness(0, 0, 12, 0) };
        sp.Children.Add(new TextBlock
        {
            Text = label,
            FontSize = 11,
            Foreground = Br("TextMutedBrush")
        });
        sp.Children.Add(control);
        return sp;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        var statusOptions = new List<StatusFilterOption> { new(null, "— كل الحالات —") };
        foreach (JournalEntryStatus status in Enum.GetValues(typeof(JournalEntryStatus)))
            statusOptions.Add(new(status, status.ToDisplay()));

        _statusFilter.ItemsSource = statusOptions;
        _statusFilter.DisplayMemberPath = nameof(StatusFilterOption.Label);
        _statusFilter.SelectedIndex = 0;

        _btnPrimary.IsEnabled = await AccountingUiService.Instance.CanCreateJournalAsync();
        await LoadAsync();
    }

    private void OnRefreshRequested(object? sender, EventArgs e) => _ = LoadAsync();

    private async Task LoadAsync()
    {
        if (_isLoading || !AppServices.IsInitialized) return;
        _isLoading = true;
        try
        {
            JournalEntryStatus? status = _statusFilter.SelectedItem is StatusFilterOption opt ? opt.Status : null;
            var filter = new JournalEntryListFilter
            {
                Search = string.IsNullOrWhiteSpace(_search.Text) ? null : _search.Text.Trim(),
                Status = status,
                FromDate = _fromDate.SelectedDate,
                ToDate = _toDate.SelectedDate
            };

            var result = await AccountingUiService.Instance.GetJournalEntriesAsync(filter, 1, 500);
            if (!ApplicationResultPresenter.Present(result)) return;

            var list = result.Value?.Items ?? [];
            _cardsHost.Children.Clear();
            foreach (var entry in list)
                _cardsHost.Children.Add(new JournalEntryCardControl(entry));

            _emptyState.Visibility = list.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            _recordCount.Text = $"عرض {list.Count} قيد";
        }
        finally { _isLoading = false; }
    }

    private static Brush Br(string key) => (Brush)WpfApplication.Current.Resources[key]!;
    private static Style S(string key) => (Style)WpfApplication.Current.Resources[key]!;
}
