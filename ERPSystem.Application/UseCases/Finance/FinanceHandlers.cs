using ERPSystem.Application.Abstractions;
using ERPSystem.Application.Abstractions.Repositories;
using ERPSystem.Application.Abstractions.Services;
using ERPSystem.Application.Commands.Finance;
using ERPSystem.Application.Common;
using ERPSystem.Application.Notifications;
using ERPSystem.Application.Results;
using ERPSystem.Domain.Entities.Finance;
using ERPSystem.Domain.ValueObjects;

namespace ERPSystem.Application.UseCases.Finance;

public sealed class CreateReceiptVoucherHandler(
    IReceiptVoucherRepository voucherRepository,
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

        try
        {
            var number = await numberingService.NextReceiptNumberAsync(command.BranchId, cancellationToken);
            var voucher = ReceiptVoucher.CreateDraft(
                number,
                command.CustomerId,
                command.CashboxId,
                new Money(command.Amount));

            Domain.Validators.ReceiptVoucherValidator.Validate(voucher);

            await voucherRepository.AddAsync(voucher, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            return ApplicationResult<Guid>.Success(voucher.Id);
        }
        catch (Exception ex)
        {
            return ex.ToFailureResult<Guid>();
        }
    }
}

public sealed class PostReceiptVoucherHandler(
    IReceiptVoucherRepository voucherRepository,
    ICustomerRepository customerRepository,
    ICashboxRepository cashboxRepository,
    IIntegratedAccountingService integratedAccounting,
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

        if (!await permissionService.CanAsync("finance.receipt.post", cancellationToken))
            return ApplicationResult.PermissionDenied("Not allowed to post receipt vouchers.");

        var voucher = await voucherRepository.GetByIdAsync(command.VoucherId, cancellationToken);
        if (voucher is null)
            return ApplicationResult.NotFound("Receipt voucher not found.");

        var customer = await customerRepository.GetByIdAsync(voucher.CustomerId, cancellationToken);
        if (customer is null)
            return ApplicationResult.NotFound("Customer not found.");

        var cashbox = await cashboxRepository.GetByIdAsync(voucher.CashboxId, cancellationToken);
        if (cashbox is null)
            return ApplicationResult.NotFound("Cashbox not found.");

        try
        {
            if (voucher.Status == Domain.Enums.VoucherStatus.Draft)
                voucher.Approve();

            voucher.Post();
            customer.RecordPostedReceipt(voucher.Amount.Amount);
            cashbox.ApplyReceipt(voucher.Amount);

            await voucherRepository.UpdateAsync(voucher, cancellationToken);
            await customerRepository.UpdateAsync(customer, cancellationToken);
            await cashboxRepository.UpdateAsync(cashbox, cancellationToken);
            await integratedAccounting.PostReceiptVoucherAsync(
                voucher.Id,
                voucher.VoucherNumber,
                voucher.CustomerId,
                voucher.Amount.Amount,
                cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);

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
            return ex.ToFailureResult();
        }
    }
}

public sealed class CreatePaymentVoucherHandler(
    IPaymentVoucherRepository voucherRepository,
    IUnitOfWork unitOfWork,
    INumberingService numberingService,
    IPermissionService permissionService)
    : ICommandHandler<CreatePaymentVoucherCommand, ApplicationResult<Guid>>
{
    public async Task<ApplicationResult<Guid>> HandleAsync(
        CreatePaymentVoucherCommand command,
        CancellationToken cancellationToken = default)
    {
        var validation = Validation.ApplicationValidators.Validate(command);
        if (validation is not null)
            return ApplicationResult<Guid>.ValidationFailed(validation.ValidationErrors);

        if (!await permissionService.CanAsync("finance.payment.create", cancellationToken))
            return ApplicationResult<Guid>.PermissionDenied("Not allowed to create payment vouchers.");

        try
        {
            var number = await numberingService.NextPaymentNumberAsync(command.BranchId, cancellationToken);
            var voucher = PaymentVoucher.CreateDraft(
                number,
                command.SupplierId,
                command.CashboxId,
                new Money(command.Amount));

            Domain.Validators.PaymentVoucherValidator.Validate(voucher);

            await voucherRepository.AddAsync(voucher, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            return ApplicationResult<Guid>.Success(voucher.Id);
        }
        catch (Exception ex)
        {
            return ex.ToFailureResult<Guid>();
        }
    }
}

public sealed class PostPaymentVoucherHandler(
    IPaymentVoucherRepository voucherRepository,
    ICashboxRepository cashboxRepository,
    IIntegratedAccountingService integratedAccounting,
    IUnitOfWork unitOfWork,
    IPermissionService permissionService)
    : ICommandHandler<PostPaymentVoucherCommand, ApplicationResult>
{
    public async Task<ApplicationResult> HandleAsync(
        PostPaymentVoucherCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command.VoucherId == Guid.Empty)
            return ApplicationResult.ValidationFailed(nameof(command.VoucherId), "Voucher is required.");

        if (!await permissionService.CanAsync("finance.payment.post", cancellationToken))
            return ApplicationResult.PermissionDenied("Not allowed to post payment vouchers.");

        var voucher = await voucherRepository.GetByIdAsync(command.VoucherId, cancellationToken);
        if (voucher is null)
            return ApplicationResult.NotFound("Payment voucher not found.");

        var cashbox = await cashboxRepository.GetByIdAsync(voucher.CashboxId, cancellationToken);
        if (cashbox is null)
            return ApplicationResult.NotFound("Cashbox not found.");

        try
        {
            voucher.Approve();
            voucher.Post();
            cashbox.ApplyPayment(voucher.Amount);

            await voucherRepository.UpdateAsync(voucher, cancellationToken);
            await cashboxRepository.UpdateAsync(cashbox, cancellationToken);
            await integratedAccounting.PostPaymentVoucherAsync(
                voucher.Id,
                voucher.VoucherNumber,
                voucher.SupplierId,
                voucher.Amount.Amount,
                cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            return ApplicationResult.Success();
        }
        catch (Exception ex)
        {
            return ex.ToFailureResult();
        }
    }
}
