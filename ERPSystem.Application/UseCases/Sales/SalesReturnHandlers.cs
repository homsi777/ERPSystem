using ERPSystem.Application.Abstractions;
using ERPSystem.Application.Abstractions.Repositories;
using ERPSystem.Application.Abstractions.Services;
using ERPSystem.Application.Commands.Sales;
using ERPSystem.Application.Common;
using ERPSystem.Application.Results;
using ERPSystem.Application.Services;
using ERPSystem.Domain.Aggregates;
using ERPSystem.Domain.Enums;
using ERPSystem.Domain.Exceptions;
using ERPSystem.Domain.ValueObjects;

namespace ERPSystem.Application.UseCases.Sales;

public sealed class CreateSalesReturnHandler(
    ISalesReturnRepository returnRepository,
    ISalesInvoiceRepository invoiceRepository,
    INumberingService numberingService,
    IUnitOfWork unitOfWork,
    IPermissionService permissionService,
    ICurrentUserService currentUserService)
    : ICommandHandler<CreateSalesReturnCommand, ApplicationResult<Guid>>
{
    public async Task<ApplicationResult<Guid>> HandleAsync(
        CreateSalesReturnCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command.OriginalInvoiceId == Guid.Empty)
            return ApplicationResult<Guid>.ValidationFailed(nameof(command.OriginalInvoiceId), "Original invoice is required.");
        if (command.Lines is null || command.Lines.Count == 0)
            return ApplicationResult<Guid>.ValidationFailed(nameof(command.Lines), "At least one line is required.");

        if (!await permissionService.CanAsync("sales.return", cancellationToken))
            return ApplicationResult<Guid>.PermissionDenied("Not allowed to create sales returns.");

        var invoice = await invoiceRepository.GetByIdAsync(command.OriginalInvoiceId, cancellationToken);
        if (invoice is null)
            return ApplicationResult<Guid>.NotFound("Original invoice not found.");

        if (invoice.Status is not (SalesInvoiceStatus.Approved or SalesInvoiceStatus.Printed or SalesInvoiceStatus.Delivered
            or SalesInvoiceStatus.PartiallyReturned))
            return ApplicationResult<Guid>.Conflict("Only approved, delivered, or partially returned invoices can be returned.");

        try
        {
            var lines = BuildReturnLines(invoice, command.Lines);

            var returnNumber = await numberingService.NextSalesReturnNumberAsync(invoice.BranchId, cancellationToken);
            var userId = currentUserService.UserId ?? Guid.Empty;

            var aggregate = SalesReturnAggregate.CreateDraft(
                returnNumber,
                invoice.CompanyId,
                invoice.BranchId,
                invoice.Id,
                invoice.InvoiceNumber.Value,
                invoice.CustomerId,
                invoice.WarehouseId,
                command.ReturnDate == default ? DateTime.UtcNow : command.ReturnDate,
                command.Reason,
                command.ReasonNotes,
                command.Notes,
                userId,
                lines);

            await returnRepository.AddAsync(aggregate, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return ApplicationResult<Guid>.Success(aggregate.Id);
        }
        catch (Exception ex)
        {
            return ex.ToFailureResult<Guid>();
        }
    }

    internal static IReadOnlyList<Domain.Aggregates.SalesReturnLine> BuildReturnLines(
        SalesInvoiceAggregate invoice,
        IReadOnlyList<SalesReturnLineCommand> requested)
    {
        var lines = new List<Domain.Aggregates.SalesReturnLine>();
        foreach (var line in requested.Where(l => l.ReturnMeters > 0))
        {
            var item = invoice.Items.FirstOrDefault(i => i.Id == line.OriginalInvoiceItemId)
                ?? throw new ValidationException("Original invoice item not found.");

            var originalMeters = invoice.RollDetails
                .Where(r => r.SalesInvoiceItemId == item.Id)
                .Sum(r => r.LengthMeters.Value);

            if (line.ReturnMeters > originalMeters + 0.001m)
                throw new ValidationException($"Return quantity ({line.ReturnMeters}) exceeds original ({originalMeters}).");

            lines.Add(Domain.Aggregates.SalesReturnLine.Create(
                line.LineNumber,
                item.Id,
                item.FabricItemId,
                item.FabricColorId,
                originalMeters,
                line.ReturnMeters,
                item.UnitPrice));
        }
        return lines;
    }
}

