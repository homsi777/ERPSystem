namespace ERPSystem.Core.Domain
{
    public class FabricBolt
    {
        public int BoltNumber { get; set; }
        public string FabricCode { get; set; } = "";
        public string FabricType { get; set; } = "";
        public string Color { get; set; } = "";
        public decimal LengthMeters { get; set; }
        public decimal WeightKg { get; set; }
        public string Note { get; set; } = "";
        public string RowStatus { get; set; } = "صحيح";
    }

    public class Warehouse
    {
        public string Code { get; set; } = "";
        public string Name { get; set; } = "";
        public string City { get; set; } = "";
        public bool IsActive { get; set; } = true;
    }

    public class WarehouseStockRow
    {
        public string GoodsType { get; set; } = "";
        public string BoltCode { get; set; } = "";
        public string Color { get; set; } = "";
        public int RollCount { get; set; }
        public decimal TotalLength { get; set; }
        public string Unit { get; set; } = "متر";
        public string Lot { get; set; } = "";
        public string Location { get; set; } = "";
        public string Warehouse { get; set; } = "";
        public string Status { get; set; } = "متوفر";
    }

    public class StockMovement
    {
        public DateTime Date { get; set; }
        public string Type { get; set; } = "";
        public string Reference { get; set; } = "";
        public decimal Quantity { get; set; }
        public string Warehouse { get; set; } = "";
    }

    public class StockTransfer
    {
        public string Number { get; set; } = "";
        public string FromWarehouse { get; set; } = "";
        public string ToWarehouse { get; set; } = "";
        public int ItemCount { get; set; }
        public string Status { get; set; } = "";
        public DateTime Date { get; set; }
    }

    public class StocktakeSession
    {
        public string SessionNumber { get; set; } = "";
        public DateTime Date { get; set; }
        public string Warehouse { get; set; } = "";
        public string Responsible { get; set; } = "";
        public string Progress { get; set; } = "";
        public int VarianceCount { get; set; }
        public string Status { get; set; } = "";
    }

    public class SalesInvoicePieceLength
    {
        public int PieceNumber { get; set; }
        public decimal Length { get; set; }
        public string Unit { get; set; } = "متر";
    }

    public class ContainerBolt
    {
        public int BoltNumber { get; set; }
        public string FabricCode { get; set; } = "";
        public string FabricType { get; set; } = "";
        public string Color { get; set; } = "";
        public decimal LengthMeters { get; set; }
        public decimal WeightKg { get; set; }
        public string Note { get; set; } = "";
        public string RowStatus { get; set; } = "صحيح";
    }

    public class ContainerCustomerDistribution
    {
        public string CustomerName { get; set; } = "";
        public string FabricCode { get; set; } = "";
        public string Color { get; set; } = "";
        public int Rolls { get; set; }
        public decimal Meters { get; set; }
    }

    public class ImportExcelBatch
    {
        public string BatchNumber { get; set; } = "";
        public DateTime ImportDate { get; set; }
        public string ContainerNumber { get; set; } = "";
        public string FileType { get; set; } = "";
        public int ValidRows { get; set; }
        public int ErrorRows { get; set; }
    }

    public class ReceiptVoucher
    {
        public string Number { get; set; } = "";
        public DateTime Date { get; set; }
        public string PartyName { get; set; } = "";
        public decimal Amount { get; set; }
        public string Cashbox { get; set; } = "";
        public string Status { get; set; } = "مسودة";
    }

    public class PaymentVoucher
    {
        public string Number { get; set; } = "";
        public DateTime Date { get; set; }
        public string PartyName { get; set; } = "";
        public decimal Amount { get; set; }
        public string Cashbox { get; set; } = "";
        public string Status { get; set; } = "مسودة";
    }

    public class Cashbox
    {
        public string Code { get; set; } = "";
        public string Name { get; set; } = "";
        public decimal Balance { get; set; }
        public string Currency { get; set; } = "ر.س";
    }

    public class CashboxTransfer
    {
        public string Number { get; set; } = "";
        public string FromCashbox { get; set; } = "";
        public string ToCashbox { get; set; } = "";
        public decimal Amount { get; set; }
        public DateTime Date { get; set; }
        public string Status { get; set; } = "";
    }

    public class ErpUser
    {
        public string Username { get; set; } = "";
        public string FullName { get; set; } = "";
        public string Role { get; set; } = "";
        public bool IsActive { get; set; } = true;
    }

    public class ErpRole
    {
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public int PermissionCount { get; set; }
    }

    public class WarehouseEntity
    {
        public string Code { get; set; } = "";
        public string Name { get; set; } = "";
        public string City { get; set; } = "";
        public int RollCount { get; set; }
        public decimal TotalLength { get; set; }
        public decimal CapacityPercent { get; set; }
        public string Status { get; set; } = "نشط";
    }

    public class ContainerLandingCost
    {
        public decimal TotalLengthFromInvoice { get; set; }
        public decimal ContainerWeightKg { get; set; }
        public decimal ContainerWeightGrams => ContainerWeightKg * 1000;
        public decimal CustomsAmountPaid { get; set; }
        public decimal CustomsCostPerMeter => TotalLengthFromInvoice > 0 ? CustomsAmountPaid / TotalLengthFromInvoice : 0;
        public decimal AvgGramPerMeter => TotalLengthFromInvoice > 0 ? ContainerWeightGrams / TotalLengthFromInvoice : 0;
        public decimal Shipping { get; set; }
        public decimal Clearance { get; set; }
        public decimal OtherExpenses { get; set; }
        public decimal TotalImportExpenses => CustomsAmountPaid + Shipping + Clearance + OtherExpenses;
        public decimal ExpenseCostPerMeter => TotalLengthFromInvoice > 0 ? TotalImportExpenses / TotalLengthFromInvoice : 0;
    }
}
