using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Focus.Apps.EasyNpc
{
    public class EqualsVisibilityConverter : IValueConverter
    {
        protected virtual bool Invert => false;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var isVisible = Equals(value, parameter) || Equals(value?.ToString(), parameter);
            if (Invert)
                isVisible = !isVisible;
            return isVisible ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException($"{nameof(EqualsVisibilityConverter)} requires a one-way binding.");
        }
    }

    public class InvertedEqualsVisibilityConverter : EqualsVisibilityConverter
    {
        protected override bool Invert => true;
    }
}
