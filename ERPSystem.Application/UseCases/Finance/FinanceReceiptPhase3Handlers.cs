using ERPSystem.Application.Abstractions;
using ERPSystem.Application.Abstractions.Repositories;
using ERPSystem.Application.Abstractions.Services;
using ERPSystem.Application.Commands.Finance;
using ERPSystem.Application.Common;
using ERPSystem.Application.DTOs.Finance;
using ERPSystem.Application.Notifications;
using ERPSystem.Application.Results;
using ERPSystem.Domain.Entities.Finance;
using ERPSystem.Domain.Entities.Sales;
using ERPSystem.Domain.Enums;
using ERPSystem.Domain.ValueObjects;

namespace ERPSystem.Application.UseCases.Finance;

public sealed class CreateReceiptVoucherHandler(
    IReceiptVoucherRepository voucherRepository,
    IReceiptInvoicePaymentRepository paymentRepository,
    ICashboxPostingValidator cashboxValidator,
    IBankAccountPostingValidator bankAccountValidator,
    IUnitOfWork unitOfWork,
    INumberingService numberingService,
    IPermissionService permissionService)
    : ICommandHandler<CreateReceiptVoucherCommand, ApplicationResult<Guid>>
{
    public async Task<ApplicationResult<Guid>> HandleAsync(
        CreateReceiptVoucherCommand command,
        CancellationToken cancellationToken = default)
    {
        var validation = Validation.ApplicationValidators.Validate(command);
        if (validation is not null)
            return ApplicationResult<Guid>.ValidationFailed(validation.ValidationErrors);

        if (!await permissionService.CanAsync("finance.receipt.create", cancellationToken))
            return ApplicationResult<Guid>.PermissionDenied("Not allowed to create receipt vouchers.");

        if (command.BankAccountId is Guid bankAccountId)
        {
            var bankValidation = await bankAccountValidator.ValidateForReceiptAsync(
                command.CompanyId, bankAccountId, command.Currency, command.Reference, cancellationToken);
            if (!bankValidation.IsValid)
                return ApplicationResult<Guid>.ValidationFailed(
                    "BankAccountId", bankValidation.ErrorMessage ?? "Invalid bank account.");
        }
        else
        {
            var cashValidation = await cashboxValidator.ValidateForReceiptAsync(
                command.CompanyId, command.CashboxId, command.Currency, cancellationToken);
            if (!cashValidation.IsValid)
                return ApplicationResult<Guid>.ValidationFailed(
                    "CashboxId", cashValidation.ErrorMessage ?? "Invalid cashbox.");
        }

        var allocationTotal = command.Allocations.Sum(a => a.Amount);
        if (allocationTotal > command.Amount)
            return ApplicationResult<Guid>.ValidationFailed("Allocations", "مجموع التخصيصات يتجاوز مبلغ السند.");

        try
        {
            var number = await numberingService.NextReceiptNumberAsync(command.BranchId, cancellationToken);
            var voucher = ReceiptVoucher.CreateDraft(
                command.CompanyId,
                command.BranchId,
                number,
                command.CustomerId,
                command.CashboxId,
                command.PaymentMethodId,
                new Money(command.Amount, command.Currency));

            foreach (var allocation in command.Allocations)
            {
                if (allocation.SalesInvoiceId == Guid.Empty || allocation.Amount <= 0) continue;
                voucher.Allocate(allocation.SalesInvoiceId, new Money(allocation.Amount));
            }

            Domain.Validators.ReceiptVoucherValidator.Validate(voucher);
            await voucherRepository.AddAsync(voucher, cancellationToken);

            var tender = command.BankAccountId is Guid bankId
                ? ReceiptTenderLine.CreateBank(voucher.Id, command.PaymentMethodId, bankId,
                    new Money(command.Amount, command.Currency), command.Reference ?? "", command.Currency, command.ExchangeRate)
                : ReceiptTenderLine.CreateCash(voucher.Id, command.PaymentMethodId, command.CashboxId,
                    new Money(command.Amount, command.Currency), command.Currency, command.ExchangeRate);
            await voucherRepository.AddTenderLineAsync(tender, cancellationToken);

            foreach (var allocation in command.Allocations)
            {
                if (allocation.SalesInvoiceId == Guid.Empty || allocation.Amount <= 0) continue;
                await paymentRepository.AddAsync(
                    ReceiptInvoicePayment.Create(allocation.SalesInvoiceId, voucher.Id, new Money(allocation.Amount)),
                    cancellationToken);
            }

            await unitOfWork.SaveChangesAsync(cancellationToken);
            return ApplicationResult<Guid>.Success(voucher.Id);
        }
        catch (Exception ex)
        {
            return ex.ToFailureResult<Guid>();
        }
    }
}

