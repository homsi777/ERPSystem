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

    public static class SupplierSampleData
    {
        public static List<SupplierModel> Generate(int count = 20)
        {
            var rnd = new Random(13);
            var china = new[] { "مورد قوانغتشو", "مورد شنتشن", "مورد هانغتشو", "مورد نينغبو", "مورد شنغهاي" };
            var local = new[] { "مورد محلي الرياض", "مورد جدة", "مورد الدمام", "مورد مكة" };

            return Enumerable.Range(1, count).Select(i =>
            {
                var isChina = i <= 12;
                var names = isChina ? china : local;
                return new SupplierModel
                {
                    Code = $"SUP-{i:D3}",
                    Name = names[rnd.Next(names.Length)] + (i > 5 ? $" {i}" : ""),
                    Type = isChina ? SupplierType.China : SupplierType.Local,
                    Phone = isChina ? "+86 138 0000 0000" : $"01{rnd.Next(1000000, 9999999)}",
                    Country = isChina ? "الصين" : "السعودية",
                    Balance = rnd.Next(0, 200000),
                    InvoiceCount = rnd.Next(2, 45),
                    LastInvoiceDate = DateTime.Today.AddDays(-rnd.Next(1, 60)),
                    IsActive = rnd.Next(10) != 0
                };
            }).ToList();
        }
    }
}
