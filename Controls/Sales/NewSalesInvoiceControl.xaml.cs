using ERPSystem.Application.Queries.Containers;
using ERPSystem.Application.UseCases.Queries;
using ERPSystem.Core;
using ERPSystem.Core.ChinaImport;
using ERPSystem.Infrastructure.Seed;
using ERPSystem.Services;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;

namespace ERPSystem.Controls.Sales
{
    public sealed class ContainerPickItem
    {
        public Guid Id { get; init; }
        public string Display { get; init; } = "";
    }

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
            Loaded += OnLoaded;
        }

        public Guid? SelectedContainerId =>
            CmbContainer.SelectedItem is ContainerPickItem item ? item.Id : null;

        private async void OnLoaded(object sender, RoutedEventArgs e)
        {
            Loaded -= OnLoaded;
            LoadSampleLines();
            UpdateStatusBadge();
            await LoadContainersAsync();
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
            if (CmbContainer.SelectedItem is not ContainerPickItem)
            {
                MockInteractionService.ShowWarning("اختر الحاوية المرتبطة بالفاتورة.", "فاتورة بيع");
                return;
            }

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
            var containerLabel = (CmbContainer.SelectedItem as ContainerPickItem)?.Display ?? "—";
            MockInteractionService.ShowSuccess(
                $"تم حفظ الفاتورة {TxtInvoiceNumber.Text}.\n\nالحاوية: {containerLabel}\nالحالة: {_status}",
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
                "بانتظار التفصيل" => (System.Windows.Media.Brush)System.Windows.Application.Current.Resources["WarningBrush"]!,
                "معتمدة" => (System.Windows.Media.Brush)System.Windows.Application.Current.Resources["SuccessBrush"]!,
                _ => (System.Windows.Media.Brush)System.Windows.Application.Current.Resources["PrimaryBrush"]!
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
            (System.Windows.Media.Brush)System.Windows.Application.Current.Resources[key]!;
    }
}
