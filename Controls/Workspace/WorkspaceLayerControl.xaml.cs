using ERPSystem.Core.Workspace;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ERPSystem.Controls.Workspace
{
    public partial class WorkspaceLayerControl : UserControl
    {
        private readonly WorkspaceWindowManager _manager = WorkspaceWindowManager.Instance;

        public WorkspaceLayerControl()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            _manager.PropertyChanged += (_, args) =>
            {
                if (args.PropertyName is nameof(WorkspaceWindowManager.HasOpenWorkspaces)
                    or nameof(WorkspaceWindowManager.ActiveWorkspace)
                    or nameof(WorkspaceWindowManager.OpenWorkspaces))
                    Refresh();
            };

            _manager.WorkspaceOpened += (_, _) => Refresh();
            _manager.WorkspaceClosed += (_, _) => Refresh();
            Refresh();
        }

        private void Refresh()
        {
            Visibility = _manager.HasOpenWorkspaces ? Visibility.Visible : Visibility.Collapsed;
            TabItems.ItemsSource = null;
            TabItems.ItemsSource = _manager.OpenWorkspaces;
            ActiveContentHost.Content = _manager.ActiveWorkspace?.Content;
        }

        private void Tab_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is WorkspaceInstance ws)
                _manager.Activate(ws);
        }

        private void CloseTab_Click(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
            if (sender is Button btn && btn.Tag is WorkspaceInstance ws)
                _manager.Close(ws);
        }

        private void CloseAll_Click(object sender, RoutedEventArgs e) =>
            _manager.CloseAll();

        private void Backdrop_MouseDown(object sender, MouseButtonEventArgs e)
        {
            // Click outside minimizes focus — don't close on backdrop click
        }
    }
}
