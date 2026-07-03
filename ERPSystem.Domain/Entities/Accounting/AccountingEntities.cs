using ERPSystem.Domain.Enums;
using ERPSystem.Domain.Exceptions;
using ERPSystem.Domain.ValueObjects;

namespace ERPSystem.Domain.Entities.Accounting;

public class Account
{
    public Guid Id { get; private set; }
    public Guid CompanyId { get; private set; }
    public string Code { get; private set; } = "";
    public string NameAr { get; private set; } = "";
    public string NameEn { get; private set; } = "";
    public GlAccountType AccountType { get; private set; }
    public Guid? ParentId { get; private set; }
    public bool IsPostable { get; private set; } = true;
    public bool IsActive { get; private set; } = true;

    private Account() { }

    public static Account Create(
        Guid companyId,
        string code,
        string nameAr,
        string nameEn,
        GlAccountType accountType,
        Guid? parentId = null,
        bool isPostable = true) => new()
    {
        Id = Guid.NewGuid(),
        CompanyId = companyId,
        Code = code.Trim(),
        NameAr = nameAr.Trim(),
        NameEn = string.IsNullOrWhiteSpace(nameEn) ? nameAr.Trim() : nameEn.Trim(),
        AccountType = accountType,
        ParentId = parentId,
        IsPostable = isPostable,
        IsActive = true
    };

    public void Update(
        string code,
        string nameAr,
        string nameEn,
        GlAccountType accountType,
        Guid? parentId,
        bool isPostable)
    {
        if (!IsActive)
            throw new AccountingException("Cannot update an inactive account.");

        if (parentId == Id)
            throw new AccountingException("Account cannot be its own parent.");

        Code = code.Trim();
        NameAr = nameAr.Trim();
        NameEn = string.IsNullOrWhiteSpace(nameEn) ? nameAr.Trim() : nameEn.Trim();
        AccountType = accountType;
        ParentId = parentId;
        IsPostable = isPostable;
    }

    public void Deactivate()
    {
        if (!IsActive)
            return;

        IsActive = false;
        IsPostable = false;
    }
}

public class JournalEntryLine
{
    public Guid Id { get; private set; }
    public Guid AccountId { get; private set; }
    public Money Debit { get; private set; } = Money.Zero();
    public Money Credit { get; private set; } = Money.Zero();
    public string Narrative { get; private set; } = "";
    public Guid? PartyId { get; private set; }

    private JournalEntryLine() { }

    public static JournalEntryLine Create(
        Guid accountId,
        Money debit,
        Money credit,
        string narrative,
        Guid? partyId = null) => new()
    {
        Id = Guid.NewGuid(),
        AccountId = accountId,
        Debit = debit,
        Credit = credit,
        Narrative = narrative,
        PartyId = partyId
    };
}

public class PostingBatch
{
    public Guid Id { get; private set; }
    public string BatchNumber { get; private set; } = "";
    public DateTime PostedAt { get; private set; }
    public Guid PostedByUserId { get; private set; }

    private PostingBatch() { }

    public static PostingBatch Create(string batchNumber, Guid userId) => new()
    {
        Id = Guid.NewGuid(),
        BatchNumber = batchNumber,
        PostedAt = DateTime.UtcNow,
        PostedByUserId = userId
    };
}

public class CustomerStatementEntry
{
    public Guid Id { get; private set; }
    public Guid CustomerId { get; private set; }
    public DateTime EntryDate { get; private set; }
    public DocumentType DocumentType { get; private set; }
    public Guid DocumentId { get; private set; }
    public Money Debit { get; private set; } = Money.Zero();
    public Money Credit { get; private set; } = Money.Zero();
    public Money RunningBalance { get; private set; } = Money.Zero();

    private CustomerStatementEntry() { }

    public static CustomerStatementEntry Create(
        Guid customerId,
        DateTime entryDate,
        DocumentType documentType,
        Guid documentId,
        Money debit,
        Money credit,
        Money runningBalance) => new()
    {
        Id = Guid.NewGuid(),
        CustomerId = customerId,
        EntryDate = entryDate,
        DocumentType = documentType,
        DocumentId = documentId,
        Debit = debit,
        Credit = credit,
        RunningBalance = runningBalance
    };
}

public class SupplierStatementEntry
{
    public Guid Id { get; private set; }
    public Guid SupplierId { get; private set; }
    public DateTime EntryDate { get; private set; }
    public DocumentType DocumentType { get; private set; }
    public Guid DocumentId { get; private set; }
    public Money Debit { get; private set; } = Money.Zero();
    public Money Credit { get; private set; } = Money.Zero();

    private SupplierStatementEntry() { }

    public static SupplierStatementEntry Create(
        Guid supplierId,
        DateTime entryDate,
        DocumentType documentType,
        Guid documentId,
        Money debit,
        Money credit) => new()
    {
        Id = Guid.NewGuid(),
        SupplierId = supplierId,
        EntryDate = entryDate,
        DocumentType = documentType,
        DocumentId = documentId,
        Debit = debit,
        Credit = credit
    };
}
