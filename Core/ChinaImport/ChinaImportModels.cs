namespace ERPSystem.Core.ChinaImport
{
    public enum ContainerStatus
    {
        InTransit,
        Arrived,
        Customs,
        Distributed,
        Approved,
        Archived,
        Closed
    }

    public class ImportContainerModel
    {
        public string ContainerNumber { get; set; } = "";
        public string SupplierName { get; set; } = "";
        public string OrderNumber { get; set; } = "";
        public DateTime ShipmentDate { get; set; }
        public DateTime? ExpectedArrival { get; set; }
        public DateTime? ArrivalDate { get; set; }
        public ContainerStatus Status { get; set; }
        public int CodeCount { get; set; }
        public int ColorCount { get; set; }
        public int TotalRolls { get; set; }
        public decimal TotalMeters { get; set; }
        public decimal TotalWeightKg { get; set; }
        public decimal WastePercent { get; set; }
        public string LinkedCustomers { get; set; } = "";
        public DateTime LastUpdated { get; set; }
        public decimal ImportCost { get; set; }
        public string Port { get; set; } = "";
        public string Notes { get; set; } = "";

        public int FabricCount => CodeCount;

        public string StatusDisplay => Status switch
        {
            ContainerStatus.InTransit => "بالطريق",
            ContainerStatus.Arrived => "واصلة",
            ContainerStatus.Customs => "جمرك",
            ContainerStatus.Distributed => "موزعة",
            ContainerStatus.Approved => "معتمدة",
            ContainerStatus.Archived => "مؤرشفة",
            ContainerStatus.Closed => "مغلقة",
            _ => ""
        };
    }

    public class ContainerFabricLine
    {
        public int BoltNumber { get; set; }
        public string FabricCode { get; set; } = "";
        public string FabricType { get; set; } = "";
        public string Color { get; set; } = "";
        public decimal LengthMeters { get; set; }
        public decimal WeightKg { get; set; }
        public string Note { get; set; } = "";
        public bool IsValid { get; set; } = true;
        public string InvalidReason { get; set; } = "";
        public Guid? FabricItemId { get; set; }
        public Guid? FabricColorId { get; set; }
        public string RowStatusDisplay => IsValid ? "صحيح" : "خطأ";
        public string RowStatusDetail => IsValid ? "صحيح" : InvalidReason;
        public string FabricName => FabricType;
        public decimal Meters => LengthMeters;
        public int Rolls { get; set; } = 1;
        public string BuyerName { get; set; } = "";
    }

    public static class ChinaImportSampleData
    {
        private static readonly string[] Fabrics =
        [
            "قماش كولومبيا", "قماش تركي فاخر", "قماش صيني قطن", "قماش جاكار",
            "قماش مخمل", "قماش كريب", "قماش ساتان", "قماش لينن"
        ];

        private static readonly string[] Colors =
            ["أبيض", "أسود", "بيج", "كحلي", "زيتي", "وردي", "رمادي", "بني"];

        private static readonly string[] Suppliers =
            ["مورد قوانغتشو", "مورد شنتشن", "مورد هانغتشو", "مورد نينغبو"];

        public static List<ImportContainerModel> Generate(int count = 30)
        {
            var rnd = new Random(42);
            var list = new List<ImportContainerModel>();

            for (int i = 1; i <= count; i++)
            {
                var status = (ContainerStatus)(i % 7);
                var rolls = rnd.Next(80, 450);
                var meters = rnd.Next(8000, 45000);
                list.Add(new ImportContainerModel
                {
                    ContainerNumber = $"CN-2026-{i:D3}",
                    SupplierName = Suppliers[rnd.Next(Suppliers.Length)],
                    OrderNumber = $"PO-CN-{2026000 + i}",
                    ShipmentDate = DateTime.Today.AddDays(-rnd.Next(5, 90)),
                    ExpectedArrival = DateTime.Today.AddDays(rnd.Next(3, 30)),
                    ArrivalDate = status >= ContainerStatus.Arrived ? DateTime.Today.AddDays(-rnd.Next(1, 30)) : null,
                    Status = status,
                    CodeCount = rnd.Next(3, 12),
                    ColorCount = rnd.Next(2, 8),
                    TotalRolls = rolls,
                    TotalMeters = meters,
                    TotalWeightKg = rnd.Next(5000, 25000),
                    WastePercent = rnd.Next(1, 5),
                    LinkedCustomers = $"عميل {rnd.Next(1, 8)}, عميل {rnd.Next(9, 15)}",
                    LastUpdated = DateTime.Today.AddDays(-rnd.Next(0, 5)),
                    ImportCost = rnd.Next(50000, 350000),
                    Port = "جدة الإسلامية",
                    Notes = "شحنة أقمشة جملة"
                });
            }

            return list;
        }

        public static List<ContainerFabricLine> GetContainerLines(string containerNumber)
        {
            var rnd = new Random(containerNumber.GetHashCode());
            return Enumerable.Range(1, rnd.Next(5, 12)).Select(i => new ContainerFabricLine
            {
                BoltNumber = i,
                FabricCode = $"FAB-{rnd.Next(100, 999)}",
                FabricType = Fabrics[rnd.Next(Fabrics.Length)],
                Color = Colors[rnd.Next(Colors.Length)],
                LengthMeters = rnd.Next(40, 65),
                WeightKg = rnd.Next(18, 32),
                Note = "",
                IsValid = rnd.Next(10) != 0,
                InvalidReason = rnd.Next(10) == 0 ? "كود القماش غير موجود" : "",
                Rolls = 1,
                BuyerName = rnd.Next(3) == 0 ? "—" : $"عميل {rnd.Next(1, 20)}"
            }).ToList();
        }
    }
}
