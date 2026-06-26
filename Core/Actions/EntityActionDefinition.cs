namespace ERPSystem.Core.Actions
{
    public sealed class EntityActionDefinition
    {
        public EntityActionId Id { get; init; }
        public string LabelAr { get; init; } = "";
        public string IconGlyph { get; init; } = "\uE8A5";
        public string? GroupAr { get; init; }
        public bool IsDestructive { get; init; }
        public bool RequiresConfirmation { get; init; }

        public EntityActionDefinition(
            EntityActionId id,
            string labelAr,
            string icon,
            string? groupAr = null,
            bool destructive = false,
            bool requiresConfirmation = false)
        {
            Id = id;
            LabelAr = labelAr;
            IconGlyph = icon;
            GroupAr = groupAr;
            IsDestructive = destructive;
            RequiresConfirmation = requiresConfirmation || destructive;
        }
    }
}
