using ERPSystem.Application.Commands.Sales;
using ERPSystem.Application.Queries.Containers;
using ERPSystem.Application.Results;
using ERPSystem.Application.UseCases.Queries;
using ERPSystem.Core;
using ERPSystem.Core.ChinaImport;
using ERPSystem.Domain.Enums;
using ERPSystem.Infrastructure.Seed;
using ERPSystem.Services;
using ERPSystem.Services.Customers;
using ERPSystem.Services.Sales;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ERPSystem.Controls.Sales
{
    public sealed class ContainerPickItem
    {
        public Guid Id { get; init; }
        public string Display { get; init; } = "";
    }

    public sealed class CustomerPickItem
    {
        public Guid Id { get; init; }
        public string Display { get; init; } = "";
    }

    public sealed class WarehousePickItem
    {
        public Guid Id { get; init; }
        public string Display { get; init; } = "";
    }

    public class SalesInvoiceLineRow : INotifyPropertyChanged
    {
        private string _goodsType = "قماش قطن";
        private string _boltCode = "FAB-001";
        private string _color = "أبيض";
        private int _rollCount = 1;
        private string _lengthStatus = "بانتظار التفصيل";
        private string _unit = "متر";
        private decimal _unitPrice = 12.5m;

        public string GoodsType
        {
            get => _goodsType;
            set => SetField(ref _goodsType, value);
        }

        public string BoltCode
        {
            get => _boltCode;
            set => SetField(ref _boltCode, value);
        }

        public string Color
        {
            get => _color;
            set => SetField(ref _color, value);
        }

        public int RollCount
        {
            get => _rollCount;
            set => SetField(ref _rollCount, value);
        }

        public string LengthStatus
        {
            get => _lengthStatus;
            set => SetField(ref _lengthStatus, value);
        }

        public string Unit
        {
            get => _unit;
            set => SetField(ref _unit, value);
        }

        public decimal UnitPrice
        {
            get => _unitPrice;
            set => SetField(ref _unitPrice, value);
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
                return;
            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public partial class NewSalesInvoiceControl : UserControl
    {
        private readonly ObservableCollection<SalesInvoiceLineRow> _lines = new();
        private Guid? _invoiceId;
        private SalesInvoiceStatus _domainStatus = SalesInvoiceStatus.Draft;
        private bool _cellEditFailed;
        private bool _isSaving;

        public NewSalesInvoiceControl()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        public Guid? SelectedContainerId =>
            CmbContainer.SelectedItem is ContainerPickItem item ? item.Id : null;

        private async void OnLoaded(object sender, RoutedEventArgs e)
        {
            Loaded -= OnLoaded;
            TxtInvoiceNumber.Text = "جديد";
            DpDate.SelectedDate = DateTime.Today;
            ItemsGrid.ItemsSource = _lines;

            await LoadLookupsAsync();

            var editId = SalesNavigationContext.EditInvoiceId;
            if (editId.HasValue)
            {
                await LoadInvoiceAsync(editId.Value);
            }
            else
            {
                EnsureDefaultLine();
            }

            UpdateStatusBadge();
        }

        private void EnsureDefaultLine()
        {
            if (_lines.Count == 0)
            {
                _lines.Add(new SalesInvoiceLineRow());
            }
            RefreshSummary();
        }

        private async Task LoadLookupsAsync()
        {
            await Task.WhenAll(LoadCustomersAsync(), LoadWarehousesAsync(), LoadContainersAsync());
        }

        private async Task LoadCustomersAsync()
        {
            var items = new List<CustomerPickItem>();
            if (AppServices.IsInitialized)
            {
                var result = await CustomerUiService.Instance.GetListAsync("", 1, 200);
                if (result.IsSuccess && result.Value?.Items.Count > 0)
                {
                    items.AddRange(result.Value.Items.Select(c => new CustomerPickItem
                    {
                        Id = c.Id,
                        Display = string.IsNullOrWhiteSpace(c.NameEn) ? c.NameAr : $"{c.NameAr} — {c.NameEn}"
                    }));
                }
            }

            CmbCustomer.ItemsSource = items;
            CmbCustomer.DisplayMemberPath = nameof(CustomerPickItem.Display);
            if (items.Count > 0)
                CmbCustomer.SelectedIndex = 0;
        }

        private async Task LoadWarehousesAsync()
        {
            var items = new List<WarehousePickItem>();
            if (AppServices.IsInitialized)
            {
                var result = await SalesUiService.Instance.GetWarehousesAsync();
                if (result.IsSuccess && result.Value?.Count > 0)
                {
                    items.AddRange(result.Value.Select(w => new WarehousePickItem
                    {
                        Id = w.Id,
                        Display = w.NameAr
                    }));
                }
            }

            if (items.Count == 0)
            {
                items.Add(new WarehousePickItem
                {
                    Id = DatabaseSeeder.DefaultWarehouseId,
                    Display = "المستودع الرئيسي"
                });
            }

            CmbWarehouse.ItemsSource = items;
            CmbWarehouse.DisplayMemberPath = nameof(WarehousePickItem.Display);
            if (items.Count > 0)
                CmbWarehouse.SelectedIndex = 0;
        }

        private async Task LoadContainersAsync()
        {
            var items = new List<ContainerPickItem>();

            if (AppServices.IsInitialized)
            {
                try
                {
                    using var scope = AppServices.CreateScope();
                    var handler = scope.ServiceProvider.GetRequiredService<GetChinaContainerListHandler>();
                    var result = await handler.HandleAsync(new GetChinaContainerListQuery
                    {
                        CompanyId = DatabaseSeeder.DefaultCompanyId,
                        BranchId = DatabaseSeeder.DefaultBranchId,
                        Page = 1,
                        PageSize = 100
                    });

                    if (result.IsSuccess && result.Value?.Items.Count > 0)
                    {
                        items.AddRange(result.Value.Items.Select(c => new ContainerPickItem
                        {
                            Id = c.Id,
                            Display = string.IsNullOrWhiteSpace(c.SupplierName)
                                ? c.ContainerNumber
                                : $"{c.ContainerNumber} — {c.SupplierName}"
                        }));
                    }
                }
                catch
                {
                    // Fall back to sample data below.
                }
            }

            if (items.Count == 0)
            {
                items.AddRange(ChinaImportSampleData.Generate(20).Select(c => new ContainerPickItem
                {
                    Id = Guid.Empty,
                    Display = string.IsNullOrWhiteSpace(c.SupplierName)
                        ? c.ContainerNumber
                        : $"{c.ContainerNumber} — {c.SupplierName}"
                }));
            }

            CmbContainer.ItemsSource = items;
            if (items.Count > 0)
                CmbContainer.SelectedIndex = 0;
        }

        private async Task LoadInvoiceAsync(Guid invoiceId)
        {
            if (!AppServices.IsInitialized)
                return;

            var result = await SalesUiService.Instance.GetOperationsCenterAsync(invoiceId);
            if (!ApplicationResultPresenter.Present(result) || result.Value?.Invoice is null)
                return;

            var invoice = result.Value.Invoice;
            _invoiceId = invoice.Id;
            _domainStatus = invoice.Status;
            TxtInvoiceNumber.Text = invoice.InvoiceNumber;
            DpDate.SelectedDate = invoice.InvoiceDate;

            SelectCustomer(invoice.CustomerId);
            SelectWarehouse(invoice.WarehouseId);
            SelectContainer(invoice.ChinaContainerId);
            BtnCash.IsChecked = invoice.PaymentType == PaymentType.Cash;
            BtnCredit.IsChecked = invoice.PaymentType == PaymentType.Credit;

            _lines.Clear();
            foreach (var line in invoice.Lines.OrderBy(l => l.LineNumber))
            {
                _lines.Add(new SalesInvoiceLineRow
                {
                    GoodsType = "قماش قطن",
                    BoltCode = "FAB-001",
                    Color = "أبيض",
                    RollCount = line.RollCount,
                    UnitPrice = line.UnitPrice,
                    LengthStatus = _domainStatus switch
                    {
                        SalesInvoiceStatus.Detailed or SalesInvoiceStatus.ReadyForApproval or SalesInvoiceStatus.Approved
                            => "مُدخل",
                        SalesInvoiceStatus.AwaitingDetailing => "بانتظار التفصيل",
                        _ => "—"
                    }
                });
            }

            if (_lines.Count == 0)
                EnsureDefaultLine();
            else
                RefreshSummary();

            UpdateStatusBadge();
        }

        private void SelectCustomer(Guid customerId)
        {
            if (CmbCustomer.ItemsSource is IEnumerable<CustomerPickItem> items)
            {
                var match = items.FirstOrDefault(i => i.Id == customerId);
                if (match is not null)
                    CmbCustomer.SelectedItem = match;
            }
        }

        private void SelectWarehouse(Guid warehouseId)
        {
            if (CmbWarehouse.ItemsSource is IEnumerable<WarehousePickItem> items)
            {
                var match = items.FirstOrDefault(i => i.Id == warehouseId);
                if (match is not null)
                    CmbWarehouse.SelectedItem = match;
            }
        }

        private void SelectContainer(Guid containerId)
        {
            if (CmbContainer.ItemsSource is IEnumerable<ContainerPickItem> items)
            {
                var match = items.FirstOrDefault(i => i.Id == containerId);
                if (match is not null)
                    CmbContainer.SelectedItem = match;
            }
        }

        private void BtnCash_Checked(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded) return;
            TxtPartialPayment.Text = "0";
        }

        private async void BtnSaveDraft_Click(object sender, RoutedEventArgs e) =>
            await SaveDraftAsync();

        private async Task SaveDraftAsync()
        {
            if (_isSaving)
                return;

            if (!TryGetHeader(out var customerId, out var warehouseId, out var containerId, out var paymentType))
                return;

            if (_lines.Count == 0 || _lines.All(l => l.RollCount <= 0))
            {
                MockInteractionService.ShowWarning("أضف صنفاً واحداً على الأقل قبل الحفظ.");
                return;
            }

            _isSaving = true;
            try
            {
                var lines = BuildLineCommands();

                if (_invoiceId is null)
                {
                    var createResult = await SalesUiService.Instance.CreateDraftAsync(
                        customerId, warehouseId, containerId, paymentType, lines);
                    if (!ApplicationResultPresenter.Present(createResult))
                        return;

                    _invoiceId = createResult.Value;
                }
                else if (_domainStatus == SalesInvoiceStatus.Draft)
                {
                    var updateResult = await SalesUiService.Instance.UpdateDraftAsync(
                        _invoiceId.Value, customerId, warehouseId, containerId, paymentType, lines);
                    if (!ApplicationResultPresenter.Present(updateResult))
                        return;
                }
                else
                {
                    MockInteractionService.ShowWarning("لا يمكن تعديل الفاتورة إلا وهي في حالة مسودة.");
                    return;
                }

                await ReloadInvoiceAsync();
                SalesListRefreshHub.RequestRefresh();

                MockInteractionService.ShowSuccess(
                    $"تم حفظ المسودة {TxtInvoiceNumber.Text}.",
                    "حفظ مسودة");

                if (_domainStatus != SalesInvoiceStatus.Draft)
                    return;

                if (!MockInteractionService.Confirm(
                        "إرسال الفاتورة للمستودع لتنفيذ الأطوال؟",
                        "إرسال للتنفيذ"))
                    return;

                if (!await SalesUiService.Instance.CanSendToWarehouseAsync())
                {
                    MockInteractionService.ShowWarning("لا تملك صلاحية إرسال الفاتورة للمستودع.");
                    return;
                }

                var sendResult = await SalesUiService.Instance.SendToWarehouseAsync(_invoiceId!.Value);
                if (!ApplicationResultPresenter.Present(sendResult))
                    return;

                await ReloadInvoiceAsync();
                SalesListRefreshHub.RequestRefresh();
                DetailingQueueRefreshHub.RequestRefresh();
                SalesNavigationContext.BeginDetailing(_invoiceId, TxtInvoiceNumber.Text);

                MockInteractionService.ShowSuccess(
                    $"تم إرسال الفاتورة {TxtInvoiceNumber.Text} للمستودع.\nالحالة: {StatusDisplay(_domainStatus)}",
                    "تم الإرسال للمستودع");

                if (MockInteractionService.Confirm("فتح شاشة تفصيل الأطوال الآن؟", "تفصيل المستودع"))
                    MockInteractionService.NavigateToWarehouseDetailing(TxtInvoiceNumber.Text);
            }
            finally
            {
                _isSaving = false;
            }
        }

        private async Task ReloadInvoiceAsync()
        {
            if (_invoiceId is null)
                return;

            var result = await SalesUiService.Instance.GetOperationsCenterAsync(_invoiceId.Value);
            if (!result.IsSuccess || result.Value?.Invoice is null)
                return;

            _domainStatus = result.Value.Invoice.Status;
            TxtInvoiceNumber.Text = result.Value.Invoice.InvoiceNumber;
            UpdateStatusBadge();
        }

        private async void BtnApprove_Click(object sender, RoutedEventArgs e)
        {
            if (_invoiceId is null)
            {
                MockInteractionService.ShowWarning("احفظ الفاتورة كمسودة أولاً قبل الاعتماد.");
                return;
            }

            if (_domainStatus is not (SalesInvoiceStatus.Detailed or SalesInvoiceStatus.ReadyForApproval))
            {
                MockInteractionService.ShowWarning("لا يمكن اعتماد الفاتورة قبل إكمال تفصيل الأطوال في المستودع.");
                return;
            }

            if (!MockInteractionService.Confirm("اعتماد الفاتورة نهائياً؟", "اعتماد الفاتورة"))
                return;

            var result = await SalesUiService.Instance.ApproveAsync(_invoiceId.Value);
            if (!ApplicationResultPresenter.Present(result))
                return;

            await ReloadInvoiceAsync();
            SalesListRefreshHub.RequestRefresh();
            MockInteractionService.ShowSuccess("تم اعتماد الفاتورة بنجاح.", "اعتماد الفاتورة");
        }

        private async void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            if (!MockInteractionService.Confirm("إلغاء الفاتورة والعودة؟", "إلغاء"))
                return;

            if (_invoiceId is not null &&
                _domainStatus is not (SalesInvoiceStatus.Approved or SalesInvoiceStatus.Printed or SalesInvoiceStatus.Delivered or SalesInvoiceStatus.Cancelled))
            {
                var result = await SalesUiService.Instance.CancelAsync(_invoiceId.Value, "إلغاء من شاشة الفاتورة");
                if (!ApplicationResultPresenter.Present(result))
                    return;

                SalesListRefreshHub.RequestRefresh();
            }

            MockInteractionService.Navigate(AppModule.Sales, "Invoices");
        }

        private bool TryGetHeader(
            out Guid customerId,
            out Guid warehouseId,
            out Guid containerId,
            out PaymentType paymentType)
        {
            customerId = Guid.Empty;
            warehouseId = Guid.Empty;
            containerId = Guid.Empty;
            paymentType = PaymentType.Cash;

            if (CmbCustomer.SelectedItem is not CustomerPickItem customer || customer.Id == Guid.Empty)
            {
                MockInteractionService.ShowWarning("اختر العميل.", "فاتورة بيع");
                return false;
            }

            if (CmbWarehouse.SelectedItem is not WarehousePickItem warehouse || warehouse.Id == Guid.Empty)
            {
                MockInteractionService.ShowWarning("اختر المستودع.", "فاتورة بيع");
                return false;
            }

            if (CmbContainer.SelectedItem is not ContainerPickItem container || container.Id == Guid.Empty)
            {
                MockInteractionService.ShowWarning("اختر الحاوية المرتبطة بالفاتورة.", "فاتورة بيع");
                return false;
            }

            customerId = customer.Id;
            warehouseId = warehouse.Id;
            containerId = container.Id;
            paymentType = BtnCredit.IsChecked == true ? PaymentType.Credit : PaymentType.Cash;
            return true;
        }

        private List<SalesInvoiceLineCommand> BuildLineCommands()
        {
            var commands = new List<SalesInvoiceLineCommand>();
            var lineNumber = 1;
            foreach (var row in _lines.Where(l => l.RollCount > 0))
            {
                commands.Add(new SalesInvoiceLineCommand
                {
                    LineNumber = lineNumber++,
                    FabricItemId = SalesCatalogDefaults.FabricItemId,
                    FabricColorId = SalesCatalogDefaults.FabricColorId,
                    RollCount = row.RollCount,
                    UnitPrice = row.UnitPrice
                });
            }
            return commands;
        }

        private void BtnPrint_Click(object sender, RoutedEventArgs e) =>
            MockInteractionService.ShowDocumentPreview($"فاتورة {TxtInvoiceNumber.Text}", "طباعة");

        private void BtnPdf_Click(object sender, RoutedEventArgs e) =>
            MockInteractionService.ShowDocumentPreview($"فاتورة {TxtInvoiceNumber.Text}", "PDF");

        private void BtnPreview_Click(object sender, RoutedEventArgs e) =>
            MockInteractionService.ShowDocumentPreview($"فاتورة {TxtInvoiceNumber.Text}", "معاينة");

        private void UpdateStatusBadge()
        {
            TxtStatusBadge.Text = StatusDisplay(_domainStatus);
            TxtStatusBadge.Foreground = _domainStatus switch
            {
                SalesInvoiceStatus.AwaitingDetailing => Br("WarningBrush"),
                SalesInvoiceStatus.Approved or SalesInvoiceStatus.Printed or SalesInvoiceStatus.Delivered => Br("SuccessBrush"),
                SalesInvoiceStatus.Cancelled => Br("DangerBrush"),
                _ => Br("PrimaryBrush")
            };
        }

        private static string StatusDisplay(SalesInvoiceStatus status) => status switch
        {
            SalesInvoiceStatus.Draft => "مسودة",
            SalesInvoiceStatus.AwaitingDetailing => "بانتظار التفصيل",
            SalesInvoiceStatus.Detailed => "مفصلة",
            SalesInvoiceStatus.ReadyForApproval => "جاهزة للاعتماد",
            SalesInvoiceStatus.Approved => "معتمدة",
            SalesInvoiceStatus.Printed => "مطبوعة",
            SalesInvoiceStatus.Delivered => "مسلمة",
            SalesInvoiceStatus.Cancelled => "ملغاة",
            _ => status.ToString()
        };

        private void BtnAddLine_Click(object sender, RoutedEventArgs e)
        {
            _lines.Add(new SalesInvoiceLineRow());
            RefreshSummary();
        }

        private void BtnRemoveLine_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button { Tag: SalesInvoiceLineRow row })
            {
                _lines.Remove(row);
                RefreshSummary();
            }
        }

        private void ItemsGrid_BeginningEdit(object sender, DataGridBeginningEditEventArgs e)
        {
            if (_domainStatus != SalesInvoiceStatus.Draft)
            {
                e.Cancel = true;
                return;
            }

            if (e.Column is DataGridTemplateColumn)
                e.Cancel = true;
        }

        private void ItemsGrid_PreparingCellForEdit(object sender, DataGridPreparingCellForEditEventArgs e)
        {
            if (e.EditingElement is not TextBox textBox)
                return;

            textBox.AcceptsReturn = false;
            textBox.PreviewKeyDown -= ItemsGrid_EditingTextBox_PreviewKeyDown;
            textBox.PreviewKeyDown += ItemsGrid_EditingTextBox_PreviewKeyDown;
        }

        private void ItemsGrid_EditingTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter)
                return;

            e.Handled = true;
            _cellEditFailed = false;
            ItemsGrid.CommitEdit(DataGridEditingUnit.Cell, true);
            ItemsGrid.CommitEdit(DataGridEditingUnit.Row, true);
            Dispatcher.BeginInvoke(MoveToNextEditableCell);
        }

        private void MoveToNextEditableCell()
        {
            if (_cellEditFailed || ItemsGrid.CurrentItem == null)
                return;

            var rowIndex = ItemsGrid.Items.IndexOf(ItemsGrid.CurrentItem);
            if (rowIndex < 0)
                return;

            var editableColumns = GetEditableColumns();
            if (editableColumns.Count == 0)
                return;

            var currentColumn = ItemsGrid.CurrentColumn;
            var currentIndex = currentColumn == null
                ? -1
                : editableColumns.FindIndex(c => c == currentColumn);

            var nextIndex = currentIndex + 1;
            if (nextIndex >= editableColumns.Count)
                nextIndex = 0;

            var nextColumn = editableColumns[nextIndex];
            ItemsGrid.CurrentCell = new DataGridCellInfo(ItemsGrid.Items[rowIndex], nextColumn);
            ItemsGrid.Focus();
            ItemsGrid.BeginEdit();
        }

        private List<DataGridColumn> GetEditableColumns() =>
            ItemsGrid.Columns
                .Where(c => c is DataGridTextColumn && !c.IsReadOnly)
                .OrderBy(c => c.DisplayIndex)
                .ToList();

        private void ItemsGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            if (e.EditAction != DataGridEditAction.Commit)
                return;

            if (e.Row.Item is not SalesInvoiceLineRow row)
                return;

            _cellEditFailed = false;
            var header = e.Column.Header?.ToString();
            if (e.EditingElement is not TextBox textBox)
            {
                RefreshSummary();
                return;
            }

            var text = textBox.Text.Trim();

            switch (header)
            {
                case "عدد الأثواب":
                    if (!int.TryParse(text, NumberStyles.Integer, CultureInfo.CurrentCulture, out var rolls) || rolls < 0)
                    {
                        e.Cancel = true;
                        _cellEditFailed = true;
                        MockInteractionService.ShowWarning("أدخل عدداً صحيحاً موجباً لعدد الأثواب.");
                        textBox.Text = row.RollCount.ToString(CultureInfo.CurrentCulture);
                        return;
                    }
                    row.RollCount = rolls;
                    break;

                case "سعر الوحدة":
                    if (!decimal.TryParse(text, NumberStyles.Number, CultureInfo.CurrentCulture, out var price) || price < 0)
                    {
                        e.Cancel = true;
                        _cellEditFailed = true;
                        MockInteractionService.ShowWarning("أدخل سعر وحدة صحيحاً.");
                        textBox.Text = row.UnitPrice.ToString(CultureInfo.CurrentCulture);
                        return;
                    }
                    row.UnitPrice = price;
                    break;
            }

            Dispatcher.BeginInvoke(RefreshSummary);
        }

        private void RefreshSummary()
        {
            SummaryPills.Children.Clear();
            var groups = _lines.GroupBy(l => l.GoodsType).ToList();

            foreach (var g in groups)
            {
                SummaryPills.Children.Add(CreatePill(
                    $"مجموع {g.Key}: {g.Sum(x => x.RollCount)} ثوب",
                    Br("SurfaceAltBrush"), Br("TextSecondaryBrush")));
            }

            var total = _lines.Sum(l => l.RollCount);
            SummaryPills.Children.Add(CreatePill(
                $"إجمالي الأثواب: {total} ثوب",
                Br("PrimaryVeryLightBrush"), Br("PrimaryBrush"), bold: true));
        }

        private static Border CreatePill(string text, System.Windows.Media.Brush bg, System.Windows.Media.Brush fg, bool bold = false)
        {
            return new Border
            {
                Background = bg,
                CornerRadius = new CornerRadius(100),
                Padding = new Thickness(12, 6, 12, 6),
                Margin = new Thickness(0, 0, 8, 8),
                Child = new TextBlock
                {
                    Text = text,
                    FontSize = 11,
                    FontWeight = bold ? FontWeights.SemiBold : FontWeights.Normal,
                    Foreground = fg,
                    FontFamily = new System.Windows.Media.FontFamily("Segoe UI, Tahoma, Arial")
                }
            };
        }

        private static System.Windows.Media.Brush Br(string key) =>
            (System.Windows.Media.Brush)System.Windows.Application.Current.Resources[key]!;
    }
}
