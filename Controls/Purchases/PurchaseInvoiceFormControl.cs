using ERPSystem.Application.Commands.Purchases;
using ERPSystem.Application.DTOs.Accounting;
using ERPSystem.Application.DTOs.Purchases;
using ERPSystem.Application.DTOs.Suppliers;
using ERPSystem.Core;
using ERPSystem.Domain.Enums;
using ERPSystem.Helpers;
using ERPSystem.Services;
using ERPSystem.Services.Purchases;
using ERPSystem.Services.Suppliers;
using ERPSystem.Services.Sales;
using System.Windows;
using System.Windows.Controls;

namespace ERPSystem.Controls.Purchases;

public sealed class PurchaseInvoiceFormControl : UserControl
{
    private readonly TextBox _txtNumber = ErpUiFactory.FormField("");
    private readonly ComboBox _cmbSupplier = new() { Height = 36, MinWidth = 240, Style = S("EnterpriseComboBoxStyle") };
    private readonly DatePicker _dpDate = ErpUiFactory.FormDate(DateTime.Today);
    private readonly DatePicker _dpDue = ErpUiFactory.FormDate(DateTime.Today.AddDays(30));
    private readonly TextBox _txtSupplierRef = ErpUiFactory.FormField("");
    private readonly ComboBox _cmbWarehouse = new() { Height = 36, MinWidth = 200, Style = S("EnterpriseComboBoxStyle") };
    private readonly TextBox _txtNotes = ErpUiFactory.FormField("");
    private readonly StackPanel _linesHost = new();
    private readonly TextBlock _txtSummary = new() { Margin = new Thickness(0, 8, 0, 0) };
    private readonly Button _btnSave = new() { Content = "حفظ مسودة", Style = S("PrimaryButtonStyle"), MinWidth = 120, Height = 36 };
    private readonly Button _btnPost = new() { Content = "ترحيل الفاتورة", Style = S("PrimaryButtonStyle"), MinWidth = 120, Height = 36, Margin = new Thickness(8, 0, 0, 0) };
    private readonly Button _btnCancel = new() { Content = "إلغاء", Style = S("SecondaryButtonStyle"), MinWidth = 100, Height = 36, Margin = new Thickness(8, 0, 0, 0) };

    private readonly List<PurchaseLineEditors.InventoryLineEditor> _inventoryLines = [];
    private readonly List<PurchaseLineEditors.ExpenseLineEditor> _expenseLines = [];

    private Guid? _editId;
    private IReadOnlyList<PurchaseFabricPickDto> _fabrics = [];
    private IReadOnlyList<AccountListDto> _expenseAccounts = [];
    private bool _readOnly;

