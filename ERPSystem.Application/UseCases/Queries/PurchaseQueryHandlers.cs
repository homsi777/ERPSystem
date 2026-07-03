using ERPSystem.Application.Abstractions;
using ERPSystem.Application.Abstractions.Repositories;
using ERPSystem.Application.Common;
using ERPSystem.Application.DTOs.Purchases;
using ERPSystem.Application.Queries.Purchases;
using ERPSystem.Application.Results;
using ERPSystem.Domain.Enums;

namespace ERPSystem.Application.UseCases.Queries;

public sealed class GetPurchaseInvoiceListHandler(
    IPurchaseInvoiceRepository invoiceRepository,
    ISupplierRepository supplierRepository)
    : IQueryHandler<GetPurchaseInvoiceListQuery, ApplicationResult<IReadOnlyList<PurchaseInvoiceListDto>>>
{
    public async Task<ApplicationResult<IReadOnlyList<PurchaseInvoiceListDto>>> HandleAsync(
        GetPurchaseInvoiceListQuery query,
        CancellationToken cancellationToken = default)
    {
        var (invoices, _) = await invoiceRepository.GetPagedAsync(
            query.CompanyId, query.Search, query.Status, cancellationToken: cancellationToken);

        var supplierIds = invoices.Select(i => i.SupplierId).Distinct().ToList();
        var suppliers = new Dictionary<Guid, string>();
        foreach (var id in supplierIds)
        {
            var s = await supplierRepository.GetByIdAsync(id, cancellationToken);
            if (s is not null)
                suppliers[id] = s.Supplier.NameAr;
        }

        var rows = invoices.Select(i => new PurchaseInvoiceListDto
        {
            Id = i.Id,
            InvoiceNumber = i.InvoiceNumber,
            InvoiceDate = i.InvoiceDate,
            DueDate = i.DueDate,
            SupplierId = i.SupplierId,
            SupplierName = suppliers.GetValueOrDefault(i.SupplierId, "—"),
            TotalAmount = i.TotalAmount.Amount,
            PaidAmount = i.PaidAmount.Amount,
            RemainingAmount = i.Remaining.Amount,
            Status = i.Status,
            StatusDisplay = i.Status.ToStatusDisplay(),
            IsOverdue = i.DueDate.Date < DateTime.Today &&
                        i.Status is not (PurchaseInvoiceStatus.Paid or PurchaseInvoiceStatus.Cancelled or PurchaseInvoiceStatus.Draft)
        }).ToList();

        return ApplicationResult<IReadOnlyList<PurchaseInvoiceListDto>>.Success(rows);
    }
}

public sealed class GetPurchaseInvoiceDetailsHandler(
    IPurchaseInvoiceRepository invoiceRepository,
    ISupplierRepository supplierRepository,
    IWarehouseRepository warehouseRepository,
    IFabricCatalogRepository fabricCatalog)
    : IQueryHandler<GetPurchaseInvoiceDetailsQuery, ApplicationResult<PurchaseInvoiceDetailsDto>>
{
    public async Task<ApplicationResult<PurchaseInvoiceDetailsDto>> HandleAsync(
        GetPurchaseInvoiceDetailsQuery query,
        CancellationToken cancellationToken = default)
    {
        var invoice = await invoiceRepository.GetByIdAsync(query.InvoiceId, cancellationToken);
        if (invoice is null)
            return ApplicationResult<PurchaseInvoiceDetailsDto>.NotFound("Invoice not found.");

        var supplier = await supplierRepository.GetByIdAsync(invoice.SupplierId, cancellationToken);
        string? warehouseName = null;
        if (invoice.WarehouseId is Guid whId)
        {
            var wh = await warehouseRepository.GetByIdAsync(whId, cancellationToken);
            warehouseName = wh?.Warehouse.NameAr;
        }

        var lineDtos = new List<PurchaseInvoiceLineDto>();
        foreach (var line in invoice.Items)
        {
            string? fabricName = null;
            if (line.FabricItemId is Guid fid)
            {
                var item = await fabricCatalog.GetItemByIdAsync(fid, cancellationToken);
                fabricName = item?.NameAr;
            }
            lineDtos.Add(new PurchaseInvoiceLineDto
            {
                Id = line.Id,
                LineType = line.LineType,
                FabricItemId = line.FabricItemId,
                FabricItemName = fabricName,
                FabricColorId = line.FabricColorId,
                ExpenseAccountId = line.ExpenseAccountId,
                Description = line.Description,
                QuantityMeters = line.Quantity.Value,
                RollCount = line.RollCount,
                UnitPrice = line.UnitPrice.Amount,
                LineTotal = line.LineTotal.Amount
            });
        }

        return ApplicationResult<PurchaseInvoiceDetailsDto>.Success(new PurchaseInvoiceDetailsDto
        {
            Id = invoice.Id,
            InvoiceNumber = invoice.InvoiceNumber,
            SupplierId = invoice.SupplierId,
            SupplierName = supplier?.Supplier.NameAr ?? "—",
            SupplierPaymentTermsDays = supplier?.Supplier.PaymentTermsDays ?? 30,
            SupplierReference = invoice.SupplierReference,
            InvoiceDate = invoice.InvoiceDate,
            DueDate = invoice.DueDate,
            WarehouseId = invoice.WarehouseId,
            WarehouseName = warehouseName,
            CurrencyCode = invoice.CurrencyCode,
            SubTotal = invoice.SubTotal.Amount,
            DiscountAmount = invoice.DiscountAmount.Amount,
            TaxAmount = invoice.TaxAmount.Amount,
            TotalAmount = invoice.TotalAmount.Amount,
            PaidAmount = invoice.PaidAmount.Amount,
            RemainingAmount = invoice.Remaining.Amount,
            Status = invoice.Status,
            StatusDisplay = invoice.Status.ToStatusDisplay(),
            PurchaseOrderId = invoice.PurchaseOrderId,
            Notes = invoice.Notes,
            PostedAt = invoice.PostedAt,
            IsReadOnly = invoice.Status != PurchaseInvoiceStatus.Draft,
            Lines = lineDtos
        });
    }
}

