using ERPSystem.Controls.China;
using ERPSystem.Core.Actions;
using ERPSystem.Core.Customers;
using ERPSystem.Core.Domain;
using ERPSystem.Core.HR;
using ERPSystem.Core.Inventory;
using ERPSystem.Core.Purchases;
using ERPSystem.Core.Sales;
using ERPSystem.Core.Suppliers;
using ERPSystem.Core.Workspace;
using ERPSystem.Helpers;
using ERPSystem.Views.OperationsCenters;
using ERPSystem.Views.Workspaces;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ERPSystem.Controls.Workspace
{
    public partial class ActionWorkspaceView : UserControl
    {
        private readonly WorkspaceOpenRequest _request;

        public ActionWorkspaceView(WorkspaceOpenRequest request)
        {
            _request = request;
            InitializeComponent();
            Loaded += (_, _) => BuildContent();
        }

        private void BuildContent()
        {
            var action = _request.ActionId;
            var def = EntityActionRegistry.GetActions(_request.EntityType)
                .FirstOrDefault(a => a.Id == action);

            ApplyEntityTheme(def?.IconGlyph);

            TxtActionTitle.Text = _request.Title;
            TxtActionSubtitle.Text = BuildSubtitle();

            ContentPanel.Children.Clear();

            var specialized = EntityWorkspaceContentFactory.TryBuild(_request);
            if (specialized != null)
            {
                if (UsesOperationsCenterChrome(_request)
                    || _request.ActionId is EntityActionId.ContainerApprove
                        or EntityActionId.ContainerCosts
                        or EntityActionId.ContainerDistribution
                        or EntityActionId.ContainerStocktake)
                    ApplyOperationsCenterLayout();
                ContentPanel.Children.Add(specialized);
                return;
            }

            ContentPanel.Children.Add(BuildInfoSection());
            ContentPanel.Children.Add(BuildGenericActionContent());
        }

        private void ApplyEntityTheme(string? iconOverride)
        {
            var (accent, accentLight, icon) = ErpUxFactory.EntityTheme(_request.EntityType);
            TxtActionIcon.Text = iconOverride ?? icon;
            TxtActionIcon.Foreground = accent;
            if (TxtActionIcon.Parent is Border badge)
            {
                badge.Background = accentLight;
            }
        }

        private string BuildSubtitle() => _request.EntityType switch
        {
            EntityType.Customer => "مركز عمليات العميل — جملة أقمشة",
            EntityType.SalesInvoice => "فاتورة بيع — سير عمل التفصيل بالمتر",
            EntityType.FabricItem => "بطاقة صنف — مخزون تشغيلي",
            EntityType.Supplier => "مركز عمليات المورد",
            EntityType.PurchaseInvoice => "فاتورة شراء أقمشة",
            EntityType.ImportContainer => "سير عمل استيراد الحاوية من الصين",
            EntityType.Employee => "ملف الموظف — HR",
            EntityType.JournalEntry => "سند مالي — محاسبة",
            EntityType.Warehouse => "مركز عمليات المستودع",
            EntityType.Cashbox => "مركز عمليات الصندوق",
            EntityType.Expense => "مركز عمل المصروف — إدارة مالية",
            _ => "ERP PRO"
        };

        private static bool UsesOperationsCenterChrome(WorkspaceOpenRequest request) =>
            OperationsCenterFactory.IsOperationsCenterRequest(request);

        private void ApplyOperationsCenterLayout()
        {
            HeaderCard.Visibility = Visibility.Collapsed;
            ContentCard.Padding = new Thickness(0);
            ContentCard.Background = Brushes.Transparent;
            ContentCard.BorderThickness = new Thickness(0);
            ContentCard.Effect = null;
            if (Parent is ScrollViewer sv)
                sv.Padding = new Thickness(8, 4, 8, 8);
            RootPanel.MaxWidth = double.PositiveInfinity;
        }

        private UIElement BuildInfoSection()
        {
            var grid = new Grid { Margin = new Thickness(0, 0, 0, 16) };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var fields = GetEntityFields();
            for (int i = 0; i < fields.Count; i++)
            {
                var (label, value) = fields[i];
                var panel = new StackPanel { Margin = new Thickness(i % 2 == 0 ? 0 : 12, 0, i % 2 == 0 ? 12 : 0, 10) };
                panel.Children.Add(MakeLabel(label));
                panel.Children.Add(MakeValue(value));
                Grid.SetColumn(panel, i % 2);
                Grid.SetRow(panel, i / 2);
                if (i / 2 >= grid.RowDefinitions.Count)
                    grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                grid.Children.Add(panel);
            }
            return ErpUiFactory.Card(grid, new Thickness(0, 0, 0, 12));
        }

        private List<(string Label, string Value)> GetEntityFields()
        {
            var row = _request.EntityRow;
            return _request.EntityType switch
            {
                EntityType.Customer when row is CustomerListRow c => new()
                {
                    ("كود العميل", c.Code), ("اسم العميل", c.NameAr),
                    ("الرصيد", $"{c.Balance:N2} $"),
                    ("الحد الائتماني", c.CreditLimitDisplay),
                    ("النوع", c.TypeDisplay),
                    ("الحالة", c.StatusDisplay),
                },
                EntityType.Customer when row is CustomerModel c => new()
                {
                    ("كود العميل", c.Code), ("اسم العميل", c.NameAr),
                    ("الهاتف", c.Phone), ("الرصيد", $"{c.Balance:N2} $"),
                    ("الحد الائتماني", $"{c.CreditLimit:N2} $"),
                    ("الحالة", c.Status == CustomerStatus.Active ? "نشط" : "موقوف"),
                },
                EntityType.SalesInvoice when row is SalesInvoice inv => new()
                {
                    ("رقم الفاتورة", inv.InvoiceNumber), ("العميل", inv.CustomerNameAr),
                    ("التاريخ", inv.Date.ToString("yyyy/MM/dd")),
                    ("الإجمالي", $"{inv.GrandTotal:N2} $"),
                    ("المتبقي", $"{inv.RemainingAmount:N2} $"),
                    ("الحالة", inv.StatusDisplayAr),
                },
                EntityType.FabricItem when row is FabricItemModel f => new()
                {
                    ("كود الصنف", f.Code), ("اسم القماش", f.FabricName),
                    ("اللون", f.Color), ("الأتواب", f.RollCount.ToString()),
                    ("الأمتار", $"{f.TotalMeters:N0} م"), ("المستودع", f.Warehouse),
                },
                EntityType.FabricItem when row is WarehouseStockRow w => new()
                {
                    ("كود التوب", w.BoltCode), ("نوع البضاعة", w.GoodsType),
                    ("اللون", w.Color), ("الأثواب", w.RollCount.ToString()),
                    ("الأطوال", $"{w.TotalLength:N0} م"), ("الموقع", w.Location),
                    ("المستودع", w.Warehouse), ("الحالة", w.Status),
                },
                EntityType.Supplier when row is SupplierModel s => new()
                {
                    ("كود المورد", s.Code), ("اسم المورد", s.Name),
                    ("البلد", s.Country), ("الرصيد", $"{s.Balance:N2} $"),
                },
                EntityType.PurchaseInvoice when row is PurchaseInvoiceModel p => new()
                {
                    ("رقم الفاتورة", p.InvoiceNumber), ("المورد", p.SupplierName),
                    ("الإجمالي", $"{p.TotalAmount:N2} $"), ("الحالة", p.StatusDisplay),
                },
                EntityType.ImportContainer when row is ContainerListRow c => new()
                {
                    ("رقم الحاوية", c.ContainerNumber), ("المورد", c.SupplierName),
                    ("الحالة", c.StatusDisplay), ("الأثواب", c.TotalRolls.ToString()),
                    ("الأمتار", $"{c.TotalMeters:N0} م"),
                },
                EntityType.Employee when row is EmployeeModel e => new()
                {
                    ("الموظف", e.FullName), ("القسم", e.Department),
                    ("المسمى", e.JobTitle), ("الحالة", e.StatusDisplay),
                },
                EntityType.JournalEntry when row is Core.Accounting.JournalEntryModel j => new()
                {
                    ("رقم القيد", j.EntryNumber), ("الوصف", j.Description),
                    ("مدين", $"{j.DebitTotal:N2} $"), ("دائن", $"{j.CreditTotal:N2} $"),
                },
                _ => new() { ("ملاحظة", "بيانات تجريبية") }
            };
        }

        private UIElement BuildGenericActionContent()
        {
            var stack = new StackPanel();
            stack.Children.Add(new TextBlock
            {
                Text = GetContentSectionTitle(),
                FontSize = 15, FontWeight = FontWeights.SemiBold,
                Foreground = Br("TextPrimaryBrush"),
                Margin = new Thickness(0, 0, 0, 12),
                FontFamily = Ff()
            });

            if (_request.ActionId == EntityActionId.ContainerExcelImport)
                stack.Children.Add(BuildExcelImportPanel());

            if (_request.ActionId == EntityActionId.FabricMovement)
            {
                stack.Children.Add(ErpUxFactory.ActionToolbar("حركة الأقمشة", ("تصدير", false)));
                stack.Children.Add(PlaceholderUi.EmptyMessage("لا توجد حركات مخزنية مسجلة"));
            }
            else if (_request.ActionId is EntityActionId.ContainerItems or EntityActionId.ContainerImportReview)
            {
                if (_request.EntityRow is ContainerListRow row)
                {
                    var items = new ContainerItemsWorkspaceControl();
                    items.Initialize(row.Id);
                    stack.Children.Add(items);
                }
                else
                {
                    stack.Children.Add(ErpUxFactory.InfoBanner("لم يتم تحديد حاوية.", "warning"));
                }
            }
            else
                stack.Children.Add(PlaceholderUi.DevelopmentPhase(GetContentSectionTitle()));

            return stack;
        }

        private string GetContentSectionTitle() => _request.ActionId switch
        {
            EntityActionId.CustomerInvoices or EntityActionId.SupplierInvoices => "قائمة الفواتير",
            EntityActionId.FabricMovement => "سجل الحركات",
            EntityActionId.ContainerDistribution => "توزيع الكميات",
            EntityActionId.ContainerStocktake => "جرد الحاوية",
            EntityActionId.ContainerApprove => "اعتماد الحاوية",
            EntityActionId.ContainerArchive => "أرشفة الحاوية",
            EntityActionId.EmployeeAttendance => "الحضور والانصراف",
            EntityActionId.EmployeeLeaves => "الإجازات",
            _ => "تفاصيل العملية"
        };

        private UIElement BuildExcelImportPanel()
        {
            var sp = new StackPanel();
            sp.Children.Add(ErpUxFactory.WorkflowStepper(
                ("وصول", true, true), ("رفع Excel", true, false),
                ("معاينة", false, false), ("Landing Cost", false, false), ("اعتماد", false, false)));
            sp.Children.Add(ErpUxFactory.InfoBanner("ارفع ملف Excel ثم راجع الأصناف قبل الحفظ.", "info"));
            sp.Children.Add(ErpUxFactory.ActionToolbar("استيراد Excel", ("اختيار ملف", true), ("معاينة", false), ("تأكيد", false)));
            return sp;
        }

        private static TextBlock MakeLabel(string text) => new()
        {
            Text = text, FontSize = 11,
            Foreground = Br("TextMutedBrush"),
            Margin = new Thickness(0, 0, 0, 2), FontFamily = Ff()
        };

        private static TextBlock MakeValue(string text) => new()
        {
            Text = text, FontSize = 14, FontWeight = FontWeights.SemiBold,
            Foreground = Br("TextPrimaryBrush"), FontFamily = Ff()
        };

        private static Brush Br(string k) => (Brush)System.Windows.Application.Current.Resources[k]!;
        private static FontFamily Ff() => new("Segoe UI, Tahoma, Arial");
    }
}
