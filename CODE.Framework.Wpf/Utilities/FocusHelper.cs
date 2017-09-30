using System;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace CODE.Framework.Wpf.Utilities
{
    /// <summary>This class provides helpful features for focus management</summary>
    public static class FocusHelper
    {
        /// <summary>
        /// Can move the focus to a true control within a hierarchy of items. 
        /// For instance, this method can be called by passing a Grid which in turn contains a Panel
        /// which in turn contains a TextBox. The TextBox will receive focus.
        /// </summary>
        /// <param name="parent">Root element that is to receive focus.</param>
        /// <param name="delay">The delay in milliseconds before the focus is moved (100ms is the default).</param>
        /// <returns>True if a focusable element was found</returns>
        public static bool FocusFirstControlDelayed(UIElement parent, int delay = 100)
        {
            var itemsControl = parent as ItemsControl;
            if (itemsControl != null)
            {
                foreach (var element in itemsControl.Items.OfType<UIElement>())
                    if (FocusFirstControlDelayed(element)) return true;
                return false;
            }

            var panel = parent as Panel;
            if (panel != null)
            {
                foreach (var child in panel.Children.OfType<UIElement>())
                    if (FocusFirstControlDelayed(child)) return true;
            }

            var content = parent as ContentControl;
            if (content != null && content.Content != null)
            {
                var innerControl = content.Content as UIElement;
                if (innerControl != null)
                    return FocusFirstControlDelayed(innerControl);
            }

            if (parent is Control && !(parent is Label))
            {
                FocusDelayed(parent, delay: delay);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Sets the focus to the specified control(s) but after a slight delay to allow the calling method to finish before the focus is moved (by routing through a background thread and the message pump)
        /// </summary>
        /// <param name="focusElement1">The element to set the focus to.</param>
        /// <param name="focusElement2">An (optional) next element to set the focus to (typically a parent of the prior element).</param>
        /// <param name="focusElement3">An (optional) next element to set the focus to (typically a parent of the prior element).</param>
        /// <param name="focusElement4">An (optional) next element to set the focus to (typically a parent of the prior element).</param>
        /// <param name="delay">The delay in milliseconds before the focus is moved (100ms is the default).</param>
        public static void FocusDelayed(UIElement focusElement1, UIElement focusElement2 = null, UIElement focusElement3 = null, UIElement focusElement4 = null, int delay = 100)
        {
            var action = new Action<UIElement, UIElement, UIElement, UIElement, int>(FocusDelayed2);
            action.BeginInvoke(focusElement1, focusElement2, focusElement3, focusElement4, delay, null, null);
        }

        private static void FocusDelayed2(UIElement focusElement1, UIElement focusElement2, UIElement focusElement3, UIElement focusElement4, int delay)
        {
            Thread.Sleep(delay);
            var action = new Action<UIElement, UIElement, UIElement, UIElement, int>(FocusDelayed3);
            if (Application.Current != null && Application.Current.Dispatcher != null)
                try
                {
                    Application.Current.Dispatcher.BeginInvoke(action, DispatcherPriority.Normal, focusElement1, focusElement2, focusElement3, focusElement4, delay);
                }
                catch
                {
                    // Nothing we can do
                }
            //Dispatcher.CurrentDispatcher.BeginInvoke(action, DispatcherPriority.ApplicationIdle, new object[] {focusElement1, focusElement2, focusElement3, focusElement4, delay});
        }

        private static void FocusDelayed3(UIElement focusElement1, UIElement focusElement2, UIElement focusElement3, UIElement focusElement4, int delay)
        {
            MoveFocusIfPossible(focusElement1);
            MoveFocusIfPossible(focusElement2);
            MoveFocusIfPossible(focusElement3);
            MoveFocusIfPossible(focusElement4);
        }

        private static void MoveFocusIfPossible(UIElement element)
        {
            if (element == null) return;
            if (element.IsVisible)
                element.Focus();
            else
                element.IsVisibleChanged += TryAgainWhenVisible; // We try again when the control changes visibility
        }

        private static void TryAgainWhenVisible(object sender, DependencyPropertyChangedEventArgs e)
        {
            // Not there yet... we can try again
            var element = sender as UIElement;
            if (element == null) return;
            element.IsVisibleChanged -= TryAgainWhenVisible;
            FocusDelayed(element);
        }
    }
}