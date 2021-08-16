using System;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Linq.Expressions;
using System.Reflection;

namespace Focus.Apps.EasyNpc
{
    static class PropertyChangedExtensions
    {
        private static readonly ConcurrentDictionary<Type, FieldInfo?> handlerFields = new();

        public static IDisposable WhenChanged<T>(this T source, string propertyName, Action action)
            where T : INotifyPropertyChanged
        {
            void handler(object? _, PropertyChangedEventArgs e)
            {
                if (e.PropertyName == propertyName)
                    action();
            }

            source.PropertyChanged += handler;
            return new RunOnDispose(() => source.PropertyChanged -= handler);
        }
    }
}
