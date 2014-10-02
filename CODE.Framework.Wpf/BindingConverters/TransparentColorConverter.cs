using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace CODE.Framework.Wpf.BindingConverters
{
    /// <summary>
    /// Converts colors to a semi-transparent version of the same color
    /// </summary>
    public class TransparentColorConverter : IValueConverter
    {
        /// <summary>
        /// Converts a color value to its semi-transparent counterpart. The converter parameter defines the opacity (1 = fully opaque, 0 = fully transparent).
        /// </summary>
        /// <param name="value">The value produced by the binding source.</param>
        /// <param name="targetType">The type of the binding target property.</param>
        /// <param name="parameter">The converter parameter to use.</param>
        /// <param name="culture">The culture to use in the converter.</param>
        /// <returns>
        /// A converted value. If the method returns null, the valid null value is used.
        /// </returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is Color)) return value;
            var originalColor = (Color)value;
            var transparency = (byte)((decimal.Parse(parameter.ToString(), CultureInfo.InvariantCulture)) * 255);
            return Color.FromArgb(transparency, originalColor.R, originalColor.G, originalColor.B);
        }

        /// <summary>
        /// Converts a value.
        /// </summary>
        /// <param name="value">The value that is produced by the binding target.</param>
        /// <param name="targetType">The type to convert to.</param>
        /// <param name="parameter">The converter parameter to use.</param>
        /// <param name="culture">The culture to use in the converter.</param>
        /// <returns>
        /// A converted value. If the method returns null, the valid null value is used.
        /// </returns>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Not needed
            return null;
        }
    }
}
