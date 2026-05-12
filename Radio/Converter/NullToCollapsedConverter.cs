using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Radio.Converter
{
    /// <summary>
    /// يحول null أو string فارغ إلى Collapsed، وغيره إلى Visible.
    /// يُستخدم لإخفاء عناصر مثل أيقونة "ملاحظات" عندما لا توجد ملاحظات.
    /// الاستخدام: Visibility="{Binding SpecialNotes, Converter={StaticResource NullToCollapsedConverter}}"
    /// </summary>
    public class NullToCollapsedConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return string.IsNullOrWhiteSpace(value?.ToString()) ? Visibility.Collapsed : Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
