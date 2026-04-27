using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Radio.Converter
{
    public class PermissionVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is UserSession session && parameter is string permission)
                return session.HasPermission(permission)
                    ? Visibility.Visible
                    : Visibility.Collapsed;
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
