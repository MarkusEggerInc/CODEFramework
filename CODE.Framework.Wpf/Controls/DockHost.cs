using System;
using System.Windows;
using System.Windows.Controls;
using CODE.Framework.Wpf.Layout;

namespace CODE.Framework.Wpf.Controls
{
    /// <summary>
    /// This content control can be used for content that can be undocked into a separate window
    /// </summary>
    public class DockHost : ContentControl
    {
        /// <summary>Indicates and sets whether the current content is displayed in-place/docked (true) or in a separate window (false)</summary>
        /// <value><c>true</c> if the content is docked; otherwise, <c>false</c>.</value>
        public bool IsDocked
        {
            get { return (bool)GetValue(IsDockedProperty); }
            set { SetValue(IsDockedProperty, value); }
        }

        /// <summary>Indicates and sets whether the current content is displayed in-place/docked (true) or in a separate window (false)</summary>
        /// <value><c>true</c> if the content is docked; otherwise, <c>false</c>.</value>
        public static readonly DependencyProperty IsDockedProperty = DependencyProperty.Register("IsDocked", typeof (bool), typeof (DockHost), new FrameworkPropertyMetadata(true, IsDockedChanged) {BindsTwoWayByDefault = true});
        /// <summary>Fires when IsDocked changes</summary>
        /// <param name="d">The dependency object.</param>
        /// <param name="args">The <see cref="DependencyPropertyChangedEventArgs"/> instance containing the event data.</param>
        private static void IsDockedChanged(DependencyObject d, DependencyPropertyChangedEventArgs args)
        {
            var host = d as DockHost;
            if (host == null) return;

            if ((bool)args.NewValue)
            {
                // Switching back into docked mode
                if (host._floatWindow == null) return;
                var content = host._floatWindow.Content;
                host._floatWindow.Content = null;
                if (content != null)
                {
                    host.Content = content;
                    host.Visibility = host.ContentVisibility;
                }
                if (!host._windowClosing)
                {
                    host._floatWindow.Close();
                    host._floatWindow = null;
                }
            }
            else
            {
                // Switching into float mode
                if (host._floatWindow == null)
                {
                    host._floatWindow = new FloatingDockWindow(host);
                    if (host.FloatWindowStyle != null)
                        host._floatWindow.Style = host.FloatWindowStyle;
                }

                var content = host.Content;
                host.Content = null;
                host.Visibility = Visibility.Collapsed;
                if (content != null)
                {
                    host._floatWindow.Content = content;
                    host._floatWindow.Show();
                }
            }
        }

        private FloatingDockWindow _floatWindow;

        /// <summary>
        /// Potential reference to the window used to host the floating content
        /// </summary>
        public FloatingDockWindow FloatWindow
        {
            get
            {
                return _floatWindow;
            }
            set
            {
                _floatWindow = value;
            }
        }

        private bool _windowClosing;

        /// <summary>
        /// True when the window is in the process of being closed
        /// </summary>
        /// <value><c>true</c> if [window closing]; otherwise, <c>false</c>.</value>
        public bool WindowClosing
        {
            get
            {
                return _windowClosing;
            }
            set
            {
                _windowClosing = value;
            }
        }

        /// <summary>Gets or sets the title.</summary>
        /// <value>The title.</value>
        public string Title
        {
            get { return (string)GetValue(TitleProperty); }
            set { SetValue(TitleProperty, value); }
        }
        /// <summary>The title property </summary>
        public static readonly DependencyProperty TitleProperty = DependencyProperty.Register("Title", typeof(string), typeof(DockHost), new PropertyMetadata("", TitleChanged));
        /// <summary>Fires when the title changes</summary>
        /// <param name="d">The dependency object.</param>
        /// <param name="args">The <see cref="DependencyPropertyChangedEventArgs"/> instance containing the event data.</param>
        private static void TitleChanged(DependencyObject d, DependencyPropertyChangedEventArgs args)
        {
            var host = d as DockHost;
            if (host == null) return;
            if (host._floatWindow != null) host._floatWindow.Title = args.NewValue.ToString();
        }

        /// <summary>Style for the floating window</summary>
        /// <value>The float window style.</value>
        public Style FloatWindowStyle
        {
            get { return (Style)GetValue(FloatWindowStyleProperty); }
            set { SetValue(FloatWindowStyleProperty, value); }
        }
        /// <summary>Style for the floating window</summary>
        /// <value>The float window style.</value>
        public static readonly DependencyProperty FloatWindowStyleProperty = DependencyProperty.Register("FloatWindowStyle", typeof(Style), typeof(DockHost), new PropertyMetadata(null));

