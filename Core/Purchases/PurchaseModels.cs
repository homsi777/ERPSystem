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

    public static class PurchaseSampleData
    {
        public static List<PurchaseInvoiceModel> Generate(int count = 30)
        {
            var rnd = new Random(21);
            var suppliers = new[] { "مورد قوانغتشو", "مورد شنتشن", "مورد محلي الرياض", "مورد جدة" };

            return Enumerable.Range(1, count).Select(i => new PurchaseInvoiceModel
            {
                InvoiceNumber = $"PUR-2026-{i:D4}",
                SupplierName = suppliers[rnd.Next(suppliers.Length)],
                InvoiceDate = DateTime.Today.AddDays(-rnd.Next(1, 90)),
                TotalAmount = rnd.Next(5000, 250000),
                PaidAmount = rnd.Next(0, 2) == 0 ? 0 : rnd.Next(1000, 200000),
                Status = (PurchaseStatus)(rnd.Next(4)),
                LineCount = rnd.Next(2, 15),
                Warehouse = "المستودع الرئيسي"
            }).ToList();
        }
    }
}
