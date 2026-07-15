using ERPSystem.Application.Abstractions.Repositories;
using ERPSystem.Application.Abstractions.Services;
using ERPSystem.Application.Common;
using ERPSystem.Application.DTOs.Finance;
using ERPSystem.Domain.Enums;
using ERPSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ERPSystem.Infrastructure.Services;

internal sealed class CashboxPostingValidator(ErpDbContext context) : ICashboxPostingValidator
{
    public async Task<CashboxPostingValidationResult> ValidateForReceiptAsync(
        Guid companyId,
        Guid cashboxId,
        string tenderCurrency,
        CancellationToken cancellationToken = default)
    {
        var cashbox = await context.Cashboxes.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == cashboxId, cancellationToken);
        if (cashbox is null)
            return new(false, "الصندوق غير موجود.", null);

        var branchCompanyId = await context.Branches.AsNoTracking()
            .Where(b => b.Id == cashbox.BranchId)
            .Select(b => b.CompanyId)
            .FirstOrDefaultAsync(cancellationToken);
        if (branchCompanyId != companyId)
            return new(false, "الصندوق لا ينتمي لنفس الشركة.", null);

        if (!cashbox.IsActive)
            return new(false, $"الصندوق '{cashbox.Name}' غير نشط.", null);

        if (cashbox.AccountId is not Guid accountId || accountId == Guid.Empty)
            return new(false, $"الصندوق '{cashbox.Code}' لا يملك حساب GL مرتبط.", null);

        var account = await context.Accounts.AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == accountId, cancellationToken);
        if (account is null)
            return new(false, $"حساب GL للصندوق '{cashbox.Code}' غير موجود.", null);
        if (account.CompanyId != companyId)
            return new(false, $"حساب GL للصندوق '{cashbox.Code}' لا ينتمي لنفس الشركة.", null);
        if (!account.IsActive)
            return new(false, $"حساب GL للصندوق '{cashbox.Code}' غير نشط.", null);

        if (!string.Equals(cashbox.Currency, tenderCurrency, StringComparison.OrdinalIgnoreCase))
            return new(false, $"عملة الصندوق '{cashbox.Currency}' لا تطابق عملة السند '{tenderCurrency}'.", null);

        return new(true, null, accountId);
    }
}

internal sealed class BankAccountPostingValidator(ErpDbContext context) : IBankAccountPostingValidator
{
    public async Task<CashboxPostingValidationResult> ValidateForReceiptAsync(
        Guid companyId,
        Guid bankAccountId,
        string tenderCurrency,
        string? reference,
        CancellationToken cancellationToken = default)
    {
        var bank = await context.BankAccounts.AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == bankAccountId, cancellationToken);
        if (bank is null)
            return new(false, "الحساب البنكي غير موجود.", null);
        if (bank.CompanyId != companyId)
            return new(false, "الحساب البنكي لا ينتمي لنفس الشركة.", null);
        if (!bank.IsActive)
            return new(false, $"الحساب البنكي '{bank.Name}' غير نشط.", null);
        if (bank.GlAccountId == Guid.Empty)
            return new(false, $"الحساب البنكي '{bank.Code}' لا يملك حساب GL.", null);
        if (string.IsNullOrWhiteSpace(reference))
            return new(false, $"الحساب البنكي '{bank.Code}' يتطلب مرجع تحويل.", null);
        if (!string.Equals(bank.Currency, tenderCurrency, StringComparison.OrdinalIgnoreCase))
            return new(false, $"عملة البنك '{bank.Currency}' لا تطابق عملة السند.", null);

        var account = await context.Accounts.AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == bank.GlAccountId, cancellationToken);
        if (account is null || account.CompanyId != companyId || !account.IsActive)
            return new(false, $"حساب GL للبنك '{bank.Code}' غير صالح.", null);

        return new(true, null, bank.GlAccountId);
    }
}

