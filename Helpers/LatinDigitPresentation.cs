using ERPSystem.Application.Common;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Threading;

namespace ERPSystem.Helpers;

/// <summary>
/// Single consolidated WPF presentation layer for Western digits (0-9).
/// Merges the former <c>LatinDigitPresentationHook</c> and <c>ForcedLatinDigitsEnforcer</c>.
/// </summary>
public static class LatinDigitPresentation
{
    private static readonly XmlLanguage LatinLang = XmlLanguage.GetLanguage("en-US");
    private static readonly CultureInfo LatinCulture = CultureInfo.GetCultureInfo("en-US");
    private static readonly ConditionalWeakTable<DependencyObject, object> TrackedElements = new();
    private static readonly ConditionalWeakTable<Window, DispatcherTimer> WindowTimers = new();
    private const int DebounceMs = 200;

    public static void Enable()
    {
        EventManager.RegisterClassHandler(typeof(Window), FrameworkElement.LoadedEvent, new RoutedEventHandler(OnWindowLoaded), true);

        RegisterTextProperty<TextBlock>(TextBlock.TextProperty, static tb => tb.Text, static (tb, v) => tb.Text = v);
        RegisterTextProperty<Label>(Label.ContentProperty, static l => l.Content as string, static (l, v) => l.Content = v);
        RegisterTextProperty<Button>(Button.ContentProperty, static b => b.Content as string, static (b, v) => b.Content = v);
        RegisterTextProperty<TextBox>(TextBox.TextProperty, static tb => tb.Text, static (tb, v) => tb.Text = v);
        RegisterTextProperty<DatePickerTextBox>(TextBox.TextProperty, static tb => tb.Text, static (tb, v) => tb.Text = v);
        RegisterTextProperty<Run>(Run.TextProperty, static r => r.Text, static (r, v) => r.Text = v);
        RegisterTextProperty<AccessText>(AccessText.TextProperty, static a => a.Text, static (a, v) => a.Text = v);

        EventManager.RegisterClassHandler(
            typeof(DataGrid),
            FrameworkElement.LoadedEvent,
            new RoutedEventHandler(OnDataGridLoaded),
            true);
    }

    public static void ApplyToElement(DependencyObject node)
    {
        if (node is FrameworkElement fe)
        {
            fe.Language = LatinLang;
            fe.SetValue(NumberSubstitution.CultureOverrideProperty, LatinCulture);
            fe.SetValue(NumberSubstitution.SubstitutionProperty, NumberSubstitutionMethod.European);
            fe.SetValue(NumberSubstitution.CultureSourceProperty, NumberCultureSource.Override);
        }

        NormalizeTextOnNode(node);
    }

    public static void NormalizeVisualTree(DependencyObject root)
    {
        ApplyToElement(root);
        var count = VisualTreeHelper.GetChildrenCount(root);
        for (var i = 0; i < count; i++)
            NormalizeVisualTree(VisualTreeHelper.GetChild(root, i));
    }

    private static void OnWindowLoaded(object? sender, RoutedEventArgs e)
    {
        if (sender is not Window window || TrackedElements.TryGetValue(window, out object? _))
            return;

        TrackedElements.Add(window, null!);
        window.Language = LatinLang;
        NormalizeVisualTree(window);

        var timer = new DispatcherTimer(DispatcherPriority.Background, window.Dispatcher)
        {
            Interval = TimeSpan.FromMilliseconds(DebounceMs)
        };
        timer.Tick += (_, _) =>
        {
            try
            {
                timer.Stop();
                NormalizeVisualTree(window);
            }
            catch
            {
                // best-effort
            }
        };

        WindowTimers.Add(window, timer);

        void OnLayoutUpdated(object? _, EventArgs __)
        {
            try
            {
                if (!WindowTimers.TryGetValue(window, out var t))
                    return;
                t.Stop();
                t.Start();
            }
            catch { }
        }

        window.LayoutUpdated += OnLayoutUpdated;
        window.Closed += (_, _) =>
        {
            window.LayoutUpdated -= OnLayoutUpdated;
            if (WindowTimers.TryGetValue(window, out var t))
                t.Stop();
        };
    }

    private static void OnDataGridLoaded(object sender, RoutedEventArgs e)
    {
        if (sender is not DataGrid grid || TrackedElements.TryGetValue(grid, out object? _))
            return;

        TrackedElements.Add(grid, null!);
        grid.LoadingRow += (_, args) => NormalizeVisualTree(args.Row);
    }

    private static void RegisterTextProperty<T>(
        DependencyProperty property,
        Func<T, string?> getText,
        Action<T, string> setText)
        where T : DependencyObject
    {
        EventManager.RegisterClassHandler(
            typeof(T),
            FrameworkElement.LoadedEvent,
            new RoutedEventHandler((sender, _) =>
            {
                if (sender is not T element || TrackedElements.TryGetValue(element, out object? _))
                    return;

                TrackedElements.Add(element, null!);
                ApplyToElement(element);

                void Apply()
                {
                    var current = getText(element);
                    if (current is null)
                        return;

                    var normalized = LatinDigits.Normalize(current);
                    if (normalized != current)
                        setText(element, normalized);
                }

                var descriptor = System.ComponentModel.DependencyPropertyDescriptor.FromProperty(property, typeof(T));
                descriptor?.AddValueChanged(element, (_, _) => Apply());
                Apply();
            }),
            true);
    }

    private static void NormalizeTextOnNode(DependencyObject node)
    {
        switch (node)
        {
            case TextBlock tb:
                TryNormalize(tb.Text, v => tb.Text = v);
                break;
            case Label lbl when lbl.Content is string s:
                TryNormalize(s, v => lbl.Content = v);
                break;
            case Button btn when btn.Content is string s:
                TryNormalize(s, v => btn.Content = v);
                break;
            case DatePickerTextBox dateBox:
                TryNormalize(dateBox.Text, v => dateBox.Text = v);
                break;
            case TextBox box:
                TryNormalize(box.Text, v => box.Text = v);
                break;
            case ContentPresenter cp when cp.Content is string s:
                TryNormalize(s, v => cp.Content = v);
                break;
            case Run run:
                TryNormalize(run.Text, v => run.Text = v);
                break;
            case AccessText access:
                TryNormalize(access.Text, v => access.Text = v);
                break;
        }
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
