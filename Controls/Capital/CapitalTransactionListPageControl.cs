using ERPSystem.Application.Abstractions.Repositories;
using ERPSystem.Application.DTOs.Capital;
using ERPSystem.Controls;
using ERPSystem.Core;
using ERPSystem.Core.Actions;
using ERPSystem.Helpers;
using ERPSystem.Services;
using ERPSystem.Services.Capital;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using ERPSystem.Diagnostics.Performance;

namespace ERPSystem.Controls.Capital;

public sealed class CapitalTransactionListPageControl : UserControl
{
    private readonly ErpListModuleControl _page = new();
    private readonly ComboBox _partnerFilter = new() { MinWidth = 200, Margin = new Thickness(0, 0, 8, 0) };
    private readonly DatePicker _fromDate = ErpUiFactory.FormDate(DateTime.Today.AddMonths(-6));
    private readonly DatePicker _toDate = ErpUiFactory.FormDate(DateTime.Today);
    private readonly DispatcherTimer _searchTimer = new() { Interval = TimeSpan.FromMilliseconds(350) };
    private string _pendingSearch = "";
    private bool _isLoading;

    public CapitalTransactionListPageControl()
    {
        Content = _page;
        ConfigureList();
        Loaded += OnLoaded;
        Unloaded += (_, _) => CapitalListRefreshHub.RefreshRequested -= OnRefreshRequested;
        CapitalListRefreshHub.RefreshRequested += OnRefreshRequested;
        _searchTimer.Tick += async (_, _) => { _searchTimer.Stop(); await LoadAsync(); };
    }

    private void ConfigureList()
    {
        _page.Configure(EntityType.CapitalPartner, AppModule.CapitalPartners);
        _page.SetHeader("سجل حركات رأس المال", "استثمارات وسحوبات الشركاء", "\uE8A5", B("PrimaryBrush"));
        _page.SetPrimaryButton("حركة جديدة");
        _page.SetEmptyState("لا توجد حركات مسجّلة", "حركة جديدة", "\uE8A5");
        _page.EnableServerSideSearch();

        var dateRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 8) };
        dateRow.Children.Add(Labeled("من", _fromDate));
        dateRow.Children.Add(Labeled("إلى", _toDate));

        var filterPanel = new StackPanel();
        filterPanel.Children.Add(dateRow);
        filterPanel.Children.Add(Labeled("الشريك", _partnerFilter));
        _page.SetFilterExtras(filterPanel);

        _partnerFilter.SelectionChanged += (_, _) => _ = LoadAsync();
        _fromDate.SelectedDateChanged += (_, _) => _ = LoadAsync();
        _toDate.SelectedDateChanged += (_, _) => _ = LoadAsync();

        _page.PrimaryActionRequested += (_, _) => MockInteractionService.Navigate(AppModule.CapitalPartners, "Investment");

        _page.SearchChanged += (_, term) =>
        {
            _pendingSearch = term;
            _searchTimer.Stop();
            _searchTimer.Start();
        };

        SetupGrid(_page.Grid);
    }

    private static UIElement Labeled(string label, UIElement control)
    {
        var sp = new StackPanel { Margin = new Thickness(0, 0, 12, 0) };
        sp.Children.Add(new TextBlock
        {
            Text = label,
            FontSize = 11,
            Foreground = (System.Windows.Media.Brush)System.Windows.Application.Current.Resources["TextMutedBrush"]!
        });
        sp.Children.Add(control);
        return sp;
    }

    private void SetupGrid(DataGrid g)
    {
        g.AutoGenerateColumns = false;
        g.IsReadOnly = true;
        foreach (var (h, p, w, fmt) in new (string, string, object, string?)[]
        {
            ("التاريخ", nameof(CapitalTransactionListDto.TransactionDate), 95, "yyyy/MM/dd"),
            ("الشريك", nameof(CapitalTransactionListDto.PartnerName), "*", null),
            ("النوع", nameof(CapitalTransactionListDto.TypeDisplay), 120, null),
            ("المبلغ", nameof(CapitalTransactionListDto.AmountOriginal), 100, "N2"),
            ("بالأساس", nameof(CapitalTransactionListDto.SignedBaseAmount), 110, "N2"),
            ("العملة", nameof(CapitalTransactionListDto.Currency), 60, null),
            ("البيان", nameof(CapitalTransactionListDto.Notes), "*", null)
        })
            ErpUiFactory.AddGridColumn(g, h, p, w, fmt);
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        var partners = await CapitalPartnerUiService.Instance.GetListAsync(new CapitalPartnerListFilter(), 1, 500);
        if (ApplicationResultPresenter.Present(partners))
        {
            var items = new List<CapitalPartnerListDto> { new() { Id = Guid.Empty, FullName = "— الكل —" } };
            items.AddRange(partners.Value?.Items ?? []);
            _partnerFilter.ItemsSource = items;
            _partnerFilter.DisplayMemberPath = nameof(CapitalPartnerListDto.FullName);
            _partnerFilter.SelectedIndex = 0;
        }

        await LoadAsync();
    }

    private void OnRefreshRequested(object? sender, EventArgs e) => _ = LoadAsync();

    private async Task LoadAsync()
    {
        using var perfScope = ScreenLoadProfiler.Begin("Capital.Transactions");
        if (_isLoading || !AppServices.IsInitialized) return;
        _isLoading = true;
        try
        {
            Guid? partnerId = null;
            if (_partnerFilter.SelectedItem is CapitalPartnerListDto sel && sel.Id != Guid.Empty)
                partnerId = sel.Id;

            var filter = new CapitalTransactionListFilter
            {
                Search = string.IsNullOrWhiteSpace(_pendingSearch) ? null : _pendingSearch.Trim(),
                PartnerId = partnerId,
                FromDate = _fromDate.SelectedDate,
                ToDate = _toDate.SelectedDate
            };

            var result = await ScreenLoadProfiler.MeasureLoadAsync(perfScope, () => CapitalPartnerUiService.Instance.GetTransactionsAsync(filter, 1, 500));
        perfScope?.IncrementServiceCalls();
            if (!ApplicationResultPresenter.Present(result)) return;
            _page.BindData(result.Value?.Items ?? Array.Empty<CapitalTransactionListDto>());
        }
        finally { _isLoading = false; }
    }

    private static System.Windows.Media.Brush B(string key) =>
        (System.Windows.Media.Brush)System.Windows.Application.Current.Resources[key]!;
}
