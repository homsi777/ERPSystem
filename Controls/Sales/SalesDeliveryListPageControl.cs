using ERPSystem.Application.DTOs.Sales;
using ERPSystem.Controls;
using ERPSystem.Core;
using ERPSystem.Core.Sales;
using ERPSystem.Domain.Enums;
using ERPSystem.Helpers;
using ERPSystem.Services;
using ERPSystem.Services.Sales;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using ERPSystem.Diagnostics.Performance;

namespace ERPSystem.Controls.Sales;

public sealed class SalesDeliveryListPageControl : UserControl
{
    private readonly ErpListModuleControl _page = new();
    private readonly ComboBox _statusFilter = ErpUiFactory.FilterCombo(
        ["كل الحالات", "بانتظار التسليم", "مسلمة"], 150);
    private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromMilliseconds(300) };
    private string _search = "";

    public SalesDeliveryListPageControl()
    {
        Content = _page;
        Configure();
        Loaded += async (_, _) => await ReloadAsync();
        Unloaded += (_, _) => SalesListRefreshHub.RefreshRequested -= OnRefresh;
        SalesListRefreshHub.RefreshRequested += OnRefresh;
        _timer.Tick += async (_, _) => { _timer.Stop(); await ReloadAsync(); };
    }

    private void OnRefresh(object? sender, EventArgs e) => _ = ReloadAsync();

    private void Configure()
    {
        _page.Configure(Core.Actions.EntityType.SalesInvoice, AppModule.Sales);
        _page.SetHeader("تسليم البضاعة", "الفواتير المعتمدة الجاهزة للتسليم + الفواتير المُسلمة", "\uE7C1", B("AccentSalesBrush"));
        _page.SetEmptyState("لا توجد فواتير للتسليم", "افتح قائمة الفواتير", "\uE7C1");
        _page.EnableServerSideSearch();
        _page.SetFilterExtras(_statusFilter);
        _statusFilter.SelectionChanged += async (_, _) => await ReloadAsync();
        _page.SearchChanged += (_, term) => { _search = term; _timer.Stop(); _timer.Start(); };

        var g = _page.Grid;
        g.AutoGenerateColumns = false;
        g.IsReadOnly = true;
        g.CanUserAddRows = false;
        AddCol(g, "رقم الفاتورة", "InvoiceNumber", 130);
        AddCol(g, "العميل", "CustomerName", "*");
        AddCol(g, "تاريخ الاعتماد", "ApprovedAtDisplay", 140);
        AddCol(g, "تاريخ التسليم", "DeliveredAtDisplay", 140);
        AddCol(g, "الإجمالي", "Amount", 110, "N2");
        AddCol(g, "الحالة", "StatusDisplay", 110);

        g.MouseDoubleClick += (_, _) =>
        {
            if (g.SelectedItem is DeliveryQueueRow row)
            {
                var listRow = new SalesInvoiceListRow
                {
                    Id = row.Id,
                    InvoiceNumber = row.InvoiceNumber,
                    CustomerName = row.CustomerName,
                    Amount = row.Amount,
                    Status = row.Status,
                    Date = row.InvoiceDate
                };
                if (row.Status is SalesInvoiceStatus.Approved or SalesInvoiceStatus.Printed)
                    SalesPopupService.ShowDelivery(listRow);
                else
                    SalesPopupService.ShowOperationsCenter(listRow);
            }
        };
    }

    private async Task ReloadAsync()
    {
        using var perfScope = ScreenLoadProfiler.Begin("Sales.Delivery");
        if (!AppServices.IsInitialized) return;
        _page.SetLoadingState(true);
        try
        {
            var includeDelivered = (_statusFilter.SelectedItem as ComboBoxItem)?.Content?.ToString() != "بانتظار التسليم";
            var result = await ScreenLoadProfiler.MeasureLoadAsync(perfScope, () => SalesUiService.Instance.GetDeliveryQueueAsync(includeDelivered));
        perfScope?.IncrementServiceCalls();
            if (!ApplicationResultPresenter.Present(result) || result.Value is null) return;

            var rows = result.Value
                .Where(i => string.IsNullOrWhiteSpace(_search)
                    || i.InvoiceNumber.Contains(_search, StringComparison.OrdinalIgnoreCase)
                    || i.CustomerName.Contains(_search, StringComparison.OrdinalIgnoreCase))
                .Where(i => (_statusFilter.SelectedItem as ComboBoxItem)?.Content?.ToString() switch
                {
                    "بانتظار التسليم" => i.Status is SalesInvoiceStatus.Approved or SalesInvoiceStatus.Printed,
                    "مسلمة" => i.Status == SalesInvoiceStatus.Delivered,
                    _ => true
                })
                .Select(i => new DeliveryQueueRow
                {
                    Id = i.Id,
                    InvoiceNumber = i.InvoiceNumber,
                    CustomerName = i.CustomerName,
                    InvoiceDate = i.InvoiceDate,
                    Amount = i.GrandTotal,
                    Status = i.Status,
                    ApprovedAt = i.ApprovedAt,
                    DeliveredAt = i.DeliveredAt
                })
                .Cast<object>()
                .ToList();

            _page.BindData(rows);
            _page.UpdateRecordCount(rows.Count, rows.Count);
        }
        finally
        {
            _page.SetLoadingState(false);
        }
    }

    private static void AddCol(DataGrid g, string h, string p, object w, string? fmt = null) =>
        ErpUiFactory.AddGridColumn(g, h, p, w, fmt);

    private static SolidColorBrush B(string k) => (SolidColorBrush)System.Windows.Application.Current.Resources[k]!;

    public sealed class DeliveryQueueRow
    {
        public Guid Id { get; init; }
        public string InvoiceNumber { get; init; } = "";
        public string CustomerName { get; init; } = "";
        public DateTime InvoiceDate { get; init; }
        public decimal Amount { get; init; }
        public SalesInvoiceStatus Status { get; init; }
        public DateTime? ApprovedAt { get; init; }
        public DateTime? DeliveredAt { get; init; }
        public string ApprovedAtDisplay => ApprovedAt?.ToString("yyyy/MM/dd") ?? "—";
        public string DeliveredAtDisplay => DeliveredAt?.ToString("yyyy/MM/dd") ?? "—";
        public string StatusDisplay => Status switch
        {
            SalesInvoiceStatus.Approved => "معتمدة (بانتظار التسليم)",
            SalesInvoiceStatus.Printed => "مطبوعة (بانتظار التسليم)",
            SalesInvoiceStatus.Delivered => "مُسلمة",
            _ => Status.ToString()
        };
    }
}