public sealed class GetPurchaseInvoiceOperationsCenterHandler(
    GetPurchaseInvoiceDetailsHandler detailsHandler,
    IPurchaseInvoicePaymentRepository paymentRepository,
    IPaymentVoucherRepository voucherRepository,
    IJournalEntryRepository journalRepository)
    : IQueryHandler<GetPurchaseInvoiceOperationsCenterQuery, ApplicationResult<PurchaseOperationsCenterDto>>
{
    public async Task<ApplicationResult<PurchaseOperationsCenterDto>> HandleAsync(
        GetPurchaseInvoiceOperationsCenterQuery query,
        CancellationToken cancellationToken = default)
    {
        var detailsResult = await detailsHandler.HandleAsync(
            new GetPurchaseInvoiceDetailsQuery { InvoiceId = query.InvoiceId }, cancellationToken);
        if (!detailsResult.IsSuccess || detailsResult.Value is null)
            return ApplicationResult<PurchaseOperationsCenterDto>.NotFound("Invoice not found.");

        var invoice = detailsResult.Value;
        var daysUntilDue = (invoice.DueDate.Date - DateTime.Today).Days;
        var isOverdue = daysUntilDue < 0 &&
                        invoice.Status is not (PurchaseInvoiceStatus.Paid or PurchaseInvoiceStatus.Cancelled or PurchaseInvoiceStatus.Draft);

        var payments = await paymentRepository.GetByInvoiceIdAsync(query.InvoiceId, cancellationToken);
        var paymentDtos = new List<PurchasePaymentDto>();
        foreach (var p in payments)
        {
            var voucher = await voucherRepository.GetByIdAsync(p.PaymentVoucherId, cancellationToken);
            if (voucher is null) continue;
            paymentDtos.Add(new PurchasePaymentDto
            {
                VoucherId = voucher.Id,
                VoucherNumber = voucher.VoucherNumber,
                VoucherDate = voucher.VoucherDate,
                Amount = p.Amount.Amount,
                StatusDisplay = voucher.Status.ToString()
            });
        }

        var journalRows = await journalRepository.GetBySourceIdAsync(query.InvoiceId, cancellationToken);
        var journalDtos = journalRows.Select(j => new PurchaseJournalEntryDto
        {
            EntryNumber = j.EntryNumber,
            EntryDate = j.EntryDate,
            Description = j.Description,
            Debit = j.DebitTotal,
            Credit = j.CreditTotal
        }).ToList();

        return ApplicationResult<PurchaseOperationsCenterDto>.Success(new PurchaseOperationsCenterDto
        {
            Invoice = invoice,
            DaysUntilDue = daysUntilDue,
            IsOverdue = isOverdue,
            JournalEntries = journalDtos,
            Payments = paymentDtos
        });
    }
}

