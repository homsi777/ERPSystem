namespace ERPSystem.Core.Purchases
{
    public enum PurchaseStatus { Draft, Posted, Cancelled, Returned }

    public class PurchaseInvoiceModel
    {
        public string InvoiceNumber { get; set; } = "";
        public string SupplierName { get; set; } = "";
        public DateTime InvoiceDate { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal PaidAmount { get; set; }
        public PurchaseStatus Status { get; set; }
        public int LineCount { get; set; }
        public string Warehouse { get; set; } = "";

        public decimal Remaining => TotalAmount - PaidAmount;

        public string StatusDisplay => Status switch
        {
            PurchaseStatus.Draft => "مسودة",
            PurchaseStatus.Posted => "مرحّل",
            PurchaseStatus.Cancelled => "ملغي",
            PurchaseStatus.Returned => "مرتجع",
            _ => ""
        };
    }
}
