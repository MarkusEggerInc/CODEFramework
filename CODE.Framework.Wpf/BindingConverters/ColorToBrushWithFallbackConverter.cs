using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace CODE.Framework.Wpf.BindingConverters
{
    /// <summary>
    /// Binds to a brush value and uses the fallback value when the brush is null
    /// </summary>
    public class ColorToBrushWithFallbackConverter : DependencyObject, IValueConverter
    {
        /// <summary>
        /// Falback brush used when the bound brush is null
        /// </summary>
        public Brush FallbackBrush
        {
            get { return (Brush)GetValue(FallbackBrushProperty); }
            set { SetValue(FallbackBrushProperty, value); }
        }
        /// <summary>
        /// Falback brush used when the bound brush is null
        /// </summary>
        public static readonly DependencyProperty FallbackBrushProperty = DependencyProperty.Register("FallbackBrush", typeof(Brush), typeof(ColorToBrushWithFallbackConverter), new PropertyMetadata(null));

        /// <summary>
        /// If the bound value is this color, then the fallback brush is used
        /// </summary>
        /// <value>The color of the ignore.</value>
        public Color IgnoreColor
        {
            get { return (Color)GetValue(IgnoreColorProperty); }
            set { SetValue(IgnoreColorProperty, value); }
        }
        /// <summary>
        /// If the bound value is this color, then the fallback brush is used
        /// </summary>
        public static readonly DependencyProperty IgnoreColorProperty = DependencyProperty.Register("IgnoreColor", typeof(Color), typeof(ColorToBrushWithFallbackConverter), new PropertyMetadata(Colors.Transparent));

        /// <summary>
        /// Converts the specified value.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="targetType">Type of the target.</param>
        /// <param name="parameter">The parameter.</param>
        /// <param name="culture">The culture.</param>
        /// <returns>System.Object.</returns>
        /// <exception cref="System.NotImplementedException"></exception>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is Color)) return FallbackBrush;
            var originalColor = (Color) value;
            if (originalColor == IgnoreColor) return FallbackBrush;
            return new SolidColorBrush(originalColor);
        }

        /// <summary>
        /// Converts the back.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="targetType">Type of the target.</param>
        /// <param name="parameter">The parameter.</param>
        /// <param name="culture">The culture.</param>
        /// <returns>System.Object.</returns>
        /// <exception cref="System.NotImplementedException"></exception>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value;
        }
    }
}
