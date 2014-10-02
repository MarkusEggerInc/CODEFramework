using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace CODE.Framework.Wpf.Theme.Workplace.Classes
{
    /// <summary>
    /// Button used to style the scroll-right button of a scroll bar
    /// </summary>
    public class ScrollRightButton : RepeatButton
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ScrollRightButton" /> class.
        /// </summary>
        public ScrollRightButton()
        {
            IsEnabled = false;
        }

        /// <summary>
        /// Raises an automation event and calls the base method to raise the <see cref="E:System.Windows.Controls.Primitives.ButtonBase.Click" /> event.
        /// </summary>
        protected override void OnClick()
        {
            if (ScrollViewer != null)
                ScrollViewer.LineRight();

            base.OnClick();
        }

        /// <summary>Reference to the scroll viewer this button goes with</summary>
        public ScrollViewer ScrollViewer
        {
            get { return (ScrollViewer)GetValue(ScrollViewerProperty); }
            set { SetValue(ScrollViewerProperty, value); }
        }
        /// <summary>Reference to the scroll viewer this button goes with</summary>
        public static readonly DependencyProperty ScrollViewerProperty = DependencyProperty.Register("ScrollViewer", typeof(ScrollViewer), typeof(ScrollRightButton), new PropertyMetadata(null, ScrollViewerChanged));
        /// <summary>
        /// This method fires when the assigned scroll viewer changes
        /// </summary>
        /// <param name="d">The dependency object.</param>
        /// <param name="e">The <see cref="DependencyPropertyChangedEventArgs" /> instance containing the event data.</param>
        private static void ScrollViewerChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var button = d as ScrollRightButton;
            var scroll = e.NewValue as ScrollViewer;
            if (button == null || scroll == null) return;

            scroll.ScrollChanged += (s, args) => button.CalculateButtonSettings();
            scroll.LayoutUpdated += (s2, args2) => button.CalculateButtonSettings();

            var descriptor = DependencyPropertyDescriptor.FromProperty(ScrollViewer.ExtentWidthProperty, typeof (ScrollViewer));
            if (descriptor != null) descriptor.AddValueChanged(button, (s3, args3) => button.CalculateButtonSettings());
        }

        private void CalculateButtonSettings()
        {
            if (ScrollViewer == null) return;
            IsEnabled = ScrollViewer.ExtentWidth > ScrollViewer.HorizontalOffset + ScrollViewer.ViewportWidth;
        }
    }

    /// <summary>
    /// Button used to style the scroll-left button of a scroll bar
    /// </summary>
    public class ScrollLeftButton : RepeatButton
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ScrollLeftButton" /> class.
        /// </summary>
        public ScrollLeftButton()
        {
            IsEnabled = false;
        }

        /// <summary>
        /// Raises an automation event and calls the base method to raise the <see cref="E:System.Windows.Controls.Primitives.ButtonBase.Click" /> event.
        /// </summary>
        protected override void OnClick()
        {
            if (ScrollViewer != null)
                ScrollViewer.LineLeft();

            base.OnClick();
        }

        /// <summary>Reference to the scroll viewer this button goes with</summary>
        public ScrollViewer ScrollViewer
        {
            get { return (ScrollViewer)GetValue(ScrollViewerProperty); }
            set { SetValue(ScrollViewerProperty, value); }
        }
        /// <summary>Reference to the scroll viewer this button goes with</summary>
        public static readonly DependencyProperty ScrollViewerProperty = DependencyProperty.Register("ScrollViewer", typeof(ScrollViewer), typeof(ScrollLeftButton), new PropertyMetadata(null, ScrollViewerChanged));
        /// <summary>
        /// This method fires when the assigned scroll viewer changes
        /// </summary>
        /// <param name="d">The dependency object.</param>
        /// <param name="e">The <see cref="DependencyPropertyChangedEventArgs" /> instance containing the event data.</param>
        private static void ScrollViewerChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var button = d as ScrollLeftButton;
            var scroll = e.NewValue as ScrollViewer;
            if (button == null || scroll == null) return;

            scroll.ScrollChanged += (s, args) => button.CalculateButtonSettings();
            scroll.LayoutUpdated += (s2, args2) => button.CalculateButtonSettings();

            var descriptor = DependencyPropertyDescriptor.FromProperty(ScrollViewer.ExtentWidthProperty, typeof(ScrollViewer));
            if (descriptor != null) descriptor.AddValueChanged(button, (s3, args3) => button.CalculateButtonSettings());
        }

        private void CalculateButtonSettings()
        {
            if (ScrollViewer == null) return;
            IsEnabled = ScrollViewer.HorizontalOffset > 0;
        }
    }
}
