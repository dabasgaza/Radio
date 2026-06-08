using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Radio.Resources.Converter;

/// <summary>
/// يحول قيمة bool إلى Visibility.
/// true → Visible, false → Collapsed
/// عند تمرير parameter="Invert" يعكس المنطق.
/// </summary>
[ValueConversion(typeof(bool), typeof(Visibility))]
public class BooleanToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter,
        CultureInfo culture)
    {
        if (value is null)
            return Visibility.Collapsed;

        bool boolValue = System.Convert.ToBoolean(value);
        bool invert = parameter is string s && s.Equals("Invert", StringComparison.OrdinalIgnoreCase);

        if (invert)
            boolValue = !boolValue;

        return boolValue ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter,
        CultureInfo culture)
    {
        if (value is Visibility visibility)
        {
            bool invert = parameter is string s && s.Equals("Invert", StringComparison.OrdinalIgnoreCase);
            bool result = visibility == Visibility.Visible;
            return invert ? !result : result;
        }

        return false;
    }
}
