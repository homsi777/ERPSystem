using ERPSystem.Application.Commands.Inventory;
using ERPSystem.Application.DTOs.Inventory;
using ERPSystem.Core;
using ERPSystem.Domain.Entities.Catalog;
using ERPSystem.Helpers;
using ERPSystem.Services;
using ERPSystem.Services.Inventory;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;

namespace ERPSystem.Controls.Inventory;

public sealed class InventoryOpeningStockFormControl : UserControl
{
    private readonly ComboBox _warehouse = new() { MinWidth = 220, DisplayMemberPath = nameof(WarehouseListExtendedDto.NameAr) };
    private readonly DatePicker _date = new() { SelectedDate = DateTime.Today };
    private readonly TextBox _reference = ErpUiFactory.FormField("");
    private readonly TextBox _currency = ErpUiFactory.FormField("USD");
    private readonly TextBox _notes = ErpUiFactory.FormField("");
    private readonly DataGrid _lines = new() { AutoGenerateColumns = false, CanUserAddRows = false, MinHeight = 200 };
    private readonly ObservableCollection<OpeningLineRow> _lineRows = [];
    private readonly ComboBox _fabric = new() { MinWidth = 180, DisplayMemberPath = nameof(FabricItem.NameAr) };
    private readonly ComboBox _color = new() { MinWidth = 120, DisplayMemberPath = nameof(FabricColor.NameAr) };
    private readonly TextBox _meters = ErpUiFactory.FormField("0");
    private readonly TextBox _rolls = ErpUiFactory.FormField("1");
    private readonly TextBox _unitCost = ErpUiFactory.FormField("0");
    private Guid? _documentId;

    public InventoryOpeningStockFormControl()
    {
        var root = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto, Padding = new Thickness(16) };
        var stack = new StackPanel();
        stack.Children.Add(ErpUiFactory.SectionTitle("مواد أول المدة"));
        stack.Children.Add(ErpUxFactory.InfoBanner("أدخل أرصدة الافتتاح — يمكن إضافة مئات الأسطر قبل الترحيل."));

        stack.Children.Add(ErpUiFactory.BuildFormGrid(
            ("المستودع *", _warehouse),
            ("تاريخ الافتتاح", Wrap(_date)),
            ("مرجع", _reference),
            ("العملة", _currency),
            ("ملاحظات", _notes)));

        ErpUiFactory.AddGridColumn(_lines, "القماش", nameof(OpeningLineRow.FabricName), "*", null);
        ErpUiFactory.AddGridColumn(_lines, "اللون", nameof(OpeningLineRow.ColorName), 100, null);
        ErpUiFactory.AddGridColumn(_lines, "الأمتار", nameof(OpeningLineRow.QuantityMeters), 80, null);
        ErpUiFactory.AddGridColumn(_lines, "Rolls", nameof(OpeningLineRow.RollCount), 60, null);
        ErpUiFactory.AddGridColumn(_lines, "التكلفة/م", nameof(OpeningLineRow.UnitCost), 80, null);
        ErpUiFactory.AddGridColumn(_lines, "القيمة", nameof(OpeningLineRow.TotalValue), 90, null);
        _lines.ItemsSource = _lineRows;
        stack.Children.Add(_lines);

