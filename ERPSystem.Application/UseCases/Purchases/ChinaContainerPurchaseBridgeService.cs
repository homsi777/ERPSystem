using ERPSystem.Application.Abstractions.Repositories;
using ERPSystem.Application.Abstractions.Services;
using ERPSystem.Application.Common;
using ERPSystem.Domain.Aggregates;
using ERPSystem.Domain.Entities.Purchasing;
using ERPSystem.Domain.Enums;
using ERPSystem.Domain.ValueObjects;

namespace ERPSystem.Application.UseCases.Purchases;

public sealed class ChinaContainerPurchaseBridgeService(
    IPurchaseInvoiceRepository invoiceRepository,
    ISupplierRepository supplierRepository,
    IChinaContainerRepository containerRepository,
    IJournalEntryRepository journalEntryRepository,
    INumberingService numberingService,
    IIntegratedAccountingService accountingService) : IChinaContainerPurchaseBridgeService
{
    public async Task<Guid?> EnsurePostedPurchaseInvoiceAsync(
        ContainerAggregate container,
        Guid userId,
        bool skipGeneralLedger = false,
        CancellationToken cancellationToken = default)
    {
        var existing = await invoiceRepository.GetBySourceContainerIdAsync(container.Id, cancellationToken);
        if (existing is not null)
            return existing.Id;

        var supplierAgg = await supplierRepository.GetByIdAsync(container.SupplierId, cancellationToken);
        if (supplierAgg is null)
            return null;

        if (supplierAgg.Supplier.PayablesAccountId == Guid.Empty)
            return null;

        var lines = BuildLines(container);
        if (lines.Count == 0)
            return null;

        var currency = "USD";
        var invoiceDate = container.ApprovedAt ?? DateTime.UtcNow;
        var dueDate = invoiceDate.Date.AddDays(Math.Max(1, supplierAgg.Supplier.PaymentTermsDays));
        var number = await numberingService.NextPurchaseInvoiceNumberAsync(container.BranchId, cancellationToken);

        var invoice = PurchaseInvoice.CreateDraft(
            container.CompanyId,
            container.BranchId,
            number,
            container.SupplierId,
            invoiceDate,
            dueDate,
            currency,
            warehouseId: null,
            purchaseOrderId: null,
            sourceContainerId: container.Id);

        invoice.UpdateHeader(
            container.SupplierId,
            invoiceDate,
            dueDate,
            container.ContainerNumber.Value,
            warehouseId: null,
            currency,
            discountAmount: 0m,
            taxAmount: 0m,
            notes: $"فاتورة مورد صيني — حاوية {container.ContainerNumber.Value} — جسر استيراد الصين ↔ المشتريات");

        invoice.ReplaceItems(lines);

        if (invoice.TotalAmount.Amount <= 0)
            return null;

        invoice.Post(userId);
        await invoiceRepository.AddAsync(invoice, cancellationToken);

        if (!skipGeneralLedger)
            await accountingService.PostPurchaseInvoiceAsync(
                invoice,
                supplierAgg.Supplier.PayablesAccountId,
                cancellationToken);

        supplierAgg.Supplier.ApplyPostedPurchase(invoice.TotalAmount);
        await supplierRepository.UpdateAsync(supplierAgg, cancellationToken);

        return invoice.Id;
    }

    public async Task<ChinaContainerPurchaseBridgeBackfillResult> BackfillApprovedContainersAsync(
        Guid companyId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var messages = new List<string>();
        var created = 0;
        var skippedExisting = 0;
        var skippedNoAmount = 0;

        var statuses = new[]
        {
            ChinaContainerStatus.Approved,
            ChinaContainerStatus.InWarehouse,
            ChinaContainerStatus.Closed
        };

        foreach (var status in statuses)
        {
            var containers = await containerRepository.GetListAsync(companyId, status: status, cancellationToken: cancellationToken);
            foreach (var container in containers)
            {
                var existing = await invoiceRepository.GetBySourceContainerIdAsync(container.Id, cancellationToken);
                if (existing is not null)
                {
                    skippedExisting++;
                    continue;
                }

                var containerJournals = await journalEntryRepository.GetBySourceIdAsync(container.Id, cancellationToken);
                var skipGl = containerJournals.Any(j =>
                    j.SourceType == DocumentType.ChinaContainer &&
                    j.Status == JournalEntryStatus.Posted);

                var id = await EnsurePostedPurchaseInvoiceAsync(container, userId, skipGl, cancellationToken);
                if (id is null)
                {
                    skippedNoAmount++;
                    messages.Add($"تخطي {container.ContainerNumber.Value}: لا مبلغ قابل للترحيل.");
                    continue;
                }

                created++;
                messages.Add(skipGl
                    ? $"أُنشئت فاتورة {id} لـ {container.ContainerNumber.Value} (بدون قيد GL مكرر)."
                    : $"أُنشئت و رُحّلت فاتورة {id} لـ {container.ContainerNumber.Value}.");
            }
        }

        return new ChinaContainerPurchaseBridgeBackfillResult
        {
            Processed = created + skippedExisting + skippedNoAmount,
            Created = created,
            SkippedExisting = skippedExisting,
            SkippedNoAmount = skippedNoAmount,
            Messages = messages
        };
    }

    internal static List<PurchaseInvoiceItem> BuildLines(ContainerAggregate container)
    {
        const string currency = "USD";
        var lines = new List<PurchaseInvoiceItem>();
        var fabricLines = container.FabricTypeLines
            .Where(l => l.InvoiceLineAmountUsd > 0)
            .OrderBy(l => l.LineNumber)
            .ToList();

        foreach (var line in fabricLines)
        {
            if (line.FabricItemId is Guid fabricId && line.LengthMeters > 0)
            {
                var unitPrice = line.InvoiceLineAmountUsd / line.LengthMeters;
                lines.Add(PurchaseInvoiceItem.CreateInventoryLine(
                    fabricId,
                    line.FabricColorId,
                    new LengthInMeters(line.LengthMeters),
                    Math.Max(1, line.RollCount),
                    new Money(unitPrice, currency),
                    line.TypeDisplayName));
            }
            else
            {
                lines.Add(PurchaseInvoiceItem.CreateExpenseLine(
                    AccountingAccountIds.LandingCostClearing,
                    new Money(line.InvoiceLineAmountUsd, currency),
                    $"قماش — {line.TypeDisplayName}"));
            }
        }

        if (lines.Count == 0 && container.ChinaInvoiceAmountUsd > 0)
        {
            lines.Add(PurchaseInvoiceItem.CreateExpenseLine(
                AccountingAccountIds.LandingCostClearing,
                new Money(container.ChinaInvoiceAmountUsd, currency),
                $"فاتورة مورد صيني — {container.ContainerNumber.Value}"));
        }

        var landing = container.LandingCost;
        if (landing is not null)
        {
            AddLandingExpense(lines, landing.CustomsAmountPaid, "جمارك وتخليص");
            AddLandingExpense(lines, landing.Shipping, "شحن");
            AddLandingExpense(lines, landing.Insurance, "تأمين");
            AddLandingExpense(lines, landing.Clearance, "تخليص");
            AddLandingExpense(lines, landing.OtherExpenses, "مصاريف أخرى");
            AddLandingExpense(lines, landing.OtherExpense1, "مصروف 1");
            AddLandingExpense(lines, landing.OtherExpense2, "مصروف 2");
            AddLandingExpense(lines, landing.OtherExpense3, "مصروف 3");
            AddLandingExpense(lines, landing.OtherExpense4, "مصروف 4");
        }

        return lines;
    }

    private static void AddLandingExpense(List<PurchaseInvoiceItem> lines, Money amount, string label)
    {
        if (amount.Amount <= 0) return;
        lines.Add(PurchaseInvoiceItem.CreateExpenseLine(
            AccountingAccountIds.LandingCostClearing,
            amount,
            $"تكلفة وصول — {label}"));
    }
}
