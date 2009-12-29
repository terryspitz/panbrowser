using System;
using System.Windows;
using System.Windows.Media;

namespace Terry
{
    /// <summary>
    ///  from http://www.wpftutorial.net/LogicalAndVisualTree.html
    /// </summary>
    public static class VisualTreeHelperExtensions
    {
        public static T FindAncestor<T>(DependencyObject dependencyObject)
            where T : class
        {
            DependencyObject target = dependencyObject;
            do
            {
                target = VisualTreeHelper.GetParent(target);
            }
            while (target != null && !(target is T));
            return target as T;
        }
        public static T FindDescendent<T>(DependencyObject dependencyObject)
            where T : class
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(dependencyObject); i++)
            {
                DependencyObject target = VisualTreeHelper.GetChild(dependencyObject, i);
                if (target == null)
                    return null;
                if (target is T)
                    return target as T;
                T ret = FindDescendent<T>(target);
                if (ret != null)
                    return ret;
            }
            return null;
        }
    }
}
