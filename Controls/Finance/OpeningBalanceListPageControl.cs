using ERPSystem.Application.DTOs.Finance;
using ERPSystem.Application.Queries.Finance;
using ERPSystem.Core;
using ERPSystem.Core.Actions;
using ERPSystem.Dialogs;
using ERPSystem.Domain.Entities.Finance;
using ERPSystem.Helpers;
using ERPSystem.Services;
using ERPSystem.Services.Finance;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace ERPSystem.Controls.Finance;

public sealed class OpeningBalanceListPageControl : UserControl
{
    private readonly ErpListModuleControl _page = new();
    private readonly ComboBox _typeFilter = ErpUiFactory.FilterCombo(
        ["الكل", "مخزون", "عملاء", "موردين", "نقدية", "بنك", "رأس مال", "دفتر الأستاذ"], 160);
    private bool _isLoading;

    public OpeningBalanceListPageControl()
    {
        _page.Configure(EntityType.OpeningBalance, AppModule.Accounting);
        _page.SetHeader("أرصدة افتتاحية", "المالية", "\uE8F1", B("AccentPrimaryBrush"));
        _page.SetPrimaryButton("رصيد جديد");
        _page.SetEmptyState("لا توجد أرصدة افتتاحية بعد", "رصيد جديد", "\uE710");
        _page.SetFilterExtras([_typeFilter]);
        _page.PrimaryActionRequested += (_, _) => ShowTypePicker();
        _page.Grid.MouseDoubleClick += (_, _) =>
        {
            if (_page.Grid.SelectedItem is OpeningBalanceListDto row)
                OpeningBalancePopupService.ShowOperationsCenter(row);
        };
        _page.Grid.PreviewMouseRightButtonDown += OnGridRightClick;

        ConfigureGrid();
        Content = _page;
        Loaded += OnLoaded;
        _typeFilter.SelectionChanged += async (_, _) => await LoadAsync();
        Unloaded += OnUnloaded;
        OpeningBalanceListRefreshHub.RefreshRequested += OnRefreshRequested;
    }

    private void OnUnloaded(object sender, RoutedEventArgs e) =>
        OpeningBalanceListRefreshHub.RefreshRequested -= OnRefreshRequested;

    private void OnRefreshRequested(object? sender, EventArgs e) => _ = LoadAsync();

    private void ConfigureGrid()
    {
        var g = _page.Grid;
        g.AutoGenerateColumns = false;
        g.IsReadOnly = true;
        foreach (var (h, p, w) in new (string, string, object)[]
        {
            ("الرقم", nameof(OpeningBalanceListDto.Number), 130),
            ("النوع", nameof(OpeningBalanceListDto.TypeDisplay), 150),
            ("الحالة", nameof(OpeningBalanceListDto.StatusDisplay), 120),
            ("التاريخ", nameof(OpeningBalanceListDto.OpeningDate), 100),
            ("العملة", nameof(OpeningBalanceListDto.CurrencyCode), 70),
            ("الإجمالي", nameof(OpeningBalanceListDto.TotalBaseAmount), 110),
            ("السطور", nameof(OpeningBalanceListDto.LineCount), 60),
            ("المصدر", nameof(OpeningBalanceListDto.SourceDisplay), 100),
            ("القيد", nameof(OpeningBalanceListDto.JournalEntryNumber), 120)
        })
            ErpUiFactory.AddGridColumn(g, h, p, w, p == nameof(OpeningBalanceListDto.TotalBaseAmount) ? "N2" : null);
    }

    private static void OnGridRightClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is not DataGrid grid) return;
        if (FindParent<DataGridRow>(e.OriginalSource as DependencyObject) is not { Item: OpeningBalanceListDto row } dgRow)
            return;

        e.Handled = true;
        dgRow.IsSelected = true;
        OpeningBalanceContextMenuService.Show(row, dgRow);
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

    private static void ShowTypePicker()
    {
        var panel = new StackPanel { Margin = new Thickness(8) };
        panel.Children.Add(ErpUiFactory.SectionTitle("اختر نوع الرصيد الافتتاحي"));
        foreach (OpeningBalanceType type in Enum.GetValues<OpeningBalanceType>())
        {
            if ((int)type > 6) continue;
            var btn = new Button
            {
                Content = OpeningBalanceDisplay.TypeName(type),
                Margin = new Thickness(0, 4, 0, 4),
                Padding = new Thickness(12, 8, 12, 8),
                HorizontalAlignment = HorizontalAlignment.Stretch
            };
            btn.Click += (_, _) =>
            {
                OpeningBalanceNavigationContext.BeginCreate(type);
                ErpModalWindow.Show(
                    "رصيد افتتاحي جديد",
                    OpeningBalanceDisplay.TypeName(type),
                    new OpeningBalanceFormControl(),
                    "\uE710", 720, 780);
            };
            panel.Children.Add(btn);
        }

        ErpModalWindow.Show("رصيد افتتاحي جديد", "اختر النوع", panel, "\uE710", 420);
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        Loaded -= OnLoaded;
        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        if (_isLoading || !AppServices.IsInitialized) { _page.BindData([]); return; }
        _isLoading = true;
        _page.SetLoadingState(true);
        try
        {
            var filter = new OpeningBalanceListFilter { Type = MapTypeFilter() };
            var result = await OpeningBalanceUiService.Instance.GetListAsync(filter, 1, 500);
            _page.BindData(result.IsSuccess && result.Value is not null
                ? result.Value.Items.Cast<object>().ToList()
                : []);
        }
        finally
        {
            _page.SetLoadingState(false);
            _isLoading = false;
        }
    }

    private OpeningBalanceType? MapTypeFilter() => _typeFilter.SelectedIndex switch
    {
        1 => OpeningBalanceType.OpeningStock,
        2 => OpeningBalanceType.CustomerReceivable,
        3 => OpeningBalanceType.SupplierPayable,
        4 => OpeningBalanceType.Cash,
        5 => OpeningBalanceType.Bank,
        6 => OpeningBalanceType.Capital,
        7 => OpeningBalanceType.GeneralLedger,
        _ => null
    };

    private static SolidColorBrush B(string key) =>
        (SolidColorBrush)System.Windows.Application.Current.Resources[key]!;
}
