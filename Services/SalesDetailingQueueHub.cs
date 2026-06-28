using ERPSystem.Views.Sales;

namespace ERPSystem.Services;

/// <summary>In-memory queue for invoices sent to warehouse detailing (until Sales vertical slice is wired).</summary>
public static class SalesDetailingQueueHub
{
    private static readonly List<FabricSalesInvoiceRow> _queue = new();
    private static readonly object _lock = new();

    public static void Enqueue(FabricSalesInvoiceRow row)
    {
        lock (_lock)
        {
            var existing = _queue.FindIndex(i =>
                i.InvoiceNumber.Equals(row.InvoiceNumber, StringComparison.OrdinalIgnoreCase));
            if (existing >= 0)
                _queue[existing] = row;
            else
                _queue.Insert(0, row);
        }
    }

    public static IReadOnlyList<FabricSalesInvoiceRow> GetAwaiting()
    {
        lock (_lock)
            return _queue
                .Where(i => i.WorkflowStatus == FabricInvoiceWorkflowStatus.AwaitingDetailing)
                .ToList();
    }

    public static FabricSalesInvoiceRow? Find(string? invoiceNumber)
    {
        if (string.IsNullOrWhiteSpace(invoiceNumber))
            return null;

        lock (_lock)
            return _queue.FirstOrDefault(i =>
                i.InvoiceNumber.Equals(invoiceNumber, StringComparison.OrdinalIgnoreCase));
    }

    public static FabricSalesInvoiceRow CreateFallback(string invoiceNumber) => new()
    {
        InvoiceNumber = invoiceNumber,
        CustomerName = "—",
        Container = "—",
        RollCount = 5,
        Date = DateTime.Today,
        WorkflowStatus = FabricInvoiceWorkflowStatus.AwaitingDetailing
    };
}
