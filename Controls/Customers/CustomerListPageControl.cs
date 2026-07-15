using ERPSystem.Controls;
using ERPSystem.Core;
using ERPSystem.Core.Actions;
using ERPSystem.Core.Customers;
using DomainCustomerStatus = ERPSystem.Domain.Enums.CustomerStatus;
using DomainCustomerType = ERPSystem.Domain.Enums.CustomerType;
using ERPSystem.Helpers;
using ERPSystem.Services;
using ERPSystem.Services.Customers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Threading;
using ERPSystem.Diagnostics.Performance;

namespace ERPSystem.Controls.Customers;

public sealed class CustomerListPageControl : UserControl
{
    private readonly ErpListModuleControl _page = new();
    private readonly ComboBox _typeFilter = ErpUiFactory.FilterCombo(["الكل", "نقدي", "آجل"]);
    private readonly ComboBox _statusFilter = ErpUiFactory.FilterCombo(["الكل", "نشط", "موقوف", "محظور"]);
    private readonly DispatcherTimer _searchTimer = new() { Interval = TimeSpan.FromMilliseconds(350) };
    private string _pendingSearch = "";
    private int _pageNumber = 1;
    private int _totalCount;
    private bool _canCreate;
    private bool _isLoading;

    public CustomerListPageControl()
    {
        Content = _page;
        ConfigureList();
        Loaded += OnLoaded;
        Unloaded += (_, _) =>
        {
            CustomerListRefreshHub.RefreshRequested -= OnRefreshRequested;
            ErpDataRefreshHub.DataChanged -= OnDataChanged;
        };
        CustomerListRefreshHub.RefreshRequested += OnRefreshRequested;
        ErpDataRefreshHub.DataChanged += OnDataChanged;
        _searchTimer.Tick += async (_, _) =>
        {
            _searchTimer.Stop();
            await LoadCustomersAsync(_pendingSearch);
        };
    }

    private void ConfigureList()
    {
        _page.Configure(EntityType.Customer, AppModule.Customers);
        _page.SetHeader("سجل العملاء", "عملاء الجملة والموزعين", "\uE716", B("AccentCustomersBrush"));
        _page.SetPrimaryButton("إضافة عميل");
        _page.SetEmptyState("لا يوجد عملاء مسجلون", "إضافة عميل", "\uE716");
        _page.EnableServerSideSearch();
        _page.SetFilterExtras(_typeFilter, _statusFilter);
        _typeFilter.SelectionChanged += (_, _) => ApplyLocalFilters();
        _statusFilter.SelectionChanged += (_, _) => ApplyLocalFilters();

        _page.PrimaryActionRequested += (_, _) =>
        {
            CustomerNavigationContext.BeginCreate();
            MockInteractionService.Navigate(AppModule.Customers, "Form");
        };

        _page.SearchChanged += (_, term) =>
        {
            _pendingSearch = term;
            _searchTimer.Stop();
            _searchTimer.Start();
        };

        var g = _page.Grid;
        g.AutoGenerateColumns = false;
        g.IsReadOnly = true;
        g.CanUserAddRows = false;
        foreach (var (h, p, w) in new (string, string, object)[]
        {
            ("الكود", "Code", 80),
            ("الاسم", "NameAr", "*"),
            ("النوع", "TypeDisplay", 70),
            ("افتتاحي", "OpeningBalanceAmount", 95),
            ("مبيعات", "TotalInvoiced", 95),
            ("قبض", "TotalReceipts", 95),
            ("المتبقي", "ComputedBalance", 100),
            ("سندات قبض", "PostedReceiptCount", 85),
            ("حد الائتمان", "CreditLimitDisplay", 100),
            ("الحالة", "StatusDisplay", 75)
        })
        {
            AddCol(g, h, p, w, p is "OpeningBalanceAmount" or "TotalInvoiced" or "TotalReceipts" or "ComputedBalance" ? "N2" : null);
        }
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        _canCreate = await CustomerUiService.Instance.CanCreateAsync();
        _page.SetPrimaryButtonEnabled(_canCreate);
        await LoadCustomersAsync("");
    }

    private void OnRefreshRequested(object? sender, EventArgs e) =>
        _ = LoadCustomersAsync(_pendingSearch);

    private void OnDataChanged(ErpDataRefreshScope scope)
    {
        if ((scope & (ErpDataRefreshScope.Customers | ErpDataRefreshScope.All)) == 0 || !IsLoaded)
            return;
        _ = LoadCustomersAsync(_pendingSearch);
    }

    private async Task LoadCustomersAsync(string search)
    {
        using var perfScope = ScreenLoadProfiler.Begin("Customers.List");
        if (_isLoading || !AppServices.IsInitialized)
            return;

        _isLoading = true;
        _page.SetLoadingState(true);

        try
        {
            var result = await ScreenLoadProfiler.MeasureLoadAsync(perfScope, () => CustomerUiService.Instance.GetListAsync(search, _pageNumber, 100));
        perfScope?.IncrementServiceCalls();
            if (!ApplicationResultPresenter.Present(result))
                return;

            _totalCount = result.Value!.TotalCount;
            var rows = result.Value.Items.Select(CustomerListRow.FromDto).Cast<object>().ToList();
            _page.BindData(rows);
            _page.UpdateRecordCount(rows.Count, _totalCount);
            UpdateFinancialSummary(result.Value.Items);
            ApplyLocalFilters();
        }
        finally
        {
            _page.SetLoadingState(false);
            _isLoading = false;
        }
    }

    private void UpdateFinancialSummary(IReadOnlyList<Application.DTOs.Customers.CustomerListDto> items)
    {
        if (items.Count == 0)
        {
            _page.SetFilterSummary(null);
            return;
        }

        var totalOutstanding = items.Sum(i => i.ComputedBalance);
        var totalReceipts = items.Sum(i => i.TotalReceipts);
        _page.SetFilterSummary(
            $"إجمالي الذمة: {totalOutstanding:N2} $  •  قبض مرحّل: {totalReceipts:N2} $  •  {items.Count} عميل في الصفحة");
    }

    private void ApplyLocalFilters()
    {
        var type = (_typeFilter.SelectedItem as ComboBoxItem)?.Content?.ToString();
        var status = (_statusFilter.SelectedItem as ComboBoxItem)?.Content?.ToString();

        _page.SetExtraFilter(row =>
        {
            if (row is not CustomerListRow c)
                return false;

            if (type is "نقدي" && c.Type != DomainCustomerType.Cash)
                return false;
            if (type is "آجل" && c.Type != DomainCustomerType.Credit)
                return false;
            if (status is "نشط" && c.Status != DomainCustomerStatus.Active)
                return false;
            if (status is "موقوف" && c.Status != DomainCustomerStatus.Suspended)
                return false;
            if (status is "محظور" && c.Status != DomainCustomerStatus.Blocked)
                return false;
            return true;
        });
    }

    private static void AddCol(DataGrid g, string h, string p, object w, string? fmt) =>
        ErpUiFactory.AddGridColumn(g, h, p, w, fmt);

    private static SolidColorBrush B(string k) => (SolidColorBrush)System.Windows.Application.Current.Resources[k]!;
}
