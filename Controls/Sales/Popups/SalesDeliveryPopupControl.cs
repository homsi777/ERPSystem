using ERPSystem.Core.Sales;
using ERPSystem.Services;
using ERPSystem.Services.Sales;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ERPSystem.Controls.Sales.Popups;

public sealed class SalesDeliveryPopupControl : UserControl
{
    private readonly SalesInvoiceListRow _row;
    private readonly DatePicker _dpDate = new() { SelectedDate = DateTime.Today, Height = 32 };
    private readonly TextBox _txtReceived = new() { Height = 32 };
    private readonly TextBox _txtDriver = new() { Height = 32 };
    private readonly TextBox _txtNotes = new()
    {
        Height = 80,
        AcceptsReturn = true,
        TextWrapping = TextWrapping.Wrap,
        VerticalScrollBarVisibility = ScrollBarVisibility.Auto
    };

    public SalesDeliveryPopupControl(SalesInvoiceListRow row)
    {
        _row = row;
        var stack = new StackPanel();

        stack.Children.Add(BuildInfo("العميل", row.CustomerName));
        stack.Children.Add(BuildInfo("رقم الفاتورة", row.InvoiceNumber));
        stack.Children.Add(BuildInfo("الإجمالي", $"${row.Amount:N2}"));

        stack.Children.Add(BuildField("تاريخ التسليم", _dpDate));
        stack.Children.Add(BuildField("اسم المستلم", _txtReceived));
        stack.Children.Add(BuildField("السائق / الجهة الناقلة (اختياري)", _txtDriver));
        stack.Children.Add(BuildField("ملاحظات (اختياري)", _txtNotes));

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

        var btnDeliver = new Button
        {
            Content = "تأكيد التسليم",
            Width = 130,
            Height = 34,
            Style = (Style)System.Windows.Application.Current.Resources["PrimaryButtonStyle"]
        };
        btnDeliver.Click += async (_, _) => await ConfirmAsync();

        var btnPrint = new Button
        {
            Content = "طباعة إشعار",
            Width = 120,
            Height = 34,
            Margin = new Thickness(8, 0, 0, 0),
            Style = (Style)System.Windows.Application.Current.Resources["GhostButtonStyle"]
        };
        btnPrint.Click += async (_, _) =>
        {
            if (!AppServices.IsInitialized) return;
            var oc = await SalesUiService.Instance.GetOperationsCenterAsync(_row.Id);
            if (oc.Value?.Invoice is null) return;
            SalesDocumentService.ShowDeliveryNotePreview(oc.Value.Invoice, _row.CustomerName);
        };

        btnRow.Children.Add(btnDeliver);
        btnRow.Children.Add(btnCancel);
        btnRow.Children.Add(btnPrint);
        stack.Children.Add(btnRow);
        Content = stack;
    }

    private async Task ConfirmAsync()
    {
        if (!AppServices.IsInitialized) return;
        if (!await SalesUiService.Instance.CanDeliverAsync())
        {
            MockInteractionService.ShowWarning("لا تملك صلاحية تأكيد التسليم.", "صلاحية");
            return;
        }
        var date = _dpDate.SelectedDate ?? DateTime.UtcNow;
        var result = await SalesUiService.Instance.DeliverAsync(
            _row.Id, date, _txtReceived.Text?.Trim(), _txtDriver.Text?.Trim(), _txtNotes.Text?.Trim());
        if (ApplicationResultPresenter.Present(result))
        {
            MockInteractionService.ShowSuccess($"تم تأكيد تسليم الفاتورة {_row.InvoiceNumber}.", "تسليم");
            SalesListRefreshHub.RequestRefresh();
            if (Window.GetWindow(this) is Window w) { w.DialogResult = true; w.Close(); }
        }
    }

    private static StackPanel BuildField(string label, UIElement content)
    {
        var sp = new StackPanel { Margin = new Thickness(0, 6, 0, 6) };
        sp.Children.Add(new TextBlock
        {
            Text = label,
            FontSize = 11,
            Foreground = (Brush)System.Windows.Application.Current.Resources["TextMutedBrush"],
            Margin = new Thickness(0, 0, 0, 4)
        });
        sp.Children.Add(content);
        return sp;
    }

    private static Grid BuildInfo(string label, string value)
    {
        var g = new Grid { Margin = new Thickness(0, 2, 0, 2) };
        g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(140) });
        g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        var lbl = new TextBlock { Text = label, FontSize = 11, Foreground = (Brush)System.Windows.Application.Current.Resources["TextMutedBrush"] };
        var val = new TextBlock { Text = value, FontSize = 12, FontWeight = FontWeights.SemiBold };
        Grid.SetColumn(lbl, 0); Grid.SetColumn(val, 1);
        g.Children.Add(lbl); g.Children.Add(val);
        return g;
    }
}
