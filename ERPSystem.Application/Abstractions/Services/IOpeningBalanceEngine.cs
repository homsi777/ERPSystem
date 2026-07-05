using ERPSystem.Application.Commands.Finance;
using ERPSystem.Application.DTOs.Finance;
using ERPSystem.Application.Results;
using ERPSystem.Domain.Entities.Finance;

namespace ERPSystem.Application.Abstractions.Services;

/// <summary>
/// The single source of truth for every opening balance in the system.
/// Responsible for validation, posting, import, audit, timeline, approval,
/// currency conversion, journal creation and duplicate prevention.
/// No module may write opening balances through any other path.
/// </summary>
public interface IOpeningBalanceEngine
{
    /// <summary>Runs the full validation pipeline without persisting anything.</summary>
    Task<OpeningBalanceValidationReportDto> ValidateAsync(
        ValidateOpeningBalanceCommand command,
        CancellationToken cancellationToken = default);

    Task<ApplicationResult<OpeningBalanceListDto>> CreateAsync(
        CreateOpeningBalanceCommand command,
        CancellationToken cancellationToken = default);

    Task<ApplicationResult<OpeningBalanceListDto>> UpdateAsync(
        UpdateOpeningBalanceCommand command,
        CancellationToken cancellationToken = default);

    Task<ApplicationResult> SubmitAsync(Guid documentId, CancellationToken cancellationToken = default);
    Task<ApplicationResult> ApproveAsync(Guid documentId, string? notes, CancellationToken cancellationToken = default);
    Task<ApplicationResult> RejectAsync(Guid documentId, string reason, CancellationToken cancellationToken = default);

    /// <summary>
    /// Posts an approved document: builds the balanced journal entry from the
    /// per-type accounting rules, converts to base currency, marks parties,
    /// records audit + timeline and (optionally) locks the document.
    /// </summary>
    Task<ApplicationResult<OpeningBalancePostResultDto>> PostAsync(
        Guid documentId,
        bool lockAfterPost,
        CancellationToken cancellationToken = default);

    Task<ApplicationResult> ArchiveAsync(Guid documentId, CancellationToken cancellationToken = default);

    Task<ApplicationResult<OpeningBalanceListDto>> DuplicateAsync(
        Guid documentId,
        CancellationToken cancellationToken = default);

    /// <summary>Excel import pipeline: template + business validation → preview/import summary.</summary>
    Task<ApplicationResult<OpeningBalanceImportResultDto>> ImportExcelAsync(
        ImportOpeningBalanceExcelCommand command,
        CancellationToken cancellationToken = default);

    /// <summary>Builds the downloadable Excel template for a balance type.</summary>
    byte[] BuildImportTemplate(OpeningBalanceType type);

    /// <summary>
    /// Creates, approves and posts a single-party opening balance (customer AR / supplier AP).
    /// Used by legacy party-module screens — all logic stays in this engine.
    /// </summary>
    Task<ApplicationResult<OpeningBalancePostResultDto>> PostPartyOpeningBalanceAsync(
        PostPartyOpeningBalanceCommand command,
        CancellationToken cancellationToken = default);
}
