using ERPSystem.Application.Abstractions;
using ERPSystem.Application.Abstractions.Repositories;
using ERPSystem.Application.Abstractions.Services;
using ERPSystem.Application.Commands.Capital;
using ERPSystem.Application.Common;
using ERPSystem.Application.DTOs.Capital;
using ERPSystem.Application.Mapping;
using ERPSystem.Application.Notifications;
using ERPSystem.Application.Queries.Capital;
using ERPSystem.Application.Results;
using ERPSystem.Domain.Aggregates;
using ERPSystem.Domain.Entities.Capital;
using ERPSystem.Domain.Enums;
using ERPSystem.Domain.Services;

namespace ERPSystem.Application.UseCases.Capital;

public sealed class CreateCapitalPartnerHandler(
    ICapitalPartnerRepository repository,
    IUnitOfWork unitOfWork,
    IPermissionService permissionService,
    INumberingService numberingService,
    ICurrentUserService currentUser,
    INotificationService notificationService)
    : ICommandHandler<CreateCapitalPartnerCommand, ApplicationResult<Guid>>
{
    public async Task<ApplicationResult<Guid>> HandleAsync(
        CreateCapitalPartnerCommand command,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command.FullName))
            return ApplicationResult<Guid>.ValidationFailed(nameof(command.FullName), "Partner name is required.");

        if (!await permissionService.CanAsync("capital.create", cancellationToken))
            return ApplicationResult<Guid>.PermissionDenied("Not allowed to create partners.");

        try
        {
            var code = await numberingService.NextCapitalPartnerCodeAsync(command.BranchId, cancellationToken);
            var partner = CapitalPartner.Create(
                command.CompanyId,
                code,
                command.FullName.Trim(),
                command.DefaultCurrency,
                command.RiskLevel);

            partner.UpdateProfile(
                command.FullName.Trim(),
                command.NationalId,
                command.Phone,
                command.Email,
                command.Address,
                command.Notes,
                command.DefaultCurrency,
                command.RiskLevel);

            var aggregate = CapitalPartnerAggregate.FromPartner(partner);
            await repository.AddAsync(aggregate, cancellationToken);
            await CapitalTrailRecorder.RecordAuditAsync(repository, currentUser, aggregate.Id, "Create", cancellationToken: cancellationToken);
            await CapitalTrailRecorder.RecordTimelineAsync(repository, currentUser, aggregate.Id, "Create", "إنشاء شريك", cancellationToken: cancellationToken);

            await notificationService.PublishAsync(new CapitalPartnerCreatedNotification
            {
                PartnerId = aggregate.Id,
                PartnerCode = partner.Code,
                PartnerName = partner.FullName
            }, cancellationToken);

            await unitOfWork.SaveChangesAsync(cancellationToken);
            return ApplicationResult<Guid>.Success(aggregate.Id);
        }
        catch (Exception ex)
        {
            return ex.ToFailureResult<Guid>();
        }
    }
}