public sealed class GetPurchaseOrderListHandler(
    IPurchaseOrderRepository orderRepository,
    ISupplierRepository supplierRepository)
    : IQueryHandler<GetPurchaseOrderListQuery, ApplicationResult<IReadOnlyList<PurchaseOrderListDto>>>
{
    public async Task<ApplicationResult<IReadOnlyList<PurchaseOrderListDto>>> HandleAsync(
        GetPurchaseOrderListQuery query,
        CancellationToken cancellationToken = default)
    {
        var orders = await orderRepository.GetListAsync(query.CompanyId, query.Status, cancellationToken);
        var rows = new List<PurchaseOrderListDto>();
        foreach (var o in orders)
        {
            var s = await supplierRepository.GetByIdAsync(o.SupplierId, cancellationToken);
            rows.Add(new PurchaseOrderListDto
            {
                Id = o.Id,
                OrderNumber = o.OrderNumber,
                SupplierName = s?.Supplier.NameAr ?? "—",
                OrderDate = o.OrderDate,
                ExpectedDeliveryDate = o.ExpectedDeliveryDate,
                TotalAmount = o.TotalAmount.Amount,
                Status = o.Status,
                StatusDisplay = o.Status.ToStatusDisplay()
            });
        }
        return ApplicationResult<IReadOnlyList<PurchaseOrderListDto>>.Success(rows);
    }
}

public sealed class GetPurchaseReturnListHandler(
    IPurchaseReturnRepository returnRepository,
    IPurchaseInvoiceRepository invoiceRepository)
    : IQueryHandler<GetPurchaseReturnListQuery, ApplicationResult<IReadOnlyList<PurchaseReturnListDto>>>
{
    public async Task<ApplicationResult<IReadOnlyList<PurchaseReturnListDto>>> HandleAsync(
        GetPurchaseReturnListQuery query,
        CancellationToken cancellationToken = default)
    {
        var returns = await returnRepository.GetListAsync(query.CompanyId, cancellationToken);
        var rows = new List<PurchaseReturnListDto>();
        foreach (var r in returns)
        {
            var inv = await invoiceRepository.GetByIdAsync(r.OriginalInvoiceId, cancellationToken);
            rows.Add(new PurchaseReturnListDto
            {
                Id = r.Id,
                ReturnNumber = r.ReturnNumber,
                OriginalInvoiceNumber = inv?.InvoiceNumber ?? "—",
                ReturnDate = r.ReturnDate,
                TotalAmount = r.TotalAmount.Amount,
                Status = r.Status,
                StatusDisplay = r.Status.ToStatusDisplay()
            });
        }
        return ApplicationResult<IReadOnlyList<PurchaseReturnListDto>>.Success(rows);
    }
}

public sealed class GetPurchaseOrderDetailsHandler(
    IPurchaseOrderRepository orderRepository,
    ISupplierRepository supplierRepository,
    IFabricCatalogRepository fabricCatalog)
    : IQueryHandler<GetPurchaseOrderDetailsQuery, ApplicationResult<PurchaseOrderDetailsDto>>
{
    public async Task<ApplicationResult<PurchaseOrderDetailsDto>> HandleAsync(
        GetPurchaseOrderDetailsQuery query,
        CancellationToken cancellationToken = default)
    {
        var order = await orderRepository.GetByIdAsync(query.OrderId, cancellationToken);
        if (order is null)
            return ApplicationResult<PurchaseOrderDetailsDto>.NotFound("Order not found.");

        var supplier = await supplierRepository.GetByIdAsync(order.SupplierId, cancellationToken);
        var lineDtos = new List<PurchaseOrderLineDto>();
        foreach (var line in order.Lines)
        {
            string? fabricName = null;
            if (line.FabricItemId is Guid fid)
            {
                var item = await fabricCatalog.GetItemByIdAsync(fid, cancellationToken);
                fabricName = item?.NameAr;
            }
            lineDtos.Add(new PurchaseOrderLineDto
            {
                Id = line.Id,
                FabricItemId = line.FabricItemId,
                FabricItemName = fabricName,
                Description = line.Description,
                Quantity = line.Quantity,
                UnitCost = line.UnitCost.Amount,
                LineTotal = line.LineTotal.Amount
            });
        }

        return ApplicationResult<PurchaseOrderDetailsDto>.Success(new PurchaseOrderDetailsDto
        {
            Id = order.Id,
            OrderNumber = order.OrderNumber,
            SupplierId = order.SupplierId,
            SupplierName = supplier?.Supplier.NameAr ?? "—",
            OrderDate = order.OrderDate,
            ExpectedDeliveryDate = order.ExpectedDeliveryDate,
            Status = order.Status,
            StatusDisplay = order.Status.ToStatusDisplay(),
            TotalAmount = order.TotalAmount.Amount,
            Notes = order.Notes,
            IsReadOnly = order.Status is PurchaseOrderStatus.Received or PurchaseOrderStatus.Cancelled,
            Lines = lineDtos
        });
    }
}

