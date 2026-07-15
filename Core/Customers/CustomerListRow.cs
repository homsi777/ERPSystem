using ERPSystem.Application.DTOs.Customers;
using DomainCustomerStatus = ERPSystem.Domain.Enums.CustomerStatus;
using DomainCustomerType = ERPSystem.Domain.Enums.CustomerType;

namespace ERPSystem.Core.Customers;

/// <summary>UI row model backed by application DTO — no mock data.</summary>
public sealed class CustomerListRow
{
    public CustomerListRow(CustomerListDto dto) => Dto = dto;

    public CustomerListDto Dto { get; }

    public Guid Id => Dto.Id;
    public string Code => Dto.Code;
    public string NameAr => Dto.NameAr;
    public string NameEn => Dto.NameEn;
    public decimal Balance => Dto.Balance;
    public decimal OpeningBalanceAmount => Dto.OpeningBalanceAmount;
    public decimal PendingOpeningBalanceAmount => Dto.PendingOpeningBalanceAmount;
    public decimal TotalInvoiced => Dto.TotalInvoiced;
    public decimal TotalReceipts => Dto.TotalReceipts;
    public decimal ComputedBalance => Dto.ComputedBalance;
    public int PostedReceiptCount => Dto.PostedReceiptCount;
    public int OpenInvoicesCount => Dto.OpenInvoicesCount;
    public bool OpeningBalancePosted => Dto.OpeningBalancePosted;
    public DateTime? LastReceiptDate => Dto.LastReceiptDate;
    public decimal CreditLimit => Dto.CreditLimit;
    public bool CreditLimitEnabled => Dto.CreditLimitEnabled;
    public DomainCustomerType Type => Dto.Type;
    public DomainCustomerStatus Status => Dto.Status;
    public bool IsActive => Dto.IsActive;

    public string TypeDisplay => Type switch
    {
        DomainCustomerType.Cash => "نقدي",
        DomainCustomerType.Credit => "آجل",
        _ => Type.ToString()
    };

    public string StatusDisplay => !IsActive ? "معطّل" : Status switch
    {
        DomainCustomerStatus.Active => "نشط",
        DomainCustomerStatus.Suspended => "موقوف",
        DomainCustomerStatus.Blocked => "محظور",
        _ => Status.ToString()
    };

    public string CreditLimitDisplay => Type switch
    {
        DomainCustomerType.Cash => "—",
        DomainCustomerType.Credit when !CreditLimitEnabled => "بدون حد",
        _ => AppFormats.Amount(CreditLimit)
    };

    public string OpeningBalanceDisplay => OpeningBalanceAmount > 0
        ? AppFormats.Amount(OpeningBalanceAmount) + (PendingOpeningBalanceAmount > 0 ? " *" : "")
        : OpeningBalancePosted ? AppFormats.Amount(0) : "—";

    public string LastReceiptDisplay => LastReceiptDate?.ToString("yyyy-MM-dd") ?? "—";

    public string FinancialActivityDisplay =>
        PostedReceiptCount > 0 || OpeningBalancePosted || TotalInvoiced > 0
            ? $"{PostedReceiptCount} قبض"
            : "—";

    public static CustomerListRow FromDto(CustomerListDto dto) => new(dto);

    public static CustomerListRow FromDetails(CustomerDetailsDto dto) => new(new CustomerListDto
    {
        Id = dto.Id,
        Code = dto.Code,
        NameAr = dto.NameAr,
        NameEn = dto.NameEn,
        Type = dto.Type,
        Status = dto.Status,
        Balance = dto.Balance,
        CreditLimit = dto.CreditLimit,
        CreditLimitEnabled = dto.CreditLimitEnabled,
        IsActive = dto.IsActive,
        OpeningBalancePosted = dto.OpeningBalancePosted
    });
}
