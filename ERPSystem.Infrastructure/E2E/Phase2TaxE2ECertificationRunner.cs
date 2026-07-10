using ERPSystem.Application.Abstractions;
using ERPSystem.Application.Abstractions.Repositories;
using ERPSystem.Application.Commands.Sales;
using ERPSystem.Application.DTOs.Sales;
using ERPSystem.Application.Results;
using ERPSystem.Application.Sales;
using ERPSystem.Application.Services;
using ERPSystem.Application.UseCases.Sales;
using ERPSystem.Domain.Enums;
using ERPSystem.Infrastructure.Persistence;
using ERPSystem.Infrastructure.Persistence.Models.Accounting;
using ERPSystem.Infrastructure.Persistence.Models.Parties;
using ERPSystem.Infrastructure.Persistence.Models.Sales;
using ERPSystem.Infrastructure.Seed;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ERPSystem.Infrastructure.E2E;

public sealed class Phase2TaxE2ECertificationRunner(
    ErpDbContext context,
    ICommandHandler<CreateSalesInvoiceDraftCommand, ApplicationResult<Guid>> createDraft,
    ICommandHandler<SendSalesInvoiceToWarehouseCommand, ApplicationResult> sendToWarehouse,
    ICommandHandler<CompleteWarehouseDetailingCommand, ApplicationResult> completeDetailing,
    ICommandHandler<ApproveSalesInvoiceCommand, ApplicationResult> approveInvoice,
    ICommandHandler<CreateSalesReturnCommand, ApplicationResult<Guid>> createReturn,
    ICommandHandler<PostSalesReturnCommand, ApplicationResult> postReturn,
    ISalesInvoiceRepository invoiceRepository,
    ISalesTaxReportRepository taxReportRepository,
    IServiceScopeFactory scopeFactory)
{
    public string CurrentRunId { get; private set; } = "";

    public async Task<Phase2E2ERunResult> RunAllScenariosAsync(CancellationToken cancellationToken = default)
    {
        Phase2E2ETestCompanySeeder.GuardNotProduction(Phase2E2ETestCompanyIds.CompanyId);
        CurrentRunId = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
        var result = new Phase2E2ERunResult { RunId = CurrentRunId };

        result.ScenarioA = await RunExclusiveScenarioAsync(cancellationToken);
        result.ScenarioB = await RunInclusiveScenarioAsync(cancellationToken);
        result.ScenarioC = await RunInvoiceDiscountScenarioAsync(cancellationToken);
        result.ScenarioD = await RunMultiRateScenarioAsync(cancellationToken);
        result.ScenarioE = await RunPartialReturnScenarioAsync(result.ScenarioA.InvoiceId, cancellationToken);
        result.ScenarioF = await RunFullReturnScenarioAsync(cancellationToken);
        result.ScenarioG = await RunLegacyReadOnlyScenarioAsync(cancellationToken);
        result.Concurrency = await RunConcurrencyTestAsync(cancellationToken);
        result.Rollback = await RunRollbackTestAsync(cancellationToken);
        result.SnapshotImmutability = await RunSnapshotImmutabilityTestAsync(result.ScenarioA.InvoiceId, cancellationToken);
        result.CompanyIsolation = await RunCompanyIsolationTestAsync(cancellationToken);

        result.AllPassed = result.Scenarios.All(s => s.Passed)
                         && result.Concurrency.Passed;

        return result;
    }

    public async Task<Phase2E2EScenarioResult> RunExclusiveScenarioAsync(CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(CurrentRunId))
            CurrentRunId = DateTime.UtcNow.ToString("yyyyMMddHHmmss");

        var invoiceId = await CreateAndApproveAsync(
            $"E2E-TAX-{CurrentRunId}-A", Phase2E2ETestCompanyIds.Vat15Exclusive,
            10m, 100m, 0m, ct);

        return await VerifyApprovedInvoiceAsync(invoiceId, "ScenarioA-Exclusive", new ExpectedTotals
        {
            SubTotal = 1000m, TaxTotal = 150m, GrandTotal = 1150m,
            RevenueCredit = 1000m, VatCredit = 150m, ArDebit = 1150m, CogsDebit = 600m
        }, ct);
    }

    public async Task<Phase2E2EScenarioResult> RunInclusiveScenarioAsync(CancellationToken ct = default)
    {
        var invoiceId = await CreateAndApproveAsync(
            $"E2E-TAX-{CurrentRunId}-B", Phase2E2ETestCompanyIds.Vat15Inclusive,
            11.5m, 100m, 0m, ct);

        return await VerifyApprovedInvoiceAsync(invoiceId, "ScenarioB-Inclusive", new ExpectedTotals
        {
            TaxTotal = 150m, GrandTotal = 1150m, RevenueCredit = 1000m, VatCredit = 150m, ArDebit = 1150m
        }, ct);
    }

    public async Task<Phase2E2EScenarioResult> RunInvoiceDiscountScenarioAsync(CancellationToken ct = default)
    {
        var invoiceId = await CreateAndApproveAsync(
            $"E2E-TAX-{CurrentRunId}-C", Phase2E2ETestCompanyIds.Vat15Exclusive,
            10m, 100m, 100m, ct);

        return await VerifyApprovedInvoiceAsync(invoiceId, "ScenarioC-InvoiceDiscount", new ExpectedTotals
        {
            SubTotal = 1000m, TaxTotal = 135m, GrandTotal = 1035m,
            RevenueCredit = 900m, VatCredit = 135m, ArDebit = 1035m
        }, ct);
    }

    public async Task<Phase2E2EScenarioResult> RunMultiRateScenarioAsync(CancellationToken ct = default)
    {
        var invoiceNumber = $"E2E-TAX-{CurrentRunId}-D";
        var lineDefs = new[]
        {
            (1, Phase2E2ETestCompanyIds.Vat15Exclusive, 10m, 50m),
            (2, Phase2E2ETestCompanyIds.ZeroRated, 10m, 30m),
            (3, Phase2E2ETestCompanyIds.Exempt, 10m, 20m)
        };

        var createResult = await createDraft.HandleAsync(new CreateSalesInvoiceDraftCommand
        {
            CompanyId = Phase2E2ETestCompanyIds.CompanyId,
            BranchId = Phase2E2ETestCompanyIds.BranchId,
            InvoiceNumber = invoiceNumber,
            CustomerId = Phase2E2ETestCompanyIds.CustomerId,
            WarehouseId = Phase2E2ETestCompanyIds.WarehouseId,
            ChinaContainerId = Phase2E2ETestCompanyIds.ContainerId,
            PaymentType = PaymentType.Credit,
            Lines = lineDefs.Select(l => new SalesInvoiceLineCommand
            {
                LineNumber = l.Item1,
                ChinaContainerId = Phase2E2ETestCompanyIds.ContainerId,
                FabricItemId = Phase2E2ETestCompanyIds.FabricItemId,
                FabricColorId = Phase2E2ETestCompanyIds.FabricColorId,
                RollCount = 1,
                UnitPrice = l.Item3,
                OriginalUnitPrice = l.Item3,
                TaxCodeId = l.Item2,
                Notes = $"E2E|{CurrentRunId}|ScenarioD|L{l.Item1}"
            }).ToList()
        }, ct);

        if (!createResult.IsSuccess)
            return Fail("ScenarioD-MultiRate", createResult.ErrorMessage ?? "create failed");

        await SendDetailApproveAsync(createResult.Value, lineDefs.Select(l => l.Item4).ToArray(), ct);

        return await VerifyApprovedInvoiceAsync(createResult.Value, "ScenarioD-MultiRate", new ExpectedTotals
        {
            SubTotal = 1000m, TaxTotal = 75m, GrandTotal = 1075m,
            RevenueCredit = 1000m, VatCredit = 75m, ArDebit = 1075m
        }, ct);
    }

    public async Task<Phase2E2EScenarioResult> RunPartialReturnScenarioAsync(Guid sourceInvoiceId, CancellationToken ct = default)
    {
        if (sourceInvoiceId == Guid.Empty)
            return Fail("ScenarioE-PartialReturn", "Source invoice missing");

        var invoice = await invoiceRepository.GetByIdAsync(sourceInvoiceId, ct);
        if (invoice is null) return Fail("ScenarioE-PartialReturn", "Invoice not found");

        var item = invoice.Items.First();
        var originalMeters = invoice.RollDetails.Where(r => r.SalesInvoiceItemId == item.Id)
            .Sum(r => r.LengthMeters.Value);

        var createRet = await createReturn.HandleAsync(new CreateSalesReturnCommand
        {
            OriginalInvoiceId = sourceInvoiceId,
            ReturnDate = DateTime.UtcNow,
            Reason = SalesReturnReason.CustomerRequest,
            Notes = $"E2E|{CurrentRunId}|ScenarioE",
            Lines = [new SalesReturnLineCommand { LineNumber = 1, OriginalInvoiceItemId = item.Id, ReturnMeters = Math.Round(originalMeters * 0.4m, 2) }]
        }, ct);

        if (!createRet.IsSuccess)
            return Fail("ScenarioE-PartialReturn", createRet.ErrorMessage ?? "create return failed");

        var postRet = await postReturn.HandleAsync(new PostSalesReturnCommand { ReturnId = createRet.Value }, ct);
        return postRet.IsSuccess
            ? new Phase2E2EScenarioResult { Name = "ScenarioE-PartialReturn", Passed = true, InvoiceId = sourceInvoiceId, ReturnId = createRet.Value }
            : Fail("ScenarioE-PartialReturn", postRet.ErrorMessage ?? "post failed");
    }

    public async Task<Phase2E2EScenarioResult> RunFullReturnScenarioAsync(CancellationToken ct = default)
    {
        var invoiceId = await CreateAndApproveAsync(
            $"E2E-TAX-{CurrentRunId}-F", Phase2E2ETestCompanyIds.Vat15Exclusive, 10m, 50m, 0m, ct);

        var invoice = await invoiceRepository.GetByIdAsync(invoiceId, ct);
        var item = invoice!.Items.First();
        var meters = invoice.RollDetails.Where(r => r.SalesInvoiceItemId == item.Id).Sum(r => r.LengthMeters.Value);

        var createRet = await createReturn.HandleAsync(new CreateSalesReturnCommand
        {
            OriginalInvoiceId = invoiceId,
            ReturnDate = DateTime.UtcNow,
            Reason = SalesReturnReason.CustomerRequest,
            Notes = $"E2E|{CurrentRunId}|ScenarioF",
            Lines = [new SalesReturnLineCommand { LineNumber = 1, OriginalInvoiceItemId = item.Id, ReturnMeters = meters }]
        }, ct);

        if (!createRet.IsSuccess) return Fail("ScenarioF-FullReturn", createRet.ErrorMessage ?? "create failed");

        var postRet = await postReturn.HandleAsync(new PostSalesReturnCommand { ReturnId = createRet.Value }, ct);
        return postRet.IsSuccess
            ? new Phase2E2EScenarioResult { Name = "ScenarioF-FullReturn", Passed = true, InvoiceId = invoiceId, ReturnId = createRet.Value }
            : Fail("ScenarioF-FullReturn", postRet.ErrorMessage ?? "post failed");
    }

    public async Task<Phase2E2EScenarioResult> RunLegacyReadOnlyScenarioAsync(CancellationToken ct = default)
    {
        var legacy = await context.Set<SalesInvoiceEntity>().AsNoTracking()
            .Where(i => i.CompanyId == DatabaseSeeder.DefaultCompanyId && i.IsLegacyUntaxed)
            .OrderByDescending(i => context.JournalEntries.Any(j => j.SourceId == i.Id))
            .ThenByDescending(i => i.ApprovedAt)
            .FirstOrDefaultAsync(ct);

        if (legacy is null) return Fail("ScenarioG-Legacy", "No legacy invoice found");

        var taxCount = await context.Set<SalesInvoiceItemTaxEntity>()
            .CountAsync(t => t.SalesInvoiceId == legacy.Id, ct);

        var journalCount = await context.JournalEntries.AsNoTracking()
            .CountAsync(t => t.SourceId == legacy.Id, ct);

        return new Phase2E2EScenarioResult
        {
            Name = "ScenarioG-Legacy",
            Passed = legacy.IsLegacyUntaxed && legacy.TaxTotal == 0m && taxCount == 0 && journalCount > 0,
            InvoiceId = legacy.Id,
            Details = $"{legacy.InvoiceNumber} legacy={legacy.IsLegacyUntaxed} tax={legacy.TaxTotal} journals={journalCount}"
        };
    }

    public async Task<Phase2E2EConcurrencyResult> RunConcurrencyTestAsync(CancellationToken ct = default)
    {
        var invoiceId = await CreateDetailedOnlyAsync(
            $"E2E-TAX-{CurrentRunId}-CONC", Phase2E2ETestCompanyIds.Vat15Exclusive, 10m, 100m, ct);

        async Task<ApplicationResult> ApproveInIsolatedScopeAsync()
        {
            using var scope = scopeFactory.CreateScope();
            var handler = scope.ServiceProvider.GetRequiredService<
                ICommandHandler<ApproveSalesInvoiceCommand, ApplicationResult>>();
            return await handler.HandleAsync(
                new ApproveSalesInvoiceCommand { InvoiceId = invoiceId }, ct);
        }

        var results = await Task.WhenAll(
            Enumerable.Range(0, 20).Select(_ => ApproveInIsolatedScopeAsync()));

        var journalCount = await context.JournalEntries.AsNoTracking()
            .CountAsync(j => j.CompanyId == Phase2E2ETestCompanyIds.CompanyId
                             && j.SourceId == invoiceId
                             && j.PostingKind == (int)PostingKind.SalesInvoicePosting, ct);

        var snapshotCount = await context.Set<SalesInvoiceItemTaxEntity>()
            .CountAsync(t => t.SalesInvoiceId == invoiceId, ct);

        return new Phase2E2EConcurrencyResult
        {
            Passed = results.Any(r => r.IsSuccess) && journalCount == 1 && snapshotCount >= 1,
            ParallelRequests = 20,
            SuccessResponses = results.Count(r => r.IsSuccess),
            JournalEntryCount = journalCount,
            TaxSnapshotCount = snapshotCount
        };
    }

    public async Task<Phase2E2EScenarioResult> RunRollbackTestAsync(CancellationToken ct = default)
    {
        var invoiceId = await CreateDetailedOnlyAsync(
            $"E2E-TAX-{CurrentRunId}-RB", Phase2E2ETestCompanyIds.Vat15Exclusive, 10m, 100m, ct);

        var customer = await context.Customers.FirstAsync(
            c => c.Id == Phase2E2ETestCompanyIds.CustomerId, ct);
        var originalLimit = customer.CreditLimit;
        customer.CreditLimit = 50m;
        await context.SaveChangesAsync(ct);

        try
        {
            var approveResult = await approveInvoice.HandleAsync(
                new ApproveSalesInvoiceCommand { InvoiceId = invoiceId }, ct);

            var status = await context.Set<SalesInvoiceEntity>().AsNoTracking()
                .Where(i => i.Id == invoiceId).Select(i => i.Status).FirstAsync(ct);
            var journalCount = await context.JournalEntries.AsNoTracking()
                .CountAsync(j => j.SourceId == invoiceId, ct);

            return new Phase2E2EScenarioResult
            {
                Name = "Rollback-CreditLimit",
                Passed = !approveResult.IsSuccess && status == (int)SalesInvoiceStatus.Detailed && journalCount == 0,
                InvoiceId = invoiceId,
                Details = $"failed={!approveResult.IsSuccess} status={status} journals={journalCount}"
            };
        }
        finally
        {
            customer.CreditLimit = originalLimit;
            await context.SaveChangesAsync(ct);
        }
    }

    public async Task<Phase2E2EScenarioResult> RunSnapshotImmutabilityTestAsync(Guid invoiceId, CancellationToken ct = default)
    {
        if (invoiceId == Guid.Empty) return Fail("SnapshotImmutability", "No invoice");

        var before = await context.Set<SalesInvoiceEntity>().AsNoTracking()
            .FirstAsync(i => i.Id == invoiceId, ct);

        var taxCode = await context.TaxCodes.FirstAsync(
            t => t.Id == Phase2E2ETestCompanyIds.Vat15Exclusive, ct);
        taxCode.Rate = 0.20m;
        await context.SaveChangesAsync(ct);

        var after = await context.Set<SalesInvoiceEntity>().AsNoTracking()
            .FirstAsync(i => i.Id == invoiceId, ct);

        taxCode.Rate = 0.15m;
        await context.SaveChangesAsync(ct);

        return new Phase2E2EScenarioResult
        {
            Name = "SnapshotImmutability",
            Passed = before.TaxTotal == after.TaxTotal && before.GrandTotal == after.GrandTotal,
            InvoiceId = invoiceId,
            Details = $"TaxTotal {before.TaxTotal} -> {after.TaxTotal}"
        };
    }

    public async Task<Phase2E2EScenarioResult> RunCompanyIsolationTestAsync(CancellationToken ct = default)
    {
        var testInProd = await context.TaxCodes.AsNoTracking()
            .AnyAsync(t => t.CompanyId == DatabaseSeeder.DefaultCompanyId && t.Code.StartsWith("E2E-"), ct);
        var testCount = await context.TaxCodes.AsNoTracking()
            .CountAsync(t => t.CompanyId == Phase2E2ETestCompanyIds.CompanyId, ct);

        return new Phase2E2EScenarioResult
        {
            Name = "CompanyIsolation",
            Passed = !testInProd && testCount >= 4,
            Details = $"testCodes={testCount} leakedToProd={testInProd}"
        };
    }

    public async Task<CrossLayerProof> BuildCrossLayerProofAsync(Guid invoiceId, CancellationToken ct = default)
    {
        var invoice = await invoiceRepository.GetByIdAsync(invoiceId, ct);
        var db = await context.Set<SalesInvoiceEntity>().AsNoTracking()
            .FirstAsync(i => i.Id == invoiceId, ct);

        var journal = await context.JournalEntries.AsNoTracking()
            .FirstOrDefaultAsync(j => j.SourceId == invoiceId
                                      && j.PostingKind == (int)PostingKind.SalesInvoicePosting, ct);
        var journalLines = journal is null
            ? []
            : await context.JournalEntryLines.AsNoTracking()
                .Where(l => l.JournalEntryId == journal.Id).ToListAsync(ct);

        var report = await taxReportRepository.GetReportAsync(
            Phase2E2ETestCompanyIds.CompanyId,
            DateTime.UtcNow.AddYears(-1),
            DateTime.UtcNow.AddDays(1),
            ct);

        var dto = MapInvoiceDto(invoice!);
        var pdf = SalesInvoicePdfTotalsModel.Build(dto);

        return new CrossLayerProof
        {
            InvoiceId = invoiceId,
            InvoiceNumber = db.InvoiceNumber,
            DbGrandTotal = db.GrandTotal,
            DbTaxTotal = db.TaxTotal,
            PdfGrandTotal = pdf.GrandTotal,
            JournalArDebit = journalLines.Where(l => l.AccountId == Phase2E2ETestCompanyIds.AccountsReceivable).Sum(l => l.Debit),
            ReportTaxAmount = report.Rows.Where(r => r.InvoiceNumber == db.InvoiceNumber).Sum(r => r.TaxAmount),
            AllMatch = Math.Abs(db.GrandTotal - pdf.GrandTotal) < 0.01m
                        && Math.Abs(db.TaxTotal - pdf.TaxBreakdown.Sum(s => s.TaxAmount)) < 0.01m
        };
    }

    private async Task<Guid> CreateAndApproveAsync(
        string invoiceNumber, Guid taxCodeId, decimal unitPrice, decimal meters,
        decimal invoiceDiscount, CancellationToken ct)
    {
        var id = await CreateDraftAsync(invoiceNumber, taxCodeId, unitPrice, ct);
        await SendDetailApproveAsync(id, [meters], ct, invoiceDiscount);
        return id;
    }

    private async Task<Guid> CreateDraftAsync(string invoiceNumber, Guid taxCodeId, decimal unitPrice, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(CurrentRunId))
        {
            CurrentRunId = DateTime.UtcNow.ToString("yyyyMMddHHmmssfff");
            invoiceNumber = $"E2E-TAX-{CurrentRunId}-{invoiceNumber.Split('-').Last()}";
        }

        var result = await createDraft.HandleAsync(new CreateSalesInvoiceDraftCommand
        {
            CompanyId = Phase2E2ETestCompanyIds.CompanyId,
            BranchId = Phase2E2ETestCompanyIds.BranchId,
            InvoiceNumber = invoiceNumber,
            CustomerId = Phase2E2ETestCompanyIds.CustomerId,
            WarehouseId = Phase2E2ETestCompanyIds.WarehouseId,
            ChinaContainerId = Phase2E2ETestCompanyIds.ContainerId,
            PaymentType = PaymentType.Credit,
            Lines =
            [
                new SalesInvoiceLineCommand
                {
                    LineNumber = 1,
                    ChinaContainerId = Phase2E2ETestCompanyIds.ContainerId,
                    FabricItemId = Phase2E2ETestCompanyIds.FabricItemId,
                    FabricColorId = Phase2E2ETestCompanyIds.FabricColorId,
                    RollCount = 1,
                    UnitPrice = unitPrice,
                    OriginalUnitPrice = unitPrice,
                    TaxCodeId = taxCodeId,
                    Notes = $"E2E|{CurrentRunId}|draft"
                }
            ]
        }, ct);

        if (!result.IsSuccess)
            throw new InvalidOperationException($"Create draft failed: {result.ErrorMessage}");
        return result.Value;
    }

    private async Task<Guid> CreateDetailedOnlyAsync(
        string invoiceNumber, Guid taxCodeId, decimal unitPrice, decimal meters, CancellationToken ct)
    {
        var id = await CreateDraftAsync(invoiceNumber, taxCodeId, unitPrice, ct);
        await SendAndDetailAsync(id, [meters], ct);
        return id;
    }

    private async Task SendDetailApproveAsync(Guid invoiceId, decimal[] meters, CancellationToken ct, decimal discount = 0m)
    {
        await SendAndDetailAsync(invoiceId, meters, ct);
        if (discount > 0)
        {
            var inv = await invoiceRepository.GetByIdAsync(invoiceId, ct);
            inv!.SetDiscountTotal(new Domain.ValueObjects.Money(discount));
            await invoiceRepository.UpdateAsync(inv, ct);
            await context.SaveChangesAsync(ct);
        }

        var approve = await approveInvoice.HandleAsync(new ApproveSalesInvoiceCommand { InvoiceId = invoiceId }, ct);
        if (!approve.IsSuccess)
            throw new InvalidOperationException($"Approve failed: {approve.ErrorMessage}");
    }

    private async Task SendAndDetailAsync(Guid invoiceId, decimal[] meters, CancellationToken ct)
    {
        var send = await sendToWarehouse.HandleAsync(new SendSalesInvoiceToWarehouseCommand { InvoiceId = invoiceId }, ct);
        if (!send.IsSuccess)
            throw new InvalidOperationException($"Send failed: {send.ErrorMessage}");

        var invoice = await invoiceRepository.GetByIdAsync(invoiceId, ct);
        var items = invoice!.Items.OrderBy(i => i.LineNumber).ToList();
        if (items.Count != meters.Length)
            throw new InvalidOperationException(
                $"Detailing input count {meters.Length} does not match invoice line count {items.Count}.");

        var entries = items.Select((item, i) =>
        {
            var roll = invoice.RollDetails
                .Where(r => r.SalesInvoiceItemId == item.Id)
                .OrderBy(r => r.RollSequence.Value)
                .First();
            return new RollLengthEntryCommand
            {
                RollDetailId = roll.Id,
                LengthMeters = meters[i]
            };
        }).ToList();

        var detail = await completeDetailing.HandleAsync(new CompleteWarehouseDetailingCommand
        {
            InvoiceId = invoiceId,
            RollEntries = entries
        }, ct);

        if (!detail.IsSuccess)
            throw new InvalidOperationException($"Detailing failed: {detail.ErrorMessage}");
    }

    private async Task<Phase2E2EScenarioResult> VerifyApprovedInvoiceAsync(
        Guid invoiceId, string name, ExpectedTotals expected, CancellationToken ct)
    {
        var db = await context.Set<SalesInvoiceEntity>().AsNoTracking()
            .FirstAsync(i => i.Id == invoiceId, ct);

        var journal = await context.JournalEntries.AsNoTracking()
            .FirstOrDefaultAsync(j => j.SourceId == invoiceId
                                      && j.PostingKind == (int)PostingKind.SalesInvoicePosting, ct);
        var lines = journal is null ? new List<JournalEntryLineEntity>() :
            await context.JournalEntryLines.AsNoTracking()
                .Where(l => l.JournalEntryId == journal.Id).ToListAsync(ct);

        var errors = new List<string>();
        if (expected.SubTotal > 0 && Math.Abs(db.SubTotal - expected.SubTotal) > 0.01m)
            errors.Add($"SubTotal {db.SubTotal}!={expected.SubTotal}");
        if (Math.Abs(db.TaxTotal - expected.TaxTotal) > 0.01m)
            errors.Add($"TaxTotal {db.TaxTotal}!={expected.TaxTotal}");
        if (Math.Abs(db.GrandTotal - expected.GrandTotal) > 0.01m)
            errors.Add($"GrandTotal {db.GrandTotal}!={expected.GrandTotal}");

        if (expected.ArDebit > 0)
        {
            var ar = lines.Where(l => l.AccountId == Phase2E2ETestCompanyIds.AccountsReceivable).Sum(l => l.Debit);
            if (Math.Abs(ar - expected.ArDebit) > 0.01m) errors.Add($"AR {ar}!={expected.ArDebit}");
        }
        if (expected.VatCredit > 0)
        {
            var vat = lines.Where(l => l.AccountId == Phase2E2ETestCompanyIds.VatPayable).Sum(l => l.Credit);
            if (Math.Abs(vat - expected.VatCredit) > 0.01m) errors.Add($"VAT {vat}!={expected.VatCredit}");
        }
        if (expected.RevenueCredit > 0)
        {
            var rev = lines.Where(l => l.AccountId == Phase2E2ETestCompanyIds.SalesRevenue).Sum(l => l.Credit);
            if (Math.Abs(rev - expected.RevenueCredit) > 0.01m) errors.Add($"Revenue {rev}!={expected.RevenueCredit}");
        }
        if (expected.CogsDebit > 0)
        {
            var cogs = lines.Where(l => l.AccountId == Phase2E2ETestCompanyIds.CostOfGoodsSold).Sum(l => l.Debit);
            if (Math.Abs(cogs - expected.CogsDebit) > 0.01m) errors.Add($"COGS {cogs}!={expected.CogsDebit}");
        }

        return new Phase2E2EScenarioResult
        {
            Name = name, Passed = errors.Count == 0, InvoiceId = invoiceId,
            Details = errors.Count == 0 ? "OK" : string.Join("; ", errors)
        };
    }

    private static Phase2E2EScenarioResult Fail(string name, string detail) =>
        new() { Name = name, Passed = false, Details = detail };

    private static SalesInvoiceDto MapInvoiceDto(Domain.Aggregates.SalesInvoiceAggregate invoice)
    {
        return new SalesInvoiceDto
        {
            SubTotal = invoice.SubTotal.Amount,
            DiscountTotal = invoice.DiscountTotal.Amount,
            TaxTotal = invoice.TaxTotal.Amount,
            GrandTotal = invoice.GrandTotal.Amount,
            IsLegacyUntaxed = invoice.IsLegacyUntaxed,
            Lines = invoice.Items.Select(i =>
            {
                var snap = invoice.ItemTaxSnapshots.FirstOrDefault(s => s.SalesInvoiceItemId == i.Id);
                return new SalesInvoiceLineDto
                {
                    LineTotal = i.LineTotal.Amount,
                    TaxCode = snap?.TaxCode,
                    TaxRate = snap?.TaxRate ?? 0m,
                    TaxableAmount = snap?.TaxableAmount.Amount ?? 0m,
                    TaxAmount = snap?.TaxAmount.Amount ?? 0m
                };
            }).ToList()
        };
    }
}

