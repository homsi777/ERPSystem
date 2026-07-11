using ERPSystem.Application.Commands.Finance;
using ERPSystem.Application.DTOs.Finance;
using ERPSystem.Domain.Entities.Finance;
using ERPSystem.Helpers;
using ERPSystem.Services;
using ERPSystem.Services.Finance;
using Microsoft.Win32;
using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace ERPSystem.Controls.Finance;

public sealed class OpeningBalanceFormControl : UserControl
{
    private readonly OpeningBalanceType _type;
    private readonly DatePicker _date = ErpUiFactory.FormDate(DateTime.Today);
    private readonly TextBox _currency = ErpUiFactory.FormField("USD");
    private readonly TextBox _rate = ErpUiFactory.FormField("1");
    private readonly TextBox _reference = ErpUiFactory.FormField();
    private readonly TextBox _description = ErpUiFactory.FormField();
    private readonly TextBox _notes = ErpUiFactory.FormField();
    private readonly DataGrid _linesGrid = ErpUiFactory.BuildGrid(new List<OpeningBalanceLineInput>(), false);
    private readonly List<OpeningBalanceLineInput> _lines = [];
    private OpeningBalanceLookupsDto? _lookups;

    public OpeningBalanceFormControl()
    {
        _type = OpeningBalanceNavigationContext.FormType ?? OpeningBalanceType.CustomerReceivable;
        var root = new StackPanel { Margin = new Thickness(8) };
        root.Children.Add(ErpUiFactory.SectionTitle(OpeningBalanceDisplay.TypeName(_type)));
        root.Children.Add(ErpUiFactory.Card(ErpUiFactory.BuildFormGrid(
            ("تاريخ الافتتاح", _date),
            ("العملة", _currency),
            ("سعر الصرف", _rate),
            ("المرجع", _reference),
            ("الوصف", _description),
            ("ملاحظات", _notes))));

        var toolbar = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 12, 0, 8) };
        var addBtn = new Button { Content = "إضافة سطر", Margin = new Thickness(0, 0, 8, 0), Padding = new Thickness(12, 6, 12, 6) };
        addBtn.Click += (_, _) => AddLineDialog();
        var importBtn = new Button { Content = "استيراد Excel", Padding = new Thickness(12, 6, 12, 6) };
        importBtn.Click += async (_, _) => await ImportExcelAsync();
        toolbar.Children.Add(addBtn);
        toolbar.Children.Add(importBtn);
        root.Children.Add(toolbar);
        root.Children.Add(ErpUiFactory.Card(_linesGrid));

        var save = new Button
        {
            Content = "حفظ المسودة",
            Margin = new Thickness(0, 12, 0, 0),
            Padding = new Thickness(16, 8, 16, 8),
            HorizontalAlignment = HorizontalAlignment.Left
        };
        save.Click += async (_, _) => await SaveAsync();
        root.Children.Add(save);

        Content = new ScrollViewer { Content = root, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        Loaded += async (_, _) => await LoadLookupsAsync();
    }

    private async Task LoadLookupsAsync()
    {
        if (!AppServices.IsInitialized) return;
        var result = await OpeningBalanceUiService.Instance.GetLookupsAsync();
        if (result.IsSuccess) _lookups = result.Value;
    }

    private void AddLineDialog()
    {
        var panel = BuildLineFields(out var party, out var account, out var warehouse, out var item,
            out var qty, out var cost, out var debit, out var credit, out var desc);
        var ok = new Button { Content = "إضافة", Margin = new Thickness(0, 8, 0, 0) };
        ok.Click += (_, _) =>
        {
            _lines.Add(new OpeningBalanceLineInput
            {
                PartyId = ResolveId(party.Text, _type is OpeningBalanceType.CustomerReceivable ? _lookups?.Customers :
                    _type is OpeningBalanceType.SupplierPayable ? _lookups?.Suppliers :
                    _type is OpeningBalanceType.Capital ? _lookups?.Partners : null),
                PartyName = party.Text,
                AccountId = ResolveId(account.Text, _type == OpeningBalanceType.Cash ? _lookups?.Cashboxes : _lookups?.Accounts),
                AccountName = account.Text,
                WarehouseId = ResolveId(warehouse.Text, _lookups?.Warehouses),
                WarehouseName = warehouse.Text,
                ItemName = item.Text,
                Quantity = decimal.TryParse(qty.Text, out var q) ? q : null,
                UnitCost = decimal.TryParse(cost.Text, out var c) ? c : null,
                Debit = decimal.TryParse(debit.Text, out var d) ? d : 0,
                Credit = decimal.TryParse(credit.Text, out var cr) ? cr : 0,
                Description = desc.Text
            });
            RefreshGrid();
        };
        panel.Children.Add(ok);
        var win = new Window
        {
            Title = "سطر جديد",
            Content = panel,
            Width = 480,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Owner = Window.GetWindow(this)
        };
        win.ShowDialog();
    }

    private static Guid? ResolveId(string text, IReadOnlyList<OpeningBalanceLookupItemDto>? pool)
    {
        if (pool is null || string.IsNullOrWhiteSpace(text)) return null;
        var m = pool.FirstOrDefault(p =>
            p.Name.Equals(text, StringComparison.OrdinalIgnoreCase) ||
            p.Code.Equals(text, StringComparison.OrdinalIgnoreCase));
        return m?.Id;
    }

    private StackPanel BuildLineFields(
        out TextBox party, out TextBox account, out TextBox warehouse, out TextBox item,
        out TextBox qty, out TextBox cost, out TextBox debit, out TextBox credit, out TextBox desc)
    {
        party = ErpUiFactory.FormField();
        account = ErpUiFactory.FormField();
        warehouse = ErpUiFactory.FormField();
        item = ErpUiFactory.FormField();
        qty = ErpUiFactory.FormField();
        cost = ErpUiFactory.FormField();
        debit = ErpUiFactory.FormField();
        credit = ErpUiFactory.FormField();
        desc = ErpUiFactory.FormField();

        var fields = new List<(string, UIElement)>();
        switch (_type)
        {
            case OpeningBalanceType.CustomerReceivable:
                fields.Add(("العميل", party));
                fields.Add(("مدين", debit));
                break;
            case OpeningBalanceType.SupplierPayable:
                fields.Add(("المورد", party));
                fields.Add(("دائن", credit));
                break;
            case OpeningBalanceType.OpeningStock:
                fields.Add(("المستودع", warehouse));
                fields.Add(("الصنف", item));
                fields.Add(("الكمية", qty));
                fields.Add(("التكلفة", cost));
                break;
            case OpeningBalanceType.Cash:
                fields.Add(("الصندوق", account));
                fields.Add(("المبلغ", debit));
                break;
            case OpeningBalanceType.Bank:
                fields.Add(("الحساب", account));
                fields.Add(("المبلغ", debit));
                break;
            case OpeningBalanceType.Capital:
                fields.Add(("الشريك", party));
                fields.Add(("المساهمة", credit));
                break;
            case OpeningBalanceType.GeneralLedger:
                fields.Add(("الحساب", account));
                fields.Add(("مدين", debit));
                fields.Add(("دائن", credit));
                break;
            default:
                fields.Add(("الطرف", party));
                fields.Add(("مدين", debit));
                fields.Add(("دائن", credit));
                break;
        }

        fields.Add(("الوصف", desc));
        var panel = new StackPanel();
        panel.Children.Add(ErpUiFactory.Card(ErpUiFactory.BuildFormGrid(fields.ToArray())));
        return panel;
    }

    private void RefreshGrid()
    {
        _linesGrid.AutoGenerateColumns = true;
        _linesGrid.ItemsSource = null;
        _linesGrid.ItemsSource = _lines.Select((l, i) => new
        {
            السطر = i + 1,
            الطرف = l.PartyName ?? l.AccountName ?? l.WarehouseName ?? "—",
            مدين = l.Debit,
            دائن = l.Credit,
            الوصف = l.Description ?? "—"
        }).ToList();
        ErpAccountingColorHelper.ApplyDebitCreditColumnsByHeader(_linesGrid);
    }

    private async Task SaveAsync()
    {
        if (_lines.Count == 0)
        {
            MockInteractionService.ShowWarning("أضف سطراً واحداً على الأقل.");
            return;
        }

        if (!decimal.TryParse(_rate.Text, out var rate)) rate = 1m;
        var cmd = new CreateOpeningBalanceCommand
        {
            Type = _type,
            Source = OpeningBalanceSource.Manual,
            OpeningDate = _date.SelectedDate?.ToUniversalTime() ?? DateTime.UtcNow,
            CurrencyCode = _currency.Text.Trim(),
            ExchangeRate = rate,
            Reference = _reference.Text,
            Description = _description.Text,
            Notes = _notes.Text,
            Lines = _lines
        };

        var result = await OpeningBalanceUiService.Instance.CreateAsync(cmd);
        if (ApplicationResultPresenter.Present(result))
        {
            MockInteractionService.ShowSuccess($"تم إنشاء {result.Value?.Number}");
            OpeningBalanceListRefreshHub.RequestRefresh();
        }
    }

    private async Task ImportExcelAsync()
    {
        var dlg = new OpenFileDialog
        {
            Filter = "Excel|*.xlsx;*.xls",
            Title = "استيراد أرصدة افتتاحية"
        };
        if (dlg.ShowDialog() != true) return;

        var bytes = await File.ReadAllBytesAsync(dlg.FileName);
        if (!decimal.TryParse(_rate.Text, out var rate)) rate = 1m;
        var result = await OpeningBalanceUiService.Instance.ImportExcelAsync(new ImportOpeningBalanceExcelCommand
        {
            Type = _type,
            FileName = dlg.SafeFileName,
            Content = bytes,
            OpeningDate = _date.SelectedDate?.ToUniversalTime() ?? DateTime.UtcNow,
            CurrencyCode = _currency.Text.Trim(),
            ExchangeRate = rate,
            Reference = _reference.Text,
            SkipInvalidRows = true
        });

        if (ApplicationResultPresenter.Present(result) && result.Value is not null)
        {
            MockInteractionService.ShowSuccess(
                $"تم الاستيراد: {result.Value.ImportedRows} سطر — {result.Value.DocumentNumber}");
            OpeningBalanceListRefreshHub.RequestRefresh();
        }
    }
}
