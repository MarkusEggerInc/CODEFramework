using System.Windows;
using System.Windows.Controls;

namespace CODE.Framework.Wpf.Theme.Newsroom.Controls
{
    /// <summary>
    /// Special content control that is aware of its size and reduces margins accordingly
    /// </summary>
    public class SizeAwareContentControl : ContentControl
    {
        private bool _inSizeChanged;

        /// <summary>Constructor</summary>
        public SizeAwareContentControl()
        {
            IsTabStop = false;
            SizeChanged += (s, e) =>
                               {
                                   if (_inSizeChanged) return;
                                   _inSizeChanged = true;
                                   CalculateMargin();
                                   _inSizeChanged = false;
                               };
        }

        /// <summary>Maximum margin allowed around the control</summary>
        public Thickness MaximumMargin
        {
            get { return (Thickness)GetValue(MaximumMarginProperty); }
            set { SetValue(MaximumMarginProperty, value); }
        }
        /// <summary>Maximum margin allowed around the control</summary>
        public static readonly DependencyProperty MaximumMarginProperty = DependencyProperty.Register("MaximumMargin", typeof(Thickness), typeof(SizeAwareContentControl), new UIPropertyMetadata(new Thickness(0), MaximumMarginChanged));
        private static void MaximumMarginChanged(DependencyObject source, DependencyPropertyChangedEventArgs args)
        {
            var contentControl = source as SizeAwareContentControl;
            if (contentControl != null) contentControl.CalculateMargin();
        }

        /// <summary>Minimum margin allowed around the control</summary>
        public Thickness MinimumMargin
        {
            get { return (Thickness)GetValue(MinimumMarginProperty); }
            set { SetValue(MinimumMarginProperty, value); }
        }
        /// <summary>Minimum margin allowed around the control</summary>
        public static readonly DependencyProperty MinimumMarginProperty = DependencyProperty.Register("MinimumMargin", typeof(Thickness), typeof(SizeAwareContentControl), new UIPropertyMetadata(new Thickness(0), MinimumMarginChanged));
        private static void MinimumMarginChanged(DependencyObject source, DependencyPropertyChangedEventArgs args)
        {
            var contentControl = source as SizeAwareContentControl;
            if (contentControl != null) contentControl.CalculateMargin();
        }

        /// <summary>If the size of the control (including margin) goes below this size, the maximum margin will not be applied and the margin will be shrunk down to the desired minimum margin</summary>
        public Size MinimumDesiredControlSize
        {
            get { return (Size)GetValue(MinimumDesiredControlSizeProperty); }
            set { SetValue(MinimumDesiredControlSizeProperty, value); }
        }
        /// <summary>If the size of the control (including margin) goes below this size, the maximum margin will not be applied and the margin will be shrunk down to the desired minimum margin</summary>
        public static readonly DependencyProperty MinimumDesiredControlSizeProperty = DependencyProperty.Register("MinimumDesiredControlSize", typeof(Size), typeof(SizeAwareContentControl), new UIPropertyMetadata(new Size(), MinimumDesiredControlSizeChanged));
        private static void MinimumDesiredControlSizeChanged(DependencyObject source, DependencyPropertyChangedEventArgs args)
        {
            var contentControl = source as SizeAwareContentControl;
            if (contentControl != null) contentControl.CalculateMargin();
        }

        private int _lastHeight = -1;
        private int _lastWidth = -1;

        /// <summary>This method re-calculates the actual margin the control should use</summary>
        private void CalculateMargin()
        {
            var width = Width;
            var height = Height;
            if (width < 1 || double.IsNaN(width)) width = ActualWidth;
            if (height < 1 || double.IsNaN(height)) height = ActualHeight;


            if (height < 1 || width < 1) return;

            height = height + Margin.Top + Margin.Bottom;
            width = width + Margin.Left + Margin.Right;

            if ((int)height == _lastHeight && (int)width == _lastWidth) return; // We are not doing this again if the height didn't really change based on the pixel-rounted (int) dimensions
            _lastHeight = (int)height;
            _lastWidth = (int)width;

            var actualMargin = MaximumMargin;

            if (height < MinimumDesiredControlSize.Height)
            {
                var heightOverage = MinimumDesiredControlSize.Height - height;
                var topShrinkPotential = MaximumMargin.Top - MinimumMargin.Top;
                var bottomShrinkPotential = MaximumMargin.Bottom - MinimumMargin.Bottom;
                if (heightOverage >= topShrinkPotential + bottomShrinkPotential)
                {
                    // It is too large anyway. We just go with the minimum, and that is the best we can do
                    actualMargin.Top = MinimumMargin.Top;
                    actualMargin.Bottom = MinimumMargin.Bottom;
                }
                else if (heightOverage <= bottomShrinkPotential)
                    // We have enough wiggle-room just at the bottom, so we use that up
                    actualMargin.Bottom -= heightOverage;
                else
                {
                    // We need to use all of the bottom and some of the top margin
                    actualMargin.Bottom = MinimumMargin.Bottom;
                    actualMargin.Top = actualMargin.Top - (heightOverage - bottomShrinkPotential);
                }
            }

            if (width < MinimumDesiredControlSize.Width)
            {
                var widthOverage = MinimumDesiredControlSize.Width - width;
                var leftShrinkPotential = MaximumMargin.Left - MinimumMargin.Left;
                var rightShrinkPotential = MaximumMargin.Right - MinimumMargin.Right;
                if (widthOverage >= leftShrinkPotential + rightShrinkPotential)
                {
                    // It is too large anyway. We just go with the minimum, and that is the best we can do
                    actualMargin.Left = MinimumMargin.Left;
                    actualMargin.Right = MinimumMargin.Right;
                }
                else if (widthOverage <= rightShrinkPotential)
                    // We have enough wiggle-room just at the right, so we use that up
                    actualMargin.Right -= widthOverage;
                else
                {
                    // We need to use all of the right and some of the left margin
                    actualMargin.Right = MinimumMargin.Right;
                    actualMargin.Left = actualMargin.Left - (widthOverage - rightShrinkPotential);
                }
            }

            Margin = actualMargin;
        }
    }
}
