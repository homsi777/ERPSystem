using ERPSystem.Application.Abstractions;
using ERPSystem.Application.Abstractions.Repositories;
using ERPSystem.Application.Abstractions.Services;
using ERPSystem.Application.Common;
using ERPSystem.Application.DTOs.Customers;
using ERPSystem.Application.Mapping;
using ERPSystem.Application.Queries.Customers;
using ERPSystem.Application.Results;
using ERPSystem.Domain.Enums;

namespace ERPSystem.Application.UseCases.Queries;

public sealed class GetCustomerAccountLedgerHandler(
    ICustomerRepository customerRepository,
    ISalesInvoiceRepository invoiceRepository,
    ISalesReturnRepository returnRepository,
    IReceiptVoucherRepository receiptRepository,
    IFabricCatalogRepository fabricCatalogRepository,
    IAccountingReportRepository accountingReports)
    : IQueryHandler<GetCustomerAccountLedgerQuery, ApplicationResult<CustomerAccountLedgerDto>>
{
    public async Task<ApplicationResult<CustomerAccountLedgerDto>> HandleAsync(
        GetCustomerAccountLedgerQuery query,
        CancellationToken cancellationToken = default)
    {
        var aggregate = await customerRepository.GetByIdAsync(query.CustomerId, cancellationToken);
        if (aggregate is null)
            return ApplicationResult<CustomerAccountLedgerDto>.NotFound("Customer not found.");

        var from = query.FromDate?.Date ?? DateTime.MinValue;
        var to = query.ToDate?.Date.AddDays(1).AddTicks(-1) ?? DateTime.MaxValue;

        var opening = aggregate.Customer.OpeningBalancePosted
            ? await accountingReports.GetPartyOpeningBalanceAsync(
                query.CustomerId, OpeningBalanceDocumentTypePolicy.SourceType, cancellationToken)
            : 0m;

        var rawLines = new List<CustomerAccountLedgerLineDto>();

        var invoices = await invoiceRepository.GetListAsync(
            aggregate.Customer.CompanyId,
            customerId: query.CustomerId,
            cancellationToken: cancellationToken);

        foreach (var invoice in invoices.Where(i =>
                     i.Status >= SalesInvoiceStatus.Approved &&
                     i.InvoiceDate >= from && i.InvoiceDate <= to))
        {
            var dto = SalesInvoiceMapper.ToDto(invoice, aggregate.Customer.NameAr);
            var enriched = await SalesInvoiceCatalogEnricher.EnrichLinesAsync(
                dto.Lines, fabricCatalogRepository, cancellationToken);

            var lineSubTotal = 0m;
            foreach (var line in enriched)
            {
                lineSubTotal += line.LineTotal;
                var fabricPart = string.IsNullOrWhiteSpace(line.FabricDisplayName) ? "—" : line.FabricDisplayName;
                var colorPart = string.IsNullOrWhiteSpace(line.ColorDisplayName) ? "—" : line.ColorDisplayName;
                rawLines.Add(new CustomerAccountLedgerLineDto
                {
                    MovementType = CustomerAccountMovementType.SalesInvoice,
                    DocumentId = invoice.Id,
                    EntryId = line.Id,
                    DocumentNumber = invoice.InvoiceNumber.Value,
                    TransactionDate = invoice.InvoiceDate,
                    FabricDescription = $"{fabricPart} — {colorPart}",
                    RollCount = line.RollCount,
                    TotalMeters = line.TotalLengthMeters > 0 ? line.TotalLengthMeters : null,
                    LengthUnit = line.Unit,
                    UnitPrice = line.UnitPrice,
                    LineAmount = line.LineTotal,
                    Notes = line.Notes
                });
            }

            // GrandTotal = SubTotal - DiscountTotal + TaxTotal — align with legacy statement debit per invoice.
            var invoiceAdjustment = invoice.GrandTotal.Amount - lineSubTotal;
            if (Math.Abs(invoiceAdjustment) >= 0.01m)
            {
                rawLines.Add(new CustomerAccountLedgerLineDto
                {
                    MovementType = CustomerAccountMovementType.SalesInvoice,
                    DocumentId = invoice.Id,
                    EntryId = InvoiceLedgerAdjustmentEntryId(invoice.Id),
                    DocumentNumber = invoice.InvoiceNumber.Value,
                    TransactionDate = invoice.InvoiceDate,
                    FabricDescription = BuildInvoiceAdjustmentDescription(invoice),
                    RollCount = null,
                    TotalMeters = null,
                    UnitPrice = null,
                    LineAmount = invoiceAdjustment,
                    Notes = null
                });
            }
        }

        var returns = await returnRepository.GetListAsync(
            aggregate.Customer.CompanyId,
            customerId: query.CustomerId,
            status: VoucherStatus.Posted,
            cancellationToken: cancellationToken);

        foreach (var salesReturn in returns.Where(r => r.ReturnDate >= from && r.ReturnDate <= to))
        {
            foreach (var line in salesReturn.Lines)
            {
                var fabric = await fabricCatalogRepository.GetItemByIdAsync(line.FabricItemId, cancellationToken);
                var color = await fabricCatalogRepository.GetColorByIdAsync(line.FabricColorId, cancellationToken);
                var fabricPart = fabric?.NameAr ?? "—";
                var colorPart = color?.NameAr ?? "—";
                var notes = !string.IsNullOrWhiteSpace(salesReturn.Notes)
                    ? salesReturn.Notes
                    : salesReturn.ReasonNotes;

                rawLines.Add(new CustomerAccountLedgerLineDto
                {
                    MovementType = CustomerAccountMovementType.SalesReturn,
                    DocumentId = salesReturn.Id,
                    EntryId = line.Id,
                    DocumentNumber = salesReturn.ReturnNumber,
                    TransactionDate = salesReturn.ReturnDate,
                    FabricDescription = $"{fabricPart} — {colorPart}",
                    RollCount = null,
                    TotalMeters = line.ReturnMeters,
                    UnitPrice = line.UnitPrice.Amount,
                    LineAmount = line.LineTotal.Amount,
                    Notes = notes
                });
            }
        }

        var receipts = await receiptRepository.GetListAsync(
            aggregate.Customer.CompanyId,
            customerId: query.CustomerId,
            status: VoucherStatus.Posted,
            cancellationToken: cancellationToken);

        foreach (var receipt in receipts.Where(r => r.VoucherDate >= from && r.VoucherDate <= to))
        {
            rawLines.Add(new CustomerAccountLedgerLineDto
            {
                MovementType = CustomerAccountMovementType.ReceiptVoucher,
                DocumentId = receipt.Id,
                EntryId = receipt.Id,
                DocumentNumber = receipt.VoucherNumber,
                TransactionDate = receipt.VoucherDate,
                FabricDescription = "",
                RollCount = null,
                TotalMeters = null,
                UnitPrice = null,
                LineAmount = -receipt.Amount.Amount,
                Notes = null
            });
        }

        var sorted = rawLines
            .OrderBy(l => l.TransactionDate)
            .ThenBy(l => l.DocumentNumber, StringComparer.OrdinalIgnoreCase)
            .ThenBy(l => l.EntryId)
            .ToList();

        var running = opening;
        var lines = new List<CustomerAccountLedgerLineDto>(sorted.Count);
        foreach (var line in sorted)
        {
            running += line.MovementType switch
            {
                CustomerAccountMovementType.SalesInvoice => line.LineAmount,
                CustomerAccountMovementType.SalesReturn => -line.LineAmount,
                CustomerAccountMovementType.ReceiptVoucher => line.LineAmount,
                _ => 0m
            };

            lines.Add(new CustomerAccountLedgerLineDto
            {
                MovementType = line.MovementType,
                DocumentId = line.DocumentId,
                EntryId = line.EntryId,
                DocumentNumber = line.DocumentNumber,
                TransactionDate = line.TransactionDate,
                FabricDescription = line.FabricDescription,
                RollCount = line.RollCount,
                TotalMeters = line.TotalMeters,
                LengthUnit = line.LengthUnit,
                UnitPrice = line.UnitPrice,
                LineAmount = line.LineAmount,
                Notes = line.Notes,
                RunningBalance = running
            });
        }

        return ApplicationResult<CustomerAccountLedgerDto>.Success(new CustomerAccountLedgerDto
        {
            CustomerId = aggregate.Customer.Id,
            CustomerName = aggregate.Customer.NameAr,
            OpeningBalance = opening,
            ClosingBalance = running,
            LastReconciliationDate = aggregate.Customer.LastReconciliationDate,
            LastReconciliationBalance = aggregate.Customer.LastReconciliationBalance,
            LastReconciliationDocumentId = aggregate.Customer.LastReconciliationDocumentId,
            Lines = lines
        });
    }

    private static Guid InvoiceLedgerAdjustmentEntryId(Guid invoiceId)
    {
        var bytes = invoiceId.ToByteArray();
        bytes[15] = (byte)(bytes[15] ^ 0xAD);
        return new Guid(bytes);
    }

    private static string BuildInvoiceAdjustmentDescription(Domain.Aggregates.SalesInvoiceAggregate invoice)
    {
        var hasDiscount = invoice.DiscountTotal.Amount > 0;
        var hasTax = invoice.TaxTotal.Amount > 0;
        if (hasDiscount && hasTax)
            return $"خصم وضريبة — {invoice.InvoiceNumber.Value}";
        if (hasDiscount)
            return $"خصم — {invoice.InvoiceNumber.Value}";
        if (hasTax)
            return $"ضريبة — {invoice.InvoiceNumber.Value}";
        return $"تسوية — {invoice.InvoiceNumber.Value}";
    }
}
