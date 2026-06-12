using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace Radio.Converter
{
    public class PlatformColorConverter : IValueConverter
    {
        private SolidColorBrush GetBrush(string hex)
        {
            var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));
            brush.Freeze();
            return brush;
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string platform = value?.ToString()?.Trim().ToLower(CultureInfo.InvariantCulture) ?? string.Empty;
            bool isBackground = parameter?.ToString() == "Background";

            if (platform.Contains("facebook") || platform.Contains("فيس") || platform.Contains("فيسبوك"))
            {
                return isBackground ? GetBrush("#E8F0FE") : GetBrush("#1877F2");
            }
            if (platform.Contains("twitter") || platform.Contains("تويتر") || platform == "x" || platform == "إكس" || platform == "اكس")
            {
                bool isX = platform == "x" || platform == "إكس" || platform == "اكس";
                if (isX)
                {
                    return isBackground ? GetBrush("#F1F1F1") : GetBrush("#0F1419");
                }
                return isBackground ? GetBrush("#E8F5FE") : GetBrush("#1DA1F2");
            }
            if (platform.Contains("youtube") || platform.Contains("يوتيوب"))
            {
                return isBackground ? GetBrush("#FFEBEE") : GetBrush("#FF0000");
            }
            if (platform.Contains("instagram") || platform.Contains("انستغرام") || platform.Contains("إنستغرام") || platform.Contains("انستجرام") || platform.Contains("انستقرام") || platform.Contains("انستا"))
            {
                return isBackground ? GetBrush("#FDF2F8") : GetBrush("#E1306C");
            }
            if (platform.Contains("tiktok") || platform.Contains("تيك"))
            {
                return isBackground ? GetBrush("#F3F4F6") : GetBrush("#010101");
            }
            if (platform.Contains("whatsapp") || platform.Contains("واتس") || platform.Contains("واتساب"))
            {
                return isBackground ? GetBrush("#E8F8EF") : GetBrush("#25D366");
            }
            if (platform.Contains("telegram") || platform.Contains("تليجرام") || platform.Contains("تيليجرام") || platform.Contains("تليغرام") || platform.Contains("تيليغرام"))
            {
                return isBackground ? GetBrush("#E8F6FD") : GetBrush("#26A5E4");
            }
            if (platform.Contains("linkedin") || platform.Contains("لينكد"))
            {
                return isBackground ? GetBrush("#E6F0FA") : GetBrush("#0A66C2");
            }
            if (platform.Contains("snapchat") || platform.Contains("سناب"))
            {
                return isBackground ? GetBrush("#FFFEE5") : GetBrush("#FFCC00");
            }

            // Default to Theme's PrimaryMain/PrimaryXLight
            return isBackground ? GetBrush("#EEF2F6") : GetBrush("#6366F1");
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
