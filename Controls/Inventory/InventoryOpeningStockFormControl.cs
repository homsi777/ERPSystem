using ERPSystem.Application.Commands.Finance;
using ERPSystem.Application.Common;
using ERPSystem.Application.DTOs.Inventory;
using ERPSystem.Core;
using ERPSystem.Domain.Entities.Catalog;
using ERPSystem.Domain.Entities.Finance;
using ERPSystem.Helpers;
using ERPSystem.Services;
using ERPSystem.Services.Inventory;
using ERPSystem.Services.Finance;
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
        if (WpfGeneralManagerAccess.CanViewSensitivePricing)
        {
            ErpUiFactory.AddGridColumn(_lines, "التكلفة/م", nameof(OpeningLineRow.UnitCost), 80, null);
            ErpUiFactory.AddGridColumn(_lines, "القيمة", nameof(OpeningLineRow.TotalValue), 90, null);
        }
        _lines.ItemsSource = _lineRows;
        stack.Children.Add(_lines);

        var addRow = new Grid { Margin = new Thickness(0, 8, 0, 0) };
        addRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star), MinWidth = 240 });
        addRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(140) });
        addRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(110) });
        addRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(90) });
        if (WpfGeneralManagerAccess.CanViewSensitivePricing)
            addRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(110) });
        addRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(100) });

        _fabric.SelectionChanged += async (_, _) => await LoadColorsAsync();
        AddEntryField(addRow, "القماش *", _fabric, 0);
        AddEntryField(addRow, "اللون *", _color, 1);
        AddEntryField(addRow, "الأمتار *", _meters, 2);
        AddEntryField(addRow, "Rolls *", _rolls, 3);

        var buttonColumn = 4;
        if (WpfGeneralManagerAccess.CanViewSensitivePricing)
        {
            AddEntryField(addRow, "التكلفة/م", _unitCost, 4);
            buttonColumn = 5;
        }

        var addBtn = new Button
        {
            Content = "إضافة",
            Margin = new Thickness(8, 20, 0, 0),
            VerticalAlignment = VerticalAlignment.Bottom
        };
        addBtn.Click += (_, _) => AddLine();
        Grid.SetColumn(addBtn, buttonColumn);
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

    private static void AddEntryField(Grid row, string label, FrameworkElement field, int column)
    {
        field.HorizontalAlignment = HorizontalAlignment.Stretch;
        var container = new StackPanel { Margin = new Thickness(0, 0, 8, 0) };
        container.Children.Add(new TextBlock
        {
            Text = label,
            Margin = new Thickness(0, 0, 0, 4)
        });
        container.Children.Add(field);
        Grid.SetColumn(container, column);
        row.Children.Add(container);
    }

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

        var lines = _lineRows.Select(l => new OpeningBalanceLineInput
        {
            WarehouseId = wh.Id,
            WarehouseName = wh.NameAr,
            FabricItemId = l.FabricItemId,
            FabricColorId = l.FabricColorId,
            ItemName = l.FabricName,
            ColorName = l.ColorName,
            Quantity = l.QuantityMeters,
            RollCount = l.RollCount,
            UnitCost = l.UnitCost,
            Debit = l.TotalValue,
            Credit = 0m
        }).ToList();

        var result = await OpeningBalanceUiService.Instance.CreateAsync(new CreateOpeningBalanceCommand
        {
            Type = OpeningBalanceType.OpeningStock,
            Source = OpeningBalanceSource.Manual,
            OpeningDate = ApplicationDateNormalizer.ToUtcDate(_date.SelectedDate) ?? DateTime.UtcNow,
            CurrencyCode = _currency.Text.Trim().ToUpperInvariant(),
            ExchangeRate = 1m,
            Reference = _reference.Text.Trim(),
            Notes = _notes.Text.Trim(),
            Lines = lines
        });

        if (ApplicationResultPresenter.Present(result) && result.IsSuccess)
        {
            _documentId = result.Value?.Id;
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
        var submit = await OpeningBalanceUiService.Instance.SubmitAsync(_documentId.Value);
        if (!ApplicationResultPresenter.Present(submit))
            return;

        var approve = await OpeningBalanceUiService.Instance.ApproveAsync(
            _documentId.Value, "اعتماد رصيد مخزون من شاشة مواد أول المدة");
        if (!ApplicationResultPresenter.Present(approve))
            return;

        var post = await OpeningBalanceUiService.Instance.PostAsync(_documentId.Value, lockAfterPost: true);
        if (ApplicationResultPresenter.Present(post))
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
