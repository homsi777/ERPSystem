using ERPSystem.Application.Abstractions;
using ERPSystem.Application.Abstractions.Repositories;
using ERPSystem.Application.Abstractions.Services;
using ERPSystem.Application.Commands.Accounting;
using ERPSystem.Application.Common;
using ERPSystem.Application.DTOs.Accounting;
using ERPSystem.Application.Queries.Accounting;
using ERPSystem.Application.Results;
using ERPSystem.Domain.Entities.Accounting;
using ERPSystem.Domain.Enums;

namespace ERPSystem.Application.UseCases.Accounting;

public sealed class CreateAccountHandler(
    IAccountRepository accountRepository,
    IUnitOfWork unitOfWork,
    IPermissionService permissionService)
    : ICommandHandler<CreateAccountCommand, ApplicationResult<Guid>>
{
    public async Task<ApplicationResult<Guid>> HandleAsync(
        CreateAccountCommand command,
        CancellationToken cancellationToken = default)
    {
        var validation = Validation.ApplicationValidators.Validate(command);
        if (validation is not null)
            return ApplicationResult<Guid>.ValidationFailed(validation.ValidationErrors);

        if (!await permissionService.CanAsync("accounting.account.create", cancellationToken))
            return ApplicationResult<Guid>.PermissionDenied("Not allowed to create accounts.");

        if (await accountRepository.GetByCodeAsync(command.CompanyId, command.Code, cancellationToken) is not null)
            return ApplicationResult<Guid>.Conflict("Account code already exists.");

        if (command.ParentId is Guid parentId)
        {
            var parent = await accountRepository.GetByIdAsync(parentId, cancellationToken);
            if (parent is null)
                return ApplicationResult<Guid>.NotFound("Parent account not found.");
            if (parent.CompanyId != command.CompanyId)
                return ApplicationResult<Guid>.ValidationFailed(nameof(command.ParentId), "Parent account belongs to another company.");
        }

        var account = Account.Create(
            command.CompanyId,
            command.Code,
            command.NameAr,
            command.NameEn,
            command.AccountType,
            command.ParentId,
            command.IsPostable);

        await accountRepository.AddAsync(account, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return ApplicationResult<Guid>.Success(account.Id);
    }
}

public sealed class UpdateAccountHandler(
    IAccountRepository accountRepository,
    IUnitOfWork unitOfWork,
    IPermissionService permissionService)
    : ICommandHandler<UpdateAccountCommand, ApplicationResult>
{
    public async Task<ApplicationResult> HandleAsync(
        UpdateAccountCommand command,
        CancellationToken cancellationToken = default)
    {
        var validation = Validation.ApplicationValidators.Validate(command);
        if (validation is not null)
            return validation;

        if (!await permissionService.CanAsync("accounting.account.edit", cancellationToken))
            return ApplicationResult.PermissionDenied("Not allowed to edit accounts.");

        var account = await accountRepository.GetByIdAsync(command.AccountId, cancellationToken);
        if (account is null)
            return ApplicationResult.NotFound("Account not found.");

        var duplicate = await accountRepository.GetByCodeAsync(account.CompanyId, command.Code, cancellationToken);
        if (duplicate is not null && duplicate.Id != account.Id)
            return ApplicationResult.Conflict("Account code already exists.");

        if (command.ParentId is Guid parentId)
        {
            if (parentId == account.Id)
                return ApplicationResult.ValidationFailed(nameof(command.ParentId), "Account cannot be its own parent.");

            var parent = await accountRepository.GetByIdAsync(parentId, cancellationToken);
            if (parent is null)
                return ApplicationResult.NotFound("Parent account not found.");
        }

        account.Update(command.Code, command.NameAr, command.NameEn, command.AccountType, command.ParentId, command.IsPostable);
        await accountRepository.UpdateAsync(account, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return ApplicationResult.Success();
    }
}

public sealed class DeactivateAccountHandler(
    IAccountRepository accountRepository,
    IUnitOfWork unitOfWork,
    IPermissionService permissionService)
    : ICommandHandler<DeactivateAccountCommand, ApplicationResult>
{
    public async Task<ApplicationResult> HandleAsync(
        DeactivateAccountCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command.AccountId == Guid.Empty)
            return ApplicationResult.ValidationFailed(nameof(command.AccountId), "Account is required.");

        if (!await permissionService.CanAsync("accounting.account.deactivate", cancellationToken))
            return ApplicationResult.PermissionDenied("Not allowed to deactivate accounts.");

        var account = await accountRepository.GetByIdAsync(command.AccountId, cancellationToken);
        if (account is null)
            return ApplicationResult.NotFound("Account not found.");

        if (await accountRepository.HasChildrenAsync(command.AccountId, cancellationToken))
            return ApplicationResult.Conflict("Cannot deactivate an account that has child accounts.");

        if (await accountRepository.HasJournalLinesAsync(command.AccountId, cancellationToken))
            return ApplicationResult.Conflict("Cannot deactivate an account used in journal entries.");

        account.Deactivate();
        await accountRepository.UpdateAsync(account, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return ApplicationResult.Success();
    }
}

public sealed class GetAccountListHandler(IAccountRepository accountRepository)
{
    public async Task<ApplicationResult<IReadOnlyList<AccountListDto>>> HandleAsync(
        GetAccountListQuery query,
        CancellationToken cancellationToken = default)
    {
        var accounts = await accountRepository.GetListAsync(
            query.CompanyId,
            query.Search,
            query.AccountType,
            query.ActiveOnly,
            cancellationToken);

        var byId = accounts.ToDictionary(a => a.Id);
        var childCounts = accounts
            .Where(a => a.ParentId.HasValue)
            .GroupBy(a => a.ParentId!.Value)
            .ToDictionary(g => g.Key, g => g.Count());

        int LevelOf(Account account)
        {
            var level = 0;
            var current = account;
            while (current.ParentId is Guid pid && byId.TryGetValue(pid, out var parent))
            {
                level++;
                current = parent;
            }
            return level;
        }

        var dtos = accounts
            .OrderBy(a => a.Code)
            .Select(a => new AccountListDto
            {
                Id = a.Id,
                Code = a.Code,
                NameAr = a.NameAr,
                NameEn = a.NameEn,
                AccountType = a.AccountType,
                AccountTypeDisplay = a.AccountType.ToDisplay(),
                ParentId = a.ParentId,
                ParentName = a.ParentId is Guid pid && byId.TryGetValue(pid, out var p) ? p.NameAr : null,
                IsPostable = a.IsPostable,
                IsActive = a.IsActive,
                ChildCount = childCounts.GetValueOrDefault(a.Id),
                Level = LevelOf(a)
            })
            .ToList();

        return ApplicationResult<IReadOnlyList<AccountListDto>>.Success(dtos);
    }
}

public sealed class GetAccountDetailsHandler(IAccountRepository accountRepository)
{
    public async Task<ApplicationResult<AccountDetailsDto>> HandleAsync(
        GetAccountDetailsQuery query,
        CancellationToken cancellationToken = default)
    {
        var account = await accountRepository.GetByIdAsync(query.AccountId, cancellationToken);
        if (account is null)
            return ApplicationResult<AccountDetailsDto>.NotFound("Account not found.");

        var all = await accountRepository.GetListAsync(account.CompanyId, activeOnly: false, cancellationToken: cancellationToken);
        var children = all
            .Where(a => a.ParentId == account.Id)
            .OrderBy(a => a.Code)
            .Select(a => new AccountListDto
            {
                Id = a.Id,
                Code = a.Code,
                NameAr = a.NameAr,
                NameEn = a.NameEn,
                AccountType = a.AccountType,
                AccountTypeDisplay = a.AccountType.ToDisplay(),
                ParentId = a.ParentId,
                IsPostable = a.IsPostable,
                IsActive = a.IsActive
            })
            .ToList();

        var parent = account.ParentId is Guid pid ? all.FirstOrDefault(a => a.Id == pid) : null;

        return ApplicationResult<AccountDetailsDto>.Success(new AccountDetailsDto
        {
            Id = account.Id,
            Code = account.Code,
            NameAr = account.NameAr,
            NameEn = account.NameEn,
            AccountType = account.AccountType,
            AccountTypeDisplay = account.AccountType.ToDisplay(),
            ParentId = account.ParentId,
            ParentName = parent?.NameAr,
            IsPostable = account.IsPostable,
            IsActive = account.IsActive,
            Children = children
        });
    }
}

public sealed class GetPostableAccountsHandler(IAccountRepository accountRepository)
{
    public async Task<ApplicationResult<IReadOnlyList<AccountLookupDto>>> HandleAsync(
        GetPostableAccountsQuery query,
        CancellationToken cancellationToken = default)
    {
        var accounts = await accountRepository.GetListAsync(query.CompanyId, activeOnly: true, cancellationToken: cancellationToken);
        var dtos = accounts
            .Where(a => a.IsPostable)
            .OrderBy(a => a.Code)
            .Select(a => new AccountLookupDto
            {
                Id = a.Id,
                Code = a.Code,
                NameAr = a.NameAr
            })
            .ToList();

        return ApplicationResult<IReadOnlyList<AccountLookupDto>>.Success(dtos);
    }
}
