using ERPSystem.Application.DTOs.Warehouses;
using ERPSystem.Core.Sales;
using ERPSystem.Services.Sales;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ERPSystem.Controls.Sales.Popups;

public sealed class SalesWarehousePickerPopupControl : UserControl
{
    private readonly ComboBox _combo;

    public Guid SelectedWarehouseId => _combo.SelectedItem is WarehousePickItem it ? it.Id : Guid.Empty;

    public SalesWarehousePickerPopupControl(IEnumerable<WarehouseListDto> warehouses, SalesInvoiceListRow row)
    {
        var items = warehouses
            .Select(w => new WarehousePickItem { Id = w.Id, Display = w.NameAr })
            .ToList();

        var stack = new StackPanel();
        stack.Children.Add(new TextBlock
        {
            Text = $"اختر المستودع الذي سيتم إرسال الفاتورة {row.InvoiceNumber} إليه للتفصيل:",
            FontSize = 12,
            TextWrapping = TextWrapping.Wrap,
            Foreground = (Brush)System.Windows.Application.Current.Resources["TextSecondaryBrush"],
            Margin = new Thickness(0, 0, 0, 12)
        });

        _combo = new ComboBox
        {
            ItemsSource = items,
            DisplayMemberPath = nameof(WarehousePickItem.Display),
            Height = 34,
            Padding = new Thickness(8, 0, 8, 0)
        };
        if (items.Count > 0) _combo.SelectedIndex = 0;
        stack.Children.Add(_combo);

        var btnRow = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Left,
            Margin = new Thickness(0, 16, 0, 0)
        };
        var btnCancel = new Button
        {
            Content = "تراجع",
            Width = 90,
            Height = 34,
            Margin = new Thickness(0, 0, 8, 0),
            Style = (Style)System.Windows.Application.Current.Resources["GhostButtonStyle"]
        };
        btnCancel.Click += (_, _) => SalesPopupService.CancelActive();

        var btnOk = new Button
        {
            Content = "متابعة",
            Width = 100,
            Height = 34,
            Style = (Style)System.Windows.Application.Current.Resources["PrimaryButtonStyle"]
        };
        btnOk.Click += (_, _) =>
        {
            if (SelectedWarehouseId == Guid.Empty)
            {
                MessageBox.Show("اختر مستودعاً أولاً.", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (Window.GetWindow(this) is Window w) { w.DialogResult = true; w.Close(); }
        };
        btnRow.Children.Add(btnOk);
        btnRow.Children.Add(btnCancel);
        stack.Children.Add(btnRow);

        Content = stack;
    }

    public sealed class WarehousePickItem
    {
        public Guid Id { get; init; }
        public string Display { get; init; } = "";
    }
}
