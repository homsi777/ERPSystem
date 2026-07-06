using ERPSystem.Application.DependencyInjection;
using ERPSystem.Application.Queries.Customers;
using ERPSystem.Application.UseCases.Queries;
using ERPSystem.Infrastructure.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace ERPSystem.Application.Tests.UseCases.Customers;

/// <summary>
/// Optional live DB check — skipped automatically when PostgreSQL is unavailable.
/// Uses existing development data only; does not seed or mutate data.
/// </summary>
public sealed class CustomerAccountLedgerLiveDbVerificationTests
{
    private const string ConnectionString =
        "Host=localhost;Port=5432;Database=erp_pro;Username=postgres;Password=12345678";

    [Fact]
    public async Task Live_db_legacy_and_new_totals_align_when_returns_accounted()
    {
        if (!await CanConnectAsync())
            return;

        var customerId = await FindSampleCustomerIdAsync();
        if (customerId is null)
            return;

        var from = DateTime.Today.AddYears(-2);
        var to = DateTime.Today;

        await using var provider = BuildServiceProvider();
        var legacyHandler = provider.GetRequiredService<GetCustomerStatementHandler>();
        var ledgerHandler = provider.GetRequiredService<GetCustomerAccountLedgerHandler>();

        var legacy = await legacyHandler.HandleAsync(new GetCustomerStatementQuery
        {
            CustomerId = customerId.Value,
            FromDate = from,
            ToDate = to
        });
        var ledger = await ledgerHandler.HandleAsync(new GetCustomerAccountLedgerQuery
        {
            CustomerId = customerId.Value,
            FromDate = from,
            ToDate = to
        });

        Assert.True(legacy.IsSuccess);
        Assert.True(ledger.IsSuccess);

        var legacyDebit = legacy.Value!.Lines.Sum(l => l.Debit);
        var legacyCredit = legacy.Value.Lines.Sum(l => l.Credit);
        var newInvoiceTotal = ledger.Value!.Lines
            .Where(l => l.MovementType == Domain.Enums.CustomerAccountMovementType.SalesInvoice)
            .Sum(l => l.LineAmount);
        var newReceiptTotal = ledger.Value.Lines
            .Where(l => l.MovementType == Domain.Enums.CustomerAccountMovementType.ReceiptVoucher)
            .Sum(l => -l.LineAmount);
        var newReturnTotal = ledger.Value.Lines
            .Where(l => l.MovementType == Domain.Enums.CustomerAccountMovementType.SalesReturn)
            .Sum(l => l.LineAmount);

        Assert.Equal(legacyDebit, newInvoiceTotal);
        Assert.Equal(legacyCredit, newReceiptTotal);
        Assert.Equal(
            legacy.Value.ClosingBalance,
            ledger.Value.ClosingBalance + newReturnTotal);

        var running = ledger.Value.OpeningBalance;
        foreach (var line in ledger.Value.Lines)
        {
            running += line.MovementType switch
            {
                Domain.Enums.CustomerAccountMovementType.SalesInvoice => line.LineAmount,
                Domain.Enums.CustomerAccountMovementType.SalesReturn => -line.LineAmount,
                Domain.Enums.CustomerAccountMovementType.ReceiptVoucher => line.LineAmount,
                _ => 0m
            };
            Assert.Equal(running, line.RunningBalance);
        }
    }

    private static ServiceProvider BuildServiceProvider()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = ConnectionString
            })
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(config);
        services.AddInfrastructure(config);
        services.AddApplication();
        return services.BuildServiceProvider();
    }

    private static async Task<bool> CanConnectAsync()
    {
        try
        {
            await using var conn = new NpgsqlConnection(ConnectionString);
            await conn.OpenAsync();
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static async Task<Guid?> FindSampleCustomerIdAsync()
    {
        await using var conn = new NpgsqlConnection(ConnectionString);
        await conn.OpenAsync();

        await using var cmd = new NpgsqlCommand("""
            SELECT c."Id"
            FROM parties.customers c
            WHERE EXISTS (
                SELECT 1 FROM sales.sales_invoices i
                WHERE i."CustomerId" = c."Id" AND i."Status" >= 4
            )
            AND (
                EXISTS (SELECT 1 FROM sales.sales_returns r WHERE r."CustomerId" = c."Id" AND r."Status" = 2)
                OR EXISTS (SELECT 1 FROM finance.receipt_vouchers rv WHERE rv."CustomerId" = c."Id" AND rv."Status" = 2)
            )
            LIMIT 1
            """, conn);

        var result = await cmd.ExecuteScalarAsync();
        return result is Guid id ? id : null;
    }
}
