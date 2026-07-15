using System.Globalization;
using System.Windows.Data;
using ERPSystem.Application.Common;

namespace ERPSystem.Helpers;

/// <summary>
/// Binding converter that always renders numbers and dates with Western digits (0-9).
/// </summary>
public sealed class LatinDisplayValueConverter : IValueConverter
{
    public static LatinDisplayValueConverter Instance { get; } = new();

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        LatinDigits.Format(value, parameter as string);

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        Binding.DoNothing;
}