public sealed class GetPurchaseReturnDetailsHandler(
    IPurchaseReturnRepository returnRepository,
    IPurchaseInvoiceRepository invoiceRepository,
    ISupplierRepository supplierRepository,
    IFabricCatalogRepository fabricCatalog)
    : IQueryHandler<GetPurchaseReturnDetailsQuery, ApplicationResult<PurchaseReturnDetailsDto>>
{
    public async Task<ApplicationResult<PurchaseReturnDetailsDto>> HandleAsync(
        GetPurchaseReturnDetailsQuery query,
        CancellationToken cancellationToken = default)
    {
        var ret = await returnRepository.GetByIdAsync(query.ReturnId, cancellationToken);
        if (ret is null)
            return ApplicationResult<PurchaseReturnDetailsDto>.NotFound("Return not found.");

        var invoice = await invoiceRepository.GetByIdAsync(ret.OriginalInvoiceId, cancellationToken);
        var supplier = invoice is not null
            ? await supplierRepository.GetByIdAsync(invoice.SupplierId, cancellationToken)
            : null;

        var lineDtos = new List<PurchaseReturnLineDto>();
        foreach (var line in ret.Lines)
        {
            string? fabricName = null;
            if (line.FabricItemId is Guid fid)
            {
                var item = await fabricCatalog.GetItemByIdAsync(fid, cancellationToken);
                fabricName = item?.NameAr;
            }
            var originalLine = invoice?.Items.FirstOrDefault(i => i.Id == line.OriginalInvoiceItemId);
            lineDtos.Add(new PurchaseReturnLineDto
            {
                Id = line.Id,
                OriginalInvoiceItemId = line.OriginalInvoiceItemId,
                LineType = line.LineType,
                FabricItemId = line.FabricItemId,
                FabricItemName = fabricName,
                FabricColorId = line.FabricColorId,
                Description = originalLine?.Description ?? fabricName ?? "",
                QuantityMeters = line.QuantityMeters,
                MaxQuantityMeters = originalLine?.Quantity.Value ?? line.QuantityMeters,
                UnitPrice = line.UnitPrice.Amount,
                LineTotal = line.LineTotal.Amount
            });
        }

        return ApplicationResult<PurchaseReturnDetailsDto>.Success(new PurchaseReturnDetailsDto
        {
            Id = ret.Id,
            ReturnNumber = ret.ReturnNumber,
            OriginalInvoiceId = ret.OriginalInvoiceId,
            OriginalInvoiceNumber = invoice?.InvoiceNumber ?? "—",
            SupplierId = invoice?.SupplierId ?? Guid.Empty,
            SupplierName = supplier?.Supplier.NameAr ?? "—",
            ReturnDate = ret.ReturnDate,
            Status = ret.Status,
            StatusDisplay = ret.Status.ToStatusDisplay(),
            TotalAmount = ret.TotalAmount.Amount,
            Notes = ret.Notes,
            IsReadOnly = ret.Status != PurchaseReturnStatus.Draft,
            Lines = lineDtos
        });
    }
}

public sealed class GetPostedPurchaseInvoicesForSupplierHandler(
    IPurchaseInvoiceRepository invoiceRepository)
    : IQueryHandler<GetPostedPurchaseInvoicesForSupplierQuery, ApplicationResult<IReadOnlyList<PurchaseInvoicePickDto>>>
{
    public async Task<ApplicationResult<IReadOnlyList<PurchaseInvoicePickDto>>> HandleAsync(
        GetPostedPurchaseInvoicesForSupplierQuery query,
        CancellationToken cancellationToken = default)
    {
        var invoices = await invoiceRepository.GetListAsync(
            query.CompanyId,
            supplierId: query.SupplierId,
            cancellationToken: cancellationToken);

        var rows = invoices
            .Where(i => i.Status is PurchaseInvoiceStatus.Posted
                or PurchaseInvoiceStatus.PartiallyPaid
                or PurchaseInvoiceStatus.Paid)
            .OrderByDescending(i => i.InvoiceDate)
            .Select(i => new PurchaseInvoicePickDto
            {
                Id = i.Id,
                InvoiceNumber = i.InvoiceNumber,
                RemainingAmount = i.Remaining.Amount
            })
            .ToList();

        return ApplicationResult<IReadOnlyList<PurchaseInvoicePickDto>>.Success(rows);
    }
}
