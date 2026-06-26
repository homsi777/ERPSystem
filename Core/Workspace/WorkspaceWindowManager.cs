using ERPSystem.Core.Actions;
using ERPSystem.ViewModels.Base;
using System.Collections.ObjectModel;

namespace ERPSystem.Core.Workspace
{
    /// <summary>
    /// Manages independent internal workspace screens opened from contextual actions.
    /// Supports multiple concurrent workspaces with tab switching.
    /// </summary>
    public sealed class WorkspaceWindowManager : ViewModelBase
    {
        private static WorkspaceWindowManager? _instance;
        public static WorkspaceWindowManager Instance => _instance ??= new WorkspaceWindowManager();

        public ObservableCollection<WorkspaceInstance> OpenWorkspaces { get; } = new();

        private WorkspaceInstance? _activeWorkspace;
        public WorkspaceInstance? ActiveWorkspace
        {
            get => _activeWorkspace;
            set
            {
                if (SetProperty(ref _activeWorkspace, value))
                    OnPropertyChanged(nameof(HasOpenWorkspaces));
            }
        }

        public bool HasOpenWorkspaces => OpenWorkspaces.Count > 0;

        public event EventHandler<WorkspaceInstance>? WorkspaceOpened;
        public event EventHandler<WorkspaceInstance>? WorkspaceClosed;

        public WorkspaceInstance Open(WorkspaceOpenRequest request)
        {
            var existing = OpenWorkspaces.FirstOrDefault(w =>
                w.ActionId == request.ActionId &&
                w.EntityKey == request.EntityKey);

            if (existing != null)
            {
                ActiveWorkspace = existing;
                return existing;
            }

            var workspace = new WorkspaceInstance
            {
                Title = string.IsNullOrWhiteSpace(request.Title)
                    ? EntityActionRegistry.GetActionTitle(request.ActionId, request.EntityDisplayName)
                    : request.Title,
                ActionId = request.ActionId,
                EntityType = request.EntityType,
                EntityRow = request.EntityRow,
                EntityKey = request.EntityKey,
                SourceModule = request.SourceModule,
                Content = WorkspaceContentFactory.Create(request)
            };

            OpenWorkspaces.Add(workspace);
            ActiveWorkspace = workspace;
            OnPropertyChanged(nameof(HasOpenWorkspaces));
            WorkspaceOpened?.Invoke(this, workspace);
            return workspace;
        }

        public void OpenAction(EntityActionId actionId, EntityType entityType, object? row,
            AppModule sourceModule, string? titleOverride = null)
        {
            var displayName = EntityDisplayNameResolver.Resolve(row, entityType);
            var key = EntityDisplayNameResolver.ResolveKey(row, entityType);

            Open(new WorkspaceOpenRequest
            {
                ActionId = actionId,
                EntityType = entityType,
                EntityRow = row,
                EntityKey = key,
                EntityDisplayName = displayName,
                Title = titleOverride ?? EntityActionRegistry.GetActionTitle(actionId, displayName),
                SourceModule = sourceModule
            });
        }

        public void Activate(WorkspaceInstance workspace)
        {
            if (OpenWorkspaces.Contains(workspace))
                ActiveWorkspace = workspace;
        }

        public void Close(WorkspaceInstance workspace)
        {
            if (!OpenWorkspaces.Contains(workspace)) return;

            var index = OpenWorkspaces.IndexOf(workspace);
            OpenWorkspaces.Remove(workspace);
            WorkspaceClosed?.Invoke(this, workspace);

            if (ActiveWorkspace == workspace)
            {
                if (OpenWorkspaces.Count == 0)
                    ActiveWorkspace = null;
                else
                    ActiveWorkspace = OpenWorkspaces[Math.Min(index, OpenWorkspaces.Count - 1)];
            }

            OnPropertyChanged(nameof(HasOpenWorkspaces));
        }

        public void CloseAll()
        {
            while (OpenWorkspaces.Count > 0)
                Close(OpenWorkspaces[0]);
        }
    }
}
