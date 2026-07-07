using ERPSystem.Application.DTOs.Expenses;
using ERPSystem.Application.DTOs.Inventory;
using ERPSystem.Controls.Inventory;
using ERPSystem.Controls.Sales;
using ERPSystem.Services.Inventory;
using ERPSystem.Controls.Customers;
using ERPSystem.Controls.Purchases;
using ERPSystem.Controls.Suppliers;
using ERPSystem.Controls.China;
using ERPSystem.Application.DTOs.Finance;
using ERPSystem.Controls.Expenses;
using ERPSystem.Controls.Finance;
using ERPSystem.Controls.OperationsCenter;
using ERPSystem.Controls.Workspace;
using ERPSystem.Core;
using ERPSystem.Core.Accounting;
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
using ERPSystem.Views.Sales;
using ERPSystem.Services;
using ERPSystem.Services.China;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ERPSystem.Views.OperationsCenters
{
    public static class OperationsCenterFactory
    {
        private static int Idx(string key, params string[] keys)
        {
            for (int i = 0; i < keys.Length; i++)
                if (keys[i].Equals(key, StringComparison.OrdinalIgnoreCase)) return i;
            return 0;
        }
        public static UIElement? TryBuild(WorkspaceOpenRequest request)
        {
            var tab = ActionToTab(request.ActionId);
            if (IsOperationsCenterRequest(request))
            {
                return request.EntityType switch
                {
                    EntityType.Customer => BuildCustomer(request, tab ?? "Overview"),
                    EntityType.Supplier => BuildSupplier(request, tab ?? "Overview"),
                    EntityType.ImportContainer => BuildContainer(request, tab ?? "Overview"),
                    EntityType.FabricItem => BuildFabric(request, tab ?? "Overview"),
                    EntityType.Warehouse => BuildWarehouse(request, tab ?? "Overview"),
                    EntityType.SalesInvoice => BuildSalesInvoice(request, tab ?? "Overview"),
                    EntityType.PurchaseInvoice => BuildPurchaseInvoice(request, tab ?? "Overview"),
                    EntityType.Employee => BuildEmployee(request, tab ?? "Overview"),
                    EntityType.JournalEntry => BuildJournal(request, tab ?? "Overview"),
                    EntityType.Cashbox => BuildCashbox(request, tab ?? "Overview"),
                    EntityType.Expense => BuildExpense(request, tab ?? "Overview"),
                    _ => null
                };
            }

            if (request.ActionId is EntityActionId.CustomerStatement or EntityActionId.SupplierStatement)
                return BuildAccountStatement(request);

            if (request.ActionId == EntityActionId.InvoiceDetailLengths)
                return BuildDetailingPanel(request);

            if (request.ActionId is EntityActionId.ContainerApprove or EntityActionId.ContainerCosts)
                return BuildContainerLandingCostWorkspace(request);

            if (request.ActionId == EntityActionId.ContainerDistribution)
                return BuildContainerWorkflowWorkspace(request, stocktake: false);

            if (request.ActionId == EntityActionId.ContainerStocktake)
                return BuildContainerWorkflowWorkspace(request, stocktake: true);

            return null;
        }

        public static bool IsOperationsCenterRequest(WorkspaceOpenRequest request)
        {
            var tab = ActionToTab(request.ActionId);
            return tab != null
                   || request.ActionId == EntityActionId.OpenOperationsCenter
                   || IsOperationsCenterAction(request.ActionId);
        }

        private static bool IsOperationsCenterAction(EntityActionId id) => id switch
        {
            EntityActionId.CustomerDetails or EntityActionId.SupplierDetails or
            EntityActionId.ContainerDetails or EntityActionId.FabricCard or
            EntityActionId.InvoiceView or EntityActionId.PurchaseView or
            EntityActionId.EmployeeProfile or EntityActionId.JournalView or
            EntityActionId.ExpenseDetails => true,
            _ => false
        };

        private static string? ActionToTab(EntityActionId id) => id switch
        {
            EntityActionId.CustomerStatement => "Statement",
            EntityActionId.CustomerInvoices => "Invoices",
            EntityActionId.CustomerReceipt => "Receipts",
            EntityActionId.SupplierInvoices => "Invoices",
            EntityActionId.InvoiceDetailLengths => "Detailing",
            EntityActionId.ContainerCosts => "LandingCost",
            EntityActionId.ContainerImportReview => "Items",
            EntityActionId.ContainerDistribution => "Distribution",
            EntityActionId.FabricMovement => "Movements",
            EntityActionId.EmployeeAttendance => "Attendance",
            _ => null
        };

        private static UserControl BuildCustomer(WorkspaceOpenRequest req, string initialTab)
        {
            if (req.EntityRow is CustomerListRow row)
            {
                var ctrl = new CustomerOperationsCenterControl();
                ctrl.Initialize(row.Id, initialTab);
                return ctrl;
            }

            return new UserControl
            {
                Content = new TextBlock
                {
                    Text = "لم يتم تحديد عميل.",
                    Margin = new Thickness(24),
                    FontSize = 14
                }
            };
        }

        private static UserControl BuildSupplier(WorkspaceOpenRequest req, string initialTab)
        {
            if (req.EntityRow is SupplierListRow row)
            {
                var ctrl = new SupplierOperationsCenterControl();
                ctrl.Initialize(row.Id, initialTab);
                return ctrl;
            }

            return NoEntityContextControl();
        }

        private static UserControl BuildContainer(WorkspaceOpenRequest req, string initialTab)
        {
            if (req.EntityRow is ContainerListRow row)
            {
                var ctrl = new ChinaContainerOperationsCenterControl();
                ctrl.Initialize(row.Id, initialTab);
                return ctrl;
            }

            return new UserControl
            {
                Content = new TextBlock
                {
                    Text = "لم يتم تحديد حاوية.",
                    Margin = new Thickness(24),
                    FontSize = 14
                }
            };
        }

        private static UserControl BuildContainerLandingCostWorkspace(WorkspaceOpenRequest req)
        {
            if (req.EntityRow is not ContainerListRow row)
                return NoEntityContextControl();

            ChinaImportNavigationContext.SetActiveContainer(row.Id);
            return new UserControl
            {
                Content = new ChinaImportLandingCostReviewControl(),
                Background = Br("AppBgBrush") as SolidColorBrush
            };
        }

        private static UserControl BuildContainerWorkflowWorkspace(WorkspaceOpenRequest req, bool stocktake)
        {
            if (req.EntityRow is not ContainerListRow row)
                return NoEntityContextControl();

            ChinaImportNavigationContext.SetActiveContainer(row.Id);
            var summary = stocktake
                ? new ContainerWorkflowSummaryControl(
                    "جرد الحاوية",
                    "مقارنة النظام مع العد الفعلي داخل الحاوية",
                    stocktakeMode: true)
                : new ContainerWorkflowSummaryControl(
                    "توزيع الكميات على العملاء",
                    "توزيع أثواب الحاوية على المشترين والحجوزات");

            return new UserControl
            {
                Content = new ScrollViewer
                {
                    VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                    Padding = new Thickness(8),
                    Content = summary
                },
                Background = Br("AppBgBrush") as SolidColorBrush
            };
        }

        private static UserControl BuildFabric(WorkspaceOpenRequest req, string initialTab)
        {
            FabricItemModel? f = req.EntityRow as FabricItemModel;
            WarehouseStockRow? w = req.EntityRow as WarehouseStockRow;
            if (f is null && w is null)
                return NoEntityContextControl();

            var title = f?.FabricName ?? w?.GoodsType ?? "—";
            var code = f?.Code ?? w?.BoltCode ?? "—";
            return OperationsCenterShell.Build(new OperationsCenterSpec
            {
                Title = title,
                Subtitle = $"مركز عمليات الصنف — {code}",
                Breadcrumb = "الأمل.AB › المخزون › مركز عمليات الصنف",
                IconGlyph = "\uE821",
                Accent = Br("AccentInventoryBrush"),
                AccentLight = Br("SuccessBgBrush"),
                StatusBadge = f?.StatusDisplay ?? w?.Status ?? "—",
                HeaderFields =
                [
                    ("كود التوب", code),
                    ("اللون", f?.Color ?? w?.Color ?? "—"),
                    ("المستودع", f?.Warehouse ?? w?.Warehouse ?? "—"),
                    ("الحاوية", "—"),
                ],
                Kpis =
                [
                    ("الأثواب", (f?.RollCount ?? w?.RollCount ?? 0).ToString(), "\uE7C3"),
                    ("الأطوال", $"{(f?.TotalMeters ?? w?.TotalLength ?? 0):N0} م", "\uE821"),
                    ("محجوز", "—", "\uE823"),
                    ("مباع", "—", "\uE8F1"),
                    ("متبقي", "—", "\uE8FD"),
                    ("تكلفة/م", "—", "\uE8C1"),
                ],
                Tabs =
                [
                    Tab("Overview", "نظرة عامة", PlaceholderUi.DevelopmentPhase("نظرة عامة الصنف")),
                    Tab("Movements", "حركة المخزون", PlaceholderUi.DevelopmentPhase("حركة المخزون")),
                    Tab("SalesHistory", "سجل المبيعات", PlaceholderUi.DevelopmentPhase("سجل المبيعات")),
                    Tab("Reservations", "الحجوزات", PlaceholderUi.DevelopmentPhase("الحجوزات")),
                    Tab("Transfers", "المناقلات", PlaceholderUi.DevelopmentPhase("المناقلات")),
                    Tab("Adjustments", "التسويات", PlaceholderUi.DevelopmentPhase("التسويات")),
                    Tab("Purchases", "سجل الشراء", PlaceholderUi.DevelopmentPhase("سجل الشراء")),
                    Tab("Notes", "ملاحظات", NotesEditor("")),
                ],
                QuickActions =
                [
                    Q("حركة الصنف", false, "Movements"),
                    Q("مناقلة", false, "Transfers", actionKey: "form:NewTransfer"),
                ],
                InitialTabIndex = Idx(initialTab, "Overview", "Movements", "SalesHistory", "Reservations", "Transfers", "Adjustments", "Purchases", "Notes")
            });
        }

        private static UserControl BuildWarehouse(WorkspaceOpenRequest req, string initialTab)
        {
            if (req.EntityRow is WarehouseListExtendedDto dto)
            {
                InventoryNavigationContext.BeginWorkspace(dto.Id, initialTab);
                return new InventoryOperationsCenterControl();
            }

            if (req.EntityRow is not WarehouseEntity w)
                return NoEntityContextControl();

            return OperationsCenterShell.Build(new OperationsCenterSpec
            {
                Title = w.Name,
                Subtitle = "مركز عمليات المستودع",
                Breadcrumb = "الأمل.AB › المخزون › مستودع",
                IconGlyph = "\uE8B7",
                Accent = Br("AccentInventoryBrush"),
                AccentLight = Br("SuccessBgBrush"),
                StatusBadge = w.Status,
                HeaderFields =
                [
                    ("الكود", w.Code), ("المدينة", w.City),
                    ("السعة المستخدمة", w.CapacityPercent > 0 ? $"{w.CapacityPercent}%" : "—"),
                    ("إجمالي الأثواب", w.RollCount > 0 ? w.RollCount.ToString("N0") : "—"),
                ],
                Kpis =
                [
                    ("السعة", w.CapacityPercent > 0 ? $"{w.CapacityPercent}%" : "—", "\uE8B7"),
                    ("مشغول", "—", "\uE821"),
                    ("متاح", "—", "\uE8FD"),
                    ("أثواب", w.RollCount > 0 ? w.RollCount.ToString("N0") : "—", "\uE7C3"),
                    ("أطوال", w.TotalLength > 0 ? $"{w.TotalLength:N0} م" : "—", "\uE821"),
                    ("تسليم معلق", "—", "\uE823"),
                    ("مناقلات", "—", "\uE8AB"),
                    ("تنبيهات", "—", "\uE783"),
                ],
                Tabs =
                [
                    Tab("Overview", "نظرة عامة", PlaceholderUi.DevelopmentPhase("نظرة عامة المستودع")),
                    Tab("Inventory", "المخزون", PlaceholderUi.DevelopmentPhase("أرصدة المستودع")),
                    Tab("Transfers", "المناقلات", PlaceholderUi.DevelopmentPhase("المناقلات")),
                    Tab("Stocktake", "الجرد", PlaceholderUi.DevelopmentPhase("الجرد")),
                    Tab("Reservations", "الحجوزات", PlaceholderUi.DevelopmentPhase("الحجوزات")),
                    Tab("Detailing", "تفصيل معلق", PlaceholderUi.DevelopmentPhase("فواتير بانتظار التفصيل")),
                    Tab("Movements", "الحركات", PlaceholderUi.DevelopmentPhase("حركات المستودع")),
                ],
                QuickActions =
                [
                    Q("مناقلة جديدة", true, "Transfers", actionKey: "form:NewTransfer"),
                    Q("جرد", false, "Stocktake", actionKey: "form:Stocktake"),
                ],
                InitialTabIndex = Idx(initialTab, "Overview", "Inventory", "Transfers", "Stocktake", "Reservations", "Detailing", "Movements")
            });
        }

        private static UserControl BuildSalesInvoice(WorkspaceOpenRequest req, string initialTab)
        {
            if (req.EntityRow is SalesInvoiceListRow listRow)
            {
                var ctrl = new SalesInvoiceOperationsCenterControl();
                ctrl.Initialize(listRow.Id, initialTab);
                return ctrl;
            }

            if (req.EntityRow is Guid invoiceId && invoiceId != Guid.Empty)
            {
                var ctrl = new SalesInvoiceOperationsCenterControl();
                ctrl.Initialize(invoiceId, initialTab);
                return ctrl;
            }

            return OperationsCenterShell.Build(new OperationsCenterSpec
            {
                Title = "فاتورة بيع",
                Subtitle = "مركز عمليات فاتورة البيع",
                Breadcrumb = "الأمل.AB › المبيعات › فاتورة",
                IconGlyph = "\uE9F9",
                Accent = Br("AccentSalesBrush"),
                AccentLight = Br("PrimaryVeryLightBrush"),
                StatusBadge = "—",
                Tabs =
                [
                    Tab("Overview", "نظرة عامة", new TextBlock
                    {
                        Text = "يرجى اختيار سجل من القائمة لفتح مركز العمليات",
                        Margin = new Thickness(24),
                        FontSize = 14,
                        TextWrapping = TextWrapping.Wrap,
                        FontFamily = new FontFamily("Segoe UI, Tahoma, Arial")
                    }),
                ],
                InitialTabIndex = 0
            });
        }

        private static UserControl BuildPurchaseInvoice(WorkspaceOpenRequest req, string initialTab)
        {
            if (req.EntityRow is PurchaseListRow row)
            {
                var ctrl = new PurchaseInvoiceOperationsCenterControl();
                ctrl.Initialize(row.Id);
                return ctrl;
            }

            return NoEntityContextControl();
        }

        private static UserControl BuildEmployee(WorkspaceOpenRequest req, string initialTab)
        {
            if (req.EntityRow is not EmployeeModel e)
                return NoEntityContextControl();

            return OperationsCenterShell.Build(new OperationsCenterSpec
            {
                Title = e.FullName,
                Subtitle = "مركز عمليات الموظف",
                Breadcrumb = "الأمل.AB › الموارد البشرية › موظف",
                IconGlyph = "\uE716",
                Accent = Br("InfoBrush"),
                AccentLight = Br("InfoBgBrush"),
                StatusBadge = e.StatusDisplay,
                HeaderFields =
                [
                    ("الكود", e.EmployeeCode), ("القسم", e.Department),
                    ("المسمى", e.JobTitle), ("الراتب", e.BasicSalary > 0 ? $"{e.BasicSalary:N0} $" : "—"),
                ],
                Kpis =
                [
                    ("القسم", e.Department, "\uEE57"),
                    ("الوردية", string.IsNullOrWhiteSpace(e.Shift) ? "—" : e.Shift, "\uE728"),
                    ("الحضور", "—", "\uE823"),
                    ("إجازات", "—", "\uE787"),
                ],
                Tabs =
                [
                    Tab("Overview", "نظرة عامة", PlaceholderUi.DevelopmentPhase("نظرة عامة الموظف")),
                    Tab("Attendance", "الحضور", PlaceholderUi.DevelopmentPhase("الحضور")),
                    Tab("Leaves", "الإجازات", PlaceholderUi.DevelopmentPhase("الإجازات")),
                    Tab("Contracts", "العقود", PlaceholderUi.DevelopmentPhase("العقود")),
                    Tab("Timeline", "الخط الزمني", EmptyTimeline()),
                ],
                QuickActions =
                [
                    Q("الحضور", false, "Attendance"),
                    Q("إجازة", false, "Leaves"),
                    Q("تعديل", false, null, actionKey: "form:EditEmployee"),
                ],
                InitialTabIndex = Idx(initialTab, "Overview", "Attendance", "Leaves", "Contracts", "Timeline"),
                Context = new OperationsCenterContext
                {
                    EntityType = EntityType.Employee,
                    EntityRow = e,
                    SourceModule = AppModule.HR,
                    Title = e.FullName
                }
            });
        }

        private static UserControl BuildJournal(WorkspaceOpenRequest req, string initialTab)
        {
            if (req.EntityRow is not JournalEntryModel j)
                return NoEntityContextControl();

            return OperationsCenterShell.Build(new OperationsCenterSpec
            {
                Title = j.EntryNumber,
                Subtitle = "مركز عمليات القيد / السند",
                Breadcrumb = "الأمل.AB › المالية › قيد",
                IconGlyph = "\uE8C1",
                Accent = Br("PrimaryBrush"),
                AccentLight = Br("PrimaryVeryLightBrush"),
                StatusBadge = j.StatusDisplay,
                HeaderFields =
                [
                    ("التاريخ", j.EntryDate.ToString("yyyy/MM/dd")),
                    ("الوصف", j.Description),
                    ("مدين", $"{j.DebitTotal:N2}"),
                    ("دائن", $"{j.CreditTotal:N2}"),
                ],
                Tabs =
                [
                    Tab("Overview", "سطور القيد", PlaceholderUi.DevelopmentPhase("سطور القيد")),
                    Tab("Timeline", "الخط الزمني", EmptyTimeline()),
                ],
                QuickActions =
                [
                    Q("طباعة", false, null, actionKey: "preview:قيد يومية"),
                ],
                InitialTabIndex = 0,
                Context = new OperationsCenterContext
                {
                    EntityType = EntityType.JournalEntry,
                    EntityRow = j,
                    SourceModule = AppModule.Accounting,
                    Title = j.EntryNumber
                }
            });
        }

        private static UserControl BuildExpense(WorkspaceOpenRequest req, string initialTab)
        {
            if (req.EntityRow is ExpenseListDto row)
            {
                var ctrl = new ExpenseOperationsCenterControl();
                ctrl.Initialize(row.Id, initialTab);
                return ctrl;
            }

            if (req.EntityRow is ExpenseDetailsDto details)
            {
                var ctrl = new ExpenseOperationsCenterControl();
                ctrl.Initialize(details.Id, initialTab);
                return ctrl;
            }

            return new UserControl
            {
                Content = new TextBlock { Text = "لم يتم تحديد مصروف.", Margin = new Thickness(24) }
            };
        }

        private static UserControl BuildCashbox(WorkspaceOpenRequest req, string initialTab)
        {
            Guid? id = req.EntityRow switch
            {
                CashboxListDto dto => dto.Id,
                _ => null
            };

            if (!id.HasValue)
                return NoEntityContextControl();

            var tab = initialTab switch
            {
                "Transfers" => "Transfers",
                _ => "Movements"
            };

            var oc = new CashboxOperationsCenterControl();
            oc.InitializeForPopup(id.Value, tab);
            return oc;
        }

        private static UIElement BuildAccountStatement(WorkspaceOpenRequest req)
        {
            if (req.EntityRow is CustomerListRow row)
            {
                var ctrl = new CustomerAccountStatementControl();
                ctrl.Initialize(row.Id, row.NameAr);
                return ctrl;
            }

            var name = EntityDisplayNameResolver.Resolve(req.EntityRow, req.EntityType);
            var fallback = new CustomerAccountStatementControl();
            fallback.SetCustomerName(name);
            return fallback;
        }

        private static UIElement BuildDetailingPanel(WorkspaceOpenRequest req)
        {
            if (req.EntityRow is SalesInvoiceListRow listRow)
            {
                var ctrl = new SalesInvoiceOperationsCenterControl();
                ctrl.Initialize(listRow.Id, "Detailing");
                return ctrl;
            }

            return new TextBlock
            {
                Text = "يرجى فتح مركز العمليات من قائمة الفواتير",
                Margin = new Thickness(24),
                FontSize = 14,
                TextWrapping = TextWrapping.Wrap,
                FontFamily = new FontFamily("Segoe UI, Tahoma, Arial")
            };
        }

        // --- Tab content helpers ---

        private static UserControl NoEntityContextControl() => new()
        {
            Content = new TextBlock
            {
                Text = "يرجى اختيار سجل من القائمة لفتح مركز العمليات",
                Margin = new Thickness(24),
                FontSize = 14,
                TextWrapping = TextWrapping.Wrap,
                FontFamily = new FontFamily("Segoe UI, Tahoma, Arial")
            }
        };

        private static UIElement NotesEditor(string placeholder) =>
            ErpUiFactory.Card(new TextBox
            {
                Text = placeholder,
                Height = 120,
                TextWrapping = TextWrapping.Wrap,
                AcceptsReturn = true,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto
            });

        private static UIElement EmptyTimeline() =>
            PlaceholderUi.EmptyMessage("لا يوجد سجل نشاط");

        private static UIElement PrintPreview()
        {
            var s = new StackPanel();
            s.Children.Add(ErpUxFactory.ExportBar());
            s.Children.Add(PlaceholderUi.DatabasePhase("معاينة الطباعة A4"));
            return s;
        }

        private static UIElement WrapDetailing(UIElement ctrl) =>
            new ScrollViewer { Content = ctrl, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };

        private static OperationsCenterTab Tab(string key, string label, UIElement content) =>
            new() { Key = key, Label = label, Content = content };

        private static OperationsCenterQuickAction Q(string label, bool primary, string? tab,
            bool destructive = false, bool confirm = false, string? actionKey = null) =>
            new() { Label = label, Primary = primary, TabKey = tab, Destructive = destructive, RequiresConfirmation = confirm, ActionKey = actionKey };

        private static SolidColorBrush Br(string k) => (SolidColorBrush)System.Windows.Application.Current.Resources[k]!;
    }
}
