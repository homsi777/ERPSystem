using Npgsql;

namespace ERPSystem.Application.Tests.UseCases.Sales;

public sealed class TempDetailingDiagnosticTests
{
    private const string ConnectionString =
        "Host=localhost;Port=5432;Database=erp_pro;Username=postgres;Password=12345678";

    [Fact(Skip = "Diagnostic dump only — not part of certification suite.")]
    public async Task Dump_awaiting_detailing_invoices_and_rolls()
    {
        await using var conn = new NpgsqlConnection(ConnectionString);
        await conn.OpenAsync();

        var sb = new System.Text.StringBuilder();

        await using (var cmd = new NpgsqlCommand(
            """
            SELECT i."Id", i."InvoiceNumber", i."Status", i."WarehouseId", i."SentToWarehouseAt"
            FROM sales.sales_invoices i
            WHERE i."Status" = 1
            """, conn))
        await using (var reader = await cmd.ExecuteReaderAsync())
        {
            while (await reader.ReadAsync())
            {
                sb.AppendLine($"Invoice {reader.GetGuid(0)} Number={reader.GetString(1)} Status={reader.GetInt32(2)} WarehouseId={reader.GetGuid(3)} SentAt={reader.GetFieldValue<DateTime?>(4)}");
            }
        }

        await using (var cmd2 = new NpgsqlCommand(
            """
            SELECT i."Id", i."InvoiceNumber", i."Status", COUNT(r."Id") AS RollCount,
                   SUM(CASE WHEN r."IsActive" THEN 1 ELSE 0 END) AS ActiveRolls
            FROM sales.sales_invoices i
            LEFT JOIN sales.sales_invoice_roll_details r ON r."SalesInvoiceId" = i."Id"
            WHERE i."Status" IN (1,2)
            GROUP BY i."Id", i."InvoiceNumber", i."Status"
            ORDER BY i."Status"
            """, conn))
        await using (var reader2 = await cmd2.ExecuteReaderAsync())
        {
            sb.AppendLine("--- roll counts ---");
            while (await reader2.ReadAsync())
            {
                sb.AppendLine($"Invoice {reader2.GetGuid(0)} Number={reader2.GetString(1)} Status={reader2.GetInt32(2)} RollCount={reader2.GetInt64(3)} ActiveRolls={reader2.GetInt64(4)}");
            }
        }

        await using (var cmd3 = new NpgsqlCommand(
            """
            SELECT r."Id", r."SalesInvoiceId", r."RollSequence", r."LengthMeters", r."IsActive"
            FROM sales.sales_invoice_roll_details r
            JOIN sales.sales_invoices i ON i."Id" = r."SalesInvoiceId"
            WHERE i."Status" IN (1,2)
            ORDER BY r."SalesInvoiceId", r."RollSequence"
            LIMIT 20
            """, conn))
        await using (var reader3 = await cmd3.ExecuteReaderAsync())
        {
            sb.AppendLine("--- sample rolls ---");
            while (await reader3.ReadAsync())
            {
                sb.AppendLine($"Roll {reader3.GetGuid(0)} InvoiceId={reader3.GetGuid(1)} Seq={reader3.GetInt32(2)} Length={reader3.GetDecimal(3)} Active={reader3.GetBoolean(4)}");
            }
        }

        Assert.Fail(sb.ToString());
    }
}
