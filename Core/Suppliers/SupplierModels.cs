namespace ERPSystem.Core.Suppliers
{
    public enum SupplierType { China, Local }

    public class SupplierModel
    {
        public string Code { get; set; } = "";
        public string Name { get; set; } = "";
        public SupplierType Type { get; set; }
        public string Phone { get; set; } = "";
        public string Country { get; set; } = "";
        public decimal Balance { get; set; }
        public int InvoiceCount { get; set; }
        public DateTime? LastInvoiceDate { get; set; }
        public bool IsActive { get; set; } = true;

        public string TypeDisplay => Type == SupplierType.China ? "صيني" : "محلي";
        public string StatusDisplay => IsActive ? "نشط" : "معطل";
    }
}
