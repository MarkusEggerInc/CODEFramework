using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using CODE.Framework.Wpf.Utilities;

namespace CODE.Framework.Wpf.BindingConverters
{
    /// <summary>
    /// Replaces dynamic brushes withing a drawing brush with a different set of brushes
    /// </summary>
    public class ReplaceBrushConverter : IValueConverter
    {
        /// <summary>
        /// Converts the specified value.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="targetType">Type of the target.</param>
        /// <param name="parameter">The parameter.</param>
        /// <param name="culture">The culture.</param>
        /// <returns>System.Object.</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var brushes = parameter as ResourceDictionary;
            if (brushes == null) return value;

            var drawingBrush = value as DrawingBrush;
            if (drawingBrush == null) return value;

            drawingBrush = drawingBrush.Clone(); // Need a copy to make sure we are not messing with existing brushes
            var replacementBrushes = new Dictionary<object, Brush>();
            foreach (var key in brushes.Keys)
                replacementBrushes.Add(key, brushes[key] as Brush);
            ResourceHelper.ReplaceDynamicDrawingBrushResources(drawingBrush, replacementBrushes);

            return drawingBrush;
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
