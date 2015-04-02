using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using CODE.Framework.Wpf.Controls;

namespace CODE.Framework.Wpf.Theme.Metro.Classes
{
    /// <summary>
    /// Converts the pointing mode to a margin
    /// </summary>
    public class PointingDeviceInputModeToMarginConverter : IValueConverter
    {
        /// <summary>
        /// Constructor
        /// </summary>
        public PointingDeviceInputModeToMarginConverter()
        {
            MouseMargin = new Thickness();
            TouchMargin = new Thickness();
        }

        /// <summary>
        /// Margin to be used when the mode is set to Mouse
        /// </summary>
        public Thickness MouseMargin { get; set; }

        /// <summary>
        /// Margin to be used when the mode is set to Touch
        /// </summary>
        public Thickness TouchMargin { get; set; }

        /// <summary>
        /// Converts the value to a margin
        /// </summary>
        /// <param name="value">Value</param>
        /// <param name="targetType">Target Type</param>
        /// <param name="parameter">Parameter</param>
        /// <param name="culture">Culture</param>
        /// <returns>Margin</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var mode = (PointingDeviceInputMode) value;
            return mode == PointingDeviceInputMode.Mouse ? MouseMargin : TouchMargin;
        }

        /// <summary>
        /// Converts the value back 
        /// </summary>
        /// <param name="value">Value</param>
        /// <param name="targetType">Target type</param>
        /// <param name="parameter">Parameter</param>
        /// <param name="culture">Culture</param>
        /// <returns>Unchanged value</returns>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value;
        }
    }
}
