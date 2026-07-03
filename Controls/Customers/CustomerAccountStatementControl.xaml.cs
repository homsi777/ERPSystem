using ERPSystem.Application.DTOs.Customers;
using ERPSystem.Core;
using ERPSystem.Domain.Enums;
using ERPSystem.Services;
using ERPSystem.Services.Customers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ERPSystem.Controls.Customers;

public class AccountLedgerRow
{
    public DateTime EntryDate { get; init; }
    public string DocumentTypeDisplay { get; init; } = "";
    public string DocumentNumber { get; init; } = "";
    public decimal Debit { get; init; }
    public decimal Credit { get; init; }
    public decimal RunningBalance { get; init; }
    public DocumentType DocumentType { get; init; }

    public string DateDisplay => EntryDate.ToString("yyyy/MM/dd");
    public string DebitDisplay => Debit > 0 ? Debit.ToString("N2") : "—";
    public string CreditDisplay => Credit > 0 ? Credit.ToString("N2") : "—";
    public string BalanceDisplay => RunningBalance.ToString("N2");
}

public partial class CustomerAccountStatementControl : UserControl
{
    private Guid? _customerId;
    private string _customerName = "";
    private readonly List<AccountLedgerRow> _allLines = new();

    public CustomerAccountStatementControl()
    {
        InitializeComponent();
        DpFrom.SelectedDate = DateTime.Today.AddMonths(-1);
        DpTo.SelectedDate = DateTime.Today;
        Loaded += OnLoaded;
    }

    public void Initialize(Guid customerId, string customerName)
    {
        _customerId = customerId;
        _customerName = customerName;
        SetCustomerName(customerName);
        UpdateViewMode();
    }

    public void SetCustomerName(string name)
    {
        _customerName = name;
        TxtTableTitle.Text = $"كشف حساب — {name}";
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        DpFrom.SelectedDateChanged += async (_, _) => await ReloadAsync();
        DpTo.SelectedDateChanged += async (_, _) => await ReloadAsync();
        TxtSearch.TextChanged += (_, _) => ApplyFilters();

        UpdateViewMode();

        if (_customerId.HasValue)
            await ReloadAsync();
    }

    private void UpdateViewMode()
    {
        var hasCustomer = _customerId.HasValue;
        EmptyStatePanel.Visibility = hasCustomer ? Visibility.Collapsed : Visibility.Visible;
        StatementContent.Visibility = hasCustomer ? Visibility.Visible : Visibility.Collapsed;
    }

    private void BtnOpenCustomerList_Click(object sender, RoutedEventArgs e) =>
        MockInteractionService.Navigate(AppModule.Customers, "List");

    private async Task ReloadAsync()
    {
        if (_customerId is not Guid id || !AppServices.IsInitialized)
            return;

        var result = await CustomerUiService.Instance.GetStatementAsync(
            id, DpFrom.SelectedDate, DpTo.SelectedDate);

        if (!ApplicationResultPresenter.Present(result))
            return;

        var dto = result.Value!;
        SetCustomerName(dto.CustomerName);
        _allLines.Clear();
        _allLines.AddRange(dto.Lines.Select(l => new AccountLedgerRow
        {
            EntryDate = l.EntryDate,
            DocumentType = l.DocumentType,
            DocumentTypeDisplay = MapDocumentType(l.DocumentType),
            DocumentNumber = l.DocumentNumber,
            Debit = l.Debit,
            Credit = l.Credit,
            RunningBalance = l.RunningBalance
        }));

        TxtOpeningBalance.Text = $"{dto.OpeningBalance:N2} $";
        TxtClosingBalance.Text = $"{dto.ClosingBalance:N2} $";
        TxtTotalDebit.Text = $"{_allLines.Sum(x => x.Debit):N2} $";
        TxtTotalCredit.Text = $"{_allLines.Sum(x => x.Credit):N2} $";

        ApplyFilters();
    }

    private void ApplyFilters()
    {
        var term = TxtSearch?.Text?.Trim() ?? "";
        IEnumerable<AccountLedgerRow> rows = _allLines;

        if (!string.IsNullOrEmpty(term))
        {
            rows = rows.Where(r =>
                r.DocumentNumber.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                r.DocumentTypeDisplay.Contains(term, StringComparison.OrdinalIgnoreCase));
        }

        LinesGrid.ItemsSource = rows.ToList();
    }

    private static string MapDocumentType(DocumentType type) => type switch
    {
        DocumentType.SalesInvoice => "فاتورة بيع",
        DocumentType.ReceiptVoucher => "سند قبض",
        DocumentType.PaymentVoucher => "سند دفع",
        _ => type.ToString()
    };

    private void BtnReceipt_Click(object sender, RoutedEventArgs e) =>
        MockInteractionService.Navigate(AppModule.Accounting, "Receipts");

    private void BtnPrint_Click(object sender, RoutedEventArgs e) =>
        MockInteractionService.ShowDocumentPreview(TxtTableTitle.Text, "طباعة");

    private void BtnPdf_Click(object sender, RoutedEventArgs e) =>
        MockInteractionService.ShowDocumentPreview(TxtTableTitle.Text, "PDF");

    private void BtnExcel_Click(object sender, RoutedEventArgs e) =>
        MockInteractionService.ShowDocumentPreview(TxtTableTitle.Text, "Excel");

    private void DocumentNumber_Click(object sender, MouseButtonEventArgs e)
    {
        if (sender is not FrameworkElement fe || fe.DataContext is not AccountLedgerRow row)
            return;

        if (row.DocumentType == DocumentType.SalesInvoice)
            MockInteractionService.OpenInvoiceOperationsCenter(row.DocumentNumber);
        else if (row.DocumentType == DocumentType.ReceiptVoucher)
            MockInteractionService.Navigate(AppModule.Accounting, "Receipts");
    }
}
