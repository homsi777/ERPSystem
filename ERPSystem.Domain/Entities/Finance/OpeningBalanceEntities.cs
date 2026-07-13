namespace ERPSystem.Domain.Entities.Finance;

/// <summary>
/// All opening balance categories handled by the unified Opening Balance Engine.
/// New categories are added here without modifying the engine workflow
/// (Open/Closed principle — each type only plugs its own validation + journal map).
/// </summary>
public enum OpeningBalanceType
{
    OpeningStock = 0,
    CustomerReceivable = 1,
    SupplierPayable = 2,
    Cash = 3,
    Bank = 4,
    Capital = 5,
    GeneralLedger = 6,

    // Future-ready categories (already supported by the generic line model)
    FixedAsset = 7,
    Loan = 8,
    EmployeeAdvance = 9,
    PettyCash = 10,
    BranchOpening = 11
}

/// <summary>Enterprise approval workflow for opening balance documents.</summary>
public enum OpeningBalanceStatus
{
    Draft = 0,
    PendingApproval = 1,
    Approved = 2,
    Posted = 3,
    Locked = 4,
    Archived = 5,
    Rejected = 6
}

/// <summary>How the document entered the system.</summary>
public enum OpeningBalanceSource
{
    Manual = 0,
    ExcelImport = 1
}

/// <summary>
/// Aggregate root for one opening balance document (header + lines).
/// Every historical balance before go-live passes through this aggregate —
/// no module may write opening balances directly.
/// </summary>
public class OpeningBalanceDocument
{
    private readonly List<OpeningBalanceLine> _lines = [];

    public Guid Id { get; private set; }
    public Guid CompanyId { get; private set; }
    public Guid BranchId { get; private set; }
    public string Number { get; private set; } = "";
    public OpeningBalanceType Type { get; private set; }
    public OpeningBalanceStatus Status { get; private set; }
    public OpeningBalanceSource Source { get; private set; }

    public DateTime OpeningDate { get; private set; }
    public string CurrencyCode { get; private set; } = "USD";
    public decimal ExchangeRate { get; private set; } = 1m;
    public string? Reference { get; private set; }
    public string? Description { get; private set; }
    public string? Notes { get; private set; }

    public decimal TotalDebit { get; private set; }
    public decimal TotalCredit { get; private set; }
    /// <summary>Grand total in base currency (max of debit/credit sides).</summary>
    public decimal TotalBaseAmount { get; private set; }

    public string? JournalEntryNumber { get; private set; }

