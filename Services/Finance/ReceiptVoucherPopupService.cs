using ERPSystem.Controls.Accounting;
using ERPSystem.Dialogs;

namespace ERPSystem.Services.Finance;

/// <summary>
/// Opens customer receipt entry as a true modal workflow above the current workspace.
/// </summary>
public static class ReceiptVoucherPopupService
{
    public static bool ShowForCustomer(Guid customerId, string customerName)
    {
        ReceiptVoucherNavigationContext.PreselectCustomerId = customerId;

        var form = new ReceiptVoucherPageControl(dialogMode: true);
        var host = new ErpModalWindow();
        host.Configure(
            "سند قبض",
            string.IsNullOrWhiteSpace(customerName)
                ? "إنشاء سند قبض للعميل"
                : $"إنشاء سند قبض - {customerName}",
            "\uE7BF",
            width: 820,
            maxHeight: 820);
        host.SetBody(form);

        form.DialogCompleted += (_, _) =>
        {
            host.DialogResult = true;
            host.Close();
        };

        try
        {
            return host.ShowDialog() == true;
        }
        finally
        {
            ReceiptVoucherNavigationContext.PreselectCustomerId = null;
        }
    }
}
