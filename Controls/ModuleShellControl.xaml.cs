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
            ContentHost.Content = SubmoduleViewFactory.Create(Module, _activeKey);
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
