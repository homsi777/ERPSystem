using ERPSystem.Application.Abstractions;
using ERPSystem.Application.Abstractions.Repositories;
using ERPSystem.Application.Abstractions.Services;
using ERPSystem.Application.Commands.Finance;
using ERPSystem.Application.Common;
using ERPSystem.Application.DTOs.Finance;
using ERPSystem.Application.Mapping;
using ERPSystem.Application.Queries.Finance;
using ERPSystem.Application.Results;
using ERPSystem.Domain.Entities.Finance;

namespace ERPSystem.Application.UseCases.Finance;

public sealed class CreateOpeningBalanceHandler(
    IOpeningBalanceEngine engine,
    IPermissionService permissions)
    : ICommandHandler<CreateOpeningBalanceCommand, ApplicationResult<OpeningBalanceListDto>>
{
    public async Task<ApplicationResult<OpeningBalanceListDto>> HandleAsync(
        CreateOpeningBalanceCommand command, CancellationToken cancellationToken = default)
    {
        if (!await OpeningBalanceAuthorization.CanCreateAsync(permissions, command.Type, cancellationToken))
            return ApplicationResult<OpeningBalanceListDto>.PermissionDenied("غير مسموح بإنشاء أرصدة افتتاحية.");
        return await engine.CreateAsync(command, cancellationToken);
    }
}

public sealed class UpdateOpeningBalanceHandler(
    IOpeningBalanceEngine engine,
    IPermissionService permissions,
    IOpeningBalanceRepository repository)
    : ICommandHandler<UpdateOpeningBalanceCommand, ApplicationResult<OpeningBalanceListDto>>
{
    public async Task<ApplicationResult<OpeningBalanceListDto>> HandleAsync(
        UpdateOpeningBalanceCommand command, CancellationToken cancellationToken = default)
    {
        var doc = await repository.GetAsync(command.DocumentId, cancellationToken);
        if (doc is null)
            return ApplicationResult<OpeningBalanceListDto>.NotFound("المستند غير موجود.");
        if (!await OpeningBalanceAuthorization.CanEditAsync(permissions, doc.Type, cancellationToken))
            return ApplicationResult<OpeningBalanceListDto>.PermissionDenied("غير مسموح بتعديل أرصدة افتتاحية.");
        return await engine.UpdateAsync(command, cancellationToken);
    }
}

public sealed class SubmitOpeningBalanceHandler(
    IOpeningBalanceEngine engine,
    IPermissionService permissions,
    IOpeningBalanceRepository repository)
    : ICommandHandler<SubmitOpeningBalanceCommand, ApplicationResult>
{
    public async Task<ApplicationResult> HandleAsync(
        SubmitOpeningBalanceCommand command, CancellationToken cancellationToken = default)
    {
        if (!await OpeningBalanceAuthorization.CanWorkflowAsync(
                permissions, repository, command.DocumentId, "openingbalances.edit", cancellationToken))
            return ApplicationResult.PermissionDenied("غير مسموح.");
        return await engine.SubmitAsync(command.DocumentId, cancellationToken);
    }
}

public sealed class ApproveOpeningBalanceHandler(
    IOpeningBalanceEngine engine,
    IPermissionService permissions,
    IOpeningBalanceRepository repository)
    : ICommandHandler<ApproveOpeningBalanceCommand, ApplicationResult>
{
    public async Task<ApplicationResult> HandleAsync(
        ApproveOpeningBalanceCommand command, CancellationToken cancellationToken = default)
    {
        if (!await OpeningBalanceAuthorization.CanWorkflowAsync(
                permissions, repository, command.DocumentId, "openingbalances.approve", cancellationToken))
            return ApplicationResult.PermissionDenied("غير مسموح بالاعتماد.");
        return await engine.ApproveAsync(command.DocumentId, command.Notes, cancellationToken);
    }
}

public sealed class RejectOpeningBalanceHandler(
    IOpeningBalanceEngine engine,
    IPermissionService permissions)
    : ICommandHandler<RejectOpeningBalanceCommand, ApplicationResult>
{
    public async Task<ApplicationResult> HandleAsync(
        RejectOpeningBalanceCommand command, CancellationToken cancellationToken = default)
    {
        if (!await permissions.CanAsync("openingbalances.approve", cancellationToken))
            return ApplicationResult.PermissionDenied("غير مسموح بالرفض.");
        return await engine.RejectAsync(command.DocumentId, command.Reason, cancellationToken);
    }
}