    public DateTime CreatedAt { get; private set; }
    public Guid? CreatedByUserId { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public DateTime? ApprovedAt { get; private set; }
    public Guid? ApprovedByUserId { get; private set; }
    public string? ApprovalNotes { get; private set; }
    public string? RejectionReason { get; private set; }
    public DateTime? PostedAt { get; private set; }
    public Guid? PostedByUserId { get; private set; }
    public DateTime? LockedAt { get; private set; }
    public DateTime? ArchivedAt { get; private set; }

    public IReadOnlyList<OpeningBalanceLine> Lines => _lines;

    private OpeningBalanceDocument() { }

    public static OpeningBalanceDocument Create(
        Guid companyId,
        Guid branchId,
        string number,
        OpeningBalanceType type,
        OpeningBalanceSource source,
        DateTime openingDate,
        string currencyCode,
        decimal exchangeRate,
        string? reference,
        string? description,
        string? notes,
        Guid? createdByUserId) => new()
    {
        Id = Guid.NewGuid(),
        CompanyId = companyId,
        BranchId = branchId,
        Number = number,
        Type = type,
        Source = source,
        Status = OpeningBalanceStatus.Draft,
        OpeningDate = openingDate,
        CurrencyCode = string.IsNullOrWhiteSpace(currencyCode) ? "USD" : currencyCode.Trim().ToUpperInvariant(),
        ExchangeRate = exchangeRate <= 0 ? 1m : exchangeRate,
        Reference = reference,
        Description = description,
        Notes = notes,
        CreatedAt = DateTime.UtcNow,
        CreatedByUserId = createdByUserId
    };

    public bool IsEditable => Status is OpeningBalanceStatus.Draft or OpeningBalanceStatus.Rejected;
    public bool IsLockedOrArchived => Status is OpeningBalanceStatus.Locked or OpeningBalanceStatus.Archived;

    public void UpdateHeader(
        DateTime openingDate,
        string currencyCode,
        decimal exchangeRate,
        string? reference,
        string? description,
        string? notes)
    {
        EnsureEditable();
        OpeningDate = openingDate;
        CurrencyCode = string.IsNullOrWhiteSpace(currencyCode) ? CurrencyCode : currencyCode.Trim().ToUpperInvariant();
        ExchangeRate = exchangeRate <= 0 ? ExchangeRate : exchangeRate;
        Reference = reference;
        Description = description;
        Notes = notes;
        UpdatedAt = DateTime.UtcNow;
    }

    public void ReplaceLines(IEnumerable<OpeningBalanceLine> lines)
    {
        EnsureEditable();
        _lines.Clear();
        var n = 0;
        foreach (var line in lines)
        {
            line.AttachTo(Id, ++n);
            _lines.Add(line);
        }

        RecalculateTotals();
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>Used by persistence rehydration only.</summary>
    public void HydrateLines(IEnumerable<OpeningBalanceLine> lines)
    {
        _lines.Clear();
        _lines.AddRange(lines);
        RecalculateTotals();
    }

    public void SubmitForApproval()
    {
        if (Status is not (OpeningBalanceStatus.Draft or OpeningBalanceStatus.Rejected))
            throw new InvalidOperationException("لا يمكن إرسال المستند للاعتماد إلا من حالة مسودة أو مرفوض.");
        if (_lines.Count == 0)
            throw new InvalidOperationException("لا يمكن إرسال مستند بدون سطور للاعتماد.");
        Status = OpeningBalanceStatus.PendingApproval;
        RejectionReason = null;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Approve(Guid? userId, string? notes)
    {
        if (Status is not (OpeningBalanceStatus.PendingApproval or OpeningBalanceStatus.Draft))
            throw new InvalidOperationException("لا يمكن اعتماد المستند من حالته الحالية.");
        Status = OpeningBalanceStatus.Approved;
        ApprovedAt = DateTime.UtcNow;
        ApprovedByUserId = userId;
        ApprovalNotes = notes;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Reject(string reason)
    {
        if (Status != OpeningBalanceStatus.PendingApproval)
            throw new InvalidOperationException("لا يمكن رفض مستند غير معلق للاعتماد.");
        Status = OpeningBalanceStatus.Rejected;
        RejectionReason = reason;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkPosted(Guid? userId, string journalEntryNumber)
    {
        if (Status != OpeningBalanceStatus.Approved)
            throw new InvalidOperationException("لا يمكن ترحيل مستند غير معتمد.");
        Status = OpeningBalanceStatus.Posted;
        JournalEntryNumber = journalEntryNumber;
        PostedAt = DateTime.UtcNow;
        PostedByUserId = userId;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Lock()
    {
        if (Status != OpeningBalanceStatus.Posted)
            throw new InvalidOperationException("لا يمكن قفل مستند غير مرحّل.");
        Status = OpeningBalanceStatus.Locked;
        LockedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Archive()
    {
        if (Status is OpeningBalanceStatus.Archived)
            return;
        if (Status is OpeningBalanceStatus.Posted)
            throw new InvalidOperationException("المستند المرحّل يجب قفله قبل الأرشفة.");
        Status = OpeningBalanceStatus.Archived;
        ArchivedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    private void RecalculateTotals()
    {
        TotalDebit = _lines.Sum(l => l.Debit);
        TotalCredit = _lines.Sum(l => l.Credit);
        TotalBaseAmount = Math.Max(TotalDebit, TotalCredit) * ExchangeRate;
    }

    private void EnsureEditable()
    {
        if (!IsEditable)
            throw new InvalidOperationException("لا يمكن تعديل مستند بعد اعتماده أو ترحيله.");
    }
}

/// <summary>
/// A single opening balance line. The generic shape covers every balance type:
/// stock lines use warehouse/item/quantity fields, party lines use PartyId,
/// cash/bank/GL lines use AccountId, capital lines use PartyId + scope.
/// </summary>
public class OpeningBalanceLine
{
    public Guid Id { get; private set; }
    public Guid DocumentId { get; private set; }
    public int LineNumber { get; private set; }

    // Party (customer / supplier / partner / employee)
    public Guid? PartyId { get; private set; }
    public string? PartyName { get; private set; }

    // GL account (cash / bank / GL / capital funding account)
    public Guid? AccountId { get; private set; }
    public string? AccountName { get; private set; }

    // Inventory (opening stock)
    public Guid? WarehouseId { get; private set; }
    public string? WarehouseName { get; private set; }
    public Guid? FabricItemId { get; private set; }
    public Guid? FabricColorId { get; private set; }
    public string? ItemCode { get; private set; }
    public string? ItemName { get; private set; }
    public string? ColorName { get; private set; }
    public string? BatchNumber { get; private set; }
    public string? LocationCode { get; private set; }
    public decimal? RollCount { get; private set; }
    public decimal? Quantity { get; private set; }
    public decimal? UnitCost { get; private set; }

    // Bank
    public string? BankName { get; private set; }
    public string? BankAccountNumber { get; private set; }

    // Capital
    public string? InvestmentScope { get; private set; }

    // Amounts (document currency)
    public decimal Debit { get; private set; }
    public decimal Credit { get; private set; }

    public string? Reference { get; private set; }
    public string? Description { get; private set; }
    public string? Notes { get; private set; }

    /// <summary>The economic value of the line (whichever side is non-zero).</summary>
    public decimal Amount => Debit > 0 ? Debit : Credit;

    private OpeningBalanceLine() { }

    public static OpeningBalanceLine Create(
        decimal debit,
        decimal credit,
        Guid? partyId = null,
        string? partyName = null,
        Guid? accountId = null,
        string? accountName = null,
        Guid? warehouseId = null,
        string? warehouseName = null,
        Guid? fabricItemId = null,
        Guid? fabricColorId = null,
        string? itemCode = null,
        string? itemName = null,
        string? colorName = null,
        string? batchNumber = null,
        string? locationCode = null,
        decimal? rollCount = null,
        decimal? quantity = null,
        decimal? unitCost = null,
        string? bankName = null,
        string? bankAccountNumber = null,
        string? investmentScope = null,
        string? reference = null,
        string? description = null,
        string? notes = null) => new()
    {
        Id = Guid.NewGuid(),
        Debit = debit,
        Credit = credit,
        PartyId = partyId,
        PartyName = partyName,
        AccountId = accountId,
        AccountName = accountName,
        WarehouseId = warehouseId,
        WarehouseName = warehouseName,
        FabricItemId = fabricItemId,
        FabricColorId = fabricColorId,
        ItemCode = itemCode,
        ItemName = itemName,
        ColorName = colorName,
        BatchNumber = batchNumber,
        LocationCode = locationCode,
        RollCount = rollCount,
        Quantity = quantity,
        UnitCost = unitCost,
        BankName = bankName,
        BankAccountNumber = bankAccountNumber,
        InvestmentScope = investmentScope,
        Reference = reference,
        Description = description,
        Notes = notes
    };

    internal void AttachTo(Guid documentId, int lineNumber)
    {
        DocumentId = documentId;
        LineNumber = lineNumber;
    }
}

/// <summary>
/// Unified audit + timeline event for opening balance documents. Every workflow
/// transition, edit, and import records one event (who / when / where / what).
/// </summary>
public class OpeningBalanceEvent
{
    public Guid Id { get; private set; }
    public Guid DocumentId { get; private set; }
    public DateTime OccurredAt { get; private set; }
    public Guid? UserId { get; private set; }
    public string UserName { get; private set; } = "";
    public string Action { get; private set; } = "";
    public string? OldValues { get; private set; }
    public string? NewValues { get; private set; }
    public string? Notes { get; private set; }
    public string? MachineName { get; private set; }
    public string? IpAddress { get; private set; }

    private OpeningBalanceEvent() { }

    public static OpeningBalanceEvent Record(
        Guid documentId,
        Guid? userId,
        string userName,
        string action,
        string? oldValues = null,
        string? newValues = null,
        string? notes = null,
        string? machineName = null,
        string? ipAddress = null) => new()
    {
        Id = Guid.NewGuid(),
        DocumentId = documentId,
        OccurredAt = DateTime.UtcNow,
        UserId = userId,
        UserName = userName,
        Action = action,
        OldValues = oldValues,
        NewValues = newValues,
        Notes = notes,
        MachineName = machineName,
        IpAddress = ipAddress
    };
}
