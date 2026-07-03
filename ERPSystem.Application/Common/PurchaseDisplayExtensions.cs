using ERPSystem.Domain.Enums;

namespace ERPSystem.Application.Common;

public static class PurchaseDisplayExtensions
{
    public static string ToStatusDisplay(this PurchaseInvoiceStatus status) => status switch
    {
        PurchaseInvoiceStatus.Draft => "مسودة",
        PurchaseInvoiceStatus.Approved => "معتمدة",
        PurchaseInvoiceStatus.Posted => "مرحّلة",
        PurchaseInvoiceStatus.PartiallyPaid => "مدفوعة جزئياً",
        PurchaseInvoiceStatus.Paid => "مدفوعة",
        PurchaseInvoiceStatus.Cancelled => "ملغاة",
        _ => status.ToString()
    };

    public static string ToStatusDisplay(this PurchaseOrderStatus status) => status switch
    {
        PurchaseOrderStatus.Draft => "مسودة",
        PurchaseOrderStatus.Sent => "مُرسل",
        PurchaseOrderStatus.Received => "مُستلم",
        PurchaseOrderStatus.Cancelled => "ملغى",
        _ => status.ToString()
    };

    public static string ToStatusDisplay(this PurchaseReturnStatus status) => status switch
    {
        PurchaseReturnStatus.Draft => "مسودة",
        PurchaseReturnStatus.Posted => "مرحّل",
        PurchaseReturnStatus.Cancelled => "ملغى",
        _ => status.ToString()
    };
}