public sealed class ApproveReceiptVoucherHandler(
    IReceiptVoucherRepository voucherRepository,
    IUnitOfWork unitOfWork,
    IPermissionService permissionService)
    : ICommandHandler<ApproveReceiptVoucherCommand, ApplicationResult>
{
    public async Task<ApplicationResult> HandleAsync(
        ApproveReceiptVoucherCommand command,
        CancellationToken cancellationToken = default)
    {
        if (!await permissionService.CanAsync("finance.receipt.approve", cancellationToken))
            return ApplicationResult.PermissionDenied("Not allowed to approve receipt vouchers.");

        var voucher = await voucherRepository.GetByIdAsync(command.VoucherId, cancellationToken);
        if (voucher is null)
            return ApplicationResult.NotFound("Receipt voucher not found.");

        try
        {
            voucher.Approve();
            await voucherRepository.UpdateAsync(voucher, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return ApplicationResult.Success();
        }
        catch (Exception ex)
        {
            return ex.ToFailureResult();
        }
    }
}

public sealed class PostReceiptVoucherHandler(
    IReceiptVoucherRepository voucherRepository,
    ICustomerRepository customerRepository,
    ICashboxRepository cashboxRepository,
    IReceiptPostingService receiptPostingService,
    IIntegratedAccountingService integratedAccounting,
    ICashboxPostingValidator cashboxValidator,
    IPostingSaveCoordinator postingSaveCoordinator,
    IUnitOfWork unitOfWork,
    IPermissionService permissionService,
    INotificationService notificationService)
    : ICommandHandler<PostReceiptVoucherCommand, ApplicationResult>
{
    public async Task<ApplicationResult> HandleAsync(
        PostReceiptVoucherCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command.VoucherId == Guid.Empty)
            return ApplicationResult.ValidationFailed(nameof(command.VoucherId), "Voucher is required.");

        if (!string.IsNullOrWhiteSpace(command.IdempotencyKey))
        {
            var existingId = await voucherRepository.GetIdByIdempotencyKeyAsync(
                command.IdempotencyKey, cancellationToken);
            if (existingId is Guid keyedId)
            {
                if (keyedId == command.VoucherId)
                    return ApplicationResult.Success();
                return ApplicationResult.Conflict(
                    "Idempotency key already used by another receipt voucher.");
            }
        }

        if (!await permissionService.CanAsync("finance.receipt.post", cancellationToken))
            return ApplicationResult.PermissionDenied("Not allowed to post receipt vouchers.");

        var voucher = await voucherRepository.GetByIdAsync(command.VoucherId, cancellationToken);
        if (voucher is null)
            return ApplicationResult.NotFound("Receipt voucher not found.");
        if (voucher.Status == VoucherStatus.Posted)
            return ApplicationResult.Success();
        if (voucher.Status == VoucherStatus.Reversed)
            return ApplicationResult.Conflict("Receipt voucher is reversed.");

        var tenders = await voucherRepository.GetTenderLinesAsync(voucher.Id, cancellationToken);
        if (tenders.Count == 0)
            return ApplicationResult.ValidationFailed("Tender", "سند القبض لا يحتوي على وسيلة دفع.");

        var tenderDtos = tenders.Select(t => new ReceiptTenderLineDto
        {
            PaymentMethodId = t.PaymentMethodId,
            CashboxId = t.CashboxId,
            BankAccountId = t.BankAccountId,
            Amount = t.Amount.Amount,
            Currency = t.Currency,
            ExchangeRate = t.ExchangeRate,
            Reference = t.Reference
        }).ToList();

        var cashbox = await cashboxRepository.GetByIdAsync(voucher.CashboxId, cancellationToken);
        if (cashbox is null)
            return ApplicationResult.NotFound("Cashbox not found.");

        var cashValidation = await cashboxValidator.ValidateForReceiptAsync(
            voucher.CompanyId, voucher.CashboxId, cashbox.Currency, cancellationToken);
        if (!cashValidation.IsValid && tenderDtos.All(t => t.BankAccountId is null))
            return ApplicationResult.ValidationFailed("CashboxId", cashValidation.ErrorMessage ?? "Cashbox cannot post.");

        var customer = await customerRepository.GetByIdAsync(voucher.CustomerId, cancellationToken);
        if (customer is null)
            return ApplicationResult.NotFound("Customer not found.");

        var allocated = await voucherRepository.GetAllocatedTotalAsync(voucher.Id, cancellationToken);
        if (allocated > voucher.Amount.Amount)
            return ApplicationResult.ValidationFailed("Allocations", "التخصيصات تتجاوز مبلغ السند.");

        try
        {
            await unitOfWork.BeginTransactionAsync(cancellationToken);

            if (voucher.Status is VoucherStatus.Draft or VoucherStatus.Submitted)
                voucher.Approve();
            voucher.Post();

            if (customer.Customer.Type == CustomerType.Credit)
                customer.RecordPostedReceipt(voucher.Amount.Amount);
            cashbox.ApplyReceipt(voucher.Amount);

            await voucherRepository.UpdateAsync(voucher, cancellationToken);
            await customerRepository.UpdateAsync(customer, cancellationToken);
            await cashboxRepository.UpdateAsync(cashbox, cancellationToken);

            await receiptPostingService.PostReceiptCollectionAsync(
                voucher.Id,
                voucher.VoucherNumber,
                voucher.CompanyId,
                voucher.CustomerId,
                tenderDtos,
                allocated,
                voucher.Amount.Amount,
                cancellationToken);

            if (!string.IsNullOrWhiteSpace(command.IdempotencyKey))
                await voucherRepository.SetIdempotencyKeyAsync(
                    voucher.Id, command.IdempotencyKey, cancellationToken);

            var recoveryRequests = integratedAccounting.ConsumePendingPostingRequests();
            await postingSaveCoordinator.SaveChangesWithPostingRecoveryAsync(recoveryRequests, cancellationToken);
            await unitOfWork.CommitTransactionAsync(cancellationToken);

            await notificationService.PublishAsync(new ReceiptVoucherPostedNotification
            {
                VoucherId = voucher.Id,
                VoucherNumber = voucher.VoucherNumber,
                Amount = voucher.Amount.Amount
            }, cancellationToken);

            return ApplicationResult.Success();
        }
        catch (Exception ex)
        {
            await unitOfWork.RollbackTransactionAsync(cancellationToken);
            return ex.ToFailureResult();
        }
    }
}

