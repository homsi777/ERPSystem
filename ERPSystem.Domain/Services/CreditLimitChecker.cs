using ERPSystem.Domain.Aggregates;
using ERPSystem.Domain.Entities.Parties;
using ERPSystem.Domain.Events.Finance;
using ERPSystem.Domain.Exceptions;
using ERPSystem.Domain.ValueObjects;

namespace ERPSystem.Domain.Services;

public static class CreditLimitChecker
{
    public static bool IsWithinLimit(Customer customer, Money additionalAmount) =>
        !customer.WouldExceedCreditLimit(additionalAmount);

    public static void EnsureWithinLimit(CustomerAggregate customerAggregate, Money invoiceTotal)
    {
        if (customerAggregate.WouldExceedCreditLimit(invoiceTotal.Amount))
            throw new CreditLimitExceededException(
                customerAggregate.Customer.CreditLimit.Amount,
                customerAggregate.Customer.Balance.Add(invoiceTotal).Amount);
    }

    public static CustomerCreditLimitExceeded? TryCreateExceededEvent(
        Customer customer,
        Money additionalAmount)
    {
        if (!customer.WouldExceedCreditLimit(additionalAmount))
            return null;

        return new CustomerCreditLimitExceeded(
            customer.Id,
            additionalAmount.Amount,
            customer.CreditLimit.Amount);
    }
}
