namespace ERPSystem.Core.Inventory
{
    public enum FabricStatus { Available, LowStock, Reserved, OutOfStock }

    public class FabricItemModel
    {
        public string Code { get; set; } = "";
        public string FabricName { get; set; } = "";
        public string Color { get; set; } = "";
        public string Type { get; set; } = "";
        public int RollCount { get; set; }
        public decimal TotalMeters { get; set; }
        public string Warehouse { get; set; } = "";
        public decimal PricePerMeter { get; set; }
        public FabricStatus Status { get; set; }

        public string StatusDisplay => Status switch
        {
            FabricStatus.Available => "متوفر",
            FabricStatus.LowStock => "منخفض",
            FabricStatus.Reserved => "محجوز",
            FabricStatus.OutOfStock => "نفد",
            _ => ""
        };
    }

    public static class FabricInventorySampleData
    {
        private static readonly (string Name, string Color, string Type)[] Items =
        [
            ("قماش كولومبيا", "أبيض", "قطن"),
            ("قماش تركي فاخر", "بيج", "قطن"),
            ("قماش صيني قطن", "أسود", "قطن"),
            ("قماش جاكار", "كحلي", "بوليستر"),
            ("قماش مخمل", "زيتي", "مخمل"),
            ("قماش كريب", "وردي", "كريب"),
            ("قماش ساتان", "رمادي", "ساتان"),
            ("قماش لينن", "بني", "لينن"),
            ("قماش شيفون", "أبيض", "شيفون"),
            ("قماش دنيم", "أزرق", "دنيم"),
        ];

        public static List<FabricItemModel> Generate(int count = 40)
        {
            var rnd = new Random(7);
            var warehouses = new[] { "المستودع الرئيسي", "مستودع جدة", "مستودع الرياض" };
            var list = new List<FabricItemModel>();

            for (int i = 1; i <= count; i++)
            {
                var item = Items[rnd.Next(Items.Length)];
                var rolls = rnd.Next(2, 120);
                var meters = rolls * rnd.Next(40, 65);
                var status = meters < 200 ? FabricStatus.LowStock
                    : meters == 0 ? FabricStatus.OutOfStock
                    : rnd.Next(10) == 0 ? FabricStatus.Reserved
                    : FabricStatus.Available;

                list.Add(new FabricItemModel
                {
                    Code = $"FAB-{i:D4}",
                    FabricName = item.Name,
                    Color = item.Color,
                    Type = item.Type,
                    RollCount = rolls,
                    TotalMeters = meters,
                    Warehouse = warehouses[rnd.Next(warehouses.Length)],
                    PricePerMeter = rnd.Next(15, 85),
                    Status = status
                });
            }

            return list;
        }
    }
}