public sealed class CreateCapitalPartnerWithSetupHandler(
    ICapitalPartnerRepository repository,
    IUnitOfWork unitOfWork,
    IPermissionService permissionService,
    INumberingService numberingService,
    ICurrentUserService currentUser,
    INotificationService notificationService)
    : ICommandHandler<CreateCapitalPartnerWithSetupCommand, ApplicationResult<Guid>>
{
    public async Task<ApplicationResult<Guid>> HandleAsync(
        CreateCapitalPartnerWithSetupCommand command,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command.FullName))
            return ApplicationResult<Guid>.ValidationFailed(nameof(command.FullName), "Partner name is required.");

        if (command.OwnershipPercentage <= 0 || command.OwnershipPercentage > 100)
            return ApplicationResult<Guid>.ValidationFailed(nameof(command.OwnershipPercentage), "Ownership must be between 0 and 100.");

        if (command.InitialInvestmentAmount < 0)
            return ApplicationResult<Guid>.ValidationFailed(nameof(command.InitialInvestmentAmount), "Amount cannot be negative.");

        if (!await permissionService.CanAsync("capital.create", cancellationToken))
            return ApplicationResult<Guid>.PermissionDenied("Not allowed to create partners.");

        try
        {
            var code = await numberingService.NextCapitalPartnerCodeAsync(command.BranchId, cancellationToken);
            var partner = CapitalPartner.Create(
                command.CompanyId,
                code,
                command.FullName.Trim(),
                command.DefaultCurrency,
                command.RiskLevel);

            partner.UpdateProfile(
                command.FullName.Trim(),
                command.NationalId,
                command.Phone,
                command.Email,
                command.Address,
                command.Notes,
                command.DefaultCurrency,
                command.RiskLevel);

            partner.SetCompanyOwnership(command.OwnershipPercentage);

            if (command.InitialInvestmentAmount > 0)
            {
                partner.RecordTransaction(
                    CapitalTransactionType.InitialInvestment,
                    command.InitialInvestmentAmount,
                    command.DefaultCurrency,
                    1m,
                    "SAR",
                    ApplicationDateNormalizer.ToUtcDate(DateTime.Today),
                    PartnershipScope.Company,
                    notes: "استثمار أولي عند التسجيل");
            }

            var aggregate = CapitalPartnerAggregate.FromPartner(partner);
            await repository.AddAsync(aggregate, cancellationToken);
            await CapitalTrailRecorder.RecordAuditAsync(repository, currentUser, aggregate.Id, "Create", cancellationToken: cancellationToken);
            await CapitalTrailRecorder.RecordTimelineAsync(repository, currentUser, aggregate.Id, "Create", "إنشاء شريك", cancellationToken: cancellationToken);

            if (command.InitialInvestmentAmount > 0)
            {
                await CapitalTrailRecorder.RecordTimelineAsync(
                    repository, currentUser, aggregate.Id, "Transaction",
                    "استثمار أولي",
                    description: $"{command.InitialInvestmentAmount:N2} {command.DefaultCurrency}",
                    cancellationToken: cancellationToken);
            }

            await notificationService.PublishAsync(new CapitalPartnerCreatedNotification
            {
                PartnerId = aggregate.Id,
                PartnerCode = partner.Code,
                PartnerName = partner.FullName
            }, cancellationToken);

            await unitOfWork.SaveChangesAsync(cancellationToken);
            return ApplicationResult<Guid>.Success(aggregate.Id);
        }
        catch (Exception ex)
        {
            return ex.ToFailureResult<Guid>();
        }
    }
}

public sealed class UpdateCapitalPartnerHandler(
    ICapitalPartnerRepository repository,
    IUnitOfWork unitOfWork,
    IPermissionService permissionService,
    ICurrentUserService currentUser)
    : ICommandHandler<UpdateCapitalPartnerCommand, ApplicationResult>
{
    public async Task<ApplicationResult> HandleAsync(
        UpdateCapitalPartnerCommand command,
        CancellationToken cancellationToken = default)
    {
        if (!await permissionService.CanAsync("capital.edit", cancellationToken))
            return ApplicationResult.PermissionDenied("Not allowed to edit partners.");

        var aggregate = await repository.GetByIdAsync(command.PartnerId, includeChildren: true, cancellationToken);
        if (aggregate is null)
            return ApplicationResult.NotFound("Partner not found.");

        try
        {
            var partner = aggregate.Partner;
            var previousName = partner.FullName;
            partner.UpdateProfile(
                command.FullName.Trim(),
                command.NationalId,
                command.Phone,
                command.Email,
                command.Address,
                command.Notes,
                command.DefaultCurrency,
                command.RiskLevel);

            await repository.UpdateAsync(aggregate, cancellationToken);
            if (!string.Equals(previousName, partner.FullName, StringComparison.Ordinal))
            {
                await CapitalTrailRecorder.RecordAuditAsync(
                    repository, currentUser, partner.Id, "Update",
                    "FullName", previousName, partner.FullName, cancellationToken: cancellationToken);
            }

            await CapitalTrailRecorder.RecordTimelineAsync(
                repository, currentUser, partner.Id, "Update", "تحديث بيانات الشريك", cancellationToken: cancellationToken);

            await unitOfWork.SaveChangesAsync(cancellationToken);
            return ApplicationResult.Success();
        }
        catch (Exception ex)
        {
            return ex.ToFailureResult();
        }
    }
}

