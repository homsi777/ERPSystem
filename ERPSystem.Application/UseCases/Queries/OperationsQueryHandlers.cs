using ERPSystem.Application.Abstractions;
using ERPSystem.Application.Abstractions.Repositories;
using ERPSystem.Application.Abstractions.Services;
using ERPSystem.Application.Common;
using ERPSystem.Application.DTOs.Containers;
using ERPSystem.Application.DTOs.Sales;
using ERPSystem.Application.DTOs.Warehouses;
using ERPSystem.Application.Mapping;
using ERPSystem.Application.Queries.Containers;
using ERPSystem.Application.Queries.Sales;
using ERPSystem.Application.Queries.Warehouses;
using ERPSystem.Application.Queries.Reports;
using ERPSystem.Application.Results;

namespace ERPSystem.Application.UseCases.Queries;

public sealed class GetChinaContainerListHandler(
    IChinaContainerRepository containerRepository,
    ISupplierRepository supplierRepository)
    : IQueryHandler<GetChinaContainerListQuery, ApplicationResult<PagedResult<ContainerListDto>>>
{
    public async Task<ApplicationResult<PagedResult<ContainerListDto>>> HandleAsync(
        GetChinaContainerListQuery query,
        CancellationToken cancellationToken = default)
    {
        var containers = await containerRepository.GetListAsync(
            query.CompanyId, query.BranchId, query.Status, cancellationToken);

        var supplierIds = containers.Select(c => c.SupplierId).Distinct();
        var supplierNames = await supplierRepository.GetNameLookupAsync(
            query.CompanyId, supplierIds, cancellationToken);

        var items = containers
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(c => ContainerMapper.ToListDto(c, supplierNames.GetValueOrDefault(c.SupplierId, "—")))
            .ToList();

        return ApplicationResult<PagedResult<ContainerListDto>>.Success(new PagedResult<ContainerListDto>
        {
            Items = items,
            TotalCount = containers.Count,
            Page = query.Page,
            PageSize = query.PageSize
        });
    }
}

public sealed class GetContainerOperationsCenterHandler(
    IChinaContainerRepository containerRepository,
    ISupplierRepository supplierRepository,
    IInventoryRepository inventoryRepository)
    : IQueryHandler<GetContainerOperationsCenterQuery, ApplicationResult<ContainerOperationsCenterDto>>
{
    public async Task<ApplicationResult<ContainerOperationsCenterDto>> HandleAsync(
        GetContainerOperationsCenterQuery query,
        CancellationToken cancellationToken = default)
    {
        var aggregate = await containerRepository.GetByIdAsync(query.ContainerId, cancellationToken);
        if (aggregate is null)
            return ApplicationResult<ContainerOperationsCenterDto>.NotFound("Container not found.");

        var supplier = await supplierRepository.GetByIdAsync(aggregate.SupplierId, cancellationToken);
        var supplierName = supplier?.Supplier.Name ?? "—";
        var inventory = await inventoryRepository.GetContainerMetricsAsync(query.ContainerId, cancellationToken);
        var baseDto = ContainerMapper.ToOperationsCenterDto(aggregate, supplierName);

        return ApplicationResult<ContainerOperationsCenterDto>.Success(new ContainerOperationsCenterDto
        {
            Container = baseDto.Container,
            Inventory = inventory,
            CanApprove = baseDto.CanApprove,
            CanSetSalePrices = baseDto.CanSetSalePrices,
            CanMoveToWarehouse = baseDto.CanMoveToWarehouse,
            CanCalculateLandingCost = baseDto.CanCalculateLandingCost,
            IsReadyForSale = baseDto.IsReadyForSale
        });
    }
}

