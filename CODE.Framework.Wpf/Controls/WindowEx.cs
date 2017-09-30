using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing.Printing;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using CODE.Framework.Core.Utilities;
using CODE.Framework.Wpf.Utilities;

namespace CODE.Framework.Wpf.Controls
{
    /// <summary>
    /// Special window class with various (attached) properties that provide special capabilities
    /// </summary>
    public class WindowEx : Window
    {
        /// <summary>
        /// Creates a stylable version of the window startup position (the default property on the Window class is not a dependency property)
        /// </summary>
        public static readonly DependencyProperty WindowStartupLocationStylableProperty = DependencyProperty.RegisterAttached("WindowStartupLocationStylable", typeof(WindowStartupLocation), typeof(WindowEx), new PropertyMetadata(WindowStartupLocation.Manual, OnWindowStartupLocationStylableChanged));

        /// <summary>
        /// Fires when the WindowStartupPosition attached property changes
        /// </summary>
        /// <param name="d">The d.</param>
        /// <param name="e">The <see cref="DependencyPropertyChangedEventArgs"/> instance containing the event data.</param>
        private static void OnWindowStartupLocationStylableChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var window = d as Window;
            if (window == null) return;
            window.WindowStartupLocation = (WindowStartupLocation) e.NewValue;
        }

        /// <summary>
        /// Creates a stylable version of the window startup position (the default property on the Window class is not a dependency property)
        /// </summary>
        public static WindowStartupLocation GetWindowStartupLocationStylable(DependencyObject obj)
        {
            return (WindowStartupLocation)obj.GetValue(WindowStartupLocationStylableProperty);
        }

        /// <summary>
        /// Creates a stylable version of the window startup position (the default property on the Window class is not a dependency property)
        /// </summary>
        public static void SetWindowStartupLocationStylable(DependencyObject obj, WindowStartupLocation value)
        {
            obj.SetValue(WindowStartupLocationStylableProperty, value);
        }

        /// <summary>
        /// If set to true, the Window will remember the last size and position it had and re-open at that same position
        /// </summary>
        public static readonly DependencyProperty AutoSaveWindowPositionProperty = DependencyProperty.RegisterAttached("AutoSaveWindowPosition", typeof(bool), typeof(WindowEx), new PropertyMetadata(false, OnAutoSaveWindowPositionChanged));

        /// <summary>
        /// If set to true, the Window will remember the last size and position it had and re-open at that same position
        /// </summary>
        public static bool GetAutoSaveWindowPosition(DependencyObject obj)
        {
            return (bool)obj.GetValue(AutoSaveWindowPositionProperty);
        }

        /// <summary>
        /// If set to true, the Window will remember the last size and position it had and re-open at that same position
        /// </summary>
        public static void SetAutoSaveWindowPosition(DependencyObject obj, bool value)
        {
            obj.SetValue(AutoSaveWindowPositionProperty, value);
        }

        /// <summary>
        /// Fires when the AutoSaveWindowPosition property is set
        /// </summary>
        /// <param name="d">The d.</param>
        /// <param name="e">The <see cref="DependencyPropertyChangedEventArgs"/> instance containing the event data.</param>
        private static void OnAutoSaveWindowPositionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var window = d as Window;
            if (window == null) return;
            if (!(bool) e.NewValue) return;

