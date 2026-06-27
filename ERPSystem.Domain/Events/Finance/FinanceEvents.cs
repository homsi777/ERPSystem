using ERPSystem.Domain.Common;

namespace ERPSystem.Domain.Events.Finance;

public sealed record ReceiptVoucherPosted(Guid VoucherId, string VoucherNumber, decimal Amount) : DomainEvent;

public sealed record PaymentVoucherPosted(Guid VoucherId, string VoucherNumber, decimal Amount) : DomainEvent;

public sealed record CustomerCreditLimitExceeded(Guid CustomerId, decimal RequestedAmount, decimal CreditLimit) : DomainEvent;
