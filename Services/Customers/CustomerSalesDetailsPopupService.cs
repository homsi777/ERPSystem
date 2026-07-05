using ERPSystem.Controls.Customers;
using ERPSystem.Core.Customers;
using ERPSystem.Dialogs;

namespace ERPSystem.Services.Customers;

public static class CustomerSalesDetailsPopupService
{
    public static void Show(Guid customerId, string customerName)
    {
        var control = new CustomerSalesDetailsControl(customerId, customerName);
        ErpModalWindow.Show(
            "تفاصيل بيع",
            $"الأقمشة المباعة للعميل — {customerName}",
            control,
            "\uE8F1",
            840,
            560);
    }

    public static void Show(CustomerListRow row) =>
        Show(row.Id, row.NameAr);
}
