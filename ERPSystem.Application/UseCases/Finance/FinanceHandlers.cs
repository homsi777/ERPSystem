using ERPSystem.Application.Abstractions;
using ERPSystem.Application.Abstractions.Repositories;
using ERPSystem.Application.Abstractions.Services;
using ERPSystem.Application.Commands.Finance;
using ERPSystem.Application.Common;
using ERPSystem.Application.Notifications;
using ERPSystem.Application.Results;
using ERPSystem.Domain.Entities.Finance;
using ERPSystem.Domain.Entities.Purchasing;
using ERPSystem.Domain.ValueObjects;

namespace ERPSystem.Application.UseCases.Finance;

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
    ISupplierRepository supplierRepository,
    IPurchaseInvoiceRepository purchaseInvoiceRepository,
    IPurchaseInvoicePaymentRepository purchasePaymentRepository,
    ICashboxRepository cashboxRepository,
    IIntegratedAccountingService integratedAccounting,
    IPostingSaveCoordinator postingSaveCoordinator,
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

        var supplierAgg = await supplierRepository.GetByIdAsync(voucher.SupplierId, cancellationToken);
        if (supplierAgg is null)
            return ApplicationResult.NotFound("Supplier not found.");

        var cashbox = await cashboxRepository.GetByIdAsync(voucher.CashboxId, cancellationToken);
        if (cashbox is null)
            return ApplicationResult.NotFound("Cashbox not found.");

        try
        {
            await unitOfWork.BeginTransactionAsync(cancellationToken);

            voucher.Approve();
            voucher.Post();
            cashbox.ApplyPayment(voucher.Amount);
            supplierAgg.Supplier.ApplyPostedPayment(voucher.Amount);

            await voucherRepository.UpdateAsync(voucher, cancellationToken);
            await cashboxRepository.UpdateAsync(cashbox, cancellationToken);
            await integratedAccounting.PostPaymentVoucherAsync(
                voucher.Id,
                voucher.VoucherNumber,
                voucher.SupplierId,
                supplierAgg.Supplier.PayablesAccountId,
                AccountingAccountIds.CashUsd,
                voucher.Amount.Amount,
                cancellationToken);

            if (command.PurchaseInvoiceId is Guid invoiceId)
            {
                var invoice = await purchaseInvoiceRepository.GetByIdAsync(invoiceId, cancellationToken);
                if (invoice is not null)
                {
                    invoice.ApplyPayment(voucher.Amount.Amount);
                    await purchasePaymentRepository.AddAsync(
                        PurchaseInvoicePayment.Create(invoiceId, voucher.Id, voucher.Amount),
                        cancellationToken);
                    await purchaseInvoiceRepository.UpdateAsync(invoice, cancellationToken);
                }
            }

            await supplierRepository.UpdateAsync(supplierAgg, cancellationToken);

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
