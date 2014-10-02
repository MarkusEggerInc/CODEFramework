using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using CODE.Framework.Wpf.Utilities;

namespace CODE.Framework.Wpf.Mvvm
{
    /// <summary>
    /// Special tab control used to host views (typically normal views in the shell)
    /// </summary>
    /// <remarks>Designed for internal use only</remarks>
    public class ViewHostTabControl : TabControl
    {
        /// <summary>Defines whether tab headers shall be displayed</summary>
        /// <remarks>It is up to each theme to respect this property</remarks>
        public bool ShowHeaders
        {
            get { return (bool)GetValue(ShowHeadersProperty); }
            set { SetValue(ShowHeadersProperty, value); }
        }
        /// <summary>Defines whether tab headers shall be displayed</summary>
        /// <remarks>It is up to each theme to respect this property</remarks>
        public static readonly DependencyProperty ShowHeadersProperty = DependencyProperty.Register("ShowHeaders", typeof(bool), typeof(ViewHostTabControl), new PropertyMetadata(true));

        /// <summary>Main model (typically the start view model)</summary>
        public object MainModel
        {
            get { return (object)GetValue(MainModelProperty); }
            set { SetValue(MainModelProperty, value); }
        }
        /// <summary>Main model (typically the start view model)</summary>
        public static readonly DependencyProperty MainModelProperty = DependencyProperty.Register("MainModel", typeof(object), typeof(ViewHostTabControl), new PropertyMetadata(null));
    }

    /// <summary>
    /// Special tab control class for top-level views
    /// </summary>
    public class TopLevelViewHostTabControl : ViewHostTabControl
    {
        /// <summary>Link to a normal view host, which can potentially be disabled whenever top level views are active</summary>
        public UIElement NormalViewHost
        {
            get { return (UIElement)GetValue(NormalViewHostProperty); }
            set { SetValue(NormalViewHostProperty, value); }
        }

        /// <summary>Link to a normal view host, which can potentially be disabled whenever top level views are active</summary>
        public static readonly DependencyProperty NormalViewHostProperty = DependencyProperty.Register("NormalViewHost", typeof (UIElement), typeof (TopLevelViewHostTabControl), new FrameworkPropertyMetadata(null) {BindsTwoWayByDefault = false});

        /// <summary>Defines whether normal views should be disabled when top level views are active</summary>
        public bool DisableNormalViewHostWhenTopLevelIsActive
        {
            get { return (bool)GetValue(DisableNormalViewHostWhenTopLevelIsActiveProperty); }
            set { SetValue(DisableNormalViewHostWhenTopLevelIsActiveProperty, value); }
        }
        /// <summary>Defines whether normal views should be disabled when top level views are active</summary>
        public static readonly DependencyProperty DisableNormalViewHostWhenTopLevelIsActiveProperty = DependencyProperty.Register("DisableNormalViewHostWhenTopLevelIsActive", typeof(bool), typeof(TopLevelViewHostTabControl), new PropertyMetadata(true));

        /// <summary>InputBindingsSet</summary>
        /// <param name="obj">Object to get value from</param>
        /// <returns>True/False</returns>
        public static bool GetInputBindingsSet(DependencyObject obj)
        {
            return (bool)obj.GetValue(InputBindingsSetProperty);
        }

        /// <summary>InputBindingsSet</summary>
        /// <param name="obj">Object to set the value on</param>
        /// <param name="value">Value to set</param>
        public static void SetInputBindingsSet(DependencyObject obj, bool value)
        {
            obj.SetValue(InputBindingsSetProperty, value);
        }

        /// <summary>Indicates whether input bindings have been set on a certain control of interest</summary>
        public static readonly DependencyProperty InputBindingsSetProperty = DependencyProperty.RegisterAttached("InputBindingsSet", typeof(bool), typeof(TopLevelViewHostTabControl), new PropertyMetadata(false));

        /// <summary>Called to update the current selection when items change.</summary>
        /// <param name="e">The event data for the System.Windows.Controls.ItemContainerGenerator.ItemsChanged event</param>
        protected override void OnItemsChanged(NotifyCollectionChangedEventArgs e)
        {
            base.OnItemsChanged(e);

            if (NormalViewHost != null && DisableNormalViewHostWhenTopLevelIsActive)
                NormalViewHost.IsEnabled = Items.Count < 1;
        }

        /// <summary>Raises the System.Windows.Controls.Primitives.Selector.SelectionChanged routed event</summary>
        /// <param name="e">Provides data for System.Windows.Controls.SelectionChangedEventArgs.</param>
        protected override void OnSelectionChanged(SelectionChangedEventArgs e)
        {
            base.OnSelectionChanged(e);

            var result = SelectedContent as ViewResult;
            if (result == null || result.View == null) return;

            FocusHelper.FocusDelayed(this);

            InputBindings.Clear();
            foreach (InputBinding binding in result.View.InputBindings)
                InputBindings.Add(binding);
        }
    }
}
