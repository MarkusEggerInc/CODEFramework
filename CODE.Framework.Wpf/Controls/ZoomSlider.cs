using System;
using System.Windows;
using System.Windows.Controls;

namespace CODE.Framework.Wpf.Controls
{
    /// <summary>
    /// Slider class used to set zoom levels
    /// </summary>
    /// <remarks>
    /// In zoom scenarios, the value 1 is usually the middle of the slider, while all the way
    /// to the left is something like .5 and all the way to the right is something like 500
    /// </remarks>
    public class ZoomSlider : Slider
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ZoomSlider"/> class.
        /// </summary>
        public ZoomSlider()
        {
            //MouseDoubleClick += (o, e) => { Value = 1; };
            ValueChanged += (o, e) =>
                {
                    UpdateChangeIncrement();
                    UpdateZoomFromValue();
                };
            MouseDoubleClick += (o, e) => { Value = 1d; };

            SmallChange = .1d;
            Value = 1;
            Minimum = 0;
            Maximum = 2;
            UpdateChangeIncrement();
        }

        /// <summary>
        /// Maximum zoom factor (example: 500% is 5)
        /// </summary>
        public double MaximumZoom
        {
            get { return (double)GetValue(MaximumZoomProperty); }
            set { SetValue(MaximumZoomProperty, value); }
        }
        /// <summary>
        /// Maximum zoom factor (example: 500% is 5)
        /// </summary>
        public static readonly DependencyProperty MaximumZoomProperty = DependencyProperty.Register("MaximumZoom", typeof(double), typeof(ZoomSlider), new PropertyMetadata(5d));

        /// <summary>
        /// Minimum zoom factor (example: 50% is 0.5)
        /// </summary>
        public double MinimumZoom
        {
            get { return (double)GetValue(MinimumZoomProperty); }
            set { SetValue(MinimumZoomProperty, value); }
        }
        /// <summary>
        /// Minimum zoom factor (example: 50% is 0.5)
        /// </summary>
        public static readonly DependencyProperty MinimumZoomProperty = DependencyProperty.Register("MinimumZoom", typeof(double), typeof(ZoomSlider), new PropertyMetadata(.2d));

        /// <summary>
        /// Text representation of the current zoom level
        /// </summary>
        public string ZoomText
        {
            get { return (string)GetValue(ZoomTextProperty); }
            set { SetValue(ZoomTextProperty, value); }
        }

        /// <summary>
        /// Text representation of the current zoom level
        /// </summary>
        public static readonly DependencyProperty ZoomTextProperty = DependencyProperty.Register("ZoomText", typeof(string), typeof(ZoomSlider), new PropertyMetadata("100%"));

        /// <summary>
        /// Actual zoom factor based on the slider value
        /// </summary>
        public double Zoom
        {
            get { return (double)GetValue(ZoomProperty); }
            set { SetValue(ZoomProperty, value); }
        }
        /// <summary>
        /// Actual zoom factor based on the slider value
        /// </summary>
        public static readonly DependencyProperty ZoomProperty = DependencyProperty.Register("Zoom", typeof(double), typeof(ZoomSlider), new PropertyMetadata(1d, OnZoomChanged));

        private static void OnZoomChanged(DependencyObject d, DependencyPropertyChangedEventArgs args)
        {
            var zoom = d as ZoomSlider;
            if (zoom == null) return;
            zoom.OnZoomFactorChanged();
        }

        /// <summary>
        /// Called when the zoom factor changes (can be overridden in subclasses)
        /// </summary>
        protected virtual void OnZoomFactorChanged()
        {
        }

        private IncrementMode _incrementMode = IncrementMode.Unset;

        /// <summary>
        /// Calculates the change increment of the slider when the user clicks +/- based
        /// on the current value as well as min/max increments
        /// </summary>
        private void UpdateChangeIncrement()
        {
            if (Value <= 1d && _incrementMode != IncrementMode.Negative)
            {
                // Each small click represents a 10% increase downwards
                var negativeRange = 1d - MinimumZoom;
                var negativeMultiplier = negativeRange * 10;
                SmallChange = 1/negativeMultiplier;
                _incrementMode = IncrementMode.Negative;
            }
            else if (Value > 1d && _incrementMode != IncrementMode.Positive)
            {
                // Each small click represents a 10% increase upwards (which is on a different scale compared to shrinking)
                _incrementMode = IncrementMode.Positive;
                var positiveRange = MaximumZoom - 1d;
                var positiveMultiplier = positiveRange*10;
                SmallChange = 1/positiveMultiplier;
            }
        }

        private enum IncrementMode
        {
            Unset,
            Negative,
            Positive
        }

        /// <summary>
        /// Calculates the zoom factor from the current value and min/max settings
        /// </summary>
        private void UpdateZoomFromValue()
        {
            if (Value < 1d)
            {
                var negativeRange = 1d - MinimumZoom;
                var currentPercentageOfNegativeRange = Value;
                var currentShrinkFactor = MinimumZoom + (currentPercentageOfNegativeRange*negativeRange);
                Zoom = currentShrinkFactor;
                if (Zoom > 0.97d && Zoom < 1.03d) Zoom = 1d;
                var percentage = Math.Round(Zoom*100);
                ZoomText = percentage + "%";
            }
            else if (Value > 1d)
            {
                // Note: The positive side of the slider is on a different scale than the negative side
                var positiveRange = MaximumZoom - 1d;
                var currentPercentageOfPositiveRange = Value - 1;
                var currentGrowFactor = (currentPercentageOfPositiveRange*positiveRange) + 1;
                Zoom = currentGrowFactor;
                if (Zoom > 0.97d && Zoom < 1.03d) Zoom = 1d;
                var percentage = Math.Round(Zoom * 100);
                ZoomText = percentage + "%";
            }
            else
            {
                Zoom = 1d;
                ZoomText = "100%";
            }
        }
    }
}
