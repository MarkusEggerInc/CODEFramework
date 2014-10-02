using System.Windows;
using System.Windows.Controls;

namespace CODE.Framework.Wpf.Controls
{
    /// <summary>
    /// Extensions for list box items
    /// </summary>
    public class ListBoxItemEx : ListBoxItem
    {
        /// <summary>Indicates whether the list box item should automatically be considered selected when the focus moves to any of the controls within the item</summary>
        public static readonly DependencyProperty SelectItemWhenFocusWithinProperty = DependencyProperty.RegisterAttached("SelectItemWhenFocusWithin", typeof(bool), typeof(ListBoxItemEx), new UIPropertyMetadata(false, SelectItemwhenFocusWithinChanged));

        /// <summary>
        /// Selects the itemwhen focus within changed.
        /// </summary>
        /// <param name="o">The o.</param>
        /// <param name="args">The <see cref="System.Windows.DependencyPropertyChangedEventArgs"/> instance containing the event data.</param>
        private static void SelectItemwhenFocusWithinChanged(DependencyObject o, DependencyPropertyChangedEventArgs args)
        {
            var item = o as ListBoxItem;
            if (item == null) return;

            // This is a bit heavy handed, but it seems different scenarios fire different events, so I (Markus) added all these to account for various scenarios

            item.PreviewMouseDown += (s, e) =>
                                         {
                                             if (!GetSelectItemWhenFocusWithin(item)) return;
                                             if (!item.IsSelected)
                                                 item.IsSelected = true;
                                         };

            item.MouseDown += (s, e) =>
                                  {
                                      if (!GetSelectItemWhenFocusWithin(item)) return;
                                      if (!item.IsSelected)
                                          item.IsSelected = true;
                                  };

            item.PreviewGotKeyboardFocus += (s, e) =>
                                                {
                                                    if (!GetSelectItemWhenFocusWithin(item)) return;
                                                    if (!item.IsSelected)
                                                        item.IsSelected = true;
                                                };

            item.GotKeyboardFocus += (s, e) =>
                                         {
                                             if (!GetSelectItemWhenFocusWithin(item)) return;
                                             if (!item.IsSelected)
                                                 item.IsSelected = true;
                                         };

            item.GotFocus += (s, e) =>
                                 {
                                     if (!GetSelectItemWhenFocusWithin(item)) return;
                                     if (!item.IsSelected)
                                         item.IsSelected = true;
                                 };

            item.IsKeyboardFocusWithinChanged += (s, e) =>
                                                     {
                                                         if (!GetSelectItemWhenFocusWithin(item)) return;
                                                         if (item.IsKeyboardFocusWithin && !item.IsSelected)
                                                             item.IsSelected = true;
                                                     };
        }

        /// <summary>Indicates whether the list box item should automatically be considered selected when the focus moves to any of the controls within the item</summary>
        /// <param name="o">The object to set the value on.</param>
        /// <returns></returns>
        public static bool GetSelectItemWhenFocusWithin(DependencyObject o)
        {
            return (bool)o.GetValue(SelectItemWhenFocusWithinProperty);
        }
        /// <summary>
        /// Indicates whether the list box item should automatically be considered selected when the focus moves to any of the controls within the item
        /// </summary>
        /// <param name="o">The object to set the value on.</param>
        /// <param name="value">if set to <c>true</c> [value].</param>
        public static void SetSelectItemWhenFocusWithin(DependencyObject o, bool value)
        {
            o.SetValue(SelectItemWhenFocusWithinProperty, value);
        }
    }
}
