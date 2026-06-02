using System;
using System.Globalization;
using System.Windows.Data;

namespace Radio.Converter
{
    public class FileSizeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is long bytes)
            {
                if (bytes <= 0) return "0 بابت";
                string[] suf = { "بايت", "كيلوبايت", "ميغابايت", "جيجابايت" };
                int place = System.Convert.ToInt32(Math.Floor(Math.Log(bytes, 1024)));
                double num = Math.Round(bytes / Math.Pow(1024, place), 1);
                return $"{num} {suf[place]}";
            }
            return "غير معروف";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
