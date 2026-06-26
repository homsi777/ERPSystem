using ERPSystem.Core.Workspace;
using ERPSystem.Views.OperationsCenters;
using System.Windows;

namespace ERPSystem.Views.Workspaces
{
    public static class EntityWorkspaceContentFactory
    {
        public static UIElement? TryBuild(WorkspaceOpenRequest request) =>
            OperationsCenterFactory.TryBuild(request);
    }
}
