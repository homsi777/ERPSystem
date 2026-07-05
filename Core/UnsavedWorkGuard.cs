using System.Windows;

namespace ERPSystem.Core;

/// <summary>Blocks navigation / app exit when forms hold unsaved user input.</summary>
public static class UnsavedWorkGuard
{
    private sealed class Entry(string Title, Func<bool> IsDirty)
    {
        public string Title { get; } = Title;
        public Func<bool> IsDirty { get; } = IsDirty;
    }

    private static readonly Dictionary<object, Entry> Entries = new();

    public static void Register(object owner, string title, Func<bool> isDirty) =>
        Entries[owner] = new Entry(title, isDirty);

    public static void Unregister(object owner) => Entries.Remove(owner);

    public static bool HasDirtyWork => Entries.Values.Any(e => e.IsDirty());

    public static bool TryConfirmLeave()
    {
        var titles = Entries.Values.Where(e => e.IsDirty()).Select(e => e.Title).Distinct().ToList();
        if (titles.Count == 0)
            return true;

        var list = string.Join("\n", titles.Select(t => $"• {t}"));
        var message =
            $"لديك بيانات غير محفوظة:\n{list}\n\n" +
            "إذا غادرت الآن قد تفقد ما أدخلته.\n" +
            "هل تريد المغادرة بدون حفظ؟";

        return MessageBox.Show(
                   message,
                   "تنبيه — بيانات غير محفوظة",
                   MessageBoxButton.YesNo,
                   MessageBoxImage.Warning,
                   MessageBoxResult.No) == MessageBoxResult.Yes;
    }
}
