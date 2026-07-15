using ERPSystem.Application.Common;
using ERPSystem.Application.DTOs.Customers;
using ERPSystem.Core;
using ERPSystem.Domain.Enums;
using ERPSystem.Services;
using ERPSystem.Services.Customers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace ERPSystem.Controls.Customers;

public sealed class CustomerLedgerRow
{
    public CustomerAccountMovementType MovementType { get; init; }
    public Guid DocumentId { get; init; }
    public Guid EntryId { get; init; }
    public string DocumentNumber { get; init; } = "";
    public DateTime TransactionDate { get; init; }
    public string FabricDescription { get; init; } = "";
    public int? RollCount { get; init; }
    public decimal? TotalMeters { get; init; }
    public string? LengthUnit { get; init; }
    public decimal? UnitPrice { get; init; }
    public decimal LineAmount { get; init; }
    public string? Notes { get; init; }
    public decimal RunningBalance { get; init; }
    public bool IsReconciled { get; init; }

    public string DateDisplay => AppFormats.Date(TransactionDate);
    public string RollCountDisplay => RollCount.HasValue ? RollCount.Value.ToString() : "—";
    public string TotalLengthDisplay => SaleLengthUnitHelper.FormatLength(TotalMeters, LengthUnit);
    public string UnitPriceDisplay => UnitPrice.HasValue ? AppFormats.Amount(UnitPrice.Value) : "—";
    public string LineAmountDisplay => AppFormats.Amount(LineAmount);
    public string RunningBalanceDisplay => AppFormats.Amount(RunningBalance);
    public string NotesDisplay => string.IsNullOrWhiteSpace(Notes) ? "—" : Notes;
    public string MovementTypeDisplay => MovementType switch
    {
        CustomerAccountMovementType.SalesInvoice => "فاتورة بيع",
        CustomerAccountMovementType.SalesReturn => "مرتجع",
        CustomerAccountMovementType.ReceiptVoucher => "سند قبض",
        _ => MovementType.ToString()
    };

    public static CustomerLedgerRow FromDto(CustomerAccountLedgerLineDto dto, bool isReconciled) => new()
    {
        MovementType = dto.MovementType,
        DocumentId = dto.DocumentId,
        EntryId = dto.EntryId,
        DocumentNumber = dto.DocumentNumber,
        TransactionDate = dto.TransactionDate,
        FabricDescription = string.IsNullOrWhiteSpace(dto.FabricDescription) ? "—" : dto.FabricDescription,
        RollCount = dto.RollCount,
        TotalMeters = dto.TotalMeters,
        LengthUnit = dto.LengthUnit,
        UnitPrice = dto.UnitPrice,
        LineAmount = dto.LineAmount,
        Notes = dto.Notes,
        RunningBalance = dto.RunningBalance,
        IsReconciled = isReconciled
    };
}

public partial class CustomerAccountStatementControl : UserControl
{
    private Guid? _customerId;
    private string _customerName = "";
    private decimal _openingBalance;
    private decimal _closingBalance;
    private DateTime? _lastReconciliationDate;
    private decimal? _lastReconciliationBalance;
    private Guid? _lastReconciliationDocumentId;
    private CustomerAccountLedgerDto? _ledger;
    private readonly List<CustomerLedgerRow> _allLines = new();

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

        var result = await CustomerUiService.Instance.GetAccountLedgerAsync(
            id, DpFrom.SelectedDate, DpTo.SelectedDate);

        if (!ApplicationResultPresenter.Present(result))
            return;

        var dto = result.Value!;
        _ledger = dto;
        SetCustomerName(dto.CustomerName);
        _openingBalance = dto.OpeningBalance;
        _closingBalance = dto.ClosingBalance;
        _lastReconciliationDate = dto.LastReconciliationDate;
        _lastReconciliationBalance = dto.LastReconciliationBalance;
        _lastReconciliationDocumentId = dto.LastReconciliationDocumentId;

        TxtOpeningBalance.Text = $"{dto.OpeningBalance:N2} $";
        TxtClosingBalance.Text = $"{dto.ClosingBalance:N2} $";
        TxtReconciliationBalance.Text = dto.LastReconciliationBalance.HasValue
            ? $"{dto.LastReconciliationBalance.Value:N2} $"
            : "—";
        TxtReconciliationDate.Text = dto.LastReconciliationDate.HasValue
            ? AppFormats.Date(dto.LastReconciliationDate.Value)
            : "—";

        _allLines.Clear();
        var reconciledCutoffIndex = ResolveReconciledCutoffIndex(dto.Lines, dto.LastReconciliationDocumentId);
        for (var i = 0; i < dto.Lines.Count; i++)
        {
            var isReconciled = reconciledCutoffIndex >= 0 && i <= reconciledCutoffIndex;
            _allLines.Add(CustomerLedgerRow.FromDto(dto.Lines[i], isReconciled));
        }

