using ERPSystem.Application.Commands.Expenses;
using ERPSystem.Application.DTOs.Expenses;
using ERPSystem.Core;
using ERPSystem.Domain.Enums;
using ERPSystem.Helpers;
using ERPSystem.Services;
using ERPSystem.Services.Expenses;
using System.Windows;
using System.Windows.Controls;

namespace ERPSystem.Controls.Expenses;

/// <summary>
/// تعريف مصروف — اسم فقط (مثل: مشروع معمل). القيود اليومية تُسجّل من شاشة "قيد مصروف".
/// </summary>
public sealed class ExpenseFormControl : UserControl
{
    private readonly TextBlock _title = ErpUiFactory.SectionTitle("تعريف مصروف جديد");
    private readonly TextBlock _hint = new()
    {
        Text = "عرّف المصروف مرة واحدة بالاسم فقط. بعد الحفظ يمكنك تسجيل قيود عليه من «قيد مصروف جديد».",
        Foreground = (System.Windows.Media.Brush)System.Windows.Application.Current.Resources["TextSecondaryBrush"]!,
        Margin = new Thickness(0, 0, 0, 12),
        TextWrapping = TextWrapping.Wrap
    };
    private readonly TextBox _name = ErpUiFactory.FormField("مثال: مشروع معمل");
    private readonly TextBox _notes = ErpUiFactory.FormField("ملاحظات اختيارية");
    private readonly Button _save = new() { Content = "حفظ التعريف", Style = S("PrimaryButtonStyle"), MinWidth = 140, Height = 38 };
    private readonly Button _cancel = new() { Content = "إلغاء", Style = S("SecondaryButtonStyle"), MinWidth = 100, Height = 38, Margin = new Thickness(8, 0, 0, 0) };

    private Guid? _editId;
    private Guid _defaultCategoryId;
    private bool _saving;
    private bool _popupMode;

    public ExpenseFormControl()
    {
        var stack = new StackPanel();
        stack.Children.Add(_title);
        stack.Children.Add(_hint);
        stack.Children.Add(ErpUiFactory.Card(ErpUiFactory.BuildFormGrid(
            ("اسم المصروف *", _name),
            ("ملاحظات", _notes))));

        var actions = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 16, 0, 0) };
        actions.Children.Add(_save);
        actions.Children.Add(_cancel);
        stack.Children.Add(actions);

        Content = new ScrollViewer { Padding = new Thickness(16), Content = stack, MaxWidth = 560 };
        Loaded += OnLoaded;
        _save.Click += async (_, _) => await SaveAsync();
        _cancel.Click += (_, _) =>
        {
            if (_popupMode) ExpensePopupService.CancelActive();
            else MockInteractionService.Navigate(AppModule.Expenses, "List");
        };
    }

    public void BindPopupHost()
    {
        _popupMode = true;
        _title.Visibility = Visibility.Collapsed;
        _hint.Visibility = Visibility.Collapsed;
        _cancel.Visibility = Visibility.Collapsed;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        _editId = ExpenseNavigationContext.EditExpenseId;

        var cats = await ExpenseUiService.Instance.GetCategoriesAsync();
        if (!ApplicationResultPresenter.Present(cats) || cats.Value is null || cats.Value.Count == 0)
        {
            _save.IsEnabled = false;
            return;
        }

        _defaultCategoryId = cats.Value[0].Id;

        if (_editId is Guid id)
        {
            _title.Text = "تعديل تعريف مصروف";
            var result = await ExpenseUiService.Instance.GetDetailsAsync(id);
            if (!ApplicationResultPresenter.Present(result) || result.Value is null) return;

            _name.Text = result.Value.Name;
            _notes.Text = result.Value.Notes ?? "";
            _defaultCategoryId = result.Value.CategoryId;
        }
        else if (!await ExpenseUiService.Instance.CanCreateAsync())
        {
            _save.IsEnabled = false;
            MockInteractionService.ShowWarning("لا تملك صلاحية إنشاء مصاريف.", "صلاحية");
        }
    }

    private async Task SaveAsync()
    {
        if (_saving) return;

        if (string.IsNullOrWhiteSpace(_name.Text))
        {
            MessageBox.Show("اسم المصروف مطلوب.", "المصاريف", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        _saving = true;
        _save.IsEnabled = false;
        try
        {
            if (_editId is Guid id)
            {
                var result = await ExpenseUiService.Instance.UpdateDefinitionAsync(new UpdateExpenseCommand
                {
                    ExpenseId = id,
                    Name = _name.Text.Trim(),
                    CategoryId = _defaultCategoryId,
                    StartDate = DateTime.Today,
                    OriginalCurrency = "USD",
                    OriginalAmount = 0,
                    ExchangeRate = 1m,
                    BaseCurrency = "USD",
                    PaymentMethod = ExpensePaymentMethod.Cash,
                    Notes = NullIfEmpty(_notes.Text),
                    IsRecurring = false,
                    RecurrenceFrequency = ExpenseRecurrenceFrequency.None
                });

                if (ApplicationResultPresenter.Present(result))
                {
                    MockInteractionService.ShowSuccess("تم حفظ التعريف.");
                    if (_popupMode) ExpensePopupService.CompleteSuccess();
                    else
                    {
                        ExpenseListRefreshHub.RequestRefresh();
                        MockInteractionService.Navigate(AppModule.Expenses, "List");
                    }
                }
            }
            else
            {
                var result = await ExpenseUiService.Instance.CreateDefinitionAsync(new CreateExpenseCommand
                {
                    Name = _name.Text.Trim(),
                    CategoryId = _defaultCategoryId,
                    StartDate = DateTime.Today,
                    OriginalCurrency = "USD",
                    OriginalAmount = 0,
                    ExchangeRate = 1m,
                    BaseCurrency = "USD",
                    PaymentMethod = ExpensePaymentMethod.Cash,
                    Notes = NullIfEmpty(_notes.Text),
                    IsRecurring = false,
                    RecurrenceFrequency = ExpenseRecurrenceFrequency.None,
                    SubmitForApproval = false
                });

                if (ApplicationResultPresenter.Present(result))
                {
                    MockInteractionService.ShowSuccess("تم تعريف المصروف. يمكنك الآن تسجيل قيود عليه.");
                    if (_popupMode)
                    {
                        ExpensePopupService.CompleteSuccess();
                        ExpensePopupService.ShowEntry(new ExpenseListDto { Id = result.Value, Name = _name.Text.Trim(), Code = "" });
                    }
                    else
                    {
                        ExpenseListRefreshHub.RequestRefresh();
                        MockInteractionService.Navigate(AppModule.Expenses, "Entry");
                    }
                }
            }
        }
        finally
        {
            _saving = false;
            _save.IsEnabled = true;
        }
    }

    private static string? NullIfEmpty(string? text) =>
        string.IsNullOrWhiteSpace(text) ? null : text.Trim();

    private static Style S(string k) => (Style)System.Windows.Application.Current.Resources[k]!;
}
