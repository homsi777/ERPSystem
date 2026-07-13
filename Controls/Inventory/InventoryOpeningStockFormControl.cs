using ERPSystem.Application.Commands.Finance;
using ERPSystem.Application.Common;
using ERPSystem.Application.DTOs.Inventory;
using ERPSystem.Core;
using ERPSystem.Domain.Entities.Finance;
using ERPSystem.Helpers;
using ERPSystem.Services;
using ERPSystem.Services.Finance;
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
    private readonly TextBox _fabricName = ErpUiFactory.FormField("");
    private readonly TextBox _colorName = ErpUiFactory.FormField("");
    private readonly TextBox _meters = ErpUiFactory.FormField("");
    private readonly TextBox _rolls = ErpUiFactory.FormField("");
    private readonly TextBox _unitCost = ErpUiFactory.FormField("");
    private readonly Button _saveButton = new();
    private readonly Button _postButton = new();
    private bool _isSaving;
    private bool _isPosting;
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

        AddEntryField(addRow, "القماش *", _fabricName, 0);
        AddEntryField(addRow, "اللون *", _colorName, 1);
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
        _saveButton.Content = "حفظ المستند";
        _saveButton.Style = (Style)System.Windows.Application.Current.Resources["PrimaryButtonStyle"]!;
        _saveButton.Margin = new Thickness(0, 0, 8, 0);
        _saveButton.Click += async (_, _) => await SaveAsync();
        _postButton.Content = "ترحيل للمخزون";
        _postButton.Style = (Style)System.Windows.Application.Current.Resources["SecondaryButtonStyle"]!;
        _postButton.Click += async (_, _) => await PostAsync();
        actions.Children.Add(_saveButton);
        actions.Children.Add(_postButton);
        stack.Children.Add(actions);

        root.Content = stack;
        Content = root;
        WireEnterNavigation();
        Loaded += async (_, _) => await InitAsync();
    }

    private void WireEnterNavigation()
    {
        EnterFocusNavigation.WireChain(
            [_warehouse, _date, _reference, _currency, _notes],
            onLastEnter: () => EnterFocusNavigation.FocusNext(_fabricName));

        if (WpfGeneralManagerAccess.CanViewSensitivePricing)
        {
            EnterFocusNavigation.WireChain(
                [_fabricName, _colorName, _meters, _rolls, _unitCost],
                onLastEnter: AddLineAndRefocusFabric);
        }
        else
        {
            EnterFocusNavigation.WireChain(
                [_fabricName, _colorName, _meters, _rolls],
                onLastEnter: AddLineAndRefocusFabric);
        }
    }

    private void AddLineAndRefocusFabric()
    {
        AddLine();
        EnterFocusNavigation.FocusNext(_fabricName);
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
    }

    private void AddLine()
    {
        var fabricName = _fabricName.Text.Trim();
        var colorName = _colorName.Text.Trim();
        if (string.IsNullOrWhiteSpace(fabricName) || string.IsNullOrWhiteSpace(colorName))
        {
            MessageBox.Show("اكتب اسم القماش واللون.", "تحقق", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        if (!decimal.TryParse(_meters.Text, out var m) || m <= 0)
        {
            MessageBox.Show("اكتب إجمالي الأمتار بقيمة أكبر من صفر.", "تحقق", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        if (!int.TryParse(_rolls.Text, out var r) || r <= 0)
        {
            MessageBox.Show("اكتب عدد الرولات بقيمة صحيحة أكبر من صفر.", "تحقق", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        var cost = 0m;
        if (!string.IsNullOrWhiteSpace(_unitCost.Text) &&
            (!decimal.TryParse(_unitCost.Text, out cost) || cost < 0))
        {
            MessageBox.Show("اكتب تكلفة صحيحة أو اتركها فارغة.", "تحقق", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        _lineRows.Add(new OpeningLineRow
        {
            FabricName = fabricName,
            ColorName = colorName,
            QuantityMeters = m,
            RollCount = r,
            UnitCost = cost
        });

        _fabricName.Clear();
        _colorName.Clear();
        _meters.Clear();
        _rolls.Clear();
        _unitCost.Clear();
        _fabricName.Focus();
    }

    private async Task SaveAsync()
    {
        if (_isSaving)
            return;

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

        _isSaving = true;
        _saveButton.IsEnabled = false;
        try
        {
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
                OpeningBalanceListRefreshHub.RequestRefresh();
                InventoryListRefreshHub.RequestRefresh();
                MockInteractionService.ShowInfo(
                    $"تم حفظ مستند مواد أول المدة رقم {result.Value?.Number ?? "—"}.\nيمكنك الآن الضغط على «ترحيل للمخزون».",
                    "مواد أول المدة");
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"تعذّر حفظ مستند مواد أول المدة.\n\n{ex.Message}",
                "مواد أول المدة", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            _isSaving = false;
            _saveButton.IsEnabled = true;
        }
    }

    private async Task PostAsync()
    {
        if (_isPosting)
            return;

        if (!_documentId.HasValue)
        {
            MessageBox.Show("احفظ المستند أولاً.", "تحقق", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        _isPosting = true;
        _postButton.IsEnabled = false;
        try
        {
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
                OpeningBalanceListRefreshHub.RequestRefresh();
                InventoryListRefreshHub.RequestRefresh();
                InventoryPopupService.CompleteSuccess();
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"تعذّر ترحيل مستند مواد أول المدة.\n\n{ex.Message}",
                "مواد أول المدة", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            _isPosting = false;
            _postButton.IsEnabled = true;
        }
    }

    private sealed class OpeningLineRow
    {
        public Guid? FabricItemId { get; init; }
        public Guid? FabricColorId { get; init; }
        public string FabricName { get; init; } = "";
        public string ColorName { get; init; } = "";
        public decimal QuantityMeters { get; init; }
        public int RollCount { get; init; }
        public decimal UnitCost { get; init; }
        public decimal TotalValue => QuantityMeters * UnitCost;
    }
}
