using ERPSystem.Core;
using ERPSystem.Core.Navigation;
using ERPSystem.Views;
using System.Windows;
using System.Windows.Controls;

namespace ERPSystem.Controls
{
    public partial class ModuleShellControl : UserControl, ISubpageNavigator
    {
        public static readonly DependencyProperty ModuleProperty =
            DependencyProperty.Register(nameof(Module), typeof(AppModule), typeof(ModuleShellControl),
                new PropertyMetadata(AppModule.Dashboard, OnModuleChanged));

        private string _activeKey = "";
        private readonly Dictionary<string, (UserControl View, DateTime CachedAt)> _pageCache = [];
        private readonly LinkedList<string> _cacheOrder = [];
        private static readonly TimeSpan CacheTtl = TimeSpan.FromSeconds(45);

        public AppModule Module
        {
            get => (AppModule)GetValue(ModuleProperty);
            set => SetValue(ModuleProperty, value);
        }

        public ModuleShellControl()
        {
            InitializeComponent();
        }

        private static void OnModuleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ModuleShellControl shell)
                shell.OnModuleSet();
        }

        private void OnModuleSet()
        {
            if (string.IsNullOrEmpty(_activeKey))
                SelectSubpage(SubmoduleRegistry.Get(Module).FirstOrDefault()?.Key);
            else
                SelectSubpage(_activeKey);
        }

        public void SelectSubpage(string? key)
        {
            var resolved = SubmoduleRegistry.ResolveKey(Module, key);
            if (_activeKey == resolved && ContentHost.Content != null)
            {
                UpdatePageHeader(resolved);
                return;
            }

            if (!UnsavedWorkGuard.TryConfirmLeave())
                return;

            _activeKey = resolved;
            UpdatePageHeader(resolved);
            ContentHost.Content = GetOrCreatePage(_activeKey);
        }

        private UserControl GetOrCreatePage(string key)
        {
            // Financial, accounting, inventory and party screens must always show live balances.
            var cacheAllowed = Module is AppModule.HR or AppModule.Settings;
            if (!cacheAllowed)
                return SubmoduleViewFactory.Create(Module, key);

            if (_pageCache.TryGetValue(key, out var cached) && DateTime.UtcNow - cached.CachedAt <= CacheTtl)
            {
                _cacheOrder.Remove(key);
                _cacheOrder.AddLast(key);
                return cached.View;
            }

            _pageCache.Remove(key);
            _cacheOrder.Remove(key);
            var view = SubmoduleViewFactory.Create(Module, key);
            _pageCache[key] = (view, DateTime.UtcNow);
            _cacheOrder.AddLast(key);
            while (_cacheOrder.Count > 5)
            {
                var oldest = _cacheOrder.First!.Value;
                _cacheOrder.RemoveFirst();
                _pageCache.Remove(oldest);
            }
            return view;
        }

        public void NavigateSubpage(string? subPage) => SelectSubpage(subPage);

        private void UpdatePageHeader(string key)
        {
            var sub = NavigationCatalog.GetActiveSub(Module, key);
            var modKey = NavigationCatalog.GetModuleLabelKey(Module);
            var modLabel = LocalizationManager.Instance[modKey];
            TxtBreadcrumb.Text = sub != null ? $"{modLabel} › {sub.LabelAr}" : modLabel;
            TxtPageTitle.Text = sub?.LabelAr ?? modLabel;
        }
    }
}
