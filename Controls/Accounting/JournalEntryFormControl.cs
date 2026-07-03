using ERPSystem.Application.Commands.Accounting;
using ERPSystem.Application.DTOs.Accounting;
using ERPSystem.Core;
using ERPSystem.Domain.Enums;
using ERPSystem.Helpers;
using ERPSystem.Services;
using ERPSystem.Services.Accounting;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using WpfApplication = System.Windows.Application;

namespace ERPSystem.Controls.Accounting;

/// <summary>قيد يومية يدوي — بنود دين/مدين مع معاينة التوازن.</summary>
public sealed class JournalEntryFormControl : UserControl
{
    private sealed class LineEditor
    {
        public ComboBox Account { get; }
        public TextBox Debit { get; }
        public TextBox Credit { get; }
        public TextBox Narrative { get; }
        public Border Row { get; }

        public LineEditor(
            IReadOnlyList<AccountLookupDto> accounts,
            Action<LineEditor> onRemove)
        {
            Account = new ComboBox
            {
                MinWidth = 220,
                IsEditable = false,
                ItemsSource = accounts,
                DisplayMemberPath = nameof(AccountLookupDto.Display),
                Style = S("EnterpriseComboBoxStyle")
            };
            if (accounts.Count > 0) Account.SelectedIndex = 0;

            Debit = ErpUiFactory.FormField("0");
            Credit = ErpUiFactory.FormField("0");
            Narrative = ErpUiFactory.FormField("البيان");

            var removeBtn = new Button
            {
                Content = "حذف",
                Style = S("GhostButtonStyle"),
                Height = 32,
                VerticalAlignment = VerticalAlignment.Bottom
            };

            var grid = new Grid { Margin = new Thickness(0, 0, 0, 8) };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(2, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(2, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            Account.Margin = new Thickness(0, 0, 6, 0);
            Debit.Margin = new Thickness(0, 0, 6, 0);
            Credit.Margin = new Thickness(0, 0, 6, 0);
            Narrative.Margin = new Thickness(0, 0, 6, 0);

            Grid.SetColumn(Account, 0);
            Grid.SetColumn(Debit, 1);
            Grid.SetColumn(Credit, 2);
            Grid.SetColumn(Narrative, 3);
            Grid.SetColumn(removeBtn, 4);

            grid.Children.Add(Account);
            grid.Children.Add(Debit);
            grid.Children.Add(Credit);
            grid.Children.Add(Narrative);
            grid.Children.Add(removeBtn);

            Row = ErpUiFactory.Card(grid);
            removeBtn.Click += (_, _) => onRemove(this);

            Debit.TextChanged += (_, _) => PreviewChanged?.Invoke();
            Credit.TextChanged += (_, _) => PreviewChanged?.Invoke();
        }

        public static event Action? PreviewChanged;
    }

    private readonly TextBlock _title = ErpUiFactory.SectionTitle("قيد يومية جديد");
    private readonly TextBlock _hint = new()
    {
        Text = "أنشئ قيداً يدوياً. يجب أن يتساوى مجموع المدين مع مجموع الدائن قبل الحفظ.",
        Foreground = (Brush)WpfApplication.Current.Resources["TextSecondaryBrush"]!,
        Margin = new Thickness(0, 0, 0, 12),
        TextWrapping = TextWrapping.Wrap
    };
    private readonly TextBox _description = ErpUiFactory.FormField("البيان العام للقيد");
    private readonly DatePicker _date = ErpUiFactory.FormDate(DateTime.Today);
    private readonly ComboBox _journalBook = new() { MinWidth = 220, IsEditable = false, Style = S("EnterpriseComboBoxStyle") };
    private readonly StackPanel _linesHost = new();
    private readonly TextBlock _balancePreview = new()
    {
        FontSize = 14,
        FontWeight = FontWeights.SemiBold,
        Margin = new Thickness(0, 8, 0, 0),
        Foreground = (Brush)WpfApplication.Current.Resources["PrimaryBrush"]!
    };
    private readonly Button _addLine = new() { Content = "+ إضافة سطر", Style = S("SecondaryButtonStyle"), Height = 32, Margin = new Thickness(0, 8, 0, 0) };
    private readonly Button _save = new() { Content = "حفظ كمسودة", Style = S("PrimaryButtonStyle"), MinWidth = 140, Height = 38 };
    private readonly Button _cancel = new() { Content = "إلغاء", Style = S("SecondaryButtonStyle"), MinWidth = 100, Height = 38, Margin = new Thickness(8, 0, 0, 0) };

    private readonly List<LineEditor> _lines = [];
    private IReadOnlyList<AccountLookupDto> _accounts = [];
    private IReadOnlyList<JournalBookListDto> _journalBooks = [];
    private bool _saving;
    private bool _popupMode;

    public JournalEntryFormControl()
    {
        var headerRow = new Grid { Margin = new Thickness(0, 0, 0, 6) };
        headerRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(2, GridUnitType.Star) });
        headerRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        headerRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        headerRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(2, GridUnitType.Star) });
        headerRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        void AddHeader(int col, string text)
        {
            var tb = new TextBlock
            {
                Text = text,
                FontSize = 11,
                Foreground = (Brush)WpfApplication.Current.Resources["TextMutedBrush"]!
            };
            Grid.SetColumn(tb, col);
            headerRow.Children.Add(tb);
        }

        AddHeader(0, "الحساب *");
        AddHeader(1, "مدين");
        AddHeader(2, "دائن");
        AddHeader(3, "البيان");

        var stack = new StackPanel();
        stack.Children.Add(_title);
        stack.Children.Add(_hint);
        stack.Children.Add(ErpUiFactory.Card(ErpUiFactory.BuildFormGrid(
            ("البيان *", _description),
            ("دفتر اليومية", _journalBook),
            ("التاريخ", _date))));
        stack.Children.Add(ErpUiFactory.SectionTitle("بنود القيد"));
        stack.Children.Add(headerRow);
        stack.Children.Add(_linesHost);
        stack.Children.Add(_addLine);
        stack.Children.Add(_balancePreview);

        var actions = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 16, 0, 0) };
        actions.Children.Add(_save);
        actions.Children.Add(_cancel);
        stack.Children.Add(actions);

        Content = new ScrollViewer { Padding = new Thickness(16), Content = stack, MaxWidth = 820 };
        Loaded += OnLoaded;
        _addLine.Click += (_, _) => AddLine();
        _save.Click += async (_, _) => await SaveAsync();
        _cancel.Click += (_, _) =>
        {
            if (_popupMode) AccountingPopupService.CancelActive();
            else MockInteractionService.Navigate(AppModule.Accounting, "Journal");
        };

        LineEditor.PreviewChanged += UpdateBalancePreview;
    }

    public void BindPopupHost()
    {
        _popupMode = true;
        _title.Visibility = Visibility.Collapsed;
        _hint.Visibility = Visibility.Collapsed;
        _cancel.Visibility = Visibility.Collapsed;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        var accounts = await AccountingUiService.Instance.GetPostableAccountsAsync();
        if (!ApplicationResultPresenter.Present(accounts))
            return;

        _accounts = accounts.Value ?? [];
        if (_accounts.Count == 0)
        {
            _save.IsEnabled = false;
            MockInteractionService.ShowWarning("لا توجد حسابات قابلة للترحيل.", "دليل الحسابات");
            return;
        }

        if (!await AccountingUiService.Instance.CanCreateJournalAsync())
        {
            _save.IsEnabled = false;
            MockInteractionService.ShowWarning("لا تملك صلاحية إنشاء قيود.", "صلاحية");
        }

        var books = await AccountingUiService.Instance.GetJournalBooksAsync();
        if (ApplicationResultPresenter.Present(books) && books.Value is { Count: > 0 })
        {
            _journalBooks = books.Value;
            _journalBook.ItemsSource = _journalBooks;
            _journalBook.DisplayMemberPath = nameof(JournalBookListDto.NameAr);
            _journalBook.SelectedValuePath = nameof(JournalBookListDto.Id);
            _journalBook.SelectedIndex = 0;
        }

        AddLine();
        AddLine();
        UpdateBalancePreview();
    }

    private void AddLine()
    {
        var editor = new LineEditor(_accounts, RemoveLine);
        _lines.Add(editor);
        _linesHost.Children.Add(editor.Row);
        UpdateBalancePreview();
    }

    private void RemoveLine(LineEditor editor)
    {
        if (_lines.Count <= 2)
        {
            MockInteractionService.ShowWarning("يجب أن يحتوي القيد على سطرين على الأقل.", "القيود");
            return;
        }

        _lines.Remove(editor);
        _linesHost.Children.Remove(editor.Row);
        UpdateBalancePreview();
    }

    private void UpdateBalancePreview()
    {
        decimal debit = 0, credit = 0;
        foreach (var line in _lines)
        {
            if (TryParseDecimal(line.Debit.Text, out var d)) debit += d;
            if (TryParseDecimal(line.Credit.Text, out var c)) credit += c;
        }

        var diff = debit - credit;
        var balanced = Math.Abs(diff) < 0.01m;
        _balancePreview.Text = balanced
            ? $"التوازن: مدين {debit:N2} = دائن {credit:N2} ✓"
            : $"التوازن: مدين {debit:N2} | دائن {credit:N2} | الفرق {Math.Abs(diff):N2}";
        _balancePreview.Foreground = (Brush)WpfApplication.Current.Resources[balanced ? "SuccessBrush" : "WarningBrush"]!;
    }

    private async Task SaveAsync()
    {
        if (_saving) return;

        if (string.IsNullOrWhiteSpace(_description.Text) ||
            _description.Text.Trim() == "البيان العام للقيد")
        {
            MessageBox.Show("البيان مطلوب.", "القيود", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var lineCommands = new List<JournalEntryLineCommand>();
        foreach (var line in _lines)
        {
            if (line.Account.SelectedItem is not AccountLookupDto account)
            {
                MessageBox.Show("اختر حساباً لكل سطر.", "القيود", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            TryParseDecimal(line.Debit.Text, out var debit);
            TryParseDecimal(line.Credit.Text, out var credit);

            if (debit <= 0 && credit <= 0)
            {
                MessageBox.Show("أدخل مبلغ مدين أو دائن لكل سطر.", "القيود", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (debit > 0 && credit > 0)
            {
                MessageBox.Show("لا يمكن أن يحتوي السطر على مدين ودائن معاً.", "القيود", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            lineCommands.Add(new JournalEntryLineCommand
            {
                AccountId = account.Id,
                Debit = debit,
                Credit = credit,
                Narrative = string.IsNullOrWhiteSpace(line.Narrative.Text) || line.Narrative.Text == "البيان"
                    ? _description.Text.Trim()
                    : line.Narrative.Text.Trim()
            });
        }

        if (lineCommands.Count < 2)
        {
            MessageBox.Show("يجب إدخال سطرين على الأقل.", "القيود", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var totalDebit = lineCommands.Sum(l => l.Debit);
        var totalCredit = lineCommands.Sum(l => l.Credit);
        if (Math.Abs(totalDebit - totalCredit) > 0.01m)
        {
            MessageBox.Show("القيد غير متوازن. تحقق من المبالغ.", "القيود", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        _saving = true;
        _save.IsEnabled = false;
        try
        {
            var result = await AccountingUiService.Instance.CreateJournalEntryAsync(new CreateJournalEntryCommand
            {
                Description = _description.Text.Trim(),
                EntryDate = _date.SelectedDate ?? DateTime.Today,
                JournalBookId = ResolveJournalBookId(),
                SourceType = DocumentType.JournalEntry,
                Lines = lineCommands
            });

            if (ApplicationResultPresenter.Present(result))
            {
                MockInteractionService.ShowSuccess("تم حفظ القيد كمسودة.");
                if (_popupMode)
                    AccountingPopupService.CompleteSuccess();
                else
                {
                    AccountingListRefreshHub.RequestRefresh();
                    MockInteractionService.Navigate(AppModule.Accounting, "Journal");
                }
            }
        }
        finally
        {
            _saving = false;
            _save.IsEnabled = true;
        }
    }

    private Guid? ResolveJournalBookId()
    {
        if (_journalBook.SelectedValue is Guid id && id != Guid.Empty)
            return id;
        if (_journalBook.SelectedItem is JournalBookListDto book)
            return book.Id;
        return null;
    }

    private static bool TryParseDecimal(string text, out decimal value) =>
        decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out value)
        || decimal.TryParse(text, out value);

    private static Style S(string k) => (Style)WpfApplication.Current.Resources[k]!;
}
