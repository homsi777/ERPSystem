namespace ERPSystem.Infrastructure.Persistence.Models.Purchasing;

public class PurchaseInvoiceEntity : CancellablePersistenceEntity
{
    public Guid CompanyId { get; set; }
    public string InvoiceNumber { get; set; } = "";
    public Guid SupplierId { get; set; }
    public DateTime InvoiceDate { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal Remaining { get; set; }
    public int Status { get; set; }
}

public class PurchaseInvoiceItemEntity : PersistenceEntity
{
    public Guid PurchaseInvoiceId { get; set; }
    public Guid FabricItemId { get; set; }
    public decimal QuantityMeters { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }
}
