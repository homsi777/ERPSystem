using ERPSystem.Core.Sales;
using ERPSystem.Helpers;
using ERPSystem.Services;
using ERPSystem.Services.Sales;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ERPSystem.Controls.Sales.Popups;

/// <summary>
/// بطاقة تأكيد التسليم — لوجستية فقط (لا ترحيل صندوق/محاسبة؛ ذلك يتم عند الاعتماد).
/// </summary>
public sealed class SalesDeliveryPopupControl : UserControl
{
    private readonly SalesInvoiceListRow _row;
    private readonly DatePicker _dpDate;
    private readonly TextBox _txtReceived;
    private readonly TextBox _txtDriver;
    private readonly TextBox _txtNotes;
    private readonly Button _btnDeliver;

    public SalesDeliveryPopupControl(SalesInvoiceListRow row)
    {
        _row = row;
        _dpDate = ErpUiFactory.FormDate(DateTime.Today);
        _dpDate.Width = double.NaN;
        _dpDate.HorizontalAlignment = HorizontalAlignment.Stretch;

        _txtReceived = ErpUiFactory.FormField(row.CustomerName);
        _txtDriver = ErpUiFactory.FormField();
        _txtNotes = new TextBox
        {
            MinHeight = 72,
            AcceptsReturn = true,
            TextWrapping = TextWrapping.Wrap,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            Style = (Style)System.Windows.Application.Current.Resources["EnterpriseInputStyle"],
            FontSize = 13,
            Padding = new Thickness(10, 8, 10, 8)
        };

        _btnDeliver = new Button
        {
            Content = "تأكيد التسليم",
            MinWidth = 140,
            Height = 38,
            Style = (Style)System.Windows.Application.Current.Resources["PrimaryButtonStyle"]
        };
        _btnDeliver.Click += async (_, _) => await ConfirmAsync();

        Content = BuildLayout();
    }

    private UIElement BuildLayout()
    {
        // Margin/scroll are handled by ErpModalWindow.SetBody — keep this body compact.
        var body = new StackPanel();

        body.Children.Add(BuildSummaryCard());
        body.Children.Add(new TextBlock
        {
            Text = "التسليم خطوة لوجستية فقط — ترحيل الصندوق والمحاسبة يتم عند اعتماد الفاتورة.",
            FontSize = 11,
            TextWrapping = TextWrapping.Wrap,
            Foreground = Br("TextMutedBrush"),
            Margin = new Thickness(0, 0, 0, 14)
        });

        body.Children.Add(SectionTitle("بيانات التسليم"));
        body.Children.Add(ErpUiFactory.BuildFormGrid(
            ("تاريخ التسليم", _dpDate),
            ("اسم المستلم", _txtReceived)
        ));
        body.Children.Add(LabeledField("السائق / الجهة الناقلة (اختياري)", _txtDriver));
        body.Children.Add(LabeledField("ملاحظات (اختياري)", _txtNotes));

        var footer = new Border
        {
            BorderBrush = Br("BorderBrush"),
            BorderThickness = new Thickness(0, 1, 0, 0),
            Padding = new Thickness(0, 14, 0, 0),
            Margin = new Thickness(0, 8, 0, 0)
        };

        var actions = new Grid();
        actions.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        actions.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var secondary = new StackPanel { Orientation = Orientation.Horizontal };
        var btnPrint = GhostButton("طباعة إشعار", "\uE749");
        btnPrint.Click += async (_, _) => await PrintNoteAsync();
        var btnCancel = GhostButton("تراجع", null);
        btnCancel.Click += (_, _) => SalesPopupService.CancelActive();
        secondary.Children.Add(btnPrint);
        secondary.Children.Add(btnCancel);
        Grid.SetColumn(secondary, 0);
        actions.Children.Add(secondary);

        Grid.SetColumn(_btnDeliver, 1);
        actions.Children.Add(_btnDeliver);
        footer.Child = actions;
        body.Children.Add(footer);

        return body;
    }

    private Border BuildSummaryCard()
    {
        var grid = new Grid { Margin = new Thickness(2) };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        void AddMetric(int col, string label, string value, bool emphasize = false)
        {
            var sp = new StackPanel { Margin = new Thickness(4) };
            sp.Children.Add(new TextBlock
            {
                Text = label,
                FontSize = 11,
                Foreground = Br("TextMutedBrush")
            });
            sp.Children.Add(new TextBlock
            {
                Text = value,
                FontSize = emphasize ? 16 : 13,
                FontWeight = FontWeights.SemiBold,
                Foreground = emphasize ? Br("PrimaryBrush") : Br("TextPrimaryBrush"),
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 4, 0, 0)
            });
            Grid.SetColumn(sp, col);
            grid.Children.Add(sp);
        }

