using ERPSystem.Application.Abstractions;
using ERPSystem.Application.Abstractions.Services;
using ERPSystem.Application.Commands.Sales;
using ERPSystem.Application.DTOs.Sales;
using ERPSystem.Application.Queries.Sales;
using ERPSystem.Application.Results;
using ERPSystem.Application.UseCases.Sales;
using ERPSystem.Domain.Enums;
using Microsoft.Extensions.DependencyInjection;

namespace ERPSystem.Services.Sales;

public static class SalesReturnListRefreshHub
{
    public static event EventHandler? RefreshRequested;
    public static void RequestRefresh() => RefreshRequested?.Invoke(null, EventArgs.Empty);
}

public sealed class SalesReturnUiService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ICurrentBranchService _branch;

    public SalesReturnUiService(IServiceScopeFactory scopeFactory, ICurrentBranchService branch)
    {
        _scopeFactory = scopeFactory;
        _branch = branch;
    }

    public static SalesReturnUiService Instance =>
        AppServices.GetRequiredService<SalesReturnUiService>();

    private Guid CompanyId =>
        _branch.CompanyId ?? throw new InvalidOperationException("Company context is not set.");

    private Guid BranchId =>
        _branch.BranchId ?? throw new InvalidOperationException("Branch context is not set.");

    public async Task<ApplicationResult<IReadOnlyList<SalesReturnDto>>> GetListAsync(
        VoucherStatus? status = null,
        Guid? customerId = null,
        Guid? originalInvoiceId = null,
        CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetSalesReturnListHandler>();
        return await handler.HandleAsync(new GetSalesReturnListQuery
        {
            CompanyId = CompanyId,
            BranchId = BranchId,
            Status = status,
            CustomerId = customerId,
            OriginalInvoiceId = originalInvoiceId
        }, cancellationToken);
    }

    public async Task<ApplicationResult<SalesReturnDto>> GetDetailsAsync(
        Guid returnId,
        CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetSalesReturnDetailsHandler>();
        return await handler.HandleAsync(new GetSalesReturnDetailsQuery { ReturnId = returnId }, cancellationToken);
    }

    public async Task<ApplicationResult<Guid>> CreateDraftAsync(
        Guid originalInvoiceId,
        DateTime returnDate,
        SalesReturnReason reason,
        string? reasonNotes,
        string? notes,
        IReadOnlyList<SalesReturnLineCommand> lines,
        CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<ICommandHandler<CreateSalesReturnCommand, ApplicationResult<Guid>>>();
        return await handler.HandleAsync(new CreateSalesReturnCommand
        {
            OriginalInvoiceId = originalInvoiceId,
            ReturnDate = returnDate,
            Reason = reason,
            ReasonNotes = reasonNotes,
            Notes = notes,
            Lines = lines
        }, cancellationToken);
    }

    public async Task<ApplicationResult> UpdateAsync(
        Guid returnId,
        SalesReturnReason reason,
        string? reasonNotes,
        string? notes,
        IReadOnlyList<SalesReturnLineCommand> lines,
        CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<ICommandHandler<UpdateSalesReturnCommand, ApplicationResult>>();
        return await handler.HandleAsync(new UpdateSalesReturnCommand
        {
            ReturnId = returnId,
            Reason = reason,
            ReasonNotes = reasonNotes,
            Notes = notes,
            Lines = lines
        }, cancellationToken);
    }

    public async Task<ApplicationResult> PostAsync(Guid returnId, CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<ICommandHandler<PostSalesReturnCommand, ApplicationResult>>();
        return await handler.HandleAsync(new PostSalesReturnCommand { ReturnId = returnId }, cancellationToken);
    }

    public async Task<ApplicationResult> CancelAsync(Guid returnId, CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<ICommandHandler<CancelSalesReturnCommand, ApplicationResult>>();
        return await handler.HandleAsync(new CancelSalesReturnCommand { ReturnId = returnId }, cancellationToken);
    }

    public async Task<bool> CanCreateAsync(CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var permissions = scope.ServiceProvider.GetRequiredService<IPermissionService>();
        return await permissions.CanAsync("sales.return", cancellationToken);
    }
}
