using System;
using System.Windows;
using System.Windows.Controls;

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
                    host.Visibility = Visibility.Visible;
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
                    host._floatWindow = new Window {Title = host.Title, DataContext = host.DataContext};
                    if (host.FloatWindowStyle != null)
                        host._floatWindow.Style = host.FloatWindowStyle;
                    host._floatWindow.Closing += (o, e) =>
                    {
                        host._windowClosing = true;
                        host.IsDocked = true;
                        host._windowClosing = false;
                        host._floatWindow = null;
                    };
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

        private Window _floatWindow;
        private bool _windowClosing;

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
}
