namespace ERPSystem.Domain.Enums;

/// <summary>Canonical payment method kinds for receipt/payment tenders.</summary>
public enum PaymentMethodKind
{
    Cash = 0,
    BankTransfer = 1,
    Card = 2,
    Cheque = 3,
    CustomerCredit = 4,
    Advance = 5,
    Other = 99
}
