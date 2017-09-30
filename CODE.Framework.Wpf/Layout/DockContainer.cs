using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using CODE.Framework.Wpf.Controls;
using CODE.Framework.Wpf.Utilities;

namespace CODE.Framework.Wpf.Layout
{
    /// <summary>
    /// This class can be used to host various controls as dockable elements
    /// </summary>
    public class DockContainer : Panel
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DockContainer"/> class.
        /// </summary>
        public DockContainer()
        {
            IsHitTestVisible = true;
            Background = Brushes.Transparent;
            if (DockWellRenderer == null)
                DockWellRenderer = new StandardDockWellRenderer();
        }

        /// <summary>List of UI elements used in the main dock well</summary>
        private readonly List<DockedUIElement> _mainDockWell = new List<DockedUIElement>();

        /// <summary>List of UI elements used in the main dock well</summary>
        public List<DockedUIElement> MainDockWell
        {
            get { return _mainDockWell; }
        }

        /// <summary>List of UI elements used in the secondary dock well</summary>
        private readonly List<DockedUIElement> _secondaryDockWell = new List<DockedUIElement>();

        /// <summary>List of UI elements used in the secondary dock well</summary>
        public List<DockedUIElement> SecondaryDockWell
        {
            get { return _secondaryDockWell; }
        }

        /// <summary>'Hot' areas within the panel (these areas have special mouse actions tied to them)</summary>
        private readonly List<HotArea> _hotAreas = new List<HotArea>();

        /// <summary>Dock position attached property</summary>
        public static readonly DependencyProperty DockPositionProperty = DependencyProperty.RegisterAttached("DockPosition", typeof (DockPosition), typeof (DockContainer), new PropertyMetadata(DockPosition.Main));

        /// <summary>Gets the dock position</summary>
        /// <param name="obj">The object.</param>
        /// <returns>DockPosition.</returns>
        public static DockPosition GetDockPosition(DependencyObject obj)
        {
            return (DockPosition) obj.GetValue(DockPositionProperty);
        }

        /// <summary>Sets the dock position. </summary>
        /// <param name="obj">The object.</param>
        /// <param name="value">The value.</param>
        public static void SetDockPosition(DependencyObject obj, DockPosition value)
        {
            obj.SetValue(DockPositionProperty, value);
        }

        /// <summary>Title property used for docked elements</summary>
        public static readonly DependencyProperty TitleProperty = DependencyProperty.RegisterAttached("Title", typeof (string), typeof (DockContainer), new PropertyMetadata(""));

        /// <summary>Title property used for docked elements</summary>
        public static string GetTitle(DependencyObject obj)
        {
            return (string) obj.GetValue(TitleProperty);
        }

        /// <summary>Title property used for docked elements</summary>
        public static void SetTitle(DependencyObject obj, string value)
        {
            obj.SetValue(TitleProperty, value);
        }

        /// <summary>Width of the splitter bar</summary>
        public double SplitterWidth
        {
            get { return (double) GetValue(SplitterWidthProperty); }
            set { SetValue(SplitterWidthProperty, value); }
        }

        /// <summary>Width of the splitter bar</summary>
        public static readonly DependencyProperty SplitterWidthProperty = DependencyProperty.Register("SplitterWidth", typeof (double), typeof (DockContainer), new PropertyMetadata(5d, InvalidateAll));

        /// <summary>Height of the splitter bar</summary>
        public double SplitterHeight
        {
            get { return (double) GetValue(SplitterHeightProperty); }
            set { SetValue(SplitterHeightProperty, value); }
        }

        /// <summary>Height of the splitter bar</summary>
        public static readonly DependencyProperty SplitterHeightProperty = DependencyProperty.Register("SplitterHeight", typeof (double), typeof (DockContainer), new PropertyMetadata(5d, InvalidateAll));

        /// <summary>Gets or sets the dock well renderer object.</summary>
        /// <value>The dock well renderer.</value>
        public IDockWellRenderer DockWellRenderer
        {
            get { return (IDockWellRenderer) GetValue(DockWellRendererProperty); }
            set { SetValue(DockWellRendererProperty, value); }
        }

        /// <summary>Gets or sets the dock well renderer object.</summary>
        /// <value>The dock well renderer.</value>
        public static readonly DependencyProperty DockWellRendererProperty = DependencyProperty.Register("DockWellRenderer", typeof (IDockWellRenderer), typeof (DockContainer), new PropertyMetadata(null, InvalidateAll));

        /// <summary>Invalidates arrange and measure</summary>
        private static void InvalidateAll(DependencyObject d, DependencyPropertyChangedEventArgs args)
        {
            var container = d as DockContainer;
            if (container == null) return;
            container.InvalidateMeasure();
            container.InvalidateArrange();
            container.InvalidateVisual();
        }

        private Rect _mainWellClientRect;
        private Rect _leftDockWellClientRect;
        private Rect _rightDockWellClientRect;
        private Rect _topDockWellClientRect;
        private Rect _bottomDockWellClientRect;
        private Rect _mainWellRect;
        private Rect _leftDockWellRect;
        private Rect _rightDockWellRect;
        private Rect _topDockWellRect;
        private Rect _bottomDockWellRect;

        /// <summary>
        /// When overridden in a derived class, measures the size in layout required for child elements and determines a size for the <see cref="T:System.Windows.FrameworkElement" />-derived class.
        /// </summary>
        /// <param name="availableSize">The available size that this element can give to child elements. Infinity can be specified as a value to indicate that the element will size to whatever content is available.</param>
        /// <returns>The size that this element determines it needs during layout, based on its calculations of child element sizes.</returns>
        protected override Size MeasureOverride(Size availableSize)
        {
            var renderer = DockWellRenderer;
            if (renderer == null) return availableSize;

            renderer.SetDockContainer(this);
            renderer.MainSelectedIndex = MainDockSelectedIndex;
            renderer.LeftSelectedIndex = LeftDockSelectedIndex;
            renderer.TopSelectedIndex = TopDockSelectedIndex;
            renderer.RightSelectedIndex = RightDockSelectedIndex;
            renderer.BottomSelectedIndex = BottomDockSelectedIndex;

            OrganizeUIElements();

            renderer.TotalHeight = double.IsNaN(availableSize.Height) ? 10000d : availableSize.Height;
            renderer.TotalWidth = double.IsNaN(availableSize.Width) ? 10000d : availableSize.Width;
            renderer.TopDockHeight = 0d;
            renderer.BottomDockHeight = 0d;
            renderer.LeftDockWidth = 0d;
            renderer.RightDockWidth = 0d;
            renderer.SplitterHeight = SplitterHeight;
            renderer.SplitterWidth = SplitterWidth;

            foreach (var element in _secondaryDockWell.Where(e => e.IsDocked))
            {
                switch (element.Position)
                {
                    case DockPosition.Left:
                        renderer.LeftDockWidth = LeftDockWidth;
                        break;
                    case DockPosition.Top:
                        renderer.TopDockHeight = TopDockHeight;
                        break;
                    case DockPosition.Right:
                        renderer.RightDockWidth = RightDockWidth;
                        break;
                    case DockPosition.Bottom:
                        renderer.BottomDockHeight = BottomDockHeight;
                        break;
                }
            }

            renderer.MainElements = _mainDockWell.Where(e => e.IsDocked).ToList();
            renderer.LeftElements = _secondaryDockWell.Where(e => e.IsDocked && e.Position == DockPosition.Left).ToList();
            renderer.TopElements = _secondaryDockWell.Where(e => e.IsDocked && e.Position == DockPosition.Top).ToList();
            renderer.RightElements = _secondaryDockWell.Where(e => e.IsDocked && e.Position == DockPosition.Right).ToList();
            renderer.BottomElements = _secondaryDockWell.Where(e => e.IsDocked && e.Position == DockPosition.Bottom).ToList();

            _mainWellClientRect = renderer.GetMainWellClientRect();
            _leftDockWellClientRect = renderer.GetDockWellClientRect(DockPosition.Left);
            _rightDockWellClientRect = renderer.GetDockWellClientRect(DockPosition.Right);
            _topDockWellClientRect = renderer.GetDockWellClientRect(DockPosition.Top);
            _bottomDockWellClientRect = renderer.GetDockWellClientRect(DockPosition.Bottom);
            _mainWellRect = renderer.GetMainWellRect();
            _leftDockWellRect = renderer.GetDockWellRect(DockPosition.Left);
            _rightDockWellRect = renderer.GetDockWellRect(DockPosition.Right);
            _topDockWellRect = renderer.GetDockWellRect(DockPosition.Top);
            _bottomDockWellRect = renderer.GetDockWellRect(DockPosition.Bottom);

            foreach (var element in _mainDockWell)
                element.Element.Measure(ReducedClientSize(_mainWellClientRect.Size));

            foreach (var element in _secondaryDockWell)
                switch (element.Position)
                {
                    case DockPosition.Left:
                        element.Element.Measure(ReducedClientSize(_leftDockWellClientRect.Size));
                        break;
                    case DockPosition.Top:
                        element.Element.Measure(ReducedClientSize(_topDockWellClientRect.Size));
                        break;
                    case DockPosition.Right:
                        element.Element.Measure(ReducedClientSize(_rightDockWellClientRect.Size));
                        break;
                    case DockPosition.Bottom:
                        element.Element.Measure(ReducedClientSize(_bottomDockWellClientRect.Size));
                        break;
                }

            return availableSize;
        }

        private static Rect ReducedClientRect(Rect original)
        {
            return GeometryHelper.NewRect(original.X + 1, original.Y + 1, original.Width - 2, original.Height - 2);
        }

        private static Size ReducedClientSize(Size original)
        {
            return GeometryHelper.NewSize(original.Width - 2, original.Height - 2);
        }

