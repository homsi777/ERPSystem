using ERPSystem.Application.Abstractions;
using ERPSystem.Application.Abstractions.Repositories;
using ERPSystem.Application.DTOs.Accounting;
using ERPSystem.Application.Queries.Accounting;
using ERPSystem.Application.Results;
using ERPSystem.Domain.Enums;

namespace ERPSystem.Application.UseCases.Queries;

public sealed class GetReceivablesAgingHandler(
    ICustomerRepository customerRepository,
    ISalesInvoiceRepository invoiceRepository)
    : IQueryHandler<GetReceivablesAgingQuery, ApplicationResult<IReadOnlyList<ReceivablesAgingRowDto>>>
{
    public async Task<ApplicationResult<IReadOnlyList<ReceivablesAgingRowDto>>> HandleAsync(
        GetReceivablesAgingQuery query,
        CancellationToken cancellationToken = default)
    {
        var customers = await customerRepository.GetWithPositiveBalanceAsync(query.CompanyId, cancellationToken);
        if (customers.Count == 0)
            return ApplicationResult<IReadOnlyList<ReceivablesAgingRowDto>>.Success([]);

        var aggregates = await invoiceRepository.GetReceivablesAgingAggregatesAsync(query.CompanyId, cancellationToken);
        var now = DateTime.UtcNow;

        var rows = customers.Select(c =>
        {
            aggregates.TryGetValue(c.Id, out var invoiceAgg);
            var balance = c.Customer.Balance.Amount;
            var totalInvoiced = invoiceAgg?.TotalInvoiced ?? 0m;
            var oldest = invoiceAgg?.OldestInvoiceDate;
            var collected = Math.Max(0m, totalInvoiced - balance);
            return new ReceivablesAgingRowDto
            {
                CustomerId = c.Id,
                CustomerCode = c.Customer.Code,
                CustomerName = c.Customer.NameAr,
                TotalInvoiced = totalInvoiced,
                Collected = collected,
                Outstanding = balance,
                OldestInvoiceDate = oldest,
                DaysOverdue = oldest.HasValue ? Math.Max(0, (int)(now - oldest.Value).TotalDays) : 0
            };
        }).ToList();

        return ApplicationResult<IReadOnlyList<ReceivablesAgingRowDto>>.Success(rows);
    }
}

public sealed class GetPayablesAgingHandler(
    IPurchaseInvoiceRepository invoiceRepository)
    : IQueryHandler<GetPayablesAgingQuery, ApplicationResult<IReadOnlyList<PayablesAgingRowDto>>>
{
    public async Task<ApplicationResult<IReadOnlyList<PayablesAgingRowDto>>> HandleAsync(
        GetPayablesAgingQuery query,
        CancellationToken cancellationToken = default)
    {
        var aggregates = await invoiceRepository.GetPayablesAgingAsync(query.CompanyId, cancellationToken);
        var now = DateTime.UtcNow;

        var rows = aggregates.Select(a => new PayablesAgingRowDto
        {
            SupplierId = a.SupplierId,
            SupplierName = a.SupplierName,
            TotalInvoiced = a.TotalInvoiced,
            Paid = a.Paid,
            Outstanding = a.Outstanding,
            OldestInvoiceDate = a.OldestInvoiceDate,
            DaysOverdue = a.OldestInvoiceDate.HasValue
                ? Math.Max(0, (int)(now - a.OldestInvoiceDate.Value).TotalDays)
                : 0
        }).ToList();

        return ApplicationResult<IReadOnlyList<PayablesAgingRowDto>>.Success(rows);
    }
}
