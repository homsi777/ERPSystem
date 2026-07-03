using ERPSystem.Controls.Purchases;
using ERPSystem.Core;
using ERPSystem.Helpers;
using ERPSystem.Views.Reports;
using System.Windows;
using System.Windows.Controls;

namespace ERPSystem.Views.Purchases;

public static class PurchasesViews
{
    public static UserControl Create(string key) => key switch
    {
        "Form" => Wrap(new PurchaseInvoiceFormControl()),
        "OrderForm" => Wrap(new PurchaseOrderFormControl()),
        "ReturnForm" => Wrap(new PurchaseReturnFormControl()),
        "Orders" => Wrap(new PurchaseOrderListPageControl()),
        "Returns" => Wrap(new PurchaseReturnListPageControl()),
        "Reports" => ModuleReportsViews.CreateHub(AppModule.Purchases),
        _ => Wrap(new PurchaseInvoiceListPageControl())
    };

    private static UserControl Wrap(UIElement c) => new() { Content = c };
}
