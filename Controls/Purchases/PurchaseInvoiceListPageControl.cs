using ERPSystem.Controls;
using ERPSystem.Core;
using ERPSystem.Core.Actions;
using ERPSystem.Core.Purchases;
using ERPSystem.Domain.Enums;
using ERPSystem.Helpers;
using ERPSystem.Services;
using ERPSystem.Services.Purchases;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace ERPSystem.Controls.Purchases;

public sealed class PurchaseInvoiceListPageControl : UserControl
{
    private readonly ErpListModuleControl _page = new();
    private readonly ComboBox _statusFilter = ErpUiFactory.FilterCombo(
        ["الكل", "مسودة", "مرحّلة", "مدفوعة جزئياً", "مدفوعة", "ملغاة"]);
    private readonly DispatcherTimer _searchTimer = new() { Interval = TimeSpan.FromMilliseconds(350) };
    private string _pendingSearch = "";

    public PurchaseInvoiceListPageControl()
    {
        Content = _page;
        _page.Configure(EntityType.PurchaseInvoice, AppModule.Purchases);
        _page.SetHeader("فواتير الشراء", "مشتريات الأقمشة والموردين", "\uE9F9", B("AccentOrdersBrush"));
        _page.SetPrimaryButton("فاتورة شراء جديدة");
        _page.SetEmptyState("لا توجد فواتير مشتريات", "فاتورة شراء جديدة", "\uE9F9");
        _page.EnableServerSideSearch();
        _page.SetFilterExtras(_statusFilter);
        _statusFilter.SelectionChanged += (_, _) => _ = LoadAsync(_pendingSearch);
        _page.PrimaryActionRequested += (_, _) =>
        {
            PurchaseNavigationContext.BeginCreate();
            MockInteractionService.Navigate(AppModule.Purchases, "Form");
        };
        _page.SearchChanged += (_, term) =>
        {
            _pendingSearch = term;
            _searchTimer.Stop();
            _searchTimer.Start();
        };
        _searchTimer.Tick += async (_, _) =>
        {
            _searchTimer.Stop();
            await LoadAsync(_pendingSearch);
        };

        var g = _page.Grid;
        g.AutoGenerateColumns = false;
        g.IsReadOnly = true;
        g.LoadingRow += (_, e) =>
        {
            if (e.Row.Item is PurchaseListRow row && row.IsOverdue)
                e.Row.Background = new SolidColorBrush(Color.FromRgb(255, 243, 224));
        };
        foreach (var (h, p, w, fmt) in new (string, string, object, string?)[]
        {
            ("رقم الفاتورة", nameof(PurchaseListRow.InvoiceNumber), 110, null),
            ("التاريخ", nameof(PurchaseListRow.InvoiceDate), 100, "yyyy/MM/dd"),
            ("المورد", nameof(PurchaseListRow.SupplierName), "*", null),
            ("الإجمالي", nameof(PurchaseListRow.TotalAmount), 100, "N2"),
            ("المدفوع", nameof(PurchaseListRow.PaidAmount), 90, "N2"),
            ("المتبقي", nameof(PurchaseListRow.RemainingAmount), 90, "N2"),
            ("الاستحقاق", nameof(PurchaseListRow.DueDate), 100, "yyyy/MM/dd"),
            ("الحالة", nameof(PurchaseListRow.StatusDisplay), 100, null)
        })
            ErpUiFactory.AddGridColumn(g, h, p, w, fmt);

        g.MouseDoubleClick += (_, _) =>
        {
            if (g.SelectedItem is PurchaseListRow row)
                PurchaseActionRouter.OpenOperationsCenter(row);
        };

        Loaded += async (_, _) => await LoadAsync("");
        PurchaseListRefreshHub.RefreshRequested += (_, _) => _ = LoadAsync(_pendingSearch);
    }

    private async Task LoadAsync(string search)
    {
        if (!AppServices.IsInitialized) return;
        _page.SetLoadingState(true);
        try
        {
            PurchaseInvoiceStatus? status = (_statusFilter.SelectedItem as ComboBoxItem)?.Content?.ToString() switch
            {
                "مسودة" => PurchaseInvoiceStatus.Draft,
                "مرحّلة" => PurchaseInvoiceStatus.Posted,
                "مدفوعة جزئياً" => PurchaseInvoiceStatus.PartiallyPaid,
                "مدفوعة" => PurchaseInvoiceStatus.Paid,
                "ملغاة" => PurchaseInvoiceStatus.Cancelled,
                _ => null
            };
            var result = await PurchaseUiService.Instance.GetInvoiceListAsync(search, status);
            if (!ApplicationResultPresenter.Present(result)) return;
            var rows = result.Value!.Select(PurchaseListRow.FromDto).Cast<object>().ToList();
            _page.BindData(rows);
            _page.UpdateRecordCount(rows.Count);
        }
        finally
        {
            _page.SetLoadingState(false);
        }
    }

    private static SolidColorBrush B(string k) => (SolidColorBrush)System.Windows.Application.Current.Resources[k]!;
}
