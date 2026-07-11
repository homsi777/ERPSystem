using ERPSystem.Application.Common;

using ERPSystem.Application.DTOs.Finance;

using ERPSystem.Application.Queries.Finance;

using ERPSystem.Controls;

using ERPSystem.Dialogs;

using ERPSystem.Domain.Entities.Finance;

using ERPSystem.Helpers;

using ERPSystem.Services;

using ERPSystem.Services.Customers;

using ERPSystem.Services.Documents;

using ERPSystem.Services.Finance;

using Microsoft.Win32;

using System.Collections;

using System.IO;

using System.Windows;

using System.Windows.Controls;

using System.Windows.Controls.Primitives;

using System.Windows.Data;

using System.Windows.Input;

using System.Windows.Media;

using System.Windows.Threading;

using WpfApplication = System.Windows.Application;



namespace ERPSystem.Controls.Customers;



/// <summary>أرصدة افتتاحية العملاء — قائمة احترافية مع KPI، فلاتر، استيراد Excel، ومركز العمليات.</summary>

public sealed class CustomerOpeningBalanceControl : UserControl

{

    private const int PageSize = 50;

    private const int KpiColumns = 3;

    private const double FilterControlWidth = 200;



    private readonly ComboBox _customerFilter = new();

    private readonly ComboBox _statusFilter = ErpUiFactory.FilterCombo(

        ["الكل", "مسودة", "بانتظار الاعتماد", "معتمد", "مرحّل", "مرفوض"], FilterControlWidth);

    private readonly DatePicker _fromDate = ErpUiFactory.FormDate(DateTime.Today.AddMonths(-3));

    private readonly DatePicker _toDate = ErpUiFactory.FormDate(DateTime.Today);

    private readonly TextBox _amountFrom = ErpUiFactory.FormField("");

    private readonly TextBox _amountTo = ErpUiFactory.FormField("");

    private readonly TextBox _search = new() { Style = S("EnterpriseSearchInputStyle"), Tag = "بحث برقم المستند أو العميل..." };

    private readonly UniformGrid _kpiGrid = new() { Columns = KpiColumns, Margin = new Thickness(16, 6, 16, 4) };

    private readonly DataGrid _grid = new()

    {

        Style = S("EnterpriseDataGridStyle"),

        ColumnHeaderStyle = S("EnterpriseColumnHeaderStyle"),

        RowStyle = S("EnterpriseDataGridRowStyle"),

        CellStyle = S("EnterpriseDataGridCellStyle"),

        AutoGenerateColumns = false,

        IsReadOnly = true,

        CanUserAddRows = false,

        CanUserSortColumns = true,

        HorizontalAlignment = HorizontalAlignment.Stretch,

        VerticalAlignment = VerticalAlignment.Stretch,

        GridLinesVisibility = DataGridGridLinesVisibility.Horizontal,

        HeadersVisibility = DataGridHeadersVisibility.Column

    };

    private readonly TextBlock _recordCount = new()

    {

        FontSize = ErpDesignTokens.FontCaption,

        Foreground = Br("TextMutedBrush"),

        FontFamily = ErpDesignTokens.UiFont,

        VerticalAlignment = VerticalAlignment.Center

    };

    private readonly TextBlock _pageInfo = new()

    {

        FontSize = ErpDesignTokens.FontCaption,

        Foreground = Br("TextMutedBrush"),

        FontFamily = ErpDesignTokens.UiFont,

        VerticalAlignment = VerticalAlignment.Center,

        Margin = new Thickness(12, 0, 0, 0)

    };

    private readonly Button _btnPrev = new()

    {

        Content = "السابق",

        Style = S("GhostButtonStyle"),

        Height = ErpDesignTokens.ControlHeight,

        Padding = new Thickness(12, 0, 12, 0)

    };

    private readonly Button _btnNext = new()

    {

        Content = "التالي",

        Style = S("GhostButtonStyle"),

        Height = ErpDesignTokens.ControlHeight,

        Padding = new Thickness(12, 0, 12, 0),

        Margin = new Thickness(6, 0, 0, 0)

    };

