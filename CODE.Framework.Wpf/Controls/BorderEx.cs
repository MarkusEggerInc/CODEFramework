using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;

namespace CODE.Framework.Wpf.Controls
{
    /// <summary>
    /// Border class with special extensions
    /// </summary>
    public class BorderEx : Border 
    {
        /// <summary>
        /// If set to true, the object will always have the same height and width
        /// </summary>
        public static readonly DependencyProperty KeepHeightAndWidthEqualProperty = DependencyProperty.RegisterAttached("KeepHeightAndWidthEqual", typeof(bool), typeof(BorderEx), new PropertyMetadata(false, OnKeepHeightAndWidthEqualChanged));

        /// <summary>
        /// Fires whenever the property changes
        /// </summary>
        /// <param name="d">Object the attached property has been set on</param>
        /// <param name="args">Event arguments</param>
        private static void OnKeepHeightAndWidthEqualChanged(DependencyObject d, DependencyPropertyChangedEventArgs args)
        {
            if (!(bool) args.NewValue) return;
            var border = d as Border;
            if (border == null) return;

            var heightDescriptor = DependencyPropertyDescriptor.FromProperty(ActualHeightProperty, typeof (Border));
            if (heightDescriptor != null)
                heightDescriptor.AddValueChanged(border, (s, e) =>
                {
                    if (!GetKeepHeightAndWidthEqual(border)) return;
                    if (border.ActualWidth < border.ActualHeight - .1)
                        border.MinWidth = border.ActualHeight;
                });
            var widthDescriptor = DependencyPropertyDescriptor.FromProperty(ActualWidthProperty, typeof(Border));
            if (widthDescriptor != null)
                widthDescriptor.AddValueChanged(border, (s, e) =>
                {
                    if (!GetKeepHeightAndWidthEqual(border)) return;
                    if (border.ActualHeight < border.ActualWidth - .1)
                        border.MinHeight = border.ActualWidth;
                });
        }

        /// <summary>
        /// If set to true, the object will always have the same height and width
        /// </summary>
        /// <param name="d">The dependency object to set the value on</param>
        /// <param name="value">True if height and width are meant to always be equal.</param>
        public static void SetKeepHeightAndWidthEqual(DependencyObject d, bool value)
        {
            d.SetValue(KeepHeightAndWidthEqualProperty, value);
        }

        /// <summary>
        /// If set to true, the object will always have the same height and width
        /// </summary>
        /// <param name="d">The dependency object to set the value on</param>
        /// <returns>True if height and width are meant to always be equal.</returns>
        public static bool GetKeepHeightAndWidthEqual(DependencyObject d)
        {
            return (bool) d.GetValue(KeepHeightAndWidthEqualProperty);
        }

        /// <summary>
        /// Defines whether the border attempts to set its corner radius to form a circle
        /// </summary>
        /// <param name="obj">The border object.</param>
        /// <returns>True, if the object is meant to be circular.</returns>
        public static bool GetForceCircularOutline(DependencyObject obj)
        {
            return (bool)obj.GetValue(ForceCircularOutlineProperty);
        }

        /// <summary>
        /// Defines whether the border attempts to set its corner radius to form a circle
        /// </summary>
        /// <param name="obj">The border object.</param>
        /// <param name="value">True, if the object is meant to be circular.</param>
        public static void SetForceCircularOutline(DependencyObject obj, bool value)
        {
            obj.SetValue(ForceCircularOutlineProperty, value);
        }
        /// <summary>
        /// Defines whether the border attempts to set its corner radius to form a circle
        /// </summary>
        public static readonly DependencyProperty ForceCircularOutlineProperty = DependencyProperty.RegisterAttached("ForceCircularOutline", typeof(bool), typeof(BorderEx), new PropertyMetadata(false, OnForceCircularOutlineChanged));

        /// <summary>
        /// Fires when the property changes
        /// </summary>
        /// <param name="d">The border</param>
        /// <param name="args">The <see cref="DependencyPropertyChangedEventArgs"/> instance containing the event data.</param>
        private static void OnForceCircularOutlineChanged(DependencyObject d, DependencyPropertyChangedEventArgs args)
        {
            if (!(bool)args.NewValue) return;
            var border = d as Border;
            if (border == null) return;

            border.LayoutUpdated += (s, e) =>
            {
                if (!GetForceCircularOutline(border)) return;
                var maxDimension = Math.Max(border.ActualHeight, border.ActualWidth);
                if (double.IsNaN(maxDimension)) return;
                var radius = maxDimension/2;
                border.CornerRadius = radius < 1 ? new CornerRadius(0d) : new CornerRadius(radius);
            };
        }
    }
}
