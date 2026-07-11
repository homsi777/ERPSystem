using ERPSystem.Controls.Purchases;
using ERPSystem.Core;
using ERPSystem.Helpers;
using ERPSystem.Services;
using ERPSystem.Diagnostics.Performance;
using ERPSystem.Services.Purchases;
using System.Windows;
using System.Windows.Controls;

namespace ERPSystem.Controls.Purchases;

public sealed class PurchaseOrderListPageControl : UserControl
{
    public PurchaseOrderListPageControl()
    {
        var page = new ErpListModuleControl();
        page.SetHeader("أوامر الشراء", "طلبات الشراء للموردين", "\uE8A5", (System.Windows.Media.Brush)System.Windows.Application.Current.Resources["AccentOrdersBrush"]!);
        page.SetPrimaryButton("أمر شراء جديد");
        page.SetEmptyState("لا توجد أوامر شراء", "أمر شراء جديد", "\uE8A5");
        page.PrimaryActionRequested += (_, _) =>
        {
            PurchaseNavigationContext.BeginOrderCreate();
            MockInteractionService.Navigate(AppModule.Purchases, "OrderForm");
        };
        Loaded += async (_, _) =>
        {
            using var perfScope = ScreenLoadProfiler.Begin("Purchases.Orders");
            var result = await ScreenLoadProfiler.MeasureLoadAsync(perfScope, () => PurchaseUiService.Instance.GetOrderListAsync());
            perfScope?.IncrementServiceCalls();
            if (ApplicationResultPresenter.Present(result))
                page.BindData(result.Value!.Cast<object>().ToList());
        };
        Content = page;
    }
}

public sealed class PurchaseReturnListPageControl : UserControl
{
    public PurchaseReturnListPageControl()
    {
        var page = new ErpListModuleControl();
        page.SetHeader("مرتجعات الشراء", "إشعارات دائنة للموردين", "\uE7A6", (System.Windows.Media.Brush)System.Windows.Application.Current.Resources["AccentOrdersBrush"]!);
        page.SetPrimaryButton("مرتجع جديد");
        page.SetEmptyState("لا توجد مرتجعات شراء", "مرتجع جديد", "\uE7A6");
        page.PrimaryActionRequested += (_, _) =>
        {
            PurchaseNavigationContext.BeginReturnCreate();
            MockInteractionService.Navigate(AppModule.Purchases, "ReturnForm");
        };
        Loaded += async (_, _) =>
        {
            using var perfScope = ScreenLoadProfiler.Begin("Purchases.Returns");
            var result = await ScreenLoadProfiler.MeasureLoadAsync(perfScope, () => PurchaseUiService.Instance.GetReturnListAsync());
            perfScope?.IncrementServiceCalls();
            if (ApplicationResultPresenter.Present(result))
                page.BindData(result.Value!.Cast<object>().ToList());
        };
        Content = page;
    }
}