        AddMetric(0, "العميل", _row.CustomerName);
        AddMetric(1, "رقم الفاتورة", _row.InvoiceNumber);
        AddMetric(2, "الإجمالي", $"${_row.Amount:N2}", emphasize: true);

        return new Border
        {
            Background = Br("InfoBgBrush"),
            BorderBrush = Br("BorderBrush"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(10),
            Padding = new Thickness(14, 12, 14, 12),
            Margin = new Thickness(0, 0, 0, 12),
            Child = grid
        };
    }

    private async Task ConfirmAsync()
    {
        if (!AppServices.IsInitialized) return;

        var received = _txtReceived.Text?.Trim();
        if (string.IsNullOrWhiteSpace(received))
        {
            MockInteractionService.ShowWarning("أدخل اسم المستلم قبل تأكيد التسليم.", "تسليم");
            _txtReceived.Focus();
            return;
        }

        if (!await SalesUiService.Instance.CanDeliverAsync())
        {
            MockInteractionService.ShowWarning("لا تملك صلاحية تأكيد التسليم.", "صلاحية");
            return;
        }

        _btnDeliver.IsEnabled = false;
        try
        {
            var date = _dpDate.SelectedDate ?? DateTime.Today;
            var result = await SalesUiService.Instance.DeliverAsync(
                _row.Id,
                date,
                received,
                _txtDriver.Text?.Trim(),
                _txtNotes.Text?.Trim());

            if (ApplicationResultPresenter.Present(result))
            {
                MockInteractionService.ShowSuccess(
                    $"تم تأكيد تسليم الفاتورة {_row.InvoiceNumber}.",
                    "تسليم");
                SalesListRefreshHub.RequestRefresh();
                SalesPopupService.CompleteSuccess();
            }
        }
        finally
        {
            _btnDeliver.IsEnabled = true;
        }
    }

    private async Task PrintNoteAsync()
    {
        if (!AppServices.IsInitialized) return;
        var oc = await SalesUiService.Instance.GetOperationsCenterAsync(_row.Id);
        if (!ApplicationResultPresenter.Present(oc) || oc.Value?.Invoice is null) return;
        SalesDocumentService.ShowDeliveryNotePreview(oc.Value.Invoice, _row.CustomerName);
    }

    private static TextBlock SectionTitle(string text) => new()
    {
        Text = text,
        FontSize = 13,
        FontWeight = FontWeights.SemiBold,
        Foreground = Br("TextPrimaryBrush"),
        Margin = new Thickness(0, 0, 0, 10)
    };

    private static StackPanel LabeledField(string label, UIElement field)
    {
        var sp = new StackPanel { Margin = new Thickness(0, 0, 0, 10) };
        sp.Children.Add(new TextBlock
        {
            Text = label,
            FontSize = 11,
            Foreground = Br("TextSecondaryBrush"),
            Margin = new Thickness(0, 0, 0, 4)
        });
        sp.Children.Add(field);
        return sp;
    }

    private static Button GhostButton(string text, string? glyph)
    {
        object content = text;
        if (!string.IsNullOrEmpty(glyph))
        {
            var sp = new StackPanel { Orientation = Orientation.Horizontal };
            sp.Children.Add(new TextBlock
            {
                Text = glyph,
                FontFamily = new FontFamily("Segoe MDL2 Assets"),
                FontSize = 13,
                Margin = new Thickness(0, 0, 6, 0),
                VerticalAlignment = VerticalAlignment.Center
            });
            sp.Children.Add(new TextBlock { Text = text, VerticalAlignment = VerticalAlignment.Center });
            content = sp;
        }

        return new Button
        {
            Content = content,
            Height = 38,
            MinWidth = 100,
            Margin = new Thickness(0, 0, 8, 0),
            Padding = new Thickness(12, 0, 12, 0),
            Style = (Style)System.Windows.Application.Current.Resources["GhostButtonStyle"]
        };
    }

    private static Brush Br(string key) =>
        (Brush)System.Windows.Application.Current.Resources[key]!;
}
