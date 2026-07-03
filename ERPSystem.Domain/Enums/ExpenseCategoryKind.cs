namespace ERPSystem.Domain.Enums;

/// <summary>Top-level expense classification — extensible via expense_categories without logic changes.</summary>
public enum ExpenseCategoryKind
{
    Capital = 1,
    Personal = 2,
    Operating = 3
}