public sealed class GetWarehouseListHandler(IWarehouseRepository warehouseRepository)
    : IQueryHandler<GetWarehouseListQuery, ApplicationResult<IReadOnlyList<WarehouseListDto>>>
{
    public async Task<ApplicationResult<IReadOnlyList<WarehouseListDto>>> HandleAsync(
        GetWarehouseListQuery query,
        CancellationToken cancellationToken = default)
    {
        var warehouses = await warehouseRepository.GetListAsync(query.BranchId, cancellationToken);
        return ApplicationResult<IReadOnlyList<WarehouseListDto>>.Success(
            warehouses.Select(WarehouseMapper.ToListDto).ToList());
    }
}

public sealed class GetWarehouseOperationsCenterHandler(
    IWarehouseRepository warehouseRepository,
    ISalesInvoiceRepository invoiceRepository)
    : IQueryHandler<GetWarehouseOperationsCenterQuery, ApplicationResult<WarehouseOperationsCenterDto>>
{
    public async Task<ApplicationResult<WarehouseOperationsCenterDto>> HandleAsync(
        GetWarehouseOperationsCenterQuery query,
        CancellationToken cancellationToken = default)
    {
        var aggregate = await warehouseRepository.GetByIdAsync(query.WarehouseId, cancellationToken);
        if (aggregate is null)
            return ApplicationResult<WarehouseOperationsCenterDto>.NotFound("Warehouse not found.");

        var detailingQueue = await invoiceRepository.GetDetailingQueueAsync(query.WarehouseId, cancellationToken);

        var dto = new WarehouseOperationsCenterDto
        {
            Warehouse = WarehouseMapper.ToListDto(aggregate),
            Stock = aggregate.Balances.Select(b => new WarehouseStockDto
            {
                WarehouseId = b.WarehouseId,
                FabricItemId = b.FabricItemId,
                FabricColorId = b.FabricColorId,
                ContainerId = b.ContainerId,
                RollCount = b.RollCount,
                TotalMeters = b.TotalMeters.Value,
                ReservedMeters = b.ReservedMeters.Value,
                AvailableMeters = b.AvailableMeters.Value
            }).ToList(),
            PendingDetailingCount = detailingQueue.Count
        };

        return ApplicationResult<WarehouseOperationsCenterDto>.Success(dto);
    }
}

public sealed class GetSalesInvoiceListHandler(
    ISalesInvoiceRepository invoiceRepository,
    ISalesInvoiceListLookupLoader lookupLoader)
    : IQueryHandler<GetSalesInvoiceListQuery, ApplicationResult<PagedResult<SalesInvoiceDto>>>
{
    public async Task<ApplicationResult<PagedResult<SalesInvoiceDto>>> HandleAsync(
        GetSalesInvoiceListQuery query,
        CancellationToken cancellationToken = default)
    {
        var (invoices, totalCount) = await invoiceRepository.GetPagedListAsync(
            query.CompanyId,
            query.BranchId,
            query.Status,
            query.CustomerId,
            query.Page,
            query.PageSize,
            cancellationToken);

        if (invoices.Count == 0)
        {
            return ApplicationResult<PagedResult<SalesInvoiceDto>>.Success(new PagedResult<SalesInvoiceDto>
            {
                Items = [],
                TotalCount = totalCount,
                Page = query.Page,
                PageSize = query.PageSize
            });
        }

        var (customerNames, warehouseNames, containerNumbers) = await lookupLoader.LoadAsync(
            query.CompanyId,
            invoices.Select(i => i.CustomerId),
            invoices.Select(i => i.WarehouseId),
            invoices.Select(i => i.ChinaContainerId),
            cancellationToken);

        var items = invoices
            .Select(i =>
            {
                var dto = SalesInvoiceMapper.ToDto(i, customerNames.GetValueOrDefault(i.CustomerId, ""));
                return dto with
                {
                    WarehouseName = warehouseNames.GetValueOrDefault(i.WarehouseId, "—"),
                    ContainerNumber = containerNumbers.GetValueOrDefault(i.ChinaContainerId, "—")
                };
            })
            .ToList();

        return ApplicationResult<PagedResult<SalesInvoiceDto>>.Success(new PagedResult<SalesInvoiceDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = query.Page,
            PageSize = query.PageSize
        });
    }
}

