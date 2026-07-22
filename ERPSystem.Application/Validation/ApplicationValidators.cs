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
        if (command.ExchangeRateToLocalCurrency <= 0)
            errors.Add(new ValidationError(nameof(command.ExchangeRateToLocalCurrency), "Exchange rate must be greater than zero."));
        if (command.Lines.Count == 0)
            errors.Add(new ValidationError(nameof(command.Lines), "At least one import line is required."));
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

    public static ApplicationResult? Validate(CreateSalesInvoiceDraftCommand command) =>
        ValidateSalesInvoiceDraft(
            command.CustomerId,
            command.WarehouseId,
            command.Lines);

    public static ApplicationResult? Validate(UpdateSalesInvoiceDraftCommand command)
    {
        if (command.InvoiceId == Guid.Empty)
            return ApplicationResult.ValidationFailed(nameof(command.InvoiceId), "Invoice is required.");

        return ValidateSalesInvoiceDraft(
            command.CustomerId,
            command.WarehouseId,
            command.Lines);
    }

    private static ApplicationResult? ValidateSalesInvoiceDraft(
        Guid customerId,
        Guid warehouseId,
        IReadOnlyList<SalesInvoiceLineCommand> lines)
    {
        var errors = new List<ValidationError>();
        if (customerId == Guid.Empty)
            errors.Add(new ValidationError("CustomerId", "Customer is required."));
        if (warehouseId == Guid.Empty)
            errors.Add(new ValidationError("WarehouseId", "Warehouse is required."));
        if (lines.Count == 0)
            errors.Add(new ValidationError("Lines", "At least one line item is required."));
        return errors.Count > 0 ? ApplicationResult.ValidationFailed(errors) : null;
    }

    public static ApplicationResult? Validate(CompleteWarehouseDetailingCommand command)
    {
        if (command.InvoiceId == Guid.Empty)
            return ApplicationResult.ValidationFailed(nameof(command.InvoiceId), "Invoice is required.");
        if (command.RollEntries.Count == 0)
            return ApplicationResult.ValidationFailed(nameof(command.RollEntries), "Roll entries are required.");

        var errors = new List<ValidationError>();
        var seenSerials = new HashSet<int>();
        for (var i = 0; i < command.RollEntries.Count; i++)
        {
            var entry = command.RollEntries[i];
            if (entry.RollDetailId == Guid.Empty)
                errors.Add(new ValidationError($"RollEntries[{i}].RollDetailId", "Roll detail is required."));

            var hasSerial = entry.RollNumber is > 0;
            var hasLength = entry.LengthMeters > 0;
            if (!hasSerial && !hasLength)
            {
                errors.Add(new ValidationError(
                    $"RollEntries[{i}]",
                    "أدخل رقم التوب (سيريال) أو الطول بالمتر."));
            }

            if (entry.RollNumber is int serial and > 0 && !seenSerials.Add(serial))
            {
                errors.Add(new ValidationError(
                    $"RollEntries[{i}].RollNumber",
                    $"رقم السيريال {serial} مكرر في نفس الفاتورة. كل توب يجب أن يحمل سيريالاً فريداً."));
            }
        }

        return errors.Count > 0 ? ApplicationResult.ValidationFailed(errors) : null;
    }

    public static ApplicationResult? Validate(SaveWarehouseDetailingDraftCommand command)
    {
        if (command.InvoiceId == Guid.Empty)
            return ApplicationResult.ValidationFailed(nameof(command.InvoiceId), "Invoice is required.");
        if (command.RollEntries.Count == 0)
            return ApplicationResult.ValidationFailed(nameof(command.RollEntries), "Roll entries are required.");

        var errors = new List<ValidationError>();
        var seenSerials = new HashSet<int>();
        for (var i = 0; i < command.RollEntries.Count; i++)
        {
            if (command.RollEntries[i].RollDetailId == Guid.Empty)
                errors.Add(new ValidationError($"RollEntries[{i}].RollDetailId", "Roll detail is required."));

            if (command.RollEntries[i].RollNumber is int serial and > 0 && !seenSerials.Add(serial))
            {
                errors.Add(new ValidationError(
                    $"RollEntries[{i}].RollNumber",
                    $"رقم السيريال {serial} مكرر في نفس الفاتورة. كل توب يجب أن يحمل سيريالاً فريداً."));
            }
        }

        return errors.Count > 0 ? ApplicationResult.ValidationFailed(errors) : null;
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
        var hasCashbox = command.CashboxId is Guid cashboxId && cashboxId != Guid.Empty;
        var hasBank = command.BankAccountId is Guid bankAccountId && bankAccountId != Guid.Empty;
        if (hasCashbox == hasBank)
            errors.Add(new ValidationError("PaymentSource", "Choose exactly one payment source: cashbox or bank account."));
        if (hasBank && string.IsNullOrWhiteSpace(command.Reference))
            errors.Add(new ValidationError(nameof(command.Reference), "Bank transfer reference is required."));
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
        if (command.CompanyId == Guid.Empty)
            errors.Add(new ValidationError(nameof(command.CompanyId), "Company is required."));
        if (command.BranchId == Guid.Empty)
            errors.Add(new ValidationError(nameof(command.BranchId), "Branch is required."));

        var debit = command.Lines.Sum(l => l.Debit);
        var credit = command.Lines.Sum(l => l.Credit);
        if (Math.Abs(debit - credit) > 0.01m)
            errors.Add(new ValidationError(nameof(command.Lines), "Journal entry must be balanced."));

        return errors.Count > 0 ? ApplicationResult.ValidationFailed(errors) : null;
    }

    public static ApplicationResult? Validate(CreateAccountCommand command)
    {
        var errors = new List<ValidationError>();
        if (command.CompanyId == Guid.Empty)
            errors.Add(new ValidationError(nameof(command.CompanyId), "Company is required."));
        if (string.IsNullOrWhiteSpace(command.Code))
            errors.Add(new ValidationError(nameof(command.Code), "Account code is required."));
        if (string.IsNullOrWhiteSpace(command.NameAr))
            errors.Add(new ValidationError(nameof(command.NameAr), "Account name is required."));
        return errors.Count > 0 ? ApplicationResult.ValidationFailed(errors) : null;
    }

    public static ApplicationResult? Validate(UpdateAccountCommand command)
    {
        var errors = new List<ValidationError>();
        if (command.AccountId == Guid.Empty)
            errors.Add(new ValidationError(nameof(command.AccountId), "Account is required."));
        if (string.IsNullOrWhiteSpace(command.Code))
            errors.Add(new ValidationError(nameof(command.Code), "Account code is required."));
        if (string.IsNullOrWhiteSpace(command.NameAr))
            errors.Add(new ValidationError(nameof(command.NameAr), "Account name is required."));
        return errors.Count > 0 ? ApplicationResult.ValidationFailed(errors) : null;
    }
}
