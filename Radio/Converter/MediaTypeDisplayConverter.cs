using Domain.Models;
using System;
using System.Globalization;
using System.Windows.Data;

namespace Radio.Converter;

public class MediaTypeDisplayConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value switch
        {
            MediaType.Audio => "صوتي",
            MediaType.Video => "فيديو",
            MediaType.Both => "صوت وفيديو",
            _ => string.Empty
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return Binding.DoNothing;
    }
}
