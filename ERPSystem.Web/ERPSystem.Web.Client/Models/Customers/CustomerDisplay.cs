namespace ERPSystem.Web.Client.Models.Customers;

public static class CustomerDisplay
{
    public static string TypeLabel(CustomerTypeModel type) => type switch
    {
        CustomerTypeModel.Credit => "آجل",
        _ => "نقدي"
    };

    public static string StatusLabel(CustomerStatusModel status) => status switch
    {
        CustomerStatusModel.Suspended => "موقوف",
        CustomerStatusModel.Blocked => "محظور",
        _ => "نشط"
    };

    public static string DocumentTypeLabel(DocumentTypeModel type) => type switch
    {
        DocumentTypeModel.SalesInvoice => "فاتورة بيع",
        DocumentTypeModel.SalesReturn => "مرتجع بيع",
        DocumentTypeModel.ReceiptVoucher => "سند قبض",
        DocumentTypeModel.PaymentVoucher => "سند صرف",
        DocumentTypeModel.CustomerOpeningBalance => "رصيد افتتاحي",
        _ => type.ToString()
    };
}
