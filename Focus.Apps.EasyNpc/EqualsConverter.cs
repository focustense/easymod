using System;
using System.Globalization;
using System.Windows.Data;

namespace Focus.Apps.EasyNpc
{
    public class EqualsConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Equals(value, parameter);
        }

        public object? ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is bool b && b ? parameter : null;
        }
    }
}
