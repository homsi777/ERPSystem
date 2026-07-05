using ERPSystem.Application.Commands.HR;
using ERPSystem.Application.DTOs.HR;
using ERPSystem.Core;
using ERPSystem.Helpers;
using ERPSystem.Services;
using ERPSystem.Services.Hr;
using System.Windows;
using System.Windows.Controls;

namespace ERPSystem.Controls.Hr;

public sealed class EmployeeFormControl : UserControl
{
    private readonly TextBox _txtCode = ErpUiFactory.FormField("");
    private readonly TextBox _txtName = ErpUiFactory.FormField("");
    private readonly ComboBox _cmbDepartment = new() { Height = 36, MinWidth = 240 };
    private readonly TextBox _txtJob = ErpUiFactory.FormField("");
    private readonly TextBox _txtPhone = ErpUiFactory.FormField("");
    private readonly TextBox _txtEmail = ErpUiFactory.FormField("");
    private readonly DatePicker _dpHire = ErpUiFactory.FormDate(DateTime.Today);
    private readonly TextBox _txtSalary = ErpUiFactory.FormField("0");
    private readonly TextBox _txtNotes = ErpUiFactory.FormField("");
    private readonly Button _btnSave = new() { Content = "حفظ", Style = S("PrimaryButtonStyle"), MinWidth = 120, Height = 36 };
    private readonly Button _btnCancel = new() { Content = "إلغاء", Style = S("SecondaryButtonStyle"), MinWidth = 100, Height = 36, Margin = new Thickness(8, 0, 0, 0) };
    private Guid? _editId;
    private bool _saving;

    public EmployeeFormControl()
    {
        var stack = new StackPanel { MaxWidth = 520 };
        stack.Children.Add(ErpUiFactory.Card(ErpUiFactory.BuildFormGrid(
            ("كود الموظف", _txtCode),
            ("الاسم الكامل *", _txtName),
            ("القسم", _cmbDepartment),
            ("المسمى الوظيفي", _txtJob),
            ("الهاتف", _txtPhone),
            ("البريد الإلكتروني", _txtEmail),
            ("تاريخ التعيين", _dpHire),
            ("الراتب الأساسي", _txtSalary),
            ("ملاحظات", _txtNotes))));
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
        var deptResult = await HrUiService.Instance.GetDepartmentsAsync();
        _cmbDepartment.Items.Clear();
        _cmbDepartment.Items.Add(new ComboBoxItem { Content = "— بدون قسم —", Tag = null });
        if (deptResult.IsSuccess && deptResult.Value is not null)
            foreach (var d in deptResult.Value)
                _cmbDepartment.Items.Add(new ComboBoxItem { Content = d.Name, Tag = d.Id });
        _cmbDepartment.SelectedIndex = 0;

        _editId = HrNavigationContext.EditEmployeeId;
        _txtCode.IsReadOnly = _editId.HasValue;
        if (_editId is Guid id)
        {
            var result = await HrUiService.Instance.GetEmployeeAsync(id);
            if (!ApplicationResultPresenter.Present(result) || result.Value is null) return;
            var emp = result.Value;
            _txtCode.Text = emp.EmployeeCode;
            _txtName.Text = emp.FullName;
            _txtJob.Text = emp.JobTitle;
            _txtPhone.Text = emp.Phone ?? "";
            _txtEmail.Text = emp.Email ?? "";
            _txtNotes.Text = emp.Notes ?? "";
            _dpHire.SelectedDate = emp.HireDate;
            _txtSalary.Text = emp.BasicSalary.ToString("0.##");
            SelectDepartment(emp.DepartmentId);
        }
        else
        {
            _txtCode.Text = await HrUiService.Instance.NextEmployeeCodeAsync();
        }
    }

    private void SelectDepartment(Guid? departmentId)
    {
        for (var i = 0; i < _cmbDepartment.Items.Count; i++)
        {
            if (_cmbDepartment.Items[i] is ComboBoxItem item && (Guid?)item.Tag == departmentId)
            {
                _cmbDepartment.SelectedIndex = i;
                return;
            }
        }
        _cmbDepartment.SelectedIndex = 0;
    }

    private async Task SaveAsync()
    {
        if (_saving) return;
        if (string.IsNullOrWhiteSpace(_txtName.Text))
        {
            MockInteractionService.ShowWarning("اسم الموظف مطلوب.");
            return;
        }
        decimal.TryParse(_txtSalary.Text, out var salary);
        var departmentId = (_cmbDepartment.SelectedItem as ComboBoxItem)?.Tag as Guid?;
        var hire = _dpHire.SelectedDate ?? DateTime.Today;

        _saving = true;
        _btnSave.IsEnabled = false;
        try
        {
            if (_editId is Guid id)
            {
                var result = await HrUiService.Instance.UpdateEmployeeAsync(new UpdateEmployeeCommand
                {
                    EmployeeId = id,
                    FullName = _txtName.Text.Trim(),
                    DepartmentId = departmentId,
                    JobTitle = _txtJob.Text.Trim(),
                    Phone = NullIfBlank(_txtPhone.Text),
                    Email = NullIfBlank(_txtEmail.Text),
                    Notes = NullIfBlank(_txtNotes.Text),
                    HireDate = hire,
                    BasicSalary = salary,
                    IsActive = true
                });
                if (ApplicationResultPresenter.Present(result))
                {
                    HrListRefreshHub.RequestRefresh();
                    CloseDialog(true);
                }
            }
            else
            {
                var result = await HrUiService.Instance.CreateEmployeeAsync(
                    _txtCode.Text.Trim(),
                    _txtName.Text.Trim(),
                    departmentId,
                    _txtJob.Text.Trim(),
                    NullIfBlank(_txtPhone.Text),
                    NullIfBlank(_txtEmail.Text),
                    NullIfBlank(_txtNotes.Text),
                    hire,
                    salary);
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

    private static string? NullIfBlank(string s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();

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
