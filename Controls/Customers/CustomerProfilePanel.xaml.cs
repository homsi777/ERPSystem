using ERPSystem.Core;
using ERPSystem.Core.Customers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ERPSystem.Controls.Customers
{
    public partial class CustomerProfilePanel : UserControl
    {
        // ══════════════════════════════════════════════════════════
        //  EVENTS — consumed by parent module
        // ══════════════════════════════════════════════════════════

        public event EventHandler<CustomerModel>? EditRequested;
        public event EventHandler<CustomerModel>? NewInvoiceRequested;
        public event EventHandler<CustomerModel>? ReceiptRequested;
        public event EventHandler<CustomerModel>? StatementRequested;
        public event EventHandler<CustomerModel>? ToggleStatusRequested;

        // ══════════════════════════════════════════════════════════
        //  STATE
        // ══════════════════════════════════════════════════════════

        private CustomerModel? _customer;
        private bool _isArabic = true;

        public CustomerProfilePanel()
        {
            InitializeComponent();
            Loaded += (_, _) =>
            {
                _isArabic = LocalizationManager.Instance.IsArabic;
                LocalizationManager.Instance.LanguageChanged += (_, _) =>
                {
                    _isArabic = LocalizationManager.Instance.IsArabic;
                    if (_customer != null) Bind(_customer);
                };
            };
        }

        // ══════════════════════════════════════════════════════════
        //  PUBLIC API
        // ══════════════════════════════════════════════════════════

        public void Bind(CustomerModel customer)
        {
            _customer = customer;
            RenderProfile();
        }

        public void Clear()
        {
            _customer = null;
        }

        // ══════════════════════════════════════════════════════════
        //  RENDER
        // ══════════════════════════════════════════════════════════

        private void RenderProfile()
        {
            if (_customer == null) return;
            var c = _customer;
            string cur = _isArabic ? "ر.س" : "SAR";

            // ── Header ──
            string name = c.DisplayName(_isArabic);
            TxtAvatar.Text = name.Length > 0 ? name[0].ToString() : "؟";
            TxtProfileName.Text = name;
            TxtProfileCode.Text = c.Code;
            TxtProfilePhone.Text = c.Phone;
            TxtProfileRegion.Text = c.Region;

            // ── Type badge ──
            bool isCredit = c.Type == CustomerType.Credit;
            BadgeType.Background = isCredit
                ? (Brush)Application.Current.Resources["InfoBgBrush"]
                : (Brush)Application.Current.Resources["PrimaryVeryLightBrush"];
            TxtBadgeType.Foreground = isCredit
                ? (Brush)Application.Current.Resources["InfoBrush"]
                : (Brush)Application.Current.Resources["PrimaryBrush"];
            TxtBadgeType.Text = c.TypeDisplay(_isArabic);

            // ── Status badge ──
            bool active = c.Status == CustomerStatus.Active;
            BadgeStatus.Background = active
                ? (Brush)Application.Current.Resources["SuccessBgBrush"]
                : (Brush)Application.Current.Resources["DangerBgBrush"];
            TxtBadgeStatus.Foreground = active
                ? (Brush)Application.Current.Resources["SuccessBrush"]
                : (Brush)Application.Current.Resources["DangerBrush"];
            TxtBadgeStatus.Text = c.StatusDisplay(_isArabic);

            // ── Over-limit badge ──
            BadgeOverLimit.Visibility = c.IsOverLimit ? Visibility.Visible : Visibility.Collapsed;
            TxtOverLimit.Text = _isArabic ? "تجاوز الحد الائتماني" : "Over Credit Limit";

            // ── Financial section title ──
            TxtFinancialTitle.Text = _isArabic ? "الملخص المالي" : "FINANCIAL SUMMARY";

            // ── Balance ──
            TxtBalanceLabel.Text = _isArabic ? "الرصيد الحالي" : "Current Balance";
            switch (c.BalanceDirection)
            {
                case BalanceDirection.Debit:
                    BalanceBorder.Background = (Brush)Application.Current.Resources["SuccessBgBrush"];
                    TxtBalanceValue.Foreground = (Brush)Application.Current.Resources["SuccessBrush"];
                    TxtBalanceValue.Text = _isArabic
                        ? $"عليه {c.Balance:N2} {cur}"
                        : $"Owes {cur} {c.Balance:N2}";
                    break;
                case BalanceDirection.Credit:
                    BalanceBorder.Background = (Brush)Application.Current.Resources["DangerBgBrush"];
                    TxtBalanceValue.Foreground = (Brush)Application.Current.Resources["DangerBrush"];
                    TxtBalanceValue.Text = _isArabic
                        ? $"له {Math.Abs(c.Balance):N2} {cur}"
                        : $"Credit {cur} {Math.Abs(c.Balance):N2}";
                    break;
                default:
                    BalanceBorder.Background = (Brush)Application.Current.Resources["SurfaceAltBrush"];
                    TxtBalanceValue.Foreground = (Brush)Application.Current.Resources["TextMutedBrush"];
                    TxtBalanceValue.Text = _isArabic ? "صفر" : "Zero";
                    break;
            }

            // ── Credit limit (credit customers only) ──
            CreditLimitRow.Visibility = isCredit ? Visibility.Visible : Visibility.Collapsed;
            if (isCredit)
            {
                TxtCreditLimitLabel.Text = _isArabic ? "الحد الائتماني" : "Credit Limit";
                TxtCreditLimitValue.Text = _isArabic
                    ? $"{c.CreditLimit:N2} {cur}"
                    : $"{cur} {c.CreditLimit:N2}";

                TxtAvailableCreditLabel.Text = _isArabic ? "المتاح الائتماني" : "Available Credit";
                decimal avail = c.AvailableCredit;
                TxtAvailableCreditValue.Text = _isArabic
                    ? $"{avail:N2} {cur}"
                    : $"{cur} {avail:N2}";
                TxtAvailableCreditValue.Foreground = avail > 0
                    ? (Brush)Application.Current.Resources["SuccessBrush"]
                    : (Brush)Application.Current.Resources["DangerBrush"];

                // Credit usage bar width: bounded 0–100%
                double usedPct = c.CreditLimit > 0
                    ? Math.Min(1.0, (double)(c.Balance / c.CreditLimit))
                    : 0;
                CreditUsageBar.Width = usedPct * 260; // approximate panel inner width
                CreditUsageBar.Background = c.IsOverLimit
                    ? (Brush)Application.Current.Resources["DangerBrush"]
                    : usedPct > 0.8
                        ? (Brush)Application.Current.Resources["WarningBrush"]
                        : (Brush)Application.Current.Resources["PrimaryBrush"];
            }

            // ── Invoice count + last activity ──
            TxtInvoicesLabel.Text = _isArabic ? "إجمالي الفواتير" : "Total Invoices";
            TxtInvoicesValue.Text = c.TotalInvoices.ToString("N0");

            TxtLastActivityLabel.Text = _isArabic ? "آخر معاملة" : "Last Activity";
            TxtLastActivityValue.Text = c.LastInvoiceDisplay(_isArabic);

            // ── Contact section title ──
            TxtContactTitle.Text = _isArabic ? "معلومات الاتصال" : "CONTACT INFO";

            // ── Phone 2 ──
            Phone2Row.Visibility = !string.IsNullOrEmpty(c.Phone2) ? Visibility.Visible : Visibility.Collapsed;
            TxtPhone2Label.Text = _isArabic ? "هاتف إضافي" : "Alt Phone";
            TxtPhone2Value.Text = c.Phone2;

            // ── Address ──
            TxtAddressLabel.Text = _isArabic ? "العنوان" : "Address";
            TxtAddressValue.Text = string.IsNullOrEmpty(c.Address) ? (_isArabic ? "—" : "—") : c.Address;

            // ── Sales rep ──
            TxtRepLabel.Text = _isArabic ? "المندوب" : "Sales Rep";
            TxtRepValue.Text = string.IsNullOrEmpty(c.SalesRep) ? (_isArabic ? "—" : "—") : c.SalesRep;

            // ── Notes ──
            NotesRow.Visibility = !string.IsNullOrEmpty(c.Notes) ? Visibility.Visible : Visibility.Collapsed;
            TxtNotesValue.Text = c.Notes;

            // ── Mini statement ──
            TxtStatementTitle.Text = _isArabic ? "آخر المعاملات" : "RECENT TRANSACTIONS";
            BuildMiniStatement();

            TxtBtnFullStatement.Text = _isArabic ? "كشف الحساب الكامل" : "Full Statement";

            // ── Quick action buttons ──
            TxtBtnNewInvoice.Text = _isArabic ? "فاتورة جديدة" : "New Invoice";
            TxtBtnReceipt.Text = _isArabic ? "سند قبض" : "Receipt";
            TxtBtnEdit.Text = _isArabic ? "تعديل" : "Edit";
            TxtBtnToggleStatus.Text = active
                ? (_isArabic ? "تعطيل" : "Suspend")
                : (_isArabic ? "تفعيل" : "Activate");
        }

        private void BuildMiniStatement()
        {
            TransactionsPanel.Children.Clear();
            if (_customer == null) return;

            foreach (var txn in _customer.RecentTransactions.Take(5))
            {
                var row = new Border
                {
                    Padding = new Thickness(0, 7, 0, 7),
                    BorderBrush = (Brush)Application.Current.Resources["BorderLightBrush"],
                    BorderThickness = new Thickness(0, 0, 0, 1)
                };

                var grid = new Grid();
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                // Icon
                var iconBorder = new Border
                {
                    Width = 28, Height = 28, CornerRadius = new CornerRadius(6),
                    Background = txn.IsDebit
                        ? (Brush)Application.Current.Resources["SuccessBgBrush"]
                        : (Brush)Application.Current.Resources["InfoBgBrush"],
                    Margin = new Thickness(0, 0, 10, 0)
                };
                iconBorder.Child = new TextBlock
                {
                    Text = txn.TypeIcon,
                    FontFamily = new FontFamily("Segoe MDL2 Assets"),
                    FontSize = 13,
                    Foreground = txn.IsDebit
                        ? (Brush)Application.Current.Resources["SuccessBrush"]
                        : (Brush)Application.Current.Resources["InfoBrush"],
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };
                Grid.SetColumn(iconBorder, 0);
                grid.Children.Add(iconBorder);

                // Info
                var info = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
                info.Children.Add(new TextBlock
                {
                    Text = _isArabic ? txn.TypeDisplayAr : txn.TypeDisplayEn,
                    FontSize = 12, FontWeight = FontWeights.SemiBold,
                    Foreground = (Brush)Application.Current.Resources["TextPrimaryBrush"],
                    FontFamily = new FontFamily("Segoe UI, Tahoma, Arial")
                });
                info.Children.Add(new TextBlock
                {
                    Text = txn.Reference + " · " + txn.Date.ToString("dd/MM/yy"),
                    FontSize = 10,
                    Foreground = (Brush)Application.Current.Resources["TextMutedBrush"],
                    FontFamily = new FontFamily("Segoe UI"),
                    Margin = new Thickness(0, 2, 0, 0)
                });
                Grid.SetColumn(info, 1);
                grid.Children.Add(info);

                // Amount
                string cur = _isArabic ? "ر.س" : "SAR";
                string amtText = _isArabic
                    ? $"{txn.Amount:N2} {cur}"
                    : $"{cur} {txn.Amount:N2}";

                var amtBlock = new TextBlock
                {
                    Text = amtText,
                    FontSize = 12, FontWeight = FontWeights.Bold,
                    Foreground = txn.IsDebit
                        ? (Brush)Application.Current.Resources["SuccessBrush"]
                        : (Brush)Application.Current.Resources["DangerBrush"],
                    FontFamily = new FontFamily("Segoe UI"),
                    VerticalAlignment = VerticalAlignment.Center
                };
                Grid.SetColumn(amtBlock, 2);
                grid.Children.Add(amtBlock);

                row.Child = grid;
                TransactionsPanel.Children.Add(row);
            }

            if (!_customer.RecentTransactions.Any())
            {
                TransactionsPanel.Children.Add(new TextBlock
                {
                    Text = _isArabic ? "لا توجد معاملات سابقة" : "No transactions yet",
                    FontSize = 12,
                    Foreground = (Brush)Application.Current.Resources["TextMutedBrush"],
                    FontFamily = new FontFamily("Segoe UI"),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 8, 0, 0)
                });
            }
        }

        // ══════════════════════════════════════════════════════════
        //  EVENT HANDLERS
        // ══════════════════════════════════════════════════════════

        private void BtnNewInvoice_Click(object sender, RoutedEventArgs e)
        {
            if (_customer != null) NewInvoiceRequested?.Invoke(this, _customer);
        }

        private void BtnReceipt_Click(object sender, RoutedEventArgs e)
        {
            if (_customer != null) ReceiptRequested?.Invoke(this, _customer);
        }

        private void BtnEdit_Click(object sender, RoutedEventArgs e)
        {
            if (_customer != null) EditRequested?.Invoke(this, _customer);
        }

        private void BtnToggleStatus_Click(object sender, RoutedEventArgs e)
        {
            if (_customer != null) ToggleStatusRequested?.Invoke(this, _customer);
        }

        private void BtnFullStatement_Click(object sender, RoutedEventArgs e)
        {
            if (_customer != null) StatementRequested?.Invoke(this, _customer);
        }
    }
}
