using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace CODE.Framework.Wpf.Controls
{
    /// <summary>
    /// Abstract class used to define special window functions
    /// </summary>
    public abstract class SpecialWindowButton : Button
    {
        /// <summary>
        /// Returns a reference to the root window the button is in
        /// </summary>
        /// <value>The root window.</value>
        public Window RootWindow
        {
            get { return GetParentWindow(this); }
        }

        /// <summary>
        /// Gets the parent window.
        /// </summary>
        /// <param name="child">The child.</param>
        /// <returns>Window.</returns>
        public static Window GetParentWindow(DependencyObject child)
        {
            var parentObject = VisualTreeHelper.GetParent(child);
            if (parentObject == null) return null;

            var parent = parentObject as Window;
            return parent ?? GetParentWindow(parentObject);
        }
    }

    /// <summary>
    /// This button automatically minimizes the current window it is in
    /// </summary>
    public class MinimizeButton : SpecialWindowButton
    {
        /// <summary>
        /// Called when a <see cref="T:System.Windows.Controls.Button" /> is clicked.
        /// </summary>
        protected override void OnClick()
        {
            var window = RootWindow;
            if (window == null) return;

            window.WindowState = WindowState.Minimized;

            base.OnClick();
        }
    }

    /// <summary>
    /// This button automatically closes the current window it is in
    /// </summary>
    public class CloseButton : SpecialWindowButton
    {
        /// <summary>
        /// Called when a <see cref="T:System.Windows.Controls.Button" /> is clicked.
        /// </summary>
        protected override void OnClick()
        {
            var window = RootWindow;
            if (window == null) return;

            window.Close();

            base.OnClick();
        }
    }

    /// <summary>
    /// This button automatically toggles between normal and maximized states
    /// </summary>
    public class ToggleMaximizeButton : SpecialWindowButton
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ToggleMaximizeButton" /> class.
        /// </summary>
        public ToggleMaximizeButton()
        {
            Initialized += (s, e) =>
                {
                    var window = RootWindow;
                    if (window == null) return;
                    IsMaximized = window.WindowState == WindowState.Maximized;

                    window.StateChanged += (s2, e2) =>
                        {
                            var window2 = RootWindow;
                            if (window2 == null) return;
                            IsMaximized = window2.WindowState == WindowState.Maximized;
                        };
                };
        }

        /// <summary>
        /// Indicates whether the parent window is maximized
        /// </summary>
        public bool IsMaximized
        {
            get { return (bool)GetValue(IsMaximizedProperty); }
            set { SetValue(IsMaximizedProperty, value); }
        }
        /// <summary>
        /// Indicates whether the parent window is maximized
        /// </summary>
        public static readonly DependencyProperty IsMaximizedProperty = DependencyProperty.Register("IsMaximized", typeof(bool), typeof(ToggleMaximizeButton), new PropertyMetadata(false));

        /// <summary>
        /// Called when a <see cref="T:System.Windows.Controls.Button" /> is clicked.
        /// </summary>
        protected override void OnClick()
        {
            var window = RootWindow;
            if (window == null) return;

            window.WindowState = window.WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;

            base.OnClick();
        }        
    }
}