public sealed class GetSalesInvoiceOperationsCenterHandler(
    ISalesInvoiceRepository invoiceRepository,
    ICustomerRepository customerRepository,
    IFabricCatalogRepository fabricCatalogRepository,
    IJournalEntryRepository journalEntryRepository,
    IReceiptInvoicePaymentRepository paymentRepository,
    ISalesReturnRepository salesReturnRepository,
    IWarehouseRepository warehouseRepository,
    IChinaContainerRepository containerRepository)
    : IQueryHandler<GetSalesInvoiceOperationsCenterQuery, ApplicationResult<SalesInvoiceOperationsCenterDto>>
{
    public async Task<ApplicationResult<SalesInvoiceOperationsCenterDto>> HandleAsync(
        GetSalesInvoiceOperationsCenterQuery query,
        CancellationToken cancellationToken = default)
    {
        var aggregate = await invoiceRepository.GetByIdAsync(query.InvoiceId, cancellationToken);
        if (aggregate is null)
            return ApplicationResult<SalesInvoiceOperationsCenterDto>.NotFound("Invoice not found.");

        var customer = await customerRepository.GetByIdAsync(aggregate.CustomerId, cancellationToken);
        var customerName = customer?.Customer.NameAr ?? "";
        var customerPhone = customer?.Customer.Phone?.ToString();

        var baseDto = SalesInvoiceMapper.ToOperationsCenterDto(aggregate, customerName);
        var enrichedLines = await SalesInvoiceCatalogEnricher.EnrichLinesAsync(
            baseDto.Invoice.Lines,
            fabricCatalogRepository,
            cancellationToken);

        WarehouseDetailingDto? enrichedDetailing = null;
        if (baseDto.Detailing is not null)
        {
            var enrichedRolls = await SalesInvoiceCatalogEnricher.EnrichRollsAsync(
                aggregate,
                baseDto.Detailing.Rolls,
                fabricCatalogRepository,
                containerRepository,
                cancellationToken);
            enrichedDetailing = SalesInvoiceCatalogEnricher.WithEnrichedRolls(baseDto.Detailing, enrichedRolls);
        }

        // Journal entries linked to this invoice (approval, delivery/COGS, returns)
        var journalRows = await journalEntryRepository.GetBySourceIdAsync(aggregate.Id, cancellationToken);
        var journalEntries = await journalEntryRepository.GetByIdsAsync(
            journalRows.Select(row => row.Id), cancellationToken);
        var journalRowsById = journalRows.ToDictionary(row => row.Id);
        var journalDtos = new List<Application.DTOs.Finance.JournalEntryDto>();
        foreach (var entry in journalEntries)
        {
            journalRowsById.TryGetValue(entry.Id, out var row);
            journalDtos.Add(new Application.DTOs.Finance.JournalEntryDto
            {
                Id = entry.Id,
                EntryNumber = entry.EntryNumber,
                EntryDate = entry.EntryDate,
                Description = entry.Description ?? row?.Description ?? "",
                Status = entry.Status,
                DebitTotal = entry.Lines.Sum(l => l.Debit.Amount),
                CreditTotal = entry.Lines.Sum(l => l.Credit.Amount),
                Lines = entry.Lines.Select(l => new Application.DTOs.Finance.JournalEntryLineDto
                {
                    AccountId = l.AccountId,
                    AccountCode = l.AccountId.ToString(),
                    Debit = l.Debit.Amount,
                    Credit = l.Credit.Amount,
                    Narrative = l.Narrative ?? ""
                }).ToList()
            });
        }

        // Payments (receipts) applied to this invoice
        var payments = await paymentRepository.GetByInvoiceWithVoucherAsync(aggregate.Id, cancellationToken);
        var paymentDtos = payments
            .Select(p => new ReceiptInvoicePaymentDto
            {
                SalesInvoiceId = p.Payment.SalesInvoiceId,
                ReceiptVoucherId = p.Payment.ReceiptVoucherId,
                ReceiptNumber = p.VoucherNumber,
                Amount = p.Payment.Amount.Amount,
                AppliedAt = p.Payment.AppliedAt
            })
            .ToList();
        var collected = paymentDtos.Sum(p => p.Amount);

        // Returns referencing this invoice
        var returns = await salesReturnRepository.GetListAsync(
            aggregate.CompanyId, aggregate.BranchId, originalInvoiceId: aggregate.Id, cancellationToken: cancellationToken);
        var returnDtos = returns.Select(r => new SalesReturnDto
        {
            Id = r.Id,
            ReturnNumber = r.ReturnNumber,
            OriginalInvoiceId = r.OriginalInvoiceId,
            OriginalInvoiceNumber = aggregate.InvoiceNumber.Value,
            CustomerId = r.CustomerId,
            CustomerName = customerName,
            WarehouseId = r.WarehouseId,
            ReturnDate = r.ReturnDate,
            Reason = r.Reason,
            ReasonNotes = r.ReasonNotes,
            Notes = r.Notes,
            Status = r.Status,
            TotalAmount = r.TotalAmount.Amount,
            Lines = r.Lines.Select(l => new SalesReturnLineDto
            {
                Id = l.Id,
                LineNumber = l.LineNumber,
                OriginalInvoiceItemId = l.OriginalInvoiceItemId,
                FabricItemId = l.FabricItemId,
                FabricColorId = l.FabricColorId,
                ReturnMeters = l.ReturnMeters,
                UnitPrice = l.UnitPrice.Amount,
                LineTotal = l.LineTotal.Amount
            }).ToList()
        }).ToList();

        // Warehouse name (for header display)
        string? warehouseName = null;
        if (aggregate.WarehouseId != Guid.Empty)
        {
            var wh = await warehouseRepository.GetByIdAsync(aggregate.WarehouseId, cancellationToken);
            warehouseName = wh?.Warehouse.NameAr;
        }

        return ApplicationResult<SalesInvoiceOperationsCenterDto>.Success(new SalesInvoiceOperationsCenterDto
        {
            Invoice = SalesInvoiceCatalogEnricher.WithEnrichedLines(baseDto.Invoice, enrichedLines),
            Detailing = enrichedDetailing,
            CanSendToWarehouse = baseDto.CanSendToWarehouse,
            CanCompleteDetailing = baseDto.CanCompleteDetailing,
            CanApprove = baseDto.CanApprove,
            CanCancel = baseDto.CanCancel,
            JournalEntries = journalDtos,
            Payments = paymentDtos,
            CollectedAmount = collected,
            RemainingBalance = Math.Max(0, aggregate.GrandTotal.Amount - collected),
            Returns = returnDtos,
            WarehouseName = warehouseName,
            CustomerPhone = customerPhone
        });
    }
}

