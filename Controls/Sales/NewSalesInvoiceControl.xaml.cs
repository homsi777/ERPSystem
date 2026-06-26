using ERPSystem.Core;
using ERPSystem.Services;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;

namespace ERPSystem.Controls.Sales
{
    public class SalesInvoiceLineRow
    {
        public string GoodsType { get; set; } = "";
        public string BoltCode { get; set; } = "";
        public string Color { get; set; } = "";
        public int RollCount { get; set; }
        public string LengthStatus { get; set; } = "بانتظار التفصيل";
        public string Unit { get; set; } = "يارد";
        public decimal UnitPrice { get; set; }
    }

    public partial class NewSalesInvoiceControl : UserControl
    {
        private readonly ObservableCollection<SalesInvoiceLineRow> _lines = new();
        private string _status = "مسودة";

        public NewSalesInvoiceControl()
        {
            InitializeComponent();
            Loaded += (_, _) =>
            {
                LoadSampleLines();
                UpdateStatusBadge();
            };
        }

        private void LoadSampleLines()
        {
            _lines.Clear();
            _lines.Add(new SalesInvoiceLineRow
            {
                GoodsType = "كتان F12", BoltCode = "P32", Color = "أبيض",
                RollCount = 10, Unit = "مارد", UnitPrice = 12.5m
            });
            _lines.Add(new SalesInvoiceLineRow
            {
                GoodsType = "كتان F12", BoltCode = "P32", Color = "أبيض",
                RollCount = 4, Unit = "مارد", UnitPrice = 12.5m
            });
            _lines.Add(new SalesInvoiceLineRow
            {
                GoodsType = "كتان F12", BoltCode = "P32", Color = "أبيض",
                RollCount = 6, Unit = "مارد", UnitPrice = 11m
            });
            ItemsGrid.ItemsSource = _lines;
            RefreshSummary();
        }

        private void BtnCash_Checked(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded) return;
            TxtPartialPayment.Text = "0";
        }

        private void BtnSaveDraft_Click(object sender, RoutedEventArgs e)
        {
            if (_lines.Count == 0)
            {
                MockInteractionService.ShowWarning("أضف صنفاً واحداً على الأقل قبل الحفظ.");
                return;
            }

            if (!MockInteractionService.Confirm(
                    "حفظ المسودة وإرسالها للمستودع لتنفيذ الأطوال؟",
                    "حفظ مسودة وإرسال للتنفيذ"))
                return;

            _status = "بانتظار التفصيل";
            UpdateStatusBadge();
            MockInteractionService.ShowSuccess(
                $"تم حفظ الفاتورة {TxtInvoiceNumber.Text}.\n\nالحالة: {_status}",
                "تم الإرسال للمستودع");

            if (MockInteractionService.Confirm("فتح شاشة تفصيل الأطوال الآن؟", "تفصيل المستودع"))
                MockInteractionService.OpenDetailingWorkspace(TxtInvoiceNumber.Text);
        }

        private void BtnApprove_Click(object sender, RoutedEventArgs e)
        {
            if (_status != "بانتظار التفصيل" && _status != "مفصلة")
            {
                MockInteractionService.ShowWarning("لا يمكن اعتماد الفاتورة قبل إرسالها للمستودع وإدخال الأطوال.");
                return;
            }

            if (!MockInteractionService.Confirm("اعتماد الفاتورة نهائياً؟", "اعتماد الفاتورة"))
                return;

            _status = "معتمدة";
            UpdateStatusBadge();
            MockInteractionService.ShowSuccess("تم اعتماد الفاتورة بنجاح.", "اعتماد الفاتورة");
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            if (!MockInteractionService.Confirm("إلغاء الفاتورة والعودة؟", "إلغاء"))
                return;
            MockInteractionService.Navigate(AppModule.Sales, "Invoices");
        }

        private void BtnPrint_Click(object sender, RoutedEventArgs e) =>
            MockInteractionService.ShowDocumentPreview($"فاتورة {TxtInvoiceNumber.Text}", "طباعة");

        private void BtnPdf_Click(object sender, RoutedEventArgs e) =>
            MockInteractionService.ShowDocumentPreview($"فاتورة {TxtInvoiceNumber.Text}", "PDF");

        private void BtnPreview_Click(object sender, RoutedEventArgs e) =>
            MockInteractionService.ShowDocumentPreview($"فاتورة {TxtInvoiceNumber.Text}", "معاينة");

        private void UpdateStatusBadge()
        {
            TxtStatusBadge.Text = _status;
            TxtStatusBadge.Foreground = _status switch
            {
                "بانتظار التفصيل" => (System.Windows.Media.Brush)Application.Current.Resources["WarningBrush"]!,
                "معتمدة" => (System.Windows.Media.Brush)Application.Current.Resources["SuccessBrush"]!,
                _ => (System.Windows.Media.Brush)Application.Current.Resources["PrimaryBrush"]!
            };
        }

        private void BtnAddLine_Click(object sender, RoutedEventArgs e)
        {
            _lines.Add(new SalesInvoiceLineRow
            {
                GoodsType = "كتان F12", BoltCode = "P32", Color = "أبيض",
                RollCount = 1, Unit = "مارد", UnitPrice = 12.5m
            });
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
            (System.Windows.Media.Brush)Application.Current.Resources[key]!;
    }
}