    private readonly Border _emptyState = new() { Visibility = Visibility.Collapsed, Background = Brushes.White };

    private readonly DispatcherTimer _searchTimer = new() { Interval = TimeSpan.FromMilliseconds(400) };



    private int _page = 1;

    private int _totalCount;

    private bool _isLoading;

    private bool _isBindingFilters;

    private IReadOnlyList<OpeningBalanceLookupItemDto> _customers = [];



    public CustomerOpeningBalanceControl()

    {

        FlowDirection = FlowDirection.RightToLeft;

        Background = Br("AppBgBrush");

        PrepareFilterControls();

        Content = BuildLayout();

        ConfigureGrid();



        Loaded += OnLoaded;

        Unloaded += OnUnloaded;

        _searchTimer.Tick += async (_, _) => { _searchTimer.Stop(); _page = 1; await LoadAsync(); };

        _search.TextChanged += (_, _) => { _searchTimer.Stop(); _searchTimer.Start(); };

        foreach (var c in new[] { _customerFilter, _statusFilter })

            c.SelectionChanged += async (_, _) =>

            {

                if (_isBindingFilters) return;

                _page = 1;

                await LoadAsync();

            };

        _fromDate.SelectedDateChanged += async (_, _) => { _page = 1; await LoadAsync(); };

        _toDate.SelectedDateChanged += async (_, _) => { _page = 1; await LoadAsync(); };

        _btnPrev.Click += async (_, _) => { if (_page > 1) { _page--; await LoadAsync(); } };

        _btnNext.Click += async (_, _) =>

        {

            if (_page * PageSize < _totalCount) { _page++; await LoadAsync(); }

        };



        CustomerOpeningBalanceRefreshHub.RefreshRequested += OnRefreshRequested;

        OpeningBalanceListRefreshHub.RefreshRequested += OnRefreshRequested;

    }



    private void PrepareFilterControls()

    {

        StyleFilterControl(_customerFilter, FilterControlWidth);

        StyleFilterControl(_amountFrom, FilterControlWidth);

        StyleFilterControl(_amountTo, FilterControlWidth);

        StyleFilterControl(_fromDate, FilterControlWidth);

        StyleFilterControl(_toDate, FilterControlWidth);

        _search.Height = ErpDesignTokens.ControlHeight;

        _search.FontSize = ErpDesignTokens.FontBody - 1;

        _search.VerticalContentAlignment = VerticalAlignment.Center;

    }



    private void OnUnloaded(object sender, RoutedEventArgs e)

    {

        CustomerOpeningBalanceRefreshHub.RefreshRequested -= OnRefreshRequested;

        OpeningBalanceListRefreshHub.RefreshRequested -= OnRefreshRequested;

    }



    private void OnRefreshRequested(object? sender, EventArgs e) => _ = LoadAsync();



    private UIElement BuildLayout()

