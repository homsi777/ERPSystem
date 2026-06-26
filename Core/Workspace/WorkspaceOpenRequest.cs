using ERPSystem.Core.Actions;

namespace ERPSystem.Core.Workspace
{
    public sealed class WorkspaceOpenRequest
    {
        public EntityActionId ActionId { get; init; }
        public EntityType EntityType { get; init; }
        public object? EntityRow { get; init; }
        public string EntityKey { get; init; } = "";
        public string EntityDisplayName { get; init; } = "";
        public string Title { get; init; } = "";
        public AppModule SourceModule { get; init; }
    }
}
