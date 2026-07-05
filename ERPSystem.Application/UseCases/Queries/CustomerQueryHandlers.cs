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
    ICustomerRepository customerRepository,
    ISupplierRepository supplierRepository,
    IInventoryRepository inventoryRepository,
    IAuditLogRepository auditLogRepository)
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
        var suppliers = await supplierRepository.GetListAsync(query.CompanyId, cancellationToken: cancellationToken);

        var activeCutoff = DateTime.UtcNow.Date.AddDays(-90);
        var activeCustomers = invoices
            .Where(i => i.InvoiceDate.Date >= activeCutoff && i.Status >= SalesInvoiceStatus.AwaitingDetailing)
            .Select(i => i.CustomerId)
            .Distinct()
            .Count();

        var recent = await auditLogRepository.GetRecentAsync(10, cancellationToken);

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
            TotalSupplierPayables = suppliers.Sum(s => s.Supplier.Balance.Amount),
            ActiveCustomersCount = activeCustomers,
            TodaySalesTotal = invoices
                .Where(i => i.InvoiceDate.Date == DateTime.UtcNow.Date && i.Status >= SalesInvoiceStatus.Approved)
                .Sum(i => i.GrandTotal.Amount),
            LowStockItemsCount = await inventoryRepository.CountLowStockItemsAsync(query.BranchId, cancellationToken: cancellationToken),
            RecentActivity = recent.Select(a => new DashboardActivityDto
            {
                OccurredAt = a.OccurredAt,
                EntityType = a.EntityType,
                EntityId = a.EntityId,
                Description = DescribeActivity(a.Action, a.EntityType)
            }).ToList()
        };

        return ApplicationResult<DashboardSummaryDto>.Success(dto);
    }

    private static string DescribeActivity(string action, string entityType)
    {
        var verb = action switch
        {
            "Insert" or "Create" or "Added" => "إنشاء",
            "Update" or "Modified" => "تعديل",
            "Delete" or "Deleted" => "حذف",
            _ => action
        };
        var entity = entityType switch
        {
            "SalesInvoice" or "SalesInvoiceEntity" => "فاتورة بيع",
            "PurchaseInvoice" or "PurchaseInvoiceEntity" => "فاتورة شراء",
            "ReceiptVoucher" or "ReceiptVoucherEntity" => "سند قبض",
            "PaymentVoucher" or "PaymentVoucherEntity" => "سند صرف",
            "Customer" or "CustomerEntity" => "عميل",
            "Supplier" or "SupplierEntity" => "مورد",
            "ChinaContainer" or "ChinaContainerEntity" => "حاوية",
            "Expense" or "ExpenseEntity" => "مصروف",
            _ => entityType
        };
        return $"{verb} {entity}".Trim();
    }
}

public sealed class GetCustomerListHandler(ICustomerRepository customerRepository)
    : IQueryHandler<GetCustomerListQuery, ApplicationResult<PagedResult<CustomerListDto>>>
{
    public async Task<ApplicationResult<PagedResult<CustomerListDto>>> HandleAsync(
        GetCustomerListQuery query,
        CancellationToken cancellationToken = default)
    {
        var (customers, totalCount) = await customerRepository.GetPagedAsync(
            query.CompanyId, query.Search, query.Page, query.PageSize, cancellationToken);

        var items = customers.Select(CustomerMapper.ToListDto).ToList();

        return ApplicationResult<PagedResult<CustomerListDto>>.Success(new PagedResult<CustomerListDto>
        {
            Items = items,
            TotalCount = totalCount,
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

public sealed class GetCustomerStatementHandler(
    ICustomerRepository customerRepository,
    ISalesInvoiceRepository invoiceRepository,
    IReceiptVoucherRepository receiptRepository,
    IAccountingReportRepository accountingReports)
    : IQueryHandler<GetCustomerStatementQuery, ApplicationResult<CustomerStatementDto>>
{
    public async Task<ApplicationResult<CustomerStatementDto>> HandleAsync(
        GetCustomerStatementQuery query,
        CancellationToken cancellationToken = default)
    {
        var aggregate = await customerRepository.GetByIdAsync(query.CustomerId, cancellationToken);
        if (aggregate is null)
            return ApplicationResult<CustomerStatementDto>.NotFound("Customer not found.");

        var from = query.FromDate?.Date ?? DateTime.MinValue;
        var to = query.ToDate?.Date.AddDays(1).AddTicks(-1) ?? DateTime.MaxValue;

        var invoices = await invoiceRepository.GetListAsync(
            aggregate.Customer.CompanyId,
            customerId: query.CustomerId,
            cancellationToken: cancellationToken);

        var receipts = await receiptRepository.GetListAsync(
            aggregate.Customer.CompanyId,
            customerId: query.CustomerId,
            cancellationToken: cancellationToken);

        var rawLines = new List<(DateTime Date, DocumentType Type, string Number, decimal Debit, decimal Credit)>();

        foreach (var inv in invoices.Where(i =>
            i.Status >= SalesInvoiceStatus.Approved &&
            i.InvoiceDate >= from && i.InvoiceDate <= to))
        {
            rawLines.Add((inv.InvoiceDate, DocumentType.SalesInvoice, inv.InvoiceNumber.Value, inv.GrandTotal.Amount, 0));
        }

        foreach (var rv in receipts.Where(r =>
            r.Status == VoucherStatus.Posted &&
            r.VoucherDate >= from && r.VoucherDate <= to))
        {
            rawLines.Add((rv.VoucherDate, DocumentType.ReceiptVoucher, rv.VoucherNumber, 0, rv.Amount.Amount));
        }

        var opening = aggregate.Customer.OpeningBalancePosted
            ? await accountingReports.GetPartyOpeningBalanceAsync(
                query.CustomerId, DocumentType.CustomerOpeningBalance, cancellationToken)
            : 0m;

        var sorted = rawLines.OrderBy(l => l.Date).ThenBy(l => l.Number).ToList();
        var running = opening;
        var lines = new List<CustomerStatementLineDto>();
        foreach (var line in sorted)
        {
            running = running + line.Debit - line.Credit;
            lines.Add(new CustomerStatementLineDto
            {
                EntryDate = line.Date,
                DocumentType = line.Type,
                DocumentNumber = line.Number,
                Debit = line.Debit,
                Credit = line.Credit,
                RunningBalance = running
            });
        }

        var dto = new CustomerStatementDto
        {
            CustomerId = aggregate.Customer.Id,
            CustomerName = aggregate.Customer.NameAr,
            OpeningBalance = opening,
            ClosingBalance = running,
            Lines = lines
        };

        return ApplicationResult<CustomerStatementDto>.Success(dto);
    }
}

public sealed class GetCustomerDetailsHandler(ICustomerRepository customerRepository)
    : IQueryHandler<GetCustomerDetailsQuery, ApplicationResult<CustomerDetailsDto>>
{
    public async Task<ApplicationResult<CustomerDetailsDto>> HandleAsync(
        GetCustomerDetailsQuery query,
        CancellationToken cancellationToken = default)
    {
        var aggregate = await customerRepository.GetByIdAsync(query.CustomerId, cancellationToken);
        if (aggregate is null)
            return ApplicationResult<CustomerDetailsDto>.NotFound("Customer not found.");

        return ApplicationResult<CustomerDetailsDto>.Success(CustomerMapper.ToDetailsDto(aggregate));
    }
}
