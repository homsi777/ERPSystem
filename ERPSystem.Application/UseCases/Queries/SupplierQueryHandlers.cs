using ERPSystem.Application.Abstractions;
using ERPSystem.Application.Abstractions.Repositories;
using ERPSystem.Application.Common;
using ERPSystem.Application.DTOs.Suppliers;
using ERPSystem.Application.Mapping;
using ERPSystem.Application.Queries.Suppliers;
using ERPSystem.Application.Results;
using ERPSystem.Domain.Enums;

namespace ERPSystem.Application.UseCases.Queries;

public sealed class GetSupplierListHandler(ISupplierRepository supplierRepository)
    : IQueryHandler<GetSupplierListQuery, ApplicationResult<PagedResult<SupplierListDto>>>
{
    public async Task<ApplicationResult<PagedResult<SupplierListDto>>> HandleAsync(
        GetSupplierListQuery query,
        CancellationToken cancellationToken = default)
    {
        var (items, totalCount) = await supplierRepository.GetPagedAsync(
            query.CompanyId,
            query.Search,
            query.Country,
            query.PaymentTermsDays,
            query.HasBalance,
            query.Page,
            query.PageSize,
            cancellationToken);

        return ApplicationResult<PagedResult<SupplierListDto>>.Success(new PagedResult<SupplierListDto>
        {
            Items = items.Select(SupplierMapper.ToListDto).ToList(),
            TotalCount = totalCount,
            Page = query.Page,
            PageSize = query.PageSize
        });
    }
}

public sealed class GetSupplierDetailsHandler(
    ISupplierRepository supplierRepository,
    IAccountRepository accountRepository)
    : IQueryHandler<GetSupplierDetailsQuery, ApplicationResult<SupplierDetailsDto>>
{
    public async Task<ApplicationResult<SupplierDetailsDto>> HandleAsync(
        GetSupplierDetailsQuery query,
        CancellationToken cancellationToken = default)
    {
        var aggregate = await supplierRepository.GetByIdAsync(query.SupplierId, cancellationToken);
        if (aggregate is null)
            return ApplicationResult<SupplierDetailsDto>.NotFound("Supplier not found.");

        var account = await accountRepository.GetByIdAsync(aggregate.Supplier.PayablesAccountId, cancellationToken);
        return ApplicationResult<SupplierDetailsDto>.Success(
            SupplierMapper.ToDetailsDto(aggregate, account?.NameAr));
    }
}

public sealed class GetSupplierStatementHandler(
    ISupplierRepository supplierRepository,
    IAccountingReportRepository accountingReports)
    : IQueryHandler<GetSupplierStatementQuery, ApplicationResult<SupplierStatementDto>>
{
    public async Task<ApplicationResult<SupplierStatementDto>> HandleAsync(
        GetSupplierStatementQuery query,
        CancellationToken cancellationToken = default)
    {
        var aggregate = await supplierRepository.GetByIdAsync(query.SupplierId, cancellationToken);
        if (aggregate is null)
            return ApplicationResult<SupplierStatementDto>.NotFound("Supplier not found.");

        var from = query.FromDate?.Date ?? new DateTime(DateTime.Today.Year, 1, 1);
        var to = query.ToDate?.Date ?? DateTime.Today;
        var accountId = aggregate.Supplier.PayablesAccountId;

        var opening = await accountingReports.GetLiabilityAccountBalanceBeforeAsync(accountId, from, cancellationToken);
        var ledger = await accountingReports.GetAccountLedgerAsync(accountId, from, to, cancellationToken);

        var running = opening;
        var lines = new List<SupplierStatementLineDto>();
        foreach (var row in ledger)
        {
            running += row.Credit - row.Debit;
            lines.Add(new SupplierStatementLineDto
            {
                EntryDate = row.EntryDate,
                DocumentType = row.SourceType ?? DocumentType.JournalEntry,
                DocumentNumber = row.EntryNumber,
                Description = string.IsNullOrWhiteSpace(row.LineNarrative) ? row.Description : row.LineNarrative,
                Debit = row.Debit,
                Credit = row.Credit,
                RunningBalance = running
            });
        }

        var totalDebit = lines.Sum(l => l.Debit);
        var totalCredit = lines.Sum(l => l.Credit);

        return ApplicationResult<SupplierStatementDto>.Success(new SupplierStatementDto
        {
            SupplierId = aggregate.Supplier.Id,
            SupplierName = aggregate.Supplier.NameAr,
            OpeningBalance = opening,
            TotalDebit = totalDebit,
            TotalCredit = totalCredit,
            ClosingBalance = running,
            PaymentTermsDays = aggregate.Supplier.PaymentTermsDays,
            CreditLimit = aggregate.Supplier.CreditLimit.Amount,
            PaymentTermsDisplay = SupplierPaymentTermsDisplay.Format(aggregate.Supplier.PaymentTermsDays),
            Lines = lines
        });
    }
}

