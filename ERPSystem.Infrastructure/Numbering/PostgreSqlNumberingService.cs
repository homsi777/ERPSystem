using ERPSystem.Application.Abstractions.Services;
using ERPSystem.Infrastructure.Persistence;
using ERPSystem.Infrastructure.Persistence.Models.Documents;
using Microsoft.EntityFrameworkCore;

namespace ERPSystem.Infrastructure.Numbering;

internal sealed class PostgreSqlNumberingService(ErpDbContext context) : INumberingService
{
    private static readonly Dictionary<string, string> Prefixes = new()
    {
        ["SalesInvoice"] = "INV",
        ["Container"] = "CNT",
        ["ReceiptVoucher"] = "RCP",
        ["PaymentVoucher"] = "PAY",
        ["JournalEntry"] = "JE",
        ["Customer"] = "CUS",
        ["Supplier"] = "SUP",
        ["PurchaseInvoice"] = "PI"
    };

    public Task<string> NextInvoiceNumberAsync(Guid branchId, CancellationToken cancellationToken = default) =>
        NextAsync(branchId, "SalesInvoice", cancellationToken);

    public Task<string> NextContainerNumberAsync(Guid branchId, CancellationToken cancellationToken = default) =>
        NextAsync(branchId, "Container", cancellationToken);

    public Task<string> NextReceiptNumberAsync(Guid branchId, CancellationToken cancellationToken = default) =>
        NextAsync(branchId, "ReceiptVoucher", cancellationToken);

    public Task<string> NextPaymentNumberAsync(Guid branchId, CancellationToken cancellationToken = default) =>
        NextAsync(branchId, "PaymentVoucher", cancellationToken);

    public Task<string> NextJournalEntryNumberAsync(Guid branchId, CancellationToken cancellationToken = default) =>
        NextAsync(branchId, "JournalEntry", cancellationToken);

    private async Task<string> NextAsync(Guid branchId, string documentType, CancellationToken cancellationToken)
    {
        var counter = await context.DocumentCounters
            .FirstOrDefaultAsync(c => c.BranchId == branchId && c.DocumentType == documentType, cancellationToken);

        if (counter is null)
        {
            var prefix = Prefixes.GetValueOrDefault(documentType, "DOC");
            counter = new DocumentCounterEntity
            {
                Id = Guid.NewGuid(),
                BranchId = branchId,
                DocumentType = documentType,
                Prefix = prefix,
                LastNumber = 0,
                RowVersion = [0, 0, 0, 0, 0, 0, 0, 1]
            };
            await context.DocumentCounters.AddAsync(counter, cancellationToken);
        }

        counter.LastNumber++;
        counter.UpdatedAt = DateTime.UtcNow;
        await context.SaveChangesAsync(cancellationToken);

        var branchCode = await context.Branches.AsNoTracking()
            .Where(b => b.Id == branchId)
            .Select(b => b.Code)
            .FirstOrDefaultAsync(cancellationToken) ?? "BR";

        return $"{counter.Prefix}-{branchCode}-{counter.LastNumber:D6}";
    }
}
