using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace VSL.UI.Converters;

public sealed class NavKeyToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var selected = value as string;
        var expected = parameter as string;
        if (string.IsNullOrWhiteSpace(expected))
        {
            return Visibility.Collapsed;
        }

        return string.Equals(selected, expected, StringComparison.OrdinalIgnoreCase)
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
