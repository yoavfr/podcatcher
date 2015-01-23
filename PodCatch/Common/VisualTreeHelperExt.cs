using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;

namespace PodCatch.Common
{
    public static class VisualTreeHelperExt
    {
        public static T GetChild<T>(this DependencyObject reference, string childName) where T : DependencyObject
        {
            DependencyObject foundChild = null;
            if (reference != null)
            {
                Type childType = typeof(T);
                int childrenCount = VisualTreeHelper.GetChildrenCount(reference);
                for (int i = 0; i < childrenCount; i++)
                {
                    var child = VisualTreeHelper.GetChild(reference, i);
                    // If the child is not of the request child type child
                    if (child.GetType() != childType)
                    {
                        // recursively drill down the tree
                        foundChild = GetChild<T>(child, childName);
                    }
                    else if (!string.IsNullOrEmpty(childName))
                    {
                        var frameworkElement = child as FrameworkElement;
                        // If the child's name is set for search
                        if (frameworkElement != null && frameworkElement.Name == childName)
                        {
                            // if the child's name is of the request name
                            foundChild = child;
                            break;
                        }
                    }
                    else
                    {
                        // child element found.
                        foundChild = child;
                        break;
                    }
                }
            }
            return foundChild as T;
        }
    }
}