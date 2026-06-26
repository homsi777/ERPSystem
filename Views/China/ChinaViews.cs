using ERPSystem.Controls;
using ERPSystem.Core;
using ERPSystem.Core.Actions;
using ERPSystem.Core.ChinaImport;
using ERPSystem.Core.Domain;
using ERPSystem.Core.Workspace;
using ERPSystem.Helpers;
using System.Collections;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Media;

namespace ERPSystem.Views.China
{
    public static class ChinaViews
    {
        private static List<ImportContainerModel> _containers = ChinaImportSampleData.Generate(30);

        public static UserControl Create(string key) => key switch
        {
            "NewImport" => BuildImportForm(),
            "ExcelReview" => BuildExcelReview(),
            "Distribution" => BuildDistribution(),
            "Stocktake" => BuildStocktake(),
            "LandingCost" => BuildLandingCost(),
            _ => BuildContainerList()
        };

        private static UserControl BuildContainerList()
        {
            var page = new ErpListModuleControl();
            page.Configure(EntityType.ImportContainer, AppModule.ChinaImport);
            page.SetHeader("طلبات الصين", "قائمة حاويات استيراد الأقمشة من الصين", "\uE7BF",
                (SolidColorBrush)Application.Current.Resources["AccentOrdersBrush"]!);
            page.SetPrimaryButton("استيراد حاوية");
            page.SetEmptyState("لا توجد حاويات مستوردة", "استيراد حاوية", "\uE7BF");
            page.PrimaryActionRequested += (_, _) => Services.MockInteractionService.Navigate(AppModule.ChinaImport, "NewImport");

            var statusFilter = ErpUiFactory.FilterCombo(
                ["الكل", "بالطريق", "واصلة", "قيد المراجعة", "معتمدة", "مغلقة", "مؤرشفة"], 130);
            statusFilter.SelectionChanged += (_, _) => ApplyContainerStatusFilter(page, statusFilter);
            page.SetFilterExtras(statusFilter);

            page.SetSearchMatcher((o, term) => o is ImportContainerModel m &&
                (m.ContainerNumber.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                 m.SupplierName.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                 m.OrderNumber.Contains(term, StringComparison.OrdinalIgnoreCase)));

            SetupContainerGrid(page.Grid);
            page.BindData(_containers.Cast<object>().ToList());
            return page;
        }

        private static void ApplyContainerStatusFilter(ErpListModuleControl page, ComboBox combo)
        {
            var sel = combo.SelectedItem?.ToString() ?? "الكل";
            if (sel == "الكل")
            {
                page.SetExtraFilter(null);
                page.SetFilterSummary("");
            }
            else
            {
                page.SetExtraFilter(o => o is ImportContainerModel c && c.StatusDisplay == sel);
                page.SetFilterSummary($"الحالة: {sel}");
            }
        }

        private static void SetupContainerGrid(DataGrid g)
        {
            g.Columns.Clear();
            g.AutoGenerateColumns = false;
            AddCol(g, "رقم الحاوية", nameof(ImportContainerModel.ContainerNumber), 120);
            AddCol(g, "المورد", nameof(ImportContainerModel.SupplierName), "*");
            AddCol(g, "رقم الطلب", nameof(ImportContainerModel.OrderNumber), 110);
            AddCol(g, "تاريخ الشحن", nameof(ImportContainerModel.ShipmentDate), 100, "yyyy/MM/dd");
            AddCol(g, "الوصول المتوقع", nameof(ImportContainerModel.ExpectedArrival), 110, "yyyy/MM/dd");
            AddCol(g, "الحالة", nameof(ImportContainerModel.StatusDisplay), 80);
            AddCol(g, "الأكواد", nameof(ImportContainerModel.CodeCount), 65);
            AddCol(g, "الألوان", nameof(ImportContainerModel.ColorCount), 65);
            AddCol(g, "الأثواب", nameof(ImportContainerModel.TotalRolls), 70);
            AddCol(g, "الأطوال", nameof(ImportContainerModel.TotalMeters), 90, "N0");
            AddCol(g, "الوزن", nameof(ImportContainerModel.TotalWeightKg), 80, "N0");
            AddCol(g, "الهالك %", nameof(ImportContainerModel.WastePercent), 70);
            AddCol(g, "العملاء", nameof(ImportContainerModel.LinkedCustomers), 120);
            AddCol(g, "آخر تحديث", nameof(ImportContainerModel.LastUpdated), 100, "yyyy/MM/dd");
        }

