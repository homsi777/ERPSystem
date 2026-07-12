using ERPSystem.Core;
using ERPSystem.Core.Navigation;
using ERPSystem.Services;
using ERPSystem.Application.Abstractions.Services;
using ERPSystem.Helpers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace ERPSystem.Shell
{
    public partial class TopNavBar : UserControl
    {
        public event EventHandler<NavigationRequest>? NavigationRequested;

        private AppModule _activeModule = AppModule.Dashboard;

        private readonly List<(Button Button, Popup? Popup, AppModule Module)> _navItems = new();
        private readonly List<MenuDef> _menuDefs = new();
        private Popup? _overflowPopup;
        private bool _languageSubscribed;

        private record SubItem(string LabelKey, string Icon, AppModule Module, string SubPage,
                               bool Highlighted = false);
        private record Section(string? TitleKey, SubItem[] Items);
        private record MenuDef(string LabelKey, string Icon, AppModule Module,
                               Section[]? Sections = null, bool Direct = false);

        public TopNavBar()
        {
            InitializeComponent();
            Loaded += OnLoaded;
            SizeChanged += (_, _) => UpdateOverflow();
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (!_languageSubscribed)
            {
                LocalizationManager.Instance.LanguageChanged += (_, _) => Rebuild();
                _languageSubscribed = true;
            }
            Rebuild();
        }

        private void Rebuild()
        {
            _menuDefs.Clear();
            _menuDefs.AddRange(GetMenuDefinitions());
            CloseAllDropdowns();
            RenderNavItems(_menuDefs.Count);
            UpdateOverflow();
        }

        private void RenderNavItems(int count)
        {
            NavItemsHost.Children.Clear();
            NavItemsHost.ColumnDefinitions.Clear();
            _navItems.Clear();

            count = Math.Clamp(count, 0, _menuDefs.Count);
            for (int i = 0; i < count; i++)
                NavItemsHost.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            for (int i = 0; i < count; i++)
            {
                var entry = BuildNavEntry(_menuDefs[i]);
                entry.HorizontalAlignment = HorizontalAlignment.Stretch;
                Grid.SetColumn(entry, i);
                NavItemsHost.Children.Add(entry);
            }

            NavItemsHost.MinWidth = count * 76;
            UpdateActiveStates();
        }

        private IEnumerable<MenuDef> GetMenuDefinitions()
        {
            var permissionCodes = GetCurrentPermissionCodes();
            foreach (var item in NavigationCatalog.TopLevel)
            {
                if (!AppModuleAccess.CanAccess(item.Module, permissionCodes))
                    continue;

                var subs = NavigationCatalog.GetSubItems(item.Module);
                if (item.Direct || subs.Count == 0)
                {
                    yield return new MenuDef(item.LabelKey, item.Icon, item.Module, Direct: true);
                    continue;
                }

                var subItems = subs.Select(s => new SubItem(s.Label, s.Icon, s.Module, s.SubPage)).ToArray();
                yield return new MenuDef(item.LabelKey, item.Icon, item.Module,
                    Sections: [new Section(null, subItems)]);
            }
        }

        private FrameworkElement BuildNavEntry(MenuDef def)
        {
            var label = LocalizationManager.Instance[def.LabelKey];
            return def.Direct || def.Sections == null
                ? BuildDirectButton(label, def.Icon, def.Module)
                : BuildDropdownEntry(label, def.Icon, def.Module, def.Sections);
        }

        private FrameworkElement BuildDirectButton(string label, string icon, AppModule module)
        {
            var btn = CreateNavButton(label, icon, hasDropdown: false, module);
            btn.Click += (_, _) =>
            {
                CloseAllDropdowns();
                NavigationRequested?.Invoke(this, new NavigationRequest(module));
            };
            _navItems.Add((btn, null, module));
            return btn;
        }

        private Grid BuildDropdownEntry(string label, string icon, AppModule module, Section[] sections)
        {
            var btn = CreateNavButton(label, icon, hasDropdown: true, module);
            var dropdownContent = sections.Length > 1
                ? BuildMegaDropdown(sections)
                : BuildSimpleDropdown(sections);

            var popup = new Popup
            {
                PlacementTarget = btn,
                Placement = PlacementMode.Bottom,
                StaysOpen = false,
                AllowsTransparency = true,
                PopupAnimation = PopupAnimation.Fade,
                Child = dropdownContent,
                FlowDirection = LocalizationManager.Instance.FlowDir
            };

            btn.Click += (_, _) =>
            {
                if (popup.IsOpen)
                {
                    popup.IsOpen = false;
                    return;
                }
                CloseAllDropdowns();
                popup.FlowDirection = LocalizationManager.Instance.FlowDir;
                popup.IsOpen = true;
            };

            popup.Closed += (_, _) => btn.ClearValue(Button.BackgroundProperty);

            _navItems.Add((btn, popup, module));

            var container = new Grid { HorizontalAlignment = HorizontalAlignment.Stretch };
            container.Children.Add(btn);
            return container;
        }

        private static Button CreateNavButton(string label, string icon, bool hasDropdown, AppModule module)
        {
            return new Button
            {
                Style = (Style)System.Windows.Application.Current.Resources["TopNavItemButtonStyle"]!,
                Content = BuildNavLabel(label, icon, hasDropdown),
                Tag = module,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                HorizontalContentAlignment = HorizontalAlignment.Center
            };
        }

        /// <summary>Icon above label; one subtle chevron only when the section has submodules.</summary>
        private static StackPanel BuildNavLabel(string text, string iconGlyph, bool hasDropdown)
        {
            var root = new StackPanel
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            root.Children.Add(new TextBlock
            {
                Text = iconGlyph,
                FontFamily = ErpDesignTokens.IconFont,
                FontSize = 17,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 3)
            });

            var labelRow = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            labelRow.Children.Add(new TextBlock
            {
                Text = text,
                FontFamily = ErpDesignTokens.UiFont,
                FontSize = 11,
                TextTrimming = TextTrimming.CharacterEllipsis,
                MaxWidth = 110,
                TextAlignment = TextAlignment.Center
            });

            if (hasDropdown)
            {
                labelRow.Children.Add(new TextBlock
                {
                    Text = "\uE70D",
                    FontFamily = ErpDesignTokens.IconFont,
                    FontSize = 8,
                    Foreground = (Brush)System.Windows.Application.Current.Resources["NavBarTextBrush"]!,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(3, 2, 0, 0),
                    Opacity = 0.85
                });
            }

            root.Children.Add(labelRow);
            return root;
        }

        private Border BuildSimpleDropdown(Section[] sections)
        {
            var outer = new Border { Style = (Style)System.Windows.Application.Current.Resources["DropdownCardStyle"]! };
            outer.CornerRadius = new CornerRadius(8);
            var stack = new StackPanel { MinWidth = 220 };
            outer.Child = stack;
            foreach (var section in sections)
                AppendSection(stack, section);
            return outer;
        }

        private Border BuildMegaDropdown(Section[] sections)
        {
            var outer = new Border
            {
                Style = (Style)System.Windows.Application.Current.Resources["DropdownCardStyle"]!,
                MinWidth = 440,
                CornerRadius = new CornerRadius(8)
            };

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var divider = new Border
            {
                Width = 1,
                Background = (SolidColorBrush)System.Windows.Application.Current.Resources["BorderLightBrush"]!,
                Margin = new Thickness(4, 8, 4, 8)
            };
            Grid.SetColumn(divider, 1);
            grid.Children.Add(divider);

            for (int i = 0; i < Math.Min(sections.Length, 2); i++)
            {
                var col = new StackPanel { Margin = new Thickness(4) };
                AppendSection(col, sections[i]);
                Grid.SetColumn(col, i == 0 ? 0 : 2);
                grid.Children.Add(col);
            }

            outer.Child = grid;
            return outer;
        }

        private void AppendSection(Panel parent, Section section)
        {
            var loc = LocalizationManager.Instance;
            if (section.TitleKey != null)
            {
                parent.Children.Add(new TextBlock
                {
                    Text = loc[section.TitleKey].ToUpperInvariant(),
                    Style = (Style)System.Windows.Application.Current.Resources["DropdownSectionHeaderStyle"]!
                });
            }

            foreach (var item in section.Items)
                parent.Children.Add(BuildSubItem(item));
        }

        private Button BuildSubItem(SubItem item)
        {
            var loc = LocalizationManager.Instance;
            var style = item.Highlighted
                ? (Style)System.Windows.Application.Current.Resources["DropdownSubItemHighlightStyle"]!
                : (Style)System.Windows.Application.Current.Resources["DropdownSubItemStyle"]!;

            var sp = new StackPanel { Orientation = Orientation.Horizontal };
            sp.Children.Add(new TextBlock
            {
                Text = item.Icon,
                FontFamily = ErpDesignTokens.IconFont,
                FontSize = 14,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 10, 0),
                Foreground = item.Highlighted
                    ? (SolidColorBrush)System.Windows.Application.Current.Resources["PrimaryBrush"]!
                    : (SolidColorBrush)System.Windows.Application.Current.Resources["TextSecondaryBrush"]!,
                Width = 20,
                TextAlignment = TextAlignment.Center
            });
            sp.Children.Add(new TextBlock
            {
                Text = loc[item.LabelKey],
                VerticalAlignment = VerticalAlignment.Center,
                FontFamily = ErpDesignTokens.UiFont,
                FontSize = 13
            });

            var btn = new Button
            {
                Style = style,
                Content = sp,
                HorizontalContentAlignment = HorizontalAlignment.Stretch
            };

            btn.Click += (_, _) =>
            {
                CloseAllDropdowns();
                NavigationRequested?.Invoke(this, new NavigationRequest(item.Module, item.SubPage));
            };

            return btn;
        }

        private void CloseAllDropdowns()
        {
            foreach (var (_, popup, _) in _navItems)
                if (popup != null)
                    popup.IsOpen = false;
            _overflowPopup?.SetCurrentValue(Popup.IsOpenProperty, false);
        }

        public void SetActiveModule(AppModule module)
        {
            _activeModule = module;
            UpdateActiveStates();
        }

        private void UpdateActiveStates()
        {
            foreach (var (btn, _, mod) in _navItems)
                ApplyActiveVisual(btn, mod == _activeModule);
        }

        private static void ApplyActiveVisual(Button btn, bool isActive)
        {
            var activeText = (SolidColorBrush)System.Windows.Application.Current.Resources["NavBarActiveTextBrush"]!;
            var mutedText = (SolidColorBrush)System.Windows.Application.Current.Resources["NavBarTextBrush"]!;

            btn.Foreground = isActive ? activeText : mutedText;
            btn.FontWeight = isActive ? FontWeights.SemiBold : FontWeights.Normal;
            btn.ApplyTemplate();

            if (btn.Template.FindName("BgLayer", btn) is Border bg)
                bg.Background = isActive
                    ? (Brush)System.Windows.Application.Current.Resources["NavBarActiveBrush"]!
                    : Brushes.Transparent;

            if (btn.Template.FindName("ActiveBar", btn) is Border bar)
                bar.Visibility = isActive ? Visibility.Visible : Visibility.Collapsed;
        }

        private void UpdateOverflow()
        {
            if (ActualWidth <= 0 || _menuDefs.Count == 0) return;

            const double minCell = 76;
            const double overflowWidth = 72;
            var available = ActualWidth - overflowWidth;
            var needsOverflow = available < _menuDefs.Count * minCell;

            BtnOverflow.Visibility = needsOverflow ? Visibility.Visible : Visibility.Collapsed;

            var visibleCount = needsOverflow
                ? Math.Max(4, Math.Min(_menuDefs.Count, (int)(available / minCell)))
                : _menuDefs.Count;

            if (NavItemsHost.Children.Count != visibleCount)
                RenderNavItems(visibleCount);

            if (needsOverflow)
                BuildOverflowMenu(_menuDefs.Skip(visibleCount).ToList());
        }

        private void BuildOverflowMenu(List<MenuDef> overflowItems)
        {
            BtnOverflow.Click -= BtnOverflow_Click;
            BtnOverflow.Click += BtnOverflow_Click;

            var stack = new StackPanel { MinWidth = 200 };
            foreach (var def in overflowItems)
            {
                var label = LocalizationManager.Instance[def.LabelKey];
                var btn = new Button
                {
                    Content = label,
                    Style = (Style)System.Windows.Application.Current.Resources["DropdownSubItemStyle"]!,
                    HorizontalContentAlignment = HorizontalAlignment.Stretch
                };
                var captured = def;
                btn.Click += (_, _) =>
                {
                    CloseAllDropdowns();
                    if (captured.Direct || captured.Sections == null)
                        NavigationRequested?.Invoke(this, new NavigationRequest(captured.Module));
                    else
                    {
                        var first = captured.Sections[0].Items.FirstOrDefault();
                        if (first != null)
                            NavigationRequested?.Invoke(this, new NavigationRequest(first.Module, first.SubPage));
                    }
                    _overflowPopup?.SetCurrentValue(Popup.IsOpenProperty, false);
                };
                stack.Children.Add(btn);
            }

            _overflowPopup = new Popup
            {
                PlacementTarget = BtnOverflow,
                Placement = PlacementMode.Bottom,
                StaysOpen = false,
                AllowsTransparency = true,
                Child = new Border
                {
                    Style = (Style)System.Windows.Application.Current.Resources["DropdownCardStyle"]!,
                    CornerRadius = new CornerRadius(8),
                    Child = stack
                },
                FlowDirection = LocalizationManager.Instance.FlowDir
            };
        }

        private void BtnOverflow_Click(object sender, RoutedEventArgs e)
        {
            if (_overflowPopup == null) return;
            CloseAllDropdowns();
            _overflowPopup.IsOpen = !_overflowPopup.IsOpen;
        }

        private static IReadOnlyList<string> GetCurrentPermissionCodes()
        {
            if (!AppServices.IsInitialized)
                return Array.Empty<string>();

            return AppServices.GetRequiredService<ICurrentUserService>() is WpfCurrentUserService wpf
                ? wpf.Permissions
                : Array.Empty<string>();
        }
    }
}
