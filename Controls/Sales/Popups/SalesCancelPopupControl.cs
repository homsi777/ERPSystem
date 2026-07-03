using ERPSystem.Core.Sales;
using ERPSystem.Helpers;
using ERPSystem.Services.Sales;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ERPSystem.Controls.Sales.Popups;

public sealed class SalesCancelPopupControl : UserControl
{
    private readonly TextBox _reason;

    public string? Reason { get; private set; }

    public SalesCancelPopupControl(SalesInvoiceListRow row)
    {
        var stack = new StackPanel();
        stack.Children.Add(new TextBlock
        {
            Text = $"سيتم إلغاء الفاتورة {row.InvoiceNumber} وإطلاق أي أطباق محجوزة في المخزون.",
            FontSize = 12,
            TextWrapping = TextWrapping.Wrap,
            Foreground = (Brush)System.Windows.Application.Current.Resources["TextSecondaryBrush"],
            Margin = new Thickness(0, 0, 0, 12)
        });
        stack.Children.Add(new TextBlock
        {
            Text = "سبب الإلغاء",
            FontSize = 11,
            Foreground = (Brush)System.Windows.Application.Current.Resources["TextMutedBrush"],
            Margin = new Thickness(0, 0, 0, 4)
        });
        _reason = new TextBox
        {
            Height = 100,
            AcceptsReturn = true,
            TextWrapping = TextWrapping.Wrap,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto
        };
        stack.Children.Add(_reason);

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
            Content = "تأكيد الإلغاء",
            Width = 130,
            Height = 34,
            Foreground = Brushes.White,
            Background = (Brush)System.Windows.Application.Current.Resources["DangerBrush"],
            BorderThickness = new Thickness(0),
            FontWeight = FontWeights.SemiBold
        };
        btnOk.Click += (_, _) =>
        {
            if (string.IsNullOrWhiteSpace(_reason.Text))
            {
                MessageBox.Show("يجب إدخال سبب الإلغاء.", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            Reason = _reason.Text.Trim();
            if (Window.GetWindow(this) is Window w) { w.DialogResult = true; w.Close(); }
        };
        btnRow.Children.Add(btnOk);
        btnRow.Children.Add(btnCancel);
        stack.Children.Add(btnRow);

        Content = stack;
    }
}
