using ERPSystem.Application.Common;
using ERPSystem.Application.DTOs.Accounting;
using ERPSystem.Domain.Enums;
using ERPSystem.Helpers;
using ERPSystem.Services;
using ERPSystem.Services.Accounting;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using WpfApplication = System.Windows.Application;

namespace ERPSystem.Controls.Accounting;

/// <summary>شجرة الحسابات — عرض Odoo (جدول مجمّع حسب النوع).</summary>
public sealed class ChartOfAccountsListPageControl : UserControl
{
    private const double ColCheck = 36;
    private const double ColCode = 96;
    private const double ColType = 128;
    private const double ColReconcile = 112;
    private const double ColCurrency = 88;
    private const double ColSetup = 72;

    private readonly TextBox _search = new()
    {
        Style = S("EnterpriseSearchInputStyle"),
        Tag = "بحث...",
        BorderThickness = new Thickness(0),
        Background = Brushes.Transparent
    };
    private readonly TextBlock _pagination = new() { FontSize = 12, Foreground = Br("TextSecondaryBrush"), VerticalAlignment = VerticalAlignment.Center };
    private readonly TextBlock _recordCount = new() { FontSize = 11, Foreground = Br("TextMutedBrush") };
    private readonly StackPanel _filterChips = new() { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 8, 0, 0) };
    private readonly StackPanel _treeHost = new() { HorizontalAlignment = HorizontalAlignment.Stretch };
    private readonly StackPanel _levelSidebar = new() { Margin = new Thickness(0, 8, 0, 0) };
    private readonly Border _emptyState = new() { Visibility = Visibility.Collapsed, Background = Brushes.White };
    private readonly DispatcherTimer _searchTimer = new() { Interval = TimeSpan.FromMilliseconds(350) };

    private readonly Dictionary<string, bool> _expandedGroups = new(StringComparer.Ordinal);
    private readonly Dictionary<int, Border> _levelButtons = new();

    private bool _isLoading;
    private bool _activeOnly = true;
    private int? _maxLevel;
    private IReadOnlyList<AccountListDto> _allItems = [];

    public ChartOfAccountsListPageControl()
    {
        HorizontalAlignment = HorizontalAlignment.Stretch;
        HorizontalContentAlignment = HorizontalAlignment.Stretch;
        Background = Br("AppBgBrush");
        Content = BuildLayout();
        Loaded += OnLoaded;
        Unloaded += (_, _) => AccountingListRefreshHub.RefreshRequested -= OnRefreshRequested;
        AccountingListRefreshHub.RefreshRequested += OnRefreshRequested;
        _searchTimer.Tick += async (_, _) => { _searchTimer.Stop(); await LoadAsync(); };
        _search.TextChanged += (_, _) =>
        {
            if (_allItems.Count > 0)
                RenderTree();
            else
            {
                _searchTimer.Stop();
                _searchTimer.Start();
            }
        };
    }

    private UIElement BuildLayout()
    {
        var root = new Grid { Background = Brushes.White };
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        var header = BuildOdooHeader();
        var search = BuildSearchPanel();
        var body = BuildMainBody();
        var footer = BuildFooter();

        Grid.SetRow(header, 0);
        Grid.SetRow(search, 1);
        Grid.SetRow(body, 2);
        Grid.SetRow(footer, 3);

        root.Children.Add(header);
        root.Children.Add(search);
        root.Children.Add(body);
        root.Children.Add(footer);
        return root;
    }

    private UIElement BuildOdooHeader()
    {
        var bar = new Border
        {
            Background = Br("PrimaryBrush"),
            Padding = new Thickness(16, 10, 16, 10)
        };

        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var titleRow = new StackPanel { Orientation = Orientation.Horizontal };
        titleRow.Children.Add(new TextBlock
        {
            Text = "شجرة الحسابات",
            FontSize = 18,
            FontWeight = FontWeights.SemiBold,
            Foreground = Brushes.White,
            VerticalAlignment = VerticalAlignment.Center
        });
        titleRow.Children.Add(new TextBlock
        {
            Text = "  ▾",
            FontSize = 12,
            Foreground = new SolidColorBrush(Color.FromArgb(200, 255, 255, 255)),
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(4, 0, 0, 0)
        });
        Grid.SetColumn(titleRow, 0);

        var actions = new StackPanel { Orientation = Orientation.Horizontal };
        actions.Children.Add(CreateHeaderIconButton("\uE896", "تصدير", (_, _) =>
            MockInteractionService.ShowDocumentPreview("شجرة الحسابات", "Excel")));
        actions.Children.Add(CreateHeaderIconButton("\uE72C", "تحديث", (_, _) => _ = LoadAsync()));
        actions.Children.Add(CreateHeaderPrimaryButton("حساب جديد", (_, _) => AccountingPopupService.ShowCreateAccount()));
        Grid.SetColumn(actions, 1);

        grid.Children.Add(titleRow);
        grid.Children.Add(actions);
        bar.Child = grid;
        return bar;
    }

    private UIElement BuildSearchPanel()
    {
        var panel = new Border
        {
            Background = Br("SurfaceBrush"),
            BorderBrush = Br("BorderBrush"),
            BorderThickness = new Thickness(0, 0, 0, 1),
            Padding = new Thickness(16, 12, 16, 12)
        };

        var inner = new StackPanel();

        var searchRow = new Grid();
        searchRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        searchRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var searchBox = new Border
        {
            Background = Brushes.White,
            BorderBrush = Br("BorderBrush"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(4),
            Padding = new Thickness(10, 6, 10, 6),
            Child = new Grid()
        };
        var searchGrid = (Grid)searchBox.Child;
        searchGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        searchGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        searchGrid.Children.Add(new TextBlock
        {
            Text = "\uE721",
            FontFamily = new FontFamily("Segoe MDL2 Assets"),
            FontSize = 14,
            Foreground = Br("TextMutedBrush"),
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 8, 0)
        });
        Grid.SetColumn(_search, 1);
        searchGrid.Children.Add(_search);
        Grid.SetColumn(searchBox, 0);

        var metaRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(12, 0, 0, 0) };
        metaRow.Children.Add(_pagination);
        Grid.SetColumn(metaRow, 1);

        searchRow.Children.Add(searchBox);
        searchRow.Children.Add(metaRow);
        inner.Children.Add(searchRow);
        inner.Children.Add(_filterChips);

        panel.Child = inner;
        return panel;
    }

    private UIElement BuildMainBody()
    {
        var grid = new Grid { MinWidth = 0 };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(44) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star), MinWidth = 0 });

        var tableArea = new Grid { MinWidth = 0, HorizontalAlignment = HorizontalAlignment.Stretch };
        tableArea.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        tableArea.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star), MinHeight = 0 });

        var header = CreateColumnHeaderRow();
        Grid.SetRow(header, 0);

        var scroll = new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            Content = _treeHost,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };

        var bodyGrid = new Grid();
        bodyGrid.Children.Add(scroll);

        _emptyState.VerticalAlignment = VerticalAlignment.Center;
        _emptyState.HorizontalAlignment = HorizontalAlignment.Center;
        _emptyState.Child = ErpUiFactory.EmptyState("لا توجد حسابات مطابقة", "حساب جديد");
        if (_emptyState.Child is StackPanel emptySp && emptySp.Children.Count > 2
            && emptySp.Children[2] is Button emptyBtn)
            emptyBtn.Click += (_, _) => AccountingPopupService.ShowCreateAccount();
        bodyGrid.Children.Add(_emptyState);
        Grid.SetRow(bodyGrid, 1);

        tableArea.Children.Add(header);
        tableArea.Children.Add(bodyGrid);
        Grid.SetColumn(tableArea, 1);

        BuildLevelSidebar();
        var sidebar = new Border
        {
            BorderBrush = Br("BorderLightBrush"),
            BorderThickness = new Thickness(0, 0, 1, 0),
            Background = Br("SurfaceAltBrush"),
            Child = _levelSidebar
        };
        Grid.SetColumn(sidebar, 0);

        grid.Children.Add(sidebar);
        grid.Children.Add(tableArea);
        return grid;
    }

    private UIElement BuildFooter()
    {
        return new Border
        {
            Background = Br("SurfaceAltBrush"),
            BorderBrush = Br("BorderBrush"),
            BorderThickness = new Thickness(0, 1, 0, 0),
            Padding = new Thickness(16, 8, 16, 8),
            Child = _recordCount
        };
    }

    private Grid CreateColumnHeaderRow()
    {
        var grid = CreateRowGrid();
        grid.Background = Br("SurfaceAltBrush");
        grid.MinHeight = 34;
        grid.Children.Add(CellHeader("", ColCheck, 0));
        grid.Children.Add(CellHeader("اسم الحساب", "*", 1));
        grid.Children.Add(CellHeader("النوع", ColType, 2));
        grid.Children.Add(CellHeader("السماح بالتسوية", ColReconcile, 3));
        grid.Children.Add(CellHeader("عملة الحساب", ColCurrency, 4));
        grid.Children.Add(CellHeader("الإعداد", ColSetup, 5));
        grid.Children.Add(CellHeader("الكود", ColCode, 6));
        return grid;
    }

    private void BuildLevelSidebar()
    {
        _levelSidebar.Children.Clear();
        _levelButtons.Clear();

        AddLevelButton(null, "الكل", true);
        foreach (var level in new[] { 1, 2, 3, 4, 5 })
            AddLevelButton(level, level.ToString(), false);
    }

    private void AddLevelButton(int? level, string label, bool selected)
    {
        var btn = new Border
        {
            Width = 32,
            Height = 32,
            Margin = new Thickness(6, 4, 6, 4),
            CornerRadius = new CornerRadius(4),
            Background = selected ? Br("PrimaryBrush") : Brushes.Transparent,
            Cursor = Cursors.Hand,
            Child = new TextBlock
            {
                Text = label,
                FontSize = level is null ? 9 : 13,
                FontWeight = FontWeights.SemiBold,
                Foreground = selected ? Brushes.White : Br("TextSecondaryBrush"),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            }
        };
        btn.MouseLeftButtonUp += (_, _) =>
        {
            _maxLevel = level;
            UpdateLevelSelection();
            RenderTree();
        };
        _levelSidebar.Children.Add(btn);
        if (level.HasValue)
            _levelButtons[level.Value] = btn;
    }

    private void UpdateLevelSelection()
    {
        foreach (var child in _levelSidebar.Children.OfType<Border>())
        {
            var tb = child.Child as TextBlock;
            if (tb is null) continue;
            var isAll = tb.Text == "الكل";
            var selected = isAll ? _maxLevel is null : int.TryParse(tb.Text, out var lv) && _maxLevel == lv;
            child.Background = selected ? Br("PrimaryBrush") : Brushes.Transparent;
            tb.Foreground = selected ? Brushes.White : Br("TextSecondaryBrush");
        }
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        foreach (var cat in AccountingDisplayExtensions.OdooCategoryOrder)
            _expandedGroups[cat] = true;

        RefreshFilterChips();
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
            var result = await AccountingUiService.Instance.GetAccountsAsync(
                string.IsNullOrWhiteSpace(term) ? null : term);
            if (!ApplicationResultPresenter.Present(result)) return;
            _allItems = result.Value ?? [];
            RenderTree();
        }
        finally { _isLoading = false; }
    }

    private void RenderTree()
    {
        _treeHost.Children.Clear();
        var items = FilterItems(_allItems).ToList();

        var grouped = items
            .GroupBy(a => a.ToOdooCategoryDisplay())
            .ToDictionary(g => g.Key, g => g.OrderBy(a => a.Code).ToList());

        var orderedCategories = AccountingDisplayExtensions.OdooCategoryOrder
            .Where(grouped.ContainsKey)
            .Concat(grouped.Keys.Except(AccountingDisplayExtensions.OdooCategoryOrder))
            .ToList();

        var visibleCount = 0;
        foreach (var category in orderedCategories)
        {
            var accounts = grouped[category];
            if (accounts.Count == 0) continue;

            var expanded = _expandedGroups.GetValueOrDefault(category, true);
            _treeHost.Children.Add(CreateGroupHeader(category, accounts.Count, expanded));

            if (!expanded) continue;

            foreach (var account in accounts)
            {
                _treeHost.Children.Add(CreateAccountRow(account));
                visibleCount++;
            }

            _treeHost.Children.Add(CreateAddLineRow(category));
        }

        _emptyState.Visibility = visibleCount == 0 ? Visibility.Visible : Visibility.Collapsed;
        _pagination.Text = visibleCount == 0 ? "0 / 0" : $"1-{visibleCount} / {visibleCount}";
        _recordCount.Text = $"عرض {visibleCount} حساب في {orderedCategories.Count} مجموعة";
    }

    private IEnumerable<AccountListDto> FilterItems(IReadOnlyList<AccountListDto> items)
    {
        var term = _search.Text.Trim();
        foreach (var item in items)
        {
            if (_activeOnly && !item.IsActive) continue;
            if (_maxLevel is int max && item.Level > max) continue;
            if (!string.IsNullOrEmpty(term)
                && !item.Code.Contains(term, StringComparison.OrdinalIgnoreCase)
                && !item.NameAr.Contains(term, StringComparison.OrdinalIgnoreCase)
                && !item.NameEn.Contains(term, StringComparison.OrdinalIgnoreCase))
                continue;
            yield return item;
        }
    }

    private UIElement CreateGroupHeader(string category, int count, bool expanded)
    {
        var row = new Border
        {
            Background = new SolidColorBrush(Color.FromRgb(245, 245, 245)),
            BorderBrush = Br("BorderLightBrush"),
            BorderThickness = new Thickness(0, 0, 0, 1),
            Padding = new Thickness(8, 8, 12, 8),
            Cursor = Cursors.Hand,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };

        var sp = new StackPanel { Orientation = Orientation.Horizontal };
        sp.Children.Add(new TextBlock
        {
            Text = expanded ? "\uE70D" : "\uE76C",
            FontFamily = new FontFamily("Segoe MDL2 Assets"),
            FontSize = 11,
            Foreground = Br("TextMutedBrush"),
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 8, 0)
        });
        sp.Children.Add(new TextBlock
        {
            Text = $"{category} ({count})",
            FontSize = 13,
            FontWeight = FontWeights.SemiBold,
            Foreground = Br("TextPrimaryBrush"),
            VerticalAlignment = VerticalAlignment.Center
        });

        row.Child = sp;
        row.MouseLeftButtonUp += (_, _) =>
        {
            _expandedGroups[category] = !expanded;
            RenderTree();
        };
        return row;
    }

    private UIElement CreateAccountRow(AccountListDto account)
    {
        var row = CreateRowGrid();
        row.MinHeight = 36;
        row.Background = Brushes.White;
        row.Tag = account;

        row.Children.Add(CellCheck(0));
        row.Children.Add(CellName(account, 1));
        row.Children.Add(CellText(account.ToOdooCategoryDisplay(), ColType, 2, Br("TextSecondaryBrush")));
        row.Children.Add(CellToggle(account.IsPostable, 3));
        row.Children.Add(CellText(account.IsPostable ? "USD" : "—", ColCurrency, 4, Br("TextMutedBrush")));
        row.Children.Add(CellSetup(account, 5));
        row.Children.Add(CellText(account.Code, ColCode, 6, Br("TextPrimaryBrush"), FontWeights.Normal));

        row.MouseEnter += (_, _) => row.Background = Br("PrimaryVeryLightBrush");
        row.MouseLeave += (_, _) => row.Background = Brushes.White;
        row.MouseLeftButtonUp += (_, e) =>
        {
            if (e.OriginalSource is DependencyObject src && IsSetupClick(src))
                return;
            AccountingPopupService.ShowAccountDetails(account);
        };
        row.PreviewMouseRightButtonDown += (_, e) =>
        {
            e.Handled = true;
            AccountingContextMenuService.ShowAccount(account, row);
        };

        return new Border
        {
            BorderBrush = Br("BorderLightBrush"),
            BorderThickness = new Thickness(0, 0, 0, 1),
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Child = row
        };
    }

    private static bool IsSetupClick(DependencyObject src)
    {
        while (src is not null)
        {
            if (src is FrameworkElement fe && fe.Tag as string == "setup")
                return true;
            src = VisualTreeHelper.GetParent(src);
        }
        return false;
    }

    private UIElement CreateAddLineRow(string category)
    {
        var link = new TextBlock
        {
            Text = "إضافة بند",
            FontSize = 12,
            Foreground = Br("PrimaryBrush"),
            Cursor = Cursors.Hand,
            Margin = new Thickness(28, 6, 12, 8)
        };
        link.MouseLeftButtonUp += (_, e) =>
        {
            e.Handled = true;
            AccountingNavigationContext.BeginCreate();
            AccountingPopupService.ShowCreateAccount();
        };
        return link;
    }

    private UIElement CellSetup(AccountListDto account, int col)
    {
        var link = new TextBlock
        {
            Text = "إعداد",
            FontSize = 12,
            Foreground = Br("PrimaryBrush"),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Cursor = Cursors.Hand,
            Tag = "setup"
        };
        link.MouseLeftButtonUp += (_, e) =>
        {
            e.Handled = true;
            AccountingPopupService.ShowEditAccount(account);
        };
        var host = new Border { Child = link };
        Grid.SetColumn(host, col);
        return host;
    }

    private UIElement CellName(AccountListDto account, int col)
    {
        var tb = new TextBlock
        {
            Text = account.NameAr,
            FontSize = 13,
            Foreground = account.IsActive ? Br("TextPrimaryBrush") : Br("TextMutedBrush"),
            VerticalAlignment = VerticalAlignment.Center,
            TextTrimming = TextTrimming.CharacterEllipsis,
            Margin = new Thickness(0, 0, account.Level * 16, 0)
        };
        Grid.SetColumn(tb, col);
        return tb;
    }

    private UIElement CellToggle(bool isOn, int col)
    {
        var track = new Border
        {
            Width = 38,
            Height = 20,
            CornerRadius = new CornerRadius(10),
            Background = isOn
                ? new SolidColorBrush(Color.FromRgb(0, 150, 136))
                : new SolidColorBrush(Color.FromRgb(200, 200, 200)),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Child = new Border
            {
                Width = 16,
                Height = 16,
                CornerRadius = new CornerRadius(8),
                Background = Brushes.White,
                HorizontalAlignment = isOn ? HorizontalAlignment.Right : HorizontalAlignment.Left,
                Margin = new Thickness(2)
            }
        };
        Grid.SetColumn(track, col);
        return track;
    }

    private UIElement CellCheck(int col)
    {
        var cb = new CheckBox
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            IsHitTestVisible = false
        };
        Grid.SetColumn(cb, col);
        return cb;
    }

    private static UIElement CellText(
        string text,
        object width,
        int col,
        Brush foreground,
        FontWeight? weight = null)
    {
        var tb = new TextBlock
        {
            Text = text,
            FontSize = 12,
            Foreground = foreground,
            FontWeight = weight ?? FontWeights.Normal,
            VerticalAlignment = VerticalAlignment.Center,
            TextTrimming = TextTrimming.CharacterEllipsis
        };
        Grid.SetColumn(tb, col);
        return tb;
    }

    private static UIElement CellHeader(string text, object width, int col)
    {
        var tb = new TextBlock
        {
            Text = text,
            FontSize = 11,
            FontWeight = FontWeights.SemiBold,
            Foreground = Br("TextSecondaryBrush"),
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(8, 0, 8, 0)
        };
        Grid.SetColumn(tb, col);
        return tb;
    }

    private static Grid CreateRowGrid()
    {
        var grid = new Grid
        {
            FlowDirection = FlowDirection.RightToLeft,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(ColCheck) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star), MinWidth = 120 });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(ColType) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(ColReconcile) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(ColCurrency) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(ColSetup) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(ColCode) });
        return grid;
    }

    private void RefreshFilterChips()
    {
        _filterChips.Children.Clear();
        if (_activeOnly)
            _filterChips.Children.Add(CreateFilterChip("حساب نشط", () =>
            {
                _activeOnly = false;
                RefreshFilterChips();
                RenderTree();
            }));
    }

    private UIElement CreateFilterChip(string label, Action onRemove)
    {
        var chip = new Border
        {
            Background = Br("PrimaryVeryLightBrush"),
            BorderBrush = Br("PrimaryBrush"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(4),
            Padding = new Thickness(8, 4, 8, 4),
            Margin = new Thickness(0, 0, 6, 0)
        };
        var sp = new StackPanel { Orientation = Orientation.Horizontal };
        sp.Children.Add(new TextBlock
        {
            Text = label,
            FontSize = 11,
            Foreground = Br("PrimaryBrush"),
            VerticalAlignment = VerticalAlignment.Center
        });
        var close = new TextBlock
        {
            Text = " ×",
            FontSize = 13,
            Foreground = Br("PrimaryBrush"),
            Cursor = Cursors.Hand,
            VerticalAlignment = VerticalAlignment.Center
        };
        close.MouseLeftButtonUp += (_, e) => { e.Handled = true; onRemove(); };
        sp.Children.Add(close);
        chip.Child = sp;
        return chip;
    }

    private static Button CreateHeaderIconButton(string glyph, string tooltip, RoutedEventHandler click)
    {
        var btn = new Button
        {
            Content = new TextBlock
            {
                Text = glyph,
                FontFamily = new FontFamily("Segoe MDL2 Assets"),
                FontSize = 14,
                Foreground = Brushes.White
            },
            Background = Brushes.Transparent,
            BorderThickness = new Thickness(0),
            Width = 36,
            Height = 32,
            Margin = new Thickness(4, 0, 0, 0),
            ToolTip = tooltip,
            Cursor = Cursors.Hand
        };
        btn.Click += click;
        return btn;
    }

    private static Button CreateHeaderPrimaryButton(string text, RoutedEventHandler click)
    {
        var btn = new Button
        {
            Content = text,
            Height = 32,
            Padding = new Thickness(14, 0, 14, 0),
            Margin = new Thickness(8, 0, 0, 0),
            Background = Brushes.White,
            Foreground = Br("PrimaryBrush"),
            BorderThickness = new Thickness(0),
            FontWeight = FontWeights.SemiBold,
            FontSize = 12,
            Cursor = Cursors.Hand
        };
        btn.Click += click;
        return btn;
    }

    private static Brush Br(string key) => (Brush)WpfApplication.Current.Resources[key]!;
    private static Style S(string key) => (Style)WpfApplication.Current.Resources[key]!;
}
