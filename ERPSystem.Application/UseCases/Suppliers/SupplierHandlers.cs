using ERPSystem.Application.Abstractions;
using ERPSystem.Application.Abstractions.Repositories;
using ERPSystem.Application.Abstractions.Services;
using ERPSystem.Application.Commands.Suppliers;
using ERPSystem.Application.Common;
using ERPSystem.Application.Results;
using ERPSystem.Domain.Aggregates;
using ERPSystem.Domain.Entities.Accounting;
using ERPSystem.Domain.Entities.Parties;
using ERPSystem.Domain.Enums;
using ERPSystem.Domain.ValueObjects;

namespace ERPSystem.Application.UseCases.Suppliers;

public sealed class CreateSupplierHandler(
    ISupplierRepository supplierRepository,
    IAccountRepository accountRepository,
    IUnitOfWork unitOfWork,
    IPermissionService permissionService)
    : ICommandHandler<CreateSupplierCommand, ApplicationResult<Guid>>
{
    public async Task<ApplicationResult<Guid>> HandleAsync(
        CreateSupplierCommand command,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command.NameAr))
            return ApplicationResult<Guid>.ValidationFailed(nameof(command.NameAr), "Supplier name is required.");
        if (string.IsNullOrWhiteSpace(command.Code))
            return ApplicationResult<Guid>.ValidationFailed(nameof(command.Code), "Supplier code is required.");

        if (!await permissionService.CanAsync("suppliers.create", cancellationToken))
            return ApplicationResult<Guid>.PermissionDenied("Not allowed to create suppliers.");

        if (await supplierRepository.ExistsByCodeAsync(command.CompanyId, command.Code.Trim(), cancellationToken: cancellationToken))
            return ApplicationResult<Guid>.ValidationFailed(nameof(command.Code), "Supplier code already exists.");

        try
        {
            var payablesAccountId = command.PayablesAccountId ?? AccountingAccountIds.AccountsPayable;
            if (!command.PayablesAccountId.HasValue)
            {
                var accountCode = $"2110-{SanitizeAccountCode(command.Code)}";
                if (await accountRepository.GetByCodeAsync(command.CompanyId, accountCode, cancellationToken) is not null)
                    accountCode = $"2110-{Guid.NewGuid():N}"[..20];

                var account = Account.Create(
                    command.CompanyId,
                    accountCode,
                    $"ذمم — {command.NameAr.Trim()}",
                    $"AP — {command.NameEn.Trim()}",
                    GlAccountType.Liability,
                    AccountingAccountIds.AccountsPayable);

                await accountRepository.AddAsync(account, cancellationToken);
                payablesAccountId = account.Id;
            }

            var supplier = Supplier.Create(
                command.CompanyId,
                command.Code.Trim(),
                command.NameAr.Trim(),
                command.NameEn.Trim(),
                payablesAccountId,
                string.IsNullOrWhiteSpace(command.CurrencyCode) ? "USD" : command.CurrencyCode.Trim(),
                command.PaymentTermsDays,
                new Money(command.CreditLimit, command.CurrencyCode));

            supplier.UpdateProfile(
                command.NameAr.Trim(),
                command.NameEn.Trim(),
                command.PaymentTermsDays,
                new Money(command.CreditLimit, command.CurrencyCode),
                command.Phone?.Trim(),
                command.Email?.Trim(),
                command.Address?.Trim(),
                command.Country?.Trim(),
                command.City?.Trim(),
                command.TaxNumber?.Trim(),
                payablesAccountId,
                command.Notes?.Trim());

            var aggregate = SupplierAggregate.FromSupplier(supplier);
            await supplierRepository.AddAsync(aggregate, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            return ApplicationResult<Guid>.Success(aggregate.Id);
        }
        catch (Exception ex)
        {
            return ex.ToFailureResult<Guid>();
        }
    }

    private static string SanitizeAccountCode(string code) =>
        new(code.Where(char.IsLetterOrDigit).ToArray());
}