        ApplyFilters();
    }

    private static int ResolveReconciledCutoffIndex(
        IReadOnlyList<CustomerAccountLedgerLineDto> lines,
        Guid? reconciliationDocumentId)
    {
        if (!reconciliationDocumentId.HasValue || lines.Count == 0)
            return -1;

        for (var i = 0; i < lines.Count; i++)
        {
            if (lines[i].EntryId == reconciliationDocumentId.Value)
                return i;
        }

        return -1;
    }

    private void ApplyFilters()
    {
        var term = TxtSearch?.Text?.Trim() ?? "";
        IEnumerable<CustomerLedgerRow> rows = _allLines;

        if (!string.IsNullOrEmpty(term))
        {
            rows = rows.Where(r =>
                r.DocumentNumber.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                r.FabricDescription.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                r.MovementTypeDisplay.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                (r.Notes?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false));
        }

        LinesGrid.ItemsSource = rows.ToList();
    }

    private void BtnReceipt_Click(object sender, RoutedEventArgs e) =>
        MockInteractionService.Navigate(AppModule.Accounting, "Receipts");

    private async void BtnReconcile_Click(object sender, RoutedEventArgs e)
    {
        if (_customerId is not Guid customerId)
        {
            MockInteractionService.ShowWarning("اختر عميلاً أولاً.", "مطابقة الكشف");
            return;
        }

        var visibleRows = (LinesGrid.ItemsSource as IEnumerable<CustomerLedgerRow>)?.ToList() ?? _allLines;
        if (visibleRows.Count == 0)
        {
            MockInteractionService.ShowWarning("لا توجد حركات للمطابقة في الفترة المحددة.", "مطابقة الكشف");
            return;
        }

        var selected = LinesGrid.SelectedItem as CustomerLedgerRow;
        var target = selected ?? visibleRows[^1];

        var confirm = MockInteractionService.Confirm(
            $"مطابقة الكشف حتى السطر:\n{target.MovementTypeDisplay} — {target.DocumentNumber}\n" +
            $"التاريخ: {target.DateDisplay}\nالرصيد: {target.RunningBalanceDisplay}\n\n" +
            "المطابقة الجديدة تحل محل السابقة.",
            "مطابقة الكشف");
        if (!confirm)
            return;

        var result = await CustomerUiService.Instance.ReconcileAccountAsync(
            customerId,
            target.TransactionDate,
            target.EntryId,
            target.RunningBalance);

        if (!ApplicationResultPresenter.Present(result))
            return;

        MockInteractionService.ShowInfo("تم حفظ مطابقة الكشف بنجاح.", "مطابقة الكشف");
        await ReloadAsync();
    }

    private void BtnPrint_Click(object sender, RoutedEventArgs e) => ShowDocument(exportPdf: false);

    private void BtnPdf_Click(object sender, RoutedEventArgs e) => ShowDocument(exportPdf: true);

    private void ShowDocument(bool exportPdf)
    {
        if (_customerId is null || _ledger is null)
        {
            MockInteractionService.ShowWarning("لا توجد بيانات للتصدير.", "كشف حساب");
            return;
        }

        var visibleRows = (LinesGrid.ItemsSource as IEnumerable<CustomerLedgerRow>)?.ToList();
        var lines = visibleRows is { Count: > 0 } && !string.IsNullOrWhiteSpace(TxtSearch?.Text)
            ? visibleRows.Select(ToLineDto).ToList()
            : _ledger.Lines;

        var ledgerForExport = new CustomerAccountLedgerDto
        {
            CustomerId = _ledger.CustomerId,
            CustomerName = _ledger.CustomerName,
            OpeningBalance = _ledger.OpeningBalance,
            ClosingBalance = _ledger.ClosingBalance,
            LastReconciliationDate = _ledger.LastReconciliationDate,
            LastReconciliationBalance = _ledger.LastReconciliationBalance,
            LastReconciliationDocumentId = _ledger.LastReconciliationDocumentId,
            Lines = lines
        };

        CustomerStatementDocumentService.ShowLedgerPreview(
            ledgerForExport,
            DpFrom.SelectedDate,
            DpTo.SelectedDate,
            exportPdf);
    }

    private static CustomerAccountLedgerLineDto ToLineDto(CustomerLedgerRow row) => new()
    {
        MovementType = row.MovementType,
        DocumentId = row.DocumentId,
        EntryId = row.EntryId,
        DocumentNumber = row.DocumentNumber,
        TransactionDate = row.TransactionDate,
        FabricDescription = row.FabricDescription == "—" ? "" : row.FabricDescription,
        RollCount = row.RollCount,
        TotalMeters = row.TotalMeters,
        LengthUnit = row.LengthUnit,
        UnitPrice = row.UnitPrice,
        LineAmount = row.LineAmount,
        Notes = row.Notes,
        RunningBalance = row.RunningBalance
    };

    private void BtnExcel_Click(object sender, RoutedEventArgs e) =>
        ERPSystem.Services.Documents.ListExportService.ExportGrid(LinesGrid, $"كشف حساب - {_customerName}");

    private void DocumentNumber_Click(object sender, MouseButtonEventArgs e)
    {
        if (sender is not FrameworkElement fe || fe.DataContext is not CustomerLedgerRow row)
            return;

        if (row.MovementType == CustomerAccountMovementType.SalesInvoice)
            MockInteractionService.OpenInvoiceOperationsCenter(row.DocumentNumber);
        else if (row.MovementType == CustomerAccountMovementType.ReceiptVoucher)
            MockInteractionService.Navigate(AppModule.Accounting, "Receipts");
    }

    private void LinesGrid_LoadingRow(object sender, DataGridRowEventArgs e)
    {
        if (e.Row.Item is CustomerLedgerRow row && row.IsReconciled)
            e.Row.Background = new SolidColorBrush(Color.FromRgb(254, 226, 226));
    }
}
