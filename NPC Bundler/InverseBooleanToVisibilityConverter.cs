using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace NPC_Bundler
{
    public sealed class InverseBooleanToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var isHidden = value is not bool b || !b;
            return isHidden ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is not Visibility v || v != Visibility.Visible;
        }
    }
}