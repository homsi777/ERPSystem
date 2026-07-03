using ERPSystem.Application.Abstractions;
using ERPSystem.Application.Abstractions.Repositories;
using ERPSystem.Application.DTOs.Sales;
using ERPSystem.Application.Mapping;
using ERPSystem.Application.Queries.Sales;
using ERPSystem.Application.Results;
using ERPSystem.Domain.Enums;

namespace ERPSystem.Application.UseCases.Sales;

public sealed class GetSalesReturnListHandler(
    ISalesReturnRepository returnRepository,
    ICustomerRepository customerRepository,
    IFabricCatalogRepository fabricCatalogRepository)
    : IQueryHandler<GetSalesReturnListQuery, ApplicationResult<IReadOnlyList<SalesReturnDto>>>
{
    public async Task<ApplicationResult<IReadOnlyList<SalesReturnDto>>> HandleAsync(
        GetSalesReturnListQuery query,
        CancellationToken cancellationToken = default)
    {
        var list = await returnRepository.GetListAsync(
            query.CompanyId, query.BranchId, query.Status, query.CustomerId, query.OriginalInvoiceId, cancellationToken);

        var customers = await customerRepository.GetListAsync(query.CompanyId, cancellationToken: cancellationToken);
        var customerNames = customers.ToDictionary(c => c.Customer.Id, c => c.Customer.NameAr);

        var dtos = new List<SalesReturnDto>();
        foreach (var r in list)
        {
            var lineDtos = new List<SalesReturnLineDto>();
            foreach (var l in r.Lines)
            {
                var fabric = await fabricCatalogRepository.GetItemByIdAsync(l.FabricItemId, cancellationToken);
                var color = await fabricCatalogRepository.GetColorByIdAsync(l.FabricColorId, cancellationToken);
                lineDtos.Add(new SalesReturnLineDto
                {
                    Id = l.Id,
                    LineNumber = l.LineNumber,
                    OriginalInvoiceItemId = l.OriginalInvoiceItemId,
                    FabricItemId = l.FabricItemId,
                    FabricColorId = l.FabricColorId,
                    FabricDisplayName = fabric?.NameAr ?? "",
                    ColorDisplayName = color?.NameAr ?? "",
                    OriginalMeters = l.OriginalMeters,
                    ReturnMeters = l.ReturnMeters,
                    UnitPrice = l.UnitPrice.Amount,
                    LineTotal = l.LineTotal.Amount
                });
            }

            dtos.Add(new SalesReturnDto
            {
                Id = r.Id,
                ReturnNumber = r.ReturnNumber,
                OriginalInvoiceId = r.OriginalInvoiceId,
                OriginalInvoiceNumber = r.OriginalInvoiceNumber,
                CustomerId = r.CustomerId,
                CustomerName = customerNames.GetValueOrDefault(r.CustomerId, ""),
                WarehouseId = r.WarehouseId,
                ReturnDate = r.ReturnDate,
                Reason = r.Reason,
                ReasonNotes = r.ReasonNotes,
                Notes = r.Notes,
                Status = r.Status,
                TotalAmount = r.TotalAmount.Amount,
                JournalEntryNumber = r.JournalEntryNumber,
                PostedAt = r.PostedAt,
                Lines = lineDtos
            });
        }

        return ApplicationResult<IReadOnlyList<SalesReturnDto>>.Success(dtos);
    }
}

