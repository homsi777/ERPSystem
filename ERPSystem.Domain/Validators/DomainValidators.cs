using ERPSystem.Domain.Aggregates;
using ERPSystem.Domain.Entities.Finance;
using ERPSystem.Domain.Entities.Parties;
using ERPSystem.Domain.Enums;
using ERPSystem.Domain.Exceptions;

namespace ERPSystem.Domain.Validators;

public static class CustomerValidator
{
    public static void Validate(Customer customer)
    {
        if (string.IsNullOrWhiteSpace(customer.Code))
            throw new ValidationException("Customer code is required.");
        if (string.IsNullOrWhiteSpace(customer.NameAr))
            throw new ValidationException("Customer Arabic name is required.");
        if (customer.CompanyId == Guid.Empty)
            throw new ValidationException("Customer must belong to a company.");
        if (customer.Type == CustomerType.Credit && customer.CreditLimit.Amount < 0)
            throw new ValidationException("Credit limit cannot be negative.");
    }
}

public static class ContainerValidator
{
    public static void Validate(ContainerAggregate container)
    {
        if (container.ContainerNumber is null || string.IsNullOrWhiteSpace(container.ContainerNumber.Value))
            throw new ValidationException("Container number is required.");
        if (container.SupplierId == Guid.Empty)
            throw new ValidationException("Supplier is required.");
        if (container.Items.Count == 0 && container.Status != ChinaContainerStatus.Draft)
            throw new ValidationException("Container must have at least one item.");
    }
}

public static class SalesInvoiceValidator
{
    public static void ValidateDraft(SalesInvoiceAggregate invoice)
    {
        if (invoice.CustomerId == Guid.Empty)
            throw new ValidationException("Customer is required.");
        if (invoice.WarehouseId == Guid.Empty)
            throw new ValidationException("Warehouse is required.");
        if (invoice.ChinaContainerId == Guid.Empty)
            throw new ValidationException("China container is required.");
        if (invoice.Items.Count == 0)
            throw new ValidationException("Invoice must have at least one line item.");
    }
}

public static class LandingCostValidator
{
    public static void Validate(Entities.ChinaImport.LandingCost landingCost)
    {
        if (landingCost.TotalLengthFromInvoice.Value <= 0)
            throw new ValidationException("Total length must be greater than zero.");
        if (landingCost.CustomsAmountPaid.Amount < 0)
            throw new ValidationException("Customs amount cannot be negative.");
    }
}

public static class WarehouseValidator
{
    public static void Validate(Entities.Inventory.Warehouse warehouse)
    {
        if (string.IsNullOrWhiteSpace(warehouse.Code))
            throw new ValidationException("Warehouse code is required.");
        if (warehouse.BranchId == Guid.Empty)
            throw new ValidationException("Warehouse must belong to a branch.");
    }
}

public static class JournalValidator
{
    public static void ValidateDraft(AccountingAggregate entry)
    {
        if (string.IsNullOrWhiteSpace(entry.EntryNumber))
            throw new ValidationException("Entry number is required.");
        if (entry.Lines.Count == 0)
            throw new ValidationException("Journal entry must have lines.");
        if (Math.Abs(entry.DebitTotal.Amount - entry.CreditTotal.Amount) > 0.01m)
            throw new ValidationException("Journal entry must be balanced.");
    }
}

public static class ReceiptVoucherValidator
{
    public static void Validate(ReceiptVoucher voucher)
    {
        if (voucher.CustomerId == Guid.Empty)
            throw new ValidationException("Customer is required.");
        if (voucher.CashboxId == Guid.Empty)
            throw new ValidationException("Cashbox is required.");
        if (voucher.Amount.Amount <= 0)
            throw new ValidationException("Receipt amount must be greater than zero.");
    }
}

public static class PaymentVoucherValidator
{
    public static void Validate(PaymentVoucher voucher)
    {
        if (voucher.SupplierId == Guid.Empty)
            throw new ValidationException("Supplier is required.");
        if (voucher.CashboxId == Guid.Empty)
            throw new ValidationException("Cashbox is required.");
        if (voucher.Amount.Amount <= 0)
            throw new ValidationException("Payment amount must be greater than zero.");
    }
}