    public PurchaseInvoiceFormControl()
    {
        var addLinePanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 8, 0, 0) };
        var btnAddInventory = new Button { Content = "+ صنف مخزون", Style = S("GhostButtonStyle"), Height = 32 };
        var btnAddExpense = new Button { Content = "+ مصروف", Style = S("GhostButtonStyle"), Height = 32, Margin = new Thickness(8, 0, 0, 0) };
        btnAddInventory.Click += (_, _) => AddInventoryLine();
        btnAddExpense.Click += (_, _) => AddExpenseLine();
        addLinePanel.Children.Add(btnAddInventory);
        addLinePanel.Children.Add(btnAddExpense);

        var actions = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 16, 0, 0) };
        actions.Children.Add(_btnSave);
        actions.Children.Add(_btnPost);
        actions.Children.Add(_btnCancel);

        var stack = new StackPanel { Margin = new Thickness(16) };
        stack.Children.Add(ErpUiFactory.SectionTitle("فاتورة شراء"));
        stack.Children.Add(ErpUiFactory.Card(ErpUiFactory.BuildFormGrid(
            ("رقم الفاتورة", _txtNumber),
            ("المورد", _cmbSupplier),
            ("تاريخ الفاتورة", _dpDate),
            ("تاريخ الاستحقاق", _dpDue),
            ("مرجع المورد", _txtSupplierRef),
            ("المستودع", _cmbWarehouse),
            ("ملاحظات", _txtNotes))));
        stack.Children.Add(ErpUiFactory.Card(_linesHost));
        stack.Children.Add(addLinePanel);
        stack.Children.Add(_txtSummary);
        stack.Children.Add(actions);
        Content = new ScrollViewer { Content = stack };

        Loaded += OnLoaded;
        _btnSave.Click += async (_, _) => await SaveDraftAsync();
        _btnPost.Click += async (_, _) => await PostAsync();
        _btnCancel.Click += async (_, _) =>
        {
            if (_editId is Guid id)
                await PurchaseUiService.Instance.CancelInvoiceAsync(id);
            MockInteractionService.Navigate(AppModule.Purchases, "Invoices");
        };
        _cmbSupplier.SelectionChanged += (_, _) => UpdateDueDateFromSupplier();
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        await LoadLookupsAsync();
        _editId = PurchaseNavigationContext.EditInvoiceId;
        if (_editId is Guid id)
        {
            var result = await PurchaseUiService.Instance.GetInvoiceDetailsAsync(id);
            if (!ApplicationResultPresenter.Present(result)) return;
            var inv = result.Value!;
            _txtNumber.Text = inv.InvoiceNumber;
            _txtNumber.IsReadOnly = true;
            _dpDate.SelectedDate = inv.InvoiceDate;
            _dpDue.SelectedDate = inv.DueDate;
            _txtSupplierRef.Text = inv.SupplierReference ?? "";
            _txtNotes.Text = inv.Notes ?? "";
            SelectSupplier(inv.SupplierId);
            SelectWarehouse(inv.WarehouseId);
            foreach (var l in inv.Lines)
            {
                if (l.LineType == PurchaseLineType.Expense)
                {
                    var editor = AddExpenseLine();
                    editor.LoadFrom(l.ExpenseAccountId, l.Description, l.LineTotal);
                }
                else
                {
                    var editor = AddInventoryLine();
                    await editor.LoadFromAsync(l.FabricItemId, l.FabricColorId, l.FabricItemName ?? l.Description, "", l.QuantityMeters, l.UnitPrice);
                }
            }
            _readOnly = inv.IsReadOnly;
            _btnSave.IsEnabled = !_readOnly;
            _btnPost.IsEnabled = !_readOnly;
            _btnCancel.IsEnabled = inv.Status == PurchaseInvoiceStatus.Draft;
            SetLinesReadOnly(_readOnly);
        }
        else
        {
            _txtNumber.Text = await PurchaseUiService.Instance.NextInvoiceNumberAsync();
        }
        UpdateSummary();
    }

    private async Task LoadLookupsAsync()
    {
        var fabrics = await PurchaseUiService.Instance.GetFabricItemsAsync();
        if (fabrics.IsSuccess)
            _fabrics = fabrics.Value!;

        var accounts = await PurchaseUiService.Instance.GetExpenseAccountsAsync();
        if (accounts.IsSuccess)
            _expenseAccounts = accounts.Value!;

        var suppliers = await SupplierUiService.Instance.GetListAsync(null, pageSize: 200);
        if (suppliers.IsSuccess)
        {
            _cmbSupplier.Items.Clear();
            foreach (var s in suppliers.Value!.Items)
                _cmbSupplier.Items.Add(new ComboBoxItem { Content = s.NameAr, Tag = s });
        }

        var warehouses = await SalesUiService.Instance.GetWarehousesAsync();
        if (warehouses.IsSuccess)
        {
            _cmbWarehouse.Items.Clear();
            foreach (var w in warehouses.Value!)
                _cmbWarehouse.Items.Add(new ComboBoxItem { Content = w.NameAr, Tag = w.Id });
        }
    }

    private PurchaseLineEditors.InventoryLineEditor AddInventoryLine()
    {
        var editor = new PurchaseLineEditors.InventoryLineEditor(
            _fabrics,
            id => PurchaseUiService.Instance.GetFabricColorsAsync(id),
            RemoveInventoryLine);
        _inventoryLines.Add(editor);
        _linesHost.Children.Add(editor.Row);
        return editor;
    }

    private PurchaseLineEditors.ExpenseLineEditor AddExpenseLine()
    {
        var editor = new PurchaseLineEditors.ExpenseLineEditor(_expenseAccounts, RemoveExpenseLine);
        _expenseLines.Add(editor);
        _linesHost.Children.Add(editor.Row);
        return editor;
    }

    private void RemoveInventoryLine(PurchaseLineEditors.InventoryLineEditor editor)
    {
        _inventoryLines.Remove(editor);
        _linesHost.Children.Remove(editor.Row);
        UpdateSummary();
    }

    private void RemoveExpenseLine(PurchaseLineEditors.ExpenseLineEditor editor)
    {
        _expenseLines.Remove(editor);
        _linesHost.Children.Remove(editor.Row);
        UpdateSummary();
    }

    private void UpdateSummary()
    {
        decimal total = 0;
        foreach (var line in _inventoryLines)
            if (line.TryRead(out var input))
                total += input.QuantityMeters * input.UnitPrice;
        foreach (var line in _expenseLines)
            if (line.TryRead(out var input))
                total += input.UnitPrice;
        _txtSummary.Text = $"الإجمالي: {total:N2} ر.س";
    }

    private void SetLinesReadOnly(bool readOnly)
    {
        _linesHost.IsEnabled = !readOnly;
    }

    private async Task SaveDraftAsync()
    {
        var cmd = BuildCommand();
        if (cmd is null) return;
        if (_editId is Guid id)
        {
            var update = new UpdatePurchaseInvoiceDraftCommand
            {
                InvoiceId = id,
                SupplierId = cmd.SupplierId,
                SupplierReference = cmd.SupplierReference,
                InvoiceDate = cmd.InvoiceDate,
                DueDate = cmd.DueDate,
                WarehouseId = cmd.WarehouseId,
                CurrencyCode = cmd.CurrencyCode,
                DiscountAmount = cmd.DiscountAmount,
                TaxAmount = cmd.TaxAmount,
                Notes = cmd.Notes,
                Lines = cmd.Lines
            };
            var result = await PurchaseUiService.Instance.UpdateDraftAsync(update);
            if (ApplicationResultPresenter.Present(result))
            {
                MockInteractionService.ShowSuccess("تم حفظ المسودة.");
                PurchaseListRefreshHub.RequestRefresh();
            }
        }
        else
        {
            var result = await PurchaseUiService.Instance.CreateDraftAsync(cmd);
            if (ApplicationResultPresenter.Present(result))
            {
                _editId = result.Value;
                PurchaseNavigationContext.BeginEdit(_editId.Value);
                MockInteractionService.ShowSuccess("تم إنشاء المسودة.");
                PurchaseListRefreshHub.RequestRefresh();
            }
        }
    }

    private async Task PostAsync()
    {
        if (_editId is not Guid id)
        {
            await SaveDraftAsync();
            if (_editId is null) return;
            id = _editId.Value;
        }
        var userId = AppServices.GetRequiredService<ERPSystem.Application.Abstractions.Services.ICurrentUserService>().UserId ?? Guid.Empty;
        var result = await PurchaseUiService.Instance.PostInvoiceAsync(id, userId);
        if (ApplicationResultPresenter.Present(result))
        {
            MockInteractionService.ShowSuccess($"تم ترحيل الفاتورة.\nقيد اليومية: {result.Value}");
            PurchaseListRefreshHub.RequestRefresh();
            MockInteractionService.Navigate(AppModule.Purchases, "Invoices");
        }
    }

    private CreatePurchaseInvoiceDraftCommand? BuildCommand()
    {
        if (_cmbSupplier.SelectedItem is not ComboBoxItem supItem || supItem.Tag is not SupplierListDto supplier)
        {
            MockInteractionService.ShowWarning("اختر المورد.");
            return null;
        }

        var lines = new List<PurchaseInvoiceLineInput>();
        foreach (var editor in _inventoryLines)
        {
            if (!editor.TryRead(out var line))
            {
                MockInteractionService.ShowWarning("أكمل بيانات سطور المخزون (صنف، كمية، سعر).");
                return null;
            }
            lines.Add(line);
        }
        foreach (var editor in _expenseLines)
        {
            if (!editor.TryRead(out var line))
            {
                MockInteractionService.ShowWarning("أكمل بيانات سطور المصروف (حساب، مبلغ).");
                return null;
            }
            lines.Add(line);
        }

        if (lines.Count == 0)
        {
            MockInteractionService.ShowWarning("أضف سطراً واحداً على الأقل.");
            return null;
        }

        if (lines.Any(l => l.LineType == (int)PurchaseLineType.Inventory) && _cmbWarehouse.SelectedItem is null)
        {
            MockInteractionService.ShowWarning("اختر المستودع لسطور المخزون.");
            return null;
        }

        Guid? warehouseId = _cmbWarehouse.SelectedItem is ComboBoxItem wh ? wh.Tag as Guid? : null;
        return new CreatePurchaseInvoiceDraftCommand
        {
            InvoiceNumber = _txtNumber.Text.Trim(),
            SupplierId = supplier.Id,
            SupplierReference = _txtSupplierRef.Text.Trim(),
            InvoiceDate = _dpDate.SelectedDate ?? DateTime.Today,
            DueDate = _dpDue.SelectedDate ?? DateTime.Today.AddDays(30),
            WarehouseId = warehouseId,
            Notes = _txtNotes.Text.Trim(),
            Lines = lines
        };
    }

    private void SelectSupplier(Guid id)
    {
        foreach (ComboBoxItem item in _cmbSupplier.Items)
            if (item.Tag is SupplierListDto s && s.Id == id)
                _cmbSupplier.SelectedItem = item;
    }

    private void SelectWarehouse(Guid? id)
    {
        if (!id.HasValue) return;
        foreach (ComboBoxItem item in _cmbWarehouse.Items)
            if (item.Tag is Guid w && w == id)
                _cmbWarehouse.SelectedItem = item;
    }

    private void UpdateDueDateFromSupplier()
    {
        if (_cmbSupplier.SelectedItem is ComboBoxItem item && item.Tag is SupplierListDto s)
            _dpDue.SelectedDate = (_dpDate.SelectedDate ?? DateTime.Today).AddDays(s.PaymentTermsDays);
    }

    private static Style S(string key) => (Style)System.Windows.Application.Current.Resources[key]!;
}
