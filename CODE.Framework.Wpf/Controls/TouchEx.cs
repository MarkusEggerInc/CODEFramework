using System.Windows;

namespace CODE.Framework.Wpf.Controls
{
    /// <summary>
    /// This class provides attached properties for touch-related features
    /// </summary>
    public class TouchEx : DependencyObject
    {
        /// <summary>
        /// If set to true, the PointingDeviceInputMode attached property is automatically and continuously set on the target object
        /// </summary>
        public static readonly DependencyProperty UpdatePointingDeviceInputModeProperty = DependencyProperty.RegisterAttached("UpdatePointingDeviceInputMode", typeof(bool), typeof(TouchEx), new PropertyMetadata(false, OnUpdatePointingDeviceInputModeChanged));

        /// <summary>
        /// If set to true, the PointingDeviceInputMode attached property is automatically and continuously set on the target object
        /// </summary>
        public static bool GetUpdatePointingDeviceInputMode(DependencyObject d)
        {
            return (bool) d.GetValue(UpdatePointingDeviceInputModeProperty);
        }
        /// <summary>
        /// If set to true, the PointingDeviceInputMode attached property is automatically and continuously set on the target object
        /// </summary>
        public static void SetUpdatePointingDeviceInputMode(DependencyObject d, bool value)
        {
            d.SetValue(UpdatePointingDeviceInputModeProperty, value);
        }

        /// <summary>
        /// Fires when the property value changes
        /// </summary>
        /// <param name="d">Deopendency object the value is set on</param>
        /// <param name="e">Event args</param>
        private static void OnUpdatePointingDeviceInputModeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (!(bool) e.NewValue) return;
            var element = d as UIElement;
            if (element == null) return;

            element.PreviewMouseMove += (s, e2) =>
            {
                if (e2.StylusDevice == null)
                    SetPointingDeviceInputMode(element, PointingDeviceInputMode.Mouse);
            };
            element.PreviewMouseDown += (s, e2) =>
            {
                if (e2.StylusDevice == null)
                    SetPointingDeviceInputMode(element, PointingDeviceInputMode.Mouse);
            };
            element.PreviewTouchDown += (s, e2) =>
            {
                SetPointingDeviceInputMode(element, PointingDeviceInputMode.Touch);
            };
            element.PreviewTouchMove += (s, e2) =>
            {
                SetPointingDeviceInputMode(element, PointingDeviceInputMode.Touch);
            };

            SetPointingDeviceInputMode(element, _mostRecentPointingDeviceInputMode);
        }

        /// <summary>
        /// Indicates the current input mode
        /// </summary>
        /// <remarks>This property should never be set manually. It is auto-updated if UpdatePointingDeviceInputMode is set to true.</remarks>
        public static readonly DependencyProperty PointingDeviceInputModeProperty = DependencyProperty.RegisterAttached("PointingDeviceInputMode", typeof(PointingDeviceInputMode), typeof(TouchEx), new PropertyMetadata(PointingDeviceInputMode.Mouse, OnPointingDeviceInputModeChanged));

        /// <summary>
        /// Global flag for the most recent input mode
        /// </summary>
        private static PointingDeviceInputMode _mostRecentPointingDeviceInputMode = PointingDeviceInputMode.Mouse;

        /// <summary>
        /// Fires when the property value changes
        /// </summary>
        /// <param name="d">Reference to the object the value changed on</param>
        /// <param name="e">Event args</param>
        private static void OnPointingDeviceInputModeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            _mostRecentPointingDeviceInputMode = (PointingDeviceInputMode)e.NewValue;
        }

        /// <summary>
        /// Indicates the current input mode
        /// </summary>
        /// <remarks>This property should never be set manually. It is auto-updated if UpdatePointingDeviceInputMode is set to true.</remarks>
        public static PointingDeviceInputMode GetPointingDeviceInputMode(DependencyObject d)
        {
            return (PointingDeviceInputMode) d.GetValue(PointingDeviceInputModeProperty);
        }

        /// <summary>
        /// Indicates the current input mode
        /// </summary>
        /// <remarks>This property should never be set manually. It is auto-updated if UpdatePointingDeviceInputMode is set to true.</remarks>
        public static void SetPointingDeviceInputMode(DependencyObject d, PointingDeviceInputMode value)
        {
            d.SetValue(PointingDeviceInputModeProperty, value);
        }
    }

    /// <summary>
    /// Defines available input modes for pointing devices
    /// </summary>
    public enum PointingDeviceInputMode
    {
        /// <summary>
        /// The mouse is used as a pointing device
        /// </summary>
        Mouse,
        /// <summary>
        /// Touch is used
        /// </summary>
        Touch
    }
}
