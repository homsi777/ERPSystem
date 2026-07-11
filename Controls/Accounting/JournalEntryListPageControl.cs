using ERPSystem.Application.Abstractions.Repositories;
using ERPSystem.Application.Common;
using ERPSystem.Application.DTOs.Accounting;
using ERPSystem.Application.Queries.Accounting;
using ERPSystem.Controls;
using ERPSystem.Core;
using ERPSystem.Core.Actions;
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

public sealed class JournalEntryListPageControl : UserControl
{
    private sealed record StatusFilterOption(JournalEntryStatus? Status, string Label);

    private readonly ErpListModuleControl _page = new();
    private readonly ComboBox _statusFilter = ErpUiFactory.FilterCombo(["— كل الحالات —"], 150);
    private readonly DatePicker _fromDate = ErpUiFactory.FormDate(DateTime.Today.AddMonths(-1));
    private readonly DatePicker _toDate = ErpUiFactory.FormDate(DateTime.Today);
    private readonly DispatcherTimer _searchTimer = new() { Interval = TimeSpan.FromMilliseconds(350) };
    private string _pendingSearch = "";
    private bool _isLoading;

    public JournalEntryListPageControl()
    {
        ConfigureList();
        Content = _page;
        Loaded += OnLoaded;
        Unloaded += (_, _) => AccountingListRefreshHub.RefreshRequested -= OnRefreshRequested;
        AccountingListRefreshHub.RefreshRequested += OnRefreshRequested;
        _searchTimer.Tick += async (_, _) =>
        {
            _searchTimer.Stop();
            await LoadAsync();
        };
    }

    private void ConfigureList()
    {
        _page.Configure(EntityType.JournalEntry, AppModule.Accounting);
        _page.SetHeader("دفتر اليومية", "قيود اليومية — انقر مرتين على السطر لعرض التفاصيل", "\uE8C1", B("AccentPrimaryBrush"));
        _page.SetPrimaryButton("قيد جديد");
        _page.SetEmptyState("لا توجد قيود يومية", "قيد جديد", "\uE8C1");
        _page.EnableServerSideSearch();

        var filterPanel = new StackPanel { Orientation = Orientation.Horizontal };
        filterPanel.Children.Add(WrapFilter("من", _fromDate));
        filterPanel.Children.Add(WrapFilter("إلى", _toDate));
        filterPanel.Children.Add(WrapFilter("الحالة", _statusFilter));
        _page.SetFilterExtras(filterPanel);

        _page.PrimaryActionRequested += (_, _) => AccountingPopupService.ShowCreateJournal();
        _page.SearchChanged += (_, term) =>
        {
            _pendingSearch = term;
            _searchTimer.Stop();
            _searchTimer.Start();
        };

        _statusFilter.SelectionChanged += (_, _) => _ = LoadAsync();
        _fromDate.SelectedDateChanged += (_, _) => _ = LoadAsync();
        _toDate.SelectedDateChanged += (_, _) => _ = LoadAsync();

        _page.Grid.MouseDoubleClick += (_, _) =>
        {
            if (_page.Grid.SelectedItem is JournalEntryListDto row)
                AccountingPopupService.ShowJournalDetails(row);
        };

        _page.Grid.PreviewMouseRightButtonDown += OnGridRightClick;

        ConfigureGridColumns();
    }

    private void ConfigureGridColumns()
    {
        var g = _page.Grid;
        g.AutoGenerateColumns = false;
        g.IsReadOnly = true;
        g.CanUserAddRows = false;
        g.SelectionMode = DataGridSelectionMode.Single;
        g.Columns.Clear();

        foreach (var (h, p, w, fmt) in new (string, string, object, string?)[]
        {
            ("رقم القيد", nameof(JournalEntryListDto.EntryNumber), 120, null),
            ("التاريخ", nameof(JournalEntryListDto.EntryDate), 100, "yyyy/MM/dd"),
            ("البيان", nameof(JournalEntryListDto.Description), "*", null),
            ("مدين", nameof(JournalEntryListDto.DebitTotal), 110, "N2"),
            ("دائن", nameof(JournalEntryListDto.CreditTotal), 110, "N2"),
            ("الحالة", nameof(JournalEntryListDto.StatusDisplay), 110, null),
            ("المصدر", nameof(JournalEntryListDto.SourceTypeDisplay), 120, null),
            ("سطور", nameof(JournalEntryListDto.LineCount), 60, null)
        })
            ErpUiFactory.AddGridColumn(g, h, p, w, fmt);
    }

    private static UIElement WrapFilter(string label, UIElement control)
    {
        var sp = new StackPanel { Margin = new Thickness(0, 0, 10, 0) };
        sp.Children.Add(new TextBlock
        {
            Text = label,
            FontSize = 11,
            Foreground = Br("TextMutedBrush"),
            FontFamily = new FontFamily("Segoe UI, Tahoma, Arial")
        });
        sp.Children.Add(control);
        return sp;
    }

    private static void OnGridRightClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is not DataGrid grid) return;
        if (FindParent<DataGridRow>(e.OriginalSource as DependencyObject) is not { Item: JournalEntryListDto row } dgRow)
            return;

        e.Handled = true;
        dgRow.IsSelected = true;
        AccountingContextMenuService.ShowJournal(row, dgRow);
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
        Loaded -= OnLoaded;

        var statusOptions = new List<StatusFilterOption> { new(null, "— كل الحالات —") };
        foreach (JournalEntryStatus status in Enum.GetValues(typeof(JournalEntryStatus)))
            statusOptions.Add(new(status, status.ToDisplay()));

        _statusFilter.ItemsSource = statusOptions;
        _statusFilter.DisplayMemberPath = nameof(StatusFilterOption.Label);
        _statusFilter.SelectedIndex = 0;

        _page.SetPrimaryButtonEnabled(await AccountingUiService.Instance.CanCreateJournalAsync());
        await LoadAsync();
    }

    private void OnRefreshRequested(object? sender, EventArgs e) => _ = LoadAsync();

    private async Task LoadAsync()
    {
        if (_isLoading || !AppServices.IsInitialized)
        {
            _page.BindData([]);
            return;
        }

        _isLoading = true;
        _page.SetLoadingState(true);
        try
        {
            JournalEntryStatus? status = _statusFilter.SelectedItem is StatusFilterOption opt ? opt.Status : null;
            var filter = new JournalEntryListFilter
            {
                Search = string.IsNullOrWhiteSpace(_pendingSearch) ? null : _pendingSearch.Trim(),
                Status = status,
                FromDate = _fromDate.SelectedDate,
                ToDate = _toDate.SelectedDate
            };

            var result = await AccountingUiService.Instance.GetJournalEntriesAsync(filter, 1, 500);
            if (!ApplicationResultPresenter.Present(result))
            {
                _page.BindData([]);
                return;
            }

            var list = result.Value?.Items ?? [];
            _page.BindData(list.Cast<object>().ToList());
            _page.SetFilterSummary($"عرض {list.Count} قيد");
        }
        finally
        {
            _page.SetLoadingState(false);
            _isLoading = false;
        }
    }

    private static Brush Br(string key) => (Brush)WpfApplication.Current.Resources[key]!;
    private static SolidColorBrush B(string key) => (SolidColorBrush)WpfApplication.Current.Resources[key]!;
}
