using ERPSystem.Application.Abstractions;
using ERPSystem.Application.Abstractions.Repositories;
using ERPSystem.Application.Abstractions.Services;
using ERPSystem.Application.Common;
using ERPSystem.Application.Posting;
using ERPSystem.Domain.Aggregates;
using ERPSystem.Domain.Entities.Purchasing;
using ERPSystem.Domain.Enums;
using ERPSystem.Domain.Exceptions;
using ERPSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ERPSystem.Infrastructure.Services;

internal sealed class IntegratedAccountingService(
    ErpDbContext context,
    IAccountingPostingEngine postingEngine,
    ICurrentUserService currentUserService,
    ICurrentBranchService currentBranchService) : IIntegratedAccountingService
{
    private readonly List<PostingRequest> _pendingRecoveryRequests = [];

    /// <summary>Posting requests created since last consume — used for unique-violation recovery on SaveChanges.</summary>
    public IReadOnlyList<PostingRequest> ConsumePendingPostingRequests()
    {
        var copy = _pendingRecoveryRequests.ToList();
        _pendingRecoveryRequests.Clear();
        return copy;
    }

    public Task PostContainerApprovalAsync(ContainerAggregate container, CancellationToken cancellationToken = default)
    {
        if (container.LandingCost is null)
            return Task.CompletedTask;

        var totalExpenses = container.LandingCost.TotalImportExpenses.Amount * container.ExchangeRateToLocalCurrency;
        if (totalExpenses <= 0)
            return Task.CompletedTask;

        return PostViaEngineAsync(
            DocumentType.ChinaContainer,
            container.Id,
            PostingKind.ChinaContainerLandingCost,
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

        return PostViaEngineAsync(
            DocumentType.ChinaContainer,
            container.Id,
            PostingKind.ChinaContainerInventoryActivation,
            JournalBookIds.General,
            $"تفعيل مخزون حاوية {container.ContainerNumber.Value}",
            [
                (AccountingAccountIds.InventoryAsset, inventoryValue, 0m, "أصول مخزون أقمشة", null),
                (AccountingAccountIds.LandingCostClearing, 0m, inventoryValue, "ترحيل تكلفة وصول للمخزون", null)
            ],
            cancellationToken);
    }

    public Task PostSalesInvoiceApprovalAsync(
        SalesInvoiceAggregate invoice,
        decimal cogsAmount,
        CancellationToken cancellationToken = default)
    {
        var netReceivable = invoice.GrandTotal.Amount;
        var lineDiscount = invoice.TotalLineDiscount.Amount;
        var grossRevenue = netReceivable + lineDiscount;

        var lines = new List<(Guid, decimal, decimal, string, Guid?)>
        {
            (AccountingAccountIds.AccountsReceivable, netReceivable, 0m, "ذمم عميل — فاتورة بيع", invoice.CustomerId),
            (AccountingAccountIds.SalesRevenue, 0m, grossRevenue, "إيراد مبيعات أقمشة", null)
        };

        if (lineDiscount > 0)
            lines.Add((AccountingAccountIds.SalesDiscounts, lineDiscount, 0m, "خصم مبيعات ممنوح", invoice.CustomerId));

        if (cogsAmount > 0)
        {
            lines.Add((AccountingAccountIds.CostOfGoodsSold, cogsAmount, 0m, "تكلفة مبيعات", null));
            lines.Add((AccountingAccountIds.InventoryAsset, 0m, cogsAmount, "صرف مخزون", null));
        }

        return PostViaEngineAsync(
            DocumentType.SalesInvoice,
            invoice.Id,
            PostingKind.SalesInvoicePosting,
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
        PostViaEngineAsync(
            DocumentType.ReceiptVoucher,
            voucherId,
            PostingKind.ReceiptVoucher,
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
        Guid payablesAccountId,
        Guid cashAccountId,
        decimal amount,
        CancellationToken cancellationToken = default) =>
        PostViaEngineAsync(
            DocumentType.PaymentVoucher,
            voucherId,
            PostingKind.PaymentVoucher,
            JournalBookIds.Cash,
            $"سند صرف {voucherNumber}",
            [
                (payablesAccountId, amount, 0m, "سداد مورد", supplierId),
                (cashAccountId, 0m, amount, "صرف نقدي", null)
            ],
            cancellationToken);

    public async Task<string> PostPurchaseInvoiceAsync(
        PurchaseInvoice invoice,
        Guid payablesAccountId,
        CancellationToken cancellationToken = default)
    {
        var lines = new List<(Guid AccountId, decimal Debit, decimal Credit, string Narrative, Guid? PartyId)>();
        foreach (var item in invoice.Items)
        {
            if (item.LineType == PurchaseLineType.Inventory)
            {
                lines.Add((AccountingAccountIds.InventoryAsset, item.LineTotal.Amount, 0m,
                    $"مشتريات مخزون — {item.Description}", null));
            }
            else
            {
                var accountId = item.ExpenseAccountId ?? AccountingAccountIds.OperatingExpenses;
                lines.Add((accountId, item.LineTotal.Amount, 0m, item.Description, null));
            }
        }

        lines.Add((payablesAccountId, 0m, invoice.TotalAmount.Amount,
            $"فاتورة شراء {invoice.InvoiceNumber}", invoice.SupplierId));

        var result = await PostViaEngineAsync(
            DocumentType.PurchaseInvoice,
            invoice.Id,
            PostingKind.PurchaseInvoice,
            JournalBookIds.Purchase,
            $"فاتورة شراء {invoice.InvoiceNumber}",
            lines,
            cancellationToken,
            invoice.InvoiceDate);

        return result.JournalEntryNumber
            ?? await LookupEntryNumberAsync(DocumentType.PurchaseInvoice, invoice.Id, cancellationToken);
    }

    public async Task<string> PostSalesReturnAsync(
        SalesReturnAggregate salesReturn,
        decimal cogsReversalAmount,
        CancellationToken cancellationToken = default)
    {
        var revenueReversal = salesReturn.TotalAmount.Amount;
        var lines = new List<(Guid, decimal, decimal, string, Guid?)>
        {
            (AccountingAccountIds.SalesRevenue, revenueReversal, 0m, "عكس إيراد مبيعات — مرتجع", null),
            (AccountingAccountIds.AccountsReceivable, 0m, revenueReversal, "خصم ذمم عميل — مرتجع بيع", salesReturn.CustomerId)
        };

        if (cogsReversalAmount > 0)
        {
            lines.Add((AccountingAccountIds.InventoryAsset, cogsReversalAmount, 0m, "إعادة مخزون — مرتجع بيع", null));
            lines.Add((AccountingAccountIds.CostOfGoodsSold, 0m, cogsReversalAmount, "عكس تكلفة مبيعات — مرتجع", null));
        }

        var result = await PostViaEngineAsync(
            DocumentType.SalesReturn,
            salesReturn.Id,
            PostingKind.SalesReturn,
            JournalBookIds.Sales,
            $"مرتجع مبيعات {salesReturn.ReturnNumber} — فاتورة {salesReturn.OriginalInvoiceNumber}",
            lines,
            cancellationToken,
            salesReturn.ReturnDate);

        return result.JournalEntryNumber
            ?? await LookupEntryNumberAsync(DocumentType.SalesReturn, salesReturn.Id, cancellationToken)
            ?? "";
    }

    public async Task<string> PostPurchaseReturnAsync(
        PurchaseReturn purchaseReturn,
        Guid payablesAccountId,
        CancellationToken cancellationToken = default)
    {
        var lines = new List<(Guid AccountId, decimal Debit, decimal Credit, string Narrative, Guid? PartyId)>();
        foreach (var item in purchaseReturn.Lines)
        {
            if (item.LineType == PurchaseLineType.Inventory)
                lines.Add((AccountingAccountIds.InventoryAsset, 0m, item.LineTotal.Amount,
                    "مرتجع مشتريات — مخزون", null));
            else
                lines.Add((AccountingAccountIds.OperatingExpenses, 0m, item.LineTotal.Amount, "مرتجع مشتريات — مصروف", null));
        }

        lines.Add((payablesAccountId, purchaseReturn.TotalAmount.Amount, 0m,
            $"مرتجع شراء {purchaseReturn.ReturnNumber}", null));

        var result = await PostViaEngineAsync(
            DocumentType.PurchaseReturn,
            purchaseReturn.Id,
            PostingKind.PurchaseReturn,
            JournalBookIds.Purchase,
            $"مرتجع شراء {purchaseReturn.ReturnNumber}",
            lines,
            cancellationToken,
            purchaseReturn.ReturnDate);

        return result.JournalEntryNumber
            ?? await LookupEntryNumberAsync(DocumentType.PurchaseReturn, purchaseReturn.Id, cancellationToken);
    }

    public async Task<string> ReversePurchaseInvoiceAsync(
        PurchaseInvoice invoice,
        Guid payablesAccountId,
        CancellationToken cancellationToken = default)
    {
        var lines = new List<(Guid AccountId, decimal Debit, decimal Credit, string Narrative, Guid? PartyId)>();
        foreach (var item in invoice.Items)
        {
            if (item.LineType == PurchaseLineType.Inventory)
                lines.Add((AccountingAccountIds.InventoryAsset, 0m, item.LineTotal.Amount,
                    $"عكس مشتريات مخزون — {item.Description}", null));
            else
            {
                var accountId = item.ExpenseAccountId ?? AccountingAccountIds.OperatingExpenses;
                lines.Add((accountId, 0m, item.LineTotal.Amount, $"عكس — {item.Description}", null));
            }
        }

        lines.Add((payablesAccountId, invoice.TotalAmount.Amount, 0m,
            $"إلغاء فاتورة شراء {invoice.InvoiceNumber}", invoice.SupplierId));

        var result = await PostViaEngineAsync(
            DocumentType.PurchaseInvoiceReversal,
            invoice.Id,
            PostingKind.PurchaseInvoiceReversal,
            JournalBookIds.Purchase,
            $"إلغاء فاتورة شراء {invoice.InvoiceNumber}",
            lines,
            cancellationToken,
            DateTime.UtcNow);

        return result.JournalEntryNumber
            ?? await LookupEntryNumberAsync(DocumentType.PurchaseInvoiceReversal, invoice.Id, cancellationToken)
            ?? "";
    }

    public async Task<string> PostCashboxTransferAsync(
        Guid transferId,
        string transferNumber,
        Guid fromAccountId,
        Guid toAccountId,
        decimal amount,
        DateTime transferDate,
        CancellationToken cancellationToken = default)
    {
        if (amount <= 0)
            return "";

        var result = await PostViaEngineAsync(
            DocumentType.CashboxTransfer,
            transferId,
            PostingKind.CashboxTransfer,
            JournalBookIds.Cash,
            $"تحويل بين الصناديق {transferNumber}",
            [
                (toAccountId, amount, 0m, "تحويل وارد — صندوق مستلم", null),
                (fromAccountId, 0m, amount, "تحويل صادر — صندوق مُرسل", null)
            ],
            cancellationToken,
            transferDate);

        return result.JournalEntryNumber
            ?? await LookupEntryNumberAsync(DocumentType.CashboxTransfer, transferId, cancellationToken)
            ?? "";
    }

    [Obsolete("Use IOpeningBalanceEngine.PostPartyOpeningBalanceAsync via OpeningBalanceUiService.")]
    public async Task<string> PostCustomerOpeningBalanceAsync(
        Guid customerId,
        Guid receivablesAccountId,
        decimal amount,
        DateTime postingDate,
        string referenceNote,
        CancellationToken cancellationToken = default)
    {
        if (amount <= 0)
            return "";

        var note = string.IsNullOrWhiteSpace(referenceNote) ? "رصيد افتتاحي عميل" : referenceNote.Trim();
        var result = await PostViaEngineAsync(
            DocumentType.CustomerOpeningBalance,
            customerId,
            PostingKind.CustomerOpeningBalance,
            JournalBookIds.General,
            note,
            [
                (receivablesAccountId, amount, 0m, note, customerId),
                (AccountingAccountIds.OpeningBalanceEquity, 0m, amount, note, null)
            ],
            cancellationToken,
            postingDate);

        return result.JournalEntryNumber
            ?? await LookupEntryNumberAsync(DocumentType.CustomerOpeningBalance, customerId, cancellationToken);
    }

    public async Task<string> PostOpeningBalanceDocumentAsync(
        Guid documentId,
        string documentNumber,
        string description,
        DateTime postingDate,
        IReadOnlyList<JournalLineSpec> lines,
        CancellationToken cancellationToken = default)
    {
        var mapped = lines
            .Select(l => (l.AccountId, l.Debit, l.Credit, l.Narrative, l.PartyId))
            .ToList();

        var result = await PostViaEngineAsync(
            DocumentType.FinanceOpeningBalance,
            documentId,
            PostingKind.FinanceOpeningBalance,
            JournalBookIds.General,
            description,
            mapped,
            cancellationToken,
            postingDate);

        return result.JournalEntryNumber
            ?? await LookupEntryNumberAsync(DocumentType.FinanceOpeningBalance, documentId, cancellationToken)
            ?? documentNumber;
    }

    public Task PostExpensePaymentAsync(
        Guid expenseId,
        Guid paymentId,
        decimal amountBase,
        string description,
        CancellationToken cancellationToken = default)
    {
        if (amountBase <= 0)
            return Task.CompletedTask;

        return PostViaEngineAsync(
            DocumentType.ExpensePayment,
            paymentId,
            PostingKind.ExpensePayment,
            JournalBookIds.Cash,
            description,
            [
                (AccountingAccountIds.OperatingExpenses, amountBase, 0m, description, null),
                (AccountingAccountIds.CashUsd, 0m, amountBase, description, null)
            ],
            cancellationToken);
    }

    [Obsolete("Use IOpeningBalanceEngine.PostPartyOpeningBalanceAsync via OpeningBalanceUiService.")]
    public async Task<string> PostSupplierOpeningBalanceAsync(
        Guid supplierId,
        Guid payablesAccountId,
        decimal amount,
        DateTime postingDate,
        string referenceNote,
        CancellationToken cancellationToken = default)
    {
        if (amount <= 0)
            return "";

        var note = string.IsNullOrWhiteSpace(referenceNote) ? "رصيد افتتاحي مورد" : referenceNote.Trim();
        var result = await PostViaEngineAsync(
            DocumentType.SupplierOpeningBalance,
            supplierId,
            PostingKind.SupplierOpeningBalance,
            JournalBookIds.General,
            note,
            [
                (AccountingAccountIds.OpeningBalanceEquity, amount, 0m, note, null),
                (payablesAccountId, 0m, amount, note, supplierId)
            ],
            cancellationToken,
            postingDate);

        return result.JournalEntryNumber
            ?? await LookupEntryNumberAsync(DocumentType.SupplierOpeningBalance, supplierId, cancellationToken);
    }

    private async Task<PostingResult> PostViaEngineAsync(
        DocumentType sourceType,
        Guid sourceId,
        PostingKind postingKind,
        Guid journalBookId,
        string description,
        IReadOnlyList<(Guid AccountId, decimal Debit, decimal Credit, string Narrative, Guid? PartyId)> lines,
        CancellationToken cancellationToken,
        DateTime? entryDate = null,
        string? idempotencyKey = null,
        string? correlationId = null)
    {
        var activeLines = lines.Where(l => l.Debit > 0 || l.Credit > 0).ToList();
        if (activeLines.Count == 0)
            return PostingResult.Succeeded(Guid.Empty, "", Guid.Empty, alreadyPosted: true);

        var branchId = currentBranchService.BranchId ?? Guid.Empty;
        var companyId = currentBranchService.CompanyId ?? Guid.Empty;
        var userId = currentUserService.UserId ?? Guid.Empty;
        if (branchId == Guid.Empty || companyId == Guid.Empty)
            throw new AccountingException(
                "تعذر إنشاء القيد المحاسبي: سياق الفرع/الشركة غير محدد.");

        var request = new PostingRequest
        {
            CompanyId = companyId,
            BranchId = branchId,
            SourceType = sourceType,
            SourceId = sourceId,
            PostingKind = postingKind,
            PostingDate = entryDate ?? DateTime.UtcNow,
            UserId = userId,
            IdempotencyKey = idempotencyKey,
            CorrelationId = correlationId,
            Description = description,
            JournalBookId = journalBookId,
            Lines = activeLines.Select(l => new PostingLineRequest
            {
                AccountId = l.AccountId,
                Debit = l.Debit,
                Credit = l.Credit,
                Narrative = l.Narrative,
                PartyId = l.PartyId
            }).ToList()
        };

        var result = await postingEngine.PostAsync(request, cancellationToken);
        if (!result.Success)
            throw new AccountingException(result.ErrorMessage ?? "Posting failed.");

        if (!result.AlreadyPosted)
            _pendingRecoveryRequests.Add(request);

        return result;
    }

    private async Task<string> LookupEntryNumberAsync(
        DocumentType sourceType,
        Guid sourceId,
        CancellationToken cancellationToken) =>
        await context.JournalEntries.AsNoTracking()
            .Where(j => j.SourceType == (int)sourceType && j.SourceId == sourceId)
            .OrderByDescending(j => j.EntryDate)
            .Select(j => j.EntryNumber)
            .FirstOrDefaultAsync(cancellationToken) ?? "";
}