            SettingsManager.RegisterSerializer<WindowPositionSettingsSerializer>();
            SettingsManager.LoadSettings(window);
            window.Closing += (s2, e2) => { SettingsManager.SaveSettings(window, serializerTypeFilter: typeof (WindowPositionSettingsSerializer), includeDerivedTypeFilterTypes: true); };
        }

        /// <summary>If set to true, the window can be dragged automatically by clicking in the background or header area</summary>
        /// <remarks>If the HeaderHeight property is set to anything larger than 0, only clicks within the header area will be considered for dragging.</remarks>
        public static readonly DependencyProperty AutoWindowDragEnabledProperty = DependencyProperty.RegisterAttached("AutoWindowDragEnabled", typeof (bool), typeof (WindowEx), new PropertyMetadata(false, AutoWindowDragEnabledChanged));

        /// <summary>If set to true, the window can be dragged automatically by clicking in the background or header area</summary>
        public static bool GetAutoWindowDragEnabled(DependencyObject obj)
        {
            return (bool)obj.GetValue(AutoWindowDragEnabledProperty);
        }

        /// <summary>If set to true, the window can be dragged automatically by clicking in the background or header area</summary>
        public static void SetAutoWindowDragEnabled(DependencyObject obj, bool value)
        {
            obj.SetValue(AutoWindowDragEnabledProperty, value);
        }

        /// <summary>Handler for auto-windows-drag-enabled changes</summary>
        /// <param name="dependencyObject">The dependency object.</param>
        /// <param name="dependencyPropertyChangedEventArgs">The <see cref="DependencyPropertyChangedEventArgs" /> instance containing the event data.</param>
        private static void AutoWindowDragEnabledChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs dependencyPropertyChangedEventArgs)
        {
            If.Real<Window>(dependencyObject, win =>
            {
                if (GetAutoWindowDragEnabled(win))
                {
                    win.MouseLeftButtonDown += WindowDragMouseDownHandler;
                    win.MouseMove += WindowDragMoveHandler;
                    win.MouseLeftButtonUp += WindowDragMouseUpHandler;
                }
                else
                {
                    win.MouseLeftButtonDown -= WindowDragMouseDownHandler;
                    win.MouseMove -= WindowDragMoveHandler;
                    win.MouseLeftButtonUp -= WindowDragMouseUpHandler;
                }
            });
        }

        private static void WindowDragMouseUpHandler(object sender, MouseButtonEventArgs e)
        {
            _currentDisplays = null;
            var window = sender as Window;
            if (window == null) return;
            Mouse.Capture(null);
            SetCurrentDragOperationStartScreenPosition(window, new Point(double.MinValue, double.MinValue));
            SetCurrentDragOperationStartWindowPosition(window, new Point(double.MinValue, double.MinValue));
        }

        private static List<DisplayInformation> _currentDisplays;

        private static void WindowDragMoveHandler(object sender, MouseEventArgs e)
        {
            if (e.LeftButton != MouseButtonState.Pressed) return;
            var window = sender as Window;
            if (window == null) return;

            var startAbsoluteMousePosition = GetCurrentDragOperationStartScreenPosition(window);
            if (!(startAbsoluteMousePosition.X > double.MinValue)) return; // Not in a window drag operation

            // We now we are in a drag operation, so we have to first know how far the mouse has moved
            var currentAbsoluteMousePosition = GetMousePositionInScreen(window);
            var deltaX = startAbsoluteMousePosition.X - currentAbsoluteMousePosition.X;
            var deltaY = startAbsoluteMousePosition.Y - currentAbsoluteMousePosition.Y;
            var deltaXAbsolute = Math.Abs(deltaX);
            var deltaYAbsolute = Math.Abs(deltaY);

            // We may have to change the window state to normal if it is currently maximized
            if (window.WindowState == WindowState.Maximized)
                foreach (var display in _currentDisplays)
                    if (display.MonitorArea.Contains(currentAbsoluteMousePosition))
                        if ((deltaXAbsolute > SystemParameters.MinimumHorizontalDragDistance || deltaYAbsolute > SystemParameters.MinimumVerticalDragDistance) && currentAbsoluteMousePosition.Y > display.MonitorArea.Top + SystemParameters.MinimumHorizontalDragDistance)
                        {
                            // The user has tried to move the window, so we need to switch it out of maximized state
                            window.WindowState = WindowState.Normal;

                            window.Top = startAbsoluteMousePosition.Y*-1;
                            if (window.Top > currentAbsoluteMousePosition.Y - 10) window.Top = currentAbsoluteMousePosition.Y - 10;
                            if (window.Left > currentAbsoluteMousePosition.X) window.Left = currentAbsoluteMousePosition.X - (window.Width/2);
                            if (window.Left + window.Width < currentAbsoluteMousePosition.X) window.Left = currentAbsoluteMousePosition.X - (window.Width/2);
                            SetCurrentDragOperationStartWindowPosition(window, new Point(window.Left, window.Top));
                            SetCurrentDragOperationStartScreenPosition(window, currentAbsoluteMousePosition);
                            return;
                        }

            foreach (var display in _currentDisplays)
                if (display.MonitorArea.Contains(currentAbsoluteMousePosition))
                    if (window.WindowState == WindowState.Normal && currentAbsoluteMousePosition.Y < display.MonitorArea.Top + SystemParameters.MinimumVerticalDragDistance && currentAbsoluteMousePosition.Y >= display.MonitorArea.Top)
                        // The user has snapped the window to the top of the screen it is in, so we maximize it (but only for windows that support resizing)
                        if (GetAutoWindowResizingEnabled(window))
                        {
                            window.WindowState = WindowState.Maximized;
                            return;
                        }

            // No special mode interfered, so we simply move the window
            var originalWindowPosition = GetCurrentDragOperationStartWindowPosition(window);
            if (!(originalWindowPosition.X > double.MinValue)) return;
            var windowTop = originalWindowPosition.Y - deltaY;
            if (windowTop < -10) windowTop = -10;
            window.Top = windowTop;
            window.Left = originalWindowPosition.X - deltaX;
        }

        /// <summary>Handler for window drag operations</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="mouseButtonEventArgs">The <see cref="MouseButtonEventArgs" /> instance containing the event data.</param>
        private static void WindowDragMouseDownHandler(object sender, MouseButtonEventArgs mouseButtonEventArgs)
        {
            if (mouseButtonEventArgs.LeftButton != MouseButtonState.Pressed) return;
            var window = sender as Window;
            if (window == null) return;

            // Get the mouse position within the current window
            var position = mouseButtonEventArgs.GetPosition(window);
            _currentDisplays = GetAllDisplays();

            if (IsMouseActionInHeader(window, position))
                if (!MustHandleResize(position, window)) // This second check is to make sure we do not interfere with auto resizing
                {
                    // We also check the position on the screen
                    SetCurrentDragOperationStartScreenPosition(window, GetMousePositionInScreen(window));
                    SetCurrentDragOperationStartWindowPosition(window, new Point(window.Left, window.Top));
                    Mouse.Capture(window);
                    if (!window.IsActive) window.Activate();
                    //window.DragMove();
                }
        }

        private static Point GetMousePositionInScreen(Visual visual)
        {
            var scale = GetScreenScale(visual);
            var w32Mouse = new Win32Point();
            GetCursorPos(ref w32Mouse);
            return new Point(w32Mouse.X/scale.X, w32Mouse.Y/scale.Y);
        }

        /// <summary>For internal use only</summary>
        [Browsable(false)]
        public static readonly DependencyProperty CurrentDragOperationStartScreenPositionProperty = DependencyProperty.RegisterAttached("CurrentDragOperationStartScreenPosition", typeof(Point), typeof(WindowEx), new PropertyMetadata(new Point(double.MinValue, double.MinValue)));

        /// <summary>For internal use only</summary>
        [Browsable(false)]
        public static Point GetCurrentDragOperationStartScreenPosition(DependencyObject obj)
        {
            return (Point)obj.GetValue(CurrentDragOperationStartScreenPositionProperty);
        }
        
        /// <summary>For internal use only</summary>
        [Browsable(false)]
        public static void SetCurrentDragOperationStartScreenPosition(DependencyObject obj, Point value)
        {
            obj.SetValue(CurrentDragOperationStartScreenPositionProperty, value);
        }

        /// <summary>For internal use only</summary>
        [Browsable(false)]
        public static readonly DependencyProperty CurrentDragOperationStartWindowPositionProperty = DependencyProperty.RegisterAttached("CurrentDragOperationStartWindowPosition", typeof(Point), typeof(WindowEx), new PropertyMetadata(new Point(double.MinValue, double.MinValue)));

        /// <summary>For internal use only</summary>
        [Browsable(false)]
        public static Point GetCurrentDragOperationStartWindowPosition(DependencyObject obj)
        {
            return (Point)obj.GetValue(CurrentDragOperationStartWindowPositionProperty);
        }

        /// <summary>For internal use only</summary>
        [Browsable(false)]
        public static void SetCurrentDragOperationStartWindowPosition(DependencyObject obj, Point value)
        {
            obj.SetValue(CurrentDragOperationStartWindowPositionProperty, value);
        }

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool GetCursorPos(ref Win32Point point);

        [DllImport("user32.dll")]
        static extern bool GetMonitorInfo(IntPtr hMonitor, ref MonitorInformation lpmi);

        [DllImport("user32.dll")]
        private static extern bool EnumDisplayMonitors(IntPtr hdc, IntPtr lprcClip, EnumMonitorsDelegate lpfnEnum, IntPtr dwData);
        private delegate bool EnumMonitorsDelegate(IntPtr hMonitor, IntPtr hdcMonitor, ref Rect lprcMonitor, IntPtr dwData);

        [StructLayout(LayoutKind.Sequential)]
        private struct MonitorInformation
        {
            public int cbSize;
            public RECT rcMonitor;
            public RECT rcWork;
            public uint dwFlags;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int left;
            public int top;
            public int right;
            public int bottom;
        }

        /// <summary>
        /// Gets a list of all monitors and returns detailed monitor information
        /// </summary>
        private static List<DisplayInformation> GetAllDisplays()
        {
            var displays = new List<DisplayInformation>();

            EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero,
                (IntPtr hMonitor, IntPtr hdcMonitor, ref Rect lprcMonitor, IntPtr dwData) =>
                {
                    var mi = new MonitorInformation();
                    mi.cbSize = Marshal.SizeOf(mi);
                    if (GetMonitorInfo(hMonitor, ref mi))
                        displays.Add(new DisplayInformation
                        {
                            ScreenWidth = mi.rcMonitor.right - mi.rcMonitor.left,
                            ScreenHeight = mi.rcMonitor.bottom - mi.rcMonitor.top,
                            MonitorArea = new Rect(mi.rcMonitor.left, mi.rcMonitor.top, mi.rcMonitor.right - mi.rcMonitor.left, mi.rcMonitor.bottom - mi.rcMonitor.top),
                            WorkArea = new Rect(mi.rcWork.left, mi.rcWork.top, mi.rcWork.right - mi.rcWork.left, mi.rcWork.bottom - mi.rcWork.top),
                            Availability = mi.dwFlags.ToString()
                        });
                    return true;
                }, IntPtr.Zero);

            return displays;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct Win32Point
        {
            public int X;
            public int Y;
        };

        /// <summary>If set to true, the window automatically maximizes when double-clicked</summary>
        /// <remarks>If the HeaderHeight property is set to anything larger than 0, only clicks within the header area will be considered for dragging.</remarks>
        public static readonly DependencyProperty AutoWindowMaximizeEnabledProperty = DependencyProperty.RegisterAttached("AutoWindowMaximizeEnabled", typeof (bool), typeof (WindowEx), new PropertyMetadata(false, AutoWindowMaximizeEnabledChanged));

        /// <summary>Handler for auto-windows-maximize changes</summary>
        /// <param name="dependencyObject">The dependency object.</param>
        /// <param name="dependencyPropertyChangedEventArgs">The <see cref="DependencyPropertyChangedEventArgs" /> instance containing the event data.</param>
        private static void AutoWindowMaximizeEnabledChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs dependencyPropertyChangedEventArgs)
        {
            If.Real<Window>(dependencyObject, win =>
            {
                if (GetAutoWindowMaximizeEnabled(win))
                    win.MouseDoubleClick += WindowMaximizeHandler;
                else
                    win.MouseDoubleClick -= WindowMaximizeHandler;
            });
        }

        /// <summary>Handler for window maximize operations</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="mouseButtonEventArgs">The <see cref="MouseButtonEventArgs" /> instance containing the event data.</param>
        private static void WindowMaximizeHandler(object sender, MouseButtonEventArgs mouseButtonEventArgs)
        {
            var window = sender as Window;
            if (window == null) return;
            if (!GetAutoWindowMaximizeEnabled(window)) return; // Just doublec-checking to make sure this is still on

            // We need to make sure that the click actually happened on the window (or part of the window template) and not in a different control
            var originalSource = mouseButtonEventArgs.OriginalSource as FrameworkElement;
            if (originalSource != null && originalSource != window)
            {
                var clickRoot = FindRoot(originalSource);
                if (clickRoot == null) return; // This couldn't possibly be a window
                if (clickRoot.TemplatedParent != null) clickRoot = clickRoot.TemplatedParent as FrameworkElement;
                if (clickRoot != window) return; // Click happened on something else
            }

            if (IsMouseActionInHeader(window, mouseButtonEventArgs.GetPosition(window)))
                window.WindowState = window.WindowState == WindowState.Normal ? WindowState.Maximized : WindowState.Normal;
        }

        private static FrameworkElement FindRoot(FrameworkElement element)
        {
            while (element.Parent != null)
            {
                element = element.Parent as FrameworkElement;
                if (element == null) return null;
            }
            return element;
        }

        /// <summary>If set to true, the window can be dragged automatically by clicking in the background or header area</summary>
        public static bool GetAutoWindowMaximizeEnabled(DependencyObject obj)
        {
            return (bool) obj.GetValue(AutoWindowMaximizeEnabledProperty);
        }

        /// <summary>If set to true, the window can be dragged automatically by clicking in the background or header area</summary>
        public static void SetAutoWindowMaximizeEnabled(DependencyObject obj, bool value)
        {
            obj.SetValue(AutoWindowMaximizeEnabledProperty, value);
        }

        /// <summary>
        /// Defines the area at the top of the window that is considered the "Header". 
        /// This is the area of the screen that drag and double-click operations will be handled in
        /// If this is 0 or less, then the whole window is drag and double-click enabled
        /// </summary>
        public static readonly DependencyProperty HeaderHeightProperty = DependencyProperty.RegisterAttached("HeaderHeight", typeof (double), typeof (WindowEx), new UIPropertyMetadata(0d));

        /// <summary>Gets the height of the header.</summary>
        /// <param name="obj">Dependency object to set the value on</param>
        /// <returns></returns>
        public static double GetHeaderHeight(DependencyObject obj)
        {
            return (double) obj.GetValue(HeaderHeightProperty);
        }

        /// <summary>Sets the height of the header.</summary>
        /// <param name="obj">Dependency object to set the value on</param>
        /// <param name="value">The value.</param>
        public static void SetHeaderHeight(DependencyObject obj, double value)
        {
            obj.SetValue(HeaderHeightProperty, value);
        }

        /// <summary>Determines whether an action taken with the mouse is within the area we consider to be the "header".</summary>
        /// <param name="window">The window.</param>
        /// <param name="mousePosition">The mouse position.</param>
        /// <returns>True or false</returns>
        private static bool IsMouseActionInHeader(Window window, Point mousePosition)
        {
            if (window == null) return false;
            var headerHeight = GetHeaderHeight(window);
            if (headerHeight <= 0) return true;
            return mousePosition.Y <= headerHeight;
        }


        /// <summary>If true, the window allows resizing, even if the window is a borderless window</summary>
        public static readonly DependencyProperty AutoWindowResizingEnabledProperty = DependencyProperty.RegisterAttached("AutoWindowResizingEnabled", typeof (bool), typeof (WindowEx), new PropertyMetadata(false, AutoWindowResizingEnabledChanged));

        /// <summary>Change event handler for auto resizing</summary>
        /// <param name="dependencyObject">Dependency object</param>
        /// <param name="dependencyPropertyChangedEventArgs">Event args</param>
        private static void AutoWindowResizingEnabledChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs dependencyPropertyChangedEventArgs)
        {
            If.Real<Window>(dependencyObject, win =>
            {
                if (GetAutoWindowResizingEnabled(win))
                {
                    win.MouseMove += WindowResizeMouseOverHandler;
                    win.MouseLeftButtonDown += WindowResizeMouseDownHandler;
                }
                else
                {
                    win.MouseMove -= WindowResizeMouseOverHandler;
                    win.MouseLeftButtonDown -= WindowResizeMouseDownHandler;
                }
                win.MouseUp += (s, a) =>
                {
                    // Makes sure that when the mouse is released, the resize mover event is disconnected as well
                    var win2 = Mouse.Captured as Window;
                    if (win2 != null)
                        Mouse.Capture(null);
                    win.MouseMove -= WindowResizeMouseDownMoveHandler;
                };
                win.Deactivated += (s2, a2) =>
                {
                    // Makes sure that when the mouse is released, the resize mover event is disconnected as well
                    var win2 = Mouse.Captured as Window;
                    if (win2 != null)
                        Mouse.Capture(null);
                    win.MouseMove -= WindowResizeMouseDownMoveHandler;
                };
            });
        }

        /// <summary>Window icon path (set dynamically, which means it doesn't have to exist during design time)</summary>
        public static readonly DependencyProperty DynamicIconProperty = DependencyProperty.RegisterAttached("DynamicIcon", typeof (string), typeof (WindowEx), new PropertyMetadata("", DynamicIconChanged));

        /// <summary>
        /// Fires when the dynamic icon changes
        /// </summary>
        /// <param name="d">The d.</param>
        /// <param name="args">The <see cref="DependencyPropertyChangedEventArgs" /> instance containing the event data.</param>
        /// <exception cref="System.NotImplementedException"></exception>
        private static void DynamicIconChanged(DependencyObject d, DependencyPropertyChangedEventArgs args)
        {
            var window = d as Window;
            if (window == null) return;
            var iconPath = args.NewValue.ToString();

            ImageSource source;
            try
            {
                source = new BitmapImage(new Uri("pack://application:,,," + iconPath, UriKind.RelativeOrAbsolute));
                var height = source.Height; // If this fails, the image is invalid
            }
            catch (Exception)
            {
                try
                {
                    source = new BitmapImage(new Uri(iconPath, UriKind.RelativeOrAbsolute));
                    var height = source.Height; // If this fails, the image is invalid
                }
                catch (Exception)
                {
                    source = null;
                }
            }

            if (source != null)
                window.Icon = source;
        }

        /// <summary>Window icon path (set dynamically, which means it doesn't have to exist during design time)</summary>
        public static string GetDynamicIcon(DependencyObject obj)
        {
            return (string) obj.GetValue(DynamicIconProperty);
        }

        /// <summary>Window icon path (set dynamically, which means it doesn't have to exist during design time)</summary>
        public static void SetDynamicIcon(DependencyObject obj, string value)
        {
            obj.SetValue(DynamicIconProperty, value);
        }

        /// <summary>Indicates whether the window should switch into special borderless mode</summary>
        public static readonly DependencyProperty IsBorderlessProperty = DependencyProperty.RegisterAttached("IsBorderless", typeof (bool), typeof (WindowEx), new PropertyMetadata(false, IsBorderlessChanged));

        /// <summary>Fires when the IsBorderless property changess</summary>
        /// <param name="d">The dependency object.</param>
        /// <param name="args">The <see cref="DependencyPropertyChangedEventArgs" /> instance containing the event data.</param>
        private static void IsBorderlessChanged(DependencyObject d, DependencyPropertyChangedEventArgs args)
        {
            var window = d as Window;
            if (window == null) return;

            if ((bool) args.NewValue)
            {
                window.WindowStyle = WindowStyle.None;
                window.ResizeMode = ResizeMode.NoResize;

                window.SourceInitialized += (o, e) =>
                {
                    if (!GetIsBorderless(window)) return;
                    var handle = (new WindowInteropHelper(window)).Handle;
                    var hwndSource = HwndSource.FromHwnd(handle);
                    if (hwndSource != null) hwndSource.AddHook(WindowHook);
                };
            }
            else
            {
                window.WindowStyle = (WindowStyle) WindowStyleProperty.GetMetadata(typeof (Window)).DefaultValue;
                window.ResizeMode = (ResizeMode) ResizeModeProperty.GetMetadata(typeof (Window)).DefaultValue;
            }
        }

        /// <summary>Indicates whether the window should switch into special borderless mode</summary>
        public static bool GetIsBorderless(DependencyObject obj)
        {
            return (bool) obj.GetValue(IsBorderlessProperty);
        }

        /// <summary>Indicates whether the window should switch into special borderless mode</summary>
        public static void SetIsBorderless(DependencyObject obj, bool value)
        {
            obj.SetValue(IsBorderlessProperty, value);
        }

        /// <summary>
        /// Indicates whether and when the window shall have a drop shaddow
        /// </summary>
        public static readonly DependencyProperty HasDropShadowProperty = DependencyProperty.RegisterAttached("HasDropShadow", typeof (EffectActiveStates), typeof (WindowEx), new PropertyMetadata(EffectActiveStates.Never, HasDropShadowChanged));

        /// <summary>
        /// Fires when HasDropShadow changes
        /// </summary>
        /// <param name="source">The dependency object.</param>
        /// <param name="args">The <see cref="DependencyPropertyChangedEventArgs"/> instance containing the event data.</param>
        private static void HasDropShadowChanged(DependencyObject source, DependencyPropertyChangedEventArgs args)
        {
            var window = source as Window;
            if (window == null) return;
            var effectState = (EffectActiveStates) args.NewValue;

            window.Activated -= WindowActivatedForDropShadow;
            window.Deactivated -= WindowDeactivatedForDropShadow;
            switch (effectState)
            {
                case EffectActiveStates.Always:
                    ShowDropShadow(window);
                    break;
                case EffectActiveStates.OnlyIfElementActive:
                    window.Activated += WindowActivatedForDropShadow;
                    window.Deactivated += WindowDeactivatedForDropShadow;
                    break;
                case EffectActiveStates.Never:
                    HideDropShadow(window);
                    break;
            }
        }

        /// <summary>
        /// Fires when the window deactivates for drop-shadow purposes
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        /// <exception cref="System.NotImplementedException"></exception>
        private static void WindowDeactivatedForDropShadow(object sender, EventArgs e)
        {
            var window = sender as Window;
            if (window == null) return;
            HideDropShadow(window);
        }

        /// <summary>
        /// Fires when the window activates for drop-shadow purposes
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="eventArgs">The <see cref="EventArgs"/> instance containing the event data.</param>
        /// <exception cref="System.NotImplementedException"></exception>
        private static void WindowActivatedForDropShadow(object sender, EventArgs eventArgs)
        {
            var window = sender as Window;
            if (window == null) return;
            ShowDropShadow(window);
        }

        /// <summary>
        /// Indicates whether and when the window shall have a drop shaddow
        /// </summary>
        public static EffectActiveStates GetHasDropShadow(DependencyObject obj)
        {
            return (EffectActiveStates) obj.GetValue(HasDropShadowProperty);
        }

        /// <summary>
        /// Indicates whether and when the window shall have a drop shaddow
        /// </summary>
        public static void SetHasDropShadow(DependencyObject obj, EffectActiveStates value)
        {
            obj.SetValue(HasDropShadowProperty, value);
        }

        /// <summary>
        /// Handles window messages
        /// </summary>
        /// <param name="hwnd">The window handle.</param>
        /// <param name="msg">The message.</param>
        /// <param name="wParam">The w param.</param>
        /// <param name="lParam">The l param.</param>
        /// <param name="handled">if set to <c>true</c> [handled].</param>
        /// <returns>IntPtr.</returns>
        private static IntPtr WindowHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            switch (msg)
            {
                case 0x0024: /* WM_GETMINMAXINFO */
                    WindowGetMinMaxInfo(hwnd, lParam);
                    //handled = true;
                    break;
            }

            return (IntPtr) 0;
        }

        /// <summary>Monitor info interop class</summary>
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        internal class MonitorInfo
        {
            public int cbSize = Marshal.SizeOf(typeof (MonitorInfo));
            public InteropRect rcMonitor = new InteropRect();
            public InteropRect rcWork = new InteropRect();
            public int dwFlags = 0;
        }


        /// <summary>Win32 RECT structure</summary>
        [StructLayout(LayoutKind.Sequential, Pack = 0)]
        internal struct InteropRect
        {
            /// <summary> Win32 </summary>
            public readonly int left;

            /// <summary> Win32 </summary>
            public readonly int top;

            /// <summary> Win32 </summary>
            public readonly int right;

            /// <summary> Win32 </summary>
            public readonly int bottom;

            /// <summary> Win32 </summary>
            public static readonly InteropRect Empty = new InteropRect();

            /// <summary> Win32 </summary>
            public int Width
            {
                get { return Math.Abs(right - left); } // Abs needed for BIDI OS
            }

            /// <summary> Win32 </summary>
            public int Height
            {
                get { return bottom - top; }
            }

            /// <summary> Win32 </summary>
            public InteropRect(int left, int top, int right, int bottom)
            {
                this.left = left;
                this.top = top;
                this.right = right;
                this.bottom = bottom;
            }

            /// <summary> Win32 </summary>
            public InteropRect(InteropRect rcSrc)
            {
                left = rcSrc.left;
                top = rcSrc.top;
                right = rcSrc.right;
                bottom = rcSrc.bottom;
            }

            /// <summary> Win32 </summary>
            public bool IsEmpty
            {
                get
                {
                    // BUGBUG : On Bidi OS (hebrew arabic) left > right
                    return left >= right || top >= bottom;
                }
            }
        }

        /// <summary>MinMaxInfo interop struct</summary>
        [StructLayout(LayoutKind.Sequential)]
        internal struct MinMaxInfo
        {
            public InteropPoint ptReserved;
            public InteropPoint ptMaxSize;
            public InteropPoint ptMaxPosition;
            public InteropPoint ptMinTrackSize;
            public InteropPoint ptMaxTrackSize;
        };

        /// <summary>
        /// InteropPoint aka POINTAPI
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        internal struct InteropPoint
        {
            /// <summary>
            /// X coordinate of point.
            /// </summary>
            public int X;

            /// <summary>
            /// Y coordinate of point.
            /// </summary>
            public int Y;

            /// <summary>
            /// Construct a point of coordinates (X,Y).
            /// </summary>
            public InteropPoint(int x, int y)
            {
                X = x;
                Y = y;
            }
        }

        /// <summary>Get monitor info interop function</summary>
        /// <param name="hMonitor">The monitor handle.</param>
        /// <param name="lpmi">The lpmi.</param>
        /// <returns>True or false</returns>
        [DllImport("user32")]
        internal static extern bool GetMonitorInfo(IntPtr hMonitor, MonitorInfo lpmi);

        /// <summary>Monitor from Window handle interop function</summary>
        /// <param name="handle">The handle.</param>
        /// <param name="flags">The flags.</param>
        /// <returns>True or false</returns>
        [DllImport("User32")]
        internal static extern IntPtr MonitorFromWindow(IntPtr handle, int flags);

        /// <summary>Populates the min/max info for the specified window handle</summary>
        /// <param name="hwnd">The window handle</param>
        /// <param name="lParam">Prameters</param>
        private static void WindowGetMinMaxInfo(IntPtr hwnd, IntPtr lParam)
        {

            var mmi = (MinMaxInfo) Marshal.PtrToStructure(lParam, typeof (MinMaxInfo));

            // Adjust the maximized size and position to fit the work area of the correct monitor
            const int monitorDefaultToNearest = 0x00000002;
            var monitor = MonitorFromWindow(hwnd, monitorDefaultToNearest);

            if (monitor != IntPtr.Zero)
            {
                var monitorInfo = new MonitorInfo();
                if (!GetMonitorInfo(monitor, monitorInfo)) return;
                var rcWorkArea = monitorInfo.rcWork;
                var rcMonitorArea = monitorInfo.rcMonitor;
                mmi.ptMaxPosition.X = Math.Abs(rcWorkArea.left - rcMonitorArea.left);
                mmi.ptMaxPosition.Y = Math.Abs(rcWorkArea.top - rcMonitorArea.top);
                mmi.ptMaxSize.X = Math.Max(Math.Abs(rcWorkArea.right - rcWorkArea.left), 200);
                mmi.ptMaxSize.Y = Math.Max(Math.Abs(rcWorkArea.bottom - rcWorkArea.top), 200);
            }

            Marshal.StructureToPtr(mmi, lParam, true);
        }

        private static Point _lastResizeDownPosition;
        private static bool _resizeUp;
        private static bool _resizeDown;
        private static bool _resizeLeft;
        private static bool _resizeRight;
        private static double _previousWindowTop;
        private static double _previousWindowLeft;
        private static double _previousWindowHeight;
        private static double _previousWindowWidth;

        private static Point? _screenScale;

        private static Point GetScreenScale(Visual visual)
        {
            // Checking for scaled screen
            if (_screenScale.HasValue) return _screenScale.Value;
            _screenScale = new Point(1d, 1d);
            var source = PresentationSource.FromVisual(visual);
            if (source != null && source.CompositionTarget != null)
            {
                var dpiX = source.CompositionTarget.TransformToDevice.M11;
                var dpiY = source.CompositionTarget.TransformToDevice.M22;
                if (dpiX > 1 || dpiX < 1 || dpiY > 1 || dpiY < 1)
                    _screenScale = new Point(dpiX, dpiY);
            }
            return _screenScale.Value;
        }

        /// <summary>Mouse down handler for resize behavior</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="mouseButtonEventArgs">The <see cref="MouseButtonEventArgs" /> instance containing the event data.</param>
        private static void WindowResizeMouseDownHandler(object sender, MouseButtonEventArgs mouseButtonEventArgs)
        {
            var window = sender as Window;
            if (window == null) return;
            if (window.WindowState != WindowState.Normal) return;
            if (mouseButtonEventArgs.ButtonState != MouseButtonState.Pressed) return;

            var position = mouseButtonEventArgs.GetPosition(window);
            _lastResizeDownPosition = window.PointToScreen(position);

            if (MustHandleResize(position, window))
            {
                _resizeUp = position.Y < 5 || (position.Y < 20 && position.X < 20) || (position.Y < 20 && position.X > window.Width - 20);
                _resizeDown = position.Y > window.Height - 5 || (position.Y > window.Height - 20 && position.X < 20) || (position.Y > window.Height - 20 && position.X > window.Width - 20);
                _resizeLeft = position.X < 5 || (position.X < 20 && position.Y < 20) || (position.X < 20 && position.Y > window.Width - 20);
                _resizeRight = position.X > window.Width - 5 || (position.X > window.Width - 20 && position.Y < 20) || (position.X > window.Width - 20 && position.Y > window.Height - 20);

                _previousWindowHeight = window.Height;
                _previousWindowWidth = window.Width;
                _previousWindowTop = window.Top;
                _previousWindowLeft = window.Left;

                window.MouseMove += WindowResizeMouseDownMoveHandler;
                window.MouseUp += WindowResizeMouseUpHandler;

                Mouse.Capture(window);
            }
        }

        /// <summary>Figures out whether the current mouse position indicates a required resize operation</summary>
        /// <param name="position">The position.</param>
        /// <param name="window">The window.</param>
        /// <returns>True if resize handling is needed</returns>
        private static bool MustHandleResize(Point position, Window window)
        {
            if (window.WindowState != WindowState.Normal) return false;
            if (!(GetAutoWindowResizingEnabled(window))) return false;

            return position.X < 5 || position.X > window.Width - 5 || position.Y < 5 || position.Y > window.Height - 5 ||
                   (position.X < 20 && position.Y < 20) || (position.X > window.Width - 20 && position.Y < 20) ||
                   (position.X < 20 && position.Y > window.Height - 20) || (position.X > window.Width - 20 && position.Y > window.Height - 20);
        }

        /// <summary>This event handler is used to release the capruted mouse</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="mouseEventArgs">The <see cref="MouseEventArgs" /> instance containing the event data.</param>
        private static void WindowResizeMouseDownMoveHandler(object sender, MouseEventArgs mouseEventArgs)
        {
            var window = sender as Window;
            if (window == null) return;
            if (window.WindowState != WindowState.Normal) return;

            var position = mouseEventArgs.GetPosition(window);
            var screenPosition = window.PointToScreen(position);
            var delta = new Point(_lastResizeDownPosition.X - screenPosition.X, _lastResizeDownPosition.Y - screenPosition.Y);

            var scale = GetScreenScale(window);
            if (scale.X > 1 || scale.X < 1 || scale.Y > 1 || scale.Y < 1)
                delta = new Point(delta.X/scale.X, delta.Y/scale.Y);

            if (_resizeRight) window.Width = Math.Max(_previousWindowWidth - delta.X, window.MinWidth);
            if (_resizeDown) window.Height = Math.Max(_previousWindowHeight - delta.Y, window.MinHeight);

            if (_resizeUp || _resizeLeft)
            {
                var top = _previousWindowTop;
                var left = _previousWindowLeft;
                var height = _previousWindowHeight;
                var width = _previousWindowWidth;
                if (_resizeUp)
                {
                    top -= delta.Y;
                    height += delta.Y;
                }
                if (_resizeLeft)
                {
                    left -= delta.X;
                    width += delta.X;
                }
                width = Math.Max(width, window.MinWidth);
                height = Math.Max(height, window.MinHeight);
                var windowRect = new Rect(left, top, width, height);
                SetWindowVisualRect(windowRect, window);
            }
        }

        private static void SetWindowVisualRect(Rect windowRectangle, Window window)
        {
            var mainWindowPointer = new WindowInteropHelper(window).Handle;
            var mainWindowSource = HwndSource.FromHwnd(mainWindowPointer);

            if (mainWindowSource == null) return;
            if (mainWindowSource.CompositionTarget == null) return;

            var deviceTopLeft = mainWindowSource.CompositionTarget.TransformToDevice.Transform(windowRectangle.TopLeft);
            var deviceBottomRight = mainWindowSource.CompositionTarget.TransformToDevice.Transform(windowRectangle.BottomRight);

            NativeMethods.SetWindowPos(mainWindowSource.Handle,
                IntPtr.Zero,
                (int) (deviceTopLeft.X),
                (int) (deviceTopLeft.Y),
                (int) (Math.Abs(deviceBottomRight.X - deviceTopLeft.X)),
                (int) (Math.Abs(deviceBottomRight.Y - deviceTopLeft.Y)),
                0);
        }

        private static class NativeMethods
        {
            [DllImport("user32.dll")]
            public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int cx, int cy, int uFlags);
        }

        /// <summary>This event handler is used to release the captured mouse</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="mouseEventArgs">The <see cref="MouseEventArgs" /> instance containing the event data.</param>
        private static void WindowResizeMouseUpHandler(object sender, MouseEventArgs mouseEventArgs)
        {
            var win = Mouse.Captured as Window;
            if (win != null)
            {
                win.MouseMove -= WindowResizeMouseDownMoveHandler;
                Mouse.Capture(null);
            }
        }

        /// <summary>Mouse over handler for resize behavior</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="mouseEventArgs">The <see cref="MouseEventArgs" /> instance containing the event data.</param>
        private static void WindowResizeMouseOverHandler(object sender, MouseEventArgs mouseEventArgs)
        {
            var window = sender as Window;
            if (window == null) return;
            if (window.WindowState != WindowState.Normal) return;

            var position = mouseEventArgs.GetPosition(window);

            if (position.X < 20 && position.Y < 20) // Top-left
                Mouse.SetCursor(Cursors.SizeNWSE);
            else if (position.X > window.Width - 10 && position.Y < 10) // Top-right
                Mouse.SetCursor(Cursors.SizeNESW);
            else if (position.X > window.Width - 20 && position.Y > window.Height - 20) // bottom-right
                Mouse.SetCursor(Cursors.SizeNWSE);
            else if (position.X < 20 && position.Y > window.Height - 20) // bottom-left
                Mouse.SetCursor(Cursors.SizeNESW);
            else if (position.Y <= 5) // top
                Mouse.SetCursor(Cursors.SizeNS);
            else if (position.Y >= window.Height - 5) // bottom
                Mouse.SetCursor(Cursors.SizeNS);
            else if (position.X <= 5) // left
                Mouse.SetCursor(Cursors.SizeWE);
            else if (position.X >= window.Width - 5) // right
                Mouse.SetCursor(Cursors.SizeWE);
            else
                Mouse.SetCursor(Cursors.Arrow);
        }

        /// <summary>If true, the window allows resizing, even if the window is a borderless window</summary>
        public static bool GetAutoWindowResizingEnabled(DependencyObject obj)
        {
            return (bool) obj.GetValue(AutoWindowResizingEnabledProperty);
        }

        /// <summary>If true, the window allows resizing, even if the window is a borderless window</summary>
        public static void SetAutoWindowResizingEnabled(DependencyObject obj, bool value)
        {
            obj.SetValue(AutoWindowResizingEnabledProperty, value);
        }

        /// <summary>
        /// External API call to set window attributes
        /// </summary>
        /// <param name="hwnd">Window handle</param>
        /// <param name="attr">Attribute to set</param>
        /// <param name="attrValue">Attribute value</param>
        /// <param name="attrSize">Attribute size</param>
        /// <returns></returns>
        [DllImport("dwmapi.dll", PreserveSig = true)]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        /// <summary>
        /// Extends the frame into the client area
        /// </summary>
        /// <param name="hWnd">Window handle</param>
        /// <param name="pMarInset">Margin</param>
        /// <returns>System.Int32.</returns>
        [DllImport("dwmapi.dll")]
        private static extern int DwmExtendFrameIntoClientArea(IntPtr hWnd, ref Margins pMarInset);

        /// <summary>
        /// Drops a standard shadow to a WPF Window, even if the window is borderless. Only works with DWM (Vista and later).
        /// </summary>
        /// <remarks>
        /// This method is much more efficient than setting AllowsTransparency to true and using the DropShadow effect,
        /// as AllowsTransparency involves a huge permormance issue (hardware acceleration is turned off for all the window).
        /// </remarks>
        /// <param name="window">Window to which the shadow will be applied</param>
        public static void ShowDropShadow(Window window)
        {
            if (!DropShadow(window))
                window.SourceInitialized += OnWindowSourceInitialized; // Didn't work yet, so we try again when the window finishes initializing
        }

        /// <summary>
        /// Removes the drop shaddow from a window
        /// </summary>
        /// <param name="window">The window.</param>
        public static void HideDropShadow(Window window)
        {
            // TODO: Implement this
        }

        /// <summary>
        /// Called when the window is done initializing
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private static void OnWindowSourceInitialized(object sender, EventArgs e)
        {
            var window = sender as Window;
            if (window == null) return;

            DropShadow(window);
            window.SourceInitialized -= OnWindowSourceInitialized; // Done, so we do not need ourselves as a handler anymore
        }

        /// <summary>
        /// The actual method that makes API calls to drop the shadow to the window
        /// </summary>
        /// <param name="window">Window to which the shadow will be applied</param>
        /// <returns>True if the method succeeded, false if not</returns>
        private static bool DropShadow(Window window)
        {
            try
            {
                var interopHelper = new WindowInteropHelper(window);
                const int DWMWA_NCRENDERING_POLICY = 2;
                var value = 2;

                var result = DwmSetWindowAttribute(interopHelper.Handle, DWMWA_NCRENDERING_POLICY, ref value, 4);
                if (result == 0)
                {
                    var margins = new Margins {Bottom = 0, Left = 0, Right = 0, Top = 0};
                    var ret2 = DwmExtendFrameIntoClientArea(interopHelper.Handle, ref margins);
                    return ret2 == 0;
                }
                return false;
            }
            catch (Exception)
            {
                // Probably dwmapi.dll not found (incompatible OS)
                return false;
            }
        }
    }

    /// <summary>
    /// Indicates when a certain effect should be active
    /// </summary>
    public enum EffectActiveStates
    {
        /// <summary>
        /// Never
        /// </summary>
        Never,

        /// <summary>
        /// Only then element the effect applies to is activated
        /// </summary>
        OnlyIfElementActive,

        /// <summary>
        /// Always
        /// </summary>
        Always
    }

    /// <summary>
    /// Information about a specific display
    /// </summary>
    public class DisplayInformation
    {
        /// <summary>
        /// Availability
        /// </summary>
        public string Availability { get; set; }
        /// <summary>
        /// Screen height
        /// </summary>
        public double ScreenHeight { get; set; }
        /// <summary>
        /// Screen width
        /// </summary>
        public double ScreenWidth { get; set; }
        /// <summary>
        /// Monitor area
        /// </summary>
        public Rect MonitorArea { get; set; }
        /// <summary>
        /// Work area
        /// </summary>
        public Rect WorkArea { get; set; }
    }
}