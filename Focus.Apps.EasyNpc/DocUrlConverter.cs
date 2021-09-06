using System;
using System.Globalization;
using System.Windows.Data;

namespace Focus.Apps.EasyNpc
{
    public sealed class DocUrlConverter : IValueConverter
    {
        public string BaseUrl { get; set; } = string.Empty;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is string pageName ? $"{BaseUrl}{pageName}.md" : string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is string url ? url[(url.LastIndexOf('/') + 1)..^3] : string.Empty;
        }
    }
}