public sealed class GetSupplierInvoiceListHandler(
    ISupplierRepository supplierRepository,
    IPurchaseInvoiceRepository purchaseRepository)
    : IQueryHandler<GetSupplierInvoiceListQuery, ApplicationResult<IReadOnlyList<SupplierInvoiceListDto>>>
{
    public async Task<ApplicationResult<IReadOnlyList<SupplierInvoiceListDto>>> HandleAsync(
        GetSupplierInvoiceListQuery query,
        CancellationToken cancellationToken = default)
    {
        var aggregate = await supplierRepository.GetByIdAsync(query.SupplierId, cancellationToken);
        if (aggregate is null)
            return ApplicationResult<IReadOnlyList<SupplierInvoiceListDto>>.NotFound("Supplier not found.");

        var invoices = await purchaseRepository.GetListAsync(
            aggregate.Supplier.CompanyId,
            supplierId: query.SupplierId,
            cancellationToken: cancellationToken);

        var rows = invoices
            .OrderByDescending(i => i.InvoiceDate)
            .Select(i => new SupplierInvoiceListDto
            {
                Id = i.Id,
                InvoiceNumber = i.InvoiceNumber,
                InvoiceDate = i.InvoiceDate,
                TotalAmount = i.TotalAmount.Amount,
                PaidAmount = i.TotalAmount.Amount - i.Remaining.Amount,
                RemainingAmount = i.Remaining.Amount,
                StatusDisplay = PurchaseDisplayExtensions.ToStatusDisplay(i.Status)
            })
            .ToList();

        return ApplicationResult<IReadOnlyList<SupplierInvoiceListDto>>.Success(rows);
    }
}

public sealed class GetSupplierOperationsCenterHandler(
    ISupplierRepository supplierRepository,
    IAccountRepository accountRepository,
    IPurchaseInvoiceRepository purchaseRepository,
    IPaymentVoucherRepository paymentRepository,
    IAccountingReportRepository accountingReports)
    : IQueryHandler<GetSupplierOperationsCenterQuery, ApplicationResult<SupplierOperationsCenterDto>>
{
    public async Task<ApplicationResult<SupplierOperationsCenterDto>> HandleAsync(
        GetSupplierOperationsCenterQuery query,
        CancellationToken cancellationToken = default)
    {
        var aggregate = await supplierRepository.GetByIdAsync(query.SupplierId, cancellationToken);
        if (aggregate is null)
            return ApplicationResult<SupplierOperationsCenterDto>.NotFound("Supplier not found.");

        var account = await accountRepository.GetByIdAsync(aggregate.Supplier.PayablesAccountId, cancellationToken);
        var details = SupplierMapper.ToDetailsDto(aggregate, account?.NameAr);

        var ytdStart = new DateTime(DateTime.Today.Year, 1, 1);
        var invoices = await purchaseRepository.GetListAsync(
            aggregate.Supplier.CompanyId,
            supplierId: query.SupplierId,
            cancellationToken: cancellationToken);
        var payments = await paymentRepository.GetListAsync(
            aggregate.Supplier.CompanyId,
            supplierId: query.SupplierId,
            cancellationToken: cancellationToken);

        var purchasesYtd = invoices
            .Where(i => i.InvoiceDate >= ytdStart && i.Status != PurchaseInvoiceStatus.Cancelled)
            .Sum(i => i.TotalAmount.Amount);

        var openInvoices = invoices.Count(i =>
            i.Status is PurchaseInvoiceStatus.Posted or PurchaseInvoiceStatus.Approved);

        var overdue = invoices
            .Where(i => i.Remaining.Amount > 0 &&
                        i.InvoiceDate.AddDays(aggregate.Supplier.PaymentTermsDays) < DateTime.Today)
            .Sum(i => i.Remaining.Amount);

        var ledger = await accountingReports.GetAccountLedgerAsync(
            aggregate.Supplier.PayablesAccountId,
            ytdStart,
            DateTime.Today,
            cancellationToken);

        return ApplicationResult<SupplierOperationsCenterDto>.Success(new SupplierOperationsCenterDto
        {
            Supplier = details,
            PurchasesYtd = purchasesYtd,
            OutstandingBalance = aggregate.Supplier.Balance.Amount,
            OverdueAmount = overdue,
            LastTransactionDate = ledger.LastOrDefault()?.EntryDate,
            OpenInvoicesCount = openInvoices,
            RecentInvoices = invoices
                .OrderByDescending(i => i.InvoiceDate)
                .Take(20)
                .Select(i => new SupplierInvoiceListDto
                {
                    Id = i.Id,
                    InvoiceNumber = i.InvoiceNumber,
                    InvoiceDate = i.InvoiceDate,
                    TotalAmount = i.TotalAmount.Amount,
                    PaidAmount = i.TotalAmount.Amount - i.Remaining.Amount,
                    RemainingAmount = i.Remaining.Amount,
                    StatusDisplay = i.Status.ToString()
                })
                .ToList(),
            RecentPayments = payments
                .OrderByDescending(p => p.VoucherDate)
                .Take(20)
                .Select(p => new SupplierPaymentListDto
                {
                    Id = p.Id,
                    VoucherNumber = p.VoucherNumber,
                    VoucherDate = p.VoucherDate,
                    Amount = p.Amount.Amount,
                    StatusDisplay = p.Status switch
                    {
                        VoucherStatus.Posted => "مرحّل",
                        VoucherStatus.Approved => "معتمد",
                        _ => "مسودة"
                    }
                })
                .ToList()
        });
    }
}