public sealed class UpdateSalesReturnHandler(
    ISalesReturnRepository returnRepository,
    ISalesInvoiceRepository invoiceRepository,
    IUnitOfWork unitOfWork,
    IPermissionService permissionService)
    : ICommandHandler<UpdateSalesReturnCommand, ApplicationResult>
{
    public async Task<ApplicationResult> HandleAsync(UpdateSalesReturnCommand command, CancellationToken cancellationToken = default)
    {
        if (!await permissionService.CanAsync("sales.return", cancellationToken))
            return ApplicationResult.PermissionDenied("Not allowed.");

        var aggregate = await returnRepository.GetByIdAsync(command.ReturnId, cancellationToken);
        if (aggregate is null)
            return ApplicationResult.NotFound("Sales return not found.");

        var invoice = await invoiceRepository.GetByIdAsync(aggregate.OriginalInvoiceId, cancellationToken);
        if (invoice is null)
            return ApplicationResult.NotFound("Original invoice not found.");

        try
        {
            var lines = CreateSalesReturnHandler.BuildReturnLines(invoice, command.Lines);
            aggregate.UpdateHeader(command.Reason, command.ReasonNotes, command.Notes);
            aggregate.ReplaceLines(lines);
            await returnRepository.UpdateAsync(aggregate, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return ApplicationResult.Success();
        }
        catch (Exception ex)
        {
            return ex.ToFailureResult();
        }
    }
}

public sealed class PostSalesReturnHandler(
    ISalesReturnRepository returnRepository,
    ISalesInvoiceRepository invoiceRepository,
    ICustomerRepository customerRepository,
    IIntegratedAccountingService accountingService,
    IInventoryOperationsService inventoryOperations,
    IUnitOfWork unitOfWork,
    IPermissionService permissionService,
    ICurrentUserService currentUserService)
    : ICommandHandler<PostSalesReturnCommand, ApplicationResult>
{
    public async Task<ApplicationResult> HandleAsync(PostSalesReturnCommand command, CancellationToken cancellationToken = default)
    {
        if (!await permissionService.CanAsync("sales.return", cancellationToken))
            return ApplicationResult.PermissionDenied("Not allowed.");

        var aggregate = await returnRepository.GetByIdAsync(command.ReturnId, cancellationToken);
        if (aggregate is null)
            return ApplicationResult.NotFound("Sales return not found.");
        if (aggregate.Status != VoucherStatus.Draft)
            return ApplicationResult.Conflict("Only draft returns can be posted.");

        var invoice = await invoiceRepository.GetByIdAsync(aggregate.OriginalInvoiceId, cancellationToken);
        if (invoice is null)
            return ApplicationResult.NotFound("Original invoice not found.");

        try
        {
            await unitOfWork.BeginTransactionAsync(cancellationToken);

            var cogsReversal = await inventoryOperations.ReceiveSalesReturnAsync(aggregate, invoice, cancellationToken);
            var taxResult = SalesReturnTaxCalculator.Compute(aggregate, invoice);
            aggregate.ApplyReturnTax(
                taxResult.TaxTotal,
                taxResult.CustomerCreditTotal,
                taxResult.IsLegacyUntaxedReturn,
                taxResult.TaxIncludedInLineTotals);

            var journalNumber = await accountingService.PostSalesReturnAsync(
                aggregate,
                cogsReversal,
                taxResult.TaxTotal,
                taxResult.TaxByAccount,
                cancellationToken);

            var userId = currentUserService.UserId ?? Guid.Empty;
            aggregate.Post(userId, journalNumber);

            // Reduce customer receivable balance
            var customer = await customerRepository.GetByIdAsync(invoice.CustomerId, cancellationToken);
            if (customer is not null)
            {
                customer.RecordSalesReturn(taxResult.CustomerCreditTotal);
                await customerRepository.UpdateAsync(customer, cancellationToken);
            }

            // Update invoice status (fully or partially returned)
            var totalReturned = await ReturnedMetersAsync(returnRepository, invoice.Id, cancellationToken);
            var totalOriginal = invoice.RollDetails.Sum(r => r.LengthMeters.Value);
            var isFullyReturned = totalReturned + aggregate.Lines.Sum(l => l.ReturnMeters) >= totalOriginal - 0.001m;
            invoice.ApplyReturn(isFullyReturned);
            await invoiceRepository.UpdateAsync(invoice, cancellationToken);

            await returnRepository.UpdateAsync(aggregate, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            await unitOfWork.CommitTransactionAsync(cancellationToken);

            return ApplicationResult.Success();
        }
        catch (Exception ex)
        {
            await unitOfWork.RollbackTransactionAsync(cancellationToken);
            return ex.ToFailureResult();
        }
    }

    private static async Task<decimal> ReturnedMetersAsync(
        ISalesReturnRepository repo,
        Guid originalInvoiceId,
        CancellationToken cancellationToken)
    {
        var posted = await repo.GetListAsync(
            companyId: Guid.Empty, // company filter not strictly required for count
            status: VoucherStatus.Posted,
            originalInvoiceId: originalInvoiceId,
            cancellationToken: cancellationToken);
        return posted.SelectMany(r => r.Lines).Sum(l => l.ReturnMeters);
    }
}

public sealed class CancelSalesReturnHandler(
    ISalesReturnRepository returnRepository,
    IUnitOfWork unitOfWork,
    IPermissionService permissionService)
    : ICommandHandler<CancelSalesReturnCommand, ApplicationResult>
{
    public async Task<ApplicationResult> HandleAsync(CancelSalesReturnCommand command, CancellationToken cancellationToken = default)
    {
        if (!await permissionService.CanAsync("sales.return", cancellationToken))
            return ApplicationResult.PermissionDenied("Not allowed.");

        var aggregate = await returnRepository.GetByIdAsync(command.ReturnId, cancellationToken);
        if (aggregate is null)
            return ApplicationResult.NotFound("Sales return not found.");

        try
        {
            aggregate.Cancel();
            await returnRepository.UpdateAsync(aggregate, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return ApplicationResult.Success();
        }
        catch (Exception ex)
        {
            return ex.ToFailureResult();
        }
    }
}