        /// <summary>
        /// When overridden in a derived class, positions child elements and determines a size for a <see cref="T:System.Windows.FrameworkElement" /> derived class.
        /// </summary>
        /// <param name="finalSize">The final area within the parent that this element should use to arrange itself and its children.</param>
        /// <returns>The actual size used.</returns>
        protected override Size ArrangeOverride(Size finalSize)
        {
            _hotAreas.Clear();

            var renderer = DockWellRenderer;
            if (renderer == null) return base.ArrangeOverride(finalSize);

            // Positioning the main dock well selected element and making sure it is visible
            if (MainDockSelectedIndex > -1 && _mainDockWell.Count >= MainDockSelectedIndex + 1)
            {
                var mainElement = _mainDockWell[MainDockSelectedIndex];
                mainElement.Element.Arrange(ReducedClientRect(_mainWellClientRect));
                mainElement.Element.Visibility = Visibility.Visible;
            }
            var counter = 0;
            foreach (var element in _mainDockWell) // All other elements should be hidden
            {
                if (counter != MainDockSelectedIndex)
                    element.Element.Visibility = Visibility.Collapsed;
                counter++;
            }

            // Getting lists of all the other elements
            var leftElements = _secondaryDockWell.Where(e => e.Position == DockPosition.Left).ToList();
            var rightElements = _secondaryDockWell.Where(e => e.Position == DockPosition.Right).ToList();
            var topElements = _secondaryDockWell.Where(e => e.Position == DockPosition.Top).ToList();
            var bottomElements = _secondaryDockWell.Where(e => e.Position == DockPosition.Bottom).ToList();

            // Positioning the left dock well selected element and making sure it is visible
            if (LeftDockSelectedIndex > -1 && leftElements.Count >= LeftDockSelectedIndex + 1)
            {
                var element = leftElements[LeftDockSelectedIndex];
                element.Element.Arrange(ReducedClientRect(_leftDockWellClientRect));
                element.Element.Visibility = Visibility.Visible;
            }
            counter = 0;
            foreach (var element in leftElements) // All other elements should be hidden
            {
                if (counter != LeftDockSelectedIndex)
                    element.Element.Visibility = Visibility.Collapsed;
                counter++;
            }

            // Positioning the top dock well selected element and making sure it is visible
            if (TopDockSelectedIndex > -1 && topElements.Count >= TopDockSelectedIndex + 1)
            {
                var element = topElements[TopDockSelectedIndex];
                element.Element.Arrange(ReducedClientRect(_topDockWellClientRect));
                element.Element.Visibility = Visibility.Visible;
            }
            counter = 0;
            foreach (var element in topElements) // All other elements should be hidden
            {
                if (counter != TopDockSelectedIndex)
                    element.Element.Visibility = Visibility.Collapsed;
                counter++;
            }

            // Positioning the right dock well selected element and making sure it is visible
            if (RightDockSelectedIndex > -1 && rightElements.Count >= RightDockSelectedIndex + 1)
            {
                var element = rightElements[RightDockSelectedIndex];
                element.Element.Arrange(ReducedClientRect(_rightDockWellClientRect));
                element.Element.Visibility = Visibility.Visible;
            }
            counter = 0;
            foreach (var element in rightElements) // All other elements should be hidden
            {
                if (counter != RightDockSelectedIndex)
                    element.Element.Visibility = Visibility.Collapsed;
                counter++;
            }

            // Positioning the bottom dock well selected element and making sure it is visible
            if (BottomDockSelectedIndex > -1 && bottomElements.Count >= BottomDockSelectedIndex + 1)
            {
                var element = bottomElements[BottomDockSelectedIndex];
                element.Element.Arrange(ReducedClientRect(_bottomDockWellClientRect));
                element.Element.Visibility = Visibility.Visible;
            }
            counter = 0;
            foreach (var element in bottomElements) // All other elements should be hidden
            {
                if (counter != BottomDockSelectedIndex)
                    element.Element.Visibility = Visibility.Collapsed;
                counter++;
            }

            // Looking for resize 'hot' areas
            if (_leftDockWellRect.Width > .1d)
                _hotAreas.Add(new SplitterHotArea(this, Cursors.SizeWE, 0) {AreaRectangle = GeometryHelper.NewRect(_leftDockWellRect.Width + 1d, 0d, SplitterWidth, _leftDockWellRect.Height)});
            if (_rightDockWellRect.Width > .1d)
                _hotAreas.Add(new SplitterHotArea(this, Cursors.SizeWE, 2) {AreaRectangle = GeometryHelper.NewRect(_rightDockWellRect.Left - 1d - SplitterWidth, 0d, SplitterWidth, _rightDockWellRect.Height)});
            if (_topDockWellRect.Height > .1d)
                _hotAreas.Add(new SplitterHotArea(this, Cursors.SizeNS, 1) {AreaRectangle = GeometryHelper.NewRect(_topDockWellRect.Left, _topDockWellRect.Bottom + 1d, _topDockWellRect.Width, SplitterHeight)});
            if (_bottomDockWellRect.Height > .1d)
                _hotAreas.Add(new SplitterHotArea(this, Cursors.SizeNS, 3) {AreaRectangle = GeometryHelper.NewRect(_bottomDockWellRect.Left, _bottomDockWellRect.Top - 1d - SplitterHeight, _bottomDockWellRect.Width, SplitterHeight)});

            return base.ArrangeOverride(finalSize);
        }

        /// <summary>
        /// Invoked when an unhandled MouseMove attached event reaches an element in its route that is derived from this class. Implement this method to add class handling for this event.
        /// </summary>
        /// <param name="e">The <see cref="T:System.Windows.Input.MouseEventArgs" /> that contains the event data.</param>
        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (MouseDownAndMoveOverride != null)
            {
                MouseDownAndMoveOverride(e);
                return;
            }

            base.OnMouseMove(e);

            var mousePosition = e.GetPosition(this);
            if (mousePosition.X < 0d || mousePosition.Y < 0d || mousePosition.X > Width || mousePosition.Y > Height) return;

