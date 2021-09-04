using System;
using System.Globalization;
using System.Linq;
using System.Windows.Data;

namespace Focus.Apps.EasyNpc
{
    public class MultiBooleanConverter : IMultiValueConverter
    {
        public enum OperatorType { And, Or }

        public OperatorType Operator { get; set; } = OperatorType.And;

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            return Operator switch
            {
                OperatorType.And => values.All(v => v is bool b && b),
                OperatorType.Or => values.Any(v => v is bool b && b),
                _ => throw new InvalidOperationException($"Unsupported boolean operator: ${Operator}")
            };
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException($"{nameof(MultiBooleanConverter)} requires a one-way binding.");
        }
    }
}
