using System;
using System.Globalization;
using System.Windows.Data;

namespace Focus.Apps.EasyNpc
{
    public class EqualsConverter : IValueConverter
    {
        protected virtual bool Invert => false;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Invert ? !Equals(value, parameter) : Equals(value, parameter);
        }

        public object? ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var comparison = value is bool b && b;
            if (Invert)
                comparison = !comparison;
            return comparison ? parameter : null;
        }
    }

    public class InvertedEqualsConverter : EqualsConverter
    {
        protected override bool Invert => true;
    }
}
