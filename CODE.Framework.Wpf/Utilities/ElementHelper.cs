using System.Windows;
using System.Windows.Media;

namespace CODE.Framework.Wpf.Utilities
{
    /// <summary>
    /// This class provides utility features related to WPF elements
    /// </summary>
    public static class ElementHelper
    {
        /// <summary>
        /// Finds the parent by walking element.Parent until a parent of a certain type is found, or the end of the chain is reached
        /// </summary>
        /// <typeparam name="TType">The type of the parent element that is to be found.</typeparam>
        /// <param name="element">The element.</param>
        /// <returns>FrameworkElement.</returns>
        public static FrameworkElement FindParent<TType>(FrameworkElement element) where TType : UIElement
        {
            var currentElement = element;
            while (true)
            {
                if (currentElement == null) return null;
                if (currentElement is TType) return currentElement;
                currentElement = currentElement.Parent as FrameworkElement;
            }
        }

        /// <summary>
        /// Finds the parent by walking the complete visual tree until a parent of a certain type is found, or the end of the chain is reached
        /// </summary>
        /// <typeparam name="TType">The type of the parent element that is to be found.</typeparam>
        /// <param name="element">The element.</param>
        /// <returns>FrameworkElement.</returns>
        public static FrameworkElement FindVisualTreeParent<TType>(FrameworkElement element) where TType : UIElement
        {
            var currentElement = element;
            while (true)
            {
                if (currentElement == null) return null;
                if (currentElement is TType) return currentElement;
                currentElement = VisualTreeHelper.GetParent(currentElement) as FrameworkElement;
            }
        }
    }
}
