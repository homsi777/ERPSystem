using ERPSystem.Application.Abstractions;
using ERPSystem.Application.Abstractions.Repositories;
using ERPSystem.Application.Abstractions.Services;
using ERPSystem.Application.Commands.Finance;
using ERPSystem.Application.Common;
using ERPSystem.Application.DTOs.Finance;
using ERPSystem.Application.Mapping;
using ERPSystem.Application.Notifications;
using ERPSystem.Application.Queries.Finance;
using ERPSystem.Application.Results;
using ERPSystem.Domain.Entities.Finance;
using ERPSystem.Domain.Entities.Purchasing;
using ERPSystem.Domain.Enums;
using ERPSystem.Domain.ValueObjects;

namespace ERPSystem.Application.UseCases.Finance;

public sealed class CreatePaymentVoucherHandler(
    IPaymentVoucherRepository voucherRepository,
    IPurchaseInvoiceRepository purchaseInvoiceRepository,
    ICashboxPostingValidator cashboxValidator,
    IBankAccountPostingValidator bankAccountValidator,
    IPaymentMethodRepository paymentMethodRepository,
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

        var methods = await paymentMethodRepository.GetActiveForCompanyAsync(command.CompanyId, cancellationToken);
        var method = methods.FirstOrDefault(m => m.Id == command.PaymentMethodId);
        if (method is null)
            return ApplicationResult<Guid>.ValidationFailed("PaymentMethodId", "Payment method is not configured.");

        if (command.BankAccountId is Guid bankAccountId)
        {
            if (!method.RequiresBankAccount)
                return ApplicationResult<Guid>.ValidationFailed("PaymentMethodId", "Selected payment method is not a bank method.");
            var bankValidation = await bankAccountValidator.ValidateForReceiptAsync(
                command.CompanyId, bankAccountId, command.Currency, command.Reference, cancellationToken);
            if (!bankValidation.IsValid)
                return ApplicationResult<Guid>.ValidationFailed("BankAccountId", bankValidation.ErrorMessage ?? "Invalid bank account.");
        }
        else if (command.CashboxId is Guid cashboxId)
        {
            if (!method.RequiresCashbox)
                return ApplicationResult<Guid>.ValidationFailed("PaymentMethodId", "Selected payment method is not a cash method.");
            var cashValidation = await cashboxValidator.ValidateForReceiptAsync(
                command.CompanyId, cashboxId, command.Currency, cancellationToken);
            if (!cashValidation.IsValid)
                return ApplicationResult<Guid>.ValidationFailed("CashboxId", cashValidation.ErrorMessage ?? "Invalid cashbox.");
        }

        if (command.PurchaseInvoiceId is Guid invoiceId)
        {
            var invoice = await purchaseInvoiceRepository.GetByIdAsync(invoiceId, cancellationToken);
            if (invoice is null)
                return ApplicationResult<Guid>.NotFound("Purchase invoice not found.");
            if (invoice.SupplierId != command.SupplierId)
                return ApplicationResult<Guid>.ValidationFailed("PurchaseInvoiceId", "Invoice does not belong to the selected supplier.");
            if (command.Amount > invoice.Remaining.Amount)
                return ApplicationResult<Guid>.ValidationFailed("Amount", "Payment exceeds the invoice remaining balance.");
            if (!string.Equals(invoice.CurrencyCode, command.Currency, StringComparison.OrdinalIgnoreCase))
                return ApplicationResult<Guid>.ValidationFailed("Currency", "Payment currency must match the purchase invoice.");
        }

        try
        {
            var number = await numberingService.NextPaymentNumberAsync(command.BranchId, cancellationToken);
            var voucher = PaymentVoucher.CreateDraft(
                command.CompanyId,
                command.BranchId,
                number,
                command.SupplierId,
                command.CashboxId,
                command.BankAccountId,
                command.PaymentMethodId,
                new Money(command.Amount, command.Currency),
                command.PurchaseInvoiceId,
                command.Reference);

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
    ICashboxPostingValidator cashboxValidator,
    IBankAccountPostingValidator bankAccountValidator,
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
        if (voucher.Status == VoucherStatus.Posted)
            return ApplicationResult.Success();

        var supplierAgg = await supplierRepository.GetByIdAsync(voucher.SupplierId, cancellationToken);
        if (supplierAgg is null)
            return ApplicationResult.NotFound("Supplier not found.");

        Cashbox? cashbox = null;
        Guid sourceAccountId;
        if (voucher.CashboxId is Guid cashboxId)
        {
            cashbox = await cashboxRepository.GetByIdAsync(cashboxId, cancellationToken);
            if (cashbox is null)
                return ApplicationResult.NotFound("Cashbox not found.");
            var validation = await cashboxValidator.ValidateForReceiptAsync(
                voucher.CompanyId, cashboxId, voucher.Amount.Currency, cancellationToken);
            if (!validation.IsValid || validation.ResolvedAccountId is not Guid accountId)
                return ApplicationResult.ValidationFailed("CashboxId", validation.ErrorMessage ?? "Cashbox cannot post.");
            sourceAccountId = accountId;
        }
        else if (voucher.BankAccountId is Guid bankAccountId)
        {
            var validation = await bankAccountValidator.ValidateForReceiptAsync(
                voucher.CompanyId, bankAccountId, voucher.Amount.Currency, voucher.Reference, cancellationToken);
            if (!validation.IsValid || validation.ResolvedAccountId is not Guid accountId)
                return ApplicationResult.ValidationFailed("BankAccountId", validation.ErrorMessage ?? "Bank account cannot post.");
            sourceAccountId = accountId;
        }
        else
        {
            return ApplicationResult.ValidationFailed("PaymentSource", "Payment voucher has no payment source.");
        }

        PurchaseInvoice? invoice = null;
        var invoiceId = command.PurchaseInvoiceId ?? voucher.PurchaseInvoiceId;
        if (invoiceId is Guid resolvedInvoiceId)
        {
            invoice = await purchaseInvoiceRepository.GetByIdAsync(resolvedInvoiceId, cancellationToken);
            if (invoice is null)
                return ApplicationResult.NotFound("Purchase invoice not found.");
            if (invoice.SupplierId != voucher.SupplierId)
                return ApplicationResult.ValidationFailed("PurchaseInvoiceId", "Invoice does not belong to the voucher supplier.");
            if (voucher.Amount.Amount > invoice.Remaining.Amount)
                return ApplicationResult.ValidationFailed("Amount", "Payment exceeds the invoice remaining balance.");
        }

        try
        {
            await unitOfWork.BeginTransactionAsync(cancellationToken);

            voucher.Approve();
            voucher.Post();
            cashbox?.ApplyPayment(voucher.Amount);
            supplierAgg.Supplier.ApplyPostedPayment(voucher.Amount);

            await voucherRepository.UpdateAsync(voucher, cancellationToken);
            if (cashbox is not null)
                await cashboxRepository.UpdateAsync(cashbox, cancellationToken);
            await integratedAccounting.PostPaymentVoucherAsync(
                voucher.Id,
                voucher.VoucherNumber,
                voucher.SupplierId,
                supplierAgg.Supplier.PayablesAccountId,
                sourceAccountId,
                voucher.Amount.Amount,
                cancellationToken);

            if (invoice is not null && invoiceId is Guid allocatedInvoiceId)
            {
                invoice.ApplyPayment(voucher.Amount.Amount);
                await purchasePaymentRepository.AddAsync(
                    PurchaseInvoicePayment.Create(allocatedInvoiceId, voucher.Id, voucher.Amount),
                    cancellationToken);
                await purchaseInvoiceRepository.UpdateAsync(invoice, cancellationToken);
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

public sealed class ApprovePaymentVoucherHandler(
    IPaymentVoucherRepository voucherRepository,
    IUnitOfWork unitOfWork,
    IPermissionService permissionService)
    : ICommandHandler<ApprovePaymentVoucherCommand, ApplicationResult>
{
    public async Task<ApplicationResult> HandleAsync(ApprovePaymentVoucherCommand command, CancellationToken cancellationToken = default)
    {
        if (!await permissionService.CanAsync("finance.payment.post", cancellationToken))
            return ApplicationResult.PermissionDenied("Not allowed to approve payment vouchers.");

        var voucher = await voucherRepository.GetByIdAsync(command.VoucherId, cancellationToken);
        if (voucher is null)
            return ApplicationResult.NotFound("Payment voucher not found.");
        if (voucher.Status is VoucherStatus.Approved or VoucherStatus.Posted)
            return ApplicationResult.Success();

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

public sealed class GetPaymentVoucherDetailsHandler(
    IPaymentVoucherRepository voucherRepository,
    ISupplierRepository supplierRepository,
    ICashboxRepository cashboxRepository,
    IBankAccountRepository bankAccountRepository)
    : IQueryHandler<GetPaymentVoucherDetailsQuery, ApplicationResult<PaymentVoucherDto>>
{
    public async Task<ApplicationResult<PaymentVoucherDto>> HandleAsync(GetPaymentVoucherDetailsQuery query, CancellationToken cancellationToken = default)
    {
        var voucher = await voucherRepository.GetByIdAsync(query.VoucherId, cancellationToken);
        if (voucher is null)
            return ApplicationResult<PaymentVoucherDto>.NotFound("Payment voucher not found.");

        var supplier = await supplierRepository.GetByIdAsync(voucher.SupplierId, cancellationToken);
        var dto = FinanceMapper.ToDto(voucher, supplier?.Supplier.NameAr ?? "");
        var sourceName = voucher.CashboxId is Guid cashboxId
            ? (await cashboxRepository.GetByIdAsync(cashboxId, cancellationToken))?.Name ?? ""
            : voucher.BankAccountId is Guid bankId
                ? (await bankAccountRepository.GetByIdAsync(bankId, cancellationToken))?.Name ?? ""
                : "";
        dto = dto with { PaymentSourceName = sourceName };
        return ApplicationResult<PaymentVoucherDto>.Success(dto);
    }
}

public sealed class GetPaymentVoucherListHandler(
    IPaymentVoucherRepository voucherRepository,
    GetPaymentVoucherDetailsHandler detailsHandler)
    : IQueryHandler<GetPaymentVoucherListQuery, ApplicationResult<IReadOnlyList<PaymentVoucherDto>>>
{
    public async Task<ApplicationResult<IReadOnlyList<PaymentVoucherDto>>> HandleAsync(GetPaymentVoucherListQuery query, CancellationToken cancellationToken = default)
    {
        var vouchers = await voucherRepository.GetListAsync(query.CompanyId, query.Status, query.SupplierId, cancellationToken);
        var result = new List<PaymentVoucherDto>(vouchers.Count);
        foreach (var voucher in vouchers)
        {
            var details = await detailsHandler.HandleAsync(new GetPaymentVoucherDetailsQuery { VoucherId = voucher.Id }, cancellationToken);
            if (details.IsSuccess && details.Value is not null)
                result.Add(details.Value);
        }
        return ApplicationResult<IReadOnlyList<PaymentVoucherDto>>.Success(result);
    }
}

/// <summary>Read model for printing a receipt voucher. Every field is resolved from the actual stored voucher/cashbox/payment-method/allocation records — nothing free-text or invented.</summary>
public sealed class GetReceiptVoucherPrintHandler(
    IReceiptVoucherRepository voucherRepository,
    ICustomerRepository customerRepository,
    ICashboxRepository cashboxRepository,
    IPaymentMethodRepository paymentMethodRepository,
    ISalesInvoiceRepository salesInvoiceRepository)
    : IQueryHandler<GetReceiptVoucherPrintQuery, ApplicationResult<ReceiptVoucherPrintDto>>
{
    public async Task<ApplicationResult<ReceiptVoucherPrintDto>> HandleAsync(
        GetReceiptVoucherPrintQuery query,
        CancellationToken cancellationToken = default)
    {
        var voucher = await voucherRepository.GetByIdAsync(query.VoucherId, cancellationToken);
        if (voucher is null)
            return ApplicationResult<ReceiptVoucherPrintDto>.NotFound("Receipt voucher not found.");

        var customer = await customerRepository.GetByIdAsync(voucher.CustomerId, cancellationToken);
        var cashbox = await cashboxRepository.GetByIdAsync(voucher.CashboxId, cancellationToken);
        var paymentMethods = await paymentMethodRepository.GetActiveForCompanyAsync(voucher.CompanyId, cancellationToken);
        var paymentMethodName = paymentMethods.FirstOrDefault(m => m.Id == voucher.PaymentMethodId)?.Name ?? "";

        var allocations = new List<ReceiptVoucherAllocationDto>();
        foreach (var allocation in voucher.Allocations)
        {
            var invoice = await salesInvoiceRepository.GetByIdAsync(allocation.SalesInvoiceId, cancellationToken);
            allocations.Add(new ReceiptVoucherAllocationDto
            {
                InvoiceNumber = invoice?.InvoiceNumber.Value ?? "",
                Amount = allocation.Amount.Amount
            });
        }

        return ApplicationResult<ReceiptVoucherPrintDto>.Success(new ReceiptVoucherPrintDto
        {
            Id = voucher.Id,
            VoucherNumber = voucher.VoucherNumber,
            VoucherDate = voucher.VoucherDate,
            Status = voucher.Status,
            CustomerName = customer?.Customer.NameAr ?? "",
            CustomerPhone = customer?.Customer.Phone?.Value,
            CashboxName = cashbox?.Name ?? "",
            Currency = cashbox?.Currency ?? "USD",
            Amount = voucher.Amount.Amount,
            PaymentMethodName = paymentMethodName,
            Allocations = allocations
        });
    }
}

/// <summary>Read model for printing a payment voucher. Every field is resolved from the actual stored voucher/cashbox/supplier records.</summary>
public sealed class GetPaymentVoucherPrintHandler(
    IPaymentVoucherRepository voucherRepository,
    ISupplierRepository supplierRepository,
    ICashboxRepository cashboxRepository,
    IBankAccountRepository bankAccountRepository)
    : IQueryHandler<GetPaymentVoucherPrintQuery, ApplicationResult<PaymentVoucherPrintDto>>
{
    public async Task<ApplicationResult<PaymentVoucherPrintDto>> HandleAsync(
        GetPaymentVoucherPrintQuery query,
        CancellationToken cancellationToken = default)
    {
        var voucher = await voucherRepository.GetByIdAsync(query.VoucherId, cancellationToken);
        if (voucher is null)
            return ApplicationResult<PaymentVoucherPrintDto>.NotFound("Payment voucher not found.");

        var supplier = await supplierRepository.GetByIdAsync(voucher.SupplierId, cancellationToken);
        var cashbox = voucher.CashboxId is Guid cashboxId
            ? await cashboxRepository.GetByIdAsync(cashboxId, cancellationToken)
            : null;
        var bank = voucher.BankAccountId is Guid bankId
            ? await bankAccountRepository.GetByIdAsync(bankId, cancellationToken)
            : null;

        return ApplicationResult<PaymentVoucherPrintDto>.Success(new PaymentVoucherPrintDto
        {
            Id = voucher.Id,
            VoucherNumber = voucher.VoucherNumber,
            VoucherDate = voucher.VoucherDate,
            Status = voucher.Status,
            SupplierName = supplier?.Supplier.NameAr ?? "",
            CashboxName = cashbox?.Name ?? "",
            BankAccountName = bank?.Name ?? "",
            PaymentSourceName = cashbox?.Name ?? bank?.Name ?? "",
            Reference = voucher.Reference,
            Currency = cashbox?.Currency ?? bank?.Currency ?? voucher.Amount.Currency,
            Amount = voucher.Amount.Amount
        });
    }
}
