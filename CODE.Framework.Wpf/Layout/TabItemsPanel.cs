using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using CODE.Framework.Wpf.Utilities;

namespace CODE.Framework.Wpf.Layout
{
    /// <summary>
    /// This panel arranges all child elements as tab items, and uses SimpleView.Title (or View.Title) as the header
    /// </summary>
    /// <seealso cref="System.Windows.Controls.Panel" />
    public class TabItemsPanel : Panel
    {
        private int _selectedPage = -1;

        /// <summary>
        /// Invoked when the <see cref="T:System.Windows.Media.VisualCollection" /> of a visual object is modified.
        /// </summary>
        /// <param name="visualAdded">The <see cref="T:System.Windows.Media.Visual" /> that was added to the collection.</param>
        /// <param name="visualRemoved">The <see cref="T:System.Windows.Media.Visual" /> that was removed from the collection.</param>
        protected override void OnVisualChildrenChanged(DependencyObject visualAdded, DependencyObject visualRemoved)
        {
            var element = visualAdded as UIElement;
            if (element != null)
                element.Visibility = Visibility.Collapsed;

            base.OnVisualChildrenChanged(visualAdded, visualRemoved);
        }

        /// <summary>
        /// Gets or sets the selected page.
        /// </summary>
        /// <value>The selected page.</value>
        public int SelectedPage
        {
            get { return _selectedPage; }
            set
            {
                if (_selectedPage == value) return;
                _selectedPage = value;

                foreach (var element in Children.OfType<UIElement>())
                    element.Visibility = Visibility.Collapsed;
                if (_selectedPage < Children.Count)
                    Children[_selectedPage].Visibility = Visibility.Visible;

                InvalidateVisual();
            }
        }

        /// <summary>
        /// When overridden in a derived class, measures the size in layout required for child elements and determines a size for the <see cref="T:System.Windows.FrameworkElement" />-derived class.
        /// </summary>
        /// <param name="availableSize">The available size that this element can give to child elements. Infinity can be specified as a value to indicate that the element will size to whatever content is available.</param>
        /// <returns>The size that this element determines it needs during layout, based on its calculations of child element sizes.</returns>
        protected override Size MeasureOverride(Size availableSize)
        {
            if (HeaderRenderer == null) HeaderRenderer = new TabItemsHeaderRenderer();

            var widest = 0d;
            var tallest = 0d;
            var availableItemSize = HeaderRenderer.GetClientRect(this, availableSize).Size;
            foreach (var element in Children.OfType<UIElement>().Where(e => e.Visibility == Visibility.Visible))
            {
                element.Measure(availableItemSize);
                widest = Math.Max(widest, element.DesiredSize.Width);
                tallest= Math.Max(tallest, element.DesiredSize.Height);
            }

            base.MeasureOverride(availableSize);

            if (!double.IsInfinity(availableSize.Width) && !double.IsInfinity(availableSize.Height)) return availableSize;
            return new Size(widest, tallest);
        }

        /// <summary>
        /// When overridden in a derived class, positions child elements and determines a size for a <see cref="T:System.Windows.FrameworkElement" /> derived class.
        /// </summary>
        /// <param name="finalSize">The final area within the parent that this element should use to arrange itself and its children.</param>
        /// <returns>The actual size used.</returns>
        protected override Size ArrangeOverride(Size finalSize)
        {
            if (HeaderRenderer == null) HeaderRenderer = new TabItemsHeaderRenderer();
            var clientRect = HeaderRenderer.GetClientRect(this, finalSize);

            foreach (var element in Children.OfType<UIElement>().Where(e => e.Visibility == Visibility.Visible))
                element.Arrange(clientRect);
            base.ArrangeOverride(finalSize);
            return clientRect.Size;
        }

        /// <summary>
        /// Draws the content of a <see cref="T:System.Windows.Media.DrawingContext" /> object during the render pass of a <see cref="T:System.Windows.Controls.Panel" /> element.
        /// </summary>
        /// <param name="dc">The <see cref="T:System.Windows.Media.DrawingContext" /> object to draw.</param>
        protected override void OnRender(DrawingContext dc)
        {
            base.OnRender(dc);

            if (HeaderRenderer == null) HeaderRenderer = new TabItemsHeaderRenderer();
            HeaderRenderer.Render(dc, this);
        }

