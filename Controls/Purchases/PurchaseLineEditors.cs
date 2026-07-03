using ERPSystem.Application.Commands.Purchases;
using ERPSystem.Application.DTOs.Accounting;
using ERPSystem.Application.DTOs.Purchases;
using ERPSystem.Domain.Enums;
using ERPSystem.Helpers;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using WpfApplication = System.Windows.Application;

namespace ERPSystem.Controls.Purchases;

internal static class PurchaseLineEditors
{
    internal sealed class InventoryLineEditor
    {
        public Guid? FabricItemId { get; private set; }
        public Guid? FabricColorId { get; private set; }
        public string ItemName { get; private set; } = "";
        public string ColorName { get; private set; } = "";
        public Border Row { get; }

        private readonly ComboBox _fabric;
        private readonly ComboBox _color;
        private readonly TextBox _qty;
        private readonly TextBox _unitCost;
        private readonly Func<Guid, Task<IReadOnlyList<PurchaseFabricColorPickDto>>> _loadColors;

        public InventoryLineEditor(
            IReadOnlyList<PurchaseFabricPickDto> fabrics,
            Func<Guid, Task<IReadOnlyList<PurchaseFabricColorPickDto>>> loadColors,
            Action<InventoryLineEditor> onRemove)
        {
            _loadColors = loadColors;
            _fabric = new ComboBox
            {
                MinWidth = 200,
                IsEditable = false,
                ItemsSource = fabrics,
                DisplayMemberPath = nameof(PurchaseFabricPickDto.Display),
                SelectedValuePath = nameof(PurchaseFabricPickDto.Id),
                Style = S("EnterpriseComboBoxStyle")
            };
            _color = new ComboBox
            {
                MinWidth = 140,
                IsEditable = false,
                Style = S("EnterpriseComboBoxStyle")
            };
            _qty = ErpUiFactory.FormField("0");
            _unitCost = ErpUiFactory.FormField("0");

            var removeBtn = new Button
            {
                Content = "حذف",
                Style = S("GhostButtonStyle"),
                Height = 32,
                VerticalAlignment = VerticalAlignment.Bottom
            };

            var grid = BuildGrid(
                ("صنف مخزون", _fabric),
                ("اللون", _color),
                ("الكمية (م)", _qty),
                ("سعر الوحدة", _unitCost),
                ("", removeBtn));
            Row = ErpUiFactory.Card(grid);
            removeBtn.Click += (_, _) => onRemove(this);
            _fabric.SelectionChanged += async (_, _) => await OnFabricChangedAsync();
        }

        public async Task LoadFromAsync(
            Guid? fabricItemId,
            Guid? fabricColorId,
            string itemName,
            string colorName,
            decimal quantity,
            decimal unitCost)
        {
            ItemName = itemName;
            ColorName = colorName;
            _qty.Text = quantity.ToString(CultureInfo.InvariantCulture);
            _unitCost.Text = unitCost.ToString(CultureInfo.InvariantCulture);
            if (fabricItemId is Guid fid)
            {
                _fabric.SelectedValue = fid;
                await OnFabricChangedAsync();
                if (fabricColorId is Guid cid)
                    _color.SelectedValue = cid;
            }
        }

        public bool TryRead(out PurchaseInvoiceLineInput line)
        {
            line = null!;
            if (_fabric.SelectedValue is not Guid fabricId || fabricId == Guid.Empty)
                return false;
            if (!TryParse(_qty.Text, out var qty) || qty <= 0)
                return false;
            if (!TryParse(_unitCost.Text, out var cost) || cost <= 0)
                return false;

            FabricItemId = fabricId;
            FabricColorId = _color.SelectedValue as Guid?;
            ItemName = (_fabric.SelectedItem as PurchaseFabricPickDto)?.NameAr ?? "";
            ColorName = (_color.SelectedItem as PurchaseFabricColorPickDto)?.NameAr ?? "";

            line = new PurchaseInvoiceLineInput
            {
                LineType = (int)PurchaseLineType.Inventory,
                FabricItemId = fabricId,
                FabricColorId = FabricColorId,
                Description = string.IsNullOrWhiteSpace(ColorName) ? ItemName : $"{ItemName} — {ColorName}",
                QuantityMeters = qty,
                RollCount = 1,
                UnitPrice = cost
            };
            return true;
        }

