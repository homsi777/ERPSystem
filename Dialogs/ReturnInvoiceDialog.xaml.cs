using ERPSystem.Core;
using ERPSystem.Core.Sales;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ERPSystem.Dialogs
{
    public partial class ReturnInvoiceDialog : Window
    {
        private readonly SalesInvoice _original;
        private readonly bool _isArabic;
        public bool Confirmed { get; private set; }

        public ReturnInvoiceDialog(SalesInvoice original)
        {
            InitializeComponent();
            _original  = original;
            _isArabic  = LocalizationManager.Instance.IsArabic;
            FlowDirection = _isArabic ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
            Loaded += (_, _) => { ApplyLabels(); BuildReturnLines(); };
        }

        private void ApplyLabels()
        {
            bool ar = _isArabic;
            Title = ar ? "مرتجع مبيعات" : "Sales Return";
            TxtTitle.Text = Title;

            TxtRefInvoiceLabel.Text = ar ? "الفاتورة المرجعية" : "Reference Invoice";
            TxtRefInvoiceNum.Text   = _original.InvoiceNumber + " — " + _original.CustomerName(ar);

            TxtLblReason.Text  = ar ? "سبب الإرجاع" : "Return Reason";
            ReasonDefect.Content        = ar ? "بضاعة معيبة"        : "Defective goods";
            ReasonWrong.Content         = ar ? "صنف خاطئ"           : "Wrong item";
            ReasonCustomerCancel.Content = ar ? "إلغاء من العميل"   : "Customer cancellation";
            ReasonOther.Content         = ar ? "سبب آخر"            : "Other reason";

            TxtColItem.Text     = ar ? "الصنف"     : "Item";
            TxtColInvoiced.Text = ar ? "الكمية"    : "Invoiced";
            TxtColReturn.Text   = ar ? "إرجاع"     : "Return";
            TxtColAmount.Text   = ar ? "الإجمالي"  : "Amount";

            TxtLblRefund.Text  = ar ? "طريقة رد المبلغ" : "Refund Method";
            RefundCash.Content    = ar ? "نقداً"            : "Cash";
            RefundBalance.Content = ar ? "إضافة للرصيد"    : "Add to balance";
            RefundVoucher.Content = ar ? "قسيمة ائتمان"    : "Credit voucher";

            TxtLblNotes.Text = ar ? "ملاحظات" : "Notes";

            BtnCancel.Content       = ar ? "إلغاء"         : "Cancel";
            BtnConfirmReturn.Content = ar ? "تأكيد الإرجاع" : "Confirm Return";
        }

        private void BuildReturnLines()
        {
            ReturnItemsPanel.Children.Clear();
            string cur = _isArabic ? "ر.س" : "SAR";

            foreach (var line in _original.Lines)
            {
                var row = new Border
                {
                    Padding = new Thickness(0, 9, 0, 9),
                    BorderBrush = (Brush)Application.Current.Resources["BorderLightBrush"],
                    BorderThickness = new Thickness(0, 0, 0, 1)
                };

                var grid = new Grid();
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(70) });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(100) });

                // Item name
                grid.Children.Add(new TextBlock
                {
                    Text = line.ItemName(_isArabic), FontSize = 12, FontWeight = FontWeights.SemiBold,
                    Foreground = (Brush)Application.Current.Resources["TextPrimaryBrush"],
                    FontFamily = new FontFamily("Segoe UI, Tahoma, Arial"),
                    VerticalAlignment = VerticalAlignment.Center,
                    TextTrimming = TextTrimming.CharacterEllipsis
                });

                // Invoiced qty
                var qtyTxt = new TextBlock
                {
                    Text = line.Qty.ToString("N0"), FontSize = 12,
                    Foreground = (Brush)Application.Current.Resources["TextSecondaryBrush"],
                    FontFamily = new FontFamily("Segoe UI"),
                    TextAlignment = TextAlignment.Center, VerticalAlignment = VerticalAlignment.Center
                };
                Grid.SetColumn(qtyTxt, 1);
                grid.Children.Add(qtyTxt);

                // Return qty input
                var returnBorder = new Border
                {
                    Background = Brushes.White,
                    BorderBrush = (Brush)Application.Current.Resources["BorderBrush"],
                    BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(5),
                    Padding = new Thickness(4, 2, 4, 2), Margin = new Thickness(4, 0, 4, 0)
                };
                returnBorder.Child = new TextBox
                {
                    Text = line.Qty.ToString("N0"), FontSize = 12,
                    Background = Brushes.Transparent, BorderThickness = new Thickness(0),
                    FontFamily = new FontFamily("Segoe UI"),
                    Foreground = (Brush)Application.Current.Resources["TextPrimaryBrush"],
                    TextAlignment = TextAlignment.Center
                };
                Grid.SetColumn(returnBorder, 2);
                grid.Children.Add(returnBorder);

                // Amount
                var amtTxt = new TextBlock
                {
                    Text = _isArabic ? $"{line.LineTotal:N2} {cur}" : $"{cur} {line.LineTotal:N2}",
                    FontSize = 12, FontWeight = FontWeights.Bold,
                    Foreground = (Brush)Application.Current.Resources["WarningBrush"],
                    FontFamily = new FontFamily("Segoe UI"),
                    TextAlignment = TextAlignment.Right, VerticalAlignment = VerticalAlignment.Center
                };
                Grid.SetColumn(amtTxt, 3);
                grid.Children.Add(amtTxt);

                row.Child = grid;
                ReturnItemsPanel.Children.Add(row);
            }

            // Show return total
            string totalText = _isArabic
                ? $"إجمالي المرتجع: {_original.GrandTotal:N2} ر.س"
                : $"Return Total: SAR {_original.GrandTotal:N2}";
            TxtReturnTotal.Text = totalText;
        }

        private void BtnConfirmReturn_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                _isArabic
                    ? $"تأكيد إرجاع الفاتورة {_original.InvoiceNumber}؟\nهذا الإجراء لا يمكن التراجع عنه."
                    : $"Confirm return of invoice {_original.InvoiceNumber}?\nThis cannot be undone.",
                _isArabic ? "تأكيد الإرجاع" : "Confirm Return",
                MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                Confirmed = true;
                _original.Status = InvoiceStatus.Returned;
                DialogResult = true;
                Close();
            }
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e) => Close();
    }
}
