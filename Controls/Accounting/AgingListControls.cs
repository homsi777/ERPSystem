using ERPSystem.Core;
using ERPSystem.Core.Customers;
using ERPSystem.Domain.Enums;
using ERPSystem.Helpers;
using ERPSystem.Services;
using ERPSystem.Services.Customers;
using ERPSystem.Services.Purchases;
using ERPSystem.Services.Sales;
using ERPSystem.Services.Suppliers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ERPSystem.Controls.Accounting;

/// <summary>
/// Read-only Accounts Receivable aging list — customers with an outstanding
/// balance, aged against their posted sales invoices. Live PostgreSQL data.
/// </summary>
public sealed class ReceivablesAgingControl : UserControl
{
    private readonly DataGrid _grid;

    public ReceivablesAgingControl()
    {
        var root = new ScrollViewer { Padding = new Thickness(16), VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        var stack = new StackPanel();
        stack.Children.Add(ErpUiFactory.SectionTitle("الذمم المدينة — أعمار الديون"));
        stack.Children.Add(ErpUxFactory.InfoBanner("العملاء ذوو الأرصدة المستحقة — بيانات حية. انقر مزدوجاً لفتح مركز العميل."));

        _grid = new DataGrid
        {
            AutoGenerateColumns = false,
            IsReadOnly = true,
            CanUserAddRows = false,
            HeadersVisibility = DataGridHeadersVisibility.Column,
            Margin = new Thickness(0, 12, 0, 0),
            MaxHeight = 620
        };
        ErpUiFactory.AddGridColumn(_grid, "العميل", nameof(ArAgingRow.CustomerName), "*", null);
        ErpUiFactory.AddGridColumn(_grid, "إجمالي الفواتير", nameof(ArAgingRow.TotalInvoiced), 130, "N2");
        ErpUiFactory.AddGridColumn(_grid, "المُحصّل", nameof(ArAgingRow.Collected), 120, "N2");
        ErpUiFactory.AddGridColumn(_grid, "المتبقي", nameof(ArAgingRow.Outstanding), 120, "N2");
        ErpUiFactory.AddGridColumn(_grid, "أقدم فاتورة", nameof(ArAgingRow.OldestInvoice), 120, null);
        ErpUiFactory.AddGridColumn(_grid, "أيام التأخير", nameof(ArAgingRow.DaysOverdue), 100, null);
        _grid.MouseDoubleClick += (_, _) =>
        {
            if (_grid.SelectedItem is ArAgingRow row)
                MockInteractionService.OpenCustomerOperationsCenter(row.Customer);
        };
        stack.Children.Add(_grid);

        root.Content = stack;
        Content = root;
        Loaded += async (_, _) => await LoadAsync();
    }

    private async Task LoadAsync()
    {
        if (!AppServices.IsInitialized) return;

        var customersResult = await CustomerUiService.Instance.GetListAsync(null, 1, 1000);
        var invoicesResult = await SalesUiService.Instance.GetListAsync(null, null, 1, 5000);
        if (!customersResult.IsSuccess || customersResult.Value is null) return;

        var invoices = invoicesResult.Value?.Items ?? [];
        var byCustomer = invoices
            .Where(i => i.Status >= SalesInvoiceStatus.AwaitingDetailing && i.Status != SalesInvoiceStatus.Cancelled)
            .GroupBy(i => i.CustomerId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var rows = new List<ArAgingRow>();
        foreach (var dto in customersResult.Value.Items.Where(c => c.Balance > 0).OrderByDescending(c => c.Balance))
        {
            byCustomer.TryGetValue(dto.Id, out var custInvoices);
            var totalInvoiced = custInvoices?.Sum(i => i.GrandTotal) ?? 0m;
            var oldest = custInvoices is { Count: > 0 } ? custInvoices.Min(i => i.InvoiceDate) : (DateTime?)null;
            var collected = Math.Max(0m, totalInvoiced - dto.Balance);
            rows.Add(new ArAgingRow
            {
                Customer = CustomerListRow.FromDto(dto),
                CustomerName = dto.NameAr,
                TotalInvoiced = totalInvoiced,
                Collected = collected,
                Outstanding = dto.Balance,
                OldestInvoice = oldest?.ToString("yyyy/MM/dd") ?? "—",
                DaysOverdue = oldest.HasValue ? Math.Max(0, (int)(DateTime.UtcNow - oldest.Value).TotalDays) : 0
            });
        }

        _grid.ItemsSource = rows;
    }

    private sealed class ArAgingRow
    {
        public CustomerListRow Customer { get; init; } = null!;
        public string CustomerName { get; init; } = "";
        public decimal TotalInvoiced { get; init; }
        public decimal Collected { get; init; }
        public decimal Outstanding { get; init; }
        public string OldestInvoice { get; init; } = "";
        public int DaysOverdue { get; init; }
    }
}

/// <summary>
/// Read-only Accounts Payable aging list — suppliers with outstanding purchase
/// invoice balances. Live PostgreSQL data.
/// </summary>
public sealed class PayablesAgingControl : UserControl
{
    private readonly DataGrid _grid;

    public PayablesAgingControl()
    {
        var root = new ScrollViewer { Padding = new Thickness(16), VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        var stack = new StackPanel();
        stack.Children.Add(ErpUiFactory.SectionTitle("الذمم الدائنة — أعمار الديون"));
        stack.Children.Add(ErpUxFactory.InfoBanner("الموردون ذوو الأرصدة المستحقة — بيانات حية. انقر مزدوجاً لفتح كشف المورد."));

        _grid = new DataGrid
        {
            AutoGenerateColumns = false,
            IsReadOnly = true,
            CanUserAddRows = false,
            HeadersVisibility = DataGridHeadersVisibility.Column,
            Margin = new Thickness(0, 12, 0, 0),
            MaxHeight = 620
        };
        ErpUiFactory.AddGridColumn(_grid, "المورد", nameof(ApAgingRow.SupplierName), "*", null);
        ErpUiFactory.AddGridColumn(_grid, "إجمالي الفواتير", nameof(ApAgingRow.TotalInvoiced), 130, "N2");
        ErpUiFactory.AddGridColumn(_grid, "المدفوع", nameof(ApAgingRow.Paid), 120, "N2");
        ErpUiFactory.AddGridColumn(_grid, "المتبقي", nameof(ApAgingRow.Outstanding), 120, "N2");
        ErpUiFactory.AddGridColumn(_grid, "أقدم فاتورة", nameof(ApAgingRow.OldestInvoice), 120, null);
        ErpUiFactory.AddGridColumn(_grid, "أيام التأخير", nameof(ApAgingRow.DaysOverdue), 100, null);
        _grid.MouseDoubleClick += (_, _) =>
        {
            if (_grid.SelectedItem is ApAgingRow row)
            {
                SupplierNavigationContext.BeginStatement(row.SupplierId, row.SupplierName);
                MockInteractionService.Navigate(AppModule.Suppliers, "Statement");
            }
        };
        stack.Children.Add(_grid);

        root.Content = stack;
        Content = root;
        Loaded += async (_, _) => await LoadAsync();
    }

    private async Task LoadAsync()
    {
        if (!AppServices.IsInitialized) return;

        var result = await PurchaseUiService.Instance.GetInvoiceListAsync(null);
        if (!result.IsSuccess || result.Value is null) return;

        var rows = result.Value
            .Where(i => i.Status != PurchaseInvoiceStatus.Cancelled)
            .GroupBy(i => i.SupplierId)
            .Select(g =>
            {
                var outstanding = g.Sum(i => i.RemainingAmount);
                var unpaid = g.Where(i => i.RemainingAmount > 0).ToList();
                var oldest = unpaid.Count > 0 ? unpaid.Min(i => i.InvoiceDate) : (DateTime?)null;
                return new ApAgingRow
                {
                    SupplierId = g.Key,
                    SupplierName = g.First().SupplierName,
                    TotalInvoiced = g.Sum(i => i.TotalAmount),
                    Paid = g.Sum(i => i.PaidAmount),
                    Outstanding = outstanding,
                    OldestInvoice = oldest?.ToString("yyyy/MM/dd") ?? "—",
                    DaysOverdue = oldest.HasValue ? Math.Max(0, (int)(DateTime.UtcNow - oldest.Value).TotalDays) : 0
                };
            })
            .Where(r => r.Outstanding > 0)
            .OrderByDescending(r => r.Outstanding)
            .ToList();

        _grid.ItemsSource = rows;
    }

    private sealed class ApAgingRow
    {
        public Guid SupplierId { get; init; }
        public string SupplierName { get; init; } = "";
        public decimal TotalInvoiced { get; init; }
        public decimal Paid { get; init; }
        public decimal Outstanding { get; init; }
        public string OldestInvoice { get; init; } = "";
        public int DaysOverdue { get; init; }
    }
}
