using ERPSystem.Domain.Enums;

namespace ERPSystem.Application.Queries.Finance;

public sealed class GetCashboxListQuery
{
    public Guid BranchId { get; init; }
    public bool IncludeInactive { get; init; }
}

public sealed class GetCashboxDetailsQuery
{
    public Guid CashboxId { get; init; }
}

public sealed class GetCashboxMovementsQuery
{
    public Guid CashboxId { get; init; }
    public DateTime? FromDate { get; init; }
    public DateTime? ToDate { get; init; }
}

public sealed class GetCashboxTransferListQuery
{
    public Guid BranchId { get; init; }
    public VoucherStatus? Status { get; init; }
    public Guid? CashboxId { get; init; }
}

public sealed class GetCashboxOperationsCenterQuery
{
    public Guid CashboxId { get; init; }
}

public sealed class GetReceiptVoucherPrintQuery
{
    public Guid VoucherId { get; init; }
}

public sealed class GetPaymentVoucherPrintQuery
{
    public Guid VoucherId { get; init; }
}
