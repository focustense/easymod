using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Focus.Apps.EasyNpc
{
    public class EqualsVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var isVisible = Equals(value, parameter) || Equals(value?.ToString(), parameter);
            return isVisible ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException($"{nameof(EqualsVisibilityConverter)} requires a one-way binding.");
        }
    }
}