public sealed class GetWarehouseDetailingQueueHandler(
    ISalesInvoiceRepository invoiceRepository,
    ICustomerRepository customerRepository,
    IFabricCatalogRepository fabricCatalogRepository,
    IChinaContainerRepository containerRepository)
    : IQueryHandler<GetWarehouseDetailingQueueQuery, ApplicationResult<IReadOnlyList<WarehouseDetailingDto>>>
{
    public async Task<ApplicationResult<IReadOnlyList<WarehouseDetailingDto>>> HandleAsync(
        GetWarehouseDetailingQueueQuery query,
        CancellationToken cancellationToken = default)
    {
        var invoices = await invoiceRepository.GetDetailingQueueAsync(query.WarehouseId, cancellationToken);
        var dtos = new List<WarehouseDetailingDto>();

        foreach (var invoice in invoices)
        {
            var customer = await customerRepository.GetByIdAsync(invoice.CustomerId, cancellationToken);
            var customerName = customer?.Customer.NameAr ?? "";
            var baseDto = SalesInvoiceMapper.ToDetailingDto(invoice, customerName);
            var enrichedRolls = await SalesInvoiceCatalogEnricher.EnrichRollsAsync(
                invoice,
                baseDto.Rolls,
                fabricCatalogRepository,
                containerRepository,
                cancellationToken);
            dtos.Add(SalesInvoiceCatalogEnricher.WithEnrichedRolls(baseDto, enrichedRolls));
        }

        return ApplicationResult<IReadOnlyList<WarehouseDetailingDto>>.Success(dtos);
    }
}

