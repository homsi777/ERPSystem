using ERPSystem.Core.Actions;
using ERPSystem.ViewModels.Base;

namespace ERPSystem.Core.Workspace
{
    public sealed class WorkspaceInstance : ViewModelBase
    {
        public string Id { get; } = Guid.NewGuid().ToString("N");

        private string _title = "";
        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }

        public EntityActionId ActionId { get; init; }
        public EntityType EntityType { get; init; }
        public object? EntityRow { get; init; }
        public string EntityKey { get; init; } = "";
        public AppModule SourceModule { get; init; }

        private object? _content;
        public object? Content
        {
            get => _content;
            set => SetProperty(ref _content, value);
        }
    }
}
