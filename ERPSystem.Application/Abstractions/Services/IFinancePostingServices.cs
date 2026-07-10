using ERPSystem.Application.DTOs.Finance;

namespace ERPSystem.Application.Abstractions.Services;

public sealed record CashboxPostingValidationResult(bool IsValid, string? ErrorMessage, Guid? ResolvedAccountId);

public interface ICashboxPostingValidator
{
    Task<CashboxPostingValidationResult> ValidateForReceiptAsync(
        Guid companyId,
        Guid cashboxId,
        string tenderCurrency,
        CancellationToken cancellationToken = default);
}

public interface IBankAccountPostingValidator
{
    Task<CashboxPostingValidationResult> ValidateForReceiptAsync(
        Guid companyId,
        Guid bankAccountId,
        string tenderCurrency,
        string? reference,
        CancellationToken cancellationToken = default);
}

public interface IReceiptTenderResolver
{
    Task<(Guid DebitAccountId, bool IsAdvance)> ResolveDebitAccountAsync(
        Guid companyId,
        ReceiptTenderLineDto tender,
        CancellationToken cancellationToken = default);
}

public interface ICashboxBalanceService
{
    Task<CashboxBalanceReportDto> GetBalanceAsync(Guid cashboxId, CancellationToken cancellationToken = default);
}

public interface ICashboxReconciliationService
{
    Task<IReadOnlyList<CashboxReconciliationRowDto>> GetReconciliationAsync(
        Guid companyId,
        CancellationToken cancellationToken = default);
}

public interface IReceiptPostingService
{
    Task PostReceiptCollectionAsync(
        Guid voucherId,
        string voucherNumber,
        Guid companyId,
        Guid customerId,
        IReadOnlyList<ReceiptTenderLineDto> tenders,
        decimal allocatedAmount,
        decimal totalAmount,
        CancellationToken cancellationToken = default);

    Task PostReceiptReversalAsync(
        Guid reversalVoucherId,
        string reversalVoucherNumber,
        Guid companyId,
        Guid customerId,
        Guid originalVoucherId,
        IReadOnlyList<ReceiptTenderLineDto> tenders,
        CancellationToken cancellationToken = default);
}
