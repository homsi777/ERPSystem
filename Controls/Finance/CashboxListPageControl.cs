using ERPSystem.Application.DTOs.Finance;
using ERPSystem.Core;
using ERPSystem.Core.Actions;
using ERPSystem.Helpers;
using ERPSystem.Services;
using ERPSystem.Services.Finance;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using ERPSystem.Diagnostics.Performance;

namespace ERPSystem.Controls.Finance;

public sealed class CashboxListPageControl : UserControl
{
    private readonly ErpListModuleControl _page = new();
    private DataGridTemplateColumn? _tasksColumn;
    private bool _isLoading;

    public CashboxListPageControl()
    {
        _page.Configure(EntityType.Cashbox, AppModule.Accounting);
        _page.SetHeader("الصناديق", "إدارة الصناديق النقدية — أرصدة وتحويلات وحركات", "\uE825", B("AccentReceivableBrush"));
        _page.SetPrimaryButton("صندوق جديد");
        _page.SetEmptyState("لا توجد صناديق مضافة", "صندوق جديد", "\uE825");
        _page.PrimaryActionRequested += (_, _) => CashboxPopupService.ShowCreate();

        _page.Grid.MouseDoubleClick += (_, _) =>
        {
            if (_page.Grid.SelectedItem is CashboxListDto row)
                CashboxPopupService.ShowOperationsCenter(row.Id);
        };
        _page.Grid.PreviewMouseLeftButtonUp += OnGridTasksClick;

        var g = _page.Grid;
        g.AutoGenerateColumns = false;
        g.IsReadOnly = true;
        foreach (var (h, p, w) in new (string, string, object)[]
        {
            ("الكود", nameof(CashboxListDto.Code), 90),
            ("الاسم", nameof(CashboxListDto.Name), "*"),
            ("الرصيد", nameof(CashboxListDto.BalanceDisplay), 120),
            ("العملة", nameof(CashboxListDto.Currency), 70),
            ("الحالة", nameof(CashboxListDto.StatusDisplay), 80)
        })
            ErpUiFactory.AddGridColumn(g, h, p, w, null);

        AddActionsColumn(g);
        Content = _page;
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
        CashboxListRefreshHub.RefreshRequested += OnRefreshRequested;
    }

    private void OnUnloaded(object sender, RoutedEventArgs e) =>
        CashboxListRefreshHub.RefreshRequested -= OnRefreshRequested;

    private void OnRefreshRequested(object? sender, EventArgs e) => _ = LoadAsync();

    private void OnGridTasksClick(object sender, MouseButtonEventArgs e)
    {
        if (FindParent<DataGridCell>(e.OriginalSource as DependencyObject) is not { Column: var col, DataContext: CashboxListDto row } cell)
            return;
        if (_tasksColumn is null || col != _tasksColumn) return;
        e.Handled = true;
        CashboxContextMenuService.Show(row, cell);
    }

    private void AddActionsColumn(DataGrid grid)
    {
        _tasksColumn = new DataGridTemplateColumn { Header = "مهام", Width = 72, IsReadOnly = true };
        var template = new DataTemplate();
        var factory = new FrameworkElementFactory(typeof(Border));
        factory.SetValue(Border.BackgroundProperty, Brushes.Transparent);
        factory.SetValue(Border.PaddingProperty, new Thickness(4));
        factory.SetValue(Border.CursorProperty, Cursors.Hand);
        factory.SetValue(Border.ToolTipProperty, "قائمة المهام");
        var icon = new FrameworkElementFactory(typeof(TextBlock));
        icon.SetValue(TextBlock.TextProperty, "\uE712");
        icon.SetValue(TextBlock.FontFamilyProperty, new FontFamily("Segoe MDL2 Assets"));
        icon.SetValue(TextBlock.FontSizeProperty, 16.0);
        icon.SetValue(TextBlock.ForegroundProperty, B("PrimaryBrush"));
        icon.SetValue(TextBlock.HorizontalAlignmentProperty, HorizontalAlignment.Center);
        icon.SetValue(TextBlock.VerticalAlignmentProperty, VerticalAlignment.Center);
        factory.AppendChild(icon);
        template.VisualTree = factory;
        _tasksColumn.CellTemplate = template;
        grid.Columns.Add(_tasksColumn);
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        Loaded -= OnLoaded;
        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        using var perfScope = ScreenLoadProfiler.Begin("Finance.Cashboxes");
        if (_isLoading || !AppServices.IsInitialized) { _page.BindData([]); return; }
        _isLoading = true;
        _page.SetLoadingState(true);
        try
        {
            var result = await ScreenLoadProfiler.MeasureLoadAsync(perfScope, () => FinanceUiService.Instance.GetCashboxListAsync());
        perfScope?.IncrementServiceCalls();
            if (!result.IsSuccess || result.Value is null)
            {
                _page.BindData([]);
                return;
            }
            _page.BindData(result.Value.Cast<object>().ToList());
        }
        finally
        {
            _page.SetLoadingState(false);
            _isLoading = false;
        }
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

    private static SolidColorBrush B(string k) => (SolidColorBrush)System.Windows.Application.Current.Resources[k]!;
}
