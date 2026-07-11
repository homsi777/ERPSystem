using ERPSystem.Application.DTOs.HR;
using ERPSystem.Core;
using ERPSystem.Core.Actions;
using ERPSystem.Dialogs;
using ERPSystem.Helpers;
using ERPSystem.Services;
using ERPSystem.Services.Hr;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ERPSystem.Controls.Hr;

public sealed class EmployeeListPageControl : UserControl
{
    private readonly ErpListModuleControl _page = new();
    private bool _isLoading;

    public EmployeeListPageControl()
    {
        _page.Configure(EntityType.Employee, AppModule.HR);
        _page.SetHeader("الموظفون", "الموارد البشرية", "\uE716", B("AccentCustomersBrush"));
        _page.SetPrimaryButton("إضافة موظف");
        _page.SetEmptyState("لا يوجد موظفون مضافون بعد", "إضافة موظف", "\uE716");
        _page.PrimaryActionRequested += (_, _) => ShowForm(null);

        _page.Grid.MouseDoubleClick += (_, _) =>
        {
            if (_page.Grid.SelectedItem is EmployeeListDto row)
                ShowForm(row.Id);
        };

        var g = _page.Grid;
        g.AutoGenerateColumns = false;
        g.IsReadOnly = true;
        foreach (var (h, p, w) in new (string, string, object)[]
        {
            ("الكود", nameof(EmployeeListDto.EmployeeCode), 100),
            ("الاسم", nameof(EmployeeListDto.FullName), "*"),
            ("القسم", nameof(EmployeeListDto.DepartmentName), 140),
            ("المسمى", nameof(EmployeeListDto.JobTitle), 140),
            ("الهاتف", nameof(EmployeeListDto.Phone), 120),
            ("تاريخ التعيين", nameof(EmployeeListDto.HireDate), 110),
            ("الحالة", nameof(EmployeeListDto.StatusDisplay), 80)
        })
            ErpUiFactory.AddGridColumn(g, h, p, w, null);

        Content = _page;
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
        HrListRefreshHub.RefreshRequested += OnRefreshRequested;
    }

    private void OnUnloaded(object sender, RoutedEventArgs e) =>
        HrListRefreshHub.RefreshRequested -= OnRefreshRequested;

    private void OnRefreshRequested(object? sender, EventArgs e) => _ = LoadAsync();

    private static void ShowForm(Guid? employeeId)
    {
        if (employeeId is Guid id) HrNavigationContext.BeginEditEmployee(id);
        else HrNavigationContext.BeginCreateEmployee();
        ErpModalWindow.Show(
            employeeId is null ? "موظف جديد" : "تعديل موظف",
            "الموارد البشرية",
            new EmployeeFormControl(),
            "\uE716",
            width: 560);
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        Loaded -= OnLoaded;
        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        if (_isLoading || !AppServices.IsInitialized) { _page.BindData([]); return; }
        _isLoading = true;
        _page.SetLoadingState(true);
        try
        {
            var result = await HrUiService.Instance.GetEmployeesAsync();
            _page.BindData(result.IsSuccess && result.Value is not null
                ? result.Value.Cast<object>().ToList()
                : []);
        }
        finally
        {
            _page.SetLoadingState(false);
            _isLoading = false;
        }
    }

    private static SolidColorBrush B(string k) => (SolidColorBrush)System.Windows.Application.Current.Resources[k]!;
}
