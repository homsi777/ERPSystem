using ERPSystem.Core;
using ERPSystem.Core.Sales;
using System.Windows;

namespace ERPSystem.Dialogs
{
    public partial class NewInvoiceDialog : Window
    {
        private readonly bool _isArabic;
        public SalesInvoice? Result { get; private set; }

        public NewInvoiceDialog()
        {
            InitializeComponent();
            _isArabic = LocalizationManager.Instance.IsArabic;
            FlowDirection = _isArabic ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
            Loaded += (_, _) => ApplyLabels();
        }

        private void ApplyLabels()
        {
            bool ar = _isArabic;
            Title = ar ? "فاتورة مبيعات جديدة" : "New Sales Invoice";
            TxtTitle.Text = Title;
            TxtInvoiceNum.Text = $"INV-2026-{new Random().Next(1050, 1999):D4}";

            TxtLabelCustomer.Text  = ar ? "العميل" : "Customer";
            TxtLabelType.Text      = ar ? "نوع الفاتورة" : "Invoice Type";
            TxtLabelPayMethod.Text = ar ? "طريقة الدفع" : "Payment Method";
            TxtCustomerSearch.Text = ar ? "" : "";
            TxtCustomerSearch.Tag  = ar ? "ابحث عن عميل..." : "Search customer...";

            CmbCash.Content      = ar ? "نقدي" : "Cash";
            CmbCredit.Content    = ar ? "آجل"  : "Credit";
            CmbPayCash.Content   = ar ? "نقداً" : "Cash";
            CmbPayCard.Content   = ar ? "بطاقة" : "Card";
            CmbPayTransfer.Content = ar ? "تحويل" : "Transfer";

            TxtColItem.Text     = ar ? "الصنف" : "Item";
            TxtColQty.Text      = ar ? "الكمية" : "Qty";
            TxtColPrice.Text    = ar ? "السعر" : "Price";
            TxtColDiscount.Text = ar ? "الخصم" : "Disc.";
            TxtColTotal.Text    = ar ? "الإجمالي" : "Total";

            TxtBtnAddLine.Text  = ar ? "إضافة صنف" : "Add Item";
            TxtNotes.Tag        = ar ? "ملاحظات..." : "Notes...";

            TxtTotalsTitle.Text = ar ? "ملخص الفاتورة" : "INVOICE SUMMARY";
            TxtLblSubtotal.Text = ar ? "المجموع الجزئي" : "Subtotal";
            TxtLblDiscount.Text = ar ? "إجمالي الخصم"  : "Discount";
            TxtLblVat.Text      = ar ? "ضريبة 15%"     : "VAT 15%";
            TxtLblGrand.Text    = ar ? "الإجمالي"       : "Total";
            TxtLblPaid.Text     = ar ? "المدفوع"        : "Amount Paid";

            BtnSaveDraft.Content  = ar ? "حفظ كمسودة"    : "Save Draft";
            BtnPost.Content       = ar ? "ترحيل الفاتورة" : "Post Invoice";
            BtnCancelDialog.Content = ar ? "إلغاء"        : "Cancel";
        }

        private void BtnSelectCustomer_Click(object sender, RoutedEventArgs e)
        {
            // TODO: open customer selection dialog
            MessageBox.Show(
                _isArabic ? "سيتم فتح قائمة اختيار العملاء" : "Customer selection coming soon",
                _isArabic ? "اختيار العميل" : "Select Customer",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void BtnAddLine_Click(object sender, RoutedEventArgs e)
        {
            // TODO: attach invoice lines data binding
            MessageBox.Show(
                _isArabic ? "سيتم فتح بحث الأصناف لإضافة سطر جديد" : "Item search for new line coming soon",
                _isArabic ? "إضافة صنف" : "Add Item",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void BtnSaveDraft_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TxtCustomerSearch.Text))
            {
                TxtValidation.Text = _isArabic ? "يرجى اختيار عميل" : "Please select a customer";
                TxtValidation.Visibility = Visibility.Visible;
                return;
            }
            Result = BuildDraftInvoice(InvoiceStatus.Draft);
            DialogResult = true;
            Close();
        }

        private void BtnPost_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TxtCustomerSearch.Text))
            {
                TxtValidation.Text = _isArabic ? "يرجى اختيار عميل" : "Please select a customer";
                TxtValidation.Visibility = Visibility.Visible;
                return;
            }
            var confirm = MessageBox.Show(
                _isArabic ? "هل تريد ترحيل الفاتورة؟ لا يمكن تعديلها بعد الترحيل." : "Post this invoice? It cannot be edited after posting.",
                _isArabic ? "تأكيد الترحيل" : "Confirm Post",
                MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (confirm != MessageBoxResult.Yes) return;

            Result = BuildDraftInvoice(InvoiceStatus.Posted);
            DialogResult = true;
            Close();
        }

        private SalesInvoice BuildDraftInvoice(InvoiceStatus status)
        {
            decimal.TryParse(TxtPaidAmount.Text.Replace(",", ""), out decimal paid);
            var inv = new SalesInvoice
            {
                InvoiceNumber  = TxtInvoiceNum.Text,
                Date           = DateTime.Now,
                Branch         = "الرئيسي",
                UserName       = LocalizationManager.Instance.IsArabic ? "المستخدم الحالي" : "Current User",
                CustomerNameAr = TxtCustomerSearch.Text,
                CustomerNameEn = TxtCustomerSearch.Text,
                CustomerPhone  = "",
                Type           = CmbType.SelectedIndex == 1 ? InvoiceType.Credit : InvoiceType.Cash,
                Notes          = TxtNotes.Text
            };
            inv.Status      = status;
            inv.PaidAmount  = paid;
            return inv;
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e) => Close();
    }
}
