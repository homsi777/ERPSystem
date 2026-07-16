using ERPSystem.Application.Common;
using ERPSystem.Controls;
using ERPSystem.Controls.Settings;
using ERPSystem.Helpers;
using ERPSystem.Services;
using ERPSystem.Services.Settings;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

namespace ERPSystem.Views.Settings
{
    public static class SettingsViews
    {
        private static readonly (string Key, string Title, string Desc)[] Sections =
        [
            ("Company", "هوية الشركة", "اسم الشركة، الشعار، السجل التجاري"),
            ("Branches", "الفروع والمستودعات", "إدارة الفروع والمستودعات"),
            ("Users", "المستخدمون والأدوار والصلاحيات", "حسابات وصلاحيات"),
            ("UserSessions", "حالة المستخدمين", "تسجيل الدخول والخروج — متصفح أو سطح مكتب"),
            ("Locale", "اللغة والمنطقة", "العربية RTL والتنسيق"),
            ("Currencies", "العملات وأسعار الصرف", "$ والعملات الأخرى"),
            ("Finance", "الإعدادات المالية", "السنة المالية والحسابات"),
            ("Taxes", "الضرائب", "ضريبة القيمة المضافة 15%"),
            ("Numbering", "ترقيم المستندات", "تسلسل الفواتير والسندات"),
            ("Print", "قوالب الطباعة", "فواتير وتقارير A4"),
            ("Inventory", "إعدادات المخزون", "وحدات القياس والجرد"),
            ("Sales", "إعدادات المبيعات", "سياسة التفصيل والتسعير"),
            ("Backup", "النسخ الاحتياطي والاستعادة", "جدولة النسخ"),
            ("Audit", "سجل التدقيق", "تتبع التغييرات"),
        ];

        public static UserControl Create(string key)
        {
            if (string.IsNullOrWhiteSpace(key) || key.Equals("Hub", StringComparison.OrdinalIgnoreCase))
                return BuildHub();
            var sec = Sections.FirstOrDefault(s => s.Key.Equals(key, StringComparison.OrdinalIgnoreCase));
            if (string.IsNullOrEmpty(sec.Key))
                return BuildHub();
            return BuildSettingsForm(sec.Title, sec.Desc, sec.Key);
        }