        private static UserControl BuildImportForm()
        {
            var root = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto, Padding = new Thickness(16) };
            var stack = new StackPanel();
            stack.Children.Add(ErpUiFactory.SectionTitle("استيراد حاوية جديدة"));
            stack.Children.Add(ErpUxFactory.WorkflowStepper(
                ("وصول الحاوية", true, true),
                ("استيراد Excel", true, false),
                ("مراجعة البيانات", false, false),
                ("Landing Cost", false, false),
                ("اعتماد", false, false),
                ("تحويل للمخزن", false, false),
                ("جاهز للبيع", false, false)));

            var form = ErpUiFactory.BuildFormGrid(
                ("رقم الحاوية", ErpUiFactory.FormField("CN-2026-NEW")),
                ("المورد", ErpUiFactory.FilterCombo(["مورد قوانغتشو", "مورد شنتشن", "مورد هانغتشو"])),
                ("رقم الطلب", ErpUiFactory.FormField("PO-CN-2026100")),
                ("تاريخ الشحن", ErpUiFactory.FormDate()),
                ("الوصول المتوقع", ErpUiFactory.FormDate(DateTime.Today.AddDays(20))),
                ("نسبة الهالك %", ErpUiFactory.FormField("3")),
                ("ملاحظات", ErpUiFactory.FormField("شحنة أقمشة جملة"))
            );
            stack.Children.Add(ErpUiFactory.Card(form));