internal sealed class ReceiptTenderResolver(
    ErpDbContext context,
    ICashboxPostingValidator cashboxValidator,
    IBankAccountPostingValidator bankValidator) : IReceiptTenderResolver
{
    public async Task<(Guid DebitAccountId, bool IsAdvance)> ResolveDebitAccountAsync(
        Guid companyId,
        ReceiptTenderLineDto tender,
        CancellationToken cancellationToken = default)
    {
        var method = await context.PaymentMethods.AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == tender.PaymentMethodId && m.CompanyId == companyId, cancellationToken)
            ?? throw new InvalidOperationException("Payment method not found.");

        if (method.RequiresBankAccount)
        {
            if (tender.BankAccountId is not Guid bankId)
                throw new InvalidOperationException("Bank account is required for this payment method.");
            var bankResult = await bankValidator.ValidateForReceiptAsync(
                companyId, bankId, tender.Currency, tender.Reference, cancellationToken);
            if (!bankResult.IsValid)
                throw new InvalidOperationException(bankResult.ErrorMessage);
            return (bankResult.ResolvedAccountId!.Value, method.Kind == (int)PaymentMethodKind.Advance);
        }

        if (method.RequiresCashbox)
        {
            if (tender.CashboxId is not Guid cashboxId)
                throw new InvalidOperationException("Cashbox is required for this payment method.");
            var cashResult = await cashboxValidator.ValidateForReceiptAsync(
                companyId, cashboxId, tender.Currency, cancellationToken);
            if (!cashResult.IsValid)
                throw new InvalidOperationException(cashResult.ErrorMessage);
            return (cashResult.ResolvedAccountId!.Value, method.Kind == (int)PaymentMethodKind.Advance);
        }

        if (tender.CashboxId is Guid fallbackCashboxId)
        {
            var cashResult = await cashboxValidator.ValidateForReceiptAsync(
                companyId, fallbackCashboxId, tender.Currency, cancellationToken);
            if (cashResult.IsValid)
                return (cashResult.ResolvedAccountId!.Value, method.Kind == (int)PaymentMethodKind.Advance);
        }

        if (method.Kind == (int)PaymentMethodKind.CustomerCredit)
            throw new InvalidOperationException(
                "وسيلة «رصيد عميل» للمبيعات فقط — اختر نقدي أو تحويل بنكي لسند القبض.");

        throw new InvalidOperationException($"Payment method '{method.Code}' is not configured for receipt posting.");
    }
}

internal sealed class ReceiptPostingService(
    IIntegratedAccountingService accounting,
    IReceiptTenderResolver tenderResolver) : IReceiptPostingService
{
    public async Task PostReceiptCollectionAsync(
        Guid voucherId,
        string voucherNumber,
        Guid companyId,
        Guid customerId,
        IReadOnlyList<ReceiptTenderLineDto> tenders,
        decimal allocatedAmount,
        decimal totalAmount,
        CancellationToken cancellationToken = default)
    {
        if (tenders.Count != 1)
            throw new InvalidOperationException("Phase 3 supports a single tender line per receipt.");

        var tender = tenders[0];
        var (debitAccountId, _) = await tenderResolver.ResolveDebitAccountAsync(companyId, tender, cancellationToken);
        await accounting.PostReceiptVoucherAsync(
            voucherId, voucherNumber, customerId, debitAccountId,
            totalAmount, allocatedAmount, isReversal: false, cancellationToken: cancellationToken);
    }

    public async Task PostReceiptReversalAsync(
        Guid reversalVoucherId,
        string reversalVoucherNumber,
        Guid companyId,
        Guid customerId,
        Guid originalVoucherId,
        IReadOnlyList<ReceiptTenderLineDto> tenders,
        CancellationToken cancellationToken = default)
    {
        if (tenders.Count != 1)
            throw new InvalidOperationException("Phase 3 supports a single tender line per receipt reversal.");

        var tender = tenders[0];
        var (debitAccountId, _) = await tenderResolver.ResolveDebitAccountAsync(companyId, tender, cancellationToken);
        await accounting.PostReceiptVoucherAsync(
            reversalVoucherId, reversalVoucherNumber, customerId, debitAccountId,
            tender.Amount, tender.Amount, isReversal: true, originalVoucherId, cancellationToken);
    }
}

internal sealed class CashboxBalanceService(ErpDbContext context) : ICashboxBalanceService
{
    public async Task<CashboxBalanceReportDto> GetBalanceAsync(
        Guid cashboxId,
        CancellationToken cancellationToken = default)
    {
        var cashbox = await context.Cashboxes.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == cashboxId, cancellationToken)
            ?? throw new InvalidOperationException("Cashbox not found.");

        decimal glBalance = 0m;
        if (cashbox.AccountId is Guid accountId)
        {
            var sums = await context.JournalEntryLines.AsNoTracking()
                .Where(l => l.AccountId == accountId)
                .Join(context.JournalEntries.AsNoTracking().Where(j => j.Status == (int)JournalEntryStatus.Posted),
                    l => l.JournalEntryId, j => j.Id, (l, _) => l)
                .GroupBy(_ => 1)
                .Select(g => new { Debit = g.Sum(x => x.Debit), Credit = g.Sum(x => x.Credit) })
                .FirstOrDefaultAsync(cancellationToken);
            if (sums is not null)
                glBalance = sums.Debit - sums.Credit;
        }

