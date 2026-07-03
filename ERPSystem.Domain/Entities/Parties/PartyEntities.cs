using ERPSystem.Domain.Enums;
using ERPSystem.Domain.ValueObjects;

namespace ERPSystem.Domain.Entities.Parties;

public class Customer
{
    public Guid Id { get; private set; }
    public string Code { get; private set; } = "";
    public string NameAr { get; private set; } = "";
    public string NameEn { get; private set; } = "";
    public CustomerType Type { get; private set; }
    public CustomerStatus Status { get; private set; }
    public Money CreditLimit { get; private set; } = Money.Zero();
    public Money Balance { get; private set; } = Money.Zero();
    public int PaymentTermsDays { get; private set; }
    public PhoneNumber? Phone { get; private set; }
    public EmailAddress? Email { get; private set; }
    public Address? Address { get; private set; }
    public Guid? SalesRepUserId { get; private set; }
    public bool IsActive { get; private set; } = true;
    public Guid CompanyId { get; private set; }

    private Customer() { }

    public static Customer Create(
        Guid companyId,
        string code,
        string nameAr,
        string nameEn,
        CustomerType type,
        Money? creditLimit = null)
    {
        return new Customer
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            Code = code,
            NameAr = nameAr,
            NameEn = nameEn,
            Type = type,
            CreditLimit = creditLimit ?? Money.Zero(),
            Status = CustomerStatus.Active
        };
    }

    public void ApplyPostedInvoice(Money invoiceTotal)
    {
        if (Type == CustomerType.Credit)
            Balance = Balance.Add(invoiceTotal);
    }

    public void ApplyPostedReceipt(Money amount) =>
        Balance = Balance.Subtract(amount);

    public bool WouldExceedCreditLimit(Money additionalAmount) =>
        Type == CustomerType.Credit && Balance.Add(additionalAmount).Amount > CreditLimit.Amount;

    public void Suspend() => Status = CustomerStatus.Suspended;
    public void Block() => Status = CustomerStatus.Blocked;
    public void Activate() => Status = CustomerStatus.Active;
    public void Deactivate() => IsActive = false;

    public void UpdateProfile(string nameAr, string nameEn, Money creditLimit, int paymentTermsDays)
    {
        NameAr = nameAr;
        NameEn = nameEn;
        CreditLimit = creditLimit;
        PaymentTermsDays = paymentTermsDays;
    }
}

public class Supplier
{
    public Guid Id { get; private set; }
    public string Code { get; private set; } = "";
    public string NameAr { get; private set; } = "";
    public string NameEn { get; private set; } = "";
    public string Name => NameAr;
    public SupplierStatus Status { get; private set; }
    public Money Balance { get; private set; } = Money.Zero();
    public Money CreditLimit { get; private set; } = Money.Zero();
    public int PaymentTermsDays { get; private set; }
    public string CurrencyCode { get; private set; } = "USD";
    public string? Phone { get; private set; }
    public string? Email { get; private set; }
    public string? Address { get; private set; }
    public string? Country { get; private set; }
    public string? City { get; private set; }
    public string? TaxNumber { get; private set; }
    public Guid PayablesAccountId { get; private set; }
    public string? Notes { get; private set; }
    public bool OpeningBalancePosted { get; private set; }
    public bool IsActive { get; private set; } = true;
    public Guid CompanyId { get; private set; }

    private Supplier() { }

    public static Supplier Create(
        Guid companyId,
        string code,
        string nameAr,
        string nameEn,
        Guid payablesAccountId,
        string currencyCode = "USD",
        int paymentTermsDays = 30,
        Money? creditLimit = null) => new()
    {
        Id = Guid.NewGuid(),
        CompanyId = companyId,
        Code = code,
        NameAr = nameAr,
        NameEn = string.IsNullOrWhiteSpace(nameEn) ? nameAr : nameEn,
        PayablesAccountId = payablesAccountId,
        CurrencyCode = currencyCode,
        PaymentTermsDays = paymentTermsDays,
        CreditLimit = creditLimit ?? Money.Zero(),
        Status = SupplierStatus.Active
    };

    public void ApplyPostedPurchase(Money amount) => Balance = Balance.Add(amount);
    public void ApplyPostedPayment(Money amount) => Balance = Balance.Subtract(amount);

    public void MarkOpeningBalancePosted(decimal amount)
    {
        if (OpeningBalancePosted)
            throw new InvalidOperationException("Opening balance already posted for this supplier.");

        OpeningBalancePosted = true;
        if (amount > 0)
            Balance = Balance.Add(new Money(amount, CurrencyCode));
    }

    public void Suspend() => Status = SupplierStatus.Suspended;
    public void Block() => Status = SupplierStatus.Blocked;
    public void Activate() => Status = SupplierStatus.Active;
    public void Deactivate() => IsActive = false;

    public void UpdateProfile(
        string nameAr,
        string nameEn,
        int paymentTermsDays,
        Money creditLimit,
        string? phone,
        string? email,
        string? address,
        string? country,
        string? city,
        string? taxNumber,
        Guid payablesAccountId,
        string? notes)
    {
        NameAr = nameAr;
        NameEn = string.IsNullOrWhiteSpace(nameEn) ? nameAr : nameEn;
        PaymentTermsDays = paymentTermsDays;
        CreditLimit = creditLimit;
        Phone = phone;
        Email = email;
        Address = address;
        Country = country;
        City = city;
        TaxNumber = taxNumber;
        PayablesAccountId = payablesAccountId;
        Notes = notes;
    }
}

public class ChinaSupplier
{
    public Guid Id { get; private set; }
    public Guid SupplierId { get; private set; }
    public string Port { get; private set; } = "";
    public string DefaultIncoterm { get; private set; } = "";
    public int LeadTimeDays { get; private set; }

    private ChinaSupplier() { }

    public static ChinaSupplier Create(Guid supplierId, string port, int leadTimeDays) => new()
    {
        Id = Guid.NewGuid(),
        SupplierId = supplierId,
        Port = port,
        LeadTimeDays = leadTimeDays
    };
}
