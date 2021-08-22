using System;
using System.Globalization;
using System.Windows.Data;

namespace Focus.Apps.EasyNpc
{
    public sealed class InverseBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is not bool b || !b;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is not bool b || !b;
        }
    }
}