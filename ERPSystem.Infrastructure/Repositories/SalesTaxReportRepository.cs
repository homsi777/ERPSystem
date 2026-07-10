using ERPSystem.Application.Abstractions.Repositories;
using ERPSystem.Application.DTOs.Sales;
using ERPSystem.Domain.Enums;
using ERPSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ERPSystem.Infrastructure.Repositories;

internal sealed class SalesTaxReportRepository(ErpDbContext context) : ISalesTaxReportRepository
{
    public async Task<SalesTaxReportDto> GetReportAsync(
        Guid companyId,
        DateTime fromDate,
        DateTime toDate,
        CancellationToken cancellationToken = default)
    {
        var periodFrom = fromDate.Date;
        var periodTo = toDate.Date;

        var postedStatuses = new[]
        {
            (int)SalesInvoiceStatus.Approved,
            (int)SalesInvoiceStatus.Printed,
            (int)SalesInvoiceStatus.Delivered,
            (int)SalesInvoiceStatus.PartiallyReturned,
            (int)SalesInvoiceStatus.Returned
        };

        var rows = await (
            from tax in context.SalesInvoiceItemTaxes.AsNoTracking()
            join inv in context.SalesInvoices.AsNoTracking() on tax.SalesInvoiceId equals inv.Id
            join cust in context.Customers.AsNoTracking() on inv.CustomerId equals cust.Id
            where inv.CompanyId == companyId
                  && inv.IsActive
                  && !inv.IsArchived
                  && postedStatuses.Contains(inv.Status)
                  && inv.InvoiceDate >= periodFrom
                  && inv.InvoiceDate <= periodTo.AddDays(1)
                  && tax.IsFrozen
            select new SalesTaxReportRowDto
            {
                InvoiceNumber = inv.InvoiceNumber,
                InvoiceDate = inv.InvoiceDate,
                CustomerName = cust.NameAr,
                TaxCode = tax.TaxCode,
                TaxRate = tax.TaxRate,
                TaxableAmount = tax.TaxableAmount,
                TaxAmount = tax.TaxAmount,
                IsLegacyUntaxed = inv.IsLegacyUntaxed,
                JournalEntryNumber = null,
                PostingStatus = inv.IsLegacyUntaxed ? "Legacy Untaxed Invoice" : "Posted"
            }).ToListAsync(cancellationToken);

        var summary = rows
            .Where(r => !r.IsLegacyUntaxed && r.TaxAmount > 0)
            .GroupBy(r => r.TaxCode)
            .Select(g => new SalesTaxReportSummaryDto
            {
                TaxCode = g.Key,
                TaxableAmount = g.Sum(x => x.TaxableAmount),
                TaxAmount = g.Sum(x => x.TaxAmount)
            })
            .ToList();

        return new SalesTaxReportDto { Rows = rows, SummaryByTaxCode = summary };
    }
}