public sealed class PostOpeningBalanceHandler(
    IOpeningBalanceEngine engine,
    IPermissionService permissions,
    IOpeningBalanceRepository repository)
    : ICommandHandler<PostOpeningBalanceCommand, ApplicationResult<OpeningBalancePostResultDto>>
{
    public async Task<ApplicationResult<OpeningBalancePostResultDto>> HandleAsync(
        PostOpeningBalanceCommand command, CancellationToken cancellationToken = default)
    {
        if (!await OpeningBalanceAuthorization.CanWorkflowAsync(
                permissions, repository, command.DocumentId, "openingbalances.post", cancellationToken))
            return ApplicationResult<OpeningBalancePostResultDto>.PermissionDenied("غير مسموح بالترحيل.");
        return await engine.PostAsync(command.DocumentId, command.LockAfterPost, cancellationToken);
    }
}

public sealed class ArchiveOpeningBalanceHandler(
    IOpeningBalanceEngine engine,
    IPermissionService permissions)
    : ICommandHandler<ArchiveOpeningBalanceCommand, ApplicationResult>
{
    public async Task<ApplicationResult> HandleAsync(
        ArchiveOpeningBalanceCommand command, CancellationToken cancellationToken = default)
    {
        if (!await permissions.CanAsync("openingbalances.archive", cancellationToken))
            return ApplicationResult.PermissionDenied("غير مسموح بالأرشفة.");
        return await engine.ArchiveAsync(command.DocumentId, cancellationToken);
    }
}

public sealed class DuplicateOpeningBalanceHandler(
    IOpeningBalanceEngine engine,
    IPermissionService permissions)
    : ICommandHandler<DuplicateOpeningBalanceCommand, ApplicationResult<OpeningBalanceListDto>>
{
    public async Task<ApplicationResult<OpeningBalanceListDto>> HandleAsync(
        DuplicateOpeningBalanceCommand command, CancellationToken cancellationToken = default)
    {
        if (!await permissions.CanAsync("openingbalances.create", cancellationToken))
            return ApplicationResult<OpeningBalanceListDto>.PermissionDenied("غير مسموح.");
        return await engine.DuplicateAsync(command.DocumentId, cancellationToken);
    }
}

public sealed class ValidateOpeningBalanceHandler(IOpeningBalanceEngine engine)
    : ICommandHandler<ValidateOpeningBalanceCommand, ApplicationResult<OpeningBalanceValidationReportDto>>
{
    public async Task<ApplicationResult<OpeningBalanceValidationReportDto>> HandleAsync(
        ValidateOpeningBalanceCommand command, CancellationToken cancellationToken = default) =>
        ApplicationResult<OpeningBalanceValidationReportDto>.Success(await engine.ValidateAsync(command, cancellationToken));
}

public sealed class ImportOpeningBalanceExcelHandler(
    IOpeningBalanceEngine engine,
    IPermissionService permissions)
    : ICommandHandler<ImportOpeningBalanceExcelCommand, ApplicationResult<OpeningBalanceImportResultDto>>
{
    public async Task<ApplicationResult<OpeningBalanceImportResultDto>> HandleAsync(
        ImportOpeningBalanceExcelCommand command, CancellationToken cancellationToken = default)
    {
        if (!await permissions.CanAsync("openingbalances.import", cancellationToken))
            return ApplicationResult<OpeningBalanceImportResultDto>.PermissionDenied("غير مسموح بالاستيراد.");
        return await engine.ImportExcelAsync(command, cancellationToken);
    }
}

public sealed class GetOpeningBalanceListHandler(IOpeningBalanceRepository repository)
    : IQueryHandler<GetOpeningBalanceListQuery, ApplicationResult<PagedResult<OpeningBalanceListDto>>>
{
    public async Task<ApplicationResult<PagedResult<OpeningBalanceListDto>>> HandleAsync(
        GetOpeningBalanceListQuery query, CancellationToken cancellationToken = default)
    {
        var (items, total) = await repository.GetPagedAsync(
            query.CompanyId, query.Filter, query.Page, query.PageSize, cancellationToken);
        return ApplicationResult<PagedResult<OpeningBalanceListDto>>.Success(new PagedResult<OpeningBalanceListDto>
        {
            Items = items.Select(OpeningBalanceMappers.ToListDto).ToList(),
            TotalCount = total,
            Page = query.Page,
            PageSize = query.PageSize
        });
    }
}

public sealed class GetOpeningBalanceDetailsHandler(IOpeningBalanceRepository repository)
    : IQueryHandler<GetOpeningBalanceDetailsQuery, ApplicationResult<OpeningBalanceDetailsDto>>
{
    public async Task<ApplicationResult<OpeningBalanceDetailsDto>> HandleAsync(
        GetOpeningBalanceDetailsQuery query, CancellationToken cancellationToken = default)
    {
        var doc = await repository.GetAsync(query.DocumentId, cancellationToken);
        if (doc is null)
            return ApplicationResult<OpeningBalanceDetailsDto>.NotFound("المستند غير موجود.");

        var events = await repository.GetEventsAsync(query.DocumentId, cancellationToken);
        var journal = await repository.GetJournalLinesAsync(query.DocumentId, cancellationToken);

        return ApplicationResult<OpeningBalanceDetailsDto>.Success(new OpeningBalanceDetailsDto
        {
            Header = OpeningBalanceMappers.ToListDto(doc),
            Lines = doc.Lines.Select(OpeningBalanceMappers.ToLineDto).ToList(),
            Events = events.Select(OpeningBalanceMappers.ToEventDto).ToList(),
            ApprovalNotes = doc.ApprovalNotes,
            RejectionReason = doc.RejectionReason,
            ApprovedAt = doc.ApprovedAt,
            LockedAt = doc.LockedAt,
            ArchivedAt = doc.ArchivedAt,
            JournalLines = journal
        });
    }
}