    {

        var root = new Grid { Margin = new Thickness(0) };

        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(7, GridUnitType.Star), MinHeight = 360 });

        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });



        var toolbar = BuildToolbar();
        var filters = BuildFilters();

        Grid.SetRow(toolbar, 0);
        Grid.SetRow(_kpiGrid, 1);
        Grid.SetRow(filters, 2);



        var body = new Border

        {

            Background = Brushes.White,

            BorderBrush = Br("BorderBrush"),

            BorderThickness = new Thickness(1, 1, 1, 0),

            Margin = new Thickness(16, 0, 16, 0),

            Padding = new Thickness(0)

        };

        var bodyGrid = new Grid();

        bodyGrid.Children.Add(_grid);



        var emptyBtn = new Button

        {

            Content = "رصيد افتتاحي جديد",

            Style = S("PrimaryButtonStyle"),

            Height = ErpDesignTokens.ToolbarHeight,

            Margin = new Thickness(0, 12, 0, 0)

        };

        emptyBtn.Click += (_, _) => ShowNewForm();



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

                    FontFamily = ErpDesignTokens.IconFont,

                    FontSize = 28,

                    Foreground = Br("TextMutedBrush"),

                    HorizontalAlignment = HorizontalAlignment.Center,

                    Margin = new Thickness(0, 0, 0, 6)

                },

                new TextBlock

                {

                    Text = "لا توجد أرصدة افتتاحية للعملاء",

                    FontSize = ErpDesignTokens.FontBody,

                    Foreground = Br("TextSecondaryBrush"),

                    HorizontalAlignment = HorizontalAlignment.Center,

                    FontFamily = ErpDesignTokens.UiFont

                },

                emptyBtn

            }

        };

        bodyGrid.Children.Add(_emptyState);

        body.Child = bodyGrid;

        Grid.SetRow(body, 3);



        var footer = new Border

        {

            Background = Br("SurfaceAltBrush"),

            BorderBrush = Br("BorderBrush"),

            BorderThickness = new Thickness(1, 1, 1, 0),

            Margin = new Thickness(16, 0, 16, 8),

            Padding = new Thickness(16, 8, 16, 8),

            Child = BuildFooter()

        };

        Grid.SetRow(footer, 4);



        root.Children.Add(toolbar);
        root.Children.Add(_kpiGrid);
        root.Children.Add(filters);

        root.Children.Add(body);

        root.Children.Add(footer);

        return root;

    }



    private UIElement BuildFooter()

    {

        var grid = new Grid();

        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });



        var left = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };

        left.Children.Add(_recordCount);

        left.Children.Add(_pageInfo);



        var right = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };

        right.Children.Add(_btnPrev);

        right.Children.Add(_btnNext);



        Grid.SetColumn(left, 0);

        Grid.SetColumn(right, 1);

        grid.Children.Add(left);

        grid.Children.Add(right);

        return grid;

    }



    private UIElement BuildToolbar()

    {

        var wrap = new WrapPanel

        {

            Orientation = Orientation.Horizontal,

            Margin = new Thickness(0)

        };

        wrap.Children.Add(ToolbarBtn("رصيد افتتاحي جديد", "\uE710", S("PrimaryButtonStyle"), (_, _) => ShowNewForm()));

        wrap.Children.Add(ToolbarBtn("استيراد Excel", "\uE896", S("SecondaryButtonStyle"), (_, _) => ShowImport()));

        wrap.Children.Add(ToolbarBtn("تحميل قالب Excel", "\uE8E5", S("GhostButtonStyle"), (_, _) => DownloadTemplate()));

        wrap.Children.Add(ToolbarBtn("تصدير", "\uEDE1", S("GhostButtonStyle"), (_, _) => ExportGrid()));

        wrap.Children.Add(ToolbarBtn("تحديث", "\uE72C", S("GhostButtonStyle"), async (_, _) => await LoadAsync()));



        return new Border

        {

            Background = Br("SurfaceBrush"),

            BorderBrush = Br("BorderBrush"),

            BorderThickness = new Thickness(0, 0, 0, 1),

            Padding = ErpDesignTokens.ToolbarPadding,

            Child = wrap

        };

    }



    private UIElement BuildFilters()

    {

        var host = new Grid { Margin = new Thickness(16, 6, 16, 8) };

        host.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        host.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });



        var row1 = new Grid { Margin = new Thickness(0, 0, 0, ErpDesignTokens.SpaceSm) };

        var row2 = new Grid();

        foreach (var _ in Enumerable.Range(0, 4))

        {

            row1.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star), MinWidth = 160 });

            row2.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star), MinWidth = 160 });

        }



        PlaceFilter(row1, 0, "العميل", _customerFilter);

        PlaceFilter(row1, 1, "الحالة", _statusFilter);

        PlaceFilter(row1, 2, "من تاريخ", _fromDate);

        PlaceFilter(row1, 3, "إلى تاريخ", _toDate);



        PlaceFilter(row2, 0, "من مبلغ", _amountFrom);

        PlaceFilter(row2, 1, "إلى مبلغ", _amountTo);



        var searchHost = new Border

        {

            Background = Br("SurfaceBrush"),

            BorderBrush = Br("BorderBrush"),

            BorderThickness = new Thickness(1),

            CornerRadius = ErpDesignTokens.Radius,

            Padding = new Thickness(8, 0, 8, 0),

            Height = ErpDesignTokens.ControlHeight,

            VerticalAlignment = VerticalAlignment.Bottom,

            Child = _search

        };

        PlaceFilter(row2, 2, "بحث", searchHost);



        var clearBtn = new Button

        {

            Content = "مسح الفلاتر",

            Style = S("GhostButtonStyle"),

            Height = ErpDesignTokens.ControlHeight,

            Padding = new Thickness(14, 0, 14, 0),

            HorizontalAlignment = HorizontalAlignment.Stretch,

            VerticalAlignment = VerticalAlignment.Bottom

        };

        clearBtn.Click += async (_, _) => await ClearFiltersAsync();

        PlaceFilter(row2, 3, " ", clearBtn);



        Grid.SetRow(row1, 0);

        Grid.SetRow(row2, 1);

        host.Children.Add(row1);

        host.Children.Add(row2);



        return new Border

        {

            Background = Br("SurfaceBrush"),

            BorderBrush = Br("BorderBrush"),

            BorderThickness = new Thickness(0, 0, 0, 1),

            Child = host

        };

    }



    private static void PlaceFilter(Grid row, int column, string label, UIElement control)

    {

        var cell = new StackPanel

        {

            Margin = new Thickness(column == 0 ? 0 : ErpDesignTokens.SpaceSm, 0, ErpDesignTokens.SpaceSm, 0)

        };

        if (!string.IsNullOrWhiteSpace(label.Trim()))

        {

            cell.Children.Add(new TextBlock

            {

                Text = label,

                FontSize = ErpDesignTokens.FontCaption,

                Foreground = Br("TextMutedBrush"),

                FontFamily = ErpDesignTokens.UiFont,

                Margin = new Thickness(0, 0, 0, ErpDesignTokens.SpaceXs)

            });

        }

        cell.Children.Add(control);

        Grid.SetColumn(cell, column);

        row.Children.Add(cell);

    }



    private void ConfigureGrid()

    {

        _grid.Columns.Clear();

        AddTextColumn("رقم المستند", nameof(OpeningBalanceListDto.Number), 128);

        AddTextColumn("العميل", nameof(OpeningBalanceListDto.PrimaryPartyDisplay), "*", wrap: true);

        AddTextColumn("تاريخ الافتتاح", nameof(OpeningBalanceListDto.OpeningDate), 112, "yyyy/MM/dd");

        AddTextColumn("العملة", nameof(OpeningBalanceListDto.CurrencyCode), 72);

        AddNumericColumn("مدين", nameof(OpeningBalanceListDto.TotalDebit), 100, "N2", ErpAccountingColorHelper.ApplyDebitStyle);

        AddNumericColumn("دائن", nameof(OpeningBalanceListDto.TotalCredit), 100, "N2", ErpAccountingColorHelper.ApplyCreditStyle);

        AddNumericColumn("الرصيد", nameof(OpeningBalanceListDto.NetBalance), 100, "N2", ErpAccountingColorHelper.ApplySignedBalanceStyle);

        AddTextColumn("الحالة", nameof(OpeningBalanceListDto.StatusDisplay), 120);

        AddTextColumn("تاريخ الترحيل", nameof(OpeningBalanceListDto.PostedAt), 112, "yyyy/MM/dd");

        AddTextColumn("ملاحظات", nameof(OpeningBalanceListDto.DisplayNotes), 160, wrap: true);



        _grid.MouseDoubleClick += (_, _) =>

        {

            if (_grid.SelectedItem is OpeningBalanceListDto row)

                OpeningBalancePopupService.ShowOperationsCenter(row);

        };

        _grid.PreviewMouseRightButtonDown += OnGridRightClick;

    }



    private void AddTextColumn(string header, string path, object width, string? format = null, bool wrap = false)

    {

        var binding = new Binding(path);

        if (format != null) binding.StringFormat = format;



        var col = new DataGridTextColumn
        {
            Header = header,
            Binding = binding,
            Width = ResolveColumnWidth(width)
        };



        if (wrap)

        {

            col.ElementStyle = new Style(typeof(TextBlock))

            {

                Setters =

                {

                    new Setter(TextBlock.TextWrappingProperty, TextWrapping.Wrap),

                    new Setter(TextBlock.VerticalAlignmentProperty, VerticalAlignment.Center)

                }

            };

        }



        _grid.Columns.Add(col);

    }

    private static DataGridLength ResolveColumnWidth(object width) => width switch
    {
        DataGridLength len => len,
        string => new DataGridLength(1, DataGridLengthUnitType.Star),
        int i => i,
        double d => d,
        _ => Convert.ToDouble(width)
    };

    private void AddNumericColumn(string header, string path, double width, string format, Action<DataGridTextColumn, string>? applyStyle = null)

    {

        var col = new DataGridTextColumn

        {

            Header = header,

            Binding = new Binding(path) { StringFormat = format },

            Width = width,

            ElementStyle = new Style(typeof(TextBlock))

            {

                Setters =

                {

                    new Setter(TextBlock.TextAlignmentProperty, TextAlignment.Left),

                    new Setter(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Stretch),

                    new Setter(TextBlock.VerticalAlignmentProperty, VerticalAlignment.Center),

                    new Setter(TextBlock.FontFamilyProperty, ErpDesignTokens.UiFont)

                }

            }

        };

        applyStyle?.Invoke(col, path);
        _grid.Columns.Add(col);
    }



    private static void OnGridRightClick(object sender, MouseButtonEventArgs e)

    {

        if (sender is not DataGrid grid) return;

        if (FindParent<DataGridRow>(e.OriginalSource as DependencyObject) is not { Item: OpeningBalanceListDto row } dgRow)

            return;



        e.Handled = true;

        dgRow.IsSelected = true;

        CustomerOpeningBalanceContextMenuService.Show(row, dgRow);

    }



    private static T? FindParent<T>(DependencyObject? child) where T : DependencyObject

    {

        while (child != null)

        {

            if (child is T match) return match;

            child = VisualTreeHelper.GetParent(child);

        }

        return null;

    }



    private async void OnLoaded(object sender, RoutedEventArgs e)

    {

        await LoadCustomersAsync();

        await LoadAsync();

    }



    private async Task LoadCustomersAsync()

    {

        if (!AppServices.IsInitialized) return;



        var result = await CustomerUiService.Instance.GetListAsync(null, 1, 500);

        if (!result.IsSuccess || result.Value is null) return;



        _isBindingFilters = true;

        try

        {

            _customers = result.Value.Items

                .Select(c => new OpeningBalanceLookupItemDto

                {

                    Id = c.Id,

                    Code = c.Code,

                    Name = c.NameAr

                })

                .ToList();



            var items = new List<OpeningBalanceLookupItemDto>

            {

                new() { Id = Guid.Empty, Code = "", Name = "— الكل —" }

            };

            items.AddRange(_customers);



            _customerFilter.DisplayMemberPath = nameof(OpeningBalanceLookupItemDto.Name);

            _customerFilter.SelectedValuePath = nameof(OpeningBalanceLookupItemDto.Id);

            _customerFilter.ItemsSource = items;

            if (items.Count > 0)

                _customerFilter.SelectedItem = items[0];

        }

        finally

        {

            _isBindingFilters = false;

        }

    }



    private async Task LoadAsync()

    {

        if (_isLoading || !AppServices.IsInitialized) return;

        _isLoading = true;

        _grid.IsEnabled = false;



        try

        {

            var filter = BuildFilter();

            var listResult = await OpeningBalanceUiService.Instance.GetListAsync(filter, _page, PageSize);

            var summaryResult = await OpeningBalanceUiService.Instance.GetCustomerSummaryAsync(filter);



            if (!ApplicationResultPresenter.Present(listResult))

                return;



            _totalCount = listResult.Value?.TotalCount ?? 0;

            var items = listResult.Value?.Items ?? [];

            _grid.ItemsSource = items;

            _emptyState.Visibility = items.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

            _grid.Visibility = items.Count == 0 ? Visibility.Collapsed : Visibility.Visible;



            _recordCount.Text = $"عرض {items.Count} من {_totalCount} سجل";

            _pageInfo.Text = _totalCount == 0

                ? ""

                : $"صفحة {_page} / {Math.Max(1, (int)Math.Ceiling(_totalCount / (double)PageSize))}";

            _btnPrev.IsEnabled = _page > 1;

            _btnNext.IsEnabled = _page * PageSize < _totalCount;



            if (summaryResult.IsSuccess && summaryResult.Value is not null)

                RenderKpis(summaryResult.Value);

        }

        finally

        {

            _grid.IsEnabled = true;

            _isLoading = false;

        }

    }



    private OpeningBalanceListFilter BuildFilter()

    {

        Guid? partyId = null;

        if (_customerFilter.SelectedItem is OpeningBalanceLookupItemDto c && c.Id != Guid.Empty)

            partyId = c.Id;



        decimal? amountFrom = decimal.TryParse(_amountFrom.Text, out var af) ? af : null;

        decimal? amountTo = decimal.TryParse(_amountTo.Text, out var at) ? at : null;



        return new OpeningBalanceListFilter

        {

            Type = OpeningBalanceType.CustomerReceivable,

            Status = MapStatusFilter(),

            Search = string.IsNullOrWhiteSpace(_search.Text) ? null : _search.Text.Trim(),

            From = ApplicationDateNormalizer.ToUtcDate(_fromDate.SelectedDate),

            To = ApplicationDateNormalizer.ToUtcDate(_toDate.SelectedDate),

            PartyId = partyId,

            AmountFrom = amountFrom,

            AmountTo = amountTo

        };

    }



    private OpeningBalanceStatus? MapStatusFilter() =>

        (_statusFilter.SelectedItem as ComboBoxItem)?.Content?.ToString() switch

        {

            "مسودة" => OpeningBalanceStatus.Draft,

            "بانتظار الاعتماد" => OpeningBalanceStatus.PendingApproval,

            "معتمد" => OpeningBalanceStatus.Approved,

            "مرحّل" => OpeningBalanceStatus.Posted,

            "مرفوض" => OpeningBalanceStatus.Rejected,

            _ => null

        };



    private void RenderKpis(CustomerOpeningBalanceSummaryDto s)

    {

        _kpiGrid.Children.Clear();

        foreach (var (title, value, icon, color) in new (string, string, string, SolidColorBrush)[]

        {

            ("إجمالي الأرصدة", s.TotalCount.ToString(), "\uE8F1", SolidBr("PrimaryBrush")),

            ("إجمالي المدين", $"{s.TotalDebit:N2}", "\uE8C1", SolidBr("InfoBrush")),

            ("إجمالي الدائن", $"{s.TotalCredit:N2}", "\uE8C8", SolidBr("WarningBrush")),

            ("صافي الرصيد", $"{s.NetBalance:N2}", "\uE8AB", SolidBr("AccentPrimaryBrush")),

            ("بانتظار الاعتماد", s.PendingApprovalCount.ToString(), "\uE7BA", SolidBr("WarningBrush")),

            ("مستندات مرحّلة", s.PostedCount.ToString(), "\uE73E", SolidBr("SuccessBrush"))

        })

        {

            _kpiGrid.Children.Add(new MetricCardControl

            {

                CardTitle = title,

                CardValue = value,

                CardIcon = icon,

                AccentColor = color,

                Margin = new Thickness(4, 0, 4, ErpDesignTokens.SpaceSm),

                HorizontalAlignment = HorizontalAlignment.Stretch,

                VerticalAlignment = VerticalAlignment.Stretch,

                MinHeight = 92

            });

        }

    }



    private async Task ClearFiltersAsync()

    {

        _isBindingFilters = true;

        try

        {

            _search.Text = "";

            _amountFrom.Text = "";

            _amountTo.Text = "";

            _statusFilter.SelectedIndex = 0;

            if (_customerFilter.ItemsSource is IList list && list.Count > 0)

                _customerFilter.SelectedItem = list[0]!;

            _fromDate.SelectedDate = DateTime.Today.AddMonths(-3);

            _toDate.SelectedDate = DateTime.Today;

        }

        finally

        {

            _isBindingFilters = false;

        }



        _page = 1;

        await LoadAsync();

    }



    private static void ShowImport()

    {

        ErpModalWindow.Show(

            "استيراد أرصدة العملاء",

            "Excel — معاينة وتحقق",

            new CustomerOpeningBalanceImportControl(),

            "\uE896", 780, 820);

    }



    private static void ShowNewForm()

    {

        CustomerOpeningBalanceNavigationContext.BeginCreate();

        ErpModalWindow.Show(

            "رصيد افتتاحي جديد",

            "ذمم عملاء افتتاحية",

            new CustomerOpeningBalanceFormControl(),

            "\uE710", 640, 720);

    }



    private void DownloadTemplate()

    {

        var bytes = OpeningBalanceUiService.Instance.GetImportTemplate(OpeningBalanceType.CustomerReceivable);

        var dlg = new SaveFileDialog

        {

            Filter = "Excel|*.xlsx",

            FileName = "CustomerOpeningBalances_Template.xlsx",

            Title = "تحميل قالب Excel"

        };

        if (dlg.ShowDialog() != true) return;

        File.WriteAllBytes(dlg.FileName, bytes);

        MockInteractionService.ShowSuccess("تم تحميل القالب.");

    }



    private void ExportGrid()

    {

        if (_grid.ItemsSource is not IEnumerable<OpeningBalanceListDto> rows)

        {

            MockInteractionService.ShowWarning("لا توجد بيانات للتصدير.");

            return;

        }



        ListExportService.ExportRecords(

            rows.ToList(),

            "أرصدة افتتاحية — عملاء",

            ("رقم المستند", r => r.Number),

            ("العميل", r => r.PrimaryPartyDisplay),

            ("التاريخ", r => r.OpeningDate),

            ("العملة", r => r.CurrencyCode),

            ("مدين", r => r.TotalDebit),

            ("دائن", r => r.TotalCredit),

            ("الرصيد", r => r.NetBalance),

            ("الحالة", r => r.StatusDisplay),

            ("تاريخ الترحيل", r => r.PostedAt),

            ("ملاحظات", r => r.DisplayNotes));

    }



    private static void StyleFilterControl(FrameworkElement control, double width)

    {

        control.Width = width;

        control.Height = ErpDesignTokens.ControlHeight;

        control.VerticalAlignment = VerticalAlignment.Bottom;

        control.HorizontalAlignment = HorizontalAlignment.Stretch;

        if (control is Control c)

            c.FontSize = ErpDesignTokens.FontBody - 1;

    }



    private static Button ToolbarBtn(string text, string icon, Style style, RoutedEventHandler onClick)

    {

        var sp = new StackPanel { Orientation = Orientation.Horizontal };

        sp.Children.Add(new TextBlock

        {

            Text = icon,

            FontFamily = ErpDesignTokens.IconFont,

            FontSize = 12,

            Margin = new Thickness(0, 0, 6, 0),

            VerticalAlignment = VerticalAlignment.Center

        });

        sp.Children.Add(new TextBlock

        {

            Text = text,

            FontSize = ErpDesignTokens.FontBody - 1,

            FontFamily = ErpDesignTokens.UiFont,

            VerticalAlignment = VerticalAlignment.Center

        });

        var btn = new Button

        {

            Content = sp,

            Style = style,

            Height = ErpDesignTokens.ToolbarHeight,

            Padding = new Thickness(14, 0, 14, 0),

            Margin = new Thickness(0, 0, ErpDesignTokens.SpaceSm, ErpDesignTokens.SpaceSm)

        };

        btn.Click += onClick;

        return btn;

    }



    private static Brush Br(string key) => (Brush)WpfApplication.Current.Resources[key]!;

    private static SolidColorBrush SolidBr(string key) => (SolidColorBrush)WpfApplication.Current.Resources[key]!;

    private static Style S(string key) => (Style)WpfApplication.Current.Resources[key]!;

}


