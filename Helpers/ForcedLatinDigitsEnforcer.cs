using System.Globalization;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Threading;
using System.Runtime.CompilerServices;

namespace ERPSystem.Helpers;

internal static class ForcedLatinDigitsEnforcer
{
    private static readonly XmlLanguage LatinLang = XmlLanguage.GetLanguage("en-US");
    private static readonly CultureInfo LatinCulture = CultureInfo.GetCultureInfo("en-US");
    private static readonly ConditionalWeakTable<DependencyObject, object> Tracked = new();
    private static readonly ConditionalWeakTable<Window, DispatcherTimer> WindowTimers = new();
    private const int DebounceMs = 200;

    public static void Enable()
    {
        EventManager.RegisterClassHandler(typeof(Window), FrameworkElement.LoadedEvent, new RoutedEventHandler(OnWindowLoaded));
    }

    private static void OnWindowLoaded(object? sender, RoutedEventArgs e)
    {
        if (sender is not Window window) return;
        if (Tracked.TryGetValue(window, out _))
            return;

        Tracked.Add(window, null!);

        try
        {
            // force language on window
            window.Language = LatinLang;

            // immediate normalization once
            NormalizeVisualTree(window);

            // create a debounce timer for layout updates
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

            // attach LayoutUpdated to restart debounce timer when UI changes
            void OnLayoutUpdated(object? s, EventArgs args)
            {
                try
                {
                    if (!WindowTimers.TryGetValue(window, out var t))
                        return;
                    // restart debounce
                    t.Stop();
                    t.Start();
                }
                catch
                {
                    // ignore
                }
            }

            window.LayoutUpdated += OnLayoutUpdated;

            // cleanup when window is closed
            void OnClosed(object? s, EventArgs args)
            {
                try
                {
                    window.LayoutUpdated -= OnLayoutUpdated;
                    window.Closed -= OnClosed;
                    if (WindowTimers.TryGetValue(window, out var t))
                    {
                        t.Stop();
                    }
                }
                catch { }
            }

            window.Closed += OnClosed;
        }
        catch
        {
            // best-effort only
        }
    }

    private static void NormalizeVisualTree(DependencyObject root)
    {
        if (root is null) return;

        // Set number substitution and language on the element itself if applicable
        if (root is FrameworkElement fe)
        {
            try
            {
                fe.Language = LatinLang;
                fe.SetValue(NumberSubstitution.CultureOverrideProperty, LatinCulture);
                fe.SetValue(NumberSubstitution.SubstitutionProperty, NumberSubstitutionMethod.European);
                fe.SetValue(NumberSubstitution.CultureSourceProperty, NumberCultureSource.Override);
            }
            catch { }
        }

        // Normalize textual children
        switch (root)
        {
            case TextBlock tb:
                try { tb.Text = ERPSystem.Application.Common.LatinDigits.Normalize(tb.Text); } catch { }
                break;
            case Label lbl:
                if (lbl.Content is string s)
                {
                    try { lbl.Content = ERPSystem.Application.Common.LatinDigits.Normalize(s); } catch { }
                }
                break;
            case TextBox textbox:
                try { textbox.Text = ERPSystem.Application.Common.LatinDigits.Normalize(textbox.Text); } catch { }
                break;
            case ContentPresenter cp:
                if (cp.Content is string cs)
                    try { cp.Content = ERPSystem.Application.Common.LatinDigits.Normalize(cs); } catch { }
                break;
            case Run run:
                try { run.Text = ERPSystem.Application.Common.LatinDigits.Normalize(run.Text); } catch { }
                break;
        }

        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
        {
            var child = VisualTreeHelper.GetChild(root, i);
            NormalizeVisualTree(child);
        }
    }
}
