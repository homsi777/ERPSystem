using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Markup;
using ERPSystem.Core;

namespace ERPSystem.Helpers;

/// <summary>
/// Ensures WPF DatePicker text uses Latin digits regardless of Windows regional settings.
/// </summary>
public static class LatinDigitDatePickerHelper
{
    public static void Enable(DatePicker picker)
    {
        if (picker.GetValue(IsEnabledProperty) is true)
            return;

        picker.SetValue(IsEnabledProperty, true);
        picker.Language = XmlLanguage.GetLanguage("en-US");
        picker.Loaded += OnLoaded;
        picker.SelectedDateChanged += OnSelectedDateChanged;
        picker.CalendarClosed += OnCalendarClosed;

        if (picker.IsLoaded)
            RefreshDisplayText(picker);
    }

    public static readonly DependencyProperty IsEnabledProperty =
        DependencyProperty.RegisterAttached(
            "IsEnabled",
            typeof(bool),
            typeof(LatinDigitDatePickerHelper),
            new PropertyMetadata(false, OnIsEnabledChanged));

    public static void SetIsEnabled(DependencyObject element, bool value) =>
        element.SetValue(IsEnabledProperty, value);

    public static bool GetIsEnabled(DependencyObject element) =>
        (bool)element.GetValue(IsEnabledProperty);

    private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is DatePicker picker && e.NewValue is true)
            Enable(picker);
    }

    private static void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (sender is DatePicker picker)
            RefreshDisplayText(picker);
    }

    private static void OnSelectedDateChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (sender is DatePicker picker)
            RefreshDisplayText(picker);
    }

    private static void OnCalendarClosed(object? sender, RoutedEventArgs e)
    {
        if (sender is DatePicker picker)
            RefreshDisplayText(picker);
    }

    private static void RefreshDisplayText(DatePicker picker)
    {
        if (picker.SelectedDate is not DateTime date)
            return;

        picker.ApplyTemplate();
        if (picker.Template?.FindName("PART_TextBox", picker) is not DatePickerTextBox textBox)
            return;

        textBox.Text = AppFormats.Date(date);
        textBox.Language = XmlLanguage.GetLanguage("en-US");
    }
}
