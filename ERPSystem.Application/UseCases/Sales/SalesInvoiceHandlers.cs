using ERPSystem.Application.Abstractions;
using ERPSystem.Application.Abstractions.Repositories;
using ERPSystem.Application.Abstractions.Services;
using ERPSystem.Application.Commands.Sales;
using ERPSystem.Application.Common;
using ERPSystem.Application.Notifications;
using ERPSystem.Application.Results;
using ERPSystem.Application.Services;
using ERPSystem.Domain.Aggregates;
using ERPSystem.Domain.Entities.Finance;
using ERPSystem.Domain.Entities.Sales;
using ERPSystem.Domain.Exceptions;
using ERPSystem.Domain.Services;
using ERPSystem.Domain.Specifications;
using ERPSystem.Domain.ValueObjects;

namespace ERPSystem.Application.UseCases.Sales;

internal static class SalesInvoicePriceOverride
{
    /// <summary>
    /// Determines the catalog baseline price and captures audit info when the manager
    /// applied a manual discount (applied price below the catalog price).
    /// </summary>
    public static (Money? OriginalPrice, string? Reason, Guid? ModifiedBy, DateTime? ModifiedAt) Resolve(
        SalesInvoiceLineCommand line, Guid userId, DateTime now)
    {
        var baseline = line.OriginalUnitPrice > 0 ? line.OriginalUnitPrice : line.UnitPrice;
        var isDiscounted = baseline > line.UnitPrice;
        return (
            new Money(baseline),
            isDiscounted ? line.DiscountReason : null,
            isDiscounted && userId != Guid.Empty ? userId : null,
            isDiscounted ? now : null);
    }
}

