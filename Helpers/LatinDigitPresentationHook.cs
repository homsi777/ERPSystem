using ERPSystem.Application.Common;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Media;

namespace ERPSystem.Helpers;

/// <summary>
/// Last-resort safety net: normalize any rendered text to Western digits (0-9).
/// </summary>
public static class LatinDigitPresentationHook
{
    private static readonly ConditionalWeakTable<DependencyObject, object> Tracked = new();

    public static void EnableApplicationWide()
    {
        Register<TextBlock>(TextBlock.TextProperty, static tb => tb.Text, static (tb, v) => tb.Text = v);
        Register<Label>(Label.ContentProperty, static l => l.Content as string, static (l, v) => l.Content = v);
        Register<DatePickerTextBox>(TextBox.TextProperty, static tb => tb.Text, static (tb, v) => tb.Text = v);
        Register<TextBox>(
            TextBox.TextProperty,
            static tb => tb.IsReadOnly ? tb.Text : null,
            static (tb, v) => tb.Text = v);

        EventManager.RegisterClassHandler(
            typeof(DataGrid),
            FrameworkElement.LoadedEvent,
            new RoutedEventHandler(OnDataGridLoaded),
            true);
    }

    private static void OnDataGridLoaded(object sender, RoutedEventArgs e)
    {
        if (sender is not DataGrid grid || Tracked.TryGetValue(grid, out var _))
            return;

        Tracked.Add(grid, null!);
        grid.LoadingRow += (_, args) => NormalizeVisualTree(args.Row);
    }

    private static void Register<T>(
        DependencyProperty property,
        Func<T, string?> getText,
        Action<T, string> setText)
        where T : DependencyObject
    {
        EventManager.RegisterClassHandler(
            typeof(T),
            FrameworkElement.LoadedEvent,
            new RoutedEventHandler((sender, e) =>
            {
                if (sender is not T element || Tracked.TryGetValue(element, out var _))
                    return;

                Tracked.Add(element, null!);

                void Apply()
                {
                    var current = getText(element);
                    if (current is null)
                        return;

                    var normalized = LatinDigits.Normalize(current);
                    if (normalized == current)
                        return;

                    setText(element, normalized);
                }

                var descriptor = DependencyPropertyDescriptor.FromProperty(property, typeof(T));
                descriptor?.AddValueChanged(element, (_, _) => Apply());
                Apply();
            }),
            true);
    }

    private static void NormalizeVisualTree(DependencyObject root)
    {
        if (root is TextBlock tb)
            TryNormalize(tb.Text, v => tb.Text = v);

        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
            NormalizeVisualTree(VisualTreeHelper.GetChild(root, i));
    }

    private static void TryNormalize(string? current, Action<string> apply)
    {
        if (string.IsNullOrEmpty(current))
            return;

        var normalized = LatinDigits.Normalize(current);
        if (normalized != current)
            apply(normalized);
    }
}
