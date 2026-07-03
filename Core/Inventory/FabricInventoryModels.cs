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
}