        private static UserControl BuildHub()
        {
            var root = new ScrollViewer { Padding = new Thickness(16), VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
            var stack = new StackPanel();
            stack.Children.Add(ErpUiFactory.SectionTitle("مركز التحكم — الأمل.AB"));
            stack.Children.Add(new TextBlock
            {
                Text = "إعدادات النظام — كل فئة تفتح مساحة عمل مستقلة",
                Foreground = Br("TextSecondaryBrush"),
                Margin = new Thickness(0, 0, 0, 12),
                FontFamily = Ff()
            });

            var search = new TextBox
            {
                Height = 38, Margin = new Thickness(0, 0, 0, 16),
                Text = "", Tag = "placeholder",
                Padding = new Thickness(12, 0, 12, 0),
                VerticalContentAlignment = VerticalAlignment.Center,
                FontFamily = Ff()
            };
            SetPlaceholder(search, "بحث في الإعدادات...");
            stack.Children.Add(search);

            var grid = new UniformGrid { Columns = 3 };
            stack.Children.Add(grid);

            void RenderCards(string? filter)
            {
                grid.Children.Clear();
                var q = filter?.Trim() ?? "";
                foreach (var s in Sections)
                {
                    if (!string.IsNullOrEmpty(q) &&
                        s.Title.IndexOf(q, StringComparison.OrdinalIgnoreCase) < 0 &&
                        s.Desc.IndexOf(q, StringComparison.OrdinalIgnoreCase) < 0 &&
                        s.Key.IndexOf(q, StringComparison.OrdinalIgnoreCase) < 0)
                        continue;

                    var inner = new StackPanel
                    {
                        Children =
                        {
                            new TextBlock { Text = s.Title, FontWeight = FontWeights.SemiBold, FontSize = 14, Margin = new Thickness(0,0,0,6), FontFamily = Ff() },
                            new TextBlock { Text = s.Desc, FontSize = 12, Foreground = Br("TextSecondaryBrush"), TextWrapping = TextWrapping.Wrap, FontFamily = Ff() }
                        }
                    };
                    var card = ErpUiFactory.Card(inner, new Thickness(4));
                    card.Cursor = Cursors.Hand;
                    card.MouseLeftButtonUp += (_, _) => NavigateSettings(s.Key, card);
                    grid.Children.Add(card);
                }

                if (grid.Children.Count == 0)
                {
                    grid.Children.Add(ErpUiFactory.Card(new TextBlock
                    {
                        Text = "لا توجد إعدادات مطابقة للبحث",
                        Foreground = Br("TextMutedBrush"),
                        Margin = new Thickness(8),
                        FontFamily = Ff()
                    }, new Thickness(4)));
                }
            }

            search.TextChanged += (_, _) => RenderCards(search.Text == "placeholder" ? "" : search.Text);
            search.GotFocus += (_, _) => { if (search.Tag?.ToString() == "placeholder") { search.Text = ""; search.Tag = null; } };
            search.LostFocus += (_, _) => { if (string.IsNullOrWhiteSpace(search.Text)) SetPlaceholder(search, "بحث في الإعدادات..."); };
            RenderCards(null);

            root.Content = stack;
            return new UserControl { Content = root, Background = Br("AppBgBrush") as SolidColorBrush };
        }

        private static UserControl BuildSettingsForm(string title, string desc, string key)
        {
            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(240) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var sidebar = new StackPanel { Background = Brushes.White, Margin = new Thickness(0, 0, 1, 0) };
            sidebar.Children.Add(new TextBlock { Text = "مركز التحكم", FontWeight = FontWeights.Bold, Margin = new Thickness(16, 16, 16, 8), FontSize = 16, FontFamily = Ff() });
            var search = new TextBox { Margin = new Thickness(12, 0, 12, 12), Height = 34 };
            SetPlaceholder(search, "بحث في الإعدادات...");
            search.TextChanged += (_, _) =>
            {
                var q = search.Text.Trim();
                foreach (var child in sidebar.Children.OfType<Button>())
                {
                    if (child.Tag is not string k) continue;
                    var sec = Sections.First(s => s.Key == k);
                    child.Visibility = string.IsNullOrEmpty(q) ||
                        sec.Title.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                        sec.Desc.Contains(q, StringComparison.OrdinalIgnoreCase)
                        ? Visibility.Visible : Visibility.Collapsed;
                }
            };
            sidebar.Children.Add(search);
            foreach (var s in Sections)
            {
                var btn = new Button
                {
                    Content = s.Title,
                    HorizontalContentAlignment = HorizontalAlignment.Right,
                    Height = 36,
                    Margin = new Thickness(8, 1, 8, 1),
                    Tag = s.Key,
                    FontFamily = Ff()
                };
                btn.Click += (_, _) => NavigateSettings(s.Key, btn);
                if (s.Key == key)
                {
                    btn.Background = Br("PrimaryVeryLightBrush");
                    btn.Foreground = Br("PrimaryBrush");
                }
                sidebar.Children.Add(btn);
            }
            Grid.SetColumn(sidebar, 0);
            grid.Children.Add(sidebar);

            var content = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto, Padding = new Thickness(16) };
            var stack = new StackPanel();
            stack.Children.Add(new TextBlock
            {
                Text = "الأمل.AB › الإعدادات › " + title,
                FontSize = 11, Foreground = Br("TextMutedBrush"), Margin = new Thickness(0, 0, 0, 8), FontFamily = Ff()
            });
            stack.Children.Add(ErpUiFactory.SectionTitle(title));
            stack.Children.Add(new TextBlock { Text = desc, Foreground = Br("TextSecondaryBrush"), Margin = new Thickness(0, 0, 0, 16), FontFamily = Ff() });
            BuildSectionContent(stack, key);

            if (title.Contains("نسخ") || title.Contains("تدقيق"))
            {
                stack.Children.Add(ErpUiFactory.Card(new TextBlock
                {
                    Text = "⚠ منطقة عمليات حساسة — تتطلب صلاحية مدير",
                    Foreground = Br("DangerBrush"),
                    Margin = new Thickness(8),
                    FontFamily = Ff()
                }));
            }

            content.Content = stack;
            Grid.SetColumn(content, 1);
            grid.Children.Add(content);

            return new UserControl { Content = grid, Background = Br("AppBgBrush") as SolidColorBrush };
        }

