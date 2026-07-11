using System.Windows;
using System.Windows.Media;

namespace ERPSystem.Controls.OperationsCenter
{
    public sealed class OperationsCenterSpec
    {
        public string Title { get; init; } = "";
        public string Subtitle { get; init; } = "";
        public string Breadcrumb { get; init; } = "";
        public string IconGlyph { get; init; } = "\uE8A5";
        public Brush Accent { get; init; } = Brushes.DodgerBlue;
        public Brush AccentLight { get; init; } = Brushes.AliceBlue;
        public string StatusBadge { get; init; } = "";
        public Brush? StatusBadgeForeground { get; init; }
        public Brush? StatusBadgeBackground { get; init; }
        public IReadOnlyList<(string Label, string Value)> HeaderFields { get; init; } = [];
        public IReadOnlyList<(string Title, string Value, string Icon)> Kpis { get; init; } = [];
        public IReadOnlyList<(string Label, bool Active, bool Done)>? Workflow { get; init; }
        public IReadOnlyList<OperationsCenterTab> Tabs { get; init; } = [];
        public IReadOnlyList<OperationsCenterQuickAction> QuickActions { get; init; } = [];
        public int InitialTabIndex { get; init; }
        public ERPSystem.Services.OperationsCenterContext? Context { get; init; }
    }

    public sealed class OperationsCenterTab
    {
        public string Key { get; init; } = "";
        public string Label { get; init; } = "";
        public Func<UIElement> ContentFactory { get; init; } = () => new UIElement();
    }

    public sealed class OperationsCenterQuickAction
    {
        public string Label { get; init; } = "";
        public bool Primary { get; init; }
        public string? TabKey { get; init; }
        public bool Destructive { get; init; }
        public bool RequiresConfirmation { get; init; }
        public string? ActionKey { get; init; }
    }
}
