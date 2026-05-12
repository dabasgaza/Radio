// ═══════════════════════════════════════════════════════════════════════════
// Converters.cs — محولات القيم المطلوبة للمرحلة الأولى
// ═══════════════════════════════════════════════════════════════════════════
// المحولات:
//   1. BoolToVisibilityConverter — إخفاء الأزرار المعطلة (Visibility بدلاً من IsEnabled)
//   2. NullToCollapsedConverter — إخفاء عناصر لا تحتوي بيانات
// ═══════════════════════════════════════════════════════════════════════════

using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Radio.Converter
{
    /// <summary>
    /// يحول قيمة bool إلى Visibility.
    /// true → Visible, false → Collapsed
    /// يُستخدم لإخفاء الأزرار المعطلة بدلاً من عرضها بحالة Disabled.
    /// الاستخدام: Visibility="{Binding CanMarkExecuted, Converter={StaticResource BoolToVisConverter}}"
    /// </summary>
    public class BoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
                return boolValue ? Visibility.Visible : Visibility.Collapsed;
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Visibility visibility)
                return visibility == Visibility.Visible;
            return false;
        }
    }

   
}
