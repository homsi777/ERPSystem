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

public sealed class DepartmentListPageControl : UserControl
{
    private readonly ErpListModuleControl _page = new();
    private bool _isLoading;

    public DepartmentListPageControl()
    {
        _page.Configure(EntityType.Employee, AppModule.HR);
        _page.SetHeader("الأقسام", "الموارد البشرية", "\uE902", B("AccentCustomersBrush"));
        _page.SetPrimaryButton("إضافة قسم");
        _page.SetEmptyState("لا توجد أقسام مضافة بعد", "إضافة قسم", "\uE902");
        _page.PrimaryActionRequested += (_, _) => ShowForm(null);

        _page.Grid.MouseDoubleClick += (_, _) =>
        {
            if (_page.Grid.SelectedItem is DepartmentListDto row)
                ShowForm(row.Id);
        };

        var g = _page.Grid;
        g.AutoGenerateColumns = false;
        g.IsReadOnly = true;
        foreach (var (h, p, w) in new (string, string, object)[]
        {
            ("الكود", nameof(DepartmentListDto.Code), 120),
            ("الاسم", nameof(DepartmentListDto.Name), "*")
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

    private static void ShowForm(Guid? departmentId)
    {
        if (departmentId is Guid id) HrNavigationContext.BeginEditDepartment(id);
        else HrNavigationContext.BeginCreateDepartment();
        ErpModalWindow.Show(
            departmentId is null ? "قسم جديد" : "تعديل قسم",
            "الموارد البشرية",
            new DepartmentFormControl(),
            "\uE902",
            width: 440);
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
            var result = await HrUiService.Instance.GetDepartmentsAsync();
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
