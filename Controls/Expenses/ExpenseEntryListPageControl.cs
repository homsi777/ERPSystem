using ERPSystem.Application.Abstractions.Repositories;
using ERPSystem.Application.DTOs.Expenses;
using ERPSystem.Helpers;
using ERPSystem.Services;
using ERPSystem.Services.Expenses;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using WpfApplication = System.Windows.Application;
using ERPSystem.Diagnostics.Performance;

namespace ERPSystem.Controls.Expenses;

public sealed class ExpenseEntryListPageControl : UserControl
{
    private readonly TextBox _search = new() { Style = S("EnterpriseSearchInputStyle"), Tag = "بحث..." };
    private readonly ComboBox _expenseFilter = new() { MinWidth = 200 };
    private readonly DatePicker _fromDate = ErpUiFactory.FormDate(DateTime.Today.AddMonths(-1));
    private readonly DatePicker _toDate = ErpUiFactory.FormDate(DateTime.Today);
    private readonly Button _btnPrimary = new() { Style = S("PrimaryButtonStyle"), Height = 32, Padding = new Thickness(12, 0, 12, 0) };
    private readonly WrapPanel _cardsHost = new() { Margin = new Thickness(16) };
    private readonly TextBlock _recordCount = new() { FontSize = 11, Foreground = Br("TextMutedBrush") };
    private readonly Border _emptyState = new() { Visibility = Visibility.Collapsed, Background = Brushes.White };
    private readonly DispatcherTimer _searchTimer = new() { Interval = TimeSpan.FromMilliseconds(350) };
    private bool _isLoading;

    public ExpenseEntryListPageControl()
    {
        Background = Br("AppBgBrush");
        Content = BuildLayout();
        Loaded += OnLoaded;
        Unloaded += (_, _) => ExpenseListRefreshHub.RefreshRequested -= OnRefreshRequested;
        ExpenseListRefreshHub.RefreshRequested += OnRefreshRequested;
        _searchTimer.Tick += async (_, _) => { _searchTimer.Stop(); await LoadAsync(); };
        _search.TextChanged += (_, _) => { _searchTimer.Stop(); _searchTimer.Start(); };
        _expenseFilter.SelectionChanged += (_, _) => _ = LoadAsync();
        _fromDate.SelectedDateChanged += (_, _) => _ = LoadAsync();
        _toDate.SelectedDateChanged += (_, _) => _ = LoadAsync();
        _btnPrimary.Click += (_, _) => ExpensePopupService.ShowEntry();
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
        filterRow.Children.Add(Labeled("المصروف", _expenseFilter));
        filterRow.Children.Add(new Border
        {
            Width = 200,
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
        emptyBtn.Click += (_, _) => ExpensePopupService.ShowEntry();

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
                    Text = "لا توجد قيود مسجّلة",
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
        var defs = await ExpenseUiService.Instance.GetDefinitionsAsync();
        if (ApplicationResultPresenter.Present(defs))
        {
            var items = new List<ExpenseListDto> { new() { Id = Guid.Empty, Name = "— الكل —" } };
            items.AddRange(defs.Value?.Items ?? []);
            _expenseFilter.ItemsSource = items;
            _expenseFilter.DisplayMemberPath = nameof(ExpenseListDto.Name);
            _expenseFilter.SelectedIndex = 0;
        }
        await LoadAsync();
    }

    private void OnRefreshRequested(object? sender, EventArgs e) => _ = LoadAsync();

    private async Task LoadAsync()
    {
        using var perfScope = ScreenLoadProfiler.Begin("Expenses.Entries");
        if (_isLoading || !AppServices.IsInitialized) return;
        _isLoading = true;
        try
        {
            Guid? expenseId = null;
            if (_expenseFilter.SelectedItem is ExpenseListDto sel && sel.Id != Guid.Empty)
                expenseId = sel.Id;

            var filter = new ExpenseEntryListFilter
            {
                Search = string.IsNullOrWhiteSpace(_search.Text) ? null : _search.Text.Trim(),
                ExpenseId = expenseId,
                FromDate = _fromDate.SelectedDate,
                ToDate = _toDate.SelectedDate
            };

            var result = await ScreenLoadProfiler.MeasureLoadAsync(perfScope, () => ExpenseUiService.Instance.GetEntriesAsync(filter, 1, 500));
        perfScope?.IncrementServiceCalls();
            if (!ApplicationResultPresenter.Present(result)) return;

            var list = result.Value?.Items ?? [];
            _cardsHost.Children.Clear();
            foreach (var entry in list)
                _cardsHost.Children.Add(new ExpenseEntryCardControl(entry));

            _emptyState.Visibility = list.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            _recordCount.Text = $"عرض {list.Count} قيد";
        }
        finally { _isLoading = false; }
    }

    private static Brush Br(string key) => (Brush)WpfApplication.Current.Resources[key]!;
    private static Style S(string key) => (Style)WpfApplication.Current.Resources[key]!;
}
