using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace Radio.Converter
{
    public class PlatformColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string platform = value?.ToString()?.ToLower() ?? string.Empty;
            bool isBackground = parameter?.ToString() == "Background";

            switch (platform)
            {
                case "facebook":
                    return isBackground 
                        ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E8F0FE"))
                        : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1877F2"));
                case "twitter":
                case "x":
                    return isBackground 
                        ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E8F5FE"))
                        : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1DA1F2"));
                case "youtube":
                    return isBackground 
                        ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFEBEE"))
                        : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF0000"));
                case "instagram":
                    return isBackground 
                        ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FDF2F8"))
                        : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E1306C"));
                case "tiktok":
                    return isBackground 
                        ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F3F4F6"))
                        : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#010101"));
                default:
                    // Default to Theme's PrimaryMain/PrimaryXLight
                    return isBackground 
                        ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EEF2F6"))
                        : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#6366F1"));
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