        private static void BuildSectionContent(StackPanel stack, string key)
        {
            switch (key)
            {
                case "Company":
                    BuildKeyValueSection(stack,
                        (SystemSettingKeys.CompanyName, "اسم الشركة (عربي)"),
                        (SystemSettingKeys.CompanyNameEn, "اسم الشركة (English)"),
                        (SystemSettingKeys.CompanyTaxNumber, "الرقم الضريبي"),
                        (SystemSettingKeys.CompanyPhone, "الهاتف"),
                        (SystemSettingKeys.CompanyAddress, "العنوان"),
                        (SystemSettingKeys.CompanyLogoPath, "مسار الشعار"));
                    break;
                case "Finance":
                    BuildKeyValueSection(stack,
                        (SystemSettingKeys.DefaultCurrency, "العملة الافتراضية"),
                        (SystemSettingKeys.DefaultExchangeRate, "سعر الصرف الافتراضي"),
                        (SystemSettingKeys.EnabledCurrencies, "العملات المفعّلة (مفصولة بفاصلة)"));
                    break;
                case "Numbering":
                    BuildKeyValueSection(stack,
                        (SystemSettingKeys.InvoicePrefix, "بادئة الفواتير"),
                        (SystemSettingKeys.ReceiptPrefix, "بادئة سندات القبض"),
                        (SystemSettingKeys.PurchaseOrderPrefix, "بادئة أوامر الشراء"));
                    break;
                case "Branches":
                    BuildBranchesSection(stack);
                    break;
                case "Users":
                    stack.Children.Add(new SettingsUsersRolesPageControl());
                    break;
                case "UserSessions":
                    stack.Children.Add(new SettingsUserSessionsPageControl());
                    break;
                default:
                    BuildUnsupportedSection(stack);
                    break;
            }
        }

        private static void BuildKeyValueSection(StackPanel stack, params (string Key, string Label)[] fields)
        {
            var editors = new Dictionary<string, TextBox>();
            var formStack = new StackPanel();
            foreach (var (fieldKey, label) in fields)
            {
                var row = new StackPanel { Margin = new Thickness(0, 0, 0, 12) };
                row.Children.Add(new TextBlock { Text = label, FontSize = 12, Foreground = Br("TextSecondaryBrush"), Margin = new Thickness(0, 0, 0, 4), FontFamily = Ff() });
                var box = new TextBox { Height = 34, Padding = new Thickness(8, 0, 8, 0), VerticalContentAlignment = VerticalAlignment.Center, FontFamily = Ff() };
                editors[fieldKey] = box;
                row.Children.Add(box);
                formStack.Children.Add(row);
            }
            stack.Children.Add(ErpUiFactory.Card(formStack));

            var saveBtn = new Button { Content = "حفظ", Style = S("PrimaryButtonStyle"), Height = 34, Margin = new Thickness(0, 16, 0, 0), HorizontalAlignment = HorizontalAlignment.Left };
            saveBtn.Click += async (_, _) =>
            {
                if (!AppServices.IsInitialized) return;
                try
                {
                    var values = editors.ToDictionary(e => e.Key, e => e.Value.Text?.Trim() ?? "");
                    await SettingsUiService.Instance.SaveAsync(values);
                    MockInteractionService.ShowSuccess("تم حفظ الإعدادات بنجاح.", "الإعدادات");
                }
                catch (Exception ex)
                {
                    MockInteractionService.ShowWarning("تعذّر حفظ الإعدادات: " + ex.Message, "الإعدادات");
                }
            };
            stack.Children.Add(saveBtn);

            stack.Loaded += async (_, _) =>
            {
                if (!AppServices.IsInitialized) return;
                try
                {
                    var all = await SettingsUiService.Instance.LoadAllAsync();
                    foreach (var (fieldKey, box) in editors)
                        if (all.TryGetValue(fieldKey, out var v)) box.Text = v;
                }
                catch { /* first-run: no rows yet */ }
            };
        }

        private static void BuildBranchesSection(StackPanel stack)
        {
            var list = new StackPanel { Margin = new Thickness(0, 0, 0, 12) };
            stack.Children.Add(ErpUiFactory.SectionTitle("الفروع"));
            stack.Children.Add(ErpUiFactory.Card(list));

            var addStack = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 8, 0, 0) };
            var codeBox = new TextBox { Width = 110, Height = 34, Margin = new Thickness(0, 0, 8, 0), VerticalContentAlignment = VerticalAlignment.Center, FontFamily = Ff() };
            var nameArBox = new TextBox { Width = 200, Height = 34, Margin = new Thickness(0, 0, 8, 0), VerticalContentAlignment = VerticalAlignment.Center, FontFamily = Ff() };
            var nameEnBox = new TextBox { Width = 200, Height = 34, Margin = new Thickness(0, 0, 8, 0), VerticalContentAlignment = VerticalAlignment.Center, FontFamily = Ff() };
            SetPlaceholder(codeBox, "الكود");
            SetPlaceholder(nameArBox, "الاسم بالعربي");
            SetPlaceholder(nameEnBox, "الاسم بالإنجليزي");
            var addBtn = new Button { Content = "إضافة فرع", Style = S("PrimaryButtonStyle"), Height = 34 };
            addStack.Children.Add(codeBox);
            addStack.Children.Add(nameArBox);
            addStack.Children.Add(nameEnBox);
            addStack.Children.Add(addBtn);
            stack.Children.Add(addStack);

