using ERPSystem.Controls;
using ERPSystem.Core;
using ERPSystem.Core.Actions;
using ERPSystem.Core.Suppliers;
using ERPSystem.Helpers;
using ERPSystem.Services;
using ERPSystem.Services.Suppliers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using ERPSystem.Diagnostics.Performance;

namespace ERPSystem.Controls.Suppliers;

public sealed class SupplierListPageControl : UserControl
{
    private readonly ErpListModuleControl _page = new();
    private readonly ComboBox _countryFilter = ErpUiFactory.FilterCombo(["الكل", "الصين", "السعودية", "تركيا"]);
    private readonly ComboBox _termsFilter = ErpUiFactory.FilterCombo(["الكل", "فوري", "Net 15", "Net 30", "Net 60", "Net 90"]);
    private readonly ComboBox _balanceFilter = ErpUiFactory.FilterCombo(["الكل", "له رصيد", "بدون رصيد"]);
    private readonly DispatcherTimer _searchTimer = new() { Interval = TimeSpan.FromMilliseconds(350) };
    private string _pendingSearch = "";
    private bool _isLoading;

    public SupplierListPageControl()
    {
        Content = _page;
        ConfigureList();
        Loaded += OnLoaded;
        Unloaded += (_, _) => SupplierListRefreshHub.RefreshRequested -= OnRefreshRequested;
        SupplierListRefreshHub.RefreshRequested += OnRefreshRequested;
        _searchTimer.Tick += async (_, _) =>
        {
            _searchTimer.Stop();
            await LoadAsync(_pendingSearch);
        };
    }

    private void ConfigureList()
    {
        _page.Configure(EntityType.Supplier, AppModule.Suppliers);
        _page.SetHeader("سجل الموردين", "موردو الصين والموردون المحليون", "\uE779", B("AccentPayableBrush"));
        _page.SetPrimaryButton("إضافة مورد");
        _page.SetEmptyState("لا يوجد موردون مضافون بعد", "إضافة مورد", "\uE779");
        _page.EnableServerSideSearch();
        _page.SetFilterExtras(_countryFilter, _termsFilter, _balanceFilter);
        _countryFilter.SelectionChanged += (_, _) => _ = LoadAsync(_pendingSearch);
        _termsFilter.SelectionChanged += (_, _) => _ = LoadAsync(_pendingSearch);
        _balanceFilter.SelectionChanged += (_, _) => _ = LoadAsync(_pendingSearch);

        _page.PrimaryActionRequested += (_, _) =>
        {
            SupplierNavigationContext.BeginCreate();
            MockInteractionService.Navigate(AppModule.Suppliers, "Form");
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
        foreach (var (h, p, w, fmt) in new (string, string, object, string?)[]
        {
            ("الكود", nameof(SupplierListRow.Code), 90, null),
            ("الاسم", nameof(SupplierListRow.NameAr), "*", null),
            ("الدولة", nameof(SupplierListRow.Country), 90, null),
            ("الرصيد", nameof(SupplierListRow.Balance), 100, "N2"),
            ("شروط السداد", nameof(SupplierListRow.PaymentTermsDisplay), 90, null),
            ("الحالة", nameof(SupplierListRow.StatusDisplay), 80, null)
        })
            ErpUiFactory.AddGridColumn(g, h, p, w, fmt);

        g.MouseDoubleClick += (_, _) =>
        {
            if (g.SelectedItem is SupplierListRow row)
                SupplierActionRouter.OpenOperationsCenter(row);
        };
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        var canCreate = await SupplierUiService.Instance.CanCreateAsync();
        _page.SetPrimaryButtonEnabled(canCreate);
        await LoadAsync("");
    }

    private void OnRefreshRequested(object? sender, EventArgs e) => _ = LoadAsync(_pendingSearch);

    private async Task LoadAsync(string search)
    {
        using var perfScope = ScreenLoadProfiler.Begin("Suppliers.List");
        if (_isLoading || !AppServices.IsInitialized)
            return;

        _isLoading = true;
        _page.SetLoadingState(true);
        try
        {
            var country = (_countryFilter.SelectedItem as ComboBoxItem)?.Content?.ToString();
            if (country == "الكل") country = null;

            int? terms = (_termsFilter.SelectedItem as ComboBoxItem)?.Content?.ToString() switch
            {
                "فوري" => 0,
                "Net 15" => 15,
                "Net 30" => 30,
                "Net 60" => 60,
                "Net 90" => 90,
                _ => null
            };

            bool? hasBalance = (_balanceFilter.SelectedItem as ComboBoxItem)?.Content?.ToString() switch
            {
                "له رصيد" => true,
                "بدون رصيد" => false,
                _ => null
            };

            var result = await ScreenLoadProfiler.MeasureLoadAsync(perfScope, () => SupplierUiService.Instance.GetListAsync( search, country, terms, hasBalance, 1, 200));
        perfScope?.IncrementServiceCalls();
            if (!ApplicationResultPresenter.Present(result))
                return;

            var rows = result.Value!.Items.Select(SupplierListRow.FromDto).Cast<object>().ToList();
            _page.BindData(rows);
            _page.UpdateRecordCount(rows.Count, result.Value.TotalCount);
        }
        finally
        {
            _page.SetLoadingState(false);
            _isLoading = false;
        }
    }

    private static SolidColorBrush B(string k) => (SolidColorBrush)System.Windows.Application.Current.Resources[k]!;
}
