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
        Unloaded += (_, _) => CustomerListRefreshHub.RefreshRequested -= OnRefreshRequested;
        CustomerListRefreshHub.RefreshRequested += OnRefreshRequested;
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
            ("الكود", "Code", 90),
            ("الاسم", "NameAr", "*"),
            ("النوع", "TypeDisplay", 80),
            ("الرصيد", "Balance", 100),
            ("حد الائتمان", "CreditLimitDisplay", 110),
            ("الحالة", "StatusDisplay", 80)
        })
        {
            AddCol(g, h, p, w, p is "Balance" ? "N2" : null);
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

    private async Task LoadCustomersAsync(string search)
    {
        if (_isLoading || !AppServices.IsInitialized)
            return;

        _isLoading = true;
        _page.SetLoadingState(true);

        try
        {
            var result = await CustomerUiService.Instance.GetListAsync(search, _pageNumber, 100);
            if (!ApplicationResultPresenter.Present(result))
                return;

            _totalCount = result.Value!.TotalCount;
            var rows = result.Value.Items.Select(CustomerListRow.FromDto).Cast<object>().ToList();
            _page.BindData(rows);
            _page.UpdateRecordCount(rows.Count, _totalCount);
            ApplyLocalFilters();
        }
        finally
        {
            _page.SetLoadingState(false);
            _isLoading = false;
        }
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
