using ERPSystem.Application.DTOs.Accounting;
using ERPSystem.Helpers;
using ERPSystem.Services;
using ERPSystem.Services.Accounting;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using WpfApplication = System.Windows.Application;

namespace ERPSystem.Controls.Accounting.Popups;

public sealed class AccountDetailsPopupControl : UserControl
{
    private readonly StackPanel _root = new();

    public AccountDetailsPopupControl(Guid accountId)
    {
        Content = _root;
        Loaded += async (_, _) => await LoadAsync(accountId);
    }

    private async Task LoadAsync(Guid accountId)
    {
        if (!AppServices.IsInitialized) return;
        var result = await AccountingUiService.Instance.GetAccountDetailsAsync(accountId);
        if (!ApplicationResultPresenter.Present(result) || result.Value is null) return;
        Render(result.Value);
    }

    private void Render(AccountDetailsDto data)
    {
        _root.Children.Clear();

        _root.Children.Add(ErpUiFactory.Card(ErpUiFactory.BuildFormGrid(
            ("الكود", ReadOnly(data.Code)),
            ("الاسم (عربي)", ReadOnly(data.NameAr)),
            ("الاسم (إنجليزي)", ReadOnly(string.IsNullOrWhiteSpace(data.NameEn) ? "—" : data.NameEn)),
            ("نوع الحساب", ReadOnly(data.AccountTypeDisplay)),
            ("الحساب الأب", ReadOnly(data.ParentName ?? "—")),
            ("قابل للترحيل", ReadOnly(data.IsPostable ? "نعم" : "لا")),
            ("الحالة", ReadOnly(data.IsActive ? "نشط" : "معطّل")))));

        if (data.Children.Count > 0)
        {
            _root.Children.Add(ErpUiFactory.SectionTitle("الحسابات الفرعية"));
            var grid = new DataGrid { AutoGenerateColumns = false, IsReadOnly = true, MaxHeight = 200 };
            ErpUiFactory.AddGridColumn(grid, "الكود", nameof(AccountListDto.Code), 90);
            ErpUiFactory.AddGridColumn(grid, "الاسم", nameof(AccountListDto.NameAr), "*");
            ErpUiFactory.AddGridColumn(grid, "النوع", nameof(AccountListDto.AccountTypeDisplay), 100);
            grid.ItemsSource = data.Children.ToList();
            _root.Children.Add(ErpUiFactory.Card(grid));
        }
    }

    private static TextBlock ReadOnly(string text) => new()
    {
        Text = text,
        FontSize = 13,
        Foreground = (Brush)WpfApplication.Current.Resources["TextPrimaryBrush"]!
    };
}
