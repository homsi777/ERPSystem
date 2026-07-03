using ERPSystem.Domain.Enums;

namespace ERPSystem.Domain.Entities.Accounting;

public class JournalBook
{
    public Guid Id { get; private set; }
    public Guid CompanyId { get; private set; }
    public string Code { get; private set; } = "";
    public string NameAr { get; private set; } = "";
    public string NameEn { get; private set; } = "";
    public JournalBookType BookType { get; private set; }
    public bool IsActive { get; private set; } = true;

    private JournalBook() { }

    public static JournalBook Create(
        Guid id,
        Guid companyId,
        string code,
        string nameAr,
        string nameEn,
        JournalBookType bookType) => new()
    {
        Id = id,
        CompanyId = companyId,
        Code = code,
        NameAr = nameAr,
        NameEn = nameEn,
        BookType = bookType,
        IsActive = true
    };
}
