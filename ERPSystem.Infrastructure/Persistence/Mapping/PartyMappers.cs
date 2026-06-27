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
            Balance = c.Balance.Amount,
            BalanceCurrency = c.Balance.Currency,
            PaymentTermsDays = c.PaymentTermsDays,
            Phone = c.Phone?.Value,
            Email = c.Email?.Value,
            AddressLine1 = c.Address?.Line1,
            AddressCity = c.Address?.City,
            SalesRepUserId = c.SalesRepUserId,
            IsActive = c.IsActive
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
        DomainHydrator.Set(customer, nameof(Customer.Balance), new Money(entity.Balance, entity.BalanceCurrency));
        DomainHydrator.Set(customer, nameof(Customer.PaymentTermsDays), entity.PaymentTermsDays);
        DomainHydrator.Set(customer, nameof(Customer.IsActive), entity.IsActive);

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
        entity.Balance = mapped.Balance;
        entity.PaymentTermsDays = mapped.PaymentTermsDays;
        entity.Phone = mapped.Phone;
        entity.Email = mapped.Email;
        entity.AddressLine1 = mapped.AddressLine1;
        entity.AddressCity = mapped.AddressCity;
        entity.IsActive = mapped.IsActive;
        entity.UpdatedAt = DateTime.UtcNow;
    }
}

internal static class SupplierMapper
{
    public static SupplierEntity ToEntity(SupplierAggregate aggregate) => new()
    {
        Id = aggregate.Supplier.Id,
        CompanyId = aggregate.Supplier.CompanyId,
        Code = aggregate.Supplier.Code,
        Name = aggregate.Supplier.Name,
        Status = (int)aggregate.Supplier.Status,
        Balance = aggregate.Supplier.Balance.Amount,
        BalanceCurrency = aggregate.Supplier.Balance.Currency,
        IsActive = aggregate.Supplier.IsActive
    };

    public static SupplierAggregate ToAggregate(SupplierEntity entity)
    {
        var supplier = DomainHydrator.Create<Supplier>();
        DomainHydrator.Set(supplier, nameof(Supplier.Id), entity.Id);
        DomainHydrator.Set(supplier, nameof(Supplier.CompanyId), entity.CompanyId);
        DomainHydrator.Set(supplier, nameof(Supplier.Code), entity.Code);
        DomainHydrator.Set(supplier, nameof(Supplier.Name), entity.Name);
        DomainHydrator.Set(supplier, nameof(Supplier.Status), (SupplierStatus)entity.Status);
        DomainHydrator.Set(supplier, nameof(Supplier.Balance), new Money(entity.Balance, entity.BalanceCurrency));
        DomainHydrator.Set(supplier, nameof(Supplier.IsActive), entity.IsActive);
        return SupplierAggregate.FromSupplier(supplier);
    }

    public static void UpdateEntity(SupplierEntity entity, SupplierAggregate aggregate)
    {
        entity.Balance = aggregate.Supplier.Balance.Amount;
        entity.Status = (int)aggregate.Supplier.Status;
        entity.IsActive = aggregate.Supplier.IsActive;
        entity.UpdatedAt = DateTime.UtcNow;
    }
}
