using System.Windows;
using System.Windows.Controls;

namespace CODE.Framework.Wpf.Theme.Wildcat.Classes
{
    /// <summary>
    /// Special button class for the Wildcat theme
    /// </summary>
    public class WildcatButton : Button
    {
        /// <summary>Defines the position of a button in a group</summary>
        public static readonly DependencyProperty PositionProperty = DependencyProperty.RegisterAttached("Position", typeof(WildcatButtonPosition), typeof(WildcatButton), new PropertyMetadata(WildcatButtonPosition.Normal));
        /// <summary>Defines the position of a button in a group</summary>
        public static void SetPosition(DependencyObject obj, WildcatButtonPosition value)
        {
            obj.SetValue(PositionProperty, value);
        }
        /// <summary>Defines the position of a button in a group</summary>
        public static WildcatButtonPosition GetPosition(DependencyObject obj)
        {
            return (WildcatButtonPosition)obj.GetValue(PositionProperty);
        }
    }

    /// <summary>
    /// Defines where the button is displayed
    /// </summary>
    public enum WildcatButtonPosition
    {
        /// <summary>
        /// Normal, stand-alone button
        /// </summary>
        Normal,
        /// <summary>
        /// First button in a group
        /// </summary>
        First,
        /// <summary>
        /// Middle button in a group
        /// </summary>
        Middle,
        /// <summary>
        /// Last button in a group
        /// </summary>
        Last
    }
}
