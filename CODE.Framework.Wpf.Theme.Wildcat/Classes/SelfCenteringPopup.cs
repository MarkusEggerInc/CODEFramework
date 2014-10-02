using System;
using System.Windows;
using System.Windows.Controls.Primitives;

namespace CODE.Framework.Wpf.Theme.Wildcat.Classes
{
    /// <summary>
    /// This popup class automatically adjusts the horizontal offset to always be centered in relation to the 
    /// </summary>
    public class SelfCenteringPopup : Popup
    {
        /// <summary>
        /// Gets or sets a value indicating whether this instance is displayed.
        /// </summary>
        /// <value><c>true</c> if this instance is displayed; otherwise, <c>false</c>.</value>
        public bool IsDisplayed
        {
            get { return (bool)GetValue(IsDisplayedProperty); }
            set { SetValue(IsDisplayedProperty, value); }
        }

        /// <summary>
        /// The is displayed property
        /// </summary>
        public static readonly DependencyProperty IsDisplayedProperty = DependencyProperty.Register("IsDisplayed", typeof(bool), typeof(SelfCenteringPopup), new PropertyMetadata(false, IsDisplayedChanged));

        /// <summary>
        /// Determines whether [is displayed changed] [the specified dependency object].
        /// </summary>
        /// <param name="d">The dependency object.</param>
        /// <param name="args">The <see cref="System.Windows.DependencyPropertyChangedEventArgs"/> instance containing the event data.</param>
        private static void IsDisplayedChanged(DependencyObject d, DependencyPropertyChangedEventArgs args)
        {
            var popup = d as Popup;
            if (popup == null) return;
            var isOpen = (bool)args.NewValue;
            popup.IsOpen = isOpen;

            if (isOpen && popup.Child != null)
            {
                var element = popup.Child as FrameworkElement;
                if (element != null)
                {
                    popup.HorizontalOffset = (element.ActualWidth / 2) * -1;
                    if (popup.PlacementTarget != null)
                    {
                        var element2 = popup.PlacementTarget as FrameworkElement;
                        if (element2 != null)
                            popup.HorizontalOffset += element2.ActualWidth/2;
                    }
                }
            }
        }
    }
}
