using ERPSystem.Application.DTOs.Sales;
using ERPSystem.Controls;
using ERPSystem.Core;
using ERPSystem.Domain.Enums;
using ERPSystem.Helpers;
using ERPSystem.Services;
using ERPSystem.Services.Sales;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Threading;

namespace ERPSystem.Controls.Sales;

public sealed class SalesReturnListPageControl : UserControl
{
    private readonly ErpListModuleControl _page = new();
    private readonly ComboBox _statusFilter = ErpUiFactory.FilterCombo(
        ["كل الحالات", "مسودة", "مُرحّل", "ملغى"], 130);
    private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromMilliseconds(300) };
    private string _search = "";
    private Guid? _filterOriginalInvoiceId;
    private string? _filterOriginalInvoiceNumber;

    public SalesReturnListPageControl()
    {
        Content = _page;
        Configure();
        Loaded += OnLoaded;
        Unloaded += (_, _) => SalesReturnListRefreshHub.RefreshRequested -= OnRefresh;
        SalesReturnListRefreshHub.RefreshRequested += OnRefresh;
        _timer.Tick += async (_, _) => { _timer.Stop(); await ReloadAsync(); };
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        var (invoiceId, invoiceNumber) = SalesNavigationContext.TakeReturnsFilter();
        _filterOriginalInvoiceId = invoiceId;
        _filterOriginalInvoiceNumber = invoiceNumber;
        ApplyHeader();
        await ReloadAsync();
    }

    private void ApplyHeader()
    {
        if (_filterOriginalInvoiceId.HasValue)
        {
            var label = string.IsNullOrWhiteSpace(_filterOriginalInvoiceNumber)
                ? "مرتجعات الفاتورة"
                : $"مرتجعات {_filterOriginalInvoiceNumber}";
            _page.SetHeader(label, "مرتجعات مرتبطة بهذه الفاتورة فقط", "\uE72C", B("AccentSalesBrush"));
        }
        else
        {
            _page.SetHeader("مرتجعات البيع", "قائمة مرتجعات فواتير البيع", "\uE72C", B("AccentSalesBrush"));
        }
    }

    private void OnRefresh(object? sender, EventArgs e) => _ = ReloadAsync();

    private void Configure()
    {
        _page.Configure(Core.Actions.EntityType.SalesInvoice, AppModule.Sales);
        ApplyHeader();
        _page.SetPrimaryButton("مرتجع بيع جديد");
        _page.SetEmptyState("لا توجد مرتجعات بيع", "مرتجع بيع جديد", "\uE72C");
        _page.EnableServerSideSearch();
        _page.SetFilterExtras(_statusFilter);
        _statusFilter.SelectionChanged += async (_, _) => await ReloadAsync();
        _page.SearchChanged += (_, term) => { _search = term; _timer.Stop(); _timer.Start(); };
        _page.PrimaryActionRequested += (_, _) =>
        {
            MockInteractionService.ShowInfo(
                "افتح فاتورة بيع معتمدة/مُسلَّمة من قائمة الفواتير، ثم اختر «مرتجع» من قائمة السياق.",
                "مرتجع بيع جديد");
            MockInteractionService.Navigate(AppModule.Sales, "");
        };

        var g = _page.Grid;
        g.AutoGenerateColumns = false;
        g.IsReadOnly = true;
        g.CanUserAddRows = false;
        AddCol(g, "رقم المرتجع", "ReturnNumber", 140);
        AddCol(g, "الفاتورة الأصلية", "OriginalInvoiceNumber", 140);
        AddCol(g, "العميل", "CustomerName", "*");
        AddCol(g, "التاريخ", "ReturnDate", 100, "yyyy/MM/dd");
        AddCol(g, "السبب", "ReasonDisplay", 130);
        AddCol(g, "الحالة", "StatusDisplay", 100);
        AddCol(g, "الإجمالي", "TotalAmount", 110, "N2");
    }

    private async Task ReloadAsync()
    {
        if (!AppServices.IsInitialized) return;
        _page.SetLoadingState(true);
        try
        {
            var statusLabel = (_statusFilter.SelectedItem as ComboBoxItem)?.Content?.ToString();
            VoucherStatus? status = statusLabel switch
            {
                "مسودة" => VoucherStatus.Draft,
                "مُرحّل" => VoucherStatus.Posted,
                "ملغى" => VoucherStatus.Cancelled,
                _ => null
            };

            var result = await SalesReturnUiService.Instance.GetListAsync(
                status,
                originalInvoiceId: _filterOriginalInvoiceId);
            if (!ApplicationResultPresenter.Present(result) || result.Value is null) return;

            var rows = result.Value
                .Where(r => string.IsNullOrWhiteSpace(_search)
                    || r.ReturnNumber.Contains(_search, StringComparison.OrdinalIgnoreCase)
                    || r.CustomerName.Contains(_search, StringComparison.OrdinalIgnoreCase))
                .Select(r => new SalesReturnListRow
                {
                    Id = r.Id,
                    ReturnNumber = r.ReturnNumber,
                    OriginalInvoiceId = r.OriginalInvoiceId,
                    OriginalInvoiceNumber = r.OriginalInvoiceNumber,
                    CustomerName = r.CustomerName,
                    ReturnDate = r.ReturnDate,
                    Reason = r.Reason,
                    Status = r.Status,
                    TotalAmount = r.TotalAmount
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

    public sealed class SalesReturnListRow
    {
        public Guid Id { get; init; }
        public string ReturnNumber { get; init; } = "";
        public Guid OriginalInvoiceId { get; init; }
        public string OriginalInvoiceNumber { get; init; } = "";
        public string CustomerName { get; init; } = "";
        public DateTime ReturnDate { get; init; }
        public SalesReturnReason Reason { get; init; }
        public VoucherStatus Status { get; init; }
        public decimal TotalAmount { get; init; }

        public string ReasonDisplay => Reason switch
        {
            SalesReturnReason.DefectiveGoods => "بضاعة معيبة",
            SalesReturnReason.WrongOrder => "خطأ في الطلب",
            SalesReturnReason.CustomerRequest => "طلب العميل",
            SalesReturnReason.Other => "أخرى",
            _ => Reason.ToString()
        };

        public string StatusDisplay => Status switch
        {
            VoucherStatus.Draft => "مسودة",
            VoucherStatus.Posted => "مُرحّل",
            VoucherStatus.Cancelled => "ملغى",
            _ => Status.ToString()
        };
    }
}