            async Task RefreshAsync()
            {
                if (!AppServices.IsInitialized) return;
                list.Children.Clear();
                try
                {
                    var branches = await SettingsUiService.Instance.GetBranchesAsync();
                    if (branches.Count == 0)
                    {
                        list.Children.Add(new TextBlock { Text = "لا توجد فروع.", Foreground = Br("TextMutedBrush"), Margin = new Thickness(8), FontFamily = Ff() });
                        return;
                    }
                    foreach (var b in branches)
                        list.Children.Add(new TextBlock
                        {
                            Text = $"{b.Code} — {b.NameAr}",
                            Margin = new Thickness(8, 4, 8, 4),
                            FontFamily = Ff()
                        });
                }
                catch (Exception ex)
                {
                    list.Children.Add(new TextBlock { Text = ex.Message, Foreground = Br("DangerBrush"), Margin = new Thickness(8), FontFamily = Ff() });
                }
            }

            addBtn.Click += async (_, _) =>
            {
                if (!AppServices.IsInitialized) return;
                var code = codeBox.Tag?.ToString() == "placeholder" ? "" : codeBox.Text?.Trim() ?? "";
                var nameAr = nameArBox.Tag?.ToString() == "placeholder" ? "" : nameArBox.Text?.Trim() ?? "";
                var nameEn = nameEnBox.Tag?.ToString() == "placeholder" ? "" : nameEnBox.Text?.Trim() ?? "";
                if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(nameAr))
                {
                    MockInteractionService.ShowWarning("الكود والاسم بالعربي مطلوبان.", "الفروع");
                    return;
                }
                try
                {
                    await SettingsUiService.Instance.AddBranchAsync(code, nameAr, nameEn);
                    MockInteractionService.ShowSuccess("تمت إضافة الفرع.", "الفروع");
                    await RefreshAsync();
                }
                catch (Exception ex)
                {
                    MockInteractionService.ShowWarning("تعذّر إضافة الفرع: " + ex.Message, "الفروع");
                }
            };

            stack.Loaded += async (_, _) => await RefreshAsync();
        }

        private static void BuildUnsupportedSection(StackPanel stack)
        {
            stack.Children.Add(PlaceholderUi.DevelopmentPhase("هذا القسم"));
            var saveBtn = new Button { Content = "حفظ", Style = S("PrimaryButtonStyle"), Height = 34, Margin = new Thickness(0, 16, 0, 0), HorizontalAlignment = HorizontalAlignment.Left };
            saveBtn.Click += (_, _) => MockInteractionService.ShowInfo("هذا القسم قيد التطوير.", "الإعدادات");
            stack.Children.Add(saveBtn);
        }

        private static void NavigateSettings(string key, UIElement source)
        {
            DependencyObject? p = source;
            while (p != null)
            {
                if (p is ModuleShellControl shell)
                {
                    shell.SelectSubpage(key);
                    return;
                }
                p = VisualTreeHelper.GetParent(p);
            }
        }

        private static void SetPlaceholder(TextBox box, string text)
        {
            box.Text = text;
            box.Tag = "placeholder";
            box.Foreground = Br("TextMutedBrush");
            box.GotFocus += OnPlaceholderFocus;
            box.LostFocus += OnPlaceholderBlur;
        }

        private static void OnPlaceholderFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox box && box.Tag?.ToString() == "placeholder")
            {
                box.Text = "";
                box.Tag = null;
                box.Foreground = Br("TextPrimaryBrush");
            }
        }

        private static void OnPlaceholderBlur(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox box && string.IsNullOrWhiteSpace(box.Text))
                SetPlaceholder(box, "بحث في الإعدادات...");
        }

        private static Brush Br(string k) => (Brush)System.Windows.Application.Current.Resources[k]!;
        private static Style S(string k) => (Style)System.Windows.Application.Current.Resources[k]!;
        private static FontFamily Ff() => new("Segoe UI, Tahoma, Arial");
    }
}
