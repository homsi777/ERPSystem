using ERPSystem.Application.DTOs.Expenses;
using ERPSystem.Helpers;
using ERPSystem.Services;
using ERPSystem.Services.Expenses;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using WpfApplication = System.Windows.Application;

namespace ERPSystem.Controls.Expenses;

/// <summary>قائمة تعريفات المصاريف — بطاقات تفاعلية.</summary>
public sealed class ExpenseListPageControl : UserControl
{
    private readonly TextBox _search = new() { Style = S("EnterpriseSearchInputStyle"), Tag = "بحث بالاسم أو الكود..." };
    private readonly Button _btnPrimary = new() { Style = S("PrimaryButtonStyle"), Height = 32, Padding = new Thickness(12, 0, 12, 0) };
    private readonly WrapPanel _cardsHost = new() { Margin = new Thickness(16) };
    private readonly TextBlock _recordCount = new() { FontSize = 11, Foreground = Br("TextMutedBrush") };
    private readonly Border _emptyState = new() { Visibility = Visibility.Collapsed, Background = Brushes.White };
    private readonly DispatcherTimer _searchTimer = new() { Interval = TimeSpan.FromMilliseconds(350) };
    private bool _isLoading;
    private IReadOnlyList<ExpenseListDto> _allItems = [];

    public ExpenseListPageControl()
    {
        Background = Br("AppBgBrush");
        Content = BuildLayout();
        Loaded += OnLoaded;
        Unloaded += (_, _) => ExpenseListRefreshHub.RefreshRequested -= OnRefreshRequested;
        ExpenseListRefreshHub.RefreshRequested += OnRefreshRequested;
        _searchTimer.Tick += async (_, _) => { _searchTimer.Stop(); await LoadAsync(); };
        _search.TextChanged += (_, _) =>
        {
            if (_allItems.Count > 0)
                RenderCards(_allItems);
            else
            {
                _searchTimer.Stop();
                _searchTimer.Start();
            }
        };
        _btnPrimary.Click += (_, _) => ExpensePopupService.ShowCreate();
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
        exportBtn.Click += (_, _) => MockInteractionService.ShowDocumentPreview("تعريفات المصاريف", "Excel");

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

        var filters = new Border
        {
            Background = Br("SurfaceBrush"),
            BorderBrush = Br("BorderBrush"),
            BorderThickness = new Thickness(0, 0, 0, 1),
            Padding = new Thickness(16, 10, 16, 10),
            Child = new Border
            {
                Width = 240,
                Background = Br("SurfaceBrush"),
                BorderBrush = Br("BorderBrush"),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(8, 4, 8, 4),
                Child = _search
            }
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
            Content = "تعريف جديد",
            Style = S("PrimaryButtonStyle"),
            Height = 32,
            Margin = new Thickness(0, 10, 0, 0)
        };
        emptyAddBtn.Click += (_, _) => ExpensePopupService.ShowCreate();

        _emptyState.VerticalAlignment = VerticalAlignment.Center;
        _emptyState.HorizontalAlignment = HorizontalAlignment.Center;
        _emptyState.Child = new StackPanel
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            Children =
            {
                new TextBlock
                {
                    Text = "\uE9D9",
                    FontFamily = new FontFamily("Segoe MDL2 Assets"),
                    FontSize = 28,
                    Foreground = Br("TextMutedBrush"),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 0, 0, 6)
                },
                new TextBlock
                {
                    Text = "لا توجد تعريفات مصاريف",
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
        sp.Children.Add(new TextBlock { Text = "تعريف جديد", FontSize = 12, VerticalAlignment = VerticalAlignment.Center });
        _btnPrimary.Content = sp;
        return _btnPrimary;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        _btnPrimary.IsEnabled = await ExpenseUiService.Instance.CanCreateAsync();
        await LoadAsync();
    }

    private void OnRefreshRequested(object? sender, EventArgs e) => _ = LoadAsync();

    private async Task LoadAsync()
    {
        if (_isLoading || !AppServices.IsInitialized) return;
        _isLoading = true;
        try
        {
            var term = _search.Text.Trim();
            var result = await ExpenseUiService.Instance.GetDefinitionsAsync(
                string.IsNullOrWhiteSpace(term) ? null : term);
            if (!ApplicationResultPresenter.Present(result)) return;
            _allItems = result.Value?.Items ?? [];
            RenderCards(_allItems);
        }
        finally { _isLoading = false; }
    }

    private void RenderCards(IReadOnlyList<ExpenseListDto> items)
    {
        _cardsHost.Children.Clear();
        var term = _search.Text.Trim();
        var filtered = items.AsEnumerable();
        if (!string.IsNullOrEmpty(term))
        {
            filtered = filtered.Where(e =>
                e.Name.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                e.Code.Contains(term, StringComparison.OrdinalIgnoreCase));
        }

        var list = filtered.ToList();
        foreach (var item in list)
            _cardsHost.Children.Add(new ExpenseCardControl(item));

        _emptyState.Visibility = list.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        _recordCount.Text = $"عرض {list.Count} من {items.Count} تعريف";
    }

    private static Brush Br(string key) => (Brush)WpfApplication.Current.Resources[key]!;
    private static Style S(string key) => (Style)WpfApplication.Current.Resources[key]!;
}
