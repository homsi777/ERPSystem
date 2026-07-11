using ERPSystem.Core;
using ERPSystem.Core.Customers;
using ERPSystem.Helpers;
using ERPSystem.Services;
using ERPSystem.Services.Accounting;
using ERPSystem.Services.Suppliers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ERPSystem.Diagnostics.Performance;

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
        var root = new Grid { Margin = new Thickness(16) };
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

        var title = ErpUiFactory.SectionTitle("الذمم المدينة — أعمار الديون");
        Grid.SetRow(title, 0);
        root.Children.Add(title);

        var banner = ErpUxFactory.InfoBanner("العملاء ذوو الأرصدة المستحقة — بيانات حية. انقر مزدوجاً لفتح مركز العميل.");
        banner.Margin = new Thickness(0, 8, 0, 0);
        Grid.SetRow(banner, 1);
        root.Children.Add(banner);

        _grid = new DataGrid
        {
            AutoGenerateColumns = false,
            IsReadOnly = true,
            CanUserAddRows = false,
            HeadersVisibility = DataGridHeadersVisibility.Column,
            Margin = new Thickness(0, 12, 0, 0),
            EnableRowVirtualization = true,
            VerticalAlignment = VerticalAlignment.Stretch
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
        Grid.SetRow(_grid, 2);
        root.Children.Add(_grid);

        Content = root;
        Loaded += async (_, _) => await LoadAsync();
    }

    private async Task LoadAsync()
    {
        using var perfScope = ScreenLoadProfiler.Begin("Accounting.ReceivablesAging");
        if (!AppServices.IsInitialized) return;

        var result = await ScreenLoadProfiler.MeasureLoadAsync(perfScope, () => AccountingUiService.Instance.GetReceivablesAgingAsync());
        perfScope?.IncrementServiceCalls();
        if (!result.IsSuccess || result.Value is null) return;

        var rows = result.Value.Select(dto => new ArAgingRow
        {
            Customer = CustomerListRow.FromDto(new Application.DTOs.Customers.CustomerListDto
            {
                Id = dto.CustomerId,
                Code = dto.CustomerCode,
                NameAr = dto.CustomerName,
                Balance = dto.Outstanding
            }),
            CustomerName = dto.CustomerName,
            TotalInvoiced = dto.TotalInvoiced,
            Collected = dto.Collected,
            Outstanding = dto.Outstanding,
            OldestInvoice = dto.OldestInvoiceDate?.ToString("yyyy/MM/dd") ?? "—",
            DaysOverdue = dto.DaysOverdue
        }).ToList();

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
        var root = new Grid { Margin = new Thickness(16) };
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

        var title = ErpUiFactory.SectionTitle("الذمم الدائنة — أعمار الديون");
        Grid.SetRow(title, 0);
        root.Children.Add(title);

        var banner = ErpUxFactory.InfoBanner("الموردون ذوو الأرصدة المستحقة — بيانات حية. انقر مزدوجاً لفتح كشف المورد.");
        banner.Margin = new Thickness(0, 8, 0, 0);
        Grid.SetRow(banner, 1);
        root.Children.Add(banner);

        _grid = new DataGrid
        {
            AutoGenerateColumns = false,
            IsReadOnly = true,
            CanUserAddRows = false,
            HeadersVisibility = DataGridHeadersVisibility.Column,
            Margin = new Thickness(0, 12, 0, 0),
            EnableRowVirtualization = true,
            VerticalAlignment = VerticalAlignment.Stretch
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
        Grid.SetRow(_grid, 2);
        root.Children.Add(_grid);

        Content = root;
        Loaded += async (_, _) => await LoadAsync();
    }

    private async Task LoadAsync()
    {
        using var perfScope = ScreenLoadProfiler.Begin("Accounting.PayablesAging");
        if (!AppServices.IsInitialized) return;

        var result = await ScreenLoadProfiler.MeasureLoadAsync(perfScope, () => AccountingUiService.Instance.GetPayablesAgingAsync());
        perfScope?.IncrementServiceCalls();
        if (!result.IsSuccess || result.Value is null) return;

        _grid.ItemsSource = result.Value.Select(dto => new ApAgingRow
        {
            SupplierId = dto.SupplierId,
            SupplierName = dto.SupplierName,
            TotalInvoiced = dto.TotalInvoiced,
            Paid = dto.Paid,
            Outstanding = dto.Outstanding,
            OldestInvoice = dto.OldestInvoiceDate?.ToString("yyyy/MM/dd") ?? "—",
            DaysOverdue = dto.DaysOverdue
        }).ToList();
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
