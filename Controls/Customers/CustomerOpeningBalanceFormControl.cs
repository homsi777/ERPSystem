using ERPSystem.Application.Commands.Finance;
using ERPSystem.Application.Common;
using ERPSystem.Application.DTOs.Finance;
using ERPSystem.Core;
using ERPSystem.Domain.Entities.Finance;
using ERPSystem.Helpers;
using ERPSystem.Services;
using ERPSystem.Services.Customers;
using ERPSystem.Services.Finance;
using System.Windows;
using System.Windows.Controls;

namespace ERPSystem.Controls.Customers;

/// <summary>إدخال/تعديل رصيد افتتاحي لعميل واحد — عبر محرك الأرصدة الموحّد.</summary>
public sealed class CustomerOpeningBalanceFormControl : UserControl
{
    private readonly ComboBox _cmbCustomer = new() { Height = 36, MinWidth = 280, DisplayMemberPath = "Name", SelectedValuePath = "Id" };
    private readonly DatePicker _date = ErpUiFactory.FormDate(DateTime.Today);
    private readonly TextBox _currency = ErpUiFactory.FormField("USD");
    private readonly RadioButton _rbDebit = new() { Content = "مدين (ذمة على العميل)", IsChecked = true, Margin = new Thickness(0, 0, 16, 0) };
    private readonly RadioButton _rbCredit = new() { Content = "دائن (رصيد لصالح العميل)" };
    private readonly TextBox _txtAmount = ErpUiFactory.FormField("");
    private readonly TextBox _reference = ErpUiFactory.FormField();
    private readonly TextBox _notes = ErpUiFactory.FormField();
    private readonly StackPanel _actions = new() { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 16, 0, 0) };
    private readonly TextBlock _statusBanner = new() { Margin = new Thickness(0, 8, 0, 0), TextWrapping = TextWrapping.Wrap };

    private Guid? _documentId;
    private OpeningBalanceStatus _status = OpeningBalanceStatus.Draft;
    private OpeningBalanceLookupsDto? _lookups;

    public CustomerOpeningBalanceFormControl()
    {
        var root = new StackPanel { Margin = new Thickness(4) };
        root.Children.Add(ErpUiFactory.SectionTitle("رصيد افتتاحي — عميل"));
        root.Children.Add(ErpUxFactory.InfoBanner(
            "يُنشئ مستنداً في محرك الأرصدة الافتتاحية ويمرّ بسير الاعتماد والترحيل المحاسبي الموحّد.",
            "info"));

        var sidePanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 8, 0, 8) };
        sidePanel.Children.Add(_rbDebit);
        sidePanel.Children.Add(_rbCredit);

        root.Children.Add(ErpUiFactory.Card(ErpUiFactory.BuildFormGrid(
            ("العميل *", _cmbCustomer),
            ("تاريخ الافتتاح *", _date),
            ("العملة *", _currency),
            ("نوع الرصيد", sidePanel),
            ("المبلغ *", _txtAmount),
            ("المرجع", _reference),
            ("ملاحظات", _notes))));

        root.Children.Add(_statusBanner);
        root.Children.Add(_actions);
        Content = new ScrollViewer { Content = root, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        Loaded += async (_, _) => await InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        if (!AppServices.IsInitialized) return;

        var lookups = await OpeningBalanceUiService.Instance.GetLookupsAsync();
        if (lookups.IsSuccess && lookups.Value is not null)
        {
            _lookups = lookups.Value;
            _cmbCustomer.ItemsSource = _lookups.Customers
                .Where(c => c.Extra != "posted")
                .ToList();
        }

        _documentId = CustomerOpeningBalanceNavigationContext.EditDocumentId;
        if (_documentId is Guid id)
            await LoadDocumentAsync(id);
        else
            BuildActionButtons(canSave: true);

        _cmbCustomer.IsEnabled = _documentId is null;
    }

    private async Task LoadDocumentAsync(Guid id)
    {
        var result = await OpeningBalanceUiService.Instance.GetDetailsAsync(id);
        if (!ApplicationResultPresenter.Present(result) || result.Value is null)
            return;

        var doc = result.Value;
        _status = doc.Header.Status;
        _statusBanner.Text = $"الحالة: {doc.Header.StatusDisplay} — {doc.Header.Number}";
        _statusBanner.Foreground = Br("TextSecondaryBrush");

        _date.SelectedDate = doc.Header.OpeningDate.ToLocalTime().Date;
        _currency.Text = doc.Header.CurrencyCode;
        _reference.Text = doc.Header.Reference ?? "";
        _notes.Text = doc.Header.DisplayNotes ?? "";

        var line = doc.Lines.FirstOrDefault();
        if (line is not null)
        {
            if (line.Credit > line.Debit)
            {
                _rbCredit.IsChecked = true;
                _txtAmount.Text = AppFormats.Amount(line.Credit);
            }
            else
            {
                _rbDebit.IsChecked = true;
                _txtAmount.Text = AppFormats.Amount(line.Debit);
            }

            if (line.PartyId is Guid pid)
                _cmbCustomer.SelectedValue = pid;
            else if (!string.IsNullOrWhiteSpace(line.PartyName))
            {
                var match = _lookups?.Customers.FirstOrDefault(c =>
                    c.Name.Equals(line.PartyName, StringComparison.OrdinalIgnoreCase) ||
                    c.Code.Equals(line.PartyName, StringComparison.OrdinalIgnoreCase));
                if (match is not null)
                    _cmbCustomer.SelectedItem = match;
            }
        }

        var editable = _status is OpeningBalanceStatus.Draft or OpeningBalanceStatus.Rejected;
        _date.IsEnabled = editable;
        _currency.IsEnabled = editable;
        _txtAmount.IsEnabled = editable;
        _reference.IsEnabled = editable;
        _notes.IsEnabled = editable;
        _rbDebit.IsEnabled = editable;
        _rbCredit.IsEnabled = editable;

        BuildActionButtons(canSave: editable);
    }

    private void BuildActionButtons(bool canSave)
    {
        _actions.Children.Clear();
        if (canSave)
        {
            AddAction("حفظ مسودة", S("SecondaryButtonStyle"), async () => await SaveAsync(submit: false));
            AddAction("إرسال للاعتماد", S("PrimaryButtonStyle"), async () => await SaveAsync(submit: true));
        }

        if (_documentId is Guid id)
        {
            if (_status == OpeningBalanceStatus.PendingApproval)
                AddAction("اعتماد", S("PrimaryButtonStyle"), async () => await ApproveAsync(id));
            if (_status == OpeningBalanceStatus.Approved)
                AddAction("ترحيل", S("PrimaryButtonStyle"), async () => await PostAsync(id));
        }
    }

    private void AddAction(string label, Style style, Func<Task> handler)
    {
        var btn = new Button
        {
            Content = label,
            Style = style,
            Height = 34,
            Padding = new Thickness(14, 0, 14, 0),
            Margin = new Thickness(0, 0, 8, 0)
        };
        btn.Click += async (_, _) => await handler();
        _actions.Children.Add(btn);
    }

    private bool TryBuildLine(out OpeningBalanceLineInput line, out string? error)
    {
        line = new OpeningBalanceLineInput();
        error = null;

        if (_cmbCustomer.SelectedItem is not OpeningBalanceLookupItemDto customer)
        {
            error = "اختر عميلاً.";
            return false;
        }

        if (!decimal.TryParse(_txtAmount.Text, out var amount) || amount <= 0)
        {
            error = "أدخل مبلغاً صحيحاً أكبر من صفر.";
            return false;
        }

        if (_date.SelectedDate is null)
        {
            error = "حدّد تاريخ الافتتاح.";
            return false;
        }

        var currency = _currency.Text.Trim();
        if (currency.Length != 3)
        {
            error = "رمز العملة يجب أن يكون 3 أحرف.";
            return false;
        }

        var isCredit = _rbCredit.IsChecked == true;
        line = new OpeningBalanceLineInput
        {
            PartyId = customer.Id,
            PartyName = customer.Name,
            Debit = isCredit ? 0 : amount,
            Credit = isCredit ? amount : 0,
            Reference = _reference.Text.Trim(),
            Notes = _notes.Text.Trim()
        };
        return true;
    }

    private async Task SaveAsync(bool submit)
    {
        if (!TryBuildLine(out var line, out var error))
        {
            MockInteractionService.ShowWarning(error!, "تحقق");
            return;
        }

        var openingDate = ApplicationDateNormalizer.ToUtcDate(_date.SelectedDate) ?? DateTime.UtcNow;
        var currency = _currency.Text.Trim().ToUpperInvariant();

        if (_documentId is Guid id)
        {
            var update = await OpeningBalanceUiService.Instance.UpdateAsync(new UpdateOpeningBalanceCommand
            {
                DocumentId = id,
                OpeningDate = openingDate,
                CurrencyCode = currency,
                ExchangeRate = 1m,
                Reference = _reference.Text.Trim(),
                Notes = _notes.Text.Trim(),
                Lines = [line]
            });
            if (!ApplicationResultPresenter.Present(update))
                return;

            if (submit)
            {
                var submitResult = await OpeningBalanceUiService.Instance.SubmitAsync(id);
                if (!ApplicationResultPresenter.Present(submitResult))
                    return;
            }
        }
        else
        {
            var create = await OpeningBalanceUiService.Instance.CreateAsync(new CreateOpeningBalanceCommand
            {
                Type = OpeningBalanceType.CustomerReceivable,
                Source = OpeningBalanceSource.Manual,
                OpeningDate = openingDate,
                CurrencyCode = currency,
                ExchangeRate = 1m,
                Reference = _reference.Text.Trim(),
                Notes = _notes.Text.Trim(),
                Lines = [line],
                SubmitForApproval = submit
            });
            if (!ApplicationResultPresenter.Present(create))
                return;

            _documentId = create.Value?.Id;
            if (_documentId is Guid newId)
            {
                CustomerOpeningBalanceNavigationContext.BeginEdit(newId);
                await LoadDocumentAsync(newId);
            }
        }

        NotifyRefresh();
        MockInteractionService.ShowSuccess(submit ? "تم الحفظ والإرسال للاعتماد." : "تم حفظ المسودة.");
    }

    private async Task ApproveAsync(Guid id)
    {
        var result = await OpeningBalanceUiService.Instance.ApproveAsync(id, null);
        if (!ApplicationResultPresenter.Present(result))
            return;
        NotifyRefresh();
        await LoadDocumentAsync(id);
        MockInteractionService.ShowSuccess("تم اعتماد المستند.");
    }

    private async Task PostAsync(Guid id)
    {
        var result = await OpeningBalanceUiService.Instance.PostAsync(id);
        if (!ApplicationResultPresenter.Present(result))
            return;
        NotifyRefresh();
        CustomerListRefreshHub.RequestRefresh();
        await LoadDocumentAsync(id);
        MockInteractionService.ShowSuccess(
            result.Value?.JournalEntryNumber is { Length: > 0 } num
                ? $"تم الترحيل — القيد {num}"
                : "تم ترحيل المستند.");
    }

    private static void NotifyRefresh()
    {
        OpeningBalanceListRefreshHub.RequestRefresh();
        CustomerOpeningBalanceRefreshHub.RequestRefresh();
    }

    private static Style S(string key) => (Style)System.Windows.Application.Current.Resources[key]!;
    private static System.Windows.Media.Brush Br(string key) => (System.Windows.Media.Brush)System.Windows.Application.Current.Resources[key]!;
}
