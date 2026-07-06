using ERPSystem.Domain.Aggregates;
using ERPSystem.Domain.Entities.Parties;
using ERPSystem.Domain.Enums;
using ERPSystem.Domain.ValueObjects;
using ERPSystem.Infrastructure.Persistence.Models.Parties;

namespace ERPSystem.Infrastructure.Persistence.Mapping;

internal static class CustomerMapper
{
    public static CustomerEntity ToEntity(CustomerAggregate aggregate)
    {
        var c = aggregate.Customer;
        return new CustomerEntity
        {
            Id = c.Id,
            CompanyId = c.CompanyId,
            Code = c.Code,
            NameAr = c.NameAr,
            NameEn = c.NameEn,
            Type = (int)c.Type,
            Status = (int)c.Status,
            CreditLimit = c.CreditLimit.Amount,
            CreditLimitCurrency = c.CreditLimit.Currency,
            CreditLimitEnabled = c.CreditLimitEnabled,
            Balance = c.Balance.Amount,
            BalanceCurrency = c.Balance.Currency,
            PaymentTermsDays = c.PaymentTermsDays,
            Phone = c.Phone?.Value,
            Email = c.Email?.Value,
            AddressLine1 = c.Address?.Line1,
            AddressCity = c.Address?.City,
            SalesRepUserId = c.SalesRepUserId,
            OpeningBalancePosted = c.OpeningBalancePosted,
            IsActive = c.IsActive,
            LastReconciliationDate = c.LastReconciliationDate,
            LastReconciliationBalance = c.LastReconciliationBalance,
            LastReconciliationDocumentId = c.LastReconciliationDocumentId
        };
    }

    public static CustomerAggregate ToAggregate(CustomerEntity entity)
    {
        var customer = DomainHydrator.Create<Customer>();
        DomainHydrator.Set(customer, nameof(Customer.Id), entity.Id);
        DomainHydrator.Set(customer, nameof(Customer.CompanyId), entity.CompanyId);
        DomainHydrator.Set(customer, nameof(Customer.Code), entity.Code);
        DomainHydrator.Set(customer, nameof(Customer.NameAr), entity.NameAr);
        DomainHydrator.Set(customer, nameof(Customer.NameEn), entity.NameEn);
        DomainHydrator.Set(customer, nameof(Customer.Type), (CustomerType)entity.Type);
        DomainHydrator.Set(customer, nameof(Customer.Status), (CustomerStatus)entity.Status);
        DomainHydrator.Set(customer, nameof(Customer.CreditLimit), new Money(entity.CreditLimit, entity.CreditLimitCurrency));
        DomainHydrator.Set(customer, nameof(Customer.CreditLimitEnabled), entity.CreditLimitEnabled);
        DomainHydrator.Set(customer, nameof(Customer.Balance), new Money(entity.Balance, entity.BalanceCurrency));
        DomainHydrator.Set(customer, nameof(Customer.PaymentTermsDays), entity.PaymentTermsDays);
        DomainHydrator.Set(customer, nameof(Customer.IsActive), entity.IsActive);
        DomainHydrator.Set(customer, nameof(Customer.OpeningBalancePosted), entity.OpeningBalancePosted);

        if (entity.LastReconciliationDate.HasValue)
            DomainHydrator.Set(customer, nameof(Customer.LastReconciliationDate), entity.LastReconciliationDate);
        if (entity.LastReconciliationBalance.HasValue)
            DomainHydrator.Set(customer, nameof(Customer.LastReconciliationBalance), entity.LastReconciliationBalance);
        if (entity.LastReconciliationDocumentId.HasValue)
            DomainHydrator.Set(customer, nameof(Customer.LastReconciliationDocumentId), entity.LastReconciliationDocumentId);

        if (!string.IsNullOrWhiteSpace(entity.Phone))
            DomainHydrator.Set(customer, nameof(Customer.Phone), new PhoneNumber(entity.Phone));
        if (!string.IsNullOrWhiteSpace(entity.Email))
            DomainHydrator.Set(customer, nameof(Customer.Email), new EmailAddress(entity.Email));
        if (!string.IsNullOrWhiteSpace(entity.AddressLine1) && !string.IsNullOrWhiteSpace(entity.AddressCity))
            DomainHydrator.Set(customer, nameof(Customer.Address), new Address(entity.AddressLine1, entity.AddressCity));
        if (entity.SalesRepUserId.HasValue)
            DomainHydrator.Set(customer, nameof(Customer.SalesRepUserId), entity.SalesRepUserId);

        return CustomerAggregate.FromCustomer(customer);
    }

    public static void UpdateEntity(CustomerEntity entity, CustomerAggregate aggregate)
    {
        var mapped = ToEntity(aggregate);
        entity.NameAr = mapped.NameAr;
        entity.NameEn = mapped.NameEn;
        entity.Status = mapped.Status;
        entity.CreditLimit = mapped.CreditLimit;
        entity.CreditLimitEnabled = mapped.CreditLimitEnabled;
        entity.Balance = mapped.Balance;
        entity.PaymentTermsDays = mapped.PaymentTermsDays;
        entity.Phone = mapped.Phone;
        entity.Email = mapped.Email;
        entity.AddressLine1 = mapped.AddressLine1;
        entity.AddressCity = mapped.AddressCity;
        entity.IsActive = mapped.IsActive;
        entity.OpeningBalancePosted = mapped.OpeningBalancePosted;
        entity.LastReconciliationDate = mapped.LastReconciliationDate;
        entity.LastReconciliationBalance = mapped.LastReconciliationBalance;
        entity.LastReconciliationDocumentId = mapped.LastReconciliationDocumentId;
        entity.UpdatedAt = DateTime.UtcNow;
    }
}

