using System.Globalization;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Markup;
using System.Windows.Media;

namespace ERPSystem.Helpers;

internal static class ForcedLatinDigitsEnforcer
{
    private static readonly XmlLanguage LatinLang = XmlLanguage.GetLanguage("en-US");
    private static readonly CultureInfo LatinCulture = CultureInfo.GetCultureInfo("en-US");

    public static void Enable()
    {
        EventManager.RegisterClassHandler(typeof(Window), FrameworkElement.LoadedEvent, new RoutedEventHandler(OnWindowLoaded));
    }

    private static void OnWindowLoaded(object? sender, RoutedEventArgs e)
    {
        if (sender is not Window window) return;
        try
        {
            // force language on window
            window.Language = LatinLang;
            // traverse visual tree and normalize
            NormalizeVisualTree(window);
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
            fe.Language = LatinLang;
            fe.SetValue(NumberSubstitution.CultureOverrideProperty, LatinCulture);
            fe.SetValue(NumberSubstitution.SubstitutionProperty, NumberSubstitutionMethod.European);
            fe.SetValue(NumberSubstitution.CultureSourceProperty, NumberCultureSource.Override);
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
