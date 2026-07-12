using ERPSystem.Application.DTOs.Identity;
using ERPSystem.Helpers;
using ERPSystem.Services;
using ERPSystem.Services.Identity;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ERPSystem.Controls.Settings;

public sealed class SettingsUsersRolesPageControl : UserControl
{
    private readonly ListBox _rolesList = new() { DisplayMemberPath = nameof(IdentityRoleListDto.Name), Margin = new Thickness(8) };
    private readonly StackPanel _permissionTree = new();
    private readonly ScrollViewer _permissionScroll;
    private readonly DataGrid _usersGrid = new() { AutoGenerateColumns = false, IsReadOnly = true, Margin = new Thickness(8) };
    private readonly TextBox _usernameBox = new() { Height = 34, Width = 140, Margin = new Thickness(0, 0, 8, 0), VerticalContentAlignment = VerticalAlignment.Center };
    private readonly PasswordBox _passwordBox = new() { Height = 34, Width = 140, Margin = new Thickness(0, 0, 8, 0), VerticalContentAlignment = VerticalAlignment.Center };
    private readonly TextBox _fullNameArBox = new() { Height = 34, Width = 180, Margin = new Thickness(0, 0, 8, 0), VerticalContentAlignment = VerticalAlignment.Center };
    private readonly ComboBox _roleCombo = new() { Height = 34, Width = 180, Margin = new Thickness(0, 0, 8, 0), DisplayMemberPath = nameof(IdentityRoleListDto.Name) };

    private readonly Dictionary<string, CheckBox> _permissionChecks = new(StringComparer.OrdinalIgnoreCase);
    private readonly TextBlock _systemRoleNotice = new()
    {
        Margin = new Thickness(8, 8, 8, 8),
        TextWrapping = TextWrapping.Wrap,
        Visibility = Visibility.Collapsed
    };
    private IReadOnlyList<PermissionModuleGroupDto> _tree = [];
    private IReadOnlyList<IdentityRoleListDto> _roles = [];
    private Guid? _selectedRoleId;
    private bool _isSystemRole;
    private bool _isLoading;

    public SettingsUsersRolesPageControl()
    {
        _systemRoleNotice.Foreground = Br("PrimaryBrush");
        _systemRoleNotice.FontFamily = Ff();

        _permissionScroll = new ScrollViewer
        {
            Content = _permissionTree,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            MaxHeight = 520
        };

        var tabs = new TabControl { Margin = new Thickness(0, 8, 0, 0) };
        tabs.Items.Add(new TabItem { Header = "الأدوار والصلاحيات", Content = BuildRolesPanel() });
        tabs.Items.Add(new TabItem { Header = "المستخدمون", Content = BuildUsersPanel() });

        Content = tabs;
        Loaded += OnLoaded;
    }

    private UIElement BuildRolesPanel()
    {
        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(220) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var left = new StackPanel { Background = Brushes.White };
        left.Children.Add(new TextBlock
        {
            Text = "الأدوار",
            FontWeight = FontWeights.SemiBold,
            Margin = new Thickness(12, 12, 12, 8),
            FontFamily = Ff()
        });
        _rolesList.SelectionChanged += OnRoleSelected;
        left.Children.Add(_rolesList);

        var newRoleRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(8, 0, 8, 12) };
        var roleNameBox = new TextBox { Height = 34, Width = 120, Margin = new Thickness(0, 0, 6, 0), VerticalContentAlignment = VerticalAlignment.Center };
        var addRoleBtn = new Button { Content = "دور جديد", Style = S("SecondaryButtonStyle"), Height = 34 };
        addRoleBtn.Click += async (_, _) =>
        {
            var name = roleNameBox.Text?.Trim() ?? "";
            if (string.IsNullOrWhiteSpace(name))
            {
                MockInteractionService.ShowWarning("أدخل اسم الدور.", "الأدوار");
                return;
            }

            var result = await IdentityUiService.Instance.CreateRoleAsync(name, "");
            if (!result.IsSuccess)
            {
                MockInteractionService.ShowWarning(result.ErrorMessage ?? "تعذّر إنشاء الدور.", "الأدوار");
                return;
            }

            roleNameBox.Clear();
            MockInteractionService.ShowSuccess("تم إنشاء الدور.", "الأدوار");
            await LoadRolesAsync();
            _rolesList.SelectedItem = _roles.FirstOrDefault(r => r.Id == result.Value);
        };
        newRoleRow.Children.Add(roleNameBox);
        newRoleRow.Children.Add(addRoleBtn);
        left.Children.Add(newRoleRow);
        Grid.SetColumn(left, 0);
        grid.Children.Add(left);