internal static class SupplierMapper
{
    public static SupplierEntity ToEntity(SupplierAggregate aggregate)
    {
        var s = aggregate.Supplier;
        return new SupplierEntity
        {
            Id = s.Id,
            CompanyId = s.CompanyId,
            Code = s.Code,
            Name = s.NameAr,
            NameAr = s.NameAr,
            NameEn = s.NameEn,
            Status = (int)s.Status,
            Balance = s.Balance.Amount,
            BalanceCurrency = s.Balance.Currency,
            CreditLimit = s.CreditLimit.Amount,
            CreditLimitCurrency = s.CreditLimit.Currency,
            PaymentTermsDays = s.PaymentTermsDays,
            CurrencyCode = s.CurrencyCode,
            Phone = s.Phone,
            Email = s.Email,
            Address = s.Address,
            Country = s.Country,
            City = s.City,
            TaxNumber = s.TaxNumber,
            PayablesAccountId = s.PayablesAccountId,
            Notes = s.Notes,
            OpeningBalancePosted = s.OpeningBalancePosted,
            IsActive = s.IsActive
        };
    }

    public static SupplierAggregate ToAggregate(SupplierEntity entity)
    {
        var supplier = DomainHydrator.Create<Supplier>();
        DomainHydrator.Set(supplier, nameof(Supplier.Id), entity.Id);
        DomainHydrator.Set(supplier, nameof(Supplier.CompanyId), entity.CompanyId);
        DomainHydrator.Set(supplier, nameof(Supplier.Code), entity.Code);
        var nameAr = string.IsNullOrWhiteSpace(entity.NameAr) ? entity.Name : entity.NameAr;
        DomainHydrator.Set(supplier, nameof(Supplier.NameAr), nameAr);
        DomainHydrator.Set(supplier, nameof(Supplier.NameEn), string.IsNullOrWhiteSpace(entity.NameEn) ? nameAr : entity.NameEn);
        DomainHydrator.Set(supplier, nameof(Supplier.Status), (SupplierStatus)entity.Status);
        DomainHydrator.Set(supplier, nameof(Supplier.Balance), new Money(entity.Balance, entity.BalanceCurrency));
        DomainHydrator.Set(supplier, nameof(Supplier.CreditLimit), new Money(entity.CreditLimit, entity.CreditLimitCurrency));
        DomainHydrator.Set(supplier, nameof(Supplier.PaymentTermsDays), entity.PaymentTermsDays);
        DomainHydrator.Set(supplier, nameof(Supplier.CurrencyCode), entity.CurrencyCode);
        DomainHydrator.Set(supplier, nameof(Supplier.Phone), entity.Phone);
        DomainHydrator.Set(supplier, nameof(Supplier.Email), entity.Email);
        DomainHydrator.Set(supplier, nameof(Supplier.Address), entity.Address);
        DomainHydrator.Set(supplier, nameof(Supplier.Country), entity.Country);
        DomainHydrator.Set(supplier, nameof(Supplier.City), entity.City);
        DomainHydrator.Set(supplier, nameof(Supplier.TaxNumber), entity.TaxNumber);
        DomainHydrator.Set(supplier, nameof(Supplier.PayablesAccountId), entity.PayablesAccountId);
        DomainHydrator.Set(supplier, nameof(Supplier.Notes), entity.Notes);
        DomainHydrator.Set(supplier, nameof(Supplier.OpeningBalancePosted), entity.OpeningBalancePosted);
        DomainHydrator.Set(supplier, nameof(Supplier.IsActive), entity.IsActive);
        return SupplierAggregate.FromSupplier(supplier);
    }

    public static void UpdateEntity(SupplierEntity entity, SupplierAggregate aggregate)
    {
        var mapped = ToEntity(aggregate);
        entity.Name = mapped.Name;
        entity.NameAr = mapped.NameAr;
        entity.NameEn = mapped.NameEn;
        entity.Status = mapped.Status;
        entity.Balance = mapped.Balance;
        entity.CreditLimit = mapped.CreditLimit;
        entity.PaymentTermsDays = mapped.PaymentTermsDays;
        entity.CurrencyCode = mapped.CurrencyCode;
        entity.Phone = mapped.Phone;
        entity.Email = mapped.Email;
        entity.Address = mapped.Address;
        entity.Country = mapped.Country;
        entity.City = mapped.City;
        entity.TaxNumber = mapped.TaxNumber;
        entity.PayablesAccountId = mapped.PayablesAccountId;
        entity.Notes = mapped.Notes;
        entity.OpeningBalancePosted = mapped.OpeningBalancePosted;
        entity.IsActive = mapped.IsActive;
        entity.UpdatedAt = DateTime.UtcNow;
    }
}