public sealed class GetSalesReturnDetailsHandler(
    ISalesReturnRepository returnRepository,
    ICustomerRepository customerRepository,
    IFabricCatalogRepository fabricCatalogRepository)
    : IQueryHandler<GetSalesReturnDetailsQuery, ApplicationResult<SalesReturnDto>>
{
    public async Task<ApplicationResult<SalesReturnDto>> HandleAsync(GetSalesReturnDetailsQuery query, CancellationToken cancellationToken = default)
    {
        var aggregate = await returnRepository.GetByIdAsync(query.ReturnId, cancellationToken);
        if (aggregate is null)
            return ApplicationResult<SalesReturnDto>.NotFound("Sales return not found.");

        var customer = await customerRepository.GetByIdAsync(aggregate.CustomerId, cancellationToken);
        var customerName = customer?.Customer.NameAr ?? "";

        var lineDtos = new List<SalesReturnLineDto>();
        foreach (var l in aggregate.Lines)
        {
            var fabric = await fabricCatalogRepository.GetItemByIdAsync(l.FabricItemId, cancellationToken);
            var color = await fabricCatalogRepository.GetColorByIdAsync(l.FabricColorId, cancellationToken);
            lineDtos.Add(new SalesReturnLineDto
            {
                Id = l.Id,
                LineNumber = l.LineNumber,
                OriginalInvoiceItemId = l.OriginalInvoiceItemId,
                FabricItemId = l.FabricItemId,
                FabricColorId = l.FabricColorId,
                FabricDisplayName = fabric?.NameAr ?? "",
                ColorDisplayName = color?.NameAr ?? "",
                OriginalMeters = l.OriginalMeters,
                ReturnMeters = l.ReturnMeters,
                UnitPrice = l.UnitPrice.Amount,
                LineTotal = l.LineTotal.Amount
            });
        }

        return ApplicationResult<SalesReturnDto>.Success(new SalesReturnDto
        {
            Id = aggregate.Id,
            ReturnNumber = aggregate.ReturnNumber,
            OriginalInvoiceId = aggregate.OriginalInvoiceId,
            OriginalInvoiceNumber = aggregate.OriginalInvoiceNumber,
            CustomerId = aggregate.CustomerId,
            CustomerName = customerName,
            WarehouseId = aggregate.WarehouseId,
            ReturnDate = aggregate.ReturnDate,
            Reason = aggregate.Reason,
            ReasonNotes = aggregate.ReasonNotes,
            Notes = aggregate.Notes,
            Status = aggregate.Status,
            TotalAmount = aggregate.TotalAmount.Amount,
            JournalEntryNumber = aggregate.JournalEntryNumber,
            PostedAt = aggregate.PostedAt,
            Lines = lineDtos
        });
    }
}

public sealed class GetDeliveryQueueHandler(
    ISalesInvoiceRepository invoiceRepository,
    ICustomerRepository customerRepository)
    : IQueryHandler<GetDeliveryQueueQuery, ApplicationResult<IReadOnlyList<SalesInvoiceDto>>>
{
    public async Task<ApplicationResult<IReadOnlyList<SalesInvoiceDto>>> HandleAsync(GetDeliveryQueueQuery query, CancellationToken cancellationToken = default)
    {
        var approved = await invoiceRepository.GetListAsync(query.CompanyId, query.BranchId, SalesInvoiceStatus.Approved, cancellationToken: cancellationToken);
        var printed = await invoiceRepository.GetListAsync(query.CompanyId, query.BranchId, SalesInvoiceStatus.Printed, cancellationToken: cancellationToken);
        var delivered = query.IncludeDelivered
            ? await invoiceRepository.GetListAsync(query.CompanyId, query.BranchId, SalesInvoiceStatus.Delivered, cancellationToken: cancellationToken)
            : (IReadOnlyList<Domain.Aggregates.SalesInvoiceAggregate>)Array.Empty<Domain.Aggregates.SalesInvoiceAggregate>();

        var customers = await customerRepository.GetListAsync(query.CompanyId, cancellationToken: cancellationToken);
        var customerNames = customers.ToDictionary(c => c.Customer.Id, c => c.Customer.NameAr);

        var all = approved.Concat(printed).Concat(delivered)
            .OrderByDescending(i => i.ApprovedAt ?? i.InvoiceDate)
            .Select(i => SalesInvoiceMapper.ToDto(i, customerNames.GetValueOrDefault(i.CustomerId, "")))
            .ToList();

        return ApplicationResult<IReadOnlyList<SalesInvoiceDto>>.Success(all);
    }
}

public sealed class GetInvoicePaymentHistoryHandler(IReceiptInvoicePaymentRepository repository)
    : IQueryHandler<GetInvoicePaymentHistoryQuery, ApplicationResult<IReadOnlyList<ReceiptInvoicePaymentDto>>>
{
    public async Task<ApplicationResult<IReadOnlyList<ReceiptInvoicePaymentDto>>> HandleAsync(GetInvoicePaymentHistoryQuery query, CancellationToken cancellationToken = default)
    {
        var pairs = await repository.GetByInvoiceWithVoucherAsync(query.InvoiceId, cancellationToken);
        var dtos = pairs.Select(x => new ReceiptInvoicePaymentDto
        {
            SalesInvoiceId = x.Payment.SalesInvoiceId,
            ReceiptVoucherId = x.Payment.ReceiptVoucherId,
            ReceiptNumber = x.VoucherNumber,
            Amount = x.Payment.Amount.Amount,
            AppliedAt = x.Payment.AppliedAt
        }).ToList();
        return ApplicationResult<IReadOnlyList<ReceiptInvoicePaymentDto>>.Success(dtos);
    }
}