public sealed class CreateSalesInvoiceDraftHandler(
    ISalesInvoiceRepository invoiceRepository,
    IUnitOfWork unitOfWork,
    INumberingService numberingService,
    IPermissionService permissionService,
    ICurrentUserService currentUserService,
    IInventoryOperationsService inventoryOperations)
    : ICommandHandler<CreateSalesInvoiceDraftCommand, ApplicationResult<Guid>>
{
    public async Task<ApplicationResult<Guid>> HandleAsync(
        CreateSalesInvoiceDraftCommand command,
        CancellationToken cancellationToken = default)
    {
        var validation = Validation.ApplicationValidators.Validate(command);
        if (validation is not null)
            return ApplicationResult<Guid>.ValidationFailed(validation.ValidationErrors);

        if (!await permissionService.CanAsync("sales.create", cancellationToken))
            return ApplicationResult<Guid>.PermissionDenied("Not allowed to create sales invoices.");

        try
        {
            await inventoryOperations.ValidateInvoiceLinesAsync(
                command.WarehouseId,
                command.Lines.Select(l => (l.ChinaContainerId, l.FabricItemId, l.FabricColorId, l.RollCount)).ToList(),
                cancellationToken);

            var invoiceNumberText = string.IsNullOrWhiteSpace(command.InvoiceNumber)
                ? await numberingService.NextInvoiceNumberAsync(command.BranchId, cancellationToken)
                : command.InvoiceNumber.Trim();

            if (await invoiceRepository.GetByNumberAsync(invoiceNumberText, cancellationToken) is not null)
                return ApplicationResult<Guid>.ValidationFailed(nameof(command.InvoiceNumber), "رقم الفاتورة مستخدم مسبقاً.");

            var invoiceNumber = new InvoiceNumber(invoiceNumberText);
            // Header ChinaContainerId is now only the primary container for backward-compatible
            // display/reporting. Per-line ChinaContainerId is authoritative for stock operations.
            var primaryContainerId = command.Lines
                .OrderBy(l => l.LineNumber)
                .First(l => l.RollCount > 0)
                .ChinaContainerId;

            var userId = currentUserService.UserId ?? Guid.Empty;
            var aggregate = SalesInvoiceAggregate.CreateDraft(
                invoiceNumber,
                command.CompanyId,
                command.BranchId,
                command.CustomerId,
                command.WarehouseId,
                primaryContainerId,
                command.PaymentType,
                userId);

            var now = DateTime.UtcNow;
            foreach (var line in command.Lines)
            {
                var (originalPrice, reason, modifiedBy, modifiedAt) =
                    SalesInvoicePriceOverride.Resolve(line, userId, now);
                var item = SalesInvoiceItem.Create(
                    line.LineNumber,
                    line.ChinaContainerId,
                    line.FabricItemId,
                    line.FabricColorId,
                    line.RollCount,
                    new Money(line.UnitPrice),
                    line.Notes,
                    originalPrice,
                    reason,
                    modifiedBy,
                    modifiedAt,
                    line.TaxCodeId,
                    line.Unit);
                aggregate.AddItem(item);
            }

            if (command.DiscountAmount > 0)
                aggregate.SetDiscountTotal(new Money(command.DiscountAmount));

            aggregate.SetCashboxId(command.CashboxId);
            aggregate.SetPartialPaymentAmount(
                command.PaymentType == Domain.Enums.PaymentType.Credit && command.PartialPaymentAmount is > 0
                    ? new Money(command.PartialPaymentAmount.Value)
                    : null);

            Domain.Validators.SalesInvoiceValidator.ValidateDraft(aggregate);

            await invoiceRepository.AddAsync(aggregate, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            return ApplicationResult<Guid>.Success(aggregate.Id);
        }
        catch (Exception ex)
        {
            return ex.ToFailureResult<Guid>();
        }
    }
}

public sealed class UpdateSalesInvoiceDraftHandler(
    ISalesInvoiceRepository invoiceRepository,
    IUnitOfWork unitOfWork,
    IPermissionService permissionService,
    ICurrentUserService currentUserService,
    IInventoryOperationsService inventoryOperations)
    : ICommandHandler<UpdateSalesInvoiceDraftCommand, ApplicationResult>
{
    public async Task<ApplicationResult> HandleAsync(
        UpdateSalesInvoiceDraftCommand command,
        CancellationToken cancellationToken = default)
    {
        var validation = Validation.ApplicationValidators.Validate(command);
        if (validation is not null)
            return validation;

        if (!await permissionService.CanAsync("sales.create", cancellationToken))
            return ApplicationResult.PermissionDenied("Not allowed to update sales invoices.");

        var aggregate = await invoiceRepository.GetByIdAsync(command.InvoiceId, cancellationToken);
        if (aggregate is null)
            return ApplicationResult.NotFound("Invoice not found.");

        try
        {
            await inventoryOperations.ValidateInvoiceLinesAsync(
                command.WarehouseId,
                command.Lines.Select(l => (l.ChinaContainerId, l.FabricItemId, l.FabricColorId, l.RollCount)).ToList(),
                cancellationToken);
            // Header ChinaContainerId is now only the primary container for backward-compatible
            // display/reporting. Per-line ChinaContainerId is authoritative for stock operations.
            var primaryContainerId = command.Lines
                .OrderBy(l => l.LineNumber)
                .First(l => l.RollCount > 0)
                .ChinaContainerId;

            aggregate.UpdateDraftHeader(
                command.CustomerId,
                command.WarehouseId,
                primaryContainerId,
                command.PaymentType,
                command.CashboxId);

            var userId = currentUserService.UserId ?? Guid.Empty;
            var now = DateTime.UtcNow;
            var items = command.Lines
                .Select(line =>
                {
                    var (originalPrice, reason, modifiedBy, modifiedAt) =
                        SalesInvoicePriceOverride.Resolve(line, userId, now);
                    return SalesInvoiceItem.Create(
                        line.LineNumber,
                        line.ChinaContainerId,
                        line.FabricItemId,
                        line.FabricColorId,
                        line.RollCount,
                        new Money(line.UnitPrice),
                        line.Notes,
                        originalPrice,
                        reason,
                        modifiedBy,
                        modifiedAt,
                        line.TaxCodeId,
                        line.Unit);
                })
                .ToList();

            aggregate.ReplaceDraftLines(items);

            if (command.DiscountAmount > 0)
                aggregate.SetDiscountTotal(new Money(command.DiscountAmount));
            else
                aggregate.SetDiscountTotal(Money.Zero());

            aggregate.SetPartialPaymentAmount(
                command.PaymentType == Domain.Enums.PaymentType.Credit && command.PartialPaymentAmount is > 0
                    ? new Money(command.PartialPaymentAmount.Value)
                    : null);

            Domain.Validators.SalesInvoiceValidator.ValidateDraft(aggregate);

            await invoiceRepository.UpdateAsync(aggregate, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return ApplicationResult.Success();
        }
        catch (Exception ex)
        {
            return ex.ToFailureResult();
        }
    }
}

public sealed class UpdateSalesInvoiceDiscountHandler(
    ISalesInvoiceRepository invoiceRepository,
    IUnitOfWork unitOfWork,
    IPermissionService permissionService,
    SalesInvoiceTaxService salesInvoiceTaxService)
    : ICommandHandler<UpdateSalesInvoiceDiscountCommand, ApplicationResult>
{
    public async Task<ApplicationResult> HandleAsync(
        UpdateSalesInvoiceDiscountCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command.InvoiceId == Guid.Empty)
            return ApplicationResult.ValidationFailed(nameof(command.InvoiceId), "Invoice is required.");
        if (command.DiscountAmount < 0)
            return ApplicationResult.ValidationFailed(nameof(command.DiscountAmount), "Discount cannot be negative.");

        if (!await permissionService.CanAsync("sales.create", cancellationToken))
            return ApplicationResult.PermissionDenied("Not allowed to update sales invoices.");

        var aggregate = await invoiceRepository.GetByIdAsync(command.InvoiceId, cancellationToken);
        if (aggregate is null)
            return ApplicationResult.NotFound("Invoice not found.");

        try
        {
            aggregate.SetDiscountTotal(new Money(command.DiscountAmount));
            await salesInvoiceTaxService.ApplyTaxToInvoiceAsync(aggregate, freezeSnapshots: false, cancellationToken);
            await invoiceRepository.UpdateAsync(aggregate, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return ApplicationResult.Success();
        }
        catch (Exception ex)
        {
            return ex.ToFailureResult();
        }
    }
}

public sealed class SendSalesInvoiceToWarehouseHandler(
    ISalesInvoiceRepository invoiceRepository,
    IUnitOfWork unitOfWork,
    IPermissionService permissionService,
    IInventoryOperationsService inventoryOperations,
    IDomainEventDispatcher domainEventDispatcher,
    INotificationService notificationService)
    : ICommandHandler<SendSalesInvoiceToWarehouseCommand, ApplicationResult>
{
    public async Task<ApplicationResult> HandleAsync(
        SendSalesInvoiceToWarehouseCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command.InvoiceId == Guid.Empty)
            return ApplicationResult.ValidationFailed(nameof(command.InvoiceId), "Invoice is required.");

        if (!await permissionService.CanAsync("sales.send-to-warehouse", cancellationToken))
            return ApplicationResult.PermissionDenied("Not allowed to send invoice to warehouse.");

        var aggregate = await invoiceRepository.GetByIdAsync(command.InvoiceId, cancellationToken);
        if (aggregate is null)
            return ApplicationResult.NotFound("Invoice not found.");

        try
        {
            aggregate.SendToWarehouse();
            await inventoryOperations.ReserveForInvoiceAsync(aggregate, cancellationToken);
            await invoiceRepository.UpdateAsync(aggregate, cancellationToken);
            await unitOfWork.SaveAndDispatchAsync(domainEventDispatcher, [aggregate], cancellationToken);
            await notificationService.PublishAsync(new InventoryChangedNotification
            {
                ContainerId = aggregate.ChinaContainerId,
                WarehouseId = aggregate.WarehouseId
            }, cancellationToken);
            return ApplicationResult.Success();
        }
        catch (Exception ex)
        {
            return ex.ToFailureResult();
        }
    }
}

public sealed class CompleteWarehouseDetailingHandler(
    ISalesInvoiceRepository invoiceRepository,
    IUnitOfWork unitOfWork,
    IPermissionService permissionService,
    INotificationService notificationService,
    ICurrentUserService currentUserService,
    IInventoryOperationsService inventoryOperations,
    SalesInvoiceTaxService salesInvoiceTaxService,
    IDomainEventDispatcher domainEventDispatcher)
    : ICommandHandler<CompleteWarehouseDetailingCommand, ApplicationResult>
{
    public async Task<ApplicationResult> HandleAsync(
        CompleteWarehouseDetailingCommand command,
        CancellationToken cancellationToken = default)
    {
        var validation = Validation.ApplicationValidators.Validate(command);
        if (validation is not null)
            return validation;

        if (!await permissionService.CanAsync("warehouse.detailing", cancellationToken))
            return ApplicationResult.PermissionDenied("Not allowed to complete detailing.");

        var aggregate = await invoiceRepository.GetByIdAsync(command.InvoiceId, cancellationToken);
        if (aggregate is null)
            return ApplicationResult.NotFound("Invoice not found.");

        var spec = new WarehouseCanDetailSpecification();
        if (!spec.IsSatisfiedBy(aggregate))
            return ApplicationResult.Conflict(spec.FailureReason);

        try
        {
            var userId = currentUserService.UserId ?? Guid.Empty;

            var resolvePayload = command.RollEntries
                .Select(e => (e.RollDetailId, e.RollNumber, e.LengthMeters))
                .ToList();

            var resolvedLengths = await inventoryOperations.ResolveDetailingEntriesAsync(
                aggregate,
                resolvePayload,
                cancellationToken);

            foreach (var entry in command.RollEntries)
            {
                if (!resolvedLengths.TryGetValue(entry.RollDetailId, out var lengthMeters))
                    return ApplicationResult.Conflict("تعذّر مطابقة جميع الأثواب.");

                aggregate.EnterRollLength(entry.RollDetailId, new LengthInMeters(lengthMeters), userId);
            }

            if (!WarehouseDetailingValidator.CanCompleteDetailing(aggregate))
                return ApplicationResult.Conflict("All roll lengths must be entered before completing detailing.");

            aggregate.CompleteDetailing();
            await salesInvoiceTaxService.ApplyTaxToInvoiceAsync(aggregate, freezeSnapshots: false, cancellationToken);

            // ResolveDetailingEntriesAsync already pinned every roll detail to a specific fabric
            // roll (by serial, or by best-available match for length-only entries), so approval-time
            // deduction is guaranteed to find enough remaining length on the assigned roll.

            await invoiceRepository.UpdateAsync(aggregate, cancellationToken);
            await unitOfWork.SaveAndDispatchAsync(domainEventDispatcher, [aggregate], cancellationToken);

            // Never fail a completed detailing because the UI notification dialog threw —
            // the invoice is already saved as detailed.
            try
            {
                await notificationService.PublishAsync(new SalesInvoiceDetailedNotification
                {
                    InvoiceId = aggregate.Id,
                    InvoiceNumber = aggregate.InvoiceNumber.Value,
                    GrandTotal = aggregate.GrandTotal.Amount
                }, cancellationToken);
            }
            catch
            {
                // UI notification failures must not reverse a successful save.
            }

            return ApplicationResult.Success();
        }
        catch (Exception ex)
        {
            return ex.ToFailureResult();
        }
    }
}

public sealed class SaveWarehouseDetailingDraftHandler(
    ISalesInvoiceRepository invoiceRepository,
    IUnitOfWork unitOfWork,
    IPermissionService permissionService,
    ICurrentUserService currentUserService,
    IDomainEventDispatcher domainEventDispatcher)
    : ICommandHandler<SaveWarehouseDetailingDraftCommand, ApplicationResult>
{
    public async Task<ApplicationResult> HandleAsync(
        SaveWarehouseDetailingDraftCommand command,
        CancellationToken cancellationToken = default)
    {
        var validation = Validation.ApplicationValidators.Validate(command);
        if (validation is not null)
            return validation;

        if (!await permissionService.CanAsync("warehouse.detailing", cancellationToken))
            return ApplicationResult.PermissionDenied("Not allowed to save detailing progress.");

        var aggregate = await invoiceRepository.GetByIdAsync(command.InvoiceId, cancellationToken);
        if (aggregate is null)
            return ApplicationResult.NotFound("Invoice not found.");

        try
        {
            var userId = currentUserService.UserId ?? Guid.Empty;
            var entries = command.RollEntries
                .Select(e => (e.RollDetailId, e.RollNumber, e.LengthMeters))
                .ToList();

            aggregate.SaveDetailingDraft(entries, userId);

            await invoiceRepository.UpdateAsync(aggregate, cancellationToken);
            await unitOfWork.SaveAndDispatchAsync(domainEventDispatcher, [aggregate], cancellationToken);

            return ApplicationResult.Success();
        }
        catch (Exception ex)
        {
            return ex.ToFailureResult();
        }
    }
}

public sealed class ApproveSalesInvoiceHandler(
    ISalesInvoiceRepository invoiceRepository,
    ICustomerRepository customerRepository,
    ICashboxRepository cashboxRepository,
    IReceiptVoucherRepository receiptVoucherRepository,
    IReceiptInvoicePaymentRepository receiptInvoicePaymentRepository,
    INumberingService numberingService,
    IUnitOfWork unitOfWork,
    IPermissionService permissionService,
    INotificationService notificationService,
    ICurrentUserService currentUserService,
    IInventoryOperationsService inventoryOperations,
    IIntegratedAccountingService accountingService,
    IPostingSaveCoordinator postingSaveCoordinator,
    SalesInvoiceTaxService salesInvoiceTaxService,
    ISalesPostingProfileRepository postingProfileRepository,
    IAuditLogRepository auditLogRepository,
    ICurrentBranchService currentBranchService,
    IDomainEventDispatcher domainEventDispatcher)
    : ICommandHandler<ApproveSalesInvoiceCommand, ApplicationResult>
{
    public async Task<ApplicationResult> HandleAsync(
        ApproveSalesInvoiceCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command.InvoiceId == Guid.Empty)
            return ApplicationResult.ValidationFailed(nameof(command.InvoiceId), "Invoice is required.");

        if (!await permissionService.CanAsync("sales.approve", cancellationToken))
            return ApplicationResult.PermissionDenied("Not allowed to approve sales invoices.");

        var aggregate = await invoiceRepository.GetByIdAsync(command.InvoiceId, cancellationToken);
        if (aggregate is null)
            return ApplicationResult.NotFound("Invoice not found.");

        var spec = new InvoiceCanBeApprovedSpecification();
        if (!spec.IsSatisfiedBy(aggregate))
            return ApplicationResult.Conflict(spec.FailureReason);

        var customerAggregate = await customerRepository.GetByIdAsync(aggregate.CustomerId, cancellationToken);
        if (customerAggregate is null)
            return ApplicationResult.NotFound("Customer not found.");

        var cashCollectionAmount = ResolveCashCollectionAmount(aggregate);
        Cashbox? cashbox = null;
        if (cashCollectionAmount > 0)
        {
            if (aggregate.CashboxId is not Guid cashboxId || cashboxId == Guid.Empty)
                return ApplicationResult.ValidationFailed(
                    "CashboxId",
                    "يجب اختيار الصندوق عند البيع النقدي أو عند وجود دفعة جزئية.");

            cashbox = await cashboxRepository.GetByIdAsync(cashboxId, cancellationToken);
            if (cashbox is null)
                return ApplicationResult.NotFound("الصندوق المحدد غير موجود.");
            if (!cashbox.IsActive)
                return ApplicationResult.ValidationFailed("CashboxId", "الصندوق المحدد غير نشط.");
            if (cashbox.AccountId is not Guid accountId || accountId == Guid.Empty)
                return ApplicationResult.ValidationFailed("CashboxId", "الصندوق المحدد لا يملك حساب GL مرتبط.");
        }

        try
        {
            await unitOfWork.BeginTransactionAsync(cancellationToken);

            await salesInvoiceTaxService.ApplyTaxToInvoiceAsync(aggregate, freezeSnapshots: true, cancellationToken);
            var postingProfile = await postingProfileRepository.GetForCompanyAsync(aggregate.CompanyId, cancellationToken);
            if (aggregate.TaxTotal.Amount > 0)
            {
                if (postingProfile is null)
                    throw new ValidationException("Sales posting profile is not configured.");
                await salesInvoiceTaxService.EnsureTaxPostingReadyAsync(aggregate, postingProfile, cancellationToken);
            }

            CreditLimitChecker.EnsureWithinLimit(customerAggregate, aggregate.GrandTotal);

            var userId = currentUserService.UserId ?? Guid.Empty;
            aggregate.Approve(userId);
            customerAggregate.RecordPostedInvoice(aggregate.GrandTotal.Amount);

            var cogs = await inventoryOperations.DeductForInvoiceAsync(aggregate, cancellationToken);
            await accountingService.PostSalesInvoiceApprovalAsync(aggregate, cogs, cancellationToken);
            var recoveryRequests = accountingService.ConsumePendingPostingRequests().ToList();

            if (cashCollectionAmount > 0 && cashbox is not null)
            {
                await PostCashCollectionAsync(
                    aggregate,
                    customerAggregate,
                    cashbox,
                    cashCollectionAmount,
                    cancellationToken);
                recoveryRequests.AddRange(accountingService.ConsumePendingPostingRequests());
            }

            if (aggregate.TotalLineDiscount.Amount > 0)
            {
                var discountedLines = aggregate.Items
                    .Where(i => i.DiscountAmount.Amount > 0)
                    .Select(i =>
                        $"{{\"line\":{i.LineNumber},\"original\":{i.OriginalUnitPrice.Amount}," +
                        $"\"applied\":{i.UnitPrice.Amount},\"discount\":{i.DiscountAmount.Amount}," +
                        $"\"reason\":\"{i.DiscountReason?.Replace("\"", "'")}\"}}");

                await auditLogRepository.AddAsync(
                    Domain.Entities.System.AuditLog.Record(
                        userId,
                        "SalesPriceOverride",
                        "SalesInvoice",
                        aggregate.Id,
                        null,
                        $"{{\"invoice\":\"{aggregate.InvoiceNumber.Value}\"," +
                        $"\"totalDiscount\":{aggregate.TotalLineDiscount.Amount}," +
                        $"\"lines\":[{string.Join(",", discountedLines)}]}}",
                        currentBranchService.BranchId),
                    cancellationToken);
            }

            await invoiceRepository.UpdateAsync(aggregate, cancellationToken);
            await customerRepository.UpdateAsync(customerAggregate, cancellationToken);

            var events = aggregate.DomainEvents.ToList();
            aggregate.ClearDomainEvents();
            await postingSaveCoordinator.SaveChangesWithPostingRecoveryAsync(recoveryRequests, cancellationToken);
            if (events.Count > 0)
                await domainEventDispatcher.DispatchAsync(events, cancellationToken);

            await unitOfWork.CommitTransactionAsync(cancellationToken);

            await notificationService.PublishAsync(new SalesInvoiceApprovedNotification
            {
                InvoiceId = aggregate.Id,
                InvoiceNumber = aggregate.InvoiceNumber.Value,
                CustomerId = aggregate.CustomerId,
                GrandTotal = aggregate.GrandTotal.Amount
            }, cancellationToken);

            return ApplicationResult.Success();
        }
        catch (Exception ex)
        {
            await unitOfWork.RollbackTransactionAsync(cancellationToken);

            var exceeded = CreditLimitChecker.TryCreateExceededEvent(
                customerAggregate.Customer,
                aggregate.GrandTotal);
            if (exceeded is not null)
                await notificationService.PublishAsync(new CustomerCreditLimitExceededNotification
                {
                    CustomerId = exceeded.CustomerId,
                    RequestedAmount = exceeded.RequestedAmount,
                    CreditLimit = exceeded.CreditLimit
                }, cancellationToken);

            return ex.ToFailureResult();
        }
    }

    private static decimal ResolveCashCollectionAmount(SalesInvoiceAggregate invoice)
    {
        if (invoice.PaymentType == Domain.Enums.PaymentType.Cash)
            return invoice.GrandTotal.Amount;

        return invoice.PartialPaymentAmount is { Amount: > 0 } partial
            ? partial.Amount
            : 0m;
    }

    private async Task PostCashCollectionAsync(
        SalesInvoiceAggregate invoice,
        CustomerAggregate customerAggregate,
        Cashbox cashbox,
        decimal amount,
        CancellationToken cancellationToken)
    {
        var number = await numberingService.NextReceiptNumberAsync(invoice.BranchId, cancellationToken);
        var companyId = invoice.CompanyId;
        var voucher = ReceiptVoucher.CreateDraft(
            companyId,
            invoice.BranchId,
            number,
            invoice.CustomerId,
            cashbox.Id,
            PaymentMethodIds.Cash,
            new Money(amount));
        voucher.Allocate(invoice.Id, new Money(amount));
        voucher.Approve();
        voucher.Post();

        if (customerAggregate.Customer.Type == Domain.Enums.CustomerType.Credit)
            customerAggregate.RecordPostedReceipt(amount);

        cashbox.ApplyReceipt(new Money(amount));

        await receiptVoucherRepository.AddAsync(voucher, cancellationToken);
        await receiptVoucherRepository.AddTenderLineAsync(
            ReceiptTenderLine.CreateCash(voucher.Id, PaymentMethodIds.Cash, cashbox.Id, new Money(amount)),
            cancellationToken);
        await receiptInvoicePaymentRepository.AddAsync(
            ReceiptInvoicePayment.Create(invoice.Id, voucher.Id, new Money(amount)),
            cancellationToken);
        await cashboxRepository.UpdateAsync(cashbox, cancellationToken);

        var allocated = amount;
        await accountingService.PostReceiptVoucherAsync(
            voucher.Id,
            voucher.VoucherNumber,
            invoice.CustomerId,
            cashbox.AccountId ?? throw new ValidationException($"الصندوق '{cashbox.Code}' لا يملك حساب GL."),
            amount,
            allocated,
            cancellationToken: cancellationToken);
    }
}