        public bool TryReadOrderLine(out PurchaseOrderLineInput line)
        {
            line = null!;
            if (_fabric.SelectedValue is not Guid fabricId || fabricId == Guid.Empty)
                return false;
            if (!TryParse(_qty.Text, out var qty) || qty <= 0)
                return false;
            if (!TryParse(_unitCost.Text, out var cost) || cost <= 0)
                return false;

            line = new PurchaseOrderLineInput
            {
                FabricItemId = fabricId,
                Description = (_fabric.SelectedItem as PurchaseFabricPickDto)?.NameAr ?? "",
                Quantity = qty,
                UnitCost = cost
            };
            return true;
        }

        private async Task OnFabricChangedAsync()
        {
            if (_fabric.SelectedValue is not Guid fabricId)
            {
                _color.ItemsSource = null;
                return;
            }

            var colors = await _loadColors(fabricId);
            _color.ItemsSource = colors;
            _color.DisplayMemberPath = nameof(PurchaseFabricColorPickDto.Display);
            _color.SelectedValuePath = nameof(PurchaseFabricColorPickDto.Id);
            if (colors.Count > 0)
                _color.SelectedIndex = 0;
        }

        private static bool TryParse(string text, out decimal value) =>
            decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out value)
            || decimal.TryParse(text, out value);
    }

    internal sealed class ExpenseLineEditor
    {
        public Border Row { get; }

        private readonly ComboBox _account;
        private readonly TextBox _description;
        private readonly TextBox _amount;

        public ExpenseLineEditor(
            IReadOnlyList<AccountListDto> expenseAccounts,
            Action<ExpenseLineEditor> onRemove)
        {
            _account = new ComboBox
            {
                MinWidth = 220,
                IsEditable = false,
                ItemsSource = expenseAccounts.Where(a => a.IsPostable && a.IsActive).ToList(),
                DisplayMemberPath = nameof(AccountListDto.NameAr),
                SelectedValuePath = nameof(AccountListDto.Id),
                Style = S("EnterpriseComboBoxStyle")
            };
            if (_account.Items.Count > 0) _account.SelectedIndex = 0;
            _description = ErpUiFactory.FormField("وصف المصروف");
            _amount = ErpUiFactory.FormField("0");

            var removeBtn = new Button
            {
                Content = "حذف",
                Style = S("GhostButtonStyle"),
                Height = 32,
                VerticalAlignment = VerticalAlignment.Bottom
            };

            var grid = BuildGrid(
                ("مصروف", _account),
                ("الوصف", _description),
                ("المبلغ", _amount),
                ("", removeBtn));
            Row = ErpUiFactory.Card(grid);
            removeBtn.Click += (_, _) => onRemove(this);
        }

        public void LoadFrom(Guid? accountId, string description, decimal amount)
        {
            if (accountId is Guid id)
                _account.SelectedValue = id;
            _description.Text = description;
            _amount.Text = amount.ToString(CultureInfo.InvariantCulture);
        }

        public bool TryRead(out PurchaseInvoiceLineInput line)
        {
            line = null!;
            if (_account.SelectedValue is not Guid accountId || accountId == Guid.Empty)
                return false;
            if (!TryParse(_amount.Text, out var amount) || amount <= 0)
                return false;

            line = new PurchaseInvoiceLineInput
            {
                LineType = (int)PurchaseLineType.Expense,
                ExpenseAccountId = accountId,
                Description = string.IsNullOrWhiteSpace(_description.Text)
                    ? (_account.SelectedItem as AccountListDto)?.NameAr ?? "مصروف"
                    : _description.Text.Trim(),
                QuantityMeters = 1,
                UnitPrice = amount
            };
            return true;
        }

        private static bool TryParse(string text, out decimal value) =>
            decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out value)
            || decimal.TryParse(text, out value);
    }

    private static Grid BuildGrid(params (string Label, UIElement Control)[] fields)
    {
        var grid = new Grid { Margin = new Thickness(0, 0, 0, 8) };
        for (var i = 0; i < fields.Length; i++)
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        for (var i = 0; i < fields.Length; i++)
        {
            var (label, control) = fields[i];
            var stack = new StackPanel { Margin = new Thickness(i == 0 ? 0 : 6, 0, 0, 0) };
            if (!string.IsNullOrEmpty(label))
            {
                stack.Children.Add(new TextBlock
                {
                    Text = label,
                    FontSize = 11,
                    Foreground = (System.Windows.Media.Brush)WpfApplication.Current.Resources["TextSecondaryBrush"]!,
                    Margin = new Thickness(0, 0, 0, 4)
                });
            }
            stack.Children.Add(control);
            Grid.SetColumn(stack, i);
            grid.Children.Add(stack);
        }

        return grid;
    }

    private static Style S(string key) => (Style)WpfApplication.Current.Resources[key]!;
}
