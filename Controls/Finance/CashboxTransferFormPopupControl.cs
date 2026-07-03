using ERPSystem.Application.DTOs.Expenses;
using ERPSystem.Helpers;
using ERPSystem.Services;
using ERPSystem.Services.Finance;
using System.Windows;
using System.Windows.Controls;

namespace ERPSystem.Controls.Finance;

public sealed class CashboxTransferFormPopupControl : UserControl
{
    private readonly ComboBox _from = ErpUiFactory.FilterCombo(Array.Empty<string>());
    private readonly ComboBox _to = ErpUiFactory.FilterCombo(Array.Empty<string>());
    private readonly TextBox _amount = ErpUiFactory.FormField("0");
    private readonly TextBox _notes = ErpUiFactory.FormField("");
    private readonly Button _btnSave = new() { Content = "تنفيذ التحويل", Style = S("PrimaryButtonStyle"), MinWidth = 140, Height = 38 };
    private readonly Button _btnCancel = new() { Content = "إلغاء", Style = S("SecondaryButtonStyle"), MinWidth = 100, Height = 38, Margin = new Thickness(8, 0, 0, 0) };
    private List<CashboxOptionDto> _boxes = [];
    private bool _saving;

    public CashboxTransferFormPopupControl()
    {
        var stack = new StackPanel { MaxWidth = 480 };
        stack.Children.Add(ErpUiFactory.SectionTitle("تحويل بين الصناديق"));
        stack.Children.Add(new TextBlock
        {
            Text = "يُخصم المبلغ من الصندوق المصدر ويُضاف إلى صندوق الوجهة فوراً.",
            TextWrapping = TextWrapping.Wrap,
            FontSize = 12,
            Foreground = Br("TextSecondaryBrush"),
            Margin = new Thickness(0, 0, 0, 12)
        });
        stack.Children.Add(ErpUiFactory.Card(ErpUiFactory.BuildFormGrid(
            ("من صندوق *", _from),
            ("إلى صندوق *", _to),
            ("المبلغ *", _amount),
            ("ملاحظات", _notes))));
        var actions = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 16, 0, 0) };
        actions.Children.Add(_btnSave);
        actions.Children.Add(_btnCancel);
        stack.Children.Add(actions);
        Content = stack;
        Loaded += OnLoaded;
        _btnSave.Click += async (_, _) => await SaveAsync();
        _btnCancel.Click += (_, _) => CashboxPopupService.CancelActive();
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (!await FinanceUiService.Instance.CanTransferCashboxAsync())
        {
            _btnSave.IsEnabled = false;
            MockInteractionService.ShowWarning("لا تملك صلاحية تحويل بين الصناديق.", "صلاحية");
        }

        var result = await FinanceUiService.Instance.GetCashboxesAsync();
        if (!ApplicationResultPresenter.Present(result) || result.Value is null) return;
        _boxes = result.Value.ToList();
        _from.ItemsSource = _boxes;
        _to.ItemsSource = _boxes;
        _from.DisplayMemberPath = nameof(CashboxOptionDto.Name);
        _from.SelectedValuePath = nameof(CashboxOptionDto.Id);
        _to.DisplayMemberPath = nameof(CashboxOptionDto.Name);
        _to.SelectedValuePath = nameof(CashboxOptionDto.Id);

        if (CashboxNavigationContext.TransferFromCashboxId is Guid fromId)
        {
            _from.SelectedValue = fromId;
            CashboxNavigationContext.TransferFromCashboxId = null;
        }
        else if (_boxes.Count > 0)
            _from.SelectedIndex = 0;
        if (_boxes.Count > 1)
            _to.SelectedIndex = 1;
    }

    private async Task SaveAsync()
    {
        if (_saving) return;
        if (_from.SelectedValue is not Guid fromId || _to.SelectedValue is not Guid toId)
        {
            MockInteractionService.ShowWarning("اختر صندوق المصدر والوجهة.");
            return;
        }
        if (fromId == toId)
        {
            MockInteractionService.ShowWarning("لا يمكن التحويل لنفس الصندوق.");
            return;
        }
        if (!decimal.TryParse(_amount.Text, out var amount) || amount <= 0)
        {
            MockInteractionService.ShowWarning("أدخل مبلغاً صحيحاً.");
            return;
        }
        if (!MockInteractionService.Confirm($"تأكيد تحويل {amount:N2} بين الصناديق؟", "تحويل"))
            return;

        _saving = true;
        _btnSave.IsEnabled = false;
        try
        {
            var result = await FinanceUiService.Instance.CreateCashboxTransferAsync(
                fromId, toId, amount, _notes.Text.Trim());
            if (ApplicationResultPresenter.Present(result))
            {
                CashboxListRefreshHub.RequestRefresh();
                CashboxPopupService.CloseActive(true);
            }
        }
        finally
        {
            _saving = false;
            _btnSave.IsEnabled = true;
        }
    }

    private static System.Windows.Media.Brush Br(string k) => (System.Windows.Media.Brush)System.Windows.Application.Current.Resources[k]!;
    private static Style S(string k) => (Style)System.Windows.Application.Current.Resources[k]!;
}
