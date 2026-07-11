using ERPSystem.Application.Common;
using ERPSystem.Application.DTOs.Sales;
using ERPSystem.Application.Results;
using ERPSystem.Controls;
using ERPSystem.Core;
using ERPSystem.Core.Sales;
using ERPSystem.Diagnostics.Performance;
using ERPSystem.Domain.Enums;
using ERPSystem.Helpers;
using ERPSystem.Services;
using ERPSystem.Services.Sales;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace ERPSystem.Controls.Sales;

public sealed class SalesInvoiceListPageControl : UserControl
{
    private readonly ErpListModuleControl _page = new();
    private readonly ComboBox _statusFilter = ErpUiFactory.FilterCombo(
        [
            "كل الحالات",
            "مسودة",
            "بانتظار التفصيل",
            "مفصلة",
            "جاهزة للاعتماد",
            "معتمدة",
            "مطبوعة",
            "مُسلَّمة",
            "مرتجع جزئي",
            "مرتجعة",
            "ملغاة"
        ], 160);
    private readonly DispatcherTimer _searchTimer = new() { Interval = TimeSpan.FromMilliseconds(350) };
    private string _pendingSearch = "";
    private int _totalCount;
    private bool _isLoading;
    private Guid? _filterCustomerId;
    private string? _filterCustomerName;

    public SalesInvoiceListPageControl()
    {
        Content = _page;
        ConfigureList();
        Loaded += OnLoaded;
        Unloaded += (_, _) => SalesListRefreshHub.RefreshRequested -= OnRefreshRequested;
        SalesListRefreshHub.RefreshRequested += OnRefreshRequested;
        _searchTimer.Tick += async (_, _) =>
        {
            _searchTimer.Stop();
            await LoadInvoicesAsync(_pendingSearch);
        };
    }

    /// <summary>
    /// Scope the list to a specific customer. Call BEFORE Loaded fires (e.g. from a factory).
    /// </summary>
    public void ScopeToCustomer(Guid customerId, string customerName)
    {
        _filterCustomerId = customerId;
        _filterCustomerName = customerName;
        _page.SetHeader(
            $"فواتير {customerName}",
            "قائمة فواتير هذا العميل فقط",
            "\uE8F1",
            B("AccentSalesBrush"));
        _page.SetEmptyState(
            "لا توجد فواتير لهذا العميل",
            "فاتورة بيع جديدة",
            "\uE9F9");
    }

    private void ConfigureList()
    {
        _page.Configure(Core.Actions.EntityType.SalesInvoice, AppModule.Sales);
        _page.SetHeader("فواتير البيع", "إدارة فواتير بيع الأقمشة — التفصيل بالمتر", "\uE8F1", B("AccentSalesBrush"));
        _page.SetPrimaryButton("فاتورة بيع جديدة");
        _page.SetEmptyState("لا توجد فواتير بيع", "فاتورة بيع جديدة", "\uE9F9");
        _page.EnableServerSideSearch();
        _page.SetFilterExtras(_statusFilter);
        _statusFilter.SelectionChanged += async (_, _) => await LoadInvoicesAsync(_pendingSearch);

        _page.PrimaryActionRequested += (_, _) =>
        {
            SalesNavigationContext.BeginCreate();
            MockInteractionService.Navigate(AppModule.Sales, "NewInvoice");
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
        g.RowStyle = ErpAccountingColorHelper.CreatePaymentTypeRowStyle();
        foreach (var (h, p, w) in new (string, string, object)[]
        {
            ("رقم الفاتورة", "InvoiceNumber", 120),
            ("العميل", "CustomerName", "*"),
            ("المستودع", "Warehouse", 110),
            ("الحاوية", "Container", 110),
            ("الأثواب", "RollCount", 70),
            ("المبلغ", "Amount", 100),
            ("الحالة", "StatusDisplay", 120),
            ("التاريخ", "Date", 100)
        })
        {
            AddCol(g, h, p, w, p is "Amount" ? "N2" : p is "Date" ? "yyyy/MM/dd" : null);
        }

        g.MouseDoubleClick += (_, _) =>
        {
            if (g.SelectedItem is not SalesInvoiceListRow row)
                return;
            SalesPopupService.ShowOperationsCenter(row);
        };
        // Right-click task menu is handled by RowContextMenuService → SalesContextMenuService.
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        var canCreate = await SalesUiService.Instance.CanCreateAsync();
        _page.SetPrimaryButtonEnabled(canCreate);
        await LoadInvoicesAsync("");
    }

    private void OnRefreshRequested(object? sender, EventArgs e) =>
        _ = LoadInvoicesAsync(_pendingSearch);

    private async Task LoadInvoicesAsync(string search)
    {
        if (_isLoading || !AppServices.IsInitialized)
            return;

        _isLoading = true;
        _page.SetLoadingState(true);

        using var perfScope = AppServices.IsInitialized
            ? ScreenLoadProfiler.Begin("Sales.InvoiceList")
            : null;

        try
        {
            var status = MapStatusFilter((_statusFilter.SelectedItem as ComboBoxItem)?.Content?.ToString());

            ApplicationResult<PagedResult<SalesInvoiceDto>> result;
            using (perfScope?.MeasureDataLoad())
            {
                result = await SalesUiService.Instance.GetListAsync(search, status, 1, 100, _filterCustomerId);
            }
            perfScope?.IncrementServiceCalls();

            if (!ApplicationResultPresenter.Present(result))
                return;

            _totalCount = result.Value!.TotalCount;
            List<object> rows;
            using (perfScope?.MeasureMapping())
            {
                rows = result.Value.Items
                    .Select(dto => SalesInvoiceListRow.FromDto(
                        dto,
                        dto.WarehouseName,
                        dto.ContainerNumber))
                    .Cast<object>()
                    .ToList();
            }

            using (perfScope?.MeasureItemsSourceAssignment())
            {
                _page.BindData(rows);
                _page.UpdateRecordCount(rows.Count, _totalCount);
            }

            perfScope?.SetRowsReturned(rows.Count);
        }
        finally
        {
            _page.SetLoadingState(false);
            _isLoading = false;
        }
    }

    private static SalesInvoiceStatus? MapStatusFilter(string? label) => label switch
    {
        "مسودة" => SalesInvoiceStatus.Draft,
        "بانتظار التفصيل" => SalesInvoiceStatus.AwaitingDetailing,
        "مفصلة" => SalesInvoiceStatus.Detailed,
        "جاهزة للاعتماد" => SalesInvoiceStatus.ReadyForApproval,
        "معتمدة" => SalesInvoiceStatus.Approved,
        "مطبوعة" => SalesInvoiceStatus.Printed,
        "مُسلَّمة" => SalesInvoiceStatus.Delivered,
        "مرتجع جزئي" => SalesInvoiceStatus.PartiallyReturned,
        "مرتجعة" => SalesInvoiceStatus.Returned,
        "ملغاة" => SalesInvoiceStatus.Cancelled,
        _ => null
    };

    private static void AddCol(DataGrid g, string h, string p, object w, string? fmt) =>
        ErpUiFactory.AddGridColumn(g, h, p, w, fmt);

    private static SolidColorBrush B(string k) => (SolidColorBrush)System.Windows.Application.Current.Resources[k]!;
}
