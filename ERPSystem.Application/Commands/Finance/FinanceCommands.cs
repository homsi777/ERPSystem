namespace ERPSystem.Application.Commands.Finance;

public sealed class CreateReceiptVoucherCommand
{
    public Guid CompanyId { get; init; }
    public Guid BranchId { get; init; }
    public Guid CustomerId { get; init; }
    public Guid CashboxId { get; init; }
    public decimal Amount { get; init; }
    public IReadOnlyList<ReceiptInvoiceAllocationInput> Allocations { get; init; } = [];
}

public sealed class ReceiptInvoiceAllocationInput
{
    public Guid SalesInvoiceId { get; init; }
    public decimal Amount { get; init; }
}

public sealed class ApproveReceiptVoucherCommand
{
    public Guid VoucherId { get; init; }
}

public sealed class PostReceiptVoucherCommand
{
    public Guid VoucherId { get; init; }
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