public sealed class CancelReceiptVoucherHandler(
    IReceiptVoucherRepository voucherRepository,
    IUnitOfWork unitOfWork,
    IPermissionService permissionService)
    : ICommandHandler<CancelReceiptVoucherCommand, ApplicationResult>
{
    public async Task<ApplicationResult> HandleAsync(
        CancelReceiptVoucherCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command.VoucherId == Guid.Empty)
            return ApplicationResult.ValidationFailed(nameof(command.VoucherId), "Voucher is required.");
        if (string.IsNullOrWhiteSpace(command.Reason))
            return ApplicationResult.ValidationFailed(nameof(command.Reason), "Cancel reason is required.");

        if (!await permissionService.CanAsync("finance.receipt.cancel", cancellationToken))
            return ApplicationResult.PermissionDenied("Not allowed to cancel receipt vouchers.");

        var voucher = await voucherRepository.GetByIdAsync(command.VoucherId, cancellationToken);
        if (voucher is null)
            return ApplicationResult.NotFound("Receipt voucher not found.");

        try
        {
            voucher.Cancel(command.Reason);
            await voucherRepository.UpdateAsync(voucher, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return ApplicationResult.Success();
        }
        catch (Exception ex)
        {
            return ex.ToFailureResult();
        }
    }
}

