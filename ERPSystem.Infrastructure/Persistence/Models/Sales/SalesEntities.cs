namespace ERPSystem.Infrastructure.Persistence.Models.Sales;

public class SalesInvoiceEntity : CancellablePersistenceEntity
{
    public Guid CompanyId { get; set; }
    public Guid BranchId { get; set; }
    public string InvoiceNumber { get; set; } = "";
    public Guid CustomerId { get; set; }
    public Guid WarehouseId { get; set; }
    public Guid ChinaContainerId { get; set; }
    public DateTime InvoiceDate { get; set; }
    public int PaymentType { get; set; }
    public decimal? PartialPaymentAmount { get; set; }
    public int Status { get; set; }
    public decimal SubTotal { get; set; }
    public decimal DiscountTotal { get; set; }
    public decimal TaxTotal { get; set; }
    public decimal GrandTotal { get; set; }
    public Guid? ApprovedByUserId { get; set; }
    public DateTime? SentToWarehouseAt { get; set; }
    public DateTime? DetailedAt { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public DateTime? PrintedAt { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public Guid? ReversedByJournalEntryId { get; set; }
}

public class SalesInvoiceItemEntity : PersistenceEntity
{
    public Guid SalesInvoiceId { get; set; }
    public int LineNumber { get; set; }
    public Guid FabricItemId { get; set; }
    public Guid FabricColorId { get; set; }
    public int RollCount { get; set; }
    public decimal UnitPrice { get; set; }
    public string Unit { get; set; } = "meter";
    public decimal LineTotal { get; set; }
}

public class SalesInvoiceRollDetailEntity : PersistenceEntity
{
    public Guid SalesInvoiceId { get; set; }
    public Guid SalesInvoiceItemId { get; set; }
    public int RollSequence { get; set; }
    public Guid? FabricRollId { get; set; }
    public decimal LengthMeters { get; set; }
    public Guid? EnteredByUserId { get; set; }
    public DateTime? EnteredAt { get; set; }
}

public class WarehouseDetailingSessionEntity : PersistenceEntity
{
    public Guid SalesInvoiceId { get; set; }
    public int Status { get; set; }
    public Guid? AssignedOfficerUserId { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? RejectionReason { get; set; }
}
