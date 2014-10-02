using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace CODE.Framework.Wpf.BindingConverters
{
    /// <summary>
    /// Converts colors to a lighter or darker version
    /// </summary>
    public class LitColorConverter : IValueConverter
    {
        /// <summary>
        /// Converts a value.
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
            var lightFactor = decimal.Parse(parameter.ToString(), CultureInfo.InvariantCulture);

            if (lightFactor >= 2.0m)
                // No calculations needed. This is white
                return Colors.White;

            // OK, lighting is required, so here we go
            var red = originalColor.R;
            var green = originalColor.G;
            var blue = originalColor.B;

            // Do we need to add or remove light?
            if (lightFactor < 1.0m)
                // Darken - We can simply reduce the color intensity
                return Color.FromRgb((byte)(red*lightFactor), (byte)(green*lightFactor), (byte)(blue*lightFactor));
            // Lighten - We do this by approaching 256 for a light factor of 2.0f
            var lightFactor2 = lightFactor;
            if (lightFactor2 > 1.0m)
                lightFactor2 -= 1.0m;
            var red2 = 255 - red;
            var green2 = 255 - green;
            var blue2 = 255 - blue;
            red += (byte)(red2*lightFactor2);
            green += (byte)(green2*lightFactor2);
            blue += (byte)(blue2*lightFactor2);
            if (red > 255)
                red = 255;
            if (green > 255)
                green = 255;
            if (blue > 255)
                blue = 255;
            return Color.FromRgb(red, green, blue);
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
