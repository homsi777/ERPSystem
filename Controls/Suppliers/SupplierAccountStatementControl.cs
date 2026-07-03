using ERPSystem.Application.DTOs.Suppliers;
using ERPSystem.Core;
using ERPSystem.Domain.Enums;
using ERPSystem.Helpers;
using ERPSystem.Services;
using ERPSystem.Services.Suppliers;
using System.Windows;
using System.Windows.Controls;

namespace ERPSystem.Controls.Suppliers;

public sealed class SupplierAccountStatementControl : UserControl
{
    private readonly TextBlock _title = ErpUiFactory.SectionTitle("كشف حساب مورد");
    private readonly Border _empty = new() { Visibility = Visibility.Collapsed, Padding = new Thickness(32) };
    private readonly StackPanel _content = new();
    private readonly DatePicker _from = ErpUiFactory.FormDate(new DateTime(DateTime.Today.Year, 1, 1));
    private readonly DatePicker _to = ErpUiFactory.FormDate(DateTime.Today);
    private readonly DataGrid _grid = new() { AutoGenerateColumns = false, IsReadOnly = true, MinHeight = 240 };
    private readonly TextBlock _summary = new() { Margin = new Thickness(0, 8, 0, 0), TextWrapping = TextWrapping.Wrap };

    private Guid? _supplierId;
    private string _supplierName = "";

    public SupplierAccountStatementControl()
    {
        ErpDataGridHelper.ApplyEnterpriseStyle(_grid);
        ErpUiFactory.AddGridColumn(_grid, "التاريخ", nameof(SupplierStatementLineVm.DateDisplay), 100, null);
        ErpUiFactory.AddGridColumn(_grid, "المرجع", nameof(SupplierStatementLineVm.DocumentNumber), 120, null);
        ErpUiFactory.AddGridColumn(_grid, "البيان", nameof(SupplierStatementLineVm.Description), "*", null);
        ErpUiFactory.AddGridColumn(_grid, "مدين", nameof(SupplierStatementLineVm.DebitDisplay), 90, null);
        ErpUiFactory.AddGridColumn(_grid, "دائن", nameof(SupplierStatementLineVm.CreditDisplay), 90, null);
        ErpUiFactory.AddGridColumn(_grid, "الرصيد", nameof(SupplierStatementLineVm.BalanceDisplay), 100, null);

        _empty.Child = PlaceholderUi.EmptyMessage(
            "اختر مورداً لعرض كشف حسابه",
            "افتح سجل الموردين أو مركز عمليات المورد");

        var filters = ErpUiFactory.Card(ErpUiFactory.BuildFilterRow(
            ("من تاريخ", _from),
            ("إلى تاريخ", _to)));
        _from.SelectedDateChanged += async (_, _) => await ReloadAsync();
        _to.SelectedDateChanged += async (_, _) => await ReloadAsync();

        _content.Children.Add(filters);
        _content.Children.Add(ErpUxFactory.ExportBar($"كشف حساب — {_supplierName}"));
        _content.Children.Add(_summary);
        _content.Children.Add(ErpUiFactory.Card(_grid));

        var root = new Grid();
        root.Children.Add(_empty);
        var scroll = new ScrollViewer { Content = new StackPanel { Children = { _title, _content } }, Padding = new Thickness(16) };
        root.Children.Add(scroll);
        Content = root;
        Loaded += async (_, _) => await ReloadAsync();
    }

    public void Initialize(Guid supplierId, string supplierName)
    {
        _supplierId = supplierId;
        _supplierName = supplierName;
        _title.Text = $"كشف حساب — {supplierName}";
        UpdateVisibility();
    }

    private void UpdateVisibility()
    {
        var has = _supplierId.HasValue;
        _empty.Visibility = has ? Visibility.Collapsed : Visibility.Visible;
        _content.Visibility = has ? Visibility.Visible : Visibility.Collapsed;
    }

    private async Task ReloadAsync()
    {
        UpdateVisibility();
        if (_supplierId is not Guid id || !AppServices.IsInitialized)
            return;

        var result = await SupplierUiService.Instance.GetStatementAsync(id, _from.SelectedDate, _to.SelectedDate);
        if (!ApplicationResultPresenter.Present(result) || result.Value is null)
            return;

        var dto = result.Value;
        _summary.Text =
            $"شروط السداد: {dto.PaymentTermsDisplay} | حد الائتمان: {dto.CreditLimit:N2} ر.س | " +
            $"افتتاحي: {dto.OpeningBalance:N2} | مدين: {dto.TotalDebit:N2} | دائن: {dto.TotalCredit:N2} | ختامي: {dto.ClosingBalance:N2} ر.س";

        _grid.ItemsSource = dto.Lines.Select(SupplierStatementLineVm.FromDto).ToList();
    }

    private sealed class SupplierStatementLineVm
    {
        public string DateDisplay { get; init; } = "";
        public string DocumentNumber { get; init; } = "";
        public string Description { get; init; } = "";
        public string DebitDisplay { get; init; } = "";
        public string CreditDisplay { get; init; } = "";
        public string BalanceDisplay { get; init; } = "";

        public static SupplierStatementLineVm FromDto(SupplierStatementLineDto dto) => new()
        {
            DateDisplay = dto.EntryDate.ToString("yyyy/MM/dd"),
            DocumentNumber = dto.DocumentNumber,
            Description = dto.Description,
            DebitDisplay = dto.Debit > 0 ? dto.Debit.ToString("N2") : "—",
            CreditDisplay = dto.Credit > 0 ? dto.Credit.ToString("N2") : "—",
            BalanceDisplay = dto.RunningBalance.ToString("N2")
        };
    }
}