public sealed class AddPartnerParticipationHandler(
    ICapitalPartnerRepository repository,
    IUnitOfWork unitOfWork,
    IPermissionService permissionService,
    ICurrentUserService currentUser)
    : ICommandHandler<AddPartnerParticipationCommand, ApplicationResult<Guid>>
{
    public async Task<ApplicationResult<Guid>> HandleAsync(
        AddPartnerParticipationCommand command,
        CancellationToken cancellationToken = default)
    {
        if (!await permissionService.CanAsync("capital.edit", cancellationToken))
            return ApplicationResult<Guid>.PermissionDenied("Not allowed.");

        var aggregate = await repository.GetByIdAsync(command.PartnerId, includeChildren: true, cancellationToken);
        if (aggregate is null)
            return ApplicationResult<Guid>.NotFound("Partner not found.");

        try
        {
            var participation = aggregate.Partner.AddParticipation(
                command.Scope,
                command.OwnershipPercentage,
                command.ProjectCode,
                command.ContainerId,
                command.ContainerNumber);

            await repository.UpdateAsync(aggregate, cancellationToken);
            await CapitalTrailRecorder.RecordTimelineAsync(
                repository, currentUser, command.PartnerId, "Participation",
                $"إضافة مشاركة: {command.Scope.ToArabic()}", cancellationToken: cancellationToken);

            await unitOfWork.SaveChangesAsync(cancellationToken);
            return ApplicationResult<Guid>.Success(participation.Id);
        }
        catch (Exception ex)
        {
            return ex.ToFailureResult<Guid>();
        }
    }
}

public sealed class RecordCapitalTransactionHandler(
    ICapitalPartnerRepository repository,
    IUnitOfWork unitOfWork,
    IPermissionService permissionService,
    ICurrentUserService currentUser)
    : ICommandHandler<RecordCapitalTransactionCommand, ApplicationResult<Guid>>
{
    public async Task<ApplicationResult<Guid>> HandleAsync(
        RecordCapitalTransactionCommand command,
        CancellationToken cancellationToken = default)
    {
        if (!await permissionService.CanAsync("capital.edit", cancellationToken))
            return ApplicationResult<Guid>.PermissionDenied("Not allowed.");

        var aggregate = await repository.GetByIdAsync(command.PartnerId, includeChildren: true, cancellationToken);
        if (aggregate is null)
            return ApplicationResult<Guid>.NotFound("Partner not found.");

        try
        {
            var transactionDate = ApplicationDateNormalizer.ToUtcDate(command.TransactionDate);
            var transaction = aggregate.Partner.RecordTransaction(
                command.Type,
                command.AmountOriginal,
                command.Currency,
                command.ExchangeRate,
                command.BaseCurrency,
                transactionDate,
                command.Scope,
                command.ParticipationId,
                projectCode: command.ProjectCode,
                containerId: command.ContainerId,
                referenceNumber: command.ReferenceNumber,
                notes: command.Notes);

            await repository.UpdateAsync(aggregate, cancellationToken);
            await CapitalTrailRecorder.RecordAuditAsync(
                repository, currentUser, command.PartnerId, "Transaction",
                notes: $"{command.Type.ToArabic()}: {command.AmountOriginal:N2} {command.Currency}",
                cancellationToken: cancellationToken);
            await CapitalTrailRecorder.RecordTimelineAsync(
                repository, currentUser, command.PartnerId, "Transaction",
                command.Type.ToArabic(),
                description: $"{command.AmountOriginal:N2} {command.Currency}",
                cancellationToken: cancellationToken);

            await unitOfWork.SaveChangesAsync(cancellationToken);
            return ApplicationResult<Guid>.Success(transaction.Id);
        }
        catch (Exception ex)
        {
            return ex.ToFailureResult<Guid>();
        }
    }
}

