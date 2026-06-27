using ERPSystem.Application.Commands.Accounting;
using ERPSystem.Application.Commands.Containers;
using ERPSystem.Application.Commands.Customers;
using ERPSystem.Application.Commands.Finance;
using ERPSystem.Application.Commands.Sales;
using ERPSystem.Application.Results;

namespace ERPSystem.Application.Validation;

public static class ApplicationValidators
{
    public static ApplicationResult? Validate(CreateCustomerCommand command)
    {
        var errors = new List<ValidationError>();
        if (command.CompanyId == Guid.Empty)
            errors.Add(new ValidationError(nameof(command.CompanyId), "Company is required."));
        if (string.IsNullOrWhiteSpace(command.Code))
            errors.Add(new ValidationError(nameof(command.Code), "Customer code is required."));
        if (string.IsNullOrWhiteSpace(command.NameAr))
            errors.Add(new ValidationError(nameof(command.NameAr), "Customer name is required."));
        return errors.Count > 0 ? ApplicationResult.ValidationFailed(errors) : null;
    }

    public static ApplicationResult? Validate(CreateChinaContainerCommand command)
    {
        var errors = new List<ValidationError>();
        if (command.CompanyId == Guid.Empty)
            errors.Add(new ValidationError(nameof(command.CompanyId), "Company is required."));
        if (command.BranchId == Guid.Empty)
            errors.Add(new ValidationError(nameof(command.BranchId), "Branch is required."));
        if (command.SupplierId == Guid.Empty)
            errors.Add(new ValidationError(nameof(command.SupplierId), "Supplier is required."));
        if (string.IsNullOrWhiteSpace(command.ContainerNumber))
            errors.Add(new ValidationError(nameof(command.ContainerNumber), "Container number is required."));
        return errors.Count > 0 ? ApplicationResult.ValidationFailed(errors) : null;
    }

    public static ApplicationResult? Validate(CalculateLandingCostCommand command)
    {
        var errors = new List<ValidationError>();
        if (command.ContainerId == Guid.Empty)
            errors.Add(new ValidationError(nameof(command.ContainerId), "Container is required."));
        if (command.TotalLengthMeters <= 0)
            errors.Add(new ValidationError(nameof(command.TotalLengthMeters), "Total length must be greater than zero."));
        if (command.ContainerWeightKg <= 0)
            errors.Add(new ValidationError(nameof(command.ContainerWeightKg), "Container weight must be greater than zero."));
        return errors.Count > 0 ? ApplicationResult.ValidationFailed(errors) : null;
    }

    public static ApplicationResult? Validate(CreateSalesInvoiceDraftCommand command)
    {
        var errors = new List<ValidationError>();
        if (command.CustomerId == Guid.Empty)
            errors.Add(new ValidationError(nameof(command.CustomerId), "Customer is required."));
        if (command.WarehouseId == Guid.Empty)
            errors.Add(new ValidationError(nameof(command.WarehouseId), "Warehouse is required."));
        if (command.ChinaContainerId == Guid.Empty)
            errors.Add(new ValidationError(nameof(command.ChinaContainerId), "China container is required."));
        if (command.Lines.Count == 0)
            errors.Add(new ValidationError(nameof(command.Lines), "At least one line item is required."));
        return errors.Count > 0 ? ApplicationResult.ValidationFailed(errors) : null;
    }

    public static ApplicationResult? Validate(CompleteWarehouseDetailingCommand command)
    {
        if (command.InvoiceId == Guid.Empty)
            return ApplicationResult.ValidationFailed(nameof(command.InvoiceId), "Invoice is required.");
        if (command.RollEntries.Count == 0)
            return ApplicationResult.ValidationFailed(nameof(command.RollEntries), "Roll entries are required.");
        return null;
    }

    public static ApplicationResult? Validate(CreateReceiptVoucherCommand command)
    {
        var errors = new List<ValidationError>();
        if (command.CustomerId == Guid.Empty)
            errors.Add(new ValidationError(nameof(command.CustomerId), "Customer is required."));
        if (command.CashboxId == Guid.Empty)
            errors.Add(new ValidationError(nameof(command.CashboxId), "Cashbox is required."));
        if (command.Amount <= 0)
            errors.Add(new ValidationError(nameof(command.Amount), "Amount must be greater than zero."));
        return errors.Count > 0 ? ApplicationResult.ValidationFailed(errors) : null;
    }

    public static ApplicationResult? Validate(CreatePaymentVoucherCommand command)
    {
        var errors = new List<ValidationError>();
        if (command.SupplierId == Guid.Empty)
            errors.Add(new ValidationError(nameof(command.SupplierId), "Supplier is required."));
        if (command.CashboxId == Guid.Empty)
            errors.Add(new ValidationError(nameof(command.CashboxId), "Cashbox is required."));
        if (command.Amount <= 0)
            errors.Add(new ValidationError(nameof(command.Amount), "Amount must be greater than zero."));
        return errors.Count > 0 ? ApplicationResult.ValidationFailed(errors) : null;
    }

    public static ApplicationResult? Validate(CreateJournalEntryCommand command)
    {
        var errors = new List<ValidationError>();
        if (string.IsNullOrWhiteSpace(command.Description))
            errors.Add(new ValidationError(nameof(command.Description), "Description is required."));
        if (command.Lines.Count == 0)
            errors.Add(new ValidationError(nameof(command.Lines), "At least one journal line is required."));
        return errors.Count > 0 ? ApplicationResult.ValidationFailed(errors) : null;
    }
}