        /// <summary>Defines whether the content is supposed to be visible (docked or not)</summary>
        /// <value>The content visibility.</value>
        public Visibility ContentVisibility
        {
            get { return (Visibility)GetValue(ContentVisibilityProperty); }
            set { SetValue(ContentVisibilityProperty, value); }
        }
        /// <summary>Defines whether the content is supposed to be visible (docked or not)</summary>
        /// <value>The content visibility.</value>
        public static readonly DependencyProperty ContentVisibilityProperty = DependencyProperty.Register("ContentVisibility", typeof(Visibility), typeof(DockHost), new PropertyMetadata(Visibility.Visible, ContentVisibilityChanged));

        /// <summary>
        /// Contents the visibility changed.
        /// </summary>
        /// <param name="d">The d.</param>
        /// <param name="args">The <see cref="DependencyPropertyChangedEventArgs"/> instance containing the event data.</param>
        private static void ContentVisibilityChanged(DependencyObject d, DependencyPropertyChangedEventArgs args)
        {
            var host = d as DockHost;
            if (host == null) return;

            if (host.IsDocked)
                // We are in standard mode, so we can simply switch visibility
                host.Visibility = (Visibility) args.NewValue;
            else
            {
                // We are in float mode, so we need to hide the window
                var newVisibility = (Visibility)args.NewValue;
                if (newVisibility == Visibility.Visible)
                {
                    if (host._floatWindow != null)
                        host._floatWindow.Show();
                    else
                    {
                        // We do not have a window yet, so we simply take the content back in place (this is unlikely to happen)
                        host.IsDocked = false;
                        host.Visibility = newVisibility;
                    }
                }
                else
                    // We are going into hiding, so we take things inline again and close the window
                    host.IsDocked = true;
            }
        }
    }

    /// <summary>
    /// Window class used by the DockHost to display undocked windows
    /// </summary>
    public class FloatingDockWindow : Window
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FloatingDockWindow"/> class.
        /// </summary>
        /// <param name="host">The host.</param>
        public FloatingDockWindow(DockHost host)
        {
            DataContext = host.DataContext;
            Title = host.Title;

            Closing += (o, e) =>
            {
                host.WindowClosing = true;
                if (!AutoDockOnClose)
                    host.ContentVisibility = Visibility.Collapsed; // Also causes the content to dock again... although it won't be visible at this point... but if the control becomes visible again, the content will be there again
                else 
                    host.IsDocked = true;
                host.WindowClosing = false;
                host.FloatWindow = null;
            };
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FloatingDockWindow" /> class.
        /// </summary>
        /// <param name="multiPanel">The multi panel parent container.</param>
        /// <param name="title">The title.</param>
        /// <param name="oldChildIndex">Old index of the child in the multi panel.</param>
        public FloatingDockWindow(MultiPanel multiPanel, string title, int oldChildIndex)
        {
            DataContext = multiPanel.DataContext;
            Title = title;

            Closing += (o, e) =>
            {
                if (AutoDockOnClose)
                {
                    var content = Content as UIElement;
                    Content = null;
                    if (content != null)
                    {
                        multiPanel.Children.Insert(oldChildIndex, content);
                        multiPanel.InvalidateArrange();
                        multiPanel.InvalidateMeasure();
                        multiPanel.InvalidateVisual();
                    }
                }
            };
        }

        /// <summary>
        /// Indicates whether clicking the close button will automatically dock the content back into the host
        /// </summary>
        public bool AutoDockOnClose
        {
            get { return (bool)GetValue(AutoDockOnCloseProperty); }
            set { SetValue(AutoDockOnCloseProperty, value); }
        }
        /// <summary>
        /// Indicates whether clicking the close button will automatically dock the content back into the host
        /// </summary>
        public static readonly DependencyProperty AutoDockOnCloseProperty = DependencyProperty.Register("AutoDockOnClose", typeof(bool), typeof(FloatingDockWindow), new PropertyMetadata(true));
    }

    /// <summary>
    /// Button to initiate a dock operation in the floating dock window
    /// </summary>
    public class DockWindowContentButton : SpecialWindowButton
    {
        /// <summary>
        /// Called when a <see cref="T:System.Windows.Controls.Button" /> is clicked.
        /// </summary>
        protected override void OnClick()
        {
            var window = RootWindow;
            if (window == null) return;
            var floatWindow = window as FloatingDockWindow;
            if (floatWindow != null)
                floatWindow.AutoDockOnClose = true; // Will cause a dock operation when the window closes

            window.Close();

            base.OnClick();
        }
    }
}
