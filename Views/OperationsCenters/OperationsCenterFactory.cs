using ERPSystem.Application.DTOs.Expenses;
using ERPSystem.Controls.Sales;
using ERPSystem.Controls.Customers;
using ERPSystem.Controls.China;
using ERPSystem.Controls.Expenses;
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
            if (tab != null || request.ActionId == EntityActionId.OpenOperationsCenter ||
                IsOperationsCenterAction(request.ActionId))
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

            if (request.ActionId == EntityActionId.ContainerCosts)
                return BuildContainer(request, "LandingCost");

            return null;
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
            var s = req.EntityRow as SupplierModel ?? SupplierSampleData.Generate(1).First();
            return OperationsCenterShell.Build(new OperationsCenterSpec
            {
                Title = s.Name,
                Subtitle = "مركز عمليات المورد — استيراد ومحلي",
                Breadcrumb = "ERP PRO › الموردون › مركز العمليات",
                IconGlyph = "\uE779",
                Accent = Br("AccentPayableBrush"),
                AccentLight = Br("WarningBgBrush"),
                StatusBadge = s.StatusDisplay,
                HeaderFields =
                [
                    ("كود المورد", s.Code), ("البلد", s.Country), ("النوع", s.TypeDisplay),
                    ("الرصيد", $"{s.Balance:N0} ر.س"), ("عدد الفواتير", s.InvoiceCount.ToString()),
                ],
                Kpis =
                [
                    ("الرصيد", $"{s.Balance:N0} ر.س", "\uE8C1"),
                    ("مشتريات", "95,000 ر.س", "\uE7BF"),
                    ("حاويات", "4", "\uE7BF"),
                    ("مدفوعات", "62,000 ر.س", "\uE719"),
                ],
                Tabs =
                [
                    Tab("Overview", "نظرة عامة", OverviewSupplier(s)),
                    Tab("Statement", "كشف الحساب", StatementTabContent(s.Name)),
                    Tab("Invoices", "فواتير الشراء", PlaceholderUi.MockGrid(new[] {
                        new { رقم = "PUR-0088", التاريخ = "2026/06/24", المبلغ = "95,000" },
                    })),
                    Tab("Payments", "المدفوعات", PlaceholderUi.MockGrid(new[] {
                        new { رقم = "PAY-001", المبلغ = "40,000", التاريخ = "2026/06/10" },
                    })),
                    Tab("Containers", "الحاويات", PlaceholderUi.MockGrid(new[] {
                        new { حاوية = "CN-2026-001", المورد = s.Name, الحالة = "وصلت" },
                    })),
                    Tab("ImportHistory", "سجل الاستيراد", PlaceholderUi.MockGrid(new[] {
                        new { الشهر = "يونيو 2026", الحاويات = 2, القيمة = "185,000" },
                    })),
                    Tab("Notes", "ملاحظات", NotesEditor("ملاحظات المورد")),
                    Tab("Timeline", "الخط الزمني", TimelineMock("مورد")),
                ],
                QuickActions =
                [
                    Q("سند دفع", true, "Payments", actionKey: "nav:Accounting:Payments"),
                    Q("كشف حساب", false, "Statement"),
                    Q("PDF", false, null, actionKey: "preview:كشف حساب المورد"),
                    Q("تعديل", false, null, actionKey: "form:EditSupplier"),
                    Q("تعطيل", false, null, destructive: true, confirm: true, actionKey: "success:تم تعطيل المورد (تجريبي)"),
                ],
                InitialTabIndex = Idx(initialTab, "Overview", "Statement", "Invoices", "Payments", "Containers", "ImportHistory", "Notes", "Timeline"),
                Context = new OperationsCenterContext
                {
                    EntityType = EntityType.Supplier,
                    EntityRow = s,
                    SourceModule = AppModule.Suppliers,
                    Title = s.Name
                }
            });
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

        private static UserControl BuildFabric(WorkspaceOpenRequest req, string initialTab)
        {
            FabricItemModel? f = req.EntityRow as FabricItemModel;
            WarehouseStockRow? w = req.EntityRow as WarehouseStockRow;
            var title = f?.FabricName ?? w?.GoodsType ?? "قماش";
            var code = f?.Code ?? w?.BoltCode ?? "—";
            return OperationsCenterShell.Build(new OperationsCenterSpec
            {
                Title = title,
                Subtitle = $"مركز عمليات الصنف — {code}",
                Breadcrumb = "ERP PRO › المخزون › مركز عمليات الصنف",
                IconGlyph = "\uE821",
                Accent = Br("AccentInventoryBrush"),
                AccentLight = Br("SuccessBgBrush"),
                StatusBadge = f?.StatusDisplay ?? w?.Status ?? "متوفر",
                HeaderFields =
                [
                    ("كود التوب", code),
                    ("اللون", f?.Color ?? w?.Color ?? "—"),
                    ("المستودع", f?.Warehouse ?? w?.Warehouse ?? "—"),
                    ("الحاوية", "CN-2026-001"),
                ],
                Kpis =
                [
                    ("الأثواب", (f?.RollCount ?? w?.RollCount ?? 0).ToString(), "\uE7C3"),
                    ("الأطوال", $"{(f?.TotalMeters ?? w?.TotalLength ?? 0):N0} م", "\uE821"),
                    ("محجوز", "45 م", "\uE823"),
                    ("مباع", "120 م", "\uE8F1"),
                    ("متبقي", "555 م", "\uE8FD"),
                    ("تكلفة/م", "42.50 ر.س", "\uE8C1"),
                ],
                Tabs =
                [
                    Tab("Overview", "نظرة عامة", PlaceholderUi.MockGrid(new[] {
                        new { البند = "الموقع", القيمة = w?.Location ?? "A-12" },
                        new { البند = "اللوط", القيمة = w?.Lot ?? "LOT-01" },
                    })),
                    Tab("Movements", "حركة المخزون", PlaceholderUi.MockGrid(new[] {
                        new { التاريخ = "2026/06/26", النوع = "وارد", الكمية = "+120 م", المرجع = "CN-001" },
                        new { التاريخ = "2026/06/25", النوع = "صادر", الكمية = "-45 م", المرجع = "INV-1045" },
                    })),
                    Tab("SalesHistory", "سجل المبيعات", PlaceholderUi.TabContent("سجل المبيعات")),
                    Tab("Reservations", "الحجوزات", PlaceholderUi.TabContent("الحجوزات")),
                    Tab("Transfers", "المناقلات", PlaceholderUi.TabContent("المناقلات")),
                    Tab("Adjustments", "التسويات", PlaceholderUi.TabContent("التسويات")),
                    Tab("Purchases", "سجل الشراء", PlaceholderUi.TabContent("سجل الشراء")),
                    Tab("Notes", "ملاحظات", NotesEditor("ملاحظات الصنف")),
                ],
                QuickActions =
                [
                    Q("حركة الصنف", false, "Movements"),
                    Q("مناقلة", false, "Transfers", actionKey: "form:NewTransfer"),
                    Q("تعديل السعر", false, null, actionKey: "form:EditPrice"),
                ],
                InitialTabIndex = Idx(initialTab, "Overview", "Movements", "SalesHistory", "Reservations", "Transfers", "Adjustments", "Purchases", "Notes")
            });
        }

        private static UserControl BuildWarehouse(WorkspaceOpenRequest req, string initialTab)
        {
            var w = req.EntityRow as WarehouseEntity ?? new WarehouseEntity
            {
                Code = "WH-01", Name = "المستودع الرئيسي", City = "جدة",
                RollCount = 4850, TotalLength = 385420, CapacityPercent = 72
            };
            return OperationsCenterShell.Build(new OperationsCenterSpec
            {
                Title = w.Name,
                Subtitle = "مركز عمليات المستودع",
                Breadcrumb = "ERP PRO › المخزون › مستودع",
                IconGlyph = "\uE8B7",
                Accent = Br("AccentInventoryBrush"),
                AccentLight = Br("SuccessBgBrush"),
                StatusBadge = w.Status,
                HeaderFields =
                [
                    ("الكود", w.Code), ("المدينة", w.City),
                    ("السعة المستخدمة", $"{w.CapacityPercent}%"),
                    ("إجمالي الأثواب", w.RollCount.ToString("N0")),
                ],
                Kpis =
                [
                    ("السعة", $"{w.CapacityPercent}%", "\uE8B7"),
                    ("مشغول", "72%", "\uE821"),
                    ("متاح", "28%", "\uE8FD"),
                    ("أثواب", w.RollCount.ToString("N0"), "\uE7C3"),
                    ("أطوال", $"{w.TotalLength:N0} م", "\uE821"),
                    ("تسليم معلق", "7", "\uE823"),
                    ("مناقلات", "3", "\uE8AB"),
                    ("تنبيهات", "2", "\uE783"),
                ],
                Tabs =
                [
                    Tab("Overview", "نظرة عامة", PlaceholderUi.MockGrid(new[] {
                        new { مؤشر = "تفصيل معلق", القيمة = "7 فواتير" },
                        new { مؤشر = "مناقلات جارية", القيمة = "3" },
                    })),
                    Tab("Inventory", "المخزون", PlaceholderUi.TabContent("أرصدة المستودع")),
                    Tab("Transfers", "المناقلات", PlaceholderUi.TabContent("المناقلات")),
                    Tab("Stocktake", "الجرد", PlaceholderUi.TabContent("الجرد")),
                    Tab("Reservations", "الحجوزات", PlaceholderUi.TabContent("الحجوزات")),
                    Tab("Detailing", "تفصيل معلق", PlaceholderUi.TabContent("فواتير بانتظار التفصيل")),
                    Tab("Movements", "الحركات", PlaceholderUi.TabContent("حركات المستودع")),
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
                Breadcrumb = "ERP PRO › المبيعات › فاتورة",
                IconGlyph = "\uE9F9",
                Accent = Br("AccentSalesBrush"),
                AccentLight = Br("PrimaryVeryLightBrush"),
                StatusBadge = "—",
                Tabs =
                [
                    Tab("Overview", "نظرة عامة", new TextBlock
                    {
                        Text = "يرجى فتح مركز العمليات من قائمة الفواتير",
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
            var p = req.EntityRow as PurchaseInvoiceModel ?? PurchaseSampleData.Generate(1).First();
            return OperationsCenterShell.Build(new OperationsCenterSpec
            {
                Title = p.InvoiceNumber,
                Subtitle = "مركز عمليات فاتورة الشراء",
                Breadcrumb = "ERP PRO › المشتريات › فاتورة",
                IconGlyph = "\uE7BF",
                Accent = Br("AccentOrdersBrush"),
                AccentLight = Br("WarningBgBrush"),
                StatusBadge = p.StatusDisplay,
                HeaderFields =
                [
                    ("المورد", p.SupplierName),
                    ("التاريخ", p.InvoiceDate.ToString("yyyy/MM/dd")),
                    ("الإجمالي", $"{p.TotalAmount:N0} ر.س"),
                    ("المتبقي", $"{p.Remaining:N0} ر.س"),
                ],
                Kpis =
                [
                    ("الإجمالي", $"{p.TotalAmount:N0} ر.س", "\uE8C1"),
                    ("المتبقي", $"{p.Remaining:N0} ر.س", "\uE719"),
                    ("البنود", "12", "\uE821"),
                ],
                Tabs =
                [
                    Tab("Overview", "التفاصيل", PlaceholderUi.MockGrid(new[] {
                        new { صنف = "قماش صيني", الكمية = "500 م", السعر = "38", الإجمالي = "19,000" },
                    })),
                    Tab("Timeline", "الخط الزمني", TimelineMock("شراء")),
                    Tab("Printing", "الطباعة", PrintPreview()),
                ],
                QuickActions =
                [
                    Q("طباعة", false, null, actionKey: "preview:فاتورة الشراء"),
                    Q("PDF", false, null, actionKey: "preview:فاتورة الشراء"),
                    Q("مرتجع", false, null, actionKey: "nav:Purchases:Returns"),
                    Q("إلغاء", false, null, destructive: true, confirm: true, actionKey: "success:تم إلغاء فاتورة الشراء (تجريبي)"),
                ],
                InitialTabIndex = Idx(initialTab, "Overview", "Timeline", "Printing")
            });
        }

        private static UserControl BuildEmployee(WorkspaceOpenRequest req, string initialTab)
        {
            var e = req.EntityRow as EmployeeModel ?? HRSampleData.Generate(1).First();
            return OperationsCenterShell.Build(new OperationsCenterSpec
            {
                Title = e.FullName,
                Subtitle = "مركز عمليات الموظف",
                Breadcrumb = "ERP PRO › الموارد البشرية › موظف",
                IconGlyph = "\uE716",
                Accent = Br("InfoBrush"),
                AccentLight = Br("InfoBgBrush"),
                StatusBadge = e.StatusDisplay,
                HeaderFields =
                [
                    ("الكود", e.EmployeeCode), ("القسم", e.Department),
                    ("المسمى", e.JobTitle), ("الراتب", $"{e.BasicSalary:N0} ر.س"),
                ],
                Kpis =
                [
                    ("القسم", e.Department, "\uEE57"),
                    ("الوردية", e.Shift, "\uE728"),
                    ("الحضور", "22/26", "\uE823"),
                    ("إجازات", "3", "\uE787"),
                ],
                Tabs =
                [
                    Tab("Overview", "نظرة عامة", PlaceholderUi.MockGrid(new[] {
                        new { البند = "تاريخ التعيين", القيمة = e.HireDate.ToString("yyyy/MM/dd") },
                    })),
                    Tab("Attendance", "الحضور", PlaceholderUi.MockGrid(new[] {
                        new { تاريخ = "2026/06/26", دخول = "08:00", خروج = "17:00" },
                    })),
                    Tab("Leaves", "الإجازات", PlaceholderUi.TabContent("الإجازات")),
                    Tab("Contracts", "العقود", PlaceholderUi.TabContent("العقود")),
                    Tab("Timeline", "الخط الزمني", TimelineMock("موظف")),
                ],
                QuickActions =
                [
                    Q("الحضور", false, "Attendance"),
                    Q("إجازة", false, "Leaves"),
                    Q("تعديل", false, null, actionKey: "form:EditEmployee"),
                    Q("تعطيل", false, null, destructive: true, confirm: true, actionKey: "success:تم تعطيل الموظف (تجريبي)"),
                ],
                InitialTabIndex = Idx(initialTab, "Overview", "Attendance", "Leaves", "Contracts", "Timeline")
            });
        }

        private static UserControl BuildJournal(WorkspaceOpenRequest req, string initialTab)
        {
            var j = req.EntityRow as JournalEntryModel ?? AccountingSampleData.Generate(1).First();
            return OperationsCenterShell.Build(new OperationsCenterSpec
            {
                Title = j.EntryNumber,
                Subtitle = "مركز عمليات القيد / السند",
                Breadcrumb = "ERP PRO › المالية › قيد",
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
                    Tab("Overview", "سطور القيد", PlaceholderUi.MockGrid(new[] {
                        new { حساب = "الصندوق", مدين = "10,000", دائن = "—" },
                        new { حساب = "ذمم", مدين = "—", دائن = "10,000" },
                    })),
                    Tab("Timeline", "الخط الزمني", TimelineMock("قيد")),
                ],
                QuickActions =
                [
                    Q("طباعة", false, null, actionKey: "preview:قيد يومية"),
                    Q("PDF", false, null, actionKey: "preview:قيد يومية"),
                    Q("Excel", false, null, actionKey: "preview:قيد يومية"),
                    Q("إلغاء", false, null, destructive: true, confirm: true, actionKey: "success:تم إلغاء القيد (تجريبي)"),
                ],
                InitialTabIndex = 0
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
            var cb = req.EntityRow as Cashbox ?? new Cashbox { Code = "CB-01", Name = "الصندوق الرئيسي", Balance = 125000 };
            return OperationsCenterShell.Build(new OperationsCenterSpec
            {
                Title = cb.Name,
                Subtitle = "مركز عمليات الصندوق",
                Breadcrumb = "ERP PRO › المالية › صندوق",
                IconGlyph = "\uE825",
                Accent = Br("PrimaryBrush"),
                AccentLight = Br("PrimaryVeryLightBrush"),
                StatusBadge = "نشط",
                HeaderFields = [("الكود", cb.Code), ("الرصيد", $"{cb.Balance:N0} ر.س")],
                Kpis =
                [
                    ("الرصيد", $"{cb.Balance:N0} ر.س", "\uE8C1"),
                    ("قبض اليوم", "32,400", "\uE7BF"),
                    ("صرف اليوم", "8,200", "\uE719"),
                ],
                Tabs =
                [
                    Tab("Overview", "الحركات", PlaceholderUi.MockGrid(new[] {
                        new { التاريخ = "2026/06/26", النوع = "قبض", المبلغ = "10,000" },
                    })),
                    Tab("Transfers", "التحويلات", PlaceholderUi.TabContent("تحويلات الصندوق")),
                ],
                QuickActions =
                [
                    Q("سند قبض", true, null, actionKey: "nav:Accounting:Receipts"),
                    Q("سند صرف", false, null, actionKey: "nav:Accounting:Payments"),
                ],
                InitialTabIndex = 0
            });
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

        private static UIElement OverviewCustomer(CustomerModel c)
        {
            var s = new StackPanel();
            s.Children.Add(ErpUxFactory.InfoBanner($"مندوب المبيعات: خالد الشمري — آخر تواصل 2026/06/24", "info"));
            s.Children.Add(PlaceholderUi.MockGrid(new[] {
                new { المؤشر = "متوسط قيمة الفاتورة", القيمة = "28,500 ر.س" },
                new { المؤشر = "أكثر قماش شراءً", القيمة = "كولومبيا" },
            }));
            return s;
        }

        private static UIElement OverviewSupplier(SupplierModel s) =>
            PlaceholderUi.MockGrid(new[] {
                new { المؤشر = "آخر حاوية", القيمة = "—" },
                new { المؤشر = "متوسط lead time", القيمة = "—" },
            });

        private static UIElement StatementTabContent(string name)
        {
            var req = new WorkspaceOpenRequest
            {
                EntityType = EntityType.Customer,
                ActionId = EntityActionId.CustomerStatement,
                EntityDisplayName = name
            };
            return BuildAccountStatement(req);
        }

        private static UIElement NotesEditor(string placeholder) =>
            ErpUiFactory.Card(new TextBox
            {
                Text = placeholder,
                Height = 120,
                TextWrapping = TextWrapping.Wrap,
                AcceptsReturn = true,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto
            });

        private static UIElement TimelineMock(string entity) =>
            PlaceholderUi.MockGrid(new[] {
                new { التاريخ = "2026/06/26 09:30", الحدث = $"تحديث {entity}", المستخدم = "مدير النظام" },
                new { التاريخ = "2026/06/20 14:00", الحدث = "اعتماد", المستخدم = "محاسب" },
            });

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
