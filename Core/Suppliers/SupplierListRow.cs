using ERPSystem.Application.DTOs.Suppliers;
using ERPSystem.Domain.Enums;

namespace ERPSystem.Core.Suppliers;

public sealed class SupplierListRow
{
    public SupplierListRow(SupplierListDto dto) => Dto = dto;

    public SupplierListDto Dto { get; }

    public Guid Id => Dto.Id;
    public string Code => Dto.Code;
    public string NameAr => Dto.NameAr;
    public string NameEn => Dto.NameEn;
    public string? Country => Dto.Country;
    public string? Phone => Dto.Phone;
    public decimal Balance => Dto.Balance;
    public int PaymentTermsDays => Dto.PaymentTermsDays;
    public string PaymentTermsDisplay => Dto.PaymentTermsDisplay;
    public SupplierStatus Status => Dto.Status;
    public bool IsActive => Dto.IsActive;

    public string StatusDisplay => !IsActive ? "معطّل" : Status switch
    {
        SupplierStatus.Active => "نشط",
        SupplierStatus.Suspended => "موقوف",
        SupplierStatus.Blocked => "محظور",
        _ => Status.ToString()
    };

    public static SupplierListRow FromDto(SupplierListDto dto) => new(dto);

    public static SupplierListRow FromDetails(SupplierDetailsDto dto) => new(new SupplierListDto
    {
        Id = dto.Id,
        Code = dto.Code,
        NameAr = dto.NameAr,
        NameEn = dto.NameEn,
        Country = dto.Country,
        Phone = dto.Phone,
        Balance = dto.Balance,
        PaymentTermsDays = dto.PaymentTermsDays,
        PaymentTermsDisplay = dto.PaymentTermsDisplay,
        Status = dto.Status,
        IsActive = dto.IsActive
    });
}
