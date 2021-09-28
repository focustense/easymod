using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Windows.Data;

namespace Focus.Apps.EasyNpc
{
    public class MultiSequenceConverter : IMultiValueConverter
    {
        public object? Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            var elementType = GetElementType(targetType);
            if (elementType is null)
                return null;
            var elements = values.OfType<IEnumerable<object>>().SelectMany(seq => seq);
            return CastElementType(elements, elementType);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException($"{nameof(MultiSequenceConverter)} requires a one-way binding.");
        }

        private static object CastElementType(IEnumerable<object> sequence, Type elementType)
        {
            var castMethod = typeof(Enumerable)
                .GetMethod(nameof(Enumerable.Cast), BindingFlags.Public | BindingFlags.Static)!
                .MakeGenericMethod(elementType);
            var listType = typeof(List<>).MakeGenericType(elementType);
            return Activator.CreateInstance(listType, castMethod.Invoke(null, new[] { sequence }))!;
        }

        private static Type? GetElementType(Type collectionType)
        {
            if (collectionType.IsGenericType && collectionType.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                return collectionType.GetGenericArguments()[0];
            return collectionType.GetInterfaces()
                .Where(t => t != collectionType)
                .Select(t => GetElementType(collectionType))
                .Where(t => t is not null)
                .FirstOrDefault();
        }
    }
}
