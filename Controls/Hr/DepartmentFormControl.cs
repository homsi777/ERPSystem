using ERPSystem.Core;
using ERPSystem.Helpers;
using ERPSystem.Services;
using ERPSystem.Services.Hr;
using System.Windows;
using System.Windows.Controls;

namespace ERPSystem.Controls.Hr;

public sealed class DepartmentFormControl : UserControl
{
    private readonly TextBox _txtCode = ErpUiFactory.FormField("");
    private readonly TextBox _txtName = ErpUiFactory.FormField("");
    private readonly Button _btnSave = new() { Content = "حفظ", Style = S("PrimaryButtonStyle"), MinWidth = 120, Height = 36 };
    private readonly Button _btnCancel = new() { Content = "إلغاء", Style = S("SecondaryButtonStyle"), MinWidth = 100, Height = 36, Margin = new Thickness(8, 0, 0, 0) };
    private Guid? _editId;
    private bool _saving;

    public DepartmentFormControl()
    {
        var stack = new StackPanel { MaxWidth = 420 };
        stack.Children.Add(ErpUiFactory.Card(ErpUiFactory.BuildFormGrid(
            ("كود القسم", _txtCode),
            ("اسم القسم *", _txtName))));
        var actions = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 16, 0, 0) };
        actions.Children.Add(_btnSave);
        actions.Children.Add(_btnCancel);
        stack.Children.Add(actions);
        Content = stack;
        Loaded += OnLoaded;
        _btnSave.Click += async (_, _) => await SaveAsync();
        _btnCancel.Click += (_, _) => CloseDialog(false);
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        Loaded -= OnLoaded;
        _editId = HrNavigationContext.EditDepartmentId;
        _txtCode.IsReadOnly = _editId.HasValue;
        if (_editId is Guid id)
        {
            var result = await HrUiService.Instance.GetDepartmentsAsync();
            var dept = result.Value?.FirstOrDefault(d => d.Id == id);
            if (dept is not null)
            {
                _txtCode.Text = dept.Code;
                _txtName.Text = dept.Name;
            }
        }
        else
        {
            _txtCode.Text = await HrUiService.Instance.NextDepartmentCodeAsync();
        }
    }

    private async Task SaveAsync()
    {
        if (_saving) return;
        if (string.IsNullOrWhiteSpace(_txtName.Text))
        {
            MockInteractionService.ShowWarning("اسم القسم مطلوب.");
            return;
        }
        _saving = true;
        _btnSave.IsEnabled = false;
        try
        {
            if (_editId is Guid id)
            {
                var result = await HrUiService.Instance.UpdateDepartmentAsync(id, _txtName.Text.Trim());
                if (ApplicationResultPresenter.Present(result))
                {
                    HrListRefreshHub.RequestRefresh();
                    CloseDialog(true);
                }
            }
            else
            {
                var result = await HrUiService.Instance.CreateDepartmentAsync(_txtCode.Text.Trim(), _txtName.Text.Trim());
                if (ApplicationResultPresenter.Present(result))
                {
                    HrListRefreshHub.RequestRefresh();
                    CloseDialog(true);
                }
            }
        }
        finally
        {
            _saving = false;
            _btnSave.IsEnabled = true;
        }
    }

    private void CloseDialog(bool result)
    {
        if (Window.GetWindow(this) is { } w)
        {
            w.DialogResult = result;
            w.Close();
        }
    }

    private static Style S(string k) => (Style)System.Windows.Application.Current.Resources[k]!;
}