        var right = new StackPanel();
        right.Children.Add(new TextBlock
        {
            Text = "صلاحيات الدور — حدّد المهام المسموحة لكل قسم",
            Foreground = Br("TextSecondaryBrush"),
            Margin = new Thickness(0, 0, 0, 8),
            FontFamily = Ff()
        });
        right.Children.Add(_systemRoleNotice);
        right.Children.Add(ErpUiFactory.Card(_permissionScroll));

        var actions = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 12, 0, 0) };
        var selectAllBtn = new Button { Content = "تحديد الكل", Style = S("SecondaryButtonStyle"), Height = 34, Margin = new Thickness(0, 0, 8, 0) };
        var clearBtn = new Button { Content = "إلغاء الكل", Style = S("SecondaryButtonStyle"), Height = 34, Margin = new Thickness(0, 0, 8, 0) };
        var saveBtn = new Button { Content = "حفظ الصلاحيات", Style = S("PrimaryButtonStyle"), Height = 34 };
        selectAllBtn.Click += (_, _) => SetAllChecks(true);
        clearBtn.Click += (_, _) => SetAllChecks(false);
        saveBtn.Click += OnSavePermissions;
        actions.Children.Add(selectAllBtn);
        actions.Children.Add(clearBtn);
        actions.Children.Add(saveBtn);
        right.Children.Add(actions);
        Grid.SetColumn(right, 1);
        grid.Children.Add(right);

        return grid;
    }

    private UIElement BuildUsersPanel()
    {
        var stack = new StackPanel();
        ErpUiFactory.AddGridColumn(_usersGrid, "اسم المستخدم", nameof(IdentityUserListDto.Username), 140, null);
        ErpUiFactory.AddGridColumn(_usersGrid, "الاسم", nameof(IdentityUserListDto.FullNameAr), "*", null);
        ErpUiFactory.AddGridColumn(_usersGrid, "الأدوار", nameof(UserRoleNamesDisplay), "*", null);
        ErpUiFactory.AddGridColumn(_usersGrid, "نشط", nameof(IdentityUserListDto.IsActive), 70, null);
        stack.Children.Add(ErpUiFactory.Card(_usersGrid));

        var form = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 12, 0, 0) };
        SetPlaceholder(_usernameBox, "اسم المستخدم");
        SetPlaceholder(_fullNameArBox, "الاسم بالعربي");
        var addUserBtn = new Button { Content = "إضافة مستخدم", Style = S("PrimaryButtonStyle"), Height = 34 };
        addUserBtn.Click += OnAddUser;
        form.Children.Add(_usernameBox);
        form.Children.Add(_passwordBox);
        form.Children.Add(_fullNameArBox);
        form.Children.Add(_roleCombo);
        form.Children.Add(addUserBtn);
        stack.Children.Add(form);

        stack.Children.Add(new TextBlock
        {
            Text = "كلمة المرور تُستخدم عند أول تسجيل دخول — يمكن تغييرها لاحقاً من قاعدة البيانات.",
            Foreground = Br("TextMutedBrush"),
            FontSize = 11,
            Margin = new Thickness(0, 8, 0, 0),
            FontFamily = Ff()
        });

        return stack;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        Loaded -= OnLoaded;
        await LoadAllAsync();
    }

    private async Task LoadAllAsync()
    {
        if (!AppServices.IsInitialized || _isLoading) return;
        _isLoading = true;
        try
        {
            _tree = await IdentityUiService.Instance.GetPermissionTreeAsync();
            BuildPermissionTreeUi();
            await LoadRolesAsync();
            await LoadUsersAsync();
        }
        finally
        {
            _isLoading = false;
        }
    }

    private async Task LoadRolesAsync()
    {
        _roles = await IdentityUiService.Instance.GetRolesAsync();
        _rolesList.ItemsSource = _roles;
        _roleCombo.ItemsSource = _roles;
        if (_selectedRoleId is Guid id)
            _rolesList.SelectedItem = _roles.FirstOrDefault(r => r.Id == id);
        else if (_roles.Count > 0)
            _rolesList.SelectedIndex = 0;
    }

    private async Task LoadUsersAsync()
    {
        var users = await IdentityUiService.Instance.GetUsersAsync();
        _usersGrid.ItemsSource = users.Select(u => new UserRoleNamesDisplay(u)).ToList();
    }

    private void BuildPermissionTreeUi()
    {
        _permissionTree.Children.Clear();
        _permissionChecks.Clear();

        foreach (var module in _tree)
        {
            var modulePanel = new StackPanel { Margin = new Thickness(0, 0, 0, 4) };
            var moduleCheck = new CheckBox
            {
                Content = module.ModuleLabelAr,
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(8, 4, 8, 4),
                FontFamily = Ff()
            };

            var tasksPanel = new StackPanel { Margin = new Thickness(24, 0, 8, 8) };
            var moduleBoxes = new List<CheckBox>();

            foreach (var perm in module.Permissions)
            {
                var cb = new CheckBox
                {
                    Content = perm.LabelAr,
                    Tag = perm.Code,
                    Margin = new Thickness(0, 2, 0, 2),
                    FontFamily = Ff()
                };
                cb.Checked += (_, _) => SyncModuleCheck(moduleCheck, moduleBoxes);
                cb.Unchecked += (_, _) => SyncModuleCheck(moduleCheck, moduleBoxes);
                tasksPanel.Children.Add(cb);
                moduleBoxes.Add(cb);
                _permissionChecks[perm.Code] = cb;
            }

            moduleCheck.Checked += (_, _) =>
            {
                foreach (var b in moduleBoxes) b.IsChecked = true;
            };
            moduleCheck.Unchecked += (_, _) =>
            {
                foreach (var b in moduleBoxes) b.IsChecked = false;
            };

            modulePanel.Children.Add(moduleCheck);
            modulePanel.Children.Add(tasksPanel);
            _permissionTree.Children.Add(new Expander
            {
                Header = module.ModuleLabelAr,
                IsExpanded = true,
                Content = modulePanel,
                Margin = new Thickness(4, 2, 4, 2),
                FontFamily = Ff()
            });
        }
    }

    private static void SyncModuleCheck(CheckBox moduleCheck, IReadOnlyList<CheckBox> boxes)
    {
        if (boxes.Count == 0) return;
        var all = boxes.All(b => b.IsChecked == true);
        var none = boxes.All(b => b.IsChecked != true);
        moduleCheck.IsChecked = all ? true : none ? false : null;
    }

    private async void OnRoleSelected(object sender, SelectionChangedEventArgs e)
    {
        if (_rolesList.SelectedItem is not IdentityRoleListDto role)
            return;

        _selectedRoleId = role.Id;
        _isSystemRole = role.IsSystem;

        var dto = await IdentityUiService.Instance.GetRolePermissionsAsync(role.Id);
        var selected = dto?.PermissionCodes.ToHashSet(StringComparer.OrdinalIgnoreCase) ?? [];

        foreach (var (code, cb) in _permissionChecks)
        {
            cb.IsChecked = selected.Contains(code);
            cb.IsEnabled = !_isSystemRole;
        }

        if (_isSystemRole)
        {
            _systemRoleNotice.Text = "دور النظام (Administrator) — يملك كل الصلاحيات تلقائياً ولا يمكن تعديله.";
            _systemRoleNotice.Visibility = Visibility.Visible;
        }
        else
        {
            _systemRoleNotice.Visibility = Visibility.Collapsed;
        }
    }

    private void SetAllChecks(bool value)
    {
        if (_isSystemRole) return;
        foreach (var cb in _permissionChecks.Values)
            cb.IsChecked = value;
    }

    private async void OnSavePermissions(object sender, RoutedEventArgs e)
    {
        if (_selectedRoleId is not Guid roleId)
        {
            MockInteractionService.ShowWarning("اختر دوراً أولاً.", "الصلاحيات");
            return;
        }

        if (_isSystemRole)
        {
            MockInteractionService.ShowInfo("دور النظام لا يحتاج حفظ — كل الصلاحيات مفعّلة.", "الصلاحيات");
            return;
        }

        var codes = _permissionChecks
            .Where(p => p.Value.IsChecked == true)
            .Select(p => p.Key)
            .ToList();

        var result = await IdentityUiService.Instance.SaveRolePermissionsAsync(roleId, codes);
        if (result.IsSuccess)
            MockInteractionService.ShowSuccess("تم حفظ صلاحيات الدور.", "الصلاحيات");
        else
            MockInteractionService.ShowWarning(result.ErrorMessage ?? "تعذّر الحفظ.", "الصلاحيات");

        await LoadRolesAsync();
    }

    private async void OnAddUser(object sender, RoutedEventArgs e)
    {
        var username = _usernameBox.Tag?.ToString() == "placeholder" ? "" : _usernameBox.Text?.Trim() ?? "";
        var fullNameAr = _fullNameArBox.Tag?.ToString() == "placeholder" ? "" : _fullNameArBox.Text?.Trim() ?? "";
        var password = _passwordBox.Password;

        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(fullNameAr))
        {
            MockInteractionService.ShowWarning("اسم المستخدم وكلمة المرور والاسم بالعربي مطلوبان.", "المستخدمون");
            return;
        }

        var roleIds = _roleCombo.SelectedItem is IdentityRoleListDto role
            ? new[] { role.Id }
            : Array.Empty<Guid>();

        var result = await IdentityUiService.Instance.CreateUserAsync(username, password, fullNameAr, "", roleIds);
        if (!result.IsSuccess)
        {
            MockInteractionService.ShowWarning(result.ErrorMessage ?? "تعذّر إضافة المستخدم.", "المستخدمون");
            return;
        }

        _usernameBox.Clear();
        _passwordBox.Clear();
        _fullNameArBox.Clear();
        _roleCombo.SelectedIndex = -1;
        MockInteractionService.ShowSuccess("تم إضافة المستخدم.", "المستخدمون");
        await LoadUsersAsync();
    }

    private static void SetPlaceholder(TextBox box, string text)
    {
        box.Text = text;
        box.Tag = "placeholder";
        box.Foreground = Br("TextMutedBrush");
        box.GotFocus += (_, _) =>
        {
            if (box.Tag?.ToString() == "placeholder")
            {
                box.Text = "";
                box.Tag = null;
                box.Foreground = Br("TextPrimaryBrush");
            }
        };
        box.LostFocus += (_, _) =>
        {
            if (string.IsNullOrWhiteSpace(box.Text))
            {
                box.Text = text;
                box.Tag = "placeholder";
                box.Foreground = Br("TextMutedBrush");
            }
        };
    }

    private static FontFamily Ff() => new("Segoe UI, Tahoma, Arial");
    private static Style S(string key) => (Style)System.Windows.Application.Current.Resources[key]!;
    private static Brush Br(string key) => (Brush)System.Windows.Application.Current.Resources[key]!;

    private sealed class UserRoleNamesDisplay(IdentityUserListDto user)
    {
        public string Username => user.Username;
        public string FullNameAr => user.FullNameAr;
        public bool IsActive => user.IsActive;
        public string RoleNames => string.Join("، ", user.RoleNames);
    }
}
