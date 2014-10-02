using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace CODE.Framework.Wpf.Theme.Wildcat.Classes
{
    /// <summary>
    /// Converts view theme colors to real background colors
    /// </summary>
    public class ThemeColorConverter : IValueConverter
    {
        /// <summary>
        /// Converts a value.
        /// </summary>
        /// <param name="value">The value produced by the binding source.</param>
        /// <param name="targetType">The type of the binding target property.</param>
        /// <param name="parameter">The converter parameter to use.</param>
        /// <param name="culture">The culture to use in the converter.</param>
        /// <returns>A converted value. If the method returns null, the valid null value is used.</returns>
        /// <exception cref="System.NotImplementedException"></exception>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is Color)) return value;
            var color = (Color) value;
            if (color == Colors.Transparent)
            {
                var resource = Application.Current.FindResource("CODE.Framework-Application-ThemeColor1");
                if (resource == null) return new LinearGradientBrush(Color.FromRgb(254, 160, 46), Color.FromRgb(203, 112, 0), new Point(0, 0), new Point(0, 1));
                return new LinearGradientBrush((Color) resource, GetDarkenedColor((Color) resource), new Point(0, 0), new Point(0, 1));
            }
            return new LinearGradientBrush(color, GetDarkenedColor(color), new Point(0, 0), new Point(0, 1));
        }

        private Color GetDarkenedColor(Color originalColor)
        {
            var red = originalColor.R;
            var green = originalColor.G;
            var blue = originalColor.B;
            return Color.FromRgb((byte) (red*.8), (byte) (green*.8), (byte) (blue*.8));
        }

        /// <summary>
        /// Converts a value.
        /// </summary>
        /// <param name="value">The value that is produced by the binding target.</param>
        /// <param name="targetType">The type to convert to.</param>
        /// <param name="parameter">The converter parameter to use.</param>
        /// <param name="culture">The culture to use in the converter.</param>
        /// <returns>A converted value. If the method returns null, the valid null value is used.</returns>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value; // Not used
        }
    }
}
