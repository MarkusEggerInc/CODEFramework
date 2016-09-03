using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using CODE.Framework.Wpf.Layout;
using CODE.Framework.Wpf.Mvvm;

namespace CODE.Framework.Wpf.Theme.Universe.Controls
{
    /// <summary>
    /// Special button control used to switch the active view
    /// </summary>
    public class SelectOpenViewButton : Button
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SelectOpenViewButton"/> class.
        /// </summary>
        public SelectOpenViewButton()
        {
            Visibility = Visibility.Collapsed;
        }

        /// <summary>
        /// Parent tab control
        /// </summary>
        /// <value>The tab control.</value>
        public ShellTabControl TabControl
        {
            get { return (ShellTabControl) GetValue(TabControlProperty); }
            set { SetValue(TabControlProperty, value); }
        }

        /// <summary>
        /// Parent tab control
        /// </summary>
        public static readonly DependencyProperty TabControlProperty = DependencyProperty.Register("TabControl", typeof (ShellTabControl), typeof (SelectOpenViewButton), new PropertyMetadata(null, OnTabControlChanged));

        /// <summary>
        /// Handles the <see cref="E:TabControlChanged" /> event.
        /// </summary>
        /// <param name="d">The d.</param>
        /// <param name="e">The <see cref="DependencyPropertyChangedEventArgs"/> instance containing the event data.</param>
        private static void OnTabControlChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var button = d as SelectOpenViewButton;
            if (button == null) return;
            if (button.TabControl == null) return;

            button.TabControl.SelectionChanged += (s, e2) =>
            {
                button.Visibility = (button.TabControl.Items.Count > 1) ? Visibility.Visible : Visibility.Collapsed;
            };
        }

        /// <summary>
        /// Called when the button is clicked
        /// </summary>
        protected override void OnClick()
        {
            if (TabControl == null) return;

            var menu = new ContextMenu();
            var counter = -1;
            foreach (var view in Shell.Current.NormalViews)
            {
                counter++;
                var menuItem = new SelectViewMenuItem(TabControl, counter);
                menuItem.Header = SimpleView.GetTitle(view.View);
                var viewColor = SimpleView.GetViewThemeColor(view.View);
                if (viewColor.A == 0)
                {
                    var viewColorResource = FindResource("CODE.Framework-Application-ThemeColor1");
                    if (viewColorResource != null)
                        viewColor = (Color) viewColorResource;
                }
                if (viewColor.A != 0)
                    menuItem.Background = new SolidColorBrush(viewColor);
                menu.Items.Add(menuItem);
            }
            menu.Placement = PlacementMode.Bottom;
            menu.HorizontalOffset = ActualWidth;
            menu.PlacementTarget = this;
            menu.IsOpen = true;
        }
    }

    /// <summary>
    /// Menu item for view selection
    /// </summary>
    public class SelectViewMenuItem : MenuItem
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SelectViewMenuItem"/> class.
        /// </summary>
        /// <param name="tabControl">The tab control.</param>
        /// <param name="index">The index that is to be selected when this item is picked.</param>
        public SelectViewMenuItem(ShellTabControl tabControl, int index)
        {
            Click += (s, e) => { tabControl.SelectedIndex = index; };
        }
    }
}
