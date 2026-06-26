using ERPSystem.Controls;
using ERPSystem.Core;
using ERPSystem.Core.Actions;
using ERPSystem.Core.Domain;
using ERPSystem.Core.Inventory;
using ERPSystem.Helpers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace ERPSystem.Views.Inventory
{
    public static class InventoryViews
    {
        public static UserControl Create(string key) => key switch
        {
            "Categories" => BuildCategories(),
            "ImportExcel" => BuildImportExcel(),
            "OpeningStock" => BuildOpeningStock(),
            "Stocktake" => BuildStocktake(),
            "Transfers" => BuildTransfers(),
            "Settings" => BuildSettings(),
            "Warehouses" => BuildWarehousesHub(),
            _ => BuildWarehousesHub()
        };

        private static UserControl BuildWarehousesHub()
        {
            var tabs = new TabControl { FontFamily = new FontFamily("Segoe UI, Tahoma, Arial") };
            tabs.Items.Add(new TabItem { Header = "مراكز المستودعات", Content = BuildWarehouseEntities() });
            tabs.Items.Add(new TabItem { Header = "أرصدة الأقمشة", Content = BuildFabricStock() });
            return Wrap(new ScrollViewer { Content = tabs, Padding = new Thickness(16), VerticalScrollBarVisibility = ScrollBarVisibility.Auto });
        }

        private static UserControl BuildWarehouseEntities()
        {
            var data = new[]
            {
                new WarehouseEntity { Code = "WH-01", Name = "المستودع الرئيسي", City = "جدة", RollCount = 4850, TotalLength = 385420, CapacityPercent = 72, Status = "نشط" },
                new WarehouseEntity { Code = "WH-02", Name = "مستودع جدة", City = "جدة", RollCount = 2100, TotalLength = 168900, CapacityPercent = 58, Status = "نشط" },
                new WarehouseEntity { Code = "WH-03", Name = "مستودع الرياض", City = "الرياض", RollCount = 980, TotalLength = 72400, CapacityPercent = 41, Status = "نشط" },
            };
            var page = ListPage("المستودعات", EntityType.Warehouse, AppModule.Inventory);
            page.SetPrimaryButton("إضافة مستودع");
            page.SetEmptyState("لا توجد مستودعات", "إضافة مستودع", "\uE8B7");
            page.PrimaryActionRequested += (_, _) => Services.MockInteractionService.OpenMockForm("إضافة مستودع");
            var g = page.Grid;
            g.AutoGenerateColumns = false;
            foreach (var (h, p, w) in new (string, string, object)[]
            {
                ("الكود", "Code", 90), ("الاسم", "Name", "*"), ("المدينة", "City", 100),
                ("الأثواب", "RollCount", 90), ("الأطوال", "TotalLength", 110), ("السعة %", "CapacityPercent", 80), ("الحالة", "Status", 80)
            }) AddCol(g, h, p, w, p is "TotalLength" ? "N0" : p is "CapacityPercent" ? "N0" : null);
            page.BindData(data.Cast<object>().ToList());
            return page;
        }

        private static UserControl BuildFabricStock()
        {
            var data = FabricInventorySampleData.Generate(40).Select(f => new WarehouseStockRow
            {
                GoodsType = f.Type,
                BoltCode = f.Code,
                Color = f.Color,
                RollCount = f.RollCount,
                TotalLength = f.TotalMeters,
                Unit = "متر",
                Lot = "LOT-01",
                Location = "A-12",
                Warehouse = f.Warehouse,
                Status = f.StatusDisplay
            }).ToList();

            var page = ListPage("أرصدة الأقمشة", EntityType.FabricItem, AppModule.Inventory);
            page.SetPrimaryButton("إضافة صنف");
            page.SetEmptyState("لا توجد أرصدة أقمشة في المستودعات", null, "\uE821");
            page.PrimaryActionRequested += (_, _) => Services.MockInteractionService.OpenMockForm("إضافة صنف");

            var warehouseFilter = ErpUiFactory.FilterCombo(
                ["كل المستودعات", "المستودع الرئيسي", "مستودع جدة", "مستودع الرياض"], 160);
            warehouseFilter.SelectionChanged += (_, _) =>
            {
                var sel = warehouseFilter.SelectedItem?.ToString() ?? "كل المستودعات";
                if (sel == "كل المستودعات")
                {
                    page.SetExtraFilter(null);
                    page.SetFilterSummary("");
                }
                else
                {
                    page.SetExtraFilter(o => o is WarehouseStockRow r && r.Warehouse == sel);
                    page.SetFilterSummary($"المستودع: {sel}");
                }
            };
            page.SetFilterExtras(warehouseFilter);

            var g = page.Grid;
            g.AutoGenerateColumns = false;
            foreach (var (h, p, w) in new (string, string, object)[] {
                ("نوع البضاعة","GoodsType",100),("كود التوب","BoltCode",100),("اللون","Color",80),
                ("عدد الأثواب","RollCount",90),("إجمالي الأطوال","TotalLength",110),("الوحدة","Unit",70),
                ("اللوط","Lot",80),("الموقع","Location",80),("المستودع","Warehouse",120),("الحالة","Status",80)
            }) AddCol(g, h, p, w);
            page.BindData(data.Cast<object>().ToList());
            return page;
        }

        private static UserControl BuildCategories() => FormPage("التصنيفات", "هرمية: نوع البضاعة → كود التوب → اللون",
            ["إضافة نوع بضاعة", "إضافة كود توب", "إضافة لون", "تعديل", "تعطيل / استعادة"],
            ErpUiFactory.BuildGrid(new[] {
                new { نوع_البضاعة = "قطن", كود_التوب = "COL-01", اللون = "أبيض", كود_اللون = "W01", الحالة = "نشط" },
                new { نوع_البضاعة = "بوليستر", كود_التوب = "TRK-02", اللون = "بيج", كود_اللون = "B02", الحالة = "نشط" },
            }, false));

        private static UserControl BuildImportExcel() => FormPage("استيراد Excel للمخزون", "استيراد أرصدة الأقمشة من ملف",
            ["رفع ملف", "معاينة", "تأكيد"],
            ErpUiFactory.BuildGrid(new[] {
                new { نوع_بضاعة = "قطن", كود_التوب = "COL-01", اللون = "أبيض", كود_اللون = "W01", عدد_الأثواب = 12, الأطوال = 720, الوحدة = "متر", اللوط = "L1", الموقع = "A-1" },
            }));

        private static UserControl BuildOpeningStock() => FormPage("مواد أول مدة", "إدخال رصيد افتتاحي للمستودع",
            ["حفظ", "اعتماد"],
            ErpUiFactory.BuildFormGrid(
                ("رقم الإدخال", ErpUiFactory.FormField("OPN-2026-001")),
                ("السنة المالية", ErpUiFactory.FormField("2026")),
                ("التاريخ", ErpUiFactory.FormDate()),
                ("المستودع", ErpUiFactory.FilterCombo(["المستودع الرئيسي", "جدة"])),
                ("المسؤول", ErpUiFactory.FormField("خالد الشمري")),
                ("مصدر الإدخال", ErpUiFactory.FormField("يدوي")),
                ("ملاحظات", ErpUiFactory.FormField(""))));

        private static UserControl BuildStocktake() => FormPage("الجرد", "جلسات الجرد وجرد الحاويات",
            ["جلسة جديدة", "جرد حاوية"],
            ErpUiFactory.BuildGrid(new[] {
                new StocktakeSession { SessionNumber = "STK-001", Date = DateTime.Today, Warehouse = "الرئيسي", Responsible = "خالد", Progress = "75%", VarianceCount = 3, Status = "جاري" },
                new StocktakeSession { SessionNumber = "STK-002", Date = DateTime.Today.AddDays(-2), Warehouse = "جدة", Responsible = "فهد", Progress = "100%", VarianceCount = 0, Status = "مغلق" },
            }));

        private static UserControl BuildTransfers() => FormPage("المناقلات", "نقل الأقمشة بين المستودعات",
            ["مناقلة جديدة"],
            ErpUiFactory.BuildGrid(new[] {
                new StockTransfer { Number = "TRF-008", FromWarehouse = "الرئيسي", ToWarehouse = "جدة", ItemCount = 5, Status = "مرحّل", Date = DateTime.Today },
            }));

        private static UserControl BuildSettings() => FormPage("إعدادات المخزون", "وحدات القياس، سياسات الجرد، التنبيهات", ["حفظ"], new TextBlock { Text = "إعدادات تجريبية — جاهزة لربط PostgreSQL", Margin = new Thickness(8) });

        private static ErpListModuleControl ListPage(string title, EntityType et, AppModule mod)
        {
            var p = new ErpListModuleControl();
            p.Configure(et, mod);
            p.SetHeader(title, "", "\uE821", B("AccentInventoryBrush"));
            return p;
        }

        private static UserControl FormPage(string title, string sub, string[] actions, UIElement body)
        {
            var root = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto, Padding = new Thickness(16) };
            var stack = new StackPanel();
            stack.Children.Add(ErpUiFactory.SectionTitle(title));
            stack.Children.Add(new TextBlock { Text = sub, Foreground = Br("TextSecondaryBrush"), Margin = new Thickness(0, 0, 0, 12) });
            var row = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 12) };
            foreach (var a in actions)
                row.Children.Add(new Button { Content = a, Style = S("SecondaryButtonStyle"), Margin = new Thickness(0, 0, 8, 0) });
            stack.Children.Add(row);
            stack.Children.Add(ErpUiFactory.Card(body));
            root.Content = stack;
            return Wrap(root);
        }

        private static void AddCol(DataGrid g, string h, string p, object w, string? fmt = null)
            => ErpUiFactory.AddGridColumn(g, h, p, w, fmt);

        private static SolidColorBrush B(string k) => (SolidColorBrush)Application.Current.Resources[k]!;
        private static Brush Br(string k) => (Brush)Application.Current.Resources[k]!;
        private static Style S(string k) => (Style)Application.Current.Resources[k]!;
        private static UserControl Wrap(UIElement c) => new() { Content = c };
    }
}
