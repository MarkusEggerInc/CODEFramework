using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using CODE.Framework.Wpf.Utilities;

namespace CODE.Framework.Wpf.Layout
{
    /// <summary>
    /// This layout panel arranges objects in multiple "blades" in a left-to-right fashion
    /// </summary>
    public class BladePanel : Panel
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BladePanel"/> class.
        /// </summary>
        public BladePanel()
        {
            VerticalAlignment = VerticalAlignment.Stretch;
            HorizontalAlignment = HorizontalAlignment.Stretch;
            ClipToBounds = true;

            Loaded += (s, e) => CreateScrollbars();

            SizeChanged += (s, e) => InvalidateVisual();
            IsHitTestVisible = true;
        }

        private void CreateScrollbars()
        {
            if (Parent != null && !(Parent is AdornerDecorator) && (Parent is Grid || Parent is SimpleView || Parent is ContentControl))
            {
                // We need to make sure this control is directly within an adorner decorator
                // so elements in the adorner layer (scroll bars) behave correctly in the Z-Order
                var decorator = new AdornerDecorator
                {
                    VerticalAlignment = VerticalAlignment,
                    HorizontalAlignment = HorizontalAlignment,
                    Margin = new Thickness(Margin.Left, Margin.Top, Margin.Right, Margin.Bottom),
                    Width = Width,
                    Height = Height
                };

                Grid.SetColumn(decorator, Grid.GetColumn(this));
                Grid.SetRow(decorator, Grid.GetRow(this));
                Grid.SetColumnSpan(decorator, Grid.GetColumnSpan(this));
                Grid.SetRowSpan(decorator, Grid.GetRowSpan(this));

                SimpleView.SetUIElementTitle(decorator, SimpleView.GetUIElementTitle(this));
                SimpleView.SetUIElementType(decorator, SimpleView.GetUIElementType(this));

                var parentPanel = Parent as Panel;
                if (parentPanel != null)
                {
                    var childItemIndex = -1;
                    var found = false;
                    foreach (var child in parentPanel.Children)
                    {
                        childItemIndex++;
                        if (child == this)
                        {
                            found = true;
                            break;
                        }
                    }
                    if (found)
                    {
                        parentPanel.Children.Remove(this);
                        parentPanel.Children.Insert(childItemIndex, decorator);
                        decorator.Child = this;
                    }
                }
                else
                {
                    var parentContent = Parent as ContentControl;
                    if (parentContent != null)
                    {
                        parentContent.Content = null;
                        parentContent.Content = decorator;
                        decorator.Child = this;
                    }
                }
            }

            if (_scrollbarsCreated) return;
            _adorner = AdornerLayer.GetAdornerLayer(this);
            if (_adorner == null) return;
            _adorner.Add(new BladePanelScrollAdorner(this, _scrollHorizontal) { Visibility = Visibility.Visible });
            _scrollHorizontal.ValueChanged += (s, e) => Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, new Action(DispatchInvalidateScroll));
            _scrollbarsCreated = true;
        }

        private readonly ScrollBar _scrollHorizontal = new ScrollBar { Visibility = Visibility.Collapsed, Orientation = Orientation.Horizontal, SmallChange = 10 };
        private AdornerLayer _adorner;
        private bool _scrollbarsCreated;

        private void DispatchInvalidateScroll()
        {
            InvalidateArrange();
            InvalidateVisual();
        }

        /// <summary>
        /// Background color for each individual blade
        /// </summary>
        public static readonly DependencyProperty BladeBackgroundProperty = DependencyProperty.RegisterAttached("BladeBackground", typeof(Brush), typeof(BladePanel), new PropertyMetadata(null));

        /// <summary>
        /// Background color for each individual blade
        /// </summary>
        public static Brush GetBladeBackground(DependencyObject d)
        {
            return (Brush)d.GetValue(BladeBackgroundProperty);
        }
        /// <summary>
        /// Background color for each individual blade
        /// </summary>
        public static void SetBladeBackground(DependencyObject d, Brush value)
        {
            d.SetValue(BladeBackgroundProperty, value);
        }

        /// <summary>Style used by the scroll bars</summary>
        /// <value>The scroll bar style.</value>
        public Style ScrollBarStyle
        {
            get { return (Style)GetValue(ScrollBarStyleProperty); }
            set { SetValue(ScrollBarStyleProperty, value); }
        }
        /// <summary>Style used by the scroll bars</summary>
        public static readonly DependencyProperty ScrollBarStyleProperty = DependencyProperty.Register("ScrollBarStyle", typeof(Style), typeof(BladePanel), new PropertyMetadata(null, ScrollBarStyleChanged));
        /// <summary>
        /// This method gets called whenever the ScrollBarStyle property is updated
        /// </summary>
        /// <param name="d">Dependency object</param>
        /// <param name="args">Arguments</param>
        private static void ScrollBarStyleChanged(DependencyObject d, DependencyPropertyChangedEventArgs args)
        {
            var panel = d as BladePanel;
            if (panel == null) return;
            var style = args.NewValue as Style;
            panel._scrollHorizontal.Style = style;
        }

        /// <summary>
        /// Margin between panels
        /// </summary>
        public double Spacing
        {
            get { return (double)GetValue(SpacingProperty); }
            set { SetValue(SpacingProperty, value); }
        }

        /// <summary>
        /// Margin between panels
        /// </summary>
        public static readonly DependencyProperty SpacingProperty = DependencyProperty.Register("Spacing", typeof (double), typeof (BladePanel), new FrameworkPropertyMetadata(5d) {AffectsArrange = true, AffectsMeasure = true});

        /// <summary>
        /// Optional header renderer object
        /// </summary>
        /// <value>The header renderer.</value>
        public IBladePanelHeaderRenderer HeaderRenderer
        {
            get { return (IBladePanelHeaderRenderer)GetValue(HeaderRendererProperty); }
            set { SetValue(HeaderRendererProperty, value); }
        }

        /// <summary>
        /// Optional header renderer object
        /// </summary>
        public static readonly DependencyProperty HeaderRendererProperty = DependencyProperty.Register("HeaderRenderer", typeof(IBladePanelHeaderRenderer), typeof(BladePanel), new FrameworkPropertyMetadata(null) { AffectsArrange = true, AffectsMeasure = true });

        /// <summary>
        /// Defines the padding for each child item within a blade.
        /// </summary>
        /// <value>The child item padding.</value>
        public Thickness ChildItemPadding
        {
            get { return (Thickness)GetValue(ChildItemPaddingProperty); }
            set { SetValue(ChildItemPaddingProperty, value); }
        }

        /// <summary>
        /// Defines the padding for each child item within a blade.
        /// </summary>
        public static readonly DependencyProperty ChildItemPaddingProperty = DependencyProperty.Register("ChildItemPadding", typeof (Thickness), typeof (BladePanel), new FrameworkPropertyMetadata(new Thickness(0)) {AffectsArrange = true, AffectsMeasure = true});

        /// <summary>
        /// Indicates whether the last top (or left) item fills the remaining space of the whole panel
        /// </summary>
        /// <value><c>true</c> if last top/left item fills space; otherwise, <c>false</c>.</value>
        public bool LastTopItemFillsSpace
        {
            get { return (bool)GetValue(LastTopItemFillsSpaceProperty); }
            set { SetValue(LastTopItemFillsSpaceProperty, value); }
        }
        /// <summary>
        /// Indicates whether the last top (or left) item fills the remaining space of the whole panel
        /// </summary>
        public static readonly DependencyProperty LastTopItemFillsSpaceProperty = DependencyProperty.Register("LastTopItemFillsSpace", typeof(bool), typeof(BladePanel), new FrameworkPropertyMetadata(false) { AffectsArrange = true, AffectsMeasure = true });

        /// <summary>
        /// When overridden in a derived class, measures the size in layout required for child elements and determines a size for the <see cref="T:System.Windows.FrameworkElement" />-derived class.
        /// </summary>
        /// <param name="availableSize">The available size that this element can give to child elements. Infinity can be specified as a value to indicate that the element will size to whatever content is available.</param>
        /// <returns>The size that this element determines it needs during layout, based on its calculations of child element sizes.</returns>
        protected override Size MeasureOverride(Size availableSize)
        {
            var visibleChildren = Children.OfType<FrameworkElement>().Where(child => child.Visibility != Visibility.Collapsed).ToList();

            foreach (var child in visibleChildren)
            {
                child.SizeChanged -= ChildSizeUpdated;
                child.SizeChanged += ChildSizeUpdated;
                child.IsVisibleChanged -= ChildVisibilityUpdated;
                child.IsVisibleChanged += ChildVisibilityUpdated;
            }

            var topElements = new List<FrameworkElement>();
            var bottomElements = new List<FrameworkElement>();
            foreach (var child in visibleChildren)
            {
                if (child.HorizontalAlignment != HorizontalAlignment.Right) topElements.Add(child);
                else bottomElements.Add(child);
            }

            var availableHeight = availableSize.Height;
            if (HeaderRenderer != null)
            {
                var headerInfo = HeaderRenderer.GetClientArea(GeometryHelper.NewRect(0, 0, 10000, availableSize.Height));
                availableHeight = headerInfo.Height;
            }
            if (_scrollHorizontal.Visibility == Visibility.Visible)
                availableHeight -= _scrollHorizontal.ActualHeight;
            availableHeight -= (ChildItemPadding.Top + ChildItemPadding.Bottom);
            var childMaxSize = GeometryHelper.NewSize(10000000, availableHeight);

            var availableWidth = double.IsNaN(availableSize.Width) || double.IsInfinity(availableSize.Width) ? 1000d : availableSize.Width;
            var currentRight = availableWidth;
            foreach (var child in bottomElements)
            {
                child.Measure(childMaxSize);
                if (HeaderRenderer == null)
                    currentRight -= (child.DesiredSize.Width - Spacing - ChildItemPadding.Left - ChildItemPadding.Right);
                else
                {
                    var calculatedChildArea = HeaderRenderer.GetClientArea(GeometryHelper.NewRect(currentRight - child.DesiredSize.Width, 0d, child.DesiredSize.Width, 1d));
                    currentRight -= calculatedChildArea.Width - Spacing;
                }
            }

            var totalWidth = 0d;
            foreach (var child in topElements)
            {
                child.Measure(childMaxSize);
                if (totalWidth > 0d) totalWidth += (Spacing + ChildItemPadding.Left + ChildItemPadding.Right);
                if (HeaderRenderer == null)
                    totalWidth += child.DesiredSize.Width;
                else
                {
                    var calculatedChildArea = HeaderRenderer.GetClientArea(GeometryHelper.NewRect(totalWidth, 0d, child.DesiredSize.Width, 1d));
                    totalWidth += calculatedChildArea.Width + calculatedChildArea.Left;
                }
            }

            var baseSize = base.MeasureOverride(availableSize);
            var measuredHeight = !double.IsNaN(availableSize.Height) && !double.IsInfinity(availableSize.Height) ? availableSize.Height : baseSize.Height;
            return GeometryHelper.NewSize(availableSize.Width, measuredHeight);
        }

        private void ChildVisibilityUpdated(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (_inArrange) return;
            InvalidateMeasure();
            InvalidateArrange();
            InvalidateVisual();
        }

        private void ChildSizeUpdated(object sender, EventArgs e)
        {
            if (_inArrange) return;
            InvalidateMeasure();
            InvalidateArrange();
            InvalidateVisual();
        }

        private List<HeaderRenderInformation> _headerRenderAreas;
        private bool _inArrange;

        /// <summary>
        /// When overridden in a derived class, positions child elements and determines a size for a <see cref="T:System.Windows.FrameworkElement" /> derived class.
        /// </summary>
        /// <param name="finalSize">The final area within the parent that this element should use to arrange itself and its children.</param>
        /// <returns>The actual size used.</returns>
        protected override Size ArrangeOverride(Size finalSize)
        {
            _inArrange = true;

            var visibleChildren = Children.Cast<UIElement>().Where(child => child.Visibility != Visibility.Collapsed).ToList();

            var topElements = new List<FrameworkElement>();
            var bottomElements = new List<FrameworkElement>();
            foreach (var child in visibleChildren)
            {
                var element2 = child as FrameworkElement;
                if (element2 != null)
                    if (element2.HorizontalAlignment != HorizontalAlignment.Right) topElements.Add(element2);
                    else bottomElements.Add(element2);
            }

            var headerRenderer = HeaderRenderer;
            if (headerRenderer != null) _headerRenderAreas = new List<HeaderRenderInformation>();

            var currentRight = finalSize.Width;
            foreach (var child in bottomElements)
            {
                var childArea = GeometryHelper.NewRect(currentRight - child.DesiredSize.Width, 0d, child.DesiredSize.Width, finalSize.Height);
                if (headerRenderer != null)
                {
                    var clientAreaMargins = headerRenderer.GetClientAreaMargins();
                    _headerRenderAreas.Add(new HeaderRenderInformation { Child = child, TotalArea = GeometryHelper.NewRect(childArea.X - clientAreaMargins.Left, childArea.Y - clientAreaMargins.Top, childArea.Width + clientAreaMargins.Left + clientAreaMargins.Right, childArea.Height + clientAreaMargins.Top + clientAreaMargins.Bottom), IsTop = false });
                    if (clientAreaMargins.Right > 0)
                        childArea = GeometryHelper.NewRect(childArea.X - clientAreaMargins.Right, childArea.Y, childArea.Width, childArea.Height);
                    currentRight = childArea.Left - Spacing - clientAreaMargins.Left;
                }
                else
                    currentRight = childArea.Left - Spacing;
                child.Arrange(GeometryHelper.NewRect(childArea, ChildItemPadding));
            }
            BottomContentSize = finalSize.Width - currentRight;
            var topContentVisibleWidth = finalSize.Width - BottomContentSize;

            var currentLeft = 0d;
            var currentLeftWidth = 0d;
            var availableHeight = finalSize.Height;
            if (_scrollHorizontal.Visibility == Visibility.Visible)
            {
                currentLeft = _scrollHorizontal.Value * -1;
                availableHeight -= _scrollHorizontal.ActualHeight;
            }
            var leftChildCount = 0;
            foreach (var child in topElements)
            {
                leftChildCount++;
                Rect childArea;
                if (LastTopItemFillsSpace && leftChildCount == topElements.Count)
                {
                    var autoWidth = finalSize.Width - BottomContentSize - currentLeft - Spacing;
                    if (headerRenderer != null)
                    {
                        var margins = headerRenderer.GetClientAreaMargins();
                        autoWidth -= margins.Left + margins.Right;
                        autoWidth = Math.Max(autoWidth, child.DesiredSize.Width + margins.Left + margins.Right + Spacing);
                    }
                    else
                        autoWidth = Math.Max(autoWidth, child.DesiredSize.Width);
                    childArea = GeometryHelper.NewRect(currentLeft, 0d, (int)autoWidth, availableHeight);
                }
                else
                    childArea = GeometryHelper.NewRect(currentLeft, 0d, child.DesiredSize.Width + ChildItemPadding.Left + ChildItemPadding.Right, availableHeight);
                if (headerRenderer != null)
                {
                    _headerRenderAreas.Add(new HeaderRenderInformation {Child = child, TotalArea = childArea, IsTop = true});
                    childArea = headerRenderer.GetClientArea(childArea);
                }
                var childRect = GeometryHelper.NewRect(childArea, ChildItemPadding);
                child.Arrange(childRect);
                if (bottomElements.Count > 0 && childRect.Right > topContentVisibleWidth)
                {
                    var overlap = childRect.Right - topContentVisibleWidth;
                    var clipRect = GeometryHelper.NewRect(0, 0, childRect.Width - overlap, finalSize.Height);
                    child.Clip = new RectangleGeometry(clipRect);
                }
                else
                    child.Clip = null;
                if (headerRenderer != null)
                {
                    var clientAreaMargins = headerRenderer.GetClientAreaMargins();
                    currentLeft = childArea.Right + clientAreaMargins.Right + Spacing;
                    currentLeftWidth += childArea.Width + Spacing + clientAreaMargins.Left + clientAreaMargins.Right;
                }
                else
                {
                    currentLeft = childArea.Right + Spacing;
                    currentLeftWidth += childArea.Width + Spacing;
                }
            }

            if (currentLeftWidth > topContentVisibleWidth + .1)
            {
                if (_scrollHorizontal.Visibility != Visibility.Visible)
                {
                    _scrollHorizontal.Visibility = Visibility.Visible;
                    InvalidateVisual();
                }
                _scrollHorizontal.Maximum = currentLeftWidth - topContentVisibleWidth + Spacing;
                _scrollHorizontal.LargeChange = topContentVisibleWidth;
                _scrollHorizontal.ViewportSize = topContentVisibleWidth;
            }
            else
            {
                if (_scrollHorizontal.Visibility == Visibility.Visible)
                {
                    _scrollHorizontal.Visibility = Visibility.Collapsed;
                    _scrollHorizontal.Value = 0;
                    InvalidateVisual();
                }
            }

            _inArrange = false;
            return finalSize;
        }

        /// <summary>
        /// Gets or sets the size of the bottom content.
        /// </summary>
        /// <value>The size of the bottom content.</value>
        public double BottomContentSize { get; set; }

        /// <summary>
        /// Draws the content of a <see cref="T:System.Windows.Media.DrawingContext" /> object during the render pass of a <see cref="T:System.Windows.Controls.Panel" /> element.
        /// </summary>
        /// <param name="dc">The <see cref="T:System.Windows.Media.DrawingContext" /> object to draw.</param>
        protected override void OnRender(DrawingContext dc)
        {
            base.OnRender(dc);

            dc.DrawRectangle(Brushes.Transparent, null, GeometryHelper.NewRect(0, 0, ActualWidth, ActualHeight)); // Do NOT remove this line, otherwise, hit-testing will not work anymore

            var headerRenderer = HeaderRenderer;
            if (headerRenderer == null) return;

            foreach (var area in _headerRenderAreas)
            {
                if (area.IsTop)
                {
                    var topClip = new RectangleGeometry(GeometryHelper.NewRect(0, 0, ActualWidth - BottomContentSize, ActualHeight));
                    dc.PushClip(topClip);
                }

                var bladeBackground = GetBladeBackground(area.Child);
                if (bladeBackground == null) bladeBackground = GetBladeBackground(this);
                if (bladeBackground != null)
                {
                    dc.DrawRectangle(bladeBackground, null, area.TotalArea);
                }

                dc.PushTransform(new TranslateTransform(area.TotalArea.X, area.TotalArea.Y));
                headerRenderer.Render(dc, area.TotalArea.Size, area.Child);
                dc.Pop();
                if (area.IsTop) dc.Pop();
            }
        }

        /// <summary>
        /// Header renderer information
        /// </summary>
        public class HeaderRenderInformation
        {
            /// <summary>
            /// Complete area used up by the child element + header
            /// </summary>
            /// <value>The total area.</value>
            public Rect TotalArea { get; set; }
            /// <summary>
            /// Gets or sets the child.
            /// </summary>
            /// <value>The child.</value>
            public UIElement Child { get; set; }

            /// <summary>
            /// Indicates wheter this is a top/left element.
            /// </summary>
            /// <value><c>true</c> if this instance is top; otherwise, <c>false</c>.</value>
            public bool IsTop { get; set; }
        }
    }

    /// <summary>
    /// This interface can be implemented to create a renderer object that measures and renders
    /// headers for child elements contained in a BladePanel
    /// </summary>
    public interface IBladePanelHeaderRenderer
    {
        /// <summary>
        /// Returns the client area (the area available for the child control) based on the total area available for the child
        /// </summary>
        /// <param name="totalArea">The total area.</param>
        /// <param name="isOtherOrientation">if set to <c>true</c> [is other orientation].</param>
        /// <returns>The client area</returns>
        /// <remarks>When a header is added for a child element, it takes away from the area available for the child.
        /// For instance, when a header text is to be rendered across the top of a child, then the child must be moved down
        /// and the overall space for the child shrinks accordingly. The value returned by this method indicates
        /// the remaining space for the child element.</remarks>
        Rect GetClientArea(Rect totalArea, bool isOtherOrientation = false);

        /// <summary>
        /// Returns the margin around the client area (the area available for the child control) based on the total area available for the child
        /// </summary>
        /// <param name="isOtherOrientation">if set to <c>true</c> [is other orientation].</param>
        /// <returns>Thickness (margins)</returns>
        Thickness GetClientAreaMargins(bool isOtherOrientation = false);

        /// <summary>Renders the header element for a single child.</summary>
        /// <param name="dc">The drawing context.</param>
        /// <param name="clientAreaSize">Size of the client area of the entire contained control (including area). Note: Top/left render position is always 0,0.</param>
        /// <param name="child">The actual child element.</param>
        void Render(DrawingContext dc, Size clientAreaSize, UIElement child);

        /// <summary>
        /// Invoked when an unhandled System.Windows.Input.Mouse.MouseEnter attached event is raised on this element. Implement this method to add class handling for this event.
        /// </summary>
        /// <param name="bladePanel">The blade panel.</param>
        /// <param name="e">The <see cref="T:System.Windows.Input.MouseEventArgs" /> that contains the event data.</param>
        /// <param name="headerRenderAreas">Area information for specific headers</param>
        void OnMouseEnter(BladePanel bladePanel, MouseEventArgs e, List<BladePanel.HeaderRenderInformation> headerRenderAreas);

        /// <summary>
        /// Invoked when an unhandled System.Windows.Input.Mouse.MouseLeave attached event is raised on this element. Implement this method to add class handling for this event.
        /// </summary>
        /// <param name="bladePanel">The blade panel.</param>
        /// <param name="e">The <see cref="T:System.Windows.Input.MouseEventArgs" /> that contains the event data.</param>
        /// <param name="headerRenderAreas">Area information for specific headers</param>
        void OnMouseLeave(BladePanel bladePanel, MouseEventArgs e, List<BladePanel.HeaderRenderInformation> headerRenderAreas);

        /// <summary>
        /// Invoked when an unhandled System.Windows.Input.Mouse.MouseDown attached event reaches an element in its route that is derived from this class. Implement this method to add class handling for this event.
        /// </summary>
        /// <param name="bladePanel">The blade panel.</param>
        /// <param name="e">The <see cref="T:System.Windows.Input.MouseButtonEventArgs" /> that contains the event data. This event data reports details about the mouse button that was pressed and the handled state.</param>
        /// <param name="headerRenderAreas">Area information for specific headers</param>
        void OnMouseDown(BladePanel bladePanel, MouseButtonEventArgs e, List<BladePanel.HeaderRenderInformation> headerRenderAreas);

        /// <summary>
        /// Invoked when an unhandled System.Windows.Input.Mouse.MouseUp routed event reaches an element in its route that is derived from this class. Implement this method to add class handling for this event.
        /// </summary>
        /// <param name="bladePanel">The blade panel.</param>
        /// <param name="e">The <see cref="T:System.Windows.Input.MouseButtonEventArgs" /> that contains the event data. The event data reports that the mouse button was released.</param>
        /// <param name="headerRenderAreas">Area information for specific headers</param>
        void OnMouseUp(BladePanel bladePanel, MouseButtonEventArgs e, List<BladePanel.HeaderRenderInformation> headerRenderAreas);

        /// <summary>
        /// Invoked when an unhandled System.Windows.Input.Mouse.MouseMove attached event reaches an element in its route that is derived from this class. Implement this method to add class handling for this event.
        /// </summary>
        /// <param name="bladePanel">The blade panel.</param>
        /// <param name="e">The <see cref="T:System.Windows.Input.MouseEventArgs" /> that contains the event data.</param>
        /// <param name="headerRenderAreas">Area information for specific headers</param>
        void OnMouseMove(BladePanel bladePanel, MouseEventArgs e, List<BladePanel.HeaderRenderInformation> headerRenderAreas);
    }

    /// <summary>
    /// Standard header renderer object for multi panel headers
    /// </summary>
    public class BladePanelHeaderRenderer : FrameworkElement, IBladePanelHeaderRenderer
    {
        /// <summary>
        /// Header Text Orientation
        /// </summary>
        /// <value>The orientation of the header text.</value>
        public Orientation Orientation
        {
            get { return (Orientation)GetValue(OrientationProperty); }
            set { SetValue(OrientationProperty, value); }
        }

        /// <summary>
        /// Header Text Orientation
        /// </summary>
        /// <value>The orientation of the header text.</value>
        public static readonly DependencyProperty OrientationProperty = DependencyProperty.Register("Orientation", typeof(Orientation), typeof(BladePanelHeaderRenderer), new PropertyMetadata(Orientation.Horizontal));

        /// <summary>
        /// Header text font size
        /// </summary>
        /// <value>The header text font size.</value>
        public double FontSize
        {
            get { return (double)GetValue(FontSizeProperty); }
            set { SetValue(FontSizeProperty, value); }
        }

        /// <summary>
        /// Header text font size
        /// </summary>
        public static readonly DependencyProperty FontSizeProperty = DependencyProperty.Register("FontSize", typeof(double), typeof(BladePanelHeaderRenderer), new PropertyMetadata(12d));

        /// <summary>
        /// Header text font family
        /// </summary>
        /// <value>The header text font family.</value>
        public FontFamily FontFamily
        {
            get { return (FontFamily)GetValue(FontFamilyProperty); }
            set { SetValue(FontFamilyProperty, value); }
        }

        /// <summary>
        /// Header text font family
        /// </summary>
        public static readonly DependencyProperty FontFamilyProperty = DependencyProperty.Register("FontFamily", typeof(FontFamily), typeof(BladePanelHeaderRenderer), new PropertyMetadata(new FontFamily("Segoe UI")));

        /// <summary>
        /// Header text font style
        /// </summary>
        /// <value>The header text font style.</value>
        public FontStyle FontStyle
        {
            get { return (FontStyle)GetValue(FontStyleProperty); }
            set { SetValue(FontStyleProperty, value); }
        }

        /// <summary>
        /// Header text font style
        /// </summary>
        public static readonly DependencyProperty FontStyleProperty = DependencyProperty.Register("FontStyle", typeof(FontStyle), typeof(BladePanelHeaderRenderer), new PropertyMetadata(FontStyles.Normal));

        /// <summary>
        /// Header text font weight
        /// </summary>
        /// <value>The header text font weight.</value>
        public FontWeight FontWeight
        {
            get { return (FontWeight)GetValue(FontWeightProperty); }
            set { SetValue(FontWeightProperty, value); }
        }

        /// <summary>
        /// Header text font weight
        /// </summary>
        public static readonly DependencyProperty FontWeightProperty = DependencyProperty.Register("FontWeight", typeof(FontWeight), typeof(BladePanelHeaderRenderer), new PropertyMetadata(FontWeights.Normal));

        /// <summary>
        /// Foreground brush for the header text
        /// </summary>
        /// <value>Header text foreground brush.</value>
        public Brush Foreground
        {
            get { return (Brush)GetValue(ForegroundProperty); }
            set { SetValue(ForegroundProperty, value); }
        }

        /// <summary>
        /// Foreground brush for the header text
        /// </summary>
        public static readonly DependencyProperty ForegroundProperty = DependencyProperty.Register("Foreground", typeof(Brush), typeof(BladePanelHeaderRenderer), new PropertyMetadata(Brushes.Black));

        /// <summary>
        /// The background brush for the entire header area
        /// </summary>
        /// <value>The background brush.</value>
        public Brush Background
        {
            get { return (Brush)GetValue(BackgroundProperty); }
            set { SetValue(BackgroundProperty, value); }
        }

        /// <summary>
        /// The background brush for the entire header area
        /// </summary>
        public static readonly DependencyProperty BackgroundProperty = DependencyProperty.Register("Background", typeof(Brush), typeof(BladePanelHeaderRenderer), new PropertyMetadata(null));

        /// <summary>
        /// If true, the renderer attempts to pick up a View.TitleColor setting fore the foreground color of the header text
        /// </summary>
        /// <value><c>true</c> if [use title color for foreground]; otherwise, <c>false</c>.</value>
        public bool UseTitleColorForForeground
        {
            get { return (bool)GetValue(UseTitleColorForForegroundProperty); }
            set { SetValue(UseTitleColorForForegroundProperty, value); }
        }
        /// <summary>
        /// If true, the renderer attempts to pick up a View.TitleColor setting fore the foreground color of the header text
        /// </summary>
        public static readonly DependencyProperty UseTitleColorForForegroundProperty = DependencyProperty.Register("UseTitleColorForForeground", typeof(bool), typeof(BladePanelHeaderRenderer), new PropertyMetadata(true));

        /// <summary>
        /// If true, the renderer attempts to pick up a View.TitleColor2 setting fore the foreground color of the header text
        /// </summary>
        /// <value><c>true</c> if [use title color for foreground]; otherwise, <c>false</c>.</value>
        public bool UseTitleColor2ForForeground
        {
            get { return (bool)GetValue(UseTitleColor2ForForegroundProperty); }
            set { SetValue(UseTitleColor2ForForegroundProperty, value); }
        }
        /// <summary>
        /// If true, the renderer attempts to pick up a View.TitleColor setting fore the foreground color of the header text
        /// </summary>
        public static readonly DependencyProperty UseTitleColor2ForForegroundProperty = DependencyProperty.Register("UseTitleColor2ForForeground", typeof(bool), typeof(BladePanelHeaderRenderer), new PropertyMetadata(false));

        /// <summary>
        /// If true, the renderer attempts to pick up a View.TitleColor setting fore the background color of the header
        /// </summary>
        /// <value><c>true</c> if [use title color for background]; otherwise, <c>false</c>.</value>
        public bool UseTitleColorForBackground
        {
            get { return (bool)GetValue(UseTitleColorForBackgroundProperty); }
            set { SetValue(UseTitleColorForBackgroundProperty, value); }
        }
        /// <summary>
        /// If true, the renderer attempts to pick up a View.TitleColor setting fore the background color of the header
        /// </summary>
        public static readonly DependencyProperty UseTitleColorForBackgroundProperty = DependencyProperty.Register("UseTitleColorForBackground", typeof(bool), typeof(BladePanelHeaderRenderer), new PropertyMetadata(false));

        /// <summary>
        /// If true, the renderer attempts to pick up a View.TitleColor2 setting fore the background color of the header
        /// </summary>
        /// <value><c>true</c> if [use title color for background]; otherwise, <c>false</c>.</value>
        public bool UseTitleColor2ForBackground
        {
            get { return (bool)GetValue(UseTitleColor2ForBackgroundProperty); }
            set { SetValue(UseTitleColor2ForBackgroundProperty, value); }
        }
        /// <summary>
        /// If true, the renderer attempts to pick up a View.TitleColor setting fore the background color of the header
        /// </summary>
        public static readonly DependencyProperty UseTitleColor2ForBackgroundProperty = DependencyProperty.Register("UseTitleColor2ForBackground", typeof(bool), typeof(BladePanelHeaderRenderer), new PropertyMetadata(true));

        /// <summary>
        /// Brush used to render the close icon
        /// </summary>
        /// <value>The close icon.</value>
        public Brush CloseIcon
        {
            get { return (Brush)GetValue(CloseIconProperty); }
            set { SetValue(CloseIconProperty, value); }
        }

        /// <summary>
        /// Brush used to render the close icon
        /// </summary>
        public static readonly DependencyProperty CloseIconProperty = DependencyProperty.Register("CloseIcon", typeof(Brush), typeof(BladePanelHeaderRenderer), new PropertyMetadata(Brushes.Red));

        /// <summary>
        /// Max height/width for the close icon
        /// </summary>
        /// <value>The maximum size of the close icon.</value>
        public double MaxCloseIconSize
        {
            get { return (double)GetValue(MaxCloseIconSizeProperty); }
            set { SetValue(MaxCloseIconSizeProperty, value); }
        }
        /// <summary>
        /// Max height/width for the close icon
        /// </summary>
        public static readonly DependencyProperty MaxCloseIconSizeProperty = DependencyProperty.Register("MaxCloseIconSize", typeof(double), typeof(BladePanelHeaderRenderer), new PropertyMetadata(20d));

        /// <summary>
        /// Returns the client area (the area available for the child control) based on the total area available for the child
        /// </summary>
        /// <param name="totalArea">The total area currently set.</param>
        /// <param name="isOtherOrientation">True if the object is to be filled in from the "other" side (such as horizontal alignment = left)</param>
        /// <returns>The client area</returns>
        /// <remarks>When a header is added for a child element, it takes away from the area available for the child.
        /// For instance, when a header text is to be rendered across the top of a child, then the child must be moved down
        /// and the overall space for the child shrinks accordingly. The value returned by this method indicates
        /// the remaining space for the child element.</remarks>
        public Rect GetClientArea(Rect totalArea, bool isOtherOrientation = false)
        {
            var ft = GetFormattedText();
            if (Orientation == Orientation.Horizontal)
                return GeometryHelper.NewRect(totalArea.X, totalArea.Y + ft.Height, totalArea.Width, totalArea.Height - ft.Height);

            if (!isOtherOrientation)
                return GeometryHelper.NewRect(totalArea.X + ft.Height + 5, totalArea.Y, totalArea.Width, totalArea.Height);
            return GeometryHelper.NewRect(totalArea.X - ft.Height - 5, totalArea.Y, totalArea.Width + ft.Height + 5, totalArea.Height);
        }

        /// <summary>
        /// Returns the margin around the client area (the area available for the child control) based on the total area available for the child
        /// </summary>
        /// <param name="isOtherOrientation">if set to <c>true</c> [is other orientation].</param>
        /// <returns>Thickness (margins)</returns>
        public Thickness GetClientAreaMargins(bool isOtherOrientation = false)
        {
            var ft = GetFormattedText();
            if (Orientation == Orientation.Horizontal)
                return new Thickness(0, ft.Height, 0, 0);
            if (!isOtherOrientation)
                return new Thickness(ft.Height, 0, 0, 0);
            return new Thickness(0, 0, ft.Height, 0);
        }

        private FormattedText _standardHeight;

        private FormattedText GetFormattedText(string text = "", Brush foreground = null)
        {
            if (foreground == null) foreground = Brushes.Black;
            if (string.IsNullOrEmpty(text))
            {
                if (_standardHeight == null)
                    _standardHeight = new FormattedText("X", CultureInfo.CurrentUICulture, FlowDirection.LeftToRight, new Typeface(FontFamily, FontStyle, FontWeight, FontStretches.Normal), FontSize, foreground) { MaxLineCount = 1 };
                return _standardHeight;
            }
            return new FormattedText(text, CultureInfo.CurrentUICulture, FlowDirection.LeftToRight, new Typeface(FontFamily, FontStyle, FontWeight, FontStretches.Normal), FontSize, foreground) { MaxLineCount = 1 };
        }

        /// <summary>
        /// Renders the header element for a single child
        /// </summary>
        /// <param name="dc">The drawing context.</param>
        /// <param name="clientAreaSize">Size of the client area of the entire contained control (including area). Note: Top/left render position is always 0,0.</param>
        /// <param name="child">The actual child element.</param>
        public void Render(DrawingContext dc, Size clientAreaSize, UIElement child)
        {
            var title = SimpleView.GetTitle(child);
            var closable = SimpleView.GetClosable(child);

            var foreground = Foreground;
            if (UseTitleColorForForeground)
            {
                var titleColor = SimpleView.GetTitleColor(child);
                if (titleColor != null)
                    foreground = titleColor;
            }
            else if (UseTitleColor2ForForeground)
            {
                var titleColor = SimpleView.GetTitleColor2(child);
                if (titleColor != null)
                    foreground = titleColor;
            }
            var ft = GetFormattedText(title, foreground);
            var textHeight = ft.Height;
            var headerRect = Orientation == Orientation.Horizontal ? new Rect(0d, 0d, clientAreaSize.Width, textHeight) : new Rect(0d, 0d, textHeight + 5, clientAreaSize.Height);
            dc.PushClip(new RectangleGeometry(headerRect));

            var background = Background;
            if (UseTitleColorForBackground)
            {
                var titleColor = SimpleView.GetTitleColor(child);
                if (titleColor != null)
                    background = titleColor;
            }
            if (UseTitleColor2ForBackground)
            {
                var titleColor = SimpleView.GetTitleColor2(child);
                if (titleColor != null)
                    background = titleColor;
            }
            if (background != null)
                dc.DrawRectangle(background, null, headerRect);

            if (!closable && (string.IsNullOrEmpty(title) || Foreground == null))
            {
                dc.Pop(); // Remove the clip
                return;
            }

            ft.Trimming = TextTrimming.WordEllipsis;
            if (Orientation == Orientation.Horizontal)
            {
                ft.MaxTextWidth = headerRect.Width;
                var iconSize = Math.Min(headerRect.Height - 7, MaxCloseIconSize);
                if (closable) ft.MaxTextWidth -= (iconSize - 3); // Making room for the close button
                dc.DrawText(ft, new Point(2d, 0d));
                if (closable) dc.DrawRectangle(CloseIcon, null, GeometryHelper.NewRect(headerRect.Width - iconSize - 4, (int)((headerRect.Height - iconSize) / 2), iconSize, iconSize));
            }
            else
            {
                // We need to set the appropriate transformation to render vertical text at the right position
                dc.PushTransform(new RotateTransform(-90d));
                dc.PushTransform(new TranslateTransform((headerRect.Height + 5) * -1, 0d));
                var iconSize = Math.Min(headerRect.Height - 7, MaxCloseIconSize);
                ft.MaxTextWidth = Math.Max(headerRect.Height - 5, 0);
                if (closable) ft.MaxTextWidth = Math.Max(ft.MaxTextWidth - iconSize - 5, 0); // Making room for the close button
                ft.TextAlignment = TextAlignment.Right;
                dc.DrawText(ft, new Point(0d, 0d));
                dc.Pop();
                dc.Pop();
                if (closable) dc.DrawRectangle(CloseIcon, null, GeometryHelper.NewRect((int)((headerRect.Width + 1 - iconSize) / 2), 5, iconSize, iconSize));
            }

            dc.Pop(); // Remove the clip
        }

        /// <summary>
        /// Invoked when an unhandled System.Windows.Input.Mouse.MouseEnter attached event is raised on this element. Implement this method to add class handling for this event.
        /// </summary>
        /// <param name="bladePanel">The blade panel.</param>
        /// <param name="e">The <see cref="T:System.Windows.Input.MouseEventArgs" /> that contains the event data.</param>
        /// <param name="headerRenderAreas"></param>
        public void OnMouseEnter(BladePanel bladePanel, MouseEventArgs e, List<BladePanel.HeaderRenderInformation> headerRenderAreas)
        {
        }

        /// <summary>
        /// Invoked when an unhandled System.Windows.Input.Mouse.MouseLeave attached event is raised on this element. Implement this method to add class handling for this event.
        /// </summary>
        /// <param name="bladePanel">The blade panel.</param>
        /// <param name="e">The <see cref="T:System.Windows.Input.MouseEventArgs" /> that contains the event data.</param>
        /// <param name="headerRenderAreas"></param>
        public void OnMouseLeave(BladePanel bladePanel, MouseEventArgs e, List<BladePanel.HeaderRenderInformation> headerRenderAreas)
        {
        }

        /// <summary>
        /// Invoked when an unhandled System.Windows.Input.Mouse.MouseDown attached event reaches an element in its route that is derived from this class. Implement this method to add class handling for this event.
        /// </summary>
        /// <param name="bladePanel">The blade panel.</param>
        /// <param name="e">The <see cref="T:System.Windows.Input.MouseButtonEventArgs" /> that contains the event data. This event data reports details about the mouse button that was pressed and the handled state.</param>
        /// <param name="headerRenderAreas"></param>
        public void OnMouseDown(BladePanel bladePanel, MouseButtonEventArgs e, List<BladePanel.HeaderRenderInformation> headerRenderAreas)
        {
        }

        /// <summary>
        /// Invoked when an unhandled System.Windows.Input.Mouse.MouseUp routed event reaches an element in its route that is derived from this class. Implement this method to add class handling for this event.
        /// </summary>
        /// <param name="bladePanel">The blade panel.</param>
        /// <param name="e">The <see cref="T:System.Windows.Input.MouseButtonEventArgs" /> that contains the event data. The event data reports that the mouse button was released.</param>
        /// <param name="headerRenderAreas"></param>
        public void OnMouseUp(BladePanel bladePanel, MouseButtonEventArgs e, List<BladePanel.HeaderRenderInformation> headerRenderAreas)
        {
        }

        /// <summary>
        /// Invoked when an unhandled System.Windows.Input.Mouse.MouseMove attached event reaches an element in its route that is derived from this class. Implement this method to add class handling for this event.
        /// </summary>
        /// <param name="bladePanel">The blade panel.</param>
        /// <param name="e">The <see cref="T:System.Windows.Input.MouseEventArgs" /> that contains the event data.</param>
        /// <param name="headerRenderAreas"></param>
        public void OnMouseMove(BladePanel bladePanel, MouseEventArgs e, List<BladePanel.HeaderRenderInformation> headerRenderAreas)
        {
        }
    }

    /// <summary>Adorner UI for scrollbars of the blade panel control</summary>
    public class BladePanelScrollAdorner : Adorner
    {
        private readonly ScrollBar _horizontal;
        private readonly BladePanel _bladePanel;

        /// <summary>Constructor</summary>
        /// <param name="adornedElement">Adorned element</param>
        /// <param name="horizontal">The horizontal scrollbar.</param>
        public BladePanelScrollAdorner(BladePanel adornedElement, ScrollBar horizontal)
            : base(adornedElement)
        {
            _horizontal = horizontal;
            _bladePanel = adornedElement;
            CheckControlForExistingParent(_horizontal);
            AddVisualChild(_horizontal);
        }

        /// <summary>Implements any custom measuring behavior for the adorner.</summary>
        /// <param name="constraint">A size to constrain the adorner to.</param>
        /// <returns>A <see cref="T:System.Windows.Size"/> object representing the amount of layout space needed by the adorner.</returns>
        protected override Size MeasureOverride(Size constraint)
        {
            _horizontal.Measure(constraint);

            if (!double.IsInfinity(constraint.Height) && !double.IsInfinity(constraint.Width))
                return constraint;
            return new Size(100000, 100000);
        }

        /// <summary>When overridden in a derived class, positions child elements and determines a size for a <see cref="T:System.Windows.FrameworkElement"/> derived class.</summary>
        /// <param name="finalSize">The final area within the parent that this element should use to arrange itself and its children.</param>
        /// <returns>The actual size used.</returns>
        protected override Size ArrangeOverride(Size finalSize)
        {
            var surfaceSize = AdornedElement.RenderSize;

            if (_horizontal.Visibility == Visibility.Visible)
            {
                var scrollTop = surfaceSize.Height - SystemParameters.HorizontalScrollBarHeight;
                if (scrollTop < 0d) scrollTop = 0d;
                var scrollHeight = SystemParameters.HorizontalScrollBarHeight;
                if (scrollHeight > finalSize.Height) scrollHeight = finalSize.Height;
                _horizontal.Arrange(new Rect(0, scrollTop, surfaceSize.Width - _bladePanel.BottomContentSize, scrollHeight));
            }

            return finalSize;
        }

        private void CheckControlForExistingParent(FrameworkElement element)
        {
            DependencyObject parent = null;
            if (element.Parent != null) parent = element.Parent;
            if (parent == null)
                parent = VisualTreeHelper.GetParent(element);
            if (parent != null)
            {
                var parentContent = parent as ContentControl;
                if (parentContent != null)
                {
                    parentContent.Content = null;
                    return;
                }
                var panelContent = parent as Panel;
                if (panelContent != null)
                {
                    panelContent.Children.Remove(element);
                    return;
                }
                var adorner = parent as BladePanelScrollAdorner;
                if (adorner != null)
                    adorner.RemoveVisualChild(element);
            }
        }

        /// <summary>Gets the number of visual child elements within this element.</summary>
        /// <returns>The number of visual child elements for this element.</returns>
        protected override int VisualChildrenCount
        {
            get { return 1; }
        }

        /// <summary>Overrides <see cref="M:System.Windows.Media.Visual.GetVisualChild(System.Int32)"/>, and returns a child at the specified index from a collection of child elements.</summary>
        /// <param name="index">The zero-based index of the requested child element in the collection.</param>
        /// <returns>The requested child element. This should not return null; if the provided index is out of range, an exception is thrown.</returns>
        protected override Visual GetVisualChild(int index)
        {
            return _horizontal;
        }
    }
}