        /// <summary>
        /// Invoked when an unhandled <see cref="E:System.Windows.UIElement.MouseLeftButtonDown" /> routed event is raised on this element. Implement this method to add class handling for this event.
        /// </summary>
        /// <param name="e">The <see cref="T:System.Windows.Input.MouseButtonEventArgs" /> that contains the event data. The event data reports that the left mouse button was pressed.</param>
        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            if (HeaderRenderer == null) HeaderRenderer = new TabItemsHeaderRenderer();
            var position = e.GetPosition(this);
            HeaderRenderer.MouseClick(position, e, this);
            base.OnMouseLeftButtonDown(e);
        }

        /// <summary>
        /// Sets the visibility of the tabs
        /// </summary>
        public static readonly DependencyProperty TabVisibilityProperty = DependencyProperty.RegisterAttached("TabVisibility", typeof (Visibility), typeof (TabItemsPanel), new PropertyMetadata(Visibility.Visible, OnTabVisibilityChanged));

        /// <summary>
        /// Fires when tab visibility changes
        /// </summary>
        /// <param name="d">The object the visibility changed on</param>
        /// <param name="e">The <see cref="DependencyPropertyChangedEventArgs"/> instance containing the event data.</param>
        private static void OnTabVisibilityChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var element = d as FrameworkElement;
            if (element == null) return;
            var tabItemsPanel = ElementHelper.FindVisualTreeParent<TabItemsPanel>(element);
            if (tabItemsPanel != null)
                tabItemsPanel.InvalidateVisual();
        }

        /// <summary>
        /// Sets the visibility of the tabs
        /// </summary>
        /// <param name="d">The item to get tab visibility for.</param>
        /// <returns>Visibility.</returns>
        public static Visibility GetTabVisibility(DependencyObject d)
        {
            return (Visibility) d.GetValue(TabVisibilityProperty);
        }

        /// <summary>
        /// Sets the visibility of the tabs
        /// </summary>
        /// <param name="d">The item to set tab visibility on.</param>
        /// <param name="value">Visibility.</param>
        public static void SetTabVisibility(DependencyObject d, Visibility value)
        {
            d.SetValue(TabVisibilityProperty, value);
        }

        /// <summary>
        /// Header renderer used to create the visuals for the tab headers and borders.
        /// </summary>
        /// <value>The header renderer.</value>
        public TabItemsHeaderRenderer HeaderRenderer
        {
            get { return (TabItemsHeaderRenderer)GetValue(HeaderRendererProperty); }
            set { SetValue(HeaderRendererProperty, value); }
        }

        /// <summary>
        /// Header renderer used to create the visuals for the tab headers and borders.
        /// </summary>
        public static readonly DependencyProperty HeaderRendererProperty = DependencyProperty.Register("HeaderRenderer", typeof(TabItemsHeaderRenderer), typeof(TabItemsPanel), new PropertyMetadata(null));
    }

    /// <summary>
    /// Base class used to render tab items.
    /// </summary>
    public class TabItemsHeaderRenderer : DependencyObject
    {
        private readonly List<string> _pageHeaders = new List<string>();
        private readonly List<Rect> _headerRectangles = new List<Rect>();

        /// <summary>
        /// Header height
        /// </summary>
        public double HeaderHeight
        {
            get { return (double)GetValue(HeaderHeightProperty); }
            set { SetValue(HeaderHeightProperty, value); }
        }

        /// <summary>
        /// Header height
        /// </summary>
        public static readonly DependencyProperty HeaderHeightProperty = DependencyProperty.Register("HeaderHeight", typeof(double), typeof(TabItemsHeaderRenderer), new PropertyMetadata(30d));

        /// <summary>
        /// Font Family.
        /// </summary>
        /// <value>The font family.</value>
        public FontFamily FontFamily
        {
            get { return (FontFamily)GetValue(FontFamilyProperty); }
            set { SetValue(FontFamilyProperty, value); }
        }

        /// <summary>
        /// Font Family.
        /// </summary>
        public static readonly DependencyProperty FontFamilyProperty = DependencyProperty.Register("FontFamily", typeof(FontFamily), typeof(TabItemsHeaderRenderer), new PropertyMetadata(new FontFamily("Segoe UI")));

        /// <summary>
        /// Returns the size and position of the client area for child elements
        /// </summary>
        /// <param name="panel">The tab items panel.</param>
        /// <param name="finalSize">The size of the tab items panel.</param>
        /// <returns>Rect.</returns>
        public Rect GetClientRect(TabItemsPanel panel, Size finalSize)
        {
            return GeometryHelper.NewRect(2, HeaderHeight + 2, finalSize.Width - 4, finalSize.Height - HeaderHeight - 4);
        }

        /// <summary>
        /// Renders the actual tabs
        /// </summary>
        /// <param name="dc">The dc.</param>
        /// <param name="tabItemsPanel">The tab items panel.</param>
        public void Render(DrawingContext dc, TabItemsPanel tabItemsPanel)
        {
            // Making sure we have a selected page.
            if (tabItemsPanel.SelectedPage < 0)
            {
                var visibleCounter = -1;
                foreach (var element in tabItemsPanel.Children.OfType<UIElement>())
                {
                    visibleCounter++;
                    if (TabItemsPanel.GetTabVisibility(element) == Visibility.Visible)
                    {
                        tabItemsPanel.SelectedPage = visibleCounter;
                        break;
                    }
                }
                return;
            }

            // Making sure the selected page is in fact visible
            var allPages = tabItemsPanel.Children.OfType<UIElement>().ToList();
            if (TabItemsPanel.GetTabVisibility(allPages[tabItemsPanel.SelectedPage]) != Visibility.Visible)
            {
                var visibleCounter = -1;
                foreach (var element in tabItemsPanel.Children.OfType<UIElement>())
                {
                    visibleCounter++;
                    if (TabItemsPanel.GetTabVisibility(element) == Visibility.Visible)
                    {
                        tabItemsPanel.SelectedPage = visibleCounter;
                        break;
                    }
                }
                return;
            }

            _pageHeaders.Clear();
            foreach (var element in tabItemsPanel.Children.OfType<UIElement>())
            {
                var header = SimpleView.GetTitle(element).Trim();
                if (string.IsNullOrEmpty(header)) header = "Item";
                _pageHeaders.Add(header);
            }

            _headerRectangles.Clear();

            var boldType = new Typeface(FontFamily, FontStyles.Normal, FontWeights.Bold, FontStretches.Normal);
            var regularType = new Typeface(new FontFamily("Segoe UI"), FontStyles.Normal, FontWeights.Normal, FontStretches.Normal);

            var linePen = new Pen(Brushes.Gray, 1d);
            var highlightPen = new Pen(Brushes.RoyalBlue, 2d);

            dc.DrawLine(linePen, new Point(0, 29.5), new Point(tabItemsPanel.ActualWidth, 29.5));

            var counter = -1;
            var left = 5d;
            foreach (var element in tabItemsPanel.Children.OfType<UIElement>())
            {
                counter++;

                if (TabItemsPanel.GetTabVisibility(element) != Visibility.Visible)
                {
                    _headerRectangles.Add(Rect.Empty);
                    continue;
                }

                if (counter == tabItemsPanel.SelectedPage)
                {
                    var ft = new FormattedText(_pageHeaders[counter], CultureInfo.CurrentUICulture, FlowDirection.LeftToRight, boldType, 11d, Brushes.Black);
                    var textWidth = ft.Width;
                    var headerRect = new Rect(left, 0, textWidth + 20, 30);
                    _headerRectangles.Add(headerRect);
                    dc.DrawRectangle(Brushes.White, null, headerRect);
                    dc.DrawLine(linePen, new Point(left, 3), new Point(left, 30));
                    dc.DrawLine(linePen, new Point(left + textWidth + 20, 3), new Point(left + textWidth + 20, 30));
                    dc.DrawLine(highlightPen, new Point(left, 2), new Point(left + textWidth + 20, 2));
                    dc.DrawText(ft, new Point(left + 10, 7));
                    left += textWidth + 20;
                }
                else
                {
                    var ft = new FormattedText(_pageHeaders[counter], CultureInfo.CurrentUICulture, FlowDirection.LeftToRight, regularType, 11d, Brushes.Black);
                    var textWidth = ft.Width;
                    var headerRect = new Rect(left, 0, textWidth + 20, 30);
                    _headerRectangles.Add(headerRect);
                    dc.DrawRectangle(Brushes.Transparent, null, headerRect); // Makes the area hit-test visible
                    dc.DrawText(ft, new Point(left + 10, 7));
                    left += textWidth + 20;
                }
            }
        }

        /// <summary>
        /// Handles a mouse click
        /// </summary>
        /// <param name="position">The position.</param>
        /// <param name="mouseButtonEventArgs">The <see cref="MouseButtonEventArgs"/> instance containing the event data.</param>
        /// <param name="tabItemsPanel">The tab items panel.</param>
        public void MouseClick(Point position, MouseButtonEventArgs mouseButtonEventArgs, TabItemsPanel tabItemsPanel)
        {
            var counter = -1;
            foreach (var rect in _headerRectangles)
            {
                counter++;
                if (rect.Contains(position))
                {
                    tabItemsPanel.SelectedPage = counter;
                    mouseButtonEventArgs.Handled = true;
                    return;
                }
            }
        }
    }
}