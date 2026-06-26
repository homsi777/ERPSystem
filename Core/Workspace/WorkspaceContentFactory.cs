using ERPSystem.Controls.Workspace;
using ERPSystem.Core.Actions;

namespace ERPSystem.Core.Workspace
{
    public static class WorkspaceContentFactory
    {
        public static ActionWorkspaceView Create(WorkspaceOpenRequest request) =>
            new(request);
    }
}
