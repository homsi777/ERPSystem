using ERPSystem.Application.Abstractions;
using ERPSystem.Application.Abstractions.Repositories;
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

        var suppliers = await supplierRepository.GetListAsync(query.CompanyId, cancellationToken: cancellationToken);
        var supplierNames = suppliers.ToDictionary(s => s.Supplier.Id, s => s.Supplier.Name);

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

public sealed class GetContainerOperationsCenterHandler(IChinaContainerRepository containerRepository)
    : IQueryHandler<GetContainerOperationsCenterQuery, ApplicationResult<ContainerOperationsCenterDto>>
{
    public async Task<ApplicationResult<ContainerOperationsCenterDto>> HandleAsync(
        GetContainerOperationsCenterQuery query,
        CancellationToken cancellationToken = default)
    {
        var aggregate = await containerRepository.GetByIdAsync(query.ContainerId, cancellationToken);
        if (aggregate is null)
            return ApplicationResult<ContainerOperationsCenterDto>.NotFound("Container not found.");

        return ApplicationResult<ContainerOperationsCenterDto>.Success(
            ContainerMapper.ToOperationsCenterDto(aggregate));
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
    ICustomerRepository customerRepository)
    : IQueryHandler<GetSalesInvoiceListQuery, ApplicationResult<PagedResult<SalesInvoiceDto>>>
{
    public async Task<ApplicationResult<PagedResult<SalesInvoiceDto>>> HandleAsync(
        GetSalesInvoiceListQuery query,
        CancellationToken cancellationToken = default)
    {
        var invoices = await invoiceRepository.GetListAsync(
            query.CompanyId, query.BranchId, query.Status, query.CustomerId, cancellationToken);

        var customers = await customerRepository.GetListAsync(query.CompanyId, cancellationToken: cancellationToken);
        var customerNames = customers.ToDictionary(c => c.Customer.Id, c => c.Customer.NameAr);

        var items = invoices
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(i => SalesInvoiceMapper.ToDto(
                i,
                customerNames.GetValueOrDefault(i.CustomerId, "")))
            .ToList();

        return ApplicationResult<PagedResult<SalesInvoiceDto>>.Success(new PagedResult<SalesInvoiceDto>
        {
            Items = items,
            TotalCount = invoices.Count,
            Page = query.Page,
            PageSize = query.PageSize
        });
    }
}

public sealed class GetSalesInvoiceOperationsCenterHandler(
    ISalesInvoiceRepository invoiceRepository,
    ICustomerRepository customerRepository)
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

        return ApplicationResult<SalesInvoiceOperationsCenterDto>.Success(
            SalesInvoiceMapper.ToOperationsCenterDto(aggregate, customerName));
    }
}

public sealed class GetWarehouseDetailingQueueHandler(
    ISalesInvoiceRepository invoiceRepository,
    ICustomerRepository customerRepository)
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
            dtos.Add(SalesInvoiceMapper.ToDetailingDto(invoice, customerName));
        }

        return ApplicationResult<IReadOnlyList<WarehouseDetailingDto>>.Success(dtos);
    }
}

public sealed class GetReportPreviewHandler
    : IQueryHandler<GetReportPreviewQuery, ApplicationResult<Dictionary<string, object>>>
{
    public Task<ApplicationResult<Dictionary<string, object>>> HandleAsync(
        GetReportPreviewQuery query,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query.ReportCode))
            return Task.FromResult(ApplicationResult<Dictionary<string, object>>.ValidationFailed(
                nameof(query.ReportCode), "Report code is required."));

        var preview = new Dictionary<string, object>
        {
            ["ReportCode"] = query.ReportCode,
            ["GeneratedAt"] = DateTime.UtcNow,
            ["Status"] = "PreviewNotImplemented"
        };

        return Task.FromResult(ApplicationResult<Dictionary<string, object>>.Success(preview));
    }
}