public sealed class ExpectedTotals
{
    public decimal SubTotal { get; init; }
    public decimal TaxTotal { get; init; }
    public decimal GrandTotal { get; init; }
    public decimal ArDebit { get; init; }
    public decimal RevenueCredit { get; init; }
    public decimal VatCredit { get; init; }
    public decimal CogsDebit { get; init; }
}

public sealed class Phase2E2EScenarioResult
{
    public string Name { get; init; } = "";
    public bool Passed { get; init; }
    public Guid InvoiceId { get; init; }
    public Guid ReturnId { get; init; }
    public string Details { get; init; } = "";
}

public sealed class Phase2E2EConcurrencyResult
{
    public bool Passed { get; init; }
    public int ParallelRequests { get; init; }
    public int SuccessResponses { get; init; }
    public int JournalEntryCount { get; init; }
    public int TaxSnapshotCount { get; init; }
}

public sealed class Phase2E2ERunResult
{
    public string RunId { get; set; } = "";
    public bool AllPassed { get; set; }
    public Phase2E2EScenarioResult ScenarioA { get; set; } = new();
    public Phase2E2EScenarioResult ScenarioB { get; set; } = new();
    public Phase2E2EScenarioResult ScenarioC { get; set; } = new();
    public Phase2E2EScenarioResult ScenarioD { get; set; } = new();
    public Phase2E2EScenarioResult ScenarioE { get; set; } = new();
    public Phase2E2EScenarioResult ScenarioF { get; set; } = new();
    public Phase2E2EScenarioResult ScenarioG { get; set; } = new();
    public Phase2E2EConcurrencyResult Concurrency { get; set; } = new();
    public Phase2E2EScenarioResult Rollback { get; set; } = new();
    public Phase2E2EScenarioResult SnapshotImmutability { get; set; } = new();
    public Phase2E2EScenarioResult CompanyIsolation { get; set; } = new();
    public IReadOnlyList<Phase2E2EScenarioResult> Scenarios =>
        [ScenarioA, ScenarioB, ScenarioC, ScenarioD, ScenarioE, ScenarioF, ScenarioG, Rollback, SnapshotImmutability, CompanyIsolation];
}

public sealed class CrossLayerProof
{
    public Guid InvoiceId { get; init; }
    public string InvoiceNumber { get; init; } = "";
    public decimal DbGrandTotal { get; init; }
    public decimal DbTaxTotal { get; init; }
    public decimal PdfGrandTotal { get; init; }
    public decimal JournalArDebit { get; init; }
    public decimal ReportTaxAmount { get; init; }
    public bool AllMatch { get; init; }
}
