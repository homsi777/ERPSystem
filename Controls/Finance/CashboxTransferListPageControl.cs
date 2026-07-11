using ERPSystem.Application.DTOs.Finance;
using ERPSystem.Core;
using ERPSystem.Core.Actions;
using ERPSystem.Helpers;
using ERPSystem.Services;
using ERPSystem.Services.Finance;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ERPSystem.Diagnostics.Performance;

namespace ERPSystem.Controls.Finance;

public sealed class CashboxTransferListPageControl : UserControl
{
    private readonly ErpListModuleControl _page = new();
    private bool _isLoading;

    public CashboxTransferListPageControl()
    {
        _page.Configure(EntityType.Cashbox, AppModule.Accounting);
        _page.SetHeader("تحويلات الصناديق", "سجل التحويلات بين الصناديق النقدية", "\uE8AB", B("PrimaryBrush"));
        _page.SetPrimaryButton("تحويل جديد");
        _page.SetEmptyState("لا توجد تحويلات", "تحويل جديد", "\uE8AB");
        _page.PrimaryActionRequested += (_, _) => CashboxPopupService.ShowTransfer();

        var g = _page.Grid;
        g.AutoGenerateColumns = false;
        g.IsReadOnly = true;
        foreach (var (h, p, w) in new (string, string, object)[]
        {
            ("الرقم", nameof(CashboxTransferListDto.TransferNumber), 110),
            ("من", nameof(CashboxTransferListDto.FromCashboxName), "*"),
            ("إلى", nameof(CashboxTransferListDto.ToCashboxName), "*"),
            ("التاريخ", nameof(CashboxTransferListDto.TransferDate), 110),
            ("المبلغ", nameof(CashboxTransferListDto.Amount), 110),
            ("العملة", nameof(CashboxTransferListDto.Currency), 70),
            ("الحالة", nameof(CashboxTransferListDto.StatusDisplay), 90)
        })
            ErpUiFactory.AddGridColumn(g, h, p, w, null);

        Content = _page;
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
        CashboxListRefreshHub.RefreshRequested += OnRefreshRequested;
    }

    private void OnUnloaded(object sender, RoutedEventArgs e) =>
        CashboxListRefreshHub.RefreshRequested -= OnRefreshRequested;

    private void OnRefreshRequested(object? sender, EventArgs e) => _ = LoadAsync();

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        Loaded -= OnLoaded;
        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        using var perfScope = ScreenLoadProfiler.Begin("Finance.Transfers");
        if (_isLoading || !AppServices.IsInitialized) { _page.BindData([]); return; }
        _isLoading = true;
        _page.SetLoadingState(true);
        try
        {
            var result = await ScreenLoadProfiler.MeasureLoadAsync(perfScope, () => FinanceUiService.Instance.GetCashboxTransfersAsync());
        perfScope?.IncrementServiceCalls();
            if (!result.IsSuccess || result.Value is null)
            {
                _page.BindData([]);
                return;
            }

            var rows = result.Value
                .Select(t => (object)new CashboxTransferRow(t))
                .ToList();
            _page.BindData(rows);
        }
        finally
        {
            _page.SetLoadingState(false);
            _isLoading = false;
        }
    }

    private static SolidColorBrush B(string k) => (SolidColorBrush)System.Windows.Application.Current.Resources[k]!;

    private sealed class CashboxTransferRow(CashboxTransferListDto dto)
    {
        public string TransferNumber => dto.TransferNumber;
        public string FromCashboxName => dto.FromCashboxName;
        public string ToCashboxName => dto.ToCashboxName;
        public string TransferDate => dto.TransferDate.ToString("yyyy/MM/dd");
        public string Amount => $"{dto.Amount:N2}";
        public string Currency => dto.Currency;
        public string StatusDisplay => dto.StatusDisplay;
    }
}