        var addRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 8, 0, 0) };
        _fabric.SelectionChanged += async (_, _) => await LoadColorsAsync();
        addRow.Children.Add(_fabric);
        addRow.Children.Add(_color);
        addRow.Children.Add(_meters);
        addRow.Children.Add(_rolls);
        addRow.Children.Add(_unitCost);
        var addBtn = new Button { Content = "إضافة", Margin = new Thickness(8, 0, 0, 0) };
        addBtn.Click += (_, _) => AddLine();
        addRow.Children.Add(addBtn);
        stack.Children.Add(addRow);

        var actions = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 16, 0, 0) };
        var saveBtn = new Button { Content = "حفظ المستند", Style = (Style)System.Windows.Application.Current.Resources["PrimaryButtonStyle"]!, Margin = new Thickness(0, 0, 8, 0) };
        saveBtn.Click += async (_, _) => await SaveAsync();
        var postBtn = new Button { Content = "ترحيل للمخزون", Style = (Style)System.Windows.Application.Current.Resources["SecondaryButtonStyle"]! };
        postBtn.Click += async (_, _) => await PostAsync();
        actions.Children.Add(saveBtn);
        actions.Children.Add(postBtn);
        stack.Children.Add(actions);

        root.Content = stack;
        Content = root;
        Loaded += async (_, _) => await InitAsync();
    }

    private static UIElement Wrap(UIElement c) => new Border { Child = c, Padding = new Thickness(0, 4, 0, 0) };

    private async Task InitAsync()
    {
        if (!AppServices.IsInitialized) return;
        var wh = await InventoryUiService.Instance.GetWarehousesAsync();
        if (wh.IsSuccess && wh.Value is not null)
        {
            _warehouse.ItemsSource = wh.Value;
            var pre = InventoryNavigationContext.TakePreselectedOpeningWarehouse();
            if (pre.HasValue)
                _warehouse.SelectedItem = wh.Value.FirstOrDefault(w => w.Id == pre.Value);
        }
        var fabrics = await InventoryUiService.Instance.GetFabricCatalogAsync();
        if (fabrics.IsSuccess && fabrics.Value is not null)
            _fabric.ItemsSource = fabrics.Value;
    }

    private async Task LoadColorsAsync()
    {
        if (_fabric.SelectedItem is not FabricItem item) { _color.ItemsSource = null; return; }
        var colors = await InventoryUiService.Instance.GetFabricColorsAsync(item.Id);
        _color.ItemsSource = colors.IsSuccess ? colors.Value : [];
    }

    private void AddLine()
    {
        if (_fabric.SelectedItem is not FabricItem fabric || _color.SelectedItem is not FabricColor color)
        {
            MessageBox.Show("اختر القماش واللون.", "تحقق", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        if (!decimal.TryParse(_meters.Text, out var m) || m <= 0) return;
        if (!int.TryParse(_rolls.Text, out var r) || r <= 0) r = 1;
        if (!decimal.TryParse(_unitCost.Text, out var cost) || cost < 0) cost = 0;

        _lineRows.Add(new OpeningLineRow
        {
            FabricItemId = fabric.Id,
            FabricColorId = color.Id,
            FabricName = fabric.NameAr,
            ColorName = color.NameAr,
            QuantityMeters = m,
            RollCount = r,
            UnitCost = cost
        });
    }

    private async Task SaveAsync()
    {
        if (_warehouse.SelectedItem is not WarehouseListExtendedDto wh)
        {
            MessageBox.Show("اختر المستودع.", "تحقق", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        if (_lineRows.Count == 0)
        {
            MessageBox.Show("أضف سطراً واحداً على الأقل.", "تحقق", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var lines = _lineRows.Select(l => new OpeningStockLineCommand(
            l.FabricItemId, l.FabricColorId, l.QuantityMeters, l.RollCount, l.UnitCost)).ToList();

        var result = await InventoryUiService.Instance.CreateOpeningStockAsync(new CreateOpeningStockCommand(
            Guid.Empty, wh.Id, _date.SelectedDate ?? DateTime.Today,
            _reference.Text.Trim(), _currency.Text.Trim(), _notes.Text.Trim(), lines));

        if (ApplicationResultPresenter.Present(result) && result.IsSuccess)
        {
            _documentId = result.Value;
            InventoryListRefreshHub.RequestRefresh();
        }
    }

    private async Task PostAsync()
    {
        if (!_documentId.HasValue)
        {
            MessageBox.Show("احفظ المستند أولاً.", "تحقق", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        var result = await InventoryUiService.Instance.PostOpeningStockAsync(_documentId.Value);
        if (ApplicationResultPresenter.Present(result))
        {
            InventoryListRefreshHub.RequestRefresh();
            InventoryPopupService.CompleteSuccess();
        }
    }

    private sealed class OpeningLineRow
    {
        public Guid FabricItemId { get; init; }
        public Guid FabricColorId { get; init; }
        public string FabricName { get; init; } = "";
        public string ColorName { get; init; } = "";
        public decimal QuantityMeters { get; init; }
        public int RollCount { get; init; }
        public decimal UnitCost { get; init; }
        public decimal TotalValue => QuantityMeters * UnitCost;
    }
}