public sealed class GetOpeningBalanceDashboardHandler(IOpeningBalanceRepository repository)
    : IQueryHandler<GetOpeningBalanceDashboardQuery, ApplicationResult<OpeningBalanceDashboardDto>>
{
    public async Task<ApplicationResult<OpeningBalanceDashboardDto>> HandleAsync(
        GetOpeningBalanceDashboardQuery query, CancellationToken cancellationToken = default)
    {
        var all = await repository.GetAllAsync(query.CompanyId, cancellationToken: cancellationToken);
        var byType = Enum.GetValues<OpeningBalanceType>()
            .Select(t => new OpeningBalanceTypeSummaryDto
            {
                Type = t,
                TypeDisplay = OpeningBalanceDisplay.TypeName(t),
                DocumentCount = all.Count(d => d.Type == t),
                PostedCount = all.Count(d => d.Type == t && d.Status is OpeningBalanceStatus.Posted or OpeningBalanceStatus.Locked),
                TotalBaseAmount = all.Where(d => d.Type == t).Sum(d => d.TotalBaseAmount)
            })
            .Where(x => x.DocumentCount > 0)
            .ToList();

        return ApplicationResult<OpeningBalanceDashboardDto>.Success(new OpeningBalanceDashboardDto
        {
            TotalDocuments = all.Count,
            DraftCount = all.Count(d => d.Status == OpeningBalanceStatus.Draft),
            PendingApprovalCount = all.Count(d => d.Status == OpeningBalanceStatus.PendingApproval),
            ApprovedCount = all.Count(d => d.Status == OpeningBalanceStatus.Approved),
            PostedCount = all.Count(d => d.Status == OpeningBalanceStatus.Posted),
            LockedCount = all.Count(d => d.Status == OpeningBalanceStatus.Locked),
            ArchivedCount = all.Count(d => d.Status == OpeningBalanceStatus.Archived),
            TotalPostedBaseAmount = all.Where(d => d.Status is OpeningBalanceStatus.Posted or OpeningBalanceStatus.Locked).Sum(d => d.TotalBaseAmount),
            TotalDraftBaseAmount = all.Where(d => d.Status == OpeningBalanceStatus.Draft).Sum(d => d.TotalBaseAmount),
            ByType = byType
        });
    }
}

public sealed class GetOpeningBalanceLookupsHandler(IOpeningBalanceLookupService lookups)
    : IQueryHandler<GetOpeningBalanceLookupsQuery, ApplicationResult<OpeningBalanceLookupsDto>>
{
    public async Task<ApplicationResult<OpeningBalanceLookupsDto>> HandleAsync(
        GetOpeningBalanceLookupsQuery query, CancellationToken cancellationToken = default) =>
        ApplicationResult<OpeningBalanceLookupsDto>.Success(
            await lookups.GetLookupsAsync(query.CompanyId, cancellationToken));
}

public sealed class GetCustomerOpeningBalanceSummaryHandler(IOpeningBalanceRepository repository)
    : IQueryHandler<GetCustomerOpeningBalanceSummaryQuery, ApplicationResult<CustomerOpeningBalanceSummaryDto>>
{
    public async Task<ApplicationResult<CustomerOpeningBalanceSummaryDto>> HandleAsync(
        GetCustomerOpeningBalanceSummaryQuery query,
        CancellationToken cancellationToken = default)
    {
        var filter = new OpeningBalanceListFilter
        {
            Type = OpeningBalanceType.CustomerReceivable,
            Status = query.Filter.Status,
            Search = query.Filter.Search,
            From = query.Filter.From,
            To = query.Filter.To,
            PartyId = query.Filter.PartyId,
            PartySearch = query.Filter.PartySearch,
            AmountFrom = query.Filter.AmountFrom,
            AmountTo = query.Filter.AmountTo,
            IncludeArchived = query.Filter.IncludeArchived
        };
        var summary = await repository.GetSummaryAsync(query.CompanyId, filter, cancellationToken);
        return ApplicationResult<CustomerOpeningBalanceSummaryDto>.Success(summary);
    }
}
