using ERPSystem.Application.Abstractions;
using ERPSystem.Application.Abstractions.Repositories;
using ERPSystem.Application.Common;
using ERPSystem.Application.DTOs.Customers;
using ERPSystem.Application.DTOs.Dashboard;
using ERPSystem.Application.Mapping;
using ERPSystem.Application.Queries.Customers;
using ERPSystem.Application.Queries.Dashboard;
using ERPSystem.Application.Results;
using ERPSystem.Domain.Enums;

namespace ERPSystem.Application.UseCases.Queries;

public sealed class GetDashboardSummaryHandler(
    ISalesInvoiceRepository invoiceRepository,
    IChinaContainerRepository containerRepository,
    ICustomerRepository customerRepository)
    : IQueryHandler<GetDashboardSummaryQuery, ApplicationResult<DashboardSummaryDto>>
{
    public async Task<ApplicationResult<DashboardSummaryDto>> HandleAsync(
        GetDashboardSummaryQuery query,
        CancellationToken cancellationToken = default)
    {
        if (query.CompanyId == Guid.Empty || query.BranchId == Guid.Empty)
            return ApplicationResult<DashboardSummaryDto>.ValidationFailed("CompanyId", "Company and branch are required.");

        var invoices = await invoiceRepository.GetListAsync(
            query.CompanyId, query.BranchId, cancellationToken: cancellationToken);

        var containers = await containerRepository.GetListAsync(
            query.CompanyId, query.BranchId, cancellationToken: cancellationToken);

        var customers = await customerRepository.GetListAsync(query.CompanyId, cancellationToken: cancellationToken);

        var dto = new DashboardSummaryDto
        {
            PendingContainersCount = containers.Count(c =>
                c.Status is ChinaContainerStatus.Draft or
                ChinaContainerStatus.UnderReview or
                ChinaContainerStatus.LandingCostReviewed),
            AwaitingDetailingCount = invoices.Count(i => i.Status == SalesInvoiceStatus.AwaitingDetailing),
            ReadyForApprovalInvoicesCount = invoices.Count(i =>
                i.Status is SalesInvoiceStatus.Detailed or SalesInvoiceStatus.ReadyForApproval),
            OpenReceiptsCount = 0,
            TotalCustomerOutstanding = customers.Sum(c => c.Customer.Balance.Amount),
            TodaySalesTotal = invoices
                .Where(i => i.InvoiceDate.Date == DateTime.UtcNow.Date && i.Status >= SalesInvoiceStatus.Approved)
                .Sum(i => i.GrandTotal.Amount),
            LowStockItemsCount = 0
        };

        return ApplicationResult<DashboardSummaryDto>.Success(dto);
    }
}

public sealed class GetCustomerListHandler(ICustomerRepository customerRepository)
    : IQueryHandler<GetCustomerListQuery, ApplicationResult<PagedResult<CustomerListDto>>>
{
    public async Task<ApplicationResult<PagedResult<CustomerListDto>>> HandleAsync(
        GetCustomerListQuery query,
        CancellationToken cancellationToken = default)
    {
        var customers = await customerRepository.GetListAsync(query.CompanyId, query.Search, cancellationToken);
        var items = customers
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(CustomerMapper.ToListDto)
            .ToList();

        return ApplicationResult<PagedResult<CustomerListDto>>.Success(new PagedResult<CustomerListDto>
        {
            Items = items,
            TotalCount = customers.Count,
            Page = query.Page,
            PageSize = query.PageSize
        });
    }
}

public sealed class GetCustomerOperationsCenterHandler(
    ISalesInvoiceRepository invoiceRepository,
    ICustomerRepository customerRepository)
    : IQueryHandler<GetCustomerOperationsCenterQuery, ApplicationResult<CustomerOperationsCenterDto>>
{
    public async Task<ApplicationResult<CustomerOperationsCenterDto>> HandleAsync(
        GetCustomerOperationsCenterQuery query,
        CancellationToken cancellationToken = default)
    {
        var aggregate = await customerRepository.GetByIdAsync(query.CustomerId, cancellationToken);
        if (aggregate is null)
            return ApplicationResult<CustomerOperationsCenterDto>.NotFound("Customer not found.");

        var invoices = await invoiceRepository.GetListAsync(
            aggregate.Customer.CompanyId,
            customerId: query.CustomerId,
            cancellationToken: cancellationToken);

        var dto = new CustomerOperationsCenterDto
        {
            Customer = CustomerMapper.ToDetailsDto(aggregate),
            OpenInvoicesCount = invoices.Count(i =>
                i.Status is not (SalesInvoiceStatus.Delivered or SalesInvoiceStatus.Cancelled)),
            TotalOutstanding = aggregate.Customer.Balance.Amount,
            PendingReceiptsCount = 0
        };

        return ApplicationResult<CustomerOperationsCenterDto>.Success(dto);
    }
}

public sealed class GetCustomerStatementHandler(ICustomerRepository customerRepository)
    : IQueryHandler<GetCustomerStatementQuery, ApplicationResult<CustomerStatementDto>>
{
    public async Task<ApplicationResult<CustomerStatementDto>> HandleAsync(
        GetCustomerStatementQuery query,
        CancellationToken cancellationToken = default)
    {
        var aggregate = await customerRepository.GetByIdAsync(query.CustomerId, cancellationToken);
        if (aggregate is null)
            return ApplicationResult<CustomerStatementDto>.NotFound("Customer not found.");

        var dto = new CustomerStatementDto
        {
            CustomerId = aggregate.Customer.Id,
            CustomerName = aggregate.Customer.NameAr,
            OpeningBalance = 0,
            ClosingBalance = aggregate.Customer.Balance.Amount,
            Lines = []
        };

        return ApplicationResult<CustomerStatementDto>.Success(dto);
    }
}
