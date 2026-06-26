using System.Windows;
using System.Windows.Controls;

namespace ERPSystem.Controls
{
    public partial class EnterpriseTableControl : UserControl
    {
        public event RoutedEventHandler? AddNewClicked;

        public DataGrid Grid => MainGrid;

        public EnterpriseTableControl()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            Core.LocalizationManager.Instance.LanguageChanged += (_, _) => UpdateLabels();
            UpdateLabels();
        }

        private void UpdateLabels()
        {
            var loc = Core.LocalizationManager.Instance;
            TxtExportBtn.Text = loc["Export"];
            TxtAddNew.Text = loc["New"];
        }

        private void BtnAddNew_Click(object sender, RoutedEventArgs e)
        {
            AddNewClicked?.Invoke(this, e);
        }

        public void SetRecordCount(int shown, int total)
        {
            var loc = Core.LocalizationManager.Instance;
            if (loc.IsArabic)
                TxtRecordCount.Text = $"عرض {shown} من {total} سجل";
            else
                TxtRecordCount.Text = $"Showing {shown} of {total} records";
        }
    }
}
