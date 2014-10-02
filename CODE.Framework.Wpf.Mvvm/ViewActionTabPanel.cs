using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace CODE.Framework.Wpf.Mvvm
{
    /// <summary>
    /// Tab Panel class that can show both current tab elements as well as view actions
    /// </summary>
    public class ViewActionTabPanel : TabPanel 
    {
        /// <summary>
        /// Model object (should implement IHaveActions)
        /// </summary>
        /// <value>The actions.</value>
        public object Actions
        {
            get { return GetValue(ActionsProperty); }
            set { SetValue(ActionsProperty, value); }
        }
        /// <summary>
        /// Model object (should implement IHaveActions)
        /// </summary>
        public static readonly DependencyProperty ActionsProperty = DependencyProperty.Register("Actions", typeof(object), typeof(ViewActionTabPanel), new PropertyMetadata(null, OnActionsChanged));

        /// <summary>
        /// Fires when the actions property changes
        /// </summary>
        /// <param name="d">Source</param>
        /// <param name="args">Arguments</param>
        private static void OnActionsChanged(DependencyObject d, DependencyPropertyChangedEventArgs args)
        {
            var panel = d as ViewActionTabPanel;
            if (panel == null) return;
            panel.RepopulateItems();
        }

        /// <summary>
        /// Tab control from which the items collection is populated
        /// </summary>
        public ViewHostTabControl ParentTabControl
        {
            get { return (ViewHostTabControl)GetValue(ParentTabControlProperty); }
            set { SetValue(ParentTabControlProperty, value); }
        }

        /// <summary>
        /// Tab control from which the items collection is populated
        /// </summary>
        public static readonly DependencyProperty ParentTabControlProperty = DependencyProperty.Register("ParentTabControl", typeof(ViewHostTabControl), typeof(ViewActionTabPanel), new PropertyMetadata(null, OnParentTabControlChanged));

        /// <summary>
        /// Fires when the parent tab control changes
        /// </summary>
        /// <param name="d">Source</param>
        /// <param name="args">Arguments</param>
        private static void OnParentTabControlChanged(DependencyObject d, DependencyPropertyChangedEventArgs args)
        {
            var panel = d as ViewActionTabPanel;
            if (panel == null) return;
            var control = args.NewValue as ViewHostTabControl;
            if (control == null) return;

            var pd = DependencyPropertyDescriptor.FromProperty(ItemsControl.ItemsSourceProperty, typeof(TabControl));
            pd.AddValueChanged(control, (s, e) =>
            {
                if (control.ItemsSource != null)
                {
                    var items = control.ItemsSource as ObservableCollection<ViewResult>;
                    if (items != null)
                        items.CollectionChanged += (o2, a2) => panel.RepopulateItems();
                }
                panel.RepopulateItems();
            });

            if (control.ItemsSource != null)
            {
                var items = control.ItemsSource as ObservableCollection<ViewResult>;
                if (items != null)
                    items.CollectionChanged += (o, a) => panel.RepopulateItems();
            }

            panel.RepopulateItems();
        }


        /// <summary>
        /// Populates the children from a combination of actions and tab items
        /// </summary>
        private void RepopulateItems()
        {
            Children.Clear();

            if (ParentTabControl != null)
            {
                if (ParentTabControl.Items.Count > 0)
                {
                    ParentTabControl.SelectedItem = ParentTabControl.Items[0];
                    ParentTabControl.SelectedValue = ParentTabControl.Items[0];
                }
            }
        }
    }
}
