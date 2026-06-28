using ERPSystem.Controls;
using ERPSystem.Core;
using ERPSystem.Core.Sales;
using ERPSystem.Domain.Enums;
using ERPSystem.Helpers;
using ERPSystem.Application.Queries.Containers;
using ERPSystem.Application.UseCases.Queries;
using ERPSystem.Infrastructure.Seed;
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
        ["كل الحالات", "مسودة", "بانتظار التفصيل", "مفصلة", "معتمدة", "ملغاة"], 140);
    private readonly DispatcherTimer _searchTimer = new() { Interval = TimeSpan.FromMilliseconds(350) };
    private string _pendingSearch = "";
    private int _totalCount;
    private bool _isLoading;
    private Dictionary<Guid, string> _warehouseNames = [];
    private Dictionary<Guid, string> _containerNames = [];

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

            SalesNavigationContext.BeginEdit(row.Id);
            MockInteractionService.Navigate(AppModule.Sales, "NewInvoice");
        };
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        var canCreate = await SalesUiService.Instance.CanCreateAsync();
        _page.SetPrimaryButtonEnabled(canCreate);
        await LoadLookupsAsync();
        await LoadInvoicesAsync("");
    }

    private void OnRefreshRequested(object? sender, EventArgs e) =>
        _ = LoadInvoicesAsync(_pendingSearch);

    private async Task LoadLookupsAsync()
    {
        if (!AppServices.IsInitialized)
            return;

        var warehouseResult = await SalesUiService.Instance.GetWarehousesAsync();
        if (warehouseResult.IsSuccess && warehouseResult.Value is not null)
        {
            _warehouseNames = warehouseResult.Value.ToDictionary(w => w.Id, w => w.NameAr);
        }

        try
        {
            using var scope = AppServices.CreateScope();
            var handler = scope.ServiceProvider.GetRequiredService<GetChinaContainerListHandler>();
            var result = await handler.HandleAsync(new GetChinaContainerListQuery
            {
                CompanyId = DatabaseSeeder.DefaultCompanyId,
                BranchId = DatabaseSeeder.DefaultBranchId,
                Page = 1,
                PageSize = 500
            });

            if (result.IsSuccess && result.Value?.Items is not null)
            {
                _containerNames = result.Value.Items.ToDictionary(
                    c => c.Id,
                    c => c.ContainerNumber);
            }
        }
        catch
        {
            // Container labels are optional for the list.
        }
    }

    private async Task LoadInvoicesAsync(string search)
    {
        if (_isLoading || !AppServices.IsInitialized)
            return;

        _isLoading = true;
        _page.SetLoadingState(true);

        try
        {
            var status = MapStatusFilter((_statusFilter.SelectedItem as ComboBoxItem)?.Content?.ToString());
            var result = await SalesUiService.Instance.GetListAsync(search, status, 1, 100);
            if (!ApplicationResultPresenter.Present(result))
                return;

            _totalCount = result.Value!.TotalCount;
            var rows = result.Value.Items
                .Select(dto => SalesInvoiceListRow.FromDto(
                    dto,
                    _warehouseNames.GetValueOrDefault(dto.WarehouseId, "—"),
                    _containerNames.GetValueOrDefault(dto.ChinaContainerId, "—")))
                .Cast<object>()
                .ToList();

            _page.BindData(rows);
            _page.UpdateRecordCount(rows.Count, _totalCount);
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
        "معتمدة" => SalesInvoiceStatus.Approved,
        "ملغاة" => SalesInvoiceStatus.Cancelled,
        _ => null
    };

    private static void AddCol(DataGrid g, string h, string p, object w, string? fmt) =>
        ErpUiFactory.AddGridColumn(g, h, p, w, fmt);

    private static SolidColorBrush B(string k) => (SolidColorBrush)System.Windows.Application.Current.Resources[k]!;
}
