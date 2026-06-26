using System.Windows;

namespace ERPSystem.Dialogs
{
    public partial class DocumentPreviewWindow : Window
    {
        public DocumentPreviewWindow()
        {
            InitializeComponent();
        }

        public string DocumentTitle
        {
            set
            {
                TxtTitle.Text = "معاينة المستند";
                TxtDocName.Text = value;
            }
        }

        public string DocumentFormat
        {
            set => TxtFormat.Text = value;
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e) => Close();
    }
}
