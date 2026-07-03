using ERPSystem.Application.DTOs.Suppliers;
using ERPSystem.Helpers;
using ERPSystem.Services;
using ERPSystem.Services.Suppliers;
using System.Windows;
using System.Windows.Controls;

namespace ERPSystem.Controls.Suppliers;

public sealed class SupplierInvoiceListControl : UserControl
{
    private readonly DataGrid _grid = new() { AutoGenerateColumns = false, IsReadOnly = true, MinHeight = 220 };
    private readonly Border _empty = new() { Visibility = Visibility.Collapsed };

    private Guid? _supplierId;

    public SupplierInvoiceListControl()
    {
        ErpDataGridHelper.ApplyEnterpriseStyle(_grid);
        ErpUiFactory.AddGridColumn(_grid, "رقم الفاتورة", nameof(SupplierInvoiceListDto.InvoiceNumber), 120, null);
        ErpUiFactory.AddGridColumn(_grid, "التاريخ", nameof(SupplierInvoiceListDto.InvoiceDate), 100, "yyyy/MM/dd");
        ErpUiFactory.AddGridColumn(_grid, "الإجمالي", nameof(SupplierInvoiceListDto.TotalAmount), 100, "N2");
        ErpUiFactory.AddGridColumn(_grid, "المدفوع", nameof(SupplierInvoiceListDto.PaidAmount), 100, "N2");
        ErpUiFactory.AddGridColumn(_grid, "المتبقي", nameof(SupplierInvoiceListDto.RemainingAmount), 100, "N2");
        ErpUiFactory.AddGridColumn(_grid, "الحالة", nameof(SupplierInvoiceListDto.StatusDisplay), 100, null);

        _empty.Child = PlaceholderUi.EmptyMessage("لا توجد فواتير لهذا المورد");

        var stack = new StackPanel { Margin = new Thickness(8) };
        stack.Children.Add(_grid);
        stack.Children.Add(_empty);

        Content = stack;
        Loaded += async (_, _) => await LoadAsync();
    }

    public void Initialize(Guid supplierId) => _supplierId = supplierId;

    private async Task LoadAsync()
    {
        if (_supplierId is not Guid id || !AppServices.IsInitialized)
        {
            _grid.Visibility = Visibility.Collapsed;
            _empty.Visibility = Visibility.Visible;
            return;
        }

        var result = await SupplierUiService.Instance.GetInvoicesAsync(id);
        if (!ApplicationResultPresenter.Present(result))
            return;

        var items = result.Value ?? [];
        if (items.Count == 0)
        {
            _grid.Visibility = Visibility.Collapsed;
            _empty.Visibility = Visibility.Visible;
            return;
        }

        _empty.Visibility = Visibility.Collapsed;
        _grid.Visibility = Visibility.Visible;
        _grid.ItemsSource = items;
    }
}
