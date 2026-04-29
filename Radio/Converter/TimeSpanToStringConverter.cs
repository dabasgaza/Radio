using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Radio.Converter
{
    public class TimeSpanToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is TimeSpan ts)
            {
                // نحوّل TimeSpan إلى DateTime لنتمكن من استخدام صيغة AM/PM
                var dt = DateTime.Today.Add(ts);
                return dt.ToString("hh:mm tt", new CultureInfo("ar-SA"));
            }
            return DependencyProperty.UnsetValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}