public sealed class SetPartnerCompanyOwnershipHandler(
    ICapitalPartnerRepository repository,
    IUnitOfWork unitOfWork,
    IPermissionService permissionService,
    ICurrentUserService currentUser)
    : ICommandHandler<SetPartnerCompanyOwnershipCommand, ApplicationResult>
{
    public async Task<ApplicationResult> HandleAsync(
        SetPartnerCompanyOwnershipCommand command,
        CancellationToken cancellationToken = default)
    {
        if (!await permissionService.CanAsync("capital.edit", cancellationToken))
            return ApplicationResult.PermissionDenied("Not allowed.");

        var aggregate = await repository.GetByIdAsync(command.PartnerId, includeChildren: true, cancellationToken);
        if (aggregate is null)
            return ApplicationResult.NotFound("Partner not found.");

        try
        {
            aggregate.Partner.SetCompanyOwnership(command.OwnershipPercentage);
            await repository.UpdateAsync(aggregate, cancellationToken);
            await CapitalTrailRecorder.RecordTimelineAsync(
                repository, currentUser, command.PartnerId, "Participation",
                $"تحديث نسبة الملكية: {command.OwnershipPercentage:N2}%", cancellationToken: cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return ApplicationResult.Success();
        }
        catch (Exception ex)
        {
            return ex.ToFailureResult();
        }
    }
}

public sealed class GetCapitalTransactionsHandler(ICapitalPartnerRepository repository)
    : IQueryHandler<GetCapitalTransactionsQuery, ApplicationResult<PagedResult<CapitalTransactionListDto>>>
{
    public async Task<ApplicationResult<PagedResult<CapitalTransactionListDto>>> HandleAsync(
        GetCapitalTransactionsQuery query,
        CancellationToken cancellationToken = default)
    {
        var (items, total) = await repository.GetTransactionsPagedAsync(
            query.CompanyId, query.Filter, query.Page, query.PageSize, cancellationToken);

        var dtos = items.Select(r => new CapitalTransactionListDto
        {
            Id = r.Id,
            PartnerId = r.PartnerId,
            PartnerCode = r.PartnerCode,
            PartnerName = r.PartnerName,
            Type = r.Type,
            TypeDisplay = r.Type.ToArabic(),
            AmountOriginal = r.AmountOriginal,
            Currency = r.Currency,
            SignedBaseAmount = r.SignedBaseAmount,
            TransactionDate = r.TransactionDate,
            Notes = r.Notes
        }).ToList();

        return ApplicationResult<PagedResult<CapitalTransactionListDto>>.Success(new PagedResult<CapitalTransactionListDto>
        {
            Items = dtos,
            TotalCount = total,
            Page = query.Page,
            PageSize = query.PageSize
        });
    }
}

public sealed class ArchiveCapitalPartnerHandler(
    ICapitalPartnerRepository repository,
    IUnitOfWork unitOfWork,
    IPermissionService permissionService,
    ICurrentUserService currentUser)
    : ICommandHandler<ArchiveCapitalPartnerCommand, ApplicationResult>
{
    public async Task<ApplicationResult> HandleAsync(
        ArchiveCapitalPartnerCommand command,
        CancellationToken cancellationToken = default)
    {
        if (!await permissionService.CanAsync("capital.archive", cancellationToken))
            return ApplicationResult.PermissionDenied("Not allowed.");

        var aggregate = await repository.GetByIdAsync(command.PartnerId, cancellationToken: cancellationToken);
        if (aggregate is null)
            return ApplicationResult.NotFound("Partner not found.");

        try
        {
            aggregate.Partner.Archive();
            await repository.UpdateAsync(aggregate, cancellationToken);
            await CapitalTrailRecorder.RecordAuditAsync(
                repository, currentUser, command.PartnerId, "Archive", notes: command.Notes, cancellationToken: cancellationToken);
            await CapitalTrailRecorder.RecordTimelineAsync(
                repository, currentUser, command.PartnerId, "Archive", "أرشفة الشريك", cancellationToken: cancellationToken);

            await unitOfWork.SaveChangesAsync(cancellationToken);
            return ApplicationResult.Success();
        }
        catch (Exception ex)
        {
            return ex.ToFailureResult();
        }
    }
}

public sealed class CreateProfitDistributionHandler(
    ICapitalPartnerRepository repository,
    IUnitOfWork unitOfWork,
    IPermissionService permissionService,
    INumberingService numberingService)
    : ICommandHandler<CreateProfitDistributionCommand, ApplicationResult<Guid>>
{
    public async Task<ApplicationResult<Guid>> HandleAsync(
        CreateProfitDistributionCommand command,
        CancellationToken cancellationToken = default)
    {
        if (!await permissionService.CanAsync("capital.approve", cancellationToken))
            return ApplicationResult<Guid>.PermissionDenied("Not allowed.");

        try
        {
            var code = await numberingService.NextDistributionCodeAsync(command.BranchId, cancellationToken);
            var distribution = ProfitDistribution.CreateDraft(
                command.CompanyId,
                code,
                command.Scope,
                command.PeriodStart,
                command.PeriodEnd,
                command.ProjectCode,
                command.ContainerId);

            distribution.Calculate(command.GrossRevenue, command.TotalCosts);

            var partners = await repository.GetPagedAsync(command.CompanyId, new CapitalPartnerListFilter(), 1, 500, cancellationToken);
            var partnerInputs = partners.Items
                .SelectMany(a => a.Partner.Participations
                    .Where(p => p.IsActive && p.Scope == command.Scope)
                    .Select(p => new ProfitDistributionPartnerInput
                    {
                        PartnerId = a.Partner.Id,
                        OwnershipPercentage = p.OwnershipPercentage
                    }))
                .ToList();

            var result = ProfitDistributionCalculator.Calculate(new ProfitDistributionInput
            {
                GrossRevenue = command.GrossRevenue,
                TotalCosts = command.TotalCosts,
                Partners = partnerInputs
            });

            var lines = result.Lines.Select(l =>
                ProfitDistributionLine.Create(distribution.Id, l.PartnerId, l.OwnershipPercentage, l.PartnerShare, l.CompanyShare));
            distribution.SetLines(lines);
            distribution.SubmitForApproval();

            await repository.AddDistributionAsync(distribution, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return ApplicationResult<Guid>.Success(distribution.Id);
        }
        catch (Exception ex)
        {
            return ex.ToFailureResult<Guid>();
        }
    }
}

public sealed class ApproveProfitDistributionHandler(
    ICapitalPartnerRepository repository,
    IUnitOfWork unitOfWork,
    IPermissionService permissionService)
    : ICommandHandler<ApproveProfitDistributionCommand, ApplicationResult>
{
    public async Task<ApplicationResult> HandleAsync(
        ApproveProfitDistributionCommand command,
        CancellationToken cancellationToken = default)
    {
        if (!await permissionService.CanAsync("capital.approve", cancellationToken))
            return ApplicationResult.PermissionDenied("Not allowed.");

        var distribution = await repository.GetDistributionByIdAsync(command.DistributionId, cancellationToken);
        if (distribution is null)
            return ApplicationResult.NotFound("Distribution not found.");

        try
        {
            distribution.Approve();
            await repository.UpdateDistributionAsync(distribution, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return ApplicationResult.Success();
        }
        catch (Exception ex)
        {
            return ex.ToFailureResult();
        }
    }
}

public sealed class PostProfitDistributionHandler(
    ICapitalPartnerRepository repository,
    IUnitOfWork unitOfWork,
    IPermissionService permissionService,
    ICurrentUserService currentUser)
    : ICommandHandler<PostProfitDistributionCommand, ApplicationResult>
{
    public async Task<ApplicationResult> HandleAsync(
        PostProfitDistributionCommand command,
        CancellationToken cancellationToken = default)
    {
        if (!await permissionService.CanAsync("capital.approve", cancellationToken))
            return ApplicationResult.PermissionDenied("Not allowed.");

        var distribution = await repository.GetDistributionByIdAsync(command.DistributionId, cancellationToken);
        if (distribution is null)
            return ApplicationResult.NotFound("Distribution not found.");

        try
        {
            distribution.Post();

            foreach (var line in distribution.Lines)
            {
                var aggregate = await repository.GetByIdAsync(line.PartnerId, includeChildren: true, cancellationToken);
                if (aggregate is null) continue;

                var txType = line.PartnerShare >= 0
                    ? CapitalTransactionType.ProfitDistribution
                    : CapitalTransactionType.LossDistribution;

                var transaction = aggregate.Partner.RecordTransaction(
                    txType,
                    Math.Abs(line.PartnerShare),
                    distribution.BaseCurrency,
                    1m,
                    distribution.BaseCurrency,
                    DateTime.UtcNow.Date,
                    distribution.Scope,
                    containerId: distribution.ContainerId,
                    projectCode: distribution.ProjectCode,
                    referenceNumber: distribution.Code,
                    notes: $"توزيع {distribution.Code}");

                transaction.LinkDistribution(distribution.Id);
                await repository.UpdateAsync(aggregate, cancellationToken);
                await CapitalTrailRecorder.RecordTimelineAsync(
                    repository, currentUser, line.PartnerId, "Distribution",
                    $"ترحيل توزيع {distribution.Code}", cancellationToken: cancellationToken);
            }

            await repository.UpdateDistributionAsync(distribution, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return ApplicationResult.Success();
        }
        catch (Exception ex)
        {
            return ex.ToFailureResult();
        }
    }
}

public sealed class CloseProfitDistributionHandler(
    ICapitalPartnerRepository repository,
    IUnitOfWork unitOfWork,
    IPermissionService permissionService)
    : ICommandHandler<CloseProfitDistributionCommand, ApplicationResult>
{
    public async Task<ApplicationResult> HandleAsync(
        CloseProfitDistributionCommand command,
        CancellationToken cancellationToken = default)
    {
        if (!await permissionService.CanAsync("capital.approve", cancellationToken))
            return ApplicationResult.PermissionDenied("Not allowed.");

        var distribution = await repository.GetDistributionByIdAsync(command.DistributionId, cancellationToken);
        if (distribution is null)
            return ApplicationResult.NotFound("Distribution not found.");

        try
        {
            distribution.Close();
            await repository.UpdateDistributionAsync(distribution, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return ApplicationResult.Success();
        }
        catch (Exception ex)
        {
            return ex.ToFailureResult();
        }
    }
}

public sealed class GetCapitalPartnerListHandler(ICapitalPartnerRepository repository)
{
    public async Task<ApplicationResult<PagedResult<CapitalPartnerListDto>>> HandleAsync(
        GetCapitalPartnerListQuery query,
        CancellationToken cancellationToken = default)
    {
        var (items, total) = await repository.GetPagedAsync(
            query.CompanyId, query.Filter, query.Page, query.PageSize, cancellationToken);

        var dtos = items.Select(CapitalMapper.ToListDto).ToList();
        return ApplicationResult<PagedResult<CapitalPartnerListDto>>.Success(new PagedResult<CapitalPartnerListDto>
        {
            Items = dtos,
            TotalCount = total,
            Page = query.Page,
            PageSize = query.PageSize
        });
    }
}

public sealed class GetCapitalPartnerDetailsHandler(ICapitalPartnerRepository repository)
{
    public async Task<ApplicationResult<CapitalPartnerDetailsDto>> HandleAsync(
        GetCapitalPartnerDetailsQuery query,
        CancellationToken cancellationToken = default)
    {
        var bundle = await repository.GetWithAuditAsync(query.PartnerId, includeChildren: true, cancellationToken);
        if (bundle is null)
            return ApplicationResult<CapitalPartnerDetailsDto>.NotFound("Partner not found.");

        return ApplicationResult<CapitalPartnerDetailsDto>.Success(
            CapitalMapper.ToDetailsDto(bundle.Aggregate.Partner, bundle.CreatedAt, bundle.CreatedByName, bundle.UpdatedAt));
    }
}

public sealed class GetCapitalPartnerOperationsCenterHandler(ICapitalPartnerRepository repository)
{
    public async Task<ApplicationResult<CapitalOperationsCenterDto>> HandleAsync(
        GetCapitalPartnerOperationsCenterQuery query,
        CancellationToken cancellationToken = default)
    {
        var bundle = await repository.GetWithAuditAsync(query.PartnerId, includeChildren: true, cancellationToken);
        if (bundle is null)
            return ApplicationResult<CapitalOperationsCenterDto>.NotFound("Partner not found.");

        var timeline = await repository.GetTimelineAsync(query.PartnerId, cancellationToken);
        var audit = await repository.GetAuditTrailAsync(query.PartnerId, cancellationToken);

        return ApplicationResult<CapitalOperationsCenterDto>.Success(
            CapitalMapper.ToOperationsCenterDto(bundle, timeline, audit));
    }
}

public sealed class GetCapitalDashboardHandler(ICapitalPartnerRepository repository)
{
    public async Task<ApplicationResult<CapitalDashboardDto>> HandleAsync(
        GetCapitalDashboardQuery query,
        CancellationToken cancellationToken = default)
    {
        var data = await repository.GetDashboardDataAsync(query.CompanyId, cancellationToken);
        return ApplicationResult<CapitalDashboardDto>.Success(CapitalMapper.ToDashboardDto(data));
    }
}

public sealed class GetCapitalPartnerAuditTrailHandler(ICapitalPartnerRepository repository)
{
    public async Task<ApplicationResult<IReadOnlyList<PartnerAuditEntryDto>>> HandleAsync(
        GetCapitalPartnerAuditTrailQuery query,
        CancellationToken cancellationToken = default)
    {
        var entries = await repository.GetAuditTrailAsync(query.PartnerId, cancellationToken);
        var dtos = entries.Select(e => new PartnerAuditEntryDto
        {
            Id = e.Id,
            Action = e.Action,
            FieldName = e.FieldName,
            PreviousValue = e.PreviousValue,
            NewValue = e.NewValue,
            UserName = e.UserName,
            Timestamp = e.Timestamp,
            Notes = e.Notes
        }).ToList();
        return ApplicationResult<IReadOnlyList<PartnerAuditEntryDto>>.Success(dtos);
    }
}

public sealed class GetCapitalPartnerTimelineHandler(ICapitalPartnerRepository repository)
{
    public async Task<ApplicationResult<IReadOnlyList<PartnerTimelineEventDto>>> HandleAsync(
        GetCapitalPartnerTimelineQuery query,
        CancellationToken cancellationToken = default)
    {
        var entries = await repository.GetTimelineAsync(query.PartnerId, cancellationToken);
        var dtos = entries.Select(e => new PartnerTimelineEventDto
        {
            Id = e.Id,
            EventType = e.EventType,
            Title = e.Title,
            Description = e.Description,
            PreviousValue = e.PreviousValue,
            NewValue = e.NewValue,
            UserName = e.UserName,
            Timestamp = e.Timestamp,
            Notes = e.Notes
        }).ToList();
        return ApplicationResult<IReadOnlyList<PartnerTimelineEventDto>>.Success(dtos);
    }
}

public sealed class GetProfitDistributionListHandler(ICapitalPartnerRepository repository)
{
    public async Task<ApplicationResult<IReadOnlyList<ProfitDistributionListDto>>> HandleAsync(
        GetProfitDistributionListQuery query,
        CancellationToken cancellationToken = default)
    {
        var items = await repository.GetDistributionsAsync(query.CompanyId, cancellationToken);
        return ApplicationResult<IReadOnlyList<ProfitDistributionListDto>>.Success(
            items.Select(CapitalMapper.ToDistributionListDto).ToList());
    }
}

public sealed class GetCapitalReportHandler(ICapitalPartnerRepository repository)
{
    public async Task<ApplicationResult<CapitalReportDto>> HandleAsync(
        GetCapitalReportQuery query,
        CancellationToken cancellationToken = default)
    {
        var (partners, _) = await repository.GetPagedAsync(
            query.CompanyId, new CapitalPartnerListFilter(), 1, 1000, cancellationToken);

        var rows = partners.Select(p =>
        {
            var partner = p.Partner;
            return new CapitalReportRowDto
            {
                Key = partner.Code,
                Label = partner.FullName,
                SubLabel = partner.Status.ToArabic(),
                Amount = partner.CurrentCapitalBase,
                Currency = partner.DefaultCurrency
            };
        }).ToList();

        return ApplicationResult<CapitalReportDto>.Success(new CapitalReportDto
        {
            ReportType = query.ReportType,
            Title = query.ReportType switch
            {
                "Statement" => "كشف حساب الشريك",
                "Ledger" => "دفتر الاستثمار",
                "Summary" => "ملخص رأس المال",
                _ => "تقرير رأس المال"
            },
            GeneratedAt = DateTime.UtcNow,
            Rows = rows,
            TotalBase = rows.Sum(r => r.Amount),
            BaseCurrency = "SAR"
        });
    }
}
