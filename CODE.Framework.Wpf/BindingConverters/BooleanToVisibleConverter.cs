using System;
using System.Windows;
using System.Windows.Data;

namespace CODE.Framework.Wpf.BindingConverters
{
    /// <summary>
    /// Converts boolean values to Visibility
    /// </summary>
    public class BooleanToVisibleConverter : IValueConverter
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
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            try
            {
                string trueValue = parameter.ToString().ToLower();
                string toStringCompareValue = string.Empty;
                if (string.IsNullOrEmpty(trueValue)) trueValue = "visible";

                if (trueValue.IndexOf("=") > -1)
                {
                    var parts = trueValue.Split('=');
                    toStringCompareValue = parts[0];
                    trueValue = parts[1];
                }

                if (value is bool?) return ConvertBoolean(value, trueValue);
                string stringValue = value.ToString();
                if (stringValue.IndexOf(".") > -1)
                {
                    var parts2 = stringValue.Split('.');
                    stringValue = parts2[parts2.Length - 1];
                }
                if (string.Compare(stringValue, toStringCompareValue, true) == 0)
                {
                    switch (trueValue)
                    {
                        case "collapsed":
                            return Visibility.Collapsed;
                        case "hidden":
                            return Visibility.Hidden;
                        case "visible":
                            return Visibility.Visible;
                    }
                }

                switch (trueValue)
                {
                    case "collapsed":
                    case "hidden":
                        return Visibility.Visible;
                    case "visible":
                        return Visibility.Collapsed;
                }
            }
            catch { }

            return Visibility.Collapsed;
        }

        /// <summary>
        /// Converts strings to boolean.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="trueValue">The true value.</param>
        /// <returns></returns>
        private static object ConvertBoolean(object value, string trueValue)
        {
            bool? booleanValue = (bool?)value;

            if (booleanValue == true)
            {
                switch (trueValue)
                {
                    case "collapsed":
                        return Visibility.Collapsed;
                    case "hidden":
                        return Visibility.Hidden;
                    case "visible":
                        return Visibility.Visible;
                }
            }

            switch (trueValue)
            {
                case "collapsed":
                case "hidden":
                    return Visibility.Visible;
                case "visible":
                    return Visibility.Collapsed;
            }

            return Visibility.Collapsed;
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
        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            // Not needed
            return null;
        }
    }

}
