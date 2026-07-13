namespace ERPSystem.Infrastructure.Persistence.Models.Finance;

public class OpeningBalanceDocumentEntity : PersistenceEntity
{
    public Guid CompanyId { get; set; }
    public Guid BranchId { get; set; }
    public string Number { get; set; } = "";
    public int Type { get; set; }
    public int Status { get; set; }
    public int Source { get; set; }
    public DateTime OpeningDate { get; set; }
    public string CurrencyCode { get; set; } = "USD";
    public decimal ExchangeRate { get; set; } = 1m;
    public string? Reference { get; set; }
    public string? Description { get; set; }
    public string? Notes { get; set; }
    public decimal TotalDebit { get; set; }
    public decimal TotalCredit { get; set; }
    public decimal TotalBaseAmount { get; set; }
    public string? JournalEntryNumber { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public Guid? ApprovedByUserId { get; set; }
    public string? ApprovalNotes { get; set; }
    public string? RejectionReason { get; set; }
    public DateTime? PostedAt { get; set; }
    public Guid? PostedByUserId { get; set; }
    public DateTime? LockedAt { get; set; }
    public DateTime? ArchivedAt { get; set; }

    public ICollection<OpeningBalanceLineEntity> Lines { get; set; } = [];
    public ICollection<OpeningBalanceEventEntity> Events { get; set; } = [];
}

public class OpeningBalanceLineEntity
{
    public Guid Id { get; set; }
    public Guid DocumentId { get; set; }
    public int LineNumber { get; set; }
    public Guid? PartyId { get; set; }
    public string? PartyName { get; set; }
    public Guid? AccountId { get; set; }
    public string? AccountName { get; set; }
    public Guid? WarehouseId { get; set; }
    public string? WarehouseName { get; set; }
    public Guid? FabricItemId { get; set; }
    public Guid? FabricColorId { get; set; }
    public string? ItemCode { get; set; }
    public string? ItemName { get; set; }
    public string? ColorName { get; set; }
    public string? BatchNumber { get; set; }
    public string? LocationCode { get; set; }
    public decimal? RollCount { get; set; }
    public decimal? Quantity { get; set; }
    public decimal? UnitCost { get; set; }
    public string? BankName { get; set; }
    public string? BankAccountNumber { get; set; }
    public string? InvestmentScope { get; set; }
    public decimal Debit { get; set; }
    public decimal Credit { get; set; }
    public string? Reference { get; set; }
    public string? Description { get; set; }
    public string? Notes { get; set; }

    public OpeningBalanceDocumentEntity? Document { get; set; }
}

public class OpeningBalanceEventEntity
{
    public Guid Id { get; set; }
    public Guid DocumentId { get; set; }
    public DateTime OccurredAt { get; set; }
    public Guid? UserId { get; set; }
    public string UserName { get; set; } = "";
    public string Action { get; set; } = "";
    public string? OldValues { get; set; }
    public string? NewValues { get; set; }
    public string? Notes { get; set; }
    public string? MachineName { get; set; }
    public string? IpAddress { get; set; }

    public OpeningBalanceDocumentEntity? Document { get; set; }
}
