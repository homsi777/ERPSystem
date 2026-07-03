using ERPSystem.Application.Abstractions;
using ERPSystem.Application.Abstractions.Repositories;
using ERPSystem.Application.Abstractions.Services;
using ERPSystem.Application.Common;
using ERPSystem.Domain.Aggregates;
using ERPSystem.Domain.Entities.Accounting;
using ERPSystem.Domain.Enums;
using ERPSystem.Domain.ValueObjects;
using ERPSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ERPSystem.Infrastructure.Services;

internal sealed class IntegratedAccountingService(
    ErpDbContext context,
    IJournalEntryRepository journalRepository,
    INumberingService numberingService,
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUserService,
    ICurrentBranchService currentBranchService) : IIntegratedAccountingService
{
    public Task PostContainerApprovalAsync(ContainerAggregate container, CancellationToken cancellationToken = default)
    {
        if (container.LandingCost is null)
            return Task.CompletedTask;

        var totalExpenses = container.LandingCost.TotalImportExpenses.Amount * container.ExchangeRateToLocalCurrency;
        if (totalExpenses <= 0)
            return Task.CompletedTask;

        return PostIfNotExistsAsync(
            DocumentType.ChinaContainer,
            container.Id,
            "اعتماد حاوية",
            JournalBookIds.Purchase,
            $"اعتماد حاوية {container.ContainerNumber.Value} — تكاليف وصول",
            [
                (AccountingAccountIds.LandingCostClearing, totalExpenses, 0m, "تكاليف وصول معلقة", null),
                (AccountingAccountIds.AccountsPayable, 0m, totalExpenses, "ذمم مورد استيراد", null)
            ],
            cancellationToken);
    }

    public Task PostInventoryActivationAsync(
        ContainerAggregate container,
        Guid warehouseId,
        decimal inventoryValue,
        CancellationToken cancellationToken = default)
    {
        if (inventoryValue <= 0)
            return Task.CompletedTask;

        return PostIfNotExistsAsync(
            DocumentType.ChinaContainer,
            container.Id,
            "تفعيل مخزون",
            JournalBookIds.General,
            $"تفعيل مخزون حاوية {container.ContainerNumber.Value}",
            [
                (AccountingAccountIds.InventoryAsset, inventoryValue, 0m, "أصول مخزون أقمشة", null),
                (AccountingAccountIds.LandingCostClearing, 0m, inventoryValue, "ترحيل تكلفة وصول للمخزون", null)
            ],
            cancellationToken);
    }

    public async Task PostSalesInvoiceApprovalAsync(
        SalesInvoiceAggregate invoice,
        decimal cogsAmount,
        CancellationToken cancellationToken = default)
    {
        var revenue = invoice.GrandTotal.Amount;
        var lines = new List<(Guid, decimal, decimal, string, Guid?)>
        {
            (AccountingAccountIds.AccountsReceivable, revenue, 0m, "ذمم عميل — فاتورة بيع", invoice.CustomerId),
            (AccountingAccountIds.SalesRevenue, 0m, revenue, "إيراد مبيعات أقمشة", null)
        };

        if (cogsAmount > 0)
        {
            lines.Add((AccountingAccountIds.CostOfGoodsSold, cogsAmount, 0m, "تكلفة مبيعات", null));
            lines.Add((AccountingAccountIds.InventoryAsset, 0m, cogsAmount, "صرف مخزون", null));
        }

        await PostIfNotExistsAsync(
            DocumentType.SalesInvoice,
            invoice.Id,
            null,
            JournalBookIds.Sales,
            $"فاتورة بيع {invoice.InvoiceNumber.Value}",
            lines,
            cancellationToken);
    }

    public Task PostReceiptVoucherAsync(
        Guid voucherId,
        string voucherNumber,
        Guid customerId,
        decimal amount,
        CancellationToken cancellationToken = default) =>
        PostIfNotExistsAsync(
            DocumentType.ReceiptVoucher,
            voucherId,
            null,
            JournalBookIds.Cash,
            $"سند قبض {voucherNumber}",
            [
                (AccountingAccountIds.CashUsd, amount, 0m, "تحصيل نقدي", null),
                (AccountingAccountIds.AccountsReceivable, 0m, amount, "تسوية ذمم عميل", customerId)
            ],
            cancellationToken);

    public Task PostPaymentVoucherAsync(
        Guid voucherId,
        string voucherNumber,
        Guid supplierId,
        decimal amount,
        CancellationToken cancellationToken = default) =>
        PostIfNotExistsAsync(
            DocumentType.PaymentVoucher,
            voucherId,
            null,
            JournalBookIds.Cash,
            $"سند صرف {voucherNumber}",
            [
                (AccountingAccountIds.AccountsPayable, amount, 0m, "سداد مورد", supplierId),
                (AccountingAccountIds.CashUsd, 0m, amount, "صرف نقدي", null)
            ],
            cancellationToken);

    public Task PostExpensePaymentAsync(
        Guid expenseId,
        Guid paymentId,
        decimal amountBase,
        string description,
        CancellationToken cancellationToken = default)
    {
        if (amountBase <= 0)
            return Task.CompletedTask;

        return PostIfNotExistsAsync(
            DocumentType.ExpensePayment,
            paymentId,
            null,
            JournalBookIds.Cash,
            description,
            [
                (AccountingAccountIds.OperatingExpenses, amountBase, 0m, description, null),
                (AccountingAccountIds.CashUsd, 0m, amountBase, "صرف مصروف", null)
            ],
            cancellationToken);
    }

    private async Task PostIfNotExistsAsync(
        DocumentType sourceType,
        Guid sourceId,
        string? descriptionContains,
        Guid journalBookId,
        string description,
        IReadOnlyList<(Guid AccountId, decimal Debit, decimal Credit, string Narrative, Guid? PartyId)> lines,
        CancellationToken cancellationToken)
    {
        var query = context.JournalEntries.AsNoTracking()
            .Where(j => j.SourceType == (int)sourceType && j.SourceId == sourceId);

        if (!string.IsNullOrWhiteSpace(descriptionContains))
            query = query.Where(j => j.Description.Contains(descriptionContains));

        if (await query.AnyAsync(cancellationToken))
            return;

        var activeLines = lines.Where(l => l.Debit > 0 || l.Credit > 0).ToList();
        if (activeLines.Count == 0)
            return;

        await CreateAndPostJournalAsync(description, sourceType, sourceId, journalBookId, activeLines, cancellationToken);
    }

    private async Task CreateAndPostJournalAsync(
        string description,
        DocumentType sourceType,
        Guid sourceId,
        Guid journalBookId,
        IReadOnlyList<(Guid AccountId, decimal Debit, decimal Credit, string Narrative, Guid? PartyId)> lines,
        CancellationToken cancellationToken)
    {
        var branchId = currentBranchService.BranchId ?? Guid.Empty;
        var companyId = currentBranchService.CompanyId ?? Guid.Empty;
        if (branchId == Guid.Empty || companyId == Guid.Empty)
            return;

        var accountIds = lines.Select(l => l.AccountId).Distinct().ToList();
        var hasAccounts = await context.Accounts.AsNoTracking()
            .CountAsync(a => accountIds.Contains(a.Id), cancellationToken);
        if (hasAccounts < accountIds.Count)
            return;

        var entryNumber = await numberingService.NextJournalEntryNumberAsync(branchId, cancellationToken);
        var userId = currentUserService.UserId ?? Guid.Empty;
        var aggregate = AccountingAggregate.CreateDraft(
            entryNumber,
            DateTime.UtcNow,
            description,
            userId,
            sourceType,
            sourceId,
            journalBookId);

        foreach (var line in lines)
        {
            aggregate.AddLine(JournalEntryLine.Create(
                line.AccountId,
                new Money(line.Debit),
                new Money(line.Credit),
                line.Narrative,
                line.PartyId));
        }

        aggregate.Post(userId);
        await journalRepository.AddAsync(aggregate, companyId, branchId, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