            stack.Children.Add(ErpUiFactory.SectionTitle("نوع ملف الاستيراد"));
            var typeRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 12) };
            foreach (var t in new[] { "ملف نوع قماش واحد", "ملف عدة أنواع أقمشة", "إدخال يدوي" })
            {
                typeRow.Children.Add(new RadioButton { Content = t, Margin = new Thickness(0, 0, 16, 0), IsChecked = t.StartsWith("ملف عدة") });
            }
            stack.Children.Add(ErpUiFactory.Card(typeRow));

            var uploadRow = new StackPanel { Orientation = Orientation.Horizontal };
            uploadRow.Children.Add(new Button { Content = "رفع ملف Excel", Style = S("PrimaryButtonStyle"), Margin = new Thickness(0, 0, 8, 0) });
            uploadRow.Children.Add(new Button { Content = "معاينة البيانات", Style = S("SecondaryButtonStyle"), Margin = new Thickness(0, 0, 8, 0) });
            uploadRow.Children.Add(new Button { Content = "تأكيد الحفظ", Style = S("SecondaryButtonStyle") });
            stack.Children.Add(ErpUiFactory.Card(uploadRow));

            stack.Children.Add(ErpUiFactory.SectionTitle("معاينة Excel"));
            stack.Children.Add(ErpUiFactory.Card(BuildExcelPreviewGrid()));

            stack.Children.Add(ErpUiFactory.SectionTitle("ملخص الاستيراد"));
            stack.Children.Add(ErpUiFactory.Card(BuildImportSummaryPanel()));

            root.Content = stack;
            return Wrap(root);
        }

        private static UserControl BuildExcelReview() => BuildImportForm();

        private static DataGrid BuildExcelPreviewGrid()
        {
            var data = ChinaImportSampleData.GetContainerLines("CN-2026-001");
            var g = ErpUiFactory.BuildGrid(data, false);
            AddCol(g, "رقم التوب", nameof(ContainerFabricLine.BoltNumber), 80);
            AddCol(g, "كود القماش", nameof(ContainerFabricLine.FabricCode), 100);
            AddCol(g, "نوع القماش", nameof(ContainerFabricLine.FabricType), "*");
            AddCol(g, "اللون", nameof(ContainerFabricLine.Color), 80);
            AddCol(g, "الطول بالمتر", nameof(ContainerFabricLine.LengthMeters), 100, "N2");
            AddCol(g, "الوزن بالكغ", nameof(ContainerFabricLine.WeightKg), 100, "N2");
            AddCol(g, "ملاحظة", nameof(ContainerFabricLine.Note), 100);
            AddCol(g, "حالة الصف", nameof(ContainerFabricLine.RowStatus), 90);
            return g;
        }

        private static StackPanel BuildImportSummaryPanel()
        {
            var s = ChinaImportSampleData.GetImportSummary("CN-2026-001");
            var p = new StackPanel();
            var grid = new UniformGrid { Columns = 4 };
            foreach (var (l, v) in new[]
            {
                ("عدد الأثواب", s.rolls.ToString()), ("إجمالي الأطوال", $"{s.meters:N0} م"),
                ("الوزن الخام", $"{s.weight:N0} كغ"), ("عدد الأكواد", s.codes.ToString()),
                ("عدد الألوان", s.colors.ToString()), ("صفوف صحيحة", s.valid.ToString()),
                ("صفوف بأخطاء", s.errors.ToString()), ("الوزن الصافي بعد الهالك", $"{s.weight * 0.97m:N0} كغ")
            })
            {
                var cell = new StackPanel { Margin = new Thickness(0, 0, 8, 8) };
                cell.Children.Add(new TextBlock { Text = l, FontSize = 11, Foreground = Br("TextMutedBrush") });
                cell.Children.Add(new TextBlock { Text = v, FontWeight = FontWeights.SemiBold, FontSize = 14 });
                grid.Children.Add(cell);
            }
            p.Children.Add(grid);
            return p;
        }

        private static UserControl BuildLandingCost()
        {
            var cost = new ContainerLandingCost
            {
                TotalLengthFromInvoice = 38500,
                ContainerWeightKg = 18500,
                CustomsAmountPaid = 42000,
                Shipping = 15000,
                Clearance = 8500,
                OtherExpenses = 3200
            };

            var root = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto, Padding = new Thickness(16) };
            var stack = new StackPanel();
            stack.Children.Add(ErpUxFactory.WorkflowStepper(
                ("وصول", true, true), ("Excel", true, true), ("مراجعة", true, true),
                ("Landing Cost", true, false), ("اعتماد", false, false), ("مخزن", false, false), ("بيع", false, false)));
            stack.Children.Add(ErpUiFactory.SectionTitle("ملخص تكلفة الاستيراد — مراجعة مالية"));
            stack.Children.Add(ErpUxFactory.InfoBanner("راجع تكلفة المتر قبل اعتماد الحاوية وتحويلها للمخزن. البيع بالمتر — الوزن للتحقق فقط.", "info"));

            var form = ErpUiFactory.BuildFormGrid(
                ("إجمالي الطول من فاتورة الصين", ErpUiFactory.FormField($"{cost.TotalLengthFromInvoice:N0} م")),
                ("وزن الحاوية بالكيلو", ErpUiFactory.FormField($"{cost.ContainerWeightKg:N0}")),
                ("وزن الحاوية بالغرام", ErpUiFactory.FormField($"{cost.ContainerWeightGrams:N0}")),
                ("مبلغ الجمارك المدفوع", ErpUiFactory.FormField($"{cost.CustomsAmountPaid:N0} ر.س")),
                ("تكلفة الجمارك لكل متر", ErpUiFactory.FormField($"{cost.CustomsCostPerMeter:N4} ر.س")),
                ("متوسط وزن المتر بالغرام", ErpUiFactory.FormField($"{cost.AvgGramPerMeter:N2} غرام")),
                ("الشحن", ErpUiFactory.FormField($"{cost.Shipping:N0} ر.س")),
                ("التخليص", ErpUiFactory.FormField($"{cost.Clearance:N0} ر.س")),
                ("مصاريف أخرى", ErpUiFactory.FormField($"{cost.OtherExpenses:N0} ر.س")),
                ("إجمالي مصاريف الاستيراد", ErpUiFactory.FormField($"{cost.TotalImportExpenses:N0} ر.س")),
                ("تكلفة المصاريف لكل متر", ErpUiFactory.FormField($"{cost.ExpenseCostPerMeter:N4} ر.س"))
            );
            stack.Children.Add(ErpUiFactory.Card(form));
            stack.Children.Add(ErpUxFactory.KpiStrip(
                ("تكلفة الجمارك/م", $"{cost.CustomsCostPerMeter:N4} ر.س"),
                ("تكلفة المصاريف/م", $"{cost.ExpenseCostPerMeter:N4} ر.س"),
                ("متوسط غرام/م", $"{cost.AvgGramPerMeter:N2}"),
                ("إجمالي المصاريف", $"{cost.TotalImportExpenses:N0} ر.س")));
            stack.Children.Add(ErpUxFactory.ActionToolbar("Landing Cost", ("اعتماد التكاليف", true), ("طباعة المراجعة", false), ("التالي: اعتماد الحاوية", false)));

            root.Content = stack;
            return Wrap(root);
        }

        private static UserControl BuildDistribution()
        {
            var data = new[]
            {
                new ContainerCustomerDistribution { CustomerName = "أحمد الحمصي", FabricCode = "FAB-101", Color = "أبيض", Rolls = 12, Meters = 720 },
                new ContainerCustomerDistribution { CustomerName = "مؤسسة النسيج", FabricCode = "FAB-102", Color = "بيج", Rolls = 8, Meters = 480 },
            };
            return SimpleTablePage("توزيع الكميات على العملاء", "توزيع أثواب الحاوية على المشترين والحجوزات", data);
        }

        private static UserControl BuildStocktake()
        {
            var data = new[]
            {
                new { البند = "الوارد", القيمة = "450" }, new { البند = "المتوقع", القيمة = "448" },
                new { البند = "المعدود", القيمة = "446" }, new { البند = "الفرق", القيمة = "-2" },
                new { البند = "مبيعات", القيمة = "12" }, new { البند = "حجوزات", القيمة = "35" },
                new { البند = "إرجاع", القيمة = "1" }, new { البند = "هالك", القيمة = "3" },
                new { البند = "مناقلات", القيمة = "5" },
            };
            return SimpleTablePage("جرد الحاوية", "مقارنة النظام مع العد الفعلي داخل الحاوية", data);
        }

        private static UserControl SimpleTablePage(string title, string subtitle, IEnumerable data)
        {
            var root = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto, Padding = new Thickness(16) };
            var stack = new StackPanel();
            stack.Children.Add(ErpUiFactory.SectionTitle(title));
            stack.Children.Add(new TextBlock { Text = subtitle, Foreground = Br("TextSecondaryBrush"), Margin = new Thickness(0, 0, 0, 16) });
            stack.Children.Add(ErpUiFactory.Card(ErpUiFactory.BuildGrid(data)));
            root.Content = stack;
            return Wrap(root);
        }

        private static void AddCol(DataGrid g, string h, string path, object w, string? fmt = null)
            => ErpUiFactory.AddGridColumn(g, h, path, w, fmt);

        private static SolidColorBrush B(string key) => (SolidColorBrush)Application.Current.Resources[key]!;
        private static Brush Br(string key) => (Brush)Application.Current.Resources[key]!;
        private static Style S(string key) => (Style)Application.Current.Resources[key]!;

        private static UserControl Wrap(UIElement content)
        {
            var uc = new UserControl { Content = content, Background = Br("AppBgBrush") as SolidColorBrush };
            return uc;
        }
    }
}
