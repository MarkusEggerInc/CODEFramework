using System;
using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace CODE.Framework.Wpf.Mvvm
{
    /// <summary>
    /// Grid UI element that is automatically made visible and invisible depending on whether the current model implements IModelStatus
    /// </summary>
    public class ModelStatusGrid : Grid
    {
        /// <summary>
        /// Model used as the data context
        /// </summary>
        public object Model
        {
            get { return GetValue(ModelProperty); }
            set { SetValue(ModelProperty, value); }
        }

        /// <summary>
        /// Model dependency property
        /// </summary>
        public static readonly DependencyProperty ModelProperty = DependencyProperty.Register("Model", typeof (object), typeof (ModelStatusGrid), new UIPropertyMetadata(null, ModelChanged));

        /// <summary>
        /// Change handler for model property
        /// </summary>
        /// <param name="d">The dependency object that triggered this change.</param>
        /// <param name="e">The <see cref="System.Windows.DependencyPropertyChangedEventArgs"/> instance containing the event data.</param>
        private static void ModelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var grid = d as ModelStatusGrid;
            if (grid != null)
            {
                grid.Visibility = Visibility.Visible;
                var ms = e.NewValue as IModelStatus;
                if (ms != null)
                {
                    grid.ModelStatus = ms.ModelStatus;
                    var ms2 = ms as INotifyPropertyChanged;
                    if (ms2 != null)
                        ms2.PropertyChanged += (s2, e2) =>
                                                   {
                                                       if (string.IsNullOrEmpty(e2.PropertyName) || e2.PropertyName == "ModelStatus")
                                                           grid.ModelStatus = ms.ModelStatus;
                                                   };
                }
                else
                    grid.Visibility = Visibility.Collapsed;
            }
        }

        /// <summary>Indicates the model status of the bound model object</summary>
        public ModelStatus ModelStatus
        {
            get { return (ModelStatus) GetValue(ModelStatusProperty); }
            set { SetValue(ModelStatusProperty, value); }
        }
        /// <summary>Indicates the model status of the bound model object</summary>
        public static readonly DependencyProperty ModelStatusProperty = DependencyProperty.Register("ModelStatus", typeof(ModelStatus), typeof(ModelStatusGrid), new UIPropertyMetadata(ModelStatus.Unknown));
    }

    /// <summary>
    /// Converts a model status to visibility
    /// </summary>
    public class ModelStatusToVisibleConverter : IValueConverter
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
            if (value is ModelStatus)
            {
                var status = (ModelStatus) value;
                if (status == ModelStatus.Loading || status == ModelStatus.Saving) return Visibility.Visible;
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
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return ModelStatus.Unknown; // Not really using this
        }
    }
}