public sealed class GetReportPreviewHandler(
    IInventoryRepository inventoryRepository,
    IChinaContainerRepository containerRepository)
    : IQueryHandler<GetReportPreviewQuery, ApplicationResult<Dictionary<string, object>>>
{
    public async Task<ApplicationResult<Dictionary<string, object>>> HandleAsync(
        GetReportPreviewQuery query,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query.ReportCode))
            return ApplicationResult<Dictionary<string, object>>.ValidationFailed(
                nameof(query.ReportCode), "Report code is required.");

        if (query.ReportCode.Equals("ContainerInventory", StringComparison.OrdinalIgnoreCase) ||
            query.ReportCode.Equals("Rep_Containers", StringComparison.OrdinalIgnoreCase))
        {
            return await BuildContainerInventoryReportAsync(query, cancellationToken);
        }

        var preview = new Dictionary<string, object>
        {
            ["ReportCode"] = query.ReportCode,
            ["GeneratedAt"] = DateTime.UtcNow,
            ["Status"] = "PreviewNotImplemented"
        };

        return ApplicationResult<Dictionary<string, object>>.Success(preview);
    }

    private async Task<ApplicationResult<Dictionary<string, object>>> BuildContainerInventoryReportAsync(
        GetReportPreviewQuery query,
        CancellationToken cancellationToken)
    {
        var containers = await containerRepository.GetListAsync(
            query.CompanyId, query.BranchId, status: null, cancellationToken);

        var rows = new List<Dictionary<string, object>>();
        decimal totalImported = 0m;
        decimal totalAvailable = 0m;
        decimal totalReserved = 0m;
        decimal totalSold = 0m;
        decimal totalValuation = 0m;

        foreach (var container in containers)
        {
            var metrics = await inventoryRepository.GetContainerMetricsAsync(container.Id, cancellationToken);
            if (metrics is null)
                continue;

            totalImported += metrics.TotalMeters;
            totalAvailable += metrics.AvailableMeters;
            totalReserved += metrics.ReservedMeters;
            totalSold += metrics.SoldMeters;
            totalValuation += metrics.InventoryValuation;

            rows.Add(new Dictionary<string, object>
            {
                ["ContainerNumber"] = container.ContainerNumber.Value,
                ["TotalMeters"] = metrics.TotalMeters,
                ["AvailableMeters"] = metrics.AvailableMeters,
                ["ReservedMeters"] = metrics.ReservedMeters,
                ["SoldMeters"] = metrics.SoldMeters,
                ["CostPerMeter"] = metrics.CostPerMeter,
                ["InventoryValuation"] = metrics.InventoryValuation
            });
        }

        var avgCost = totalImported > 0 ? totalValuation / Math.Max(totalAvailable, 1m) : 0m;

        return ApplicationResult<Dictionary<string, object>>.Success(new Dictionary<string, object>
        {
            ["ReportCode"] = query.ReportCode,
            ["GeneratedAt"] = DateTime.UtcNow,
            ["ImportedMeters"] = totalImported,
            ["AvailableMeters"] = totalAvailable,
            ["ReservedMeters"] = totalReserved,
            ["SoldMeters"] = totalSold,
            ["RemainingMeters"] = totalAvailable,
            ["AverageCostPerMeter"] = avgCost,
            ["InventoryValuation"] = totalValuation,
            ["Containers"] = rows
        });
    }
}