            foreach (var hotArea in _hotAreas)
                if (hotArea.AreaRectangle.Contains(mousePosition))
                    if (hotArea.SpecialMouseCursor != null)
                        Mouse.SetCursor(hotArea.SpecialMouseCursor);
        }

        /// <summary>If this delegate is set, this delegate will receive mouse move events, rather than letting the standard mouse move event handler handle it</summary>
        public Action<MouseEventArgs> MouseDownAndMoveOverride { get; set; }

        private HotArea _lastMouseDownHotArea;

        /// <summary>
        /// Invoked when an unhandled <see cref="E:System.Windows.UIElement.MouseLeftButtonDown" /> routed event is raised on this element. Implement this method to add class handling for this event.
        /// </summary>
        /// <param name="e">The <see cref="T:System.Windows.Input.MouseButtonEventArgs" /> that contains the event data. The event data reports that the left mouse button was pressed.</param>
        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            _lastMouseDownHotArea = null;

            base.OnMouseLeftButtonDown(e);

            var mousePosition = e.GetPosition(this);
            if (mousePosition.X < 0d || mousePosition.Y < 0d || mousePosition.X > Width || mousePosition.Y > Height) return;

            foreach (var hotArea in _hotAreas)
                if (hotArea.AreaRectangle.Contains(mousePosition))
                {
                    _lastMouseDownHotArea = hotArea;
                    if (hotArea.MouseDown != null)
                    {
                        hotArea.MouseDown(this, e);
                        if (e.Handled)
                        {
                            _lastMouseDownHotArea = null;
                            break;
                        }
                    }
                }
        }

        /// <summary>
        /// Invoked when an unhandled <see cref="E:System.Windows.UIElement.MouseLeftButtonUp" /> routed event reaches an element in its route that is derived from this class. Implement this method to add class handling for this event.
        /// </summary>
        /// <param name="e">The <see cref="T:System.Windows.Input.MouseButtonEventArgs" /> that contains the event data. The event data reports that the left mouse button was released.</param>
        protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonUp(e);

            MouseDownAndMoveOverride = null; // No more special handlers after release (at least for now)
            if (Mouse.Captured != null && Mouse.Captured.Equals(this)) Mouse.Capture(null);

            var mousePosition = e.GetPosition(this);
            if (mousePosition.X < 0d || mousePosition.Y < 0d || mousePosition.X > Width || mousePosition.Y > Height) return;

            foreach (var hotArea in _hotAreas)
                if (hotArea.AreaRectangle.Contains(mousePosition))
                    if (hotArea.MouseClick != null && _lastMouseDownHotArea == hotArea)
                        hotArea.MouseClick(this, e);
        }

        /// <summary>
        /// Draws the content of a <see cref="T:System.Windows.Media.DrawingContext" /> object during the render pass of a <see cref="T:System.Windows.Controls.Panel" /> element.
        /// </summary>
        /// <param name="dc">The <see cref="T:System.Windows.Media.DrawingContext" /> object to draw.</param>
        protected override void OnRender(DrawingContext dc)
        {
            base.OnRender(dc);

            var renderer = DockWellRenderer;
            if (renderer != null) renderer.DrawChrome(dc);
        }

        /// <summary>Width of the left dock area</summary>
        /// <value>The width of the left dock.</value>
        public double LeftDockWidth
        {
            get { return (double) GetValue(LeftDockWidthProperty); }
            set { SetValue(LeftDockWidthProperty, value); }
        }

        /// <summary>Width of the left dock area</summary>
        /// <value>The width of the left dock.</value>
        public static readonly DependencyProperty LeftDockWidthProperty = DependencyProperty.Register("LeftDockWidth", typeof (double), typeof (DockContainer), new PropertyMetadata(175d, InvalidateAll));

        /// <summary>Width of the right dock area</summary>
        /// <value>The width of the right dock.</value>
        public double RightDockWidth
        {
            get { return (double) GetValue(RightDockWidthProperty); }
            set { SetValue(RightDockWidthProperty, value); }
        }

        /// <summary>Width of the right dock area</summary>
        /// <value>The width of the right dock.</value>
        public static readonly DependencyProperty RightDockWidthProperty = DependencyProperty.Register("RightDockWidth", typeof (double), typeof (DockContainer), new PropertyMetadata(175d, InvalidateAll));

        /// <summary>Height of the top dock area</summary>
        /// <value>The height of the top dock.</value>
        public double TopDockHeight
        {
            get { return (double) GetValue(TopDockHeightProperty); }
            set { SetValue(TopDockHeightProperty, value); }
        }

        /// <summary>Height of the top dock area</summary>
        /// <value>The height of the top dock.</value>
        public static readonly DependencyProperty TopDockHeightProperty = DependencyProperty.Register("TopDockHeight", typeof (double), typeof (DockContainer), new PropertyMetadata(150d, InvalidateAll));

        /// <summary>Height of the bottom dock area</summary>
        /// <value>The height of the bottom dock.</value>
        public double BottomDockHeight
        {
            get { return (double) GetValue(BottomDockHeightProperty); }
            set { SetValue(BottomDockHeightProperty, value); }
        }

        /// <summary>Height of the bottom dock area</summary>
        /// <value>The height of the bottom dock.</value>
        public static readonly DependencyProperty BottomDockHeightProperty = DependencyProperty.Register("BottomDockHeight", typeof (double), typeof (DockContainer), new PropertyMetadata(150d, InvalidateAll));

        /// <summary>Index of the main selected item in the dock well</summary>
        public int MainDockSelectedIndex
        {
            get { return (int) GetValue(MainDockSelectedIndexProperty); }
            set { SetValue(MainDockSelectedIndexProperty, value); }
        }

        /// <summary>Index of the main selected item in the dock well</summary>
        public static readonly DependencyProperty MainDockSelectedIndexProperty = DependencyProperty.Register("MainDockSelectedIndex", typeof (int), typeof (DockContainer), new PropertyMetadata(0, InvalidateAll));

        /// <summary>Index of the left selected item in the dock well</summary>
        public int LeftDockSelectedIndex
        {
            get { return (int) GetValue(LeftDockSelectedIndexProperty); }
            set { SetValue(LeftDockSelectedIndexProperty, value); }
        }

        /// <summary>Index of the left selected item in the dock well</summary>
        public static readonly DependencyProperty LeftDockSelectedIndexProperty = DependencyProperty.Register("LeftDockSelectedIndex", typeof (int), typeof (DockContainer), new PropertyMetadata(0, InvalidateAll));

        /// <summary>Index of the right selected item in the dock well</summary>
        public int RightDockSelectedIndex
        {
            get { return (int) GetValue(RightDockSelectedIndexProperty); }
            set { SetValue(RightDockSelectedIndexProperty, value); }
        }

        /// <summary>Index of the right selected item in the dock well</summary>
        public static readonly DependencyProperty RightDockSelectedIndexProperty = DependencyProperty.Register("RightDockSelectedIndex", typeof (int), typeof (DockContainer), new PropertyMetadata(0, InvalidateAll));

        /// <summary>Index of the top selected item in the dock well</summary>
        public int TopDockSelectedIndex
        {
            get { return (int) GetValue(TopDockSelectedIndexProperty); }
            set { SetValue(TopDockSelectedIndexProperty, value); }
        }

        /// <summary>Index of the top selected item in the dock well</summary>
        public static readonly DependencyProperty TopDockSelectedIndexProperty = DependencyProperty.Register("TopDockSelectedIndex", typeof (int), typeof (DockContainer), new PropertyMetadata(0, InvalidateAll));

        /// <summary>Index of the bottom selected item in the dock well</summary>
        public int BottomDockSelectedIndex
        {
            get { return (int) GetValue(BottomDockSelectedIndexProperty); }
            set { SetValue(BottomDockSelectedIndexProperty, value); }
        }

        /// <summary>Index of the bottom selected item in the dock well</summary>
        public static readonly DependencyProperty BottomDockSelectedIndexProperty = DependencyProperty.Register("BottomDockSelectedIndex", typeof (int), typeof (DockContainer), new PropertyMetadata(0, InvalidateAll));

        /// <summary>
        /// Arranges UI elements into their wells
        /// </summary>
        private void OrganizeUIElements()
        {
            _mainDockWell.Clear();
            _secondaryDockWell.Clear();

            var counter = 0;
            foreach (var child in Children)
            {
                counter++;
                var element = child as UIElement;
                if (element == null) continue;

                var title = GetTitle(element);
                if (string.IsNullOrEmpty(title))
                    title = SimpleView.GetTitle(element);
                if (string.IsNullOrEmpty(title))
                    title = SimpleView.GetUIElementTitle(element);
                if (string.IsNullOrEmpty(title))
                    title = "[" + counter + "]";

                var type = SimpleView.GetUIElementType(element);
                if (type == UIElementTypes.Primary)
                {
                    var position = GetDockPosition(element);
                    if (position == DockPosition.Main)
                        _mainDockWell.Add(new DockedUIElement {Element = element, Position = DockPosition.Main, Title = title});
                    else
                        _secondaryDockWell.Add(new DockedUIElement
                        {
                            Element = element,
                            Position = position,
                            Title = title
                        });
                }
                else
                {
                    var position = GetDockPosition(element);
                    if (position == DockPosition.Main) position = DockPosition.Left; // Main would be the default, but if we already know it is not a primary element, we assume left as the default instead
                    _secondaryDockWell.Add(new DockedUIElement
                    {
                        Element = element,
                        Position = position,
                        Title = title
                    });
                }
            }
        }

        /// <summary>Adds a hot area to the overall list</summary>
        /// <param name="hotArea">The hot area.</param>
        public void AddHotArea(HotArea hotArea)
        {
            _hotAreas.Add(hotArea);
        }

        /// <summary>See if this current control is used as an items panel to lay out another control. If so, this method returns the actual control</summary>
        public ItemsControl FindTemplatedItemsControl()
        {
            var visual = this as DependencyObject;

            while (visual != null)
            {
                visual = VisualTreeHelper.GetParent(visual);
                if (visual is SimpleView)
                    return visual as ItemsControl;
            }

            return null;
        }

    }

    /// <summary>
    /// Reference container for docked UI elements
    /// </summary>
    public class DockedUIElement
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DockedUIElement"/> class.
        /// </summary>
        public DockedUIElement()
        {
            IsDocked = true;
        }

        /// <summary>Reference to the actual UI element</summary>
        /// <value>The element.</value>
        public UIElement Element { get; set; }

        /// <summary>Gets or sets the dock position.</summary>
        /// <value>The position.</value>
        public DockPosition Position { get; set; }

        /// <summary>Indicates whether the object is docked or not (free floating)</summary>
        /// <value><c>true</c> if this instance is docked; otherwise, <c>false</c>.</value>
        public bool IsDocked { get; set; }

        /// <summary>Title property used for docked elements</summary>
        public string Title { get; set; }
    }

    /// <summary>Position of docked UI element</summary>
    public enum DockPosition
    {
        /// <summary>
        /// Top
        /// </summary>
        Top,

        /// <summary>
        /// Bottom
        /// </summary>
        Bottom,

        /// <summary>
        /// Left
        /// </summary>
        Left,

        /// <summary>
        /// Right
        /// </summary>
        Right,

        /// <summary>
        /// Main dock well
        /// </summary>
        Main
    }

    /// <summary>
    /// Common functionality for objects used to render document wells
    /// </summary>
    public interface IDockWellRenderer
    {
        /// <summary>
        /// Main well header height
        /// </summary>
        double MainWellHeaderHeight { get; }

        /// <summary>
        /// Dock well header height
        /// </summary>
        double DockWellHeaderHeight { get; }

        /// <summary>
        /// Dock well footer height
        /// </summary>
        double DockWellFooterHeight { get; }

        /// <summary>
        /// Returns the size of the client area (the area available for the actually hosted control, which excludes things like the area needed for the header) of the dock well
        /// </summary>
        /// <returns>Size.</returns>
        Rect GetMainWellClientRect();

        /// <summary>
        /// Returns the size of the client area (the area available for the actually hosted control, which excludes things like the area needed for the header) of the dock well
        /// </summary>
        /// <param name="position">The position.</param>
        /// <returns>Size.</returns>
        Rect GetDockWellClientRect(DockPosition position);

        /// <summary>Returns the size of the client area (the area available for the actually hosted control, as well as other chrome) of the dock well</summary>
        /// <returns>Size.</returns>
        Rect GetMainWellRect();

        /// <summary>Returns the size of the whole area (the area available for the actually hosted control, as well as other chrome) of the dock well</summary>
        /// <param name="position">The position.</param>
        /// <returns>Size.</returns>
        Rect GetDockWellRect(DockPosition position);

        /// <summary>Width of the left dock well</summary>
        /// <value>The width of the left dock.</value>
        double LeftDockWidth { get; set; }

        /// <summary>Width of the right dock well</summary>
        /// <value>The width of the right dock.</value>
        double RightDockWidth { get; set; }

        /// <summary>Height of the top dock well</summary>
        /// <value>The height of the top dock.</value>
        double TopDockHeight { get; set; }

        /// <summary>Height of the bottom dock well</summary>
        /// <value>The height of the bottom dock.</value>
        double BottomDockHeight { get; set; }

        /// <summary>Total height of the dock panel</summary>
        /// <value>The total height.</value>
        double TotalHeight { get; set; }

        /// <summary>Total width of the dock panel</summary>
        /// <value>The total width.</value>
        double TotalWidth { get; set; }

        /// <summary>
        /// Width of the splitter element
        /// </summary>
        double SplitterWidth { get; set; }

        /// <summary>
        /// Height of the splitter element
        /// </summary>
        double SplitterHeight { get; set; }

        /// <summary> Draws the actual chrome around the docked objects </summary>
        /// <param name="dc">Drawing context</param>
        void DrawChrome(DrawingContext dc);

        /// <summary>Draws the chrome for a window that is being dragged</summary>
        /// <param name="dc">The drawing context.</param>
        /// <param name="elements">The docked elements.</param>
        /// <param name="selectedIndex">Index of the selected dock element.</param>
        /// <param name="areaRect">The area rectangle used to draw the chrome.</param>
        /// <param name="showTabs">Defines whether tabs headers should be shown.</param>
        /// <param name="firstTabOffset">In special cases, the first tab may be offset by a certain margin</param>
        void DrawDockWindowChrome(DrawingContext dc, List<DockedUIElement> elements, int selectedIndex, Rect areaRect, bool showTabs, Point firstTabOffset);

        /// <summary>Docked main elements</summary>
        List<DockedUIElement> MainElements { get; set; }

        /// <summary>Docked left elements</summary>
        List<DockedUIElement> LeftElements { get; set; }

        /// <summary>Docked top elements</summary>
        List<DockedUIElement> TopElements { get; set; }

        /// <summary>Docked right elements</summary>
        List<DockedUIElement> RightElements { get; set; }

        /// <summary>Docked bottom elements</summary>
        List<DockedUIElement> BottomElements { get; set; }

        /// <summary>Index of the selected element in the main well</summary>
        int MainSelectedIndex { get; set; }

        /// <summary>Index of the selected element in the left well</summary>
        int LeftSelectedIndex { get; set; }

        /// <summary>Index of the selected element in the top well</summary>
        int TopSelectedIndex { get; set; }

        /// <summary>Index of the selected element in the right well</summary>
        int RightSelectedIndex { get; set; }

        /// <summary>Index of the selected element in the bottom well</summary>
        int BottomSelectedIndex { get; set; }

        /// <summary>Sets a reference to the actual doc container</summary>
        /// <param name="container">Container</param>
        void SetDockContainer(DockContainer container);
    }

    /// <summary>
    /// Standard dock well renderer
    /// </summary>
    public class StandardDockWellRenderer : FrameworkElement, IDockWellRenderer
    {
        /// <summary>
        /// Gets the height of the header.
        /// </summary>
        /// <value>The height of the header.</value>
        public virtual double MainWellHeaderHeight
        {
            get
            {
                if (_mainWellHeaderHeight < .1d)
                {
                    var font = new Typeface(MainWellHeaderFontFamily, MainWellHeaderFontStyle, MainWellHeaderFontWeight, FontStretches.Normal);
                    var ft = new FormattedText("X", CultureInfo.CurrentUICulture, FlowDirection.LeftToRight, font, MainWellHeaderFontSize, MainWellSelectedHeaderForegroundBrush)
                    {
                        TextAlignment = TextAlignment.Left,
                        Trimming = TextTrimming.CharacterEllipsis,
                    };
                    _mainWellHeaderHeight = ft.Height + 6d;
                }
                return _mainWellHeaderHeight;
            }
        }

        /// <summary>
        /// Gets the height of the dock well header.
        /// </summary>
        /// <value>The height of the dock well header.</value>
        public virtual double DockWellHeaderHeight
        {
            get
            {
                if (_dockWellHeaderHeight < .1d)
                {
                    var font = new Typeface(DockWellHeaderFontFamily, DockWellHeaderFontStyle, DockWellHeaderFontWeight, FontStretches.Normal);
                    var ft = new FormattedText("X", CultureInfo.CurrentUICulture, FlowDirection.LeftToRight, font, DockWellHeaderFontSize, DockWellHeaderForegroundBrush)
                    {
                        TextAlignment = TextAlignment.Left,
                        Trimming = TextTrimming.CharacterEllipsis,
                    };
                    _dockWellHeaderHeight = ft.Height + 6d;
                }
                return _dockWellHeaderHeight;
            }
        }

        /// <summary>
        /// Gets the height of the dock well footer.
        /// </summary>
        /// <value>The height of the dock well footer.</value>
        public virtual double DockWellFooterHeight
        {
            get
            {
                if (_dockWellFooterHeight < .1d)
                {
                    var font = new Typeface(DockWellFooterFontFamily, DockWellFooterFontStyle, DockWellFooterFontWeight, FontStretches.Normal);
                    var ft = new FormattedText("X", CultureInfo.CurrentUICulture, FlowDirection.LeftToRight, font, DockWellFooterFontSize, DockWellFooterForegroundBrush)
                    {
                        TextAlignment = TextAlignment.Left,
                        Trimming = TextTrimming.CharacterEllipsis,
                    };
                    _dockWellFooterHeight = ft.Height + 6d;
                }
                return _dockWellFooterHeight;
            }
        }

        /// <summary>
        /// Returns the size of the client area (the area available for the actually hosted control, which excludes things like the area needed for the header) of the dock well
        /// </summary>
        /// <returns>Size.</returns>
        /// <exception cref="System.NotImplementedException"></exception>
        public Rect GetMainWellClientRect()
        {
            var fullRect = GetMainWellRect();
            if (fullRect.Width < .1d && fullRect.Height < .1d) return fullRect;
            return NewRect(fullRect.X, fullRect.Y + MainWellHeaderHeight, fullRect.Width, fullRect.Height - MainWellHeaderHeight);
        }

        /// <summary>
        /// Returns the size of the client area (the area available for the actually hosted control, which excludes things like the area needed for the header) of the dock well
        /// </summary>
        /// <param name="position">The position.</param>
        /// <returns>Size.</returns>
        /// <exception cref="System.NotImplementedException"></exception>
        public Rect GetDockWellClientRect(DockPosition position)
        {
            var fullRect = GetDockWellRect(position);
            if (fullRect.Width < .1d && fullRect.Height < .1d) return fullRect;

            // TODO: Should have special header and footer heights for dock wells
            return NewRect(fullRect.X, fullRect.Y + MainWellHeaderHeight, fullRect.Width, fullRect.Height - MainWellHeaderHeight - DockWellFooterHeight);
        }

        /// <summary>
        /// Returns the size of the client area (the area available for the actually hosted control, as well as other chrome) of the dock well
        /// </summary>
        /// <returns>Size.</returns>
        /// <exception cref="System.NotImplementedException"></exception>
        public Rect GetMainWellRect()
        {
            var height = TotalHeight;
            if (TopDockHeight > 0d) height -= (SplitterHeight + TopDockHeight);
            if (BottomDockHeight > 0d) height -= (SplitterHeight + BottomDockHeight);
            var width = TotalWidth;
            if (LeftDockWidth > 0d) width -= (SplitterWidth + LeftDockWidth);
            if (RightDockWidth > 0d) width -= (SplitterWidth + RightDockWidth);
            var top = 0d;
            if (TopDockHeight > 0d) top += SplitterHeight + TopDockHeight;
            var left = 0d;
            if (LeftDockWidth > 0d) left += SplitterWidth + LeftDockWidth;
            return NewRect(left, top, width, height);
        }

        /// <summary>
        /// Returns the size of the whole area (the area available for the actually hosted control, as well as other chrome) of the dock well
        /// </summary>
        /// <param name="position">The position.</param>
        /// <returns>Size.</returns>
        /// <exception cref="System.NotImplementedException"></exception>
        public Rect GetDockWellRect(DockPosition position)
        {
            var height = TotalHeight;
            var width = TotalWidth;
            var left = 0d;

            switch (position)
            {
                case DockPosition.Left:
                    return LeftDockWidth > 0d ? NewRect(0d, 0d, LeftDockWidth, height) : Rect.Empty;
                case DockPosition.Top:
                    if (LeftDockWidth > 0d)
                    {
                        width -= (LeftDockWidth + SplitterWidth);
                        left += LeftDockWidth + SplitterWidth;
                    }
                    if (RightDockWidth > 0d)
                    {
                        width -= (RightDockWidth + SplitterWidth);
                    }
                    return TopDockHeight > 0d ? NewRect(left, 0d, width, TopDockHeight) : Rect.Empty;
                case DockPosition.Right:
                    left = width - RightDockWidth;
                    return RightDockWidth > 0d ? NewRect(left, 0d, RightDockWidth, height) : Rect.Empty;
                case DockPosition.Bottom:
                    if (LeftDockWidth > 0d)
                    {
                        width -= (LeftDockWidth + SplitterWidth);
                        left += LeftDockWidth + SplitterWidth;
                    }
                    if (RightDockWidth > 0d)
                    {
                        width -= (RightDockWidth + SplitterWidth);
                    }
                    var top = height - BottomDockHeight;
                    return BottomDockHeight > 0d ? NewRect(left, top, width, BottomDockHeight) : Rect.Empty;
                case DockPosition.Main:
                    return GetMainWellRect();
            }
            return Rect.Empty;
        }

        /// <summary>Returns a new Rect and makes sure the values used for it are not invalid</summary>
        /// <param name="x">X</param>
        /// <param name="y">Y</param>
        /// <param name="width">Width.</param>
        /// <param name="height">Height.</param>
        /// <returns>Rect.</returns>
        private static Rect NewRect(double x, double y, double width, double height)
        {
            if (x < 0d) x = 0d;
            if (y < 0d) y = 0d;
            if (width < 0d) width = 0d;
            if (height < 0d) height = 0d;
            return GeometryHelper.NewRect(x, y, width, height);
        }

        /// <summary>
        /// Width of the left dock well
        /// </summary>
        /// <value>The width of the left dock.</value>
        public double LeftDockWidth { get; set; }

        /// <summary>
        /// Width of the right dock well
        /// </summary>
        /// <value>The width of the right dock.</value>
        public double RightDockWidth { get; set; }

        /// <summary>
        /// Height of the top dock well
        /// </summary>
        /// <value>The height of the top dock.</value>
        public double TopDockHeight { get; set; }

        /// <summary>
        /// Height of the bottom dock well
        /// </summary>
        /// <value>The height of the bottom dock.</value>
        public double BottomDockHeight { get; set; }

        /// <summary>
        /// Total height of the dock panel
        /// </summary>
        /// <value>The total height.</value>
        public double TotalHeight { get; set; }

        /// <summary>
        /// Total width of the dock panel
        /// </summary>
        /// <value>The total width.</value>
        public double TotalWidth { get; set; }

        /// <summary>
        /// Width of the splitter element
        /// </summary>
        /// <value>The width of the splitter.</value>
        public double SplitterWidth { get; set; }

        /// <summary>
        /// Height of the splitter element
        /// </summary>
        /// <value>The height of the splitter.</value>
        public double SplitterHeight { get; set; }

        /// <summary>
        /// Docked main elements
        /// </summary>
        /// <value>The main elements.</value>
        public List<DockedUIElement> MainElements { get; set; }

        /// <summary>
        /// Docked left elements
        /// </summary>
        /// <value>The left elements.</value>
        public List<DockedUIElement> LeftElements { get; set; }

        /// <summary>
        /// Docked top elements
        /// </summary>
        /// <value>The top elements.</value>
        public List<DockedUIElement> TopElements { get; set; }

        /// <summary>
        /// Docked right elements
        /// </summary>
        /// <value>The right elements.</value>
        public List<DockedUIElement> RightElements { get; set; }

        /// <summary>
        /// Docked bottom elements
        /// </summary>
        /// <value>The bottom elements.</value>
        public List<DockedUIElement> BottomElements { get; set; }

        /// <summary>
        /// Index of the selected element in the main well
        /// </summary>
        /// <value>The index of the main selected.</value>
        public int MainSelectedIndex { get; set; }

        /// <summary>
        /// Index of the selected element in the left well
        /// </summary>
        /// <value>The index of the left selected.</value>
        public int LeftSelectedIndex { get; set; }

        /// <summary>
        /// Index of the selected element in the top well
        /// </summary>
        /// <value>The index of the top selected.</value>
        public int TopSelectedIndex { get; set; }

        /// <summary>
        /// Index of the selected element in the right well
        /// </summary>
        /// <value>The index of the right selected.</value>
        public int RightSelectedIndex { get; set; }

        /// <summary>
        /// Index of the selected element in the bottom well
        /// </summary>
        /// <value>The index of the bottom selected.</value>
        public int BottomSelectedIndex { get; set; }

        private DockContainer _parent;

        /// <summary>
        /// Sets a reference to the actual doc container
        /// </summary>
        /// <param name="container">Container</param>
        public void SetDockContainer(DockContainer container)
        {
            _parent = container;
        }

        /// <summary> Draws the actual chrome around the docked objects </summary>
        /// <param name="dc">Drawing context</param>
        public void DrawChrome(DrawingContext dc)
        {
            DrawMainWellChrome(dc);
            DrawDockWellChrome(dc, LeftElements, GetDockWellRect(DockPosition.Left), LeftSelectedIndex);
            DrawDockWellChrome(dc, TopElements, GetDockWellRect(DockPosition.Top), TopSelectedIndex);
            DrawDockWellChrome(dc, RightElements, GetDockWellRect(DockPosition.Right), RightSelectedIndex);
            DrawDockWellChrome(dc, BottomElements, GetDockWellRect(DockPosition.Bottom), BottomSelectedIndex);
        }

        /// <summary>
        /// Draws the chrome for a window that is being dragged
        /// </summary>
        /// <param name="dc">The drawing context.</param>
        /// <param name="elements">The docked elements.</param>
        /// <param name="selectedIndex">Index of the selected dock element.</param>
        /// <param name="clientRect">The client area.</param>
        /// <param name="showTabs">Defines whether tabs headers should be shown.</param>
        /// <param name="firstTabOffset">In special cases, the first tab may be offset by a certain margin</param>
        public void DrawDockWindowChrome(DrawingContext dc, List<DockedUIElement> elements, int selectedIndex, Rect clientRect, bool showTabs, Point firstTabOffset)
        {
            DrawDockWellChrome(dc, elements, clientRect, selectedIndex, showTabs, firstTabOffset);
        }

        /// <summary>Draws the chrome around a dock well</summary>
        /// <param name="dc">The drawing context.</param>
        /// <param name="elements">The elements.</param>
        /// <param name="areaRect">The area rect.</param>
        /// <param name="selectedIndex">Index of the selected.</param>
        /// <param name="showTabs">Defines whether tabs headers should be shown.</param>
        /// <param name="firstTabOffset">In special cases, the first tab may be offset by a certain margin</param>
        protected virtual void DrawDockWellChrome(DrawingContext dc, List<DockedUIElement> elements, Rect areaRect, int selectedIndex, bool showTabs = true, Point firstTabOffset = new Point())
        {
            if (elements == null) return;

            if (areaRect.Width < .1d || areaRect.Height < .1d) return;

            var footerHeight = showTabs ? DockWellFooterHeight : 0d;

            var headerFont = new Typeface(DockWellHeaderFontFamily, DockWellHeaderFontStyle, DockWellHeaderFontWeight, FontStretches.Normal);
            var footerFont = new Typeface(DockWellFooterFontFamily, DockWellFooterFontStyle, DockWellFooterFontWeight, FontStretches.Normal);
            var outlinePen = new Pen(DockWellBorderBrush, 1d);

            dc.DrawRectangle(DockWellHeaderBackgroundBrush, null, NewRect(areaRect.Left, areaRect.Top, areaRect.Width, DockWellHeaderHeight + 1d));
            dc.DrawRectangle(DockWellHeaderBackgroundBrush, null, NewRect(areaRect.Left, areaRect.Top + DockWellHeaderHeight, areaRect.Width, areaRect.Height - DockWellHeaderHeight - footerHeight));
            DrawHorizontalLine(areaRect.Left, areaRect.Width, areaRect.Top, dc, outlinePen);
            DrawVerticalLine(areaRect.Top, areaRect.Height - footerHeight, areaRect.Left, dc, outlinePen);
            DrawVerticalLine(areaRect.Top, areaRect.Height - footerHeight, areaRect.Right, dc, outlinePen, true);

            // Header
            if (selectedIndex > -1 && elements.Count > selectedIndex)
            {
                var ft = new FormattedText(elements[selectedIndex].Title, CultureInfo.CurrentUICulture, FlowDirection.LeftToRight, headerFont, DockWellHeaderFontSize, DockWellHeaderForegroundBrush)
                {
                    TextAlignment = TextAlignment.Left,
                    Trimming = TextTrimming.CharacterEllipsis,
                    MaxTextHeight = Math.Max(MainWellHeaderHeight - 3d, 0),
                    MaxTextWidth = Math.Max(areaRect.Width - 10d, 0)
                };
                dc.DrawText(ft, new Point(areaRect.X + 5, areaRect.Y + 3d));
                dc.DrawRectangle(DockWellHeaderFlourishBrush, null, NewRect(areaRect.Left + ft.Width + 10d, areaRect.Top + 8d, areaRect.Width - ft.Width - 16d, 6d));
            }

            if (footerHeight > 0d)
            {
                // Rendering the footer elements
                var counter = 0;
                var currentLeft = areaRect.X;
                foreach (var element in elements)
                {
                    if (counter == selectedIndex)
                    {
                        var ft = new FormattedText(element.Title, CultureInfo.CurrentUICulture, FlowDirection.LeftToRight, footerFont, DockWellFooterFontSize, DockWellSelectedFooterForegroundBrush)
                        {
                            TextAlignment = TextAlignment.Left,
                            Trimming = TextTrimming.CharacterEllipsis,
                            MaxTextHeight = MainWellHeaderHeight - 5d,
                            MaxTextWidth = areaRect.Right - currentLeft,
                        };
                        dc.DrawRectangle(DockWellSelectedFooterBackgroundBrush, null, NewRect(currentLeft, areaRect.Bottom - footerHeight - 2d, ft.Width + 15d, footerHeight + 2d));
                        DrawVerticalLine(areaRect.Bottom - footerHeight - 2d, footerHeight + 2, currentLeft, dc, outlinePen);
                        DrawHorizontalLine(currentLeft, ft.Width + 15d, areaRect.Bottom, dc, outlinePen, true);
                        DrawVerticalLine(areaRect.Bottom - footerHeight, footerHeight, currentLeft + ft.Width + 15d, dc, outlinePen, true);
                        dc.DrawText(ft, new Point(currentLeft + 7, areaRect.Bottom - 20d));
                        _parent.AddHotArea(new DockedTabHotArea(_parent)
                        {
                            Position = element.Position,
                            NewIndex = counter,
                            AreaRectangle = NewRect(currentLeft, areaRect.Bottom - footerHeight - 1d, ft.Width + 15d, footerHeight + 1d)
                        });
                        currentLeft += ft.Width + 15;
                    }
                    else
                    {
                        var ft = new FormattedText(element.Title, CultureInfo.CurrentUICulture, FlowDirection.LeftToRight, footerFont, DockWellFooterFontSize, DockWellFooterForegroundBrush)
                        {
                            TextAlignment = TextAlignment.Left,
                            Trimming = TextTrimming.CharacterEllipsis,
                            MaxTextHeight = MainWellHeaderHeight - 5d,
                            MaxTextWidth = areaRect.Right - currentLeft
                        };
                        DrawHorizontalLine(currentLeft - 1d, ft.Width + 16d, areaRect.Bottom - footerHeight, dc, outlinePen, true);
                        dc.DrawText(ft, new Point(currentLeft + 7, areaRect.Bottom - 19d));
                        _parent.AddHotArea(new DockedTabHotArea(_parent)
                        {
                            Position = element.Position,
                            NewIndex = counter,
                            AreaRectangle = NewRect(currentLeft, areaRect.Bottom - footerHeight - 1d, ft.Width + 15d, footerHeight + 1d)
                        });
                        currentLeft += ft.Width + 15;
                    }
                    if (currentLeft >= areaRect.Right) break;
                    counter++;
                }
                if (currentLeft < areaRect.Right)
                    DrawHorizontalLine(currentLeft, areaRect.Right - currentLeft, areaRect.Bottom - footerHeight, dc, outlinePen, true);
            }
            else
                DrawHorizontalLine(areaRect.Left, areaRect.Width, areaRect.Bottom, dc, outlinePen, true);
        }

        private static void DrawHorizontalLine(double x, double width, double y, DrawingContext dc, Pen pen, bool isBottomEdge = false)
        {
            x = Math.Round(x, 0);
            width = Math.Round(width, 0);
            if (isBottomEdge) y -= 1d;
            y = Math.Round(y, 0);
            dc.DrawLine(pen, new Point(x, y + .5d), new Point(x + width, y + .5d));
        }

        private static void DrawVerticalLine(double y, double height, double x, DrawingContext dc, Pen pen, bool isRightEdge = false)
        {
            if (isRightEdge) x -= 1d;
            x = Math.Round(x, 0);
            height = Math.Round(height, 0);
            y = Math.Round(y, 0);
            dc.DrawLine(pen, new Point(x + .5d, y), new Point(x + .5d, y + height));
        }

        /// <summary>Draws the chrome around the main well.</summary>
        /// <param name="dc">The drawing context.</param>
        protected virtual void DrawMainWellChrome(DrawingContext dc)
        {
            if (MainElements == null) return;

            var areaRect = GetMainWellRect();
            if (areaRect.Width < .1d || areaRect.Height < .1d) return;

            var headerBackgroundBrush = MainWellHeaderBackgroundBrush;
            var font = new Typeface(MainWellHeaderFontFamily, MainWellHeaderFontStyle, MainWellHeaderFontWeight, FontStretches.Normal);

            // Drawing a nice stylistic border and a line across the bottom of the header area
            dc.DrawRectangle(MainWellBorderBrush, null, GeometryHelper.NewRect(areaRect.X, areaRect.Y + MainWellHeaderHeight, areaRect.Width, areaRect.Height - MainWellHeaderHeight));
            dc.DrawRectangle(headerBackgroundBrush, null, GeometryHelper.NewRect(areaRect.X, areaRect.Y + MainWellHeaderHeight - 2d, areaRect.Width, 2d));

            var counter = 0;
            var currentLeft = areaRect.X;
            foreach (var element in MainElements)
            {
                if (counter == MainSelectedIndex)
                {
                    var ft = new FormattedText(element.Title, CultureInfo.CurrentUICulture, FlowDirection.LeftToRight, font, MainWellHeaderFontSize, MainWellSelectedHeaderForegroundBrush)
                    {
                        TextAlignment = TextAlignment.Left,
                        Trimming = TextTrimming.CharacterEllipsis,
                        MaxTextHeight = MainWellHeaderHeight - 5d,
                        MaxTextWidth = areaRect.Right - currentLeft,
                    };
                    dc.DrawRectangle(headerBackgroundBrush, null, NewRect(Math.Round(currentLeft, 0), areaRect.Y, Math.Round(ft.Width + 15d, 0), MainWellHeaderHeight));
                    dc.DrawText(ft, new Point(currentLeft + 7, areaRect.Y + 2d));
                    currentLeft += ft.Width + 15;
                }
                else
                {
                    var ft = new FormattedText(element.Title, CultureInfo.CurrentUICulture, FlowDirection.LeftToRight, font, MainWellHeaderFontSize, MainWellHeaderForegroundBrush)
                    {
                        TextAlignment = TextAlignment.Left,
                        Trimming = TextTrimming.CharacterEllipsis,
                        MaxTextHeight = MainWellHeaderHeight - 5d,
                        MaxTextWidth = areaRect.Right - currentLeft
                    };
                    dc.DrawText(ft, new Point(currentLeft + 7, areaRect.Y + 2d));
                    _parent.AddHotArea(new DockedTabHotArea(_parent)
                    {
                        Position = DockPosition.Main,
                        NewIndex = counter,
                        AreaRectangle = GeometryHelper.NewRect(currentLeft, areaRect.Y, ft.Width + 15, MainWellHeaderHeight)
                    });
                    currentLeft += ft.Width + 15;
                }
                if (currentLeft >= areaRect.X + areaRect.Width) break;
                counter++;
            }
        }

        /// <summary>Brush used to render the tab header of the main well</summary>
        public Brush MainWellHeaderBackgroundBrush
        {
            get { return (Brush) GetValue(MainWellHeaderBackgroundBrushProperty); }
            set { SetValue(MainWellHeaderBackgroundBrushProperty, value); }
        }

        /// <summary>Brush used to render the tab header of the main well</summary>
        public static readonly DependencyProperty MainWellHeaderBackgroundBrushProperty = DependencyProperty.Register("MainWellHeaderBackgroundBrush", typeof (Brush), typeof (StandardDockWellRenderer), new PropertyMetadata(new SolidColorBrush(Color.FromRgb(0, 122, 203))));

        /// <summary>Border color of the main well</summary>
        public Brush MainWellBorderBrush
        {
            get { return (Brush) GetValue(MainWellBorderBrushProperty); }
            set { SetValue(MainWellBorderBrushProperty, value); }
        }

        /// <summary>Border color of the main well</summary>
        public static readonly DependencyProperty MainWellBorderBrushProperty = DependencyProperty.Register("MainWellBorderBrush", typeof (Brush), typeof (StandardDockWellRenderer), new PropertyMetadata(new SolidColorBrush(Color.FromRgb(203, 205, 218))));

        /// <summary>Border color of the dock well</summary>
        public Brush DockWellBorderBrush
        {
            get { return (Brush) GetValue(DockWellBorderBrushProperty); }
            set { SetValue(DockWellBorderBrushProperty, value); }
        }

        /// <summary>Border color of the dock well</summary>
        public static readonly DependencyProperty DockWellBorderBrushProperty = DependencyProperty.Register("DockWellBorderBrush", typeof (Brush), typeof (StandardDockWellRenderer), new PropertyMetadata(new SolidColorBrush(Color.FromRgb(203, 205, 218))));

        /// <summary>Background color of the active footer tab in the dock well</summary>
        public Brush DockWellSelectedFooterBackgroundBrush
        {
            get { return (Brush) GetValue(DockWellSelectedFooterBackgroundBrushProperty); }
            set { SetValue(DockWellSelectedFooterBackgroundBrushProperty, value); }
        }

        /// <summary>Background color of the active footer tab in the dock well</summary>
        public static readonly DependencyProperty DockWellSelectedFooterBackgroundBrushProperty = DependencyProperty.Register("DockWellSelectedFooterBackgroundBrush", typeof (Brush), typeof (StandardDockWellRenderer), new PropertyMetadata(new SolidColorBrush(Color.FromRgb(244, 244, 244))));

        /// <summary>Foreground brush for unselected headers</summary>
        public Brush MainWellHeaderForegroundBrush
        {
            get { return (Brush) GetValue(MainWellHeaderForegroundBrushProperty); }
            set { SetValue(MainWellHeaderForegroundBrushProperty, value); }
        }

        /// <summary>Foreground brush for unselected headers</summary>
        public static readonly DependencyProperty MainWellHeaderForegroundBrushProperty = DependencyProperty.Register("MainWellHeaderForegroundBrush", typeof (Brush), typeof (StandardDockWellRenderer), new PropertyMetadata(new SolidColorBrush(Color.FromRgb(0, 0, 0))));

        /// <summary>Brush used to fill the empty space in the header</summary>
        public Brush DockWellHeaderFlourishBrush
        {
            get { return (Brush) GetValue(DockWellHeaderFlourishBrushProperty); }
            set { SetValue(DockWellHeaderFlourishBrushProperty, value); }
        }

        /// <summary>Brush used to fill the empty space in the header</summary>
        public static readonly DependencyProperty DockWellHeaderFlourishBrushProperty = DependencyProperty.Register("DockWellHeaderFlourishBrush", typeof (Brush), typeof (StandardDockWellRenderer), new PropertyMetadata(new SolidColorBrush(Color.FromRgb(203, 205, 218))));

        /// <summary>Brush used to fill the docked header</summary>
        public Brush DockWellHeaderBackgroundBrush
        {
            get { return (Brush) GetValue(DockWellHeaderBackgroundBrushProperty); }
            set { SetValue(DockWellHeaderBackgroundBrushProperty, value); }
        }

        /// <summary>Brush used to fill the docked header</summary>
        public static readonly DependencyProperty DockWellHeaderBackgroundBrushProperty = DependencyProperty.Register("DockWellHeaderBackgroundBrush", typeof (Brush), typeof (StandardDockWellRenderer), new PropertyMetadata(new SolidColorBrush(Color.FromRgb(244, 244, 244))));

        /// <summary>Brush used to fill the docked active page</summary>
        public Brush DockWellActivePageBackgroundBrush
        {
            get { return (Brush) GetValue(DockWellActivePageBackgroundBrushProperty); }
            set { SetValue(DockWellActivePageBackgroundBrushProperty, value); }
        }

        /// <summary>Brush used to fill the docked active page</summary>
        public static readonly DependencyProperty DockWellActivePageBackgroundBrushProperty = DependencyProperty.Register("DockWellActivePageBackgroundBrush", typeof (Brush), typeof (StandardDockWellRenderer), new PropertyMetadata(new SolidColorBrush(Color.FromRgb(244, 244, 244))));

        /// <summary>Foreground brush for unselected dock headers</summary>
        public Brush DockWellHeaderForegroundBrush
        {
            get { return (Brush) GetValue(DockWellHeaderForegroundBrushProperty); }
            set { SetValue(DockWellHeaderForegroundBrushProperty, value); }
        }

        /// <summary>Foreground brush for unselected dock headers</summary>
        public static readonly DependencyProperty DockWellHeaderForegroundBrushProperty = DependencyProperty.Register("DockWellHeaderForegroundBrush", typeof (Brush), typeof (StandardDockWellRenderer), new PropertyMetadata(new SolidColorBrush(Color.FromRgb(0, 0, 0))));

        /// <summary>Foreground brush for unselected dock footers</summary>
        public Brush DockWellFooterForegroundBrush
        {
            get { return (Brush) GetValue(DockWellFooterForegroundBrushProperty); }
            set { SetValue(DockWellFooterForegroundBrushProperty, value); }
        }

        /// <summary>Foreground brush for unselected dock footers</summary>
        public static readonly DependencyProperty DockWellFooterForegroundBrushProperty = DependencyProperty.Register("DockWellFooterForegroundBrush", typeof (Brush), typeof (StandardDockWellRenderer), new PropertyMetadata(new SolidColorBrush(Color.FromRgb(0, 0, 0))));

        /// <summary>Foreground brush for selected dock footers</summary>
        public Brush DockWellSelectedFooterForegroundBrush
        {
            get { return (Brush) GetValue(DockWellSelectedFooterForegroundBrushProperty); }
            set { SetValue(DockWellSelectedFooterForegroundBrushProperty, value); }
        }

        /// <summary>Foreground brush for selected dock footers</summary>
        public static readonly DependencyProperty DockWellSelectedFooterForegroundBrushProperty = DependencyProperty.Register("DockWellSelectedFooterForegroundBrush", typeof (Brush), typeof (StandardDockWellRenderer), new PropertyMetadata(new SolidColorBrush(Color.FromRgb(0, 122, 203))));

        /// <summary>Foreground brush for unselected headers</summary>
        public Brush MainWellSelectedHeaderForegroundBrush
        {
            get { return (Brush) GetValue(MainWellSelectedHeaderForegroundBrushProperty); }
            set { SetValue(MainWellSelectedHeaderForegroundBrushProperty, value); }
        }

        /// <summary>Foreground brush for unselected headers</summary>
        public static readonly DependencyProperty MainWellSelectedHeaderForegroundBrushProperty = DependencyProperty.Register("MainWellSelectedHeaderForegroundBrush", typeof (Brush), typeof (StandardDockWellRenderer), new PropertyMetadata(new SolidColorBrush(Color.FromRgb(255, 255, 255))));

        /// <summary>Font size for the main well header</summary>
        public double MainWellHeaderFontSize
        {
            get { return (double) GetValue(MainWellHeaderFontSizeProperty); }
            set { SetValue(MainWellHeaderFontSizeProperty, value); }
        }

        /// <summary>Font size for the main well header</summary>
        public static readonly DependencyProperty MainWellHeaderFontSizeProperty = DependencyProperty.Register("MainWellHeaderFontSize", typeof (double), typeof (StandardDockWellRenderer), new PropertyMetadata(12d));

        /// <summary>Font family used for the main well header</summary>
        public FontFamily MainWellHeaderFontFamily
        {
            get { return (FontFamily) GetValue(MainWellHeaderFontFamilyProperty); }
            set { SetValue(MainWellHeaderFontFamilyProperty, value); }
        }

        /// <summary>Font family used for the main well header</summary>
        public static readonly DependencyProperty MainWellHeaderFontFamilyProperty = DependencyProperty.Register("MainWellHeaderFontFamily", typeof (FontFamily), typeof (StandardDockWellRenderer), new PropertyMetadata(new FontFamily("Segoe UI")));

        /// <summary>Font style used for the main well header</summary>
        public FontStyle MainWellHeaderFontStyle
        {
            get { return (FontStyle) GetValue(MainWellHeaderFontStyleProperty); }
            set { SetValue(MainWellHeaderFontStyleProperty, value); }
        }

        /// <summary>Font style used for the main well header</summary>
        public static readonly DependencyProperty MainWellHeaderFontStyleProperty = DependencyProperty.Register("MainWellHeaderFontStyle", typeof (FontStyle), typeof (StandardDockWellRenderer), new PropertyMetadata(FontStyles.Normal));

        /// <summary>Font weight used for the main well header</summary>
        public FontWeight MainWellHeaderFontWeight
        {
            get { return (FontWeight) GetValue(MainWellHeaderFontWeightProperty); }
            set { SetValue(MainWellHeaderFontWeightProperty, value); }
        }

        /// <summary>Font weight used for the main well header</summary>
        public static readonly DependencyProperty MainWellHeaderFontWeightProperty = DependencyProperty.Register("MainWellHeaderFontWeight", typeof (FontWeight), typeof (StandardDockWellRenderer), new PropertyMetadata(FontWeights.Normal));

        /// <summary>Font size for the dock well header</summary>
        public double DockWellHeaderFontSize
        {
            get { return (double) GetValue(DockWellHeaderFontSizeProperty); }
            set { SetValue(DockWellHeaderFontSizeProperty, value); }
        }

        /// <summary>Font size for the dock well header</summary>
        public static readonly DependencyProperty DockWellHeaderFontSizeProperty = DependencyProperty.Register("DockWellHeaderFontSize", typeof (double), typeof (StandardDockWellRenderer), new PropertyMetadata(12d));

        /// <summary>Font family used for the dock well header</summary>
        public FontFamily DockWellHeaderFontFamily
        {
            get { return (FontFamily) GetValue(DockWellHeaderFontFamilyProperty); }
            set { SetValue(DockWellHeaderFontFamilyProperty, value); }
        }

        /// <summary>Font family used for the dock well header</summary>
        public static readonly DependencyProperty DockWellHeaderFontFamilyProperty = DependencyProperty.Register("DockWellHeaderFontFamily", typeof (FontFamily), typeof (StandardDockWellRenderer), new PropertyMetadata(new FontFamily("Segoe UI")));

        /// <summary>Font style used for the dock well header</summary>
        public FontStyle DockWellHeaderFontStyle
        {
            get { return (FontStyle) GetValue(DockWellHeaderFontStyleProperty); }
            set { SetValue(DockWellHeaderFontStyleProperty, value); }
        }

        /// <summary>Font style used for the dock well header</summary>
        public static readonly DependencyProperty DockWellHeaderFontStyleProperty = DependencyProperty.Register("DockWellHeaderFontStyle", typeof (FontStyle), typeof (StandardDockWellRenderer), new PropertyMetadata(FontStyles.Normal));

        /// <summary>Font weight used for the dock well header</summary>
        public FontWeight DockWellHeaderFontWeight
        {
            get { return (FontWeight) GetValue(DockWellHeaderFontWeightProperty); }
            set { SetValue(DockWellHeaderFontWeightProperty, value); }
        }

        /// <summary>Font weight used for the dock well header</summary>
        public static readonly DependencyProperty DockWellHeaderFontWeightProperty = DependencyProperty.Register("DockWellHeaderFontWeight", typeof (FontWeight), typeof (StandardDockWellRenderer), new PropertyMetadata(FontWeights.Normal));

        /// <summary>Font size for the dock well footer</summary>
        public double DockWellFooterFontSize
        {
            get { return (double) GetValue(DockWellFooterFontSizeProperty); }
            set { SetValue(DockWellFooterFontSizeProperty, value); }
        }

        /// <summary>Font size for the dock well footer</summary>
        public static readonly DependencyProperty DockWellFooterFontSizeProperty = DependencyProperty.Register("DockWellFooterFontSize", typeof (double), typeof (StandardDockWellRenderer), new PropertyMetadata(12d));

        /// <summary>Font family used for the dock well footer</summary>
        public FontFamily DockWellFooterFontFamily
        {
            get { return (FontFamily) GetValue(DockWellFooterFontFamilyProperty); }
            set { SetValue(DockWellFooterFontFamilyProperty, value); }
        }

        /// <summary>Font family used for the dock well footer</summary>
        public static readonly DependencyProperty DockWellFooterFontFamilyProperty = DependencyProperty.Register("DockWellFooterFontFamily", typeof (FontFamily), typeof (StandardDockWellRenderer), new PropertyMetadata(new FontFamily("Segoe UI")));

        /// <summary>Font style used for the dock well footer</summary>
        public FontStyle DockWellFooterFontStyle
        {
            get { return (FontStyle) GetValue(DockWellFooterFontStyleProperty); }
            set { SetValue(DockWellFooterFontStyleProperty, value); }
        }

        /// <summary>Font style used for the dock well footer</summary>
        public static readonly DependencyProperty DockWellFooterFontStyleProperty = DependencyProperty.Register("DockWellFooterFontStyle", typeof (FontStyle), typeof (StandardDockWellRenderer), new PropertyMetadata(FontStyles.Normal));

        /// <summary>Font weight used for the dock well footer</summary>
        public FontWeight DockWellFooterFontWeight
        {
            get { return (FontWeight) GetValue(DockWellFooterFontWeightProperty); }
            set { SetValue(DockWellFooterFontWeightProperty, value); }
        }

        /// <summary>Font weight used for the dock well footer</summary>
        public static readonly DependencyProperty DockWellFooterFontWeightProperty = DependencyProperty.Register("DockWellFooterFontWeight", typeof (FontWeight), typeof (StandardDockWellRenderer), new PropertyMetadata(FontWeights.Normal));

        private double _mainWellHeaderHeight;
        private double _dockWellHeaderHeight;
        private double _dockWellFooterHeight;
    }

    /// <summary>
    /// Special area within a panel that is 'hot' (such as when the mouse moves over)
    /// </summary>
    public class HotArea
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="HotArea"/> class.
        /// </summary>
        public HotArea(DockContainer parent)
        {
            Parent = parent;
        }

        /// <summary>The rectangle that's 'hot'</summary>
        /// <value>The area rectangle.</value>
        public Rect AreaRectangle { get; set; }

        /// <summary>Mouse cursor used when the mouse moves over the hot area</summary>
        /// <value>The special mouse cursor.</value>
        public Cursor SpecialMouseCursor { get; set; }

        /// <summary>DockContainer this element goes with</summary>
        /// <value>The parent.</value>
        public DockContainer Parent { get; private set; }

        /// <summary>Fires when the mouse moves over the hot area</summary>
        public MouseEventHandler MouseOver { get; set; }

        /// <summary>Fires when the left mouse button is pressed over the hot area</summary>
        public MouseButtonEventHandler MouseDown { get; set; }

        /// <summary>Fires when the mouse clicks in the hot area</summary>
        public MouseButtonEventHandler MouseClick { get; set; }
    }

    /// <summary>
    /// Special hot area used for splitters
    /// </summary>
    public class SplitterHotArea : HotArea
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="HotArea" /> class.
        /// </summary>
        /// <param name="parent">The parent.</param>
        /// <param name="cursor">The cursor.</param>
        /// <param name="splitterIndex">Index of the splitter.</param>
        public SplitterHotArea(DockContainer parent, Cursor cursor, int splitterIndex) : base(parent)
        {
            SpecialMouseCursor = cursor;
            _splitterIndex = splitterIndex;

            MouseDown += (sender, args) =>
            {
                Mouse.Capture(Parent);
                args.Handled = true;
                Parent.MouseDownAndMoveOverride = e =>
                {
                    var position = e.GetPosition(Parent);
                    Mouse.SetCursor(SpecialMouseCursor);
                    switch (_splitterIndex)
                    {
                        case 0: // Left splitter
                            var x = Math.Max(position.X, 25d);
                            if (x > Parent.ActualWidth - Parent.RightDockWidth - 25d) x = Parent.ActualWidth - Parent.RightDockWidth - 25d;
                            x = Math.Max(x, 25d);
                            Parent.LeftDockWidth = x;
                            break;
                        case 1: // Top splitter
                            var y = Math.Max(position.Y, 50d);
                            if (y > Parent.ActualHeight - Parent.BottomDockHeight - 40d) y = Parent.ActualHeight - Parent.BottomDockHeight - 40d;
                            y = Math.Max(y, 50d);
                            Parent.TopDockHeight = y;
                            break;
                        case 2: // Right splitter
                            var x2 = Math.Min(position.X, Parent.ActualWidth - 25d);
                            if (x2 < Parent.LeftDockWidth + 25d) x2 = Parent.LeftDockWidth + 25d;
                            x2 = Math.Min(x2, Parent.ActualWidth - 25d);
                            Parent.RightDockWidth = Parent.ActualWidth - x2;
                            break;
                        case 3: // Bottom splitter
                            var y2 = Math.Min(position.Y, Parent.ActualHeight - 50d);
                            if (y2 < Parent.TopDockHeight + 40d) y2 = Parent.TopDockHeight + 40d;
                            y2 = Math.Min(y2, Parent.ActualHeight - 50d);
                            Parent.BottomDockHeight = Parent.ActualHeight - y2;
                            break;
                    }
                };
            };
        }

        private readonly int _splitterIndex;
    }

    /// <summary>
    /// Special hot area for docked tab headers
    /// </summary>
    public class DockedTabHotArea : HotArea
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DockedTabHotArea" /> class.
        /// </summary>
        /// <param name="parent">The parent.</param>
        public DockedTabHotArea(DockContainer parent) : base(parent)
        {
            SpecialMouseCursor = Cursors.Hand;

            MouseClick += (sender, args) =>
            {
                switch (Position)
                {
                    case DockPosition.Main:
                        Parent.MainDockSelectedIndex = NewIndex;
                        break;
                    case DockPosition.Left:
                        Parent.LeftDockSelectedIndex = NewIndex;
                        break;
                    case DockPosition.Top:
                        Parent.TopDockSelectedIndex = NewIndex;
                        break;
                    case DockPosition.Right:
                        Parent.RightDockSelectedIndex = NewIndex;
                        break;
                    case DockPosition.Bottom:
                        Parent.BottomDockSelectedIndex = NewIndex;
                        break;
                }
            };

            MouseDown += (sender, args) =>
            {
                if (args.LeftButton != MouseButtonState.Pressed) return;
                var downPosition = args.GetPosition(Parent);
                Mouse.Capture(Parent);

                Parent.MouseDownAndMoveOverride = e =>
                {
                    if (e.LeftButton != MouseButtonState.Pressed) return;
                    var currentPosition = e.GetPosition(Parent);
                    if (currentPosition.X < downPosition.X - 20d || currentPosition.X > downPosition.X + 20d ||
                        currentPosition.Y < downPosition.Y - 20d || currentPosition.Y > downPosition.Y + 20d)
                    {
                        // The mouse moved far enough in the down position for us to consider this a drag operation
                        // Note: We may want to handle this differently in the future
                        if (_currentDragWindow == null)
                        {
                            var dockedElements = Parent.SecondaryDockWell.Where(d => d.Position == Position && d.IsDocked).ToList();
                            if (NewIndex >= dockedElements.Count) return;
                            var dockedElement = dockedElements[NewIndex];
                            if (dockedElement == null) return;
                            _currentDragWindow = new DockWellFloatWindow(Parent, dockedElement, Parent.DockWellRenderer, Parent.DockWellRenderer.DockWellHeaderHeight, Parent.DockWellRenderer.DockWellFooterHeight);
                            switch (Position)
                            {
                                case DockPosition.Left:
                                    _currentDragWindow.Height = Parent.ActualHeight;
                                    _currentDragWindow.Width = Parent.LeftDockWidth;
                                    var totalDocked = Parent.SecondaryDockWell.Count(d => d.Position == Position && d.IsDocked);
                                    if (totalDocked > 0 && Parent.LeftDockSelectedIndex >= totalDocked) Parent.LeftDockSelectedIndex = totalDocked - 1;
                                    break;
                                case DockPosition.Top:
                                    _currentDragWindow.Height = Parent.TopDockHeight;
                                    _currentDragWindow.Width = Parent.ActualWidth - Parent.LeftDockWidth - Parent.RightDockWidth;
                                    var totalDocked2 = Parent.SecondaryDockWell.Count(d => d.Position == Position && d.IsDocked);
                                    if (totalDocked2 > 0 && Parent.TopDockSelectedIndex >= totalDocked2) Parent.TopDockSelectedIndex = totalDocked2 - 1;
                                    break;
                                case DockPosition.Right:
                                    _currentDragWindow.Height = Parent.ActualHeight;
                                    _currentDragWindow.Width = Parent.RightDockWidth;
                                    var totalDocked3 = Parent.SecondaryDockWell.Count(d => d.Position == Position && d.IsDocked);
                                    if (totalDocked3 > 0 && Parent.RightDockSelectedIndex >= totalDocked3) Parent.RightDockSelectedIndex = totalDocked3 - 1;
                                    break;
                                case DockPosition.Bottom:
                                    _currentDragWindow.Height = Parent.BottomDockHeight;
                                    _currentDragWindow.Width = Parent.ActualWidth - Parent.LeftDockWidth - Parent.RightDockWidth;
                                    var totalDocked4 = Parent.SecondaryDockWell.Count(d => d.Position == Position && d.IsDocked);
                                    if (totalDocked4 > 0 && Parent.BottomDockSelectedIndex >= totalDocked4) Parent.BottomDockSelectedIndex = totalDocked4 - 1;
                                    break;
                            }
                            Mouse.Capture(null);
                            _currentDragWindow.DragMove();
                        }
                    }
                };
            };
        }

        private Window _currentDragWindow;

        /// <summary>Dock position</summary>
        public DockPosition Position { get; set; }

        /// <summary>New selected index when clicked</summary>
        public int NewIndex { get; set; }
    }

    /// <summary>Window class used to host floating secondary dock elements</summary>
    public class DockWellFloatWindow : Window
    {
        private readonly DockContainer _parent;
        private readonly DockedUIElement _dockedElement;
        private readonly IDockWellRenderer _renderer;

        private readonly double _headerHeight;
        private readonly double _footerHeight;

        private bool _tabsVisible = true;

        /// <summary>
        /// Initializes a new instance of the <see cref="DockWellFloatWindow" /> class.
        /// </summary>
        /// <param name="parent">The parent.</param>
        /// <param name="dockedElement">The docked element.</param>
        /// <param name="renderer">The dock well renderer.</param>
        /// <param name="headerHeight">Height of the header.</param>
        /// <param name="footerHeight">Height of the footer.</param>
        public DockWellFloatWindow(DockContainer parent, DockedUIElement dockedElement, IDockWellRenderer renderer, double headerHeight, double footerHeight)
        {
            WindowStyle = WindowStyle.None;
            AllowsTransparency = true;
            Background = Brushes.Transparent;
            ShowInTaskbar = false;

            _parent = parent;
            _dockedElement = dockedElement;
            _renderer = renderer;
            _headerHeight = headerHeight;
            _footerHeight = footerHeight;

            DataContext = parent.DataContext;

            var oldHost = _parent.FindTemplatedItemsControl();
            if (oldHost != null)
                oldHost.Items.Remove(dockedElement.Element);
            else
                _parent.Children.Remove(dockedElement.Element);

            dockedElement.IsDocked = false;
            Content = dockedElement.Element;
            dockedElement.Element.Visibility = Visibility.Visible;
            Title = dockedElement.Title;

            WindowEx.SetAutoWindowDragEnabled(this, true);
            WindowEx.SetAutoWindowResizingEnabled(this, true);

            Show();
            _parent.InvalidateArrange();
            _parent.InvalidateMeasure();
            _parent.InvalidateVisual();
        }

        /// <summary>
        /// When overridden in a derived class, participates in rendering operations that are directed by the layout system. The rendering instructions for this element are not used directly when this method is invoked, and are instead preserved for later asynchronous use by layout and drawing.
        /// </summary>
        /// <param name="drawingContext">The drawing instructions for a specific element. This context is provided to the layout system.</param>
        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);

            if (_dockedElement == null) return;
            var elements = new List<DockedUIElement> {_dockedElement};
            _renderer.DrawDockWindowChrome(drawingContext, elements, 0, GeometryHelper.NewRect(0d, 0d, ActualWidth, ActualHeight), _tabsVisible, new Point());
        }

        /// <summary>
        /// Raises the <see cref="E:System.Windows.Window.Closing" /> event.
        /// </summary>
        /// <param name="e">A <see cref="T:System.ComponentModel.CancelEventArgs" /> that contains the event data.</param>
        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);
            _dockedElement.IsDocked = true;
            var element = Content as UIElement;
            Content = null;
            if (element != null)
            {
                var itemsControl = _parent.FindTemplatedItemsControl();
                if (itemsControl != null)
                    itemsControl.Items.Add(element);
                else
                    _parent.Children.Add(element);
            }
            _parent.InvalidateArrange();
            _parent.InvalidateMeasure();
            _parent.InvalidateVisual();
        }

        /// <summary>
        /// Invoked when an unhandled <see cref="E:System.Windows.Input.Mouse.MouseUp" /> routed event reaches an element in its route that is derived from this class. Implement this method to add class handling for this event.
        /// </summary>
        /// <param name="e">The <see cref="T:System.Windows.Input.MouseButtonEventArgs" /> that contains the event data. The event data reports that the mouse button was released.</param>
        protected override void OnMouseUp(MouseButtonEventArgs e)
        {
            if (_tabsVisible)
            {
                _tabsVisible = false;
                InvalidateArrange();
                InvalidateVisual();
            }

            base.OnMouseUp(e);
        }

        /// <summary>
        /// Override this method to measure the size of a window.
        /// </summary>
        /// <param name="availableSize">A <see cref="T:System.Windows.Size" /> that reflects the available size that this window can give to the child. Infinity can be given as a value to indicate that the window will size to whatever content is available.</param>
        /// <returns>A <see cref="T:System.Windows.Size" /> that reflects the size that this window determines it needs during layout, based on its calculations of children's sizes.</returns>
        protected override Size MeasureOverride(Size availableSize)
        {
            var element = Content as UIElement;
            if (element != null && ActualHeight > 0d && ActualWidth > 0d)
            {
                var areaSize = GeometryHelper.NewSize(ActualWidth - 2d, ActualHeight - _headerHeight - (_tabsVisible ? _footerHeight : 0d) - 1d);
                element.Measure(areaSize);
                return GeometryHelper.NewSize(ActualWidth, ActualHeight);
            }
            return base.MeasureOverride(availableSize);
        }

        /// <summary>
        /// Override this method to arrange and size a window and its child elements.
        /// </summary>
        /// <param name="arrangeBounds">A <see cref="T:System.Windows.Size" /> that reflects the final size that the window should use to arrange itself and its children.</param>
        /// <returns>A <see cref="T:System.Windows.Size" /> that reflects the actual size that was used.</returns>
        protected override Size ArrangeOverride(Size arrangeBounds)
        {
            var element = Content as UIElement;
            if (element != null && ActualHeight > 0d && ActualWidth > 0d)
            {
                var areaRect = GeometryHelper.NewRect(1d, _headerHeight, ActualWidth - 2d, ActualHeight - _headerHeight - (_tabsVisible ? _footerHeight : 0d) - 1d);
                element.Arrange(areaRect);
            }

            return arrangeBounds;
        }
    }
}