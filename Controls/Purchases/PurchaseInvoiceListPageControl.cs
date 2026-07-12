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
using ERPSystem.Diagnostics.Performance;

namespace ERPSystem.Controls.Purchases;

public sealed class PurchaseInvoiceListPageControl : UserControl
{
    private readonly ErpListModuleControl _page = new();
    private readonly ComboBox _statusFilter = ErpUiFactory.FilterCombo(
        ["الكل", "مسودة", "مرحّلة", "مدفوعة جزئياً", "مدفوعة", "ملغاة"]);
    private readonly Button _backfillButton = new()
    {
        Content = "ربط حاويات معتمدة",
        Margin = new Thickness(0, 0, 8, 0),
        Padding = new Thickness(12, 6, 12, 6)
    };
    private readonly DispatcherTimer _searchTimer = new() { Interval = TimeSpan.FromMilliseconds(350) };
    private string _pendingSearch = "";

    public PurchaseInvoiceListPageControl()
    {
        Content = _page;
        _page.Configure(EntityType.PurchaseInvoice, AppModule.Purchases);
        _page.SetHeader("فواتير الشراء", "مشتريات الأقمشة والموردين", "\uE9F9", B("AccentOrdersBrush"));
        _page.SetPrimaryButton("فاتورة شراء جديدة");
        _page.SetEmptyState(
            "لا توجد فواتير مشتريات. فواتير استيراد الصين تُنشأ تلقائياً عند اعتماد الحاوية — أو استخدم «ربط حاويات معتمدة» للحاويات السابقة.",
            "فاتورة شراء جديدة",
            "\uE9F9");
        _backfillButton.SetResourceReference(StyleProperty, "SecondaryButtonStyle");
        _backfillButton.Click += async (_, _) => await BackfillAsync();
        _page.SetFilterExtras(_statusFilter, _backfillButton);
        _page.EnableServerSideSearch();
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
            ("المصدر", nameof(PurchaseListRow.SourceDisplay), 120, null),
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
        Unloaded += OnUnloaded;
        PurchaseListRefreshHub.RefreshRequested += OnRefreshRequested;
    }

    private void OnUnloaded(object sender, RoutedEventArgs e) =>
        PurchaseListRefreshHub.RefreshRequested -= OnRefreshRequested;

    private void OnRefreshRequested(object? sender, EventArgs e) => _ = LoadAsync(_pendingSearch);

    private async Task BackfillAsync()
    {
        if (!AppServices.IsInitialized)
            return;

        if (!MockInteractionService.Confirm(
                "سيتم إنشاء فواتير شراء للحاويات المعتمدة التي لا تملك فاتورة مرتبطة.\n\n" +
                "الحاويات التي سبق ترحيل قيودها المحاسبية لن يُكرّر قيد GL لها.",
                "ربط حاويات معتمدة"))
            return;

        _page.SetLoadingState(true);
        try
        {
            var result = await PurchaseUiService.Instance.BackfillChinaContainerPurchaseInvoicesAsync();
            if (!ApplicationResultPresenter.Present(result) || result.Value is null)
                return;

            var summary = result.Value;
            var detail = summary.Messages.Count > 0
                ? string.Join("\n", summary.Messages.Take(8))
                : "—";
            MockInteractionService.ShowSuccess(
                $"تمت المعالجة: {summary.Processed}\n" +
                $"أُنشئت: {summary.Created}\n" +
                $"موجودة مسبقاً: {summary.SkippedExisting}\n" +
                $"تخطي (لا مبلغ): {summary.SkippedNoAmount}\n\n{detail}",
                "ربط حاويات معتمدة");
            PurchaseListRefreshHub.RequestRefresh();
            await LoadAsync(_pendingSearch);
        }
        finally
        {
            _page.SetLoadingState(false);
        }
    }

    private async Task LoadAsync(string search)
    {
        using var perfScope = ScreenLoadProfiler.Begin("Purchases.Invoices");
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
            var result = await ScreenLoadProfiler.MeasureLoadAsync(perfScope, () => PurchaseUiService.Instance.GetInvoiceListAsync(search, status));
        perfScope?.IncrementServiceCalls();
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