        var posted = (int)VoucherStatus.Posted;
        var reversed = (int)VoucherStatus.Reversed;
        var receipts = await context.ReceiptVouchers.AsNoTracking()
            .Where(v => v.CashboxId == cashboxId && v.Status == posted)
            .SumAsync(v => v.Amount, cancellationToken);
        var reversals = await context.ReceiptVouchers.AsNoTracking()
            .Where(v => v.CashboxId == cashboxId && v.Status == reversed)
            .SumAsync(v => v.Amount, cancellationToken);
        var payments = await context.PaymentVouchers.AsNoTracking()
            .Where(v => v.CashboxId == cashboxId && v.Status == posted)
            .SumAsync(v => v.Amount, cancellationToken);

        return new CashboxBalanceReportDto
        {
            CashboxId = cashbox.Id,
            CashboxCode = cashbox.Code,
            CashboxName = cashbox.Name,
            AccountId = cashbox.AccountId,
            OpeningBalance = 0m,
            PostedReceipts = receipts,
            PostedPayments = payments,
            Reversals = reversals,
            GlBalance = glBalance,
            OperationalBalance = cashbox.Balance,
            Difference = cashbox.Balance - glBalance
        };
    }
}

internal sealed class CashboxReconciliationService(ErpDbContext context) : ICashboxReconciliationService
{
    public async Task<IReadOnlyList<CashboxReconciliationRowDto>> GetReconciliationAsync(
        Guid companyId,
        CancellationToken cancellationToken = default)
    {
        var branchIds = await context.Branches.AsNoTracking()
            .Where(b => b.CompanyId == companyId)
            .Select(b => b.Id)
            .ToListAsync(cancellationToken);

        var cashboxes = await context.Cashboxes.AsNoTracking()
            .Where(c => branchIds.Contains(c.BranchId))
            .OrderBy(c => c.Code)
            .ToListAsync(cancellationToken);

        var posted = (int)VoucherStatus.Posted;
        var reversed = (int)VoucherStatus.Reversed;
        var rows = new List<CashboxReconciliationRowDto>();

        foreach (var c in cashboxes)
        {
            decimal? glBalance = null;
            string classification;
            if (c.AccountId is not Guid accountId || accountId == Guid.Empty)
            {
                classification = "MissingAccount";
            }
            else
            {
                var account = await context.Accounts.AsNoTracking()
                    .FirstOrDefaultAsync(a => a.Id == accountId, cancellationToken);
                if (account is null)
                    classification = "MissingGL";
                else if (account.CompanyId != companyId)
                    classification = "CrossCompanyAccount";
                else
                {
                    var sums = await context.JournalEntryLines.AsNoTracking()
                        .Where(l => l.AccountId == accountId)
                        .Join(context.JournalEntries.AsNoTracking()
                                .Where(j => j.CompanyId == companyId && j.Status == (int)JournalEntryStatus.Posted),
                            l => l.JournalEntryId, j => j.Id, (l, _) => l)
                        .GroupBy(_ => 1)
                        .Select(g => g.Sum(x => x.Debit) - g.Sum(x => x.Credit))
                        .FirstOrDefaultAsync(cancellationToken);
                    glBalance = sums;
                    classification = Math.Abs(c.Balance - glBalance.Value) < 0.01m ? "Matched" : "Difference";
                }
            }

            var unposted = await context.ReceiptVouchers.AsNoTracking()
                .CountAsync(v => v.CashboxId == c.Id && v.Status != posted && v.Status != reversed, cancellationToken);
            var reversedCount = await context.ReceiptVouchers.AsNoTracking()
                .CountAsync(v => v.CashboxId == c.Id && v.Status == reversed, cancellationToken);
            var last = await context.ReceiptVouchers.AsNoTracking()
                .Where(v => v.CashboxId == c.Id && v.PostedAt != null)
                .OrderByDescending(v => v.PostedAt)
                .Select(v => v.VoucherNumber)
                .FirstOrDefaultAsync(cancellationToken);

            rows.Add(new CashboxReconciliationRowDto
            {
                CashboxId = c.Id,
                CashboxCode = c.Code,
                CashboxName = c.Name,
                CashboxAccountId = c.AccountId,
                Currency = c.Currency,
                OperationalBalance = c.Balance,
                GlBalance = glBalance,
                Difference = glBalance.HasValue ? c.Balance - glBalance : null,
                Classification = classification,
                LastTransaction = last,
                UnpostedVoucherCount = unposted,
                ReversedVoucherCount = reversedCount
            });
        }

        return rows;
    }
}
