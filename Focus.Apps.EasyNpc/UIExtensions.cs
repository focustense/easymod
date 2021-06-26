using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;

namespace Focus.Apps.EasyNpc
{
    static class UIExtensions
    {
        public static T FindVisualChildByName<T>(this DependencyObject parent, string name)
            where T : DependencyObject
        {
            return FindVisualChildrenByName<T>(parent, name).FirstOrDefault();
        }

        public static IEnumerable<T> FindVisualChildrenByName<T>(this DependencyObject parent, string name)
            where T : DependencyObject
        {
            var childCount = VisualTreeHelper.GetChildrenCount(parent);
            for (var i = 0; i < childCount; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child.GetValue(FrameworkElement.NameProperty) as string == name)
                    yield return (T)child;
                else
                {
                    foreach (var innerChild in FindVisualChildrenByName<T>(child, name))
                        yield return innerChild;
                }
            }
        }

        public static T FindVisualParentByType<T>(this DependencyObject child)
            where T : DependencyObject
        {
            for (var parent = child; parent != null; parent = VisualTreeHelper.GetParent(parent))
                if (parent is T)
                    return (T)parent;
            return default;
        }

        public static T GetFirstVisualChildByType<T>(this DependencyObject parent)
            where T : DependencyObject
        {
            var childCount = VisualTreeHelper.GetChildrenCount(parent);
            for (var i = 0; i < childCount; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child.GetType() == typeof(T))
                    return (T)child;
            }
            return null;
        }
    }
}