using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace ERPSystem.Helpers;

/// <summary>Wires Enter key to move focus to the next field (Tab-like flow for fast data entry).</summary>
public static class EnterFocusNavigation
{
    public static void WireChain(Action? onLastEnter = null, params UIElement?[] elements) =>
        WireChain(elements.Where(e => e is not null).Cast<UIElement>().ToList(), onLastEnter);

    public static void WireChain(IReadOnlyList<UIElement> elements, Action? onLastEnter = null)
    {
        for (var i = 0; i < elements.Count; i++)
        {
            var next = i + 1 < elements.Count ? elements[i + 1] : null;
            var isLast = next is null;
            Attach(elements[i], next, isLast ? onLastEnter : null);
        }
    }

    public static void WireEnterToNext(FrameworkElement element, UIElement? next, Action? onLastEnter = null) =>
        Attach(element, next, onLastEnter);

    public static void FocusNext(UIElement? next)
    {
        if (next is null)
            return;

        if (next is TextBox textBox)
        {
            textBox.Focus();
            textBox.SelectAll();
            return;
        }

        if (next is ComboBox comboBox)
        {
            comboBox.Focus();
            return;
        }

        if (next is DatePicker datePicker)
        {
            if (FindVisualChild<TextBox>(datePicker) is { } inner)
            {
                inner.Focus();
                inner.SelectAll();
            }
            else
            {
                datePicker.Focus();
            }

            return;
        }

        next.Focus();
    }

    private static void Attach(UIElement element, UIElement? next, Action? onLastEnter)
    {
        if (element is ComboBox comboBox)
        {
            comboBox.PreviewKeyDown += (_, e) =>
            {
                if (e.Key != Key.Enter || comboBox.IsDropDownOpen)
                    return;

                e.Handled = true;
                Advance(next, onLastEnter);
            };
            return;
        }

        if (element is DatePicker datePicker)
        {
            void WireDateInner()
            {
                if (FindVisualChild<TextBox>(datePicker) is not { } inner)
                    return;

                inner.PreviewKeyDown -= DateInnerOnPreviewKeyDown;
                inner.PreviewKeyDown += DateInnerOnPreviewKeyDown;
            }

            void DateInnerOnPreviewKeyDown(object sender, KeyEventArgs e)
            {
                if (e.Key != Key.Enter)
                    return;

                e.Handled = true;
                Advance(next, onLastEnter);
            }

            if (datePicker.IsLoaded)
                WireDateInner();
            else
                datePicker.Loaded += (_, _) => WireDateInner();

            return;
        }

        element.PreviewKeyDown += (_, e) =>
        {
            if (e.Key != Key.Enter)
                return;

            e.Handled = true;
            Advance(next, onLastEnter);
        };
    }

    private static void Advance(UIElement? next, Action? onLastEnter)
    {
        if (next is null)
            onLastEnter?.Invoke();
        else
            FocusNext(next);
    }

    private static T? FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
    {
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is T match)
                return match;

            var nested = FindVisualChild<T>(child);
            if (nested is not null)
                return nested;
        }

        return null;
    }
}
