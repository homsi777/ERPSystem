namespace ERPSystem.Application.Commands.Finance;

using ERPSystem.Application.Common;

public sealed class CreateReceiptVoucherCommand
{
    public Guid CompanyId { get; init; }
    public Guid BranchId { get; init; }
    public Guid CustomerId { get; init; }
    public Guid CashboxId { get; init; }
    public Guid PaymentMethodId { get; init; } = PaymentMethodIds.Cash;
    public decimal Amount { get; init; }
    public string Currency { get; init; } = "USD";
    public decimal ExchangeRate { get; init; } = 1m;
    public Guid? BankAccountId { get; init; }
    public string? Reference { get; init; }
    public IReadOnlyList<ReceiptInvoiceAllocationInput> Allocations { get; init; } = [];
}

public sealed class ReceiptInvoiceAllocationInput
{
    public Guid SalesInvoiceId { get; init; }
    public decimal Amount { get; init; }
}

public sealed class PostReceiptVoucherCommand
{
    public Guid VoucherId { get; init; }
    public string? IdempotencyKey { get; init; }
}

public sealed class ApproveReceiptVoucherCommand
{
    public Guid VoucherId { get; init; }
}

public sealed class CancelReceiptVoucherCommand
{
    public Guid VoucherId { get; init; }
    public string Reason { get; init; } = "";
}

public sealed class ReverseReceiptVoucherCommand
{
    public Guid ReceiptVoucherId { get; init; }
    public DateTime ReversalDate { get; init; } = DateTime.UtcNow;
    public string Reason { get; init; } = "";
    public Guid UserId { get; init; }
    public string? IdempotencyKey { get; init; }
}

public sealed class CreatePaymentVoucherCommand
{
    public Guid CompanyId { get; init; }
    public Guid BranchId { get; init; }
    public Guid SupplierId { get; init; }
    public Guid CashboxId { get; init; }
    public decimal Amount { get; init; }
}

public sealed class ApprovePaymentVoucherCommand
{
    public Guid VoucherId { get; init; }
}

public sealed class PostPaymentVoucherCommand
{
    public Guid VoucherId { get; init; }
    public Guid? PurchaseInvoiceId { get; init; }
}

public sealed class CreateCashboxCommand
{
    public Guid CompanyId { get; init; }
    public Guid BranchId { get; init; }
    public string Code { get; init; } = "";
    public string Name { get; init; } = "";
    public string Currency { get; init; } = "USD";
}

public sealed class UpdateCashboxCommand
{
    public Guid CashboxId { get; init; }
    public string Code { get; init; } = "";
    public string Name { get; init; } = "";
    public string Currency { get; init; } = "USD";
}

public sealed class DeactivateCashboxCommand
{
    public Guid CashboxId { get; init; }
}

public sealed class ActivateCashboxCommand
{
    public Guid CashboxId { get; init; }
}

public sealed class CreateCashboxTransferCommand
{
    public Guid CompanyId { get; init; }
    public Guid BranchId { get; init; }
    public Guid FromCashboxId { get; init; }
    public Guid ToCashboxId { get; init; }
    public decimal Amount { get; init; }
    public string? Notes { get; init; }
    public bool PostImmediately { get; init; } = true;
}

public sealed class PostCashboxTransferCommand
{
    public Guid TransferId { get; init; }
}