public sealed class UpdateSupplierHandler(
    ISupplierRepository supplierRepository,
    IUnitOfWork unitOfWork,
    IPermissionService permissionService)
    : ICommandHandler<UpdateSupplierCommand, ApplicationResult>
{
    public async Task<ApplicationResult> HandleAsync(
        UpdateSupplierCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command.SupplierId == Guid.Empty)
            return ApplicationResult.ValidationFailed(nameof(command.SupplierId), "Supplier is required.");
        if (string.IsNullOrWhiteSpace(command.NameAr))
            return ApplicationResult.ValidationFailed(nameof(command.NameAr), "Supplier name is required.");

        if (!await permissionService.CanAsync("suppliers.create", cancellationToken))
            return ApplicationResult.PermissionDenied("Not allowed to update suppliers.");

        var aggregate = await supplierRepository.GetByIdAsync(command.SupplierId, cancellationToken);
        if (aggregate is null)
            return ApplicationResult.NotFound("Supplier not found.");

        try
        {
            aggregate.Supplier.UpdateProfile(
                command.NameAr.Trim(),
                command.NameEn.Trim(),
                command.PaymentTermsDays,
                new Money(command.CreditLimit, command.CurrencyCode),
                command.Phone?.Trim(),
                command.Email?.Trim(),
                command.Address?.Trim(),
                command.Country?.Trim(),
                command.City?.Trim(),
                command.TaxNumber?.Trim(),
                command.PayablesAccountId,
                command.Notes?.Trim());

            await supplierRepository.UpdateAsync(aggregate, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return ApplicationResult.Success();
        }
        catch (Exception ex)
        {
            return ex.ToFailureResult();
        }
    }
}

public sealed class DeactivateSupplierHandler(
    ISupplierRepository supplierRepository,
    IUnitOfWork unitOfWork,
    IPermissionService permissionService)
    : ICommandHandler<DeactivateSupplierCommand, ApplicationResult>
{
    public async Task<ApplicationResult> HandleAsync(
        DeactivateSupplierCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command.SupplierId == Guid.Empty)
            return ApplicationResult.ValidationFailed(nameof(command.SupplierId), "Supplier is required.");

        if (!await permissionService.CanAsync("suppliers.deactivate", cancellationToken))
            return ApplicationResult.PermissionDenied("Not allowed to deactivate suppliers.");

        var aggregate = await supplierRepository.GetByIdAsync(command.SupplierId, cancellationToken);
        if (aggregate is null)
            return ApplicationResult.NotFound("Supplier not found.");

        try
        {
            aggregate.Supplier.Deactivate();
            await supplierRepository.UpdateAsync(aggregate, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return ApplicationResult.Success();
        }
        catch (Exception ex)
        {
            return ex.ToFailureResult();
        }
    }
}

public sealed class PostSupplierOpeningBalanceHandler(
    ISupplierRepository supplierRepository,
    IIntegratedAccountingService integratedAccounting,
    IUnitOfWork unitOfWork,
    IPermissionService permissionService)
    : ICommandHandler<PostSupplierOpeningBalanceCommand, ApplicationResult<Application.DTOs.Suppliers.SupplierOpeningBalanceResultDto>>
{
    public async Task<ApplicationResult<Application.DTOs.Suppliers.SupplierOpeningBalanceResultDto>> HandleAsync(
        PostSupplierOpeningBalanceCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command.SupplierId == Guid.Empty)
            return ApplicationResult<Application.DTOs.Suppliers.SupplierOpeningBalanceResultDto>.ValidationFailed(nameof(command.SupplierId), "Supplier is required.");
        if (command.Amount <= 0)
            return ApplicationResult<Application.DTOs.Suppliers.SupplierOpeningBalanceResultDto>.ValidationFailed(nameof(command.Amount), "Opening balance must be greater than zero.");

        if (!await permissionService.CanAsync("suppliers.opening-balance", cancellationToken))
            return ApplicationResult<Application.DTOs.Suppliers.SupplierOpeningBalanceResultDto>.PermissionDenied("Not allowed to post supplier opening balances.");

        var aggregate = await supplierRepository.GetByIdAsync(command.SupplierId, cancellationToken);
        if (aggregate is null)
            return ApplicationResult<Application.DTOs.Suppliers.SupplierOpeningBalanceResultDto>.NotFound("Supplier not found.");

        if (aggregate.Supplier.OpeningBalancePosted)
            return ApplicationResult<Application.DTOs.Suppliers.SupplierOpeningBalanceResultDto>.ValidationFailed(nameof(command.SupplierId), "Opening balance already posted for this supplier.");

        try
        {
            var entryNumber = await integratedAccounting.PostSupplierOpeningBalanceAsync(
                aggregate.Id,
                aggregate.Supplier.PayablesAccountId,
                command.Amount,
                command.PostingDate,
                command.ReferenceNote ?? $"رصيد افتتاحي — {aggregate.Supplier.NameAr}",
                cancellationToken);

            aggregate.Supplier.MarkOpeningBalancePosted(command.Amount);
            await supplierRepository.UpdateAsync(aggregate, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            return ApplicationResult<Application.DTOs.Suppliers.SupplierOpeningBalanceResultDto>.Success(
                new Application.DTOs.Suppliers.SupplierOpeningBalanceResultDto
                {
                    JournalEntryNumber = entryNumber,
                    PostedDate = command.PostingDate,
                    Amount = command.Amount
                });
        }
        catch (Exception ex)
        {
            return ex.ToFailureResult<Application.DTOs.Suppliers.SupplierOpeningBalanceResultDto>();
        }
    }
}