public sealed class ReverseReceiptVoucherHandler(
    IReceiptVoucherRepository voucherRepository,
    ICustomerRepository customerRepository,
    ICashboxRepository cashboxRepository,
    IReceiptPostingService receiptPostingService,
    IIntegratedAccountingService integratedAccounting,
    IPostingSaveCoordinator postingSaveCoordinator,
    IUnitOfWork unitOfWork,
    INumberingService numberingService,
    IPermissionService permissionService)
    : ICommandHandler<ReverseReceiptVoucherCommand, ApplicationResult>
{
    public async Task<ApplicationResult> HandleAsync(
        ReverseReceiptVoucherCommand command,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command.Reason))
            return ApplicationResult.ValidationFailed(nameof(command.Reason), "Reversal reason is required.");

        if (!await permissionService.CanAsync("finance.receipt.reverse", cancellationToken))
            return ApplicationResult.PermissionDenied("Not allowed to reverse receipt vouchers.");

        var original = await voucherRepository.GetByIdAsync(command.ReceiptVoucherId, cancellationToken);
        if (original is null)
            return ApplicationResult.NotFound("Receipt voucher not found.");
        if (original.Status != VoucherStatus.Posted)
            return ApplicationResult.Conflict("Only posted receipts can be reversed.");

        var tenders = await voucherRepository.GetTenderLinesAsync(original.Id, cancellationToken);
        var tenderDtos = tenders.Select(t => new ReceiptTenderLineDto
        {
            PaymentMethodId = t.PaymentMethodId,
            CashboxId = t.CashboxId,
            BankAccountId = t.BankAccountId,
            Amount = t.Amount.Amount,
            Currency = t.Currency,
            ExchangeRate = t.ExchangeRate,
            Reference = t.Reference
        }).ToList();

        var customer = await customerRepository.GetByIdAsync(original.CustomerId, cancellationToken);
        var cashbox = await cashboxRepository.GetByIdAsync(original.CashboxId, cancellationToken);
        if (customer is null || cashbox is null)
            return ApplicationResult.NotFound("Customer or cashbox not found.");

        try
        {
            await unitOfWork.BeginTransactionAsync(cancellationToken);

            var reversalNumber = await numberingService.NextReceiptNumberAsync(original.BranchId, cancellationToken);
            var reversal = ReceiptVoucher.CreateReversalDraft(
                original.CompanyId,
                original.BranchId,
                reversalNumber,
                original.CustomerId,
                original.CashboxId,
                original.PaymentMethodId,
                original.Amount,
                original.Id);
            reversal.Approve();
            reversal.Post();
            original.MarkReversed(command.Reason);

            if (customer.Customer.Type == CustomerType.Credit)
                customer.RecordPostedInvoice(original.Amount.Amount);
            cashbox.ApplyPayment(original.Amount);

            await voucherRepository.AddAsync(reversal, cancellationToken);
            await voucherRepository.UpdateAsync(original, cancellationToken);
            await customerRepository.UpdateAsync(customer, cancellationToken);
            await cashboxRepository.UpdateAsync(cashbox, cancellationToken);

            await receiptPostingService.PostReceiptReversalAsync(
                reversal.Id,
                reversal.VoucherNumber,
                original.CompanyId,
                original.CustomerId,
                original.Id,
                tenderDtos,
                cancellationToken);

            var recovery = integratedAccounting.ConsumePendingPostingRequests();
            await postingSaveCoordinator.SaveChangesWithPostingRecoveryAsync(recovery, cancellationToken);
            await unitOfWork.CommitTransactionAsync(cancellationToken);
            return ApplicationResult.Success();
        }
        catch (Exception ex)
        {
            await unitOfWork.RollbackTransactionAsync(cancellationToken);
            return ex.ToFailureResult();
        }
    }
}
