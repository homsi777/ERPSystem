using ERPSystem.Core;
using ERPSystem.Core.Customers;
using System.Windows;
using System.Windows.Media;

namespace ERPSystem.Dialogs
{
    public partial class AddEditCustomerDialog : Window
    {
        private readonly bool _isEditMode;
        private readonly bool _isArabic;
        public CustomerModel? Result { get; private set; }

        // ══════════════════════════════════════════════════════════
        //  CONSTRUCTION
        // ══════════════════════════════════════════════════════════

        /// <param name="existing">Pass existing customer for edit mode, null for add mode.</param>
        public AddEditCustomerDialog(CustomerModel? existing = null)
        {
            InitializeComponent();
            _isArabic = LocalizationManager.Instance.IsArabic;
            _isEditMode = existing != null;

            FlowDirection = _isArabic ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
            Loaded += (_, _) =>
            {
                ApplyLabels();
                if (existing != null) PopulateFields(existing);
                else AutoGenerateCode();
            };
        }

        // ══════════════════════════════════════════════════════════
        //  LABELS
        // ══════════════════════════════════════════════════════════

        private void ApplyLabels()
        {
            bool ar = _isArabic;

            Title = ar
                ? (_isEditMode ? "تعديل بيانات العميل" : "إضافة عميل جديد")
                : (_isEditMode ? "Edit Customer" : "Add New Customer");

            TxtDialogTitle.Text = Title;
            TxtSectionBasic.Text = ar ? "البيانات الأساسية" : "BASIC INFORMATION";

            TxtLabelName.Text = ar ? "اسم العميل" : "Customer Name";
            TxtLabelCode.Text = ar ? "كود العميل" : "Customer Code";
            TxtLabelPhone.Text = ar ? "رقم الهاتف" : "Phone";
            TxtLabelPhone2.Text = ar ? "هاتف إضافي" : "Alt Phone";
            TxtLabelType.Text = ar ? "نوع الحساب" : "Account Type";
            TxtLabelCreditLimit.Text = ar ? "الحد الائتماني" : "Credit Limit";
            TxtLabelAddress.Text = ar ? "العنوان" : "Address";
            TxtLabelRegion.Text = ar ? "المنطقة" : "Region";
            TxtLabelRep.Text = ar ? "المندوب" : "Sales Rep";
            TxtLabelTax.Text = ar ? "الرقم الضريبي" : "Tax Number";
            TxtLabelNotes.Text = ar ? "ملاحظات" : "Notes";

            CmbTypeCash.Content = ar ? "نقدي" : "Cash";
            CmbTypeCredit.Content = ar ? "آجل" : "Credit";

            TxtRequiredNote.Text = ar ? "* الحقول المطلوبة" : "* Required fields";
            BtnCancel.Content = ar ? "إلغاء" : "Cancel";
            BtnSave.Content = _isEditMode
                ? (ar ? "حفظ التعديلات" : "Save Changes")
                : (ar ? "إضافة العميل" : "Add Customer");

            // placeholder hints
            TxtName.Tag = ar ? "محمد أحمد العتيبي..." : "John Smith...";
            TxtPhone.Tag = ar ? "05xxxxxxxx" : "Phone number";
            TxtNotes.Tag = ar ? "ملاحظات اختيارية..." : "Optional notes...";
            TxtCreditLimit.Tag = "0";
        }

        // ══════════════════════════════════════════════════════════
        //  POPULATE (edit mode)
        // ══════════════════════════════════════════════════════════

        private void PopulateFields(CustomerModel c)
        {
            TxtName.Text = c.DisplayName(_isArabic);
            TxtCode.Text = c.Code;
            TxtPhone.Text = c.Phone;
            TxtPhone2.Text = c.Phone2;
            TxtAddress.Text = c.Address;
            TxtRegion.Text = c.Region;
            TxtRep.Text = c.SalesRep;
            TxtTaxNumber.Text = c.TaxNumber;
            TxtNotes.Text = c.Notes;
            TxtCreditLimit.Text = c.CreditLimit.ToString("N2");

            CmbType.SelectedIndex = c.Type == CustomerType.Credit ? 1 : 0;
        }

        private void AutoGenerateCode()
        {
            TxtCode.Text = $"CUST-{DateTime.Now:yyMMddHH}{new Random().Next(10, 99)}";
        }

        // ══════════════════════════════════════════════════════════
        //  EVENTS
        // ══════════════════════════════════════════════════════════

        private void CmbType_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            bool isCredit = CmbType.SelectedIndex == 1;
            CreditLimitBorder.Opacity = isCredit ? 1.0 : 0.4;
            TxtCreditLimit.IsEnabled = isCredit;
            if (!isCredit) TxtCreditLimit.Text = "0";
        }

        private void TxtName_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            // Clear validation highlight when user starts typing
            NameBorder.BorderBrush = (Brush)Application.Current.Resources["BorderBrush"];
            ValidationBanner.Visibility = Visibility.Collapsed;
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e) => Close();

        private void BtnCancel_Click(object sender, RoutedEventArgs e) => Close();

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (!Validate()) return;

            Result = BuildModel();
            DialogResult = true;
            Close();
        }

        // ══════════════════════════════════════════════════════════
        //  VALIDATION
        // ══════════════════════════════════════════════════════════

        private bool Validate()
        {
            if (string.IsNullOrWhiteSpace(TxtName.Text))
            {
                ShowValidation(_isArabic ? "اسم العميل مطلوب" : "Customer name is required");
                NameBorder.BorderBrush = (Brush)Application.Current.Resources["DangerBrush"];
                TxtName.Focus();
                return false;
            }

            if (CmbType.SelectedIndex == 1)
            {
                if (!decimal.TryParse(TxtCreditLimit.Text.Replace(",", ""), out decimal limit) || limit < 0)
                {
                    ShowValidation(_isArabic ? "الحد الائتماني يجب أن يكون رقماً صحيحاً" : "Credit limit must be a valid number");
                    TxtCreditLimit.Focus();
                    return false;
                }
            }

            return true;
        }

        private void ShowValidation(string msg)
        {
            TxtValidationMsg.Text = msg;
            ValidationBanner.Visibility = Visibility.Visible;
        }

        // ══════════════════════════════════════════════════════════
        //  BUILD MODEL FROM FORM
        // ══════════════════════════════════════════════════════════

        private CustomerModel BuildModel()
        {
            decimal.TryParse(TxtCreditLimit.Text.Replace(",", ""), out decimal creditLimit);

            return new CustomerModel
            {
                Code = TxtCode.Text.Trim(),
                NameAr = TxtName.Text.Trim(),
                NameEn = TxtName.Text.Trim(),
                Phone = TxtPhone.Text.Trim(),
                Phone2 = TxtPhone2.Text.Trim(),
                Address = TxtAddress.Text.Trim(),
                Region = TxtRegion.Text.Trim(),
                SalesRep = TxtRep.Text.Trim(),
                TaxNumber = TxtTaxNumber.Text.Trim(),
                Notes = TxtNotes.Text.Trim(),
                Type = CmbType.SelectedIndex == 1 ? CustomerType.Credit : CustomerType.Cash,
                CreditLimit = CmbType.SelectedIndex == 1 ? creditLimit : 0,
                Status = CustomerStatus.Active,
                Balance = 0,
                LastInvoiceDate = null,
                TotalInvoices = 0
            };
        }
    }
}