public sealed class CancelSalesInvoiceHandler(
    ISalesInvoiceRepository invoiceRepository,
    IUnitOfWork unitOfWork,
    IPermissionService permissionService,
    IInventoryOperationsService inventoryOperations,
    INotificationService notificationService)
    : ICommandHandler<CancelSalesInvoiceCommand, ApplicationResult>
{
    public async Task<ApplicationResult> HandleAsync(
        CancelSalesInvoiceCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command.InvoiceId == Guid.Empty)
            return ApplicationResult.ValidationFailed(nameof(command.InvoiceId), "Invoice is required.");
        if (string.IsNullOrWhiteSpace(command.Reason))
            return ApplicationResult.ValidationFailed(nameof(command.Reason), "Cancel reason is required.");

        if (!await permissionService.CanAsync("sales.cancel", cancellationToken))
            return ApplicationResult.PermissionDenied("Not allowed to cancel sales invoices.");

        var aggregate = await invoiceRepository.GetByIdAsync(command.InvoiceId, cancellationToken);
        if (aggregate is null)
            return ApplicationResult.NotFound("Invoice not found.");

        try
        {
            await inventoryOperations.ReleaseForInvoiceAsync(aggregate, cancellationToken);
            aggregate.Cancel(command.Reason);
            await invoiceRepository.UpdateAsync(aggregate, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            await notificationService.PublishAsync(new InventoryChangedNotification
            {
                ContainerId = aggregate.ChinaContainerId,
                WarehouseId = aggregate.WarehouseId
            }, cancellationToken);
            return ApplicationResult.Success();
        }
        catch (Exception ex)
        {
            return ex.ToFailureResult();
        }
    }
}
