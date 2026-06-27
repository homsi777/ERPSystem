using ERPSystem.Domain.Common;
using ERPSystem.Domain.Entities.Parties;

namespace ERPSystem.Domain.Aggregates;

public sealed class CustomerAggregate : AggregateRoot
{
    public Customer Customer { get; private set; } = null!;

    private CustomerAggregate() { }

    public static CustomerAggregate FromCustomer(Customer customer) => new()
    {
        Id = customer.Id,
        Customer = customer
    };

    public void RecordPostedInvoice(decimal invoiceTotal)
    {
        Customer.ApplyPostedInvoice(new ValueObjects.Money(invoiceTotal));
    }

    public void RecordPostedReceipt(decimal amount) =>
        Customer.ApplyPostedReceipt(new ValueObjects.Money(amount));

    public bool WouldExceedCreditLimit(decimal additionalAmount) =>
        Customer.WouldExceedCreditLimit(new ValueObjects.Money(additionalAmount));
}

public sealed class SupplierAggregate : AggregateRoot
{
    public Supplier Supplier { get; private set; } = null!;

    private SupplierAggregate() { }

    public static SupplierAggregate FromSupplier(Supplier supplier) => new()
    {
        Id = supplier.Id,
        Supplier = supplier
    };
}
