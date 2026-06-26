using ERPSystem.Core;
using ERPSystem.Core.Sales;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ERPSystem.Controls.Sales
{
    public partial class InvoiceDetailsPanel : UserControl
    {
        // ══ Events ═══════════════════════════════════════════
        public event EventHandler<SalesInvoice>? EditRequested;
        public event EventHandler<SalesInvoice>? PrintRequested;
        public event EventHandler<SalesInvoice>? ReturnRequested;
        public event EventHandler<SalesInvoice>? ReceiptRequested;
        public event EventHandler<SalesInvoice>? OpenCustomerRequested;
        public event EventHandler<SalesInvoice>? CancelRequested;
        public event EventHandler<SalesInvoice>? ViewLinesRequested;

        private SalesInvoice? _invoice;
        private bool _isArabic = true;

        public InvoiceDetailsPanel()
        {
            InitializeComponent();
            Loaded += (_, _) =>
            {
                _isArabic = LocalizationManager.Instance.IsArabic;
                LocalizationManager.Instance.LanguageChanged += (_, _) =>
                {
                    _isArabic = LocalizationManager.Instance.IsArabic;
                    if (_invoice != null) Bind(_invoice);
                };
            };
        }

        // ══ Public API ════════════════════════════════════════

        public void Bind(SalesInvoice invoice)
        {
            _invoice = invoice;
            Render();
        }

        // ══ Render ════════════════════════════════════════════

        private void Render()
        {
            if (_invoice == null) return;
            var inv = _invoice;
            bool ar = _isArabic;
            string cur = ar ? "ر.س" : "SAR";

            // ── Invoice header ──
            TxtInvoiceNum.Text  = inv.InvoiceNumber;
            TxtInvoiceDate.Text = inv.Date.ToString(ar ? "dd/MM/yyyy  HH:mm" : "MM/dd/yyyy  hh:mm tt");

            // Doc status badge
            (BadgeDocStatus.Background, TxtDocStatus.Foreground, TxtDocStatus.Text) = inv.Status switch
            {
                InvoiceStatus.Posted    => (Res("SuccessBgBrush"), Res("SuccessBrush"), inv.StatusDisplay(ar)),
                InvoiceStatus.Cancelled => (Res("DangerBgBrush"),  Res("DangerBrush"),  inv.StatusDisplay(ar)),
                InvoiceStatus.Returned  => (Res("WarningBgBrush"), Res("WarningBrush"), inv.StatusDisplay(ar)),
                _                       => (Res("SurfaceAltBrush"), Res("TextMutedBrush"), inv.StatusDisplay(ar))
            };

            // Pay status badge
            (BadgePayStatus.Background, TxtPayStatus.Foreground, TxtPayStatus.Text) = inv.PaymentStatus switch
            {
                PaymentStatus.Paid    => (Res("SuccessBgBrush"), Res("SuccessBrush"), inv.PayStatusDisplay(ar)),
                PaymentStatus.Partial => (Res("WarningBgBrush"), Res("WarningBrush"), inv.PayStatusDisplay(ar)),
                _                     => (Res("DangerBgBrush"),  Res("DangerBrush"),  inv.PayStatusDisplay(ar))
            };

            // Type badge
            bool isCredit = inv.Type == InvoiceType.Credit;
            BadgeType.Background = isCredit ? Res("InfoBgBrush") : Res("PrimaryVeryLightBrush");
            TxtType.Foreground   = isCredit ? Res("InfoBrush")   : Res("PrimaryBrush");
            TxtType.Text         = inv.TypeDisplay(ar);

            TxtPayMethod.Text = inv.PayMethodDisplay(ar);
            TxtUser.Text      = inv.UserName + " · " + inv.Branch;

            // ── Customer block ──
            TxtCustomerSectionTitle.Text = ar ? "العميل" : "CUSTOMER";
            TxtCustomerName.Text  = inv.CustomerName(ar);
            TxtCustPhone.Text     = inv.CustomerPhone;
            TxtCustTypeLabel.Text = ar ? "نوع الحساب" : "Account Type";
            TxtCustTypeVal.Text   = inv.CustomerIsCredit ? (ar ? "آجل" : "Credit") : (ar ? "نقدي" : "Cash");
            TxtCustBalLabel.Text  = ar ? "الرصيد" : "Balance";
            TxtCustBalVal.Text    = ar ? $"{inv.CustomerBalance:N2} {cur}" : $"{cur} {inv.CustomerBalance:N2}";
            TxtCustBalVal.Foreground = inv.CustomerBalance > 0 ? Res("SuccessBrush") : Res("TextMutedBrush");
            BadgeOverLimit.Visibility = inv.CustomerOverLimit ? Visibility.Visible : Visibility.Collapsed;
            TxtOverLimit.Text = ar ? "تجاوز الحد" : "Over Limit";

            // ── Financial block ──
            TxtFinancialTitle.Text  = ar ? "الملخص المالي" : "FINANCIAL SUMMARY";
            TxtSubtotalLabel.Text   = ar ? "المجموع الجزئي" : "Subtotal";
            TxtSubtotalVal.Text     = ar ? $"{inv.Subtotal:N2} {cur}" : $"{cur} {inv.Subtotal:N2}";
            TxtDiscountLabel.Text   = ar ? "إجمالي الخصم" : "Total Discount";
            TxtDiscountVal.Text     = ar ? $"− {inv.TotalDiscount:N2} {cur}" : $"− {cur} {inv.TotalDiscount:N2}";
            DiscountRow.Visibility  = inv.TotalDiscount > 0 ? Visibility.Visible : Visibility.Collapsed;
            TxtTaxLabel.Text        = ar ? "ضريبة القيمة المضافة 15%" : "VAT 15%";
            TxtTaxVal.Text          = ar ? $"{inv.TaxAmount:N2} {cur}" : $"{cur} {inv.TaxAmount:N2}";
            TxtGrandLabel.Text      = ar ? "الإجمالي" : "Grand Total";
            TxtGrandVal.Text        = ar ? $"{inv.GrandTotal:N2} {cur}" : $"{cur} {inv.GrandTotal:N2}";
            GrandTotalBorder.Background = Res("SurfaceAltBrush");

            TxtPaidLabel.Text     = ar ? "المدفوع" : "Paid";
            TxtPaidVal.Text       = ar ? $"{inv.PaidAmount:N2} {cur}" : $"{cur} {inv.PaidAmount:N2}";
            TxtRemainingLabel.Text = ar ? "المتبقي" : "Remaining";
            TxtRemainingVal.Text  = ar ? $"{inv.RemainingAmount:N2} {cur}" : $"{cur} {inv.RemainingAmount:N2}";
            TxtRemainingVal.Foreground = inv.RemainingAmount > 0 ? Res("DangerBrush") : Res("SuccessBrush");

            // ── Lines ──
            TxtLinesSectionTitle.Text = ar ? "الأصناف" : "LINE ITEMS";
            BuildLinesPreview();
            TxtBtnViewLines.Text = ar ? $"عرض كل الأصناف ({inv.Lines.Count})" : $"View all items ({inv.Lines.Count})";

            // ── Action buttons ──
            TxtBtnPrint.Text        = ar ? "طباعة" : "Print";
            TxtBtnEdit.Text         = ar ? "تعديل" : "Edit";
            TxtBtnReturn.Text       = ar ? "مرتجع" : "Return";
            TxtBtnReceipt.Text      = ar ? "سند قبض" : "Receipt";
            TxtBtnOpenCustomer.Text = ar ? "فتح العميل" : "Open Customer";
            TxtBtnCancel.Text       = ar ? "إلغاء الفاتورة" : "Cancel Invoice";

            BtnEdit.IsEnabled   = inv.CanEdit;
            BtnReturn.IsEnabled = inv.CanReturn;
            BtnCancel.IsEnabled = inv.CanCancel;
            BtnReceipt.IsEnabled = inv.Status == InvoiceStatus.Posted && inv.RemainingAmount > 0;

            BtnEdit.Opacity   = BtnEdit.IsEnabled   ? 1.0 : 0.4;
            BtnReturn.Opacity = BtnReturn.IsEnabled ? 1.0 : 0.4;
            BtnCancel.Opacity = BtnCancel.IsEnabled ? 1.0 : 0.4;
        }

        private void BuildLinesPreview()
        {
            LinesPanel.Children.Clear();
            if (_invoice == null) return;

            bool ar = _isArabic;
            string cur = ar ? "ر.س" : "SAR";

            foreach (var line in _invoice.Lines.Take(6))
            {
                var row = new Border
                {
                    Padding = new Thickness(0, 7, 0, 7),
                    BorderBrush = (Brush)Application.Current.Resources["BorderLightBrush"],
                    BorderThickness = new Thickness(0, 0, 0, 1)
                };

                var grid = new Grid();
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                var info = new StackPanel();
                info.Children.Add(new TextBlock
                {
                    Text = line.ItemName(ar), FontSize = 12, FontWeight = FontWeights.SemiBold,
                    Foreground = (Brush)Application.Current.Resources["TextPrimaryBrush"],
                    FontFamily = new FontFamily("Segoe UI, Tahoma, Arial"),
                    TextTrimming = TextTrimming.CharacterEllipsis
                });
                info.Children.Add(new TextBlock
                {
                    Text = ar
                        ? $"{line.Qty} × {line.UnitPrice:N2} {cur}"
                        : $"{line.Qty} × {cur} {line.UnitPrice:N2}",
                    FontSize = 11,
                    Foreground = (Brush)Application.Current.Resources["TextMutedBrush"],
                    FontFamily = new FontFamily("Segoe UI"),
                    Margin = new Thickness(0, 2, 0, 0)
                });
                Grid.SetColumn(info, 0);
                grid.Children.Add(info);

                var totalBlock = new TextBlock
                {
                    Text = ar ? $"{line.LineTotal:N2} {cur}" : $"{cur} {line.LineTotal:N2}",
                    FontSize = 12, FontWeight = FontWeights.Bold,
                    Foreground = (Brush)Application.Current.Resources["PrimaryBrush"],
                    FontFamily = new FontFamily("Segoe UI"),
                    VerticalAlignment = VerticalAlignment.Center
                };
                Grid.SetColumn(totalBlock, 1);
                grid.Children.Add(totalBlock);

                row.Child = grid;
                LinesPanel.Children.Add(row);
            }
        }

        // ══ Helpers ═══════════════════════════════════════════

        private static Brush Res(string key) =>
            (Brush)Application.Current.Resources[key];

        // ══ Button handlers ═══════════════════════════════════

        private void BtnPrint_Click(object sender, RoutedEventArgs e)         => PrintRequested?.Invoke(this, _invoice!);
        private void BtnEdit_Click(object sender, RoutedEventArgs e)           => EditRequested?.Invoke(this, _invoice!);
        private void BtnReturn_Click(object sender, RoutedEventArgs e)         => ReturnRequested?.Invoke(this, _invoice!);
        private void BtnReceipt_Click(object sender, RoutedEventArgs e)        => ReceiptRequested?.Invoke(this, _invoice!);
        private void BtnOpenCustomer_Click(object sender, RoutedEventArgs e)   => OpenCustomerRequested?.Invoke(this, _invoice!);
        private void BtnCancel_Click(object sender, RoutedEventArgs e)         => CancelRequested?.Invoke(this, _invoice!);
        private void BtnViewAllLines_Click(object sender, RoutedEventArgs e)   => ViewLinesRequested?.Invoke(this, _invoice!);
    }
}
