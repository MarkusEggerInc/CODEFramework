using System;
using System.Collections.Generic;
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
    /// This layout panel arranges objects in multiple "panels", typically multiple rows of panels
    /// </summary>
    public class MultiPanel : Panel
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MultiPanel"/> class.
        /// </summary>
        public MultiPanel()
        {
            SizeChanged += (s, e) => InvalidateVisual();
            IsHitTestVisible = true;
        }

        /// <summary>
        /// Margin between panels
        /// </summary>
        public double Spacing
        {
            get { return (double) GetValue(SpacingProperty); }
            set { SetValue(SpacingProperty, value); }
        }

        /// <summary>
        /// Margin between panels
        /// </summary>
        public static readonly DependencyProperty SpacingProperty = DependencyProperty.Register("Spacing", typeof (double), typeof (MultiPanel), new FrameworkPropertyMetadata(5d) {AffectsArrange = true, AffectsMeasure = true});

        /// <summary>
        /// Optional header renderer object
        /// </summary>
        /// <value>The header renderer.</value>
        public IMultiPanelHeaderRenderer HeaderRenderer
        {
            get { return (IMultiPanelHeaderRenderer) GetValue(HeaderRendererProperty); }
            set { SetValue(HeaderRendererProperty, value); }
        }

        /// <summary>
        /// Optional header renderer object
        /// </summary>
        public static readonly DependencyProperty HeaderRendererProperty = DependencyProperty.Register("HeaderRenderer", typeof (IMultiPanelHeaderRenderer), typeof (MultiPanel), new FrameworkPropertyMetadata(null) {AffectsArrange = true, AffectsMeasure = true});

        /// <summary>
        /// Gets or sets the general orientation of the content alignment.
        /// </summary>
        /// <value>The orientation.</value>
        public Orientation Orientation
        {
            get { return (Orientation) GetValue(OrientationProperty); }
            set { SetValue(OrientationProperty, value); }
        }

        /// <summary>
        /// Gets or sets the general orientation of the content alignment.
        /// </summary>
        public static readonly DependencyProperty OrientationProperty = DependencyProperty.Register("Orientation", typeof (Orientation), typeof (MultiPanel), new FrameworkPropertyMetadata(Orientation.Vertical) {AffectsArrange = true, AffectsMeasure = true});

        /// <summary>
        /// When overridden in a derived class, measures the size in layout required for child elements and determines a size for the <see cref="T:System.Windows.FrameworkElement" />-derived class.
        /// </summary>
        /// <param name="availableSize">The available size that this element can give to child elements. Infinity can be specified as a value to indicate that the element will size to whatever content is available.</param>
        /// <returns>The size that this element determines it needs during layout, based on its calculations of child element sizes.</returns>
        protected override Size MeasureOverride(Size availableSize)
        {
            var visibleChildren = Children.Cast<UIElement>().Where(child => child.Visibility != Visibility.Collapsed).ToList();

            foreach (var child in Children.Cast<FrameworkElement>())
            {
                child.SizeChanged -= ChildSizeUpdated;
                child.SizeChanged += ChildSizeUpdated;
                child.IsVisibleChanged -= ChildVisibilityUpdated;
                child.IsVisibleChanged += ChildVisibilityUpdated;
            }

            var childCount = visibleChildren.Count;
            var realSize = availableSize;
            if (double.IsPositiveInfinity(realSize.Width)) realSize.Width = 1000000d;
            if (double.IsPositiveInfinity(realSize.Height)) realSize.Height = 100*childCount;

            if (Orientation == Orientation.Vertical)
            {
                var usableHeight = realSize.Height - (Spacing*2);
                if (childCount > 1) usableHeight -= Spacing*(childCount - 1);
                var rowLeaders = visibleChildren.Where(child => !SimpleView.GetFlowsWithPrevious(child)).ToList();

                // Giving it a first measure pass to see what the maximum space is the elements would want
                foreach (var element in visibleChildren) element.Measure(availableSize);

                var rowHeights = GetAllRowHeights(rowLeaders, usableHeight);

                var followerElementFound = false;

                var headerRenderer = HeaderRenderer;

                var elementCounter = -1;
                foreach (var leadElement in rowLeaders)
                {
                    elementCounter++;
                    if (!followerElementFound)
                    {
                        // First, we check what other controls are in the same row
                        UIElement followerElement = null;
                        for (var childCount2 = 0; childCount2 < visibleChildren.Count; childCount2++)
                        {
                            if (visibleChildren[childCount2] == leadElement)
                                // We found the leader, so we check whether the next item is a follower
                                if (childCount2 < visibleChildren.Count - 1)
                                    foreach (var child in visibleChildren)
                                        if (SimpleView.GetFlowsWithPrevious(child))
                                        {
                                            // We found a follower
                                            followerElement = child;
                                            followerElementFound = true;
                                            break;
                                        }
                            if (followerElement != null) break;
                        }
                        if (followerElement != null)
                        {
                            followerElement.Measure(GeometryHelper.NewSize(100000, rowHeights[elementCounter]));
                            var followerWidth = followerElement.DesiredSize.Width;
                            if (followerWidth + Spacing*2 > availableSize.Width) followerWidth = availableSize.Width - Spacing*2;
                            if (followerWidth > 0d) followerElement.Measure(GeometryHelper.NewSize(followerWidth, rowHeights[elementCounter]));
                            var elementSize = GeometryHelper.NewSize(availableSize.Width - Spacing*3 - followerWidth, rowHeights[elementCounter]);
                            if (headerRenderer != null)
                            {
                                var clientArea = headerRenderer.GetClientArea(GeometryHelper.NewRect(new Point(0d, 0d), elementSize));
                                elementSize = clientArea.Size;
                            }
                            leadElement.Measure(elementSize);
                        }
                        else
                        {
                            var elementSize = GeometryHelper.NewSize(availableSize.Width - Spacing*2, rowHeights[elementCounter]);
                            if (headerRenderer != null)
                            {
                                var clientArea = headerRenderer.GetClientArea(GeometryHelper.NewRect(new Point(0d, 0d), elementSize));
                                elementSize = clientArea.Size;
                            }
                            leadElement.Measure(elementSize);
                        }
                    }
                    else
                    {
                        var elementSize = GeometryHelper.NewSize(availableSize.Width - Spacing*2, rowHeights[elementCounter]);
                        if (headerRenderer != null)
                        {
                            var clientArea = headerRenderer.GetClientArea(GeometryHelper.NewRect(new Point(0d, 0d), elementSize));
                            elementSize = clientArea.Size;
                        }
                        leadElement.Measure(elementSize);
                    }
                }
            }
            else
            {
                // General orientation is horizontal
                var usableWidth = realSize.Width - (Spacing*2);
                if (childCount > 1) usableWidth -= Spacing*(childCount - 1);
                var columnLeaders = visibleChildren.Where(child => !SimpleView.GetFlowsWithPrevious(child)).ToList();

                // Giving it a first measure pass to see what the maximum space is the elements would want
                foreach (var element in visibleChildren) element.Measure(availableSize);

                var columnWidths = GetAllColumnWidths(columnLeaders, usableWidth);

                var followerElementFound = false;

                var headerRenderer = HeaderRenderer;

                var elementCounter = -1;
                foreach (var leadElement in columnLeaders)
                {
                    elementCounter++;
                    if (!followerElementFound)
                    {
                        // First, we check what other controls are in the same column
                        UIElement followerElement = null;
                        for (var childCount2 = 0; childCount2 < visibleChildren.Count; childCount2++)
                        {
                            if (visibleChildren[childCount2] == leadElement)
                                // We found the leader, so we check whether the next item is a follower
                                if (childCount2 < visibleChildren.Count - 1)
                                    foreach (var child in visibleChildren)
                                        if (SimpleView.GetFlowsWithPrevious(child))
                                        {
                                            // We found a follower
                                            followerElement = child;
                                            followerElementFound = true;
                                            break;
                                        }
                            if (followerElement != null) break;
                        }
                        if (followerElement != null)
                        {
                            followerElement.Measure(GeometryHelper.NewSize(columnWidths[elementCounter], 100000));
                            var followerHeight = followerElement.DesiredSize.Height;
                            if (followerHeight + Spacing*2 > availableSize.Height) followerHeight = availableSize.Height - Spacing*2;
                            if (followerHeight > 0d) followerElement.Measure(GeometryHelper.NewSize(columnWidths[elementCounter], followerHeight));
                            var elementSize = GeometryHelper.NewSize(columnWidths[elementCounter], availableSize.Width - Spacing*3 - followerHeight);
                            if (headerRenderer != null)
                            {
                                var clientArea = headerRenderer.GetClientArea(GeometryHelper.NewRect(new Point(0d, 0d), elementSize));
                                elementSize = clientArea.Size;
                            }
                            leadElement.Measure(elementSize);
                        }
                        else
                        {
                            var elementSize = GeometryHelper.NewSize(columnWidths[elementCounter], availableSize.Height - Spacing*2);
                            if (headerRenderer != null)
                            {
                                var clientArea = headerRenderer.GetClientArea(GeometryHelper.NewRect(new Point(0d, 0d), elementSize));
                                elementSize = clientArea.Size;
                            }
                            leadElement.Measure(elementSize);
                        }
                    }
                    else
                    {
                        var elementSize = GeometryHelper.NewSize(columnWidths[elementCounter], availableSize.Height - Spacing*2);
                        if (headerRenderer != null)
                        {
                            var clientArea = headerRenderer.GetClientArea(GeometryHelper.NewRect(new Point(0d, 0d), elementSize));
                            elementSize = clientArea.Size;
                        }
                        leadElement.Measure(elementSize);
                    }
                }
            }

            foreach (UIElement child in Children)
                child.IsVisibleChanged += (s, e) => InvalidateVisual();

            return realSize;
        }

        private void ChildVisibilityUpdated(object sender, DependencyPropertyChangedEventArgs e)
        {
            InvalidateMeasure();
            InvalidateArrange();
            InvalidateVisual();
        }

        private void ChildSizeUpdated(object sender, EventArgs e)
        {
            InvalidateMeasure();
            InvalidateArrange();
            InvalidateVisual();
        }

        private static double[] GetAllColumnWidths(ICollection<UIElement> columnLeaders, double usableWidth)
        {
            var columnWidths = new double[columnLeaders.Count];
            var widthUsableByStarColumns = usableWidth;
            var columnLeaderCount = 0;
            var totalStarCount = 0d;
            foreach (var columnLeader in columnLeaders)
            {
                var relativeColumnWidth = SimpleView.GetRelativeWidth(columnLeader);
                if (relativeColumnWidth.IsStar)
                    totalStarCount += relativeColumnWidth.Value;
            }
            foreach (var columnLeader in columnLeaders)
            {
                var relativeColumnWidth = SimpleView.GetRelativeWidth(columnLeader);
                if (relativeColumnWidth.IsAbsolute)
                {
                    columnWidths[columnLeaderCount] = relativeColumnWidth.Value;
                    widthUsableByStarColumns -= columnWidths[columnLeaderCount];
                }
                columnLeaderCount++;
            }
            columnLeaderCount = 0;
            foreach (var columnLeader in columnLeaders)
            {
                var relativeColumnWidth = SimpleView.GetRelativeWidth(columnLeader);
                if (relativeColumnWidth.IsAuto)
                {
                    columnWidths[columnLeaderCount] = columnLeader.DesiredSize.Width;
                    widthUsableByStarColumns -= columnWidths[columnLeaderCount];
                }
                columnLeaderCount++;
            }
            if (widthUsableByStarColumns < 0) widthUsableByStarColumns = 0;
            var oneStarWidth = widthUsableByStarColumns/totalStarCount;
            columnLeaderCount = 0;
            foreach (var columnLeader in columnLeaders)
            {
                var relativeColumnWidth = SimpleView.GetRelativeWidth(columnLeader);
                if (relativeColumnWidth.IsStar)
                {
                    columnWidths[columnLeaderCount] = oneStarWidth*relativeColumnWidth.Value;
                    widthUsableByStarColumns -= columnWidths[columnLeaderCount];
                }
                columnLeaderCount++;
            }
            return columnWidths;
        }

        private static double[] GetAllRowHeights(ICollection<UIElement> rowLeaders, double usableHeight)
        {
            var rowHeights = new double[rowLeaders.Count];
            var heightUsableByStarColumns = usableHeight;
            var columnLeaderCount = 0;
            var totalStarCount = 0d;
            foreach (var columnLeader in rowLeaders)
            {
                var relativeRowHeight = SimpleView.GetRelativeHeight(columnLeader);
                if (relativeRowHeight.IsStar)
                    totalStarCount += relativeRowHeight.Value;
            }
            foreach (var columnLeader in rowLeaders)
            {
                var relativeRowHeight = SimpleView.GetRelativeHeight(columnLeader);
                if (relativeRowHeight.IsAbsolute)
                {
                    rowHeights[columnLeaderCount] = relativeRowHeight.Value;
                    heightUsableByStarColumns -= rowHeights[columnLeaderCount];
                }
                columnLeaderCount++;
            }
            columnLeaderCount = 0;
            foreach (var columnLeader in rowLeaders)
            {
                var relativeRowHeight = SimpleView.GetRelativeHeight(columnLeader);
                if (relativeRowHeight.IsAuto)
                {
                    rowHeights[columnLeaderCount] = columnLeader.DesiredSize.Width;
                    heightUsableByStarColumns -= rowHeights[columnLeaderCount];
                }
                columnLeaderCount++;
            }
            if (heightUsableByStarColumns < 0) heightUsableByStarColumns = 0;
            var oneStarHeight = heightUsableByStarColumns/totalStarCount;
            columnLeaderCount = 0;
            foreach (var columnLeader in rowLeaders)
            {
                var relativeRowHeight = SimpleView.GetRelativeHeight(columnLeader);
                if (relativeRowHeight.IsStar)
                {
                    rowHeights[columnLeaderCount] = oneStarHeight*relativeRowHeight.Value;
                    heightUsableByStarColumns -= rowHeights[columnLeaderCount];
                }
                columnLeaderCount++;
            }
            return rowHeights;
        }

        /// <summary>
        /// When overridden in a derived class, positions child elements and determines a size for a <see cref="T:System.Windows.FrameworkElement" /> derived class.
        /// </summary>
        /// <param name="finalSize">The final area within the parent that this element should use to arrange itself and its children.</param>
        /// <returns>The actual size used.</returns>
        protected override Size ArrangeOverride(Size finalSize)
        {
            var visibleChildren = Children.Cast<UIElement>().Where(child => child.Visibility != Visibility.Collapsed).ToList();
            var childCount = visibleChildren.Count;

            if (Orientation == Orientation.Vertical)
            {
                var usableHeight = finalSize.Height - (Spacing*2);
                if (childCount > 1) usableHeight -= Spacing*(childCount - 1);
                var rowLeaders = visibleChildren.Where(child => !SimpleView.GetFlowsWithPrevious(child)).ToList();
                var rowHeights = GetAllRowHeights(rowLeaders, usableHeight);

                var currentTop = Spacing;
                var followerElementFound = false;

                var headerRenderer = HeaderRenderer;
                if (headerRenderer != null) _headerRenderAreas = new List<HeaderRenderInformation>();

                var elementCounter = -1;
                foreach (var leadElement in rowLeaders)
                {
                    elementCounter++;
                    if (!followerElementFound)
                    {
                        // First, we check what other controls are in the same row
                        UIElement followerElement = null;
                        for (var childCount2 = 0; childCount2 < visibleChildren.Count; childCount2++)
                        {
                            if (visibleChildren[childCount2] == leadElement)
                                // We found the leader, so we check whether the next item is a follower
                                if (childCount2 < visibleChildren.Count - 1)
                                    foreach (var child in visibleChildren)
                                        if (SimpleView.GetFlowsWithPrevious(child))
                                        {
                                            // We found a follower
                                            followerElement = child;
                                            followerElementFound = true;
                                            break;
                                        }
                            if (followerElement != null) break;
                        }
                        if (followerElement != null)
                        {
                            var followerWidth = followerElement.DesiredSize.Width;
                            if (finalSize.Width > 0 && followerWidth + Spacing*2 > finalSize.Width) followerWidth = finalSize.Width - Spacing*2;
                            var followerArea = GeometryHelper.NewRect(finalSize.Width - followerWidth - Spacing, currentTop, followerWidth, rowHeights[elementCounter]);
                            if (headerRenderer != null)
                            {
                                _headerRenderAreas.Add(new HeaderRenderInformation {Child = followerElement, TotalArea = followerArea});
                                followerArea = headerRenderer.GetClientArea(followerArea);
                            }
                            followerElement.Arrange(followerArea);
                            var elementArea = GeometryHelper.NewRect(Spacing, currentTop, finalSize.Width - Spacing*3 - followerWidth, rowHeights[elementCounter]);
                            if (headerRenderer != null)
                            {
                                _headerRenderAreas.Add(new HeaderRenderInformation {Child = leadElement, TotalArea = elementArea});
                                elementArea = headerRenderer.GetClientArea(elementArea);
                            }
                            leadElement.Arrange(elementArea);
                        }
                        else
                        {
                            var elementArea = GeometryHelper.NewRect(Spacing, currentTop, finalSize.Width - Spacing*2, rowHeights[elementCounter]);
                            if (headerRenderer != null)
                            {
                                _headerRenderAreas.Add(new HeaderRenderInformation {Child = leadElement, TotalArea = elementArea});
                                elementArea = headerRenderer.GetClientArea(elementArea);
                            }
                            leadElement.Arrange(elementArea);
                        }
                        currentTop += (int) (rowHeights[elementCounter] + Spacing);
                    }
                    else
                    {
                        var elementArea = GeometryHelper.NewRect(Spacing, currentTop, finalSize.Width - Spacing*2, rowHeights[elementCounter]);
                        if (headerRenderer != null)
                        {
                            _headerRenderAreas.Add(new HeaderRenderInformation {Child = leadElement, TotalArea = elementArea});
                            elementArea = headerRenderer.GetClientArea(elementArea);
                        }
                        leadElement.Arrange(elementArea);
                        currentTop += (int) (rowHeights[elementCounter] + Spacing);
                    }
                }
            }
            else
            {
                var usableWidth = finalSize.Width - (Spacing*2);
                if (childCount > 1) usableWidth -= Spacing*(childCount - 1);
                var columnLeaders = visibleChildren.Where(child => !SimpleView.GetFlowsWithPrevious(child)).ToList();

                var columnWidths = GetAllColumnWidths(columnLeaders, usableWidth);

                var currentLeft = Spacing;
                var followerElementFound = false;

                var headerRenderer = HeaderRenderer;
                if (headerRenderer != null) _headerRenderAreas = new List<HeaderRenderInformation>();

                var elementCounter = -1;
                foreach (var leadElement in columnLeaders)
                {
                    elementCounter++;
                    if (!followerElementFound)
                    {
                        // First, we check what other controls are in the same row
                        UIElement followerElement = null;
                        for (var childCount2 = 0; childCount2 < visibleChildren.Count; childCount2++)
                        {
                            if (visibleChildren[childCount2] == leadElement)
                                // We found the leader, so we check whether the next item is a follower
                                if (childCount2 < visibleChildren.Count - 1)
                                    foreach (var child in visibleChildren)
                                        if (SimpleView.GetFlowsWithPrevious(child))
                                        {
                                            // We found a follower
                                            followerElement = child;
                                            followerElementFound = true;
                                            break;
                                        }
                            if (followerElement != null) break;
                        }
                        if (followerElement != null)
                        {
                            var followerHeight = followerElement.DesiredSize.Height;
                            if (finalSize.Height > 0 && followerHeight + Spacing*2 > finalSize.Height) followerHeight = finalSize.Height - Spacing*2;
                            var followerArea = GeometryHelper.NewRect(currentLeft, finalSize.Height - followerHeight - Spacing, columnWidths[elementCounter], followerHeight);
                            if (headerRenderer != null)
                            {
                                _headerRenderAreas.Add(new HeaderRenderInformation {Child = followerElement, TotalArea = followerArea});
                                followerArea = headerRenderer.GetClientArea(followerArea);
                            }
                            followerElement.Arrange(followerArea);
                            var elementArea = GeometryHelper.NewRect(currentLeft, Spacing, columnWidths[elementCounter], finalSize.Height - Spacing*3 - followerHeight);
                            if (headerRenderer != null)
                            {
                                _headerRenderAreas.Add(new HeaderRenderInformation {Child = leadElement, TotalArea = elementArea});
                                elementArea = headerRenderer.GetClientArea(elementArea);
                            }
                            leadElement.Arrange(elementArea);
                        }
                        else
                        {
                            var elementArea = GeometryHelper.NewRect(currentLeft, Spacing, columnWidths[elementCounter], finalSize.Height - Spacing*2);
                            if (headerRenderer != null)
                            {
                                _headerRenderAreas.Add(new HeaderRenderInformation {Child = leadElement, TotalArea = elementArea});
                                elementArea = headerRenderer.GetClientArea(elementArea);
                            }
                            leadElement.Arrange(elementArea);
                        }
                        currentLeft += (int) (columnWidths[elementCounter] + Spacing);
                    }
                    else
                    {
                        var elementArea = GeometryHelper.NewRect(currentLeft, Spacing, columnWidths[elementCounter], finalSize.Height - Spacing*2);
                        if (headerRenderer != null)
                        {
                            _headerRenderAreas.Add(new HeaderRenderInformation {Child = leadElement, TotalArea = elementArea});
                            elementArea = headerRenderer.GetClientArea(elementArea);
                        }
                        leadElement.Arrange(elementArea);
                        currentLeft += (int) (columnWidths[elementCounter] + Spacing);
                    }
                }
            }

            return finalSize;
        }

        /// <summary>
        /// Invoked when an unhandled System.Windows.Input.Mouse.MouseMove attached event reaches an element in its route that is derived from this class. Implement this method to add class handling for this event.
        /// </summary>
        /// <param name="e">The <see cref="T:System.Windows.Input.MouseEventArgs" /> that contains the event data.</param>
        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (HeaderRenderer != null)
            {
                HeaderRenderer.OnMouseMove(this, e, _headerRenderAreas);
                if (e.Handled) return;
            }
            base.OnMouseMove(e);
        }

        /// <summary>
        /// Invoked when an unhandled System.Windows.Input.Mouse.MouseEnter attached event is raised on this element. Implement this method to add class handling for this event.
        /// </summary>
        /// <param name="e">The <see cref="T:System.Windows.Input.MouseEventArgs" /> that contains the event data.</param>
        protected override void OnMouseEnter(MouseEventArgs e)
        {
            if (HeaderRenderer != null)
            {
                HeaderRenderer.OnMouseEnter(this, e, _headerRenderAreas);
                if (e.Handled) return;
            }
            base.OnMouseEnter(e);
        }

        /// <summary>
        /// Invoked when an unhandled System.Windows.Input.Mouse.MouseLeave attached event is raised on this element. Implement this method to add class handling for this event.
        /// </summary>
        /// <param name="e">The <see cref="T:System.Windows.Input.MouseEventArgs" /> that contains the event data.</param>
        protected override void OnMouseLeave(MouseEventArgs e)
        {
            if (HeaderRenderer != null)
            {
                HeaderRenderer.OnMouseLeave(this, e, _headerRenderAreas);
                if (e.Handled) return;
            }
            base.OnMouseLeave(e);
        }

        /// <summary>
        /// Invoked when an unhandled System.Windows.Input.Mouse.MouseDown attached event reaches an element in its route that is derived from this class. Implement this method to add class handling for this event.
        /// </summary>
        /// <param name="e">The <see cref="T:System.Windows.Input.MouseButtonEventArgs" /> that contains the event data. This event data reports details about the mouse button that was pressed and the handled state.</param>
        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            if (HeaderRenderer != null)
            {
                HeaderRenderer.OnMouseDown(this, e, _headerRenderAreas);
                if (e.Handled) return;
            }
            base.OnMouseDown(e);
        }

        /// <summary>
        /// Invoked when an unhandled System.Windows.Input.Mouse.MouseUp routed event reaches an element in its route that is derived from this class. Implement this method to add class handling for this event.
        /// </summary>
        /// <param name="e">The <see cref="T:System.Windows.Input.MouseButtonEventArgs" /> that contains the event data. The event data reports that the mouse button was released.</param>
        protected override void OnMouseUp(MouseButtonEventArgs e)
        {
            if (HeaderRenderer != null)
            {
                HeaderRenderer.OnMouseUp(this, e, _headerRenderAreas);
                if (e.Handled) return;
            }
            base.OnMouseUp(e);
        }

        private List<HeaderRenderInformation> _headerRenderAreas;

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
        }

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
                dc.PushTransform(new TranslateTransform(area.TotalArea.X, area.TotalArea.Y));
                headerRenderer.Render(dc, area.TotalArea.Size, area.Child);
                dc.Pop();
            }
        }
    }

    /// <summary>
    /// This interface can be implemented to create a renderer object that measures and renders
    /// headers for child elements contained in a MultiPanel
    /// </summary>
    public interface IMultiPanelHeaderRenderer
    {
        /// <summary>
        /// Returns the client area (the area available for the child control) based on the total area available for the child
        /// </summary>
        /// <param name="totalArea">The total area.</param>
        /// <returns>The client area</returns>
        /// <remarks>
        /// When a header is added for a child element, it takes away from the area available for the child.
        /// For instance, when a header text is to be rendered across the top of a child, then the child must be moved down 
        /// and the overall space for the child shrinks accordingly. The value returned by this method indicates
        /// the remaining space for the child element.
        /// </remarks>
        Rect GetClientArea(Rect totalArea);

        /// <summary>Renders the header element for a single child.</summary>
        /// <param name="dc">The drawing context.</param>
        /// <param name="clientAreaSize">Size of the client area of the entire contained control (including area). Note: Top/left render position is always 0,0.</param>
        /// <param name="child">The actual child element.</param>
        void Render(DrawingContext dc, Size clientAreaSize, UIElement child);

        /// <summary>
        /// Invoked when an unhandled System.Windows.Input.Mouse.MouseEnter attached event is raised on this element. Implement this method to add class handling for this event.
        /// </summary>
        /// <param name="multiPanel">The multi panel.</param>
        /// <param name="e">The <see cref="T:System.Windows.Input.MouseEventArgs" /> that contains the event data.</param>
        /// <param name="headerRenderAreas">Area information for specific headers</param>
        void OnMouseEnter(MultiPanel multiPanel, MouseEventArgs e, List<MultiPanel.HeaderRenderInformation> headerRenderAreas);

        /// <summary>
        /// Invoked when an unhandled System.Windows.Input.Mouse.MouseLeave attached event is raised on this element. Implement this method to add class handling for this event.
        /// </summary>
        /// <param name="multiPanel">The multi panel.</param>
        /// <param name="e">The <see cref="T:System.Windows.Input.MouseEventArgs" /> that contains the event data.</param>
        /// <param name="headerRenderAreas">Area information for specific headers</param>
        void OnMouseLeave(MultiPanel multiPanel, MouseEventArgs e, List<MultiPanel.HeaderRenderInformation> headerRenderAreas);

        /// <summary>
        /// Invoked when an unhandled System.Windows.Input.Mouse.MouseDown attached event reaches an element in its route that is derived from this class. Implement this method to add class handling for this event.
        /// </summary>
        /// <param name="multiPanel">The multi panel.</param>
        /// <param name="e">The <see cref="T:System.Windows.Input.MouseButtonEventArgs" /> that contains the event data. This event data reports details about the mouse button that was pressed and the handled state.</param>
        /// <param name="headerRenderAreas">Area information for specific headers</param>
        void OnMouseDown(MultiPanel multiPanel, MouseButtonEventArgs e, List<MultiPanel.HeaderRenderInformation> headerRenderAreas);

        /// <summary>
        /// Invoked when an unhandled System.Windows.Input.Mouse.MouseUp routed event reaches an element in its route that is derived from this class. Implement this method to add class handling for this event.
        /// </summary>
        /// <param name="multiPanel">The multi panel.</param>
        /// <param name="e">The <see cref="T:System.Windows.Input.MouseButtonEventArgs" /> that contains the event data. The event data reports that the mouse button was released.</param>
        /// <param name="headerRenderAreas">Area information for specific headers</param>
        void OnMouseUp(MultiPanel multiPanel, MouseButtonEventArgs e, List<MultiPanel.HeaderRenderInformation> headerRenderAreas);

        /// <summary>
        /// Invoked when an unhandled System.Windows.Input.Mouse.MouseMove attached event reaches an element in its route that is derived from this class. Implement this method to add class handling for this event.
        /// </summary>
        /// <param name="multiPanel">The multi panel.</param>
        /// <param name="e">The <see cref="T:System.Windows.Input.MouseEventArgs" /> that contains the event data.</param>
        /// <param name="headerRenderAreas">Area information for specific headers</param>
        void OnMouseMove(MultiPanel multiPanel, MouseEventArgs e, List<MultiPanel.HeaderRenderInformation> headerRenderAreas);
    }

    /// <summary>
    /// Standard header renderer object for multi panel headers
    /// </summary>
    public class MultiPanelHeaderRenderer : FrameworkElement, IMultiPanelHeaderRenderer
    {
        /// <summary>
        /// Header Text Orientation
        /// </summary>
        /// <value>The orientation of the header text.</value>
        public Orientation Orientation
        {
            get { return (Orientation) GetValue(OrientationProperty); }
            set { SetValue(OrientationProperty, value); }
        }

        /// <summary>
        /// Header Text Orientation
        /// </summary>
        /// <value>The orientation of the header text.</value>
        public static readonly DependencyProperty OrientationProperty = DependencyProperty.Register("Orientation", typeof (Orientation), typeof (MultiPanelHeaderRenderer), new PropertyMetadata(Orientation.Horizontal));

        /// <summary>
        /// Header text font size
        /// </summary>
        /// <value>The header text font size.</value>
        public double FontSize
        {
            get { return (double) GetValue(FontSizeProperty); }
            set { SetValue(FontSizeProperty, value); }
        }

        /// <summary>
        /// Header text font size
        /// </summary>
        public static readonly DependencyProperty FontSizeProperty = DependencyProperty.Register("FontSize", typeof (double), typeof (MultiPanelHeaderRenderer), new PropertyMetadata(12d));

        /// <summary>
        /// Header text font family
        /// </summary>
        /// <value>The header text font family.</value>
        public FontFamily FontFamily
        {
            get { return (FontFamily) GetValue(FontFamilyProperty); }
            set { SetValue(FontFamilyProperty, value); }
        }

        /// <summary>
        /// Header text font family
        /// </summary>
        public static readonly DependencyProperty FontFamilyProperty = DependencyProperty.Register("FontFamily", typeof (FontFamily), typeof (MultiPanelHeaderRenderer), new PropertyMetadata(new FontFamily("Segoe UI")));

        /// <summary>
        /// Header text font style
        /// </summary>
        /// <value>The header text font style.</value>
        public FontStyle FontStyle
        {
            get { return (FontStyle) GetValue(FontStyleProperty); }
            set { SetValue(FontStyleProperty, value); }
        }

        /// <summary>
        /// Header text font style
        /// </summary>
        public static readonly DependencyProperty FontStyleProperty = DependencyProperty.Register("FontStyle", typeof (FontStyle), typeof (MultiPanelHeaderRenderer), new PropertyMetadata(FontStyles.Normal));

        /// <summary>
        /// Header text font weight
        /// </summary>
        /// <value>The header text font weight.</value>
        public FontWeight FontWeight
        {
            get { return (FontWeight) GetValue(FontWeightProperty); }
            set { SetValue(FontWeightProperty, value); }
        }

        /// <summary>
        /// Header text font weight
        /// </summary>
        public static readonly DependencyProperty FontWeightProperty = DependencyProperty.Register("FontWeight", typeof (FontWeight), typeof (MultiPanelHeaderRenderer), new PropertyMetadata(FontWeights.Normal));

        /// <summary>
        /// Foreground brush for the header text
        /// </summary>
        /// <value>Header text foreground brush.</value>
        public Brush Foreground
        {
            get { return (Brush) GetValue(ForegroundProperty); }
            set { SetValue(ForegroundProperty, value); }
        }

        /// <summary>
        /// Foreground brush for the header text
        /// </summary>
        public static readonly DependencyProperty ForegroundProperty = DependencyProperty.Register("Foreground", typeof (Brush), typeof (MultiPanelHeaderRenderer), new PropertyMetadata(Brushes.Black));

        /// <summary>
        /// The background brush for the entire header area
        /// </summary>
        /// <value>The background brush.</value>
        public Brush Background
        {
            get { return (Brush) GetValue(BackgroundProperty); }
            set { SetValue(BackgroundProperty, value); }
        }

        /// <summary>
        /// The background brush for the entire header area
        /// </summary>
        public static readonly DependencyProperty BackgroundProperty = DependencyProperty.Register("Background", typeof (Brush), typeof (MultiPanelHeaderRenderer), new PropertyMetadata(null));

        /// <summary>
        /// If true, the renderer attempts to pick up a View.TitleColor setting fore the foreground color of the header text
        /// </summary>
        /// <value><c>true</c> if [use title color for foreground]; otherwise, <c>false</c>.</value>
        public bool UseTitleColorForForeground
        {
            get { return (bool) GetValue(UseTitleColorForForegroundProperty); }
            set { SetValue(UseTitleColorForForegroundProperty, value); }
        }

        /// <summary>
        /// If true, the renderer attempts to pick up a View.TitleColor setting fore the foreground color of the header text
        /// </summary>
        public static readonly DependencyProperty UseTitleColorForForegroundProperty = DependencyProperty.Register("UseTitleColorForForeground", typeof (bool), typeof (MultiPanelHeaderRenderer), new PropertyMetadata(false));

        /// <summary>
        /// If true, the renderer attempts to pick up a View.TitleColor setting fore the background color of the header
        /// </summary>
        /// <value><c>true</c> if [use title color for background]; otherwise, <c>false</c>.</value>
        public bool UseTitleColorForBackground
        {
            get { return (bool) GetValue(UseTitleColorForBackgroundProperty); }
            set { SetValue(UseTitleColorForBackgroundProperty, value); }
        }

        /// <summary>
        /// If true, the renderer attempts to pick up a View.TitleColor setting fore the background color of the header
        /// </summary>
        public static readonly DependencyProperty UseTitleColorForBackgroundProperty = DependencyProperty.Register("UseTitleColorForBackground", typeof (bool), typeof (MultiPanelHeaderRenderer), new PropertyMetadata(false));

        /// <summary>
        /// Brush used to render the close icon
        /// </summary>
        /// <value>The close icon.</value>
        public Brush CloseIcon
        {
            get { return (Brush) GetValue(CloseIconProperty); }
            set { SetValue(CloseIconProperty, value); }
        }

        /// <summary>
        /// Brush used to render the close icon
        /// </summary>
        public static readonly DependencyProperty CloseIconProperty = DependencyProperty.Register("CloseIcon", typeof (Brush), typeof (MultiPanelHeaderRenderer), new PropertyMetadata(Brushes.Red));

        /// <summary>
        /// Max height/width for the close icon
        /// </summary>
        /// <value>The maximum size of the close icon.</value>
        public double MaxCloseIconSize
        {
            get { return (double) GetValue(MaxCloseIconSizeProperty); }
            set { SetValue(MaxCloseIconSizeProperty, value); }
        }

        /// <summary>
        /// Max height/width for the close icon
        /// </summary>
        public static readonly DependencyProperty MaxCloseIconSizeProperty = DependencyProperty.Register("MaxCloseIconSize", typeof (double), typeof (MultiPanelHeaderRenderer), new PropertyMetadata(20d));

        /// <summary>
        /// Optional style for windows in undock scenarios
        /// </summary>
        /// <value>The float window style.</value>
        /// <remarks>The style needs to target type FloatingDockWindow</remarks>
        public Style FloatWindowStyle
        {
            get { return (Style) GetValue(FloatWindowStyleProperty); }
            set { SetValue(FloatWindowStyleProperty, value); }
        }

        /// <summary>
        /// Optional style for windows in undock scenarios
        /// </summary>
        /// <remarks>The style needs to target type FloatingDockWindow</remarks>
        public static readonly DependencyProperty FloatWindowStyleProperty = DependencyProperty.Register("FloatWindowStyle", typeof (Style), typeof (MultiPanelHeaderRenderer), new PropertyMetadata(null));

        /// <summary>
        /// Returns the client area (the area available for the child control) based on the total area available for the child
        /// </summary>
        /// <param name="totalArea">The total area.</param>
        /// <returns>The client area</returns>
        /// <remarks>When a header is added for a child element, it takes away from the area available for the child.
        /// For instance, when a header text is to be rendered across the top of a child, then the child must be moved down
        /// and the overall space for the child shrinks accordingly. The value returned by this method indicates
        /// the remaining space for the child element.</remarks>
        public Rect GetClientArea(Rect totalArea)
        {
            var ft = GetFormattedText();
            if (Orientation == Orientation.Horizontal)
                return GeometryHelper.NewRect(totalArea.X, totalArea.Y + ft.Height, totalArea.Width, totalArea.Height - ft.Height);
            return GeometryHelper.NewRect(totalArea.X + ft.Height + 5, totalArea.Y, totalArea.Width - ft.Height - 5, totalArea.Height);
        }

        private FormattedText _standardHeight;
        private Point _lastLeftMouseDownPosition = new Point(-1, -1);

        private FormattedText GetFormattedText(string text = "", Brush foreground = null)
        {
            if (foreground == null) foreground = Brushes.Black;
            if (string.IsNullOrEmpty(text))
            {
                if (_standardHeight == null)
                    _standardHeight = new FormattedText("X", CultureInfo.CurrentUICulture, FlowDirection.LeftToRight, new Typeface(FontFamily, FontStyle, FontWeight, FontStretches.Normal), FontSize, foreground) {MaxLineCount = 1};
                return _standardHeight;
            }
            return new FormattedText(text, CultureInfo.CurrentUICulture, FlowDirection.LeftToRight, new Typeface(FontFamily, FontStyle, FontWeight, FontStretches.Normal), FontSize, foreground) {MaxLineCount = 1};
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
                if (closable) dc.DrawRectangle(CloseIcon, null, GeometryHelper.NewRect(headerRect.Width - iconSize - 4, (int) ((headerRect.Height - iconSize)/2), iconSize, iconSize));
            }
            else
            {
                // We need to set the appropriate transformation to render vertical text at the right position
                dc.PushTransform(new RotateTransform(-90d));
                dc.PushTransform(new TranslateTransform((headerRect.Height + 5)*-1, 0d));
                var iconSize = Math.Min(headerRect.Height - 7, MaxCloseIconSize);
                ft.MaxTextWidth = Math.Max(headerRect.Height - 5, 0);
                if (closable) ft.MaxTextWidth = Math.Max(ft.MaxTextWidth - iconSize - 5, 0); // Making room for the close button
                ft.TextAlignment = TextAlignment.Right;
                dc.DrawText(ft, new Point(0d, 0d));
                dc.Pop();
                dc.Pop();
                if (closable) dc.DrawRectangle(CloseIcon, null, GeometryHelper.NewRect((int) ((headerRect.Width + 1 - iconSize)/2), 5, iconSize, iconSize));
            }

            dc.Pop(); // Remove the clip
        }

        /// <summary>
        /// Invoked when an unhandled System.Windows.Input.Mouse.MouseEnter attached event is raised on this element. Implement this method to add class handling for this event.
        /// </summary>
        /// <param name="multiPanel">The multi panel.</param>
        /// <param name="e">The <see cref="T:System.Windows.Input.MouseEventArgs" /> that contains the event data.</param>
        /// <param name="headerRenderAreas"></param>
        public void OnMouseEnter(MultiPanel multiPanel, MouseEventArgs e, List<MultiPanel.HeaderRenderInformation> headerRenderAreas)
        {
        }

        /// <summary>
        /// Invoked when an unhandled System.Windows.Input.Mouse.MouseLeave attached event is raised on this element. Implement this method to add class handling for this event.
        /// </summary>
        /// <param name="multiPanel">The multi panel.</param>
        /// <param name="e">The <see cref="T:System.Windows.Input.MouseEventArgs" /> that contains the event data.</param>
        /// <param name="headerRenderAreas"></param>
        public void OnMouseLeave(MultiPanel multiPanel, MouseEventArgs e, List<MultiPanel.HeaderRenderInformation> headerRenderAreas)
        {
        }

        /// <summary>
        /// Invoked when an unhandled System.Windows.Input.Mouse.MouseDown attached event reaches an element in its route that is derived from this class. Implement this method to add class handling for this event.
        /// </summary>
        /// <param name="multiPanel">The multi panel.</param>
        /// <param name="e">The <see cref="T:System.Windows.Input.MouseButtonEventArgs" /> that contains the event data. This event data reports details about the mouse button that was pressed and the handled state.</param>
        /// <param name="headerRenderAreas"></param>
        public void OnMouseDown(MultiPanel multiPanel, MouseButtonEventArgs e, List<MultiPanel.HeaderRenderInformation> headerRenderAreas)
        {
            if (e.ChangedButton != MouseButton.Left) return;
            _lastLeftMouseDownPosition = e.GetPosition(multiPanel);
        }

        /// <summary>
        /// Invoked when an unhandled System.Windows.Input.Mouse.MouseUp routed event reaches an element in its route that is derived from this class. Implement this method to add class handling for this event.
        /// </summary>
        /// <param name="multiPanel">The multi panel.</param>
        /// <param name="e">The <see cref="T:System.Windows.Input.MouseButtonEventArgs" /> that contains the event data. The event data reports that the mouse button was released.</param>
        /// <param name="headerRenderAreas"></param>
        public void OnMouseUp(MultiPanel multiPanel, MouseButtonEventArgs e, List<MultiPanel.HeaderRenderInformation> headerRenderAreas)
        {
            if (e.ChangedButton != MouseButton.Left) return;
            if (_lastLeftMouseDownPosition.X < 0 || _lastLeftMouseDownPosition.Y < 0) return;

            var position = e.GetPosition(multiPanel);
            var overClose = false;
            var lastLeftMouseDownPosition = new Point(_lastLeftMouseDownPosition.X, _lastLeftMouseDownPosition.Y);
            _lastLeftMouseDownPosition = new Point(-1, -1); // Since we are now in up-state again, we wipe out the last info

            foreach (var child in headerRenderAreas.Where(area => area.TotalArea.Contains(lastLeftMouseDownPosition) && SimpleView.GetClosable(area.Child)))
            {
                var clientArea = GetClientArea(child.TotalArea);
                if (Orientation == Orientation.Horizontal)
                {
                    var headerHeight = clientArea.Top - child.TotalArea.Top;
                    overClose = (lastLeftMouseDownPosition.Y <= clientArea.Top && lastLeftMouseDownPosition.X >= child.TotalArea.Width - headerHeight);
                }
                else
                {
                    var headerWidth = clientArea.Left - child.TotalArea.Left;
                    overClose = (lastLeftMouseDownPosition.X <= clientArea.Left && lastLeftMouseDownPosition.Y <= child.TotalArea.Top + headerWidth);
                }
            }
            if (!overClose) return; // The down-click didn't happen over a close button, so we are already done

            MultiPanel.HeaderRenderInformation elementInfo = null;
            foreach (var child in headerRenderAreas.Where(area => area.TotalArea.Contains(position) && SimpleView.GetClosable(area.Child)))
            {
                var clientArea = GetClientArea(child.TotalArea);
                if (Orientation == Orientation.Horizontal)
                {
                    var headerHeight = clientArea.Top - child.TotalArea.Top;
                    if (position.Y <= clientArea.Top && position.X >= child.TotalArea.Width - headerHeight)
                    {
                        elementInfo = child;
                        break;
                    }
                }
                else
                {
                    var headerWidth = clientArea.Left - child.TotalArea.Left;
                    if (position.X <= clientArea.Left && position.Y <= child.TotalArea.Top + headerWidth)
                    {
                        elementInfo = child;
                        break;
                    }
                }
            }
            if (elementInfo == null) return; // The up-click wasn't over a close button, so we ignore it

            var action = SimpleView.GetCloseAction(elementInfo.Child);
            if (action == null)
                elementInfo.Child.Visibility = Visibility.Collapsed;
            else if (action.CanExecute(elementInfo.Child))
                action.Execute(elementInfo.Child);

            multiPanel.InvalidateArrange();
            multiPanel.InvalidateMeasure();
            multiPanel.InvalidateVisual();
        }

        /// <summary>
        /// Invoked when an unhandled System.Windows.Input.Mouse.MouseMove attached event reaches an element in its route that is derived from this class. Implement this method to add class handling for this event.
        /// </summary>
        /// <param name="multiPanel">The multi panel.</param>
        /// <param name="e">The <see cref="T:System.Windows.Input.MouseEventArgs" /> that contains the event data.</param>
        /// <param name="headerRenderAreas"></param>
        public void OnMouseMove(MultiPanel multiPanel, MouseEventArgs e, List<MultiPanel.HeaderRenderInformation> headerRenderAreas)
        {
            var position = e.GetPosition(multiPanel);

            if (e.LeftButton == MouseButtonState.Pressed && headerRenderAreas != null)
                foreach (var child in headerRenderAreas.Where(area => area.TotalArea.Contains(position) && SimpleView.GetSupportsDocking(area.Child)))
                {
                    var clientArea = GetClientArea(child.TotalArea);
                    if (Orientation == Orientation.Horizontal)
                    {
                        if (position.Y > clientArea.Top) continue;
                    }
                    else
                    {
                        if (position.X > clientArea.Left) continue;
                    }

                    var mergedDictionaries = new List<ResourceDictionary>();
                    var resourceContainer = child.Child as FrameworkElement;
                    while (resourceContainer != null)
                    {
                        if (resourceContainer.Resources != null && (resourceContainer.Resources.Count > 0 || resourceContainer.Resources.MergedDictionaries.Count > 0))
                            mergedDictionaries.Add(resourceContainer.Resources);
                        resourceContainer = resourceContainer.Parent as FrameworkElement;
                    }

                    ItemsControl originalParent = null;
                    var parentElement = child.Child as FrameworkElement;
                    if (parentElement != null)
                        originalParent = parentElement.Parent as ItemsControl;

                    var floatWindow = new FloatingDockWindow(multiPanel, SimpleView.GetTitle(child.Child), GetChildIndexForHeader(child, headerRenderAreas), originalParent, mergedDictionaries) {Height = child.TotalArea.Height + 25, Width = child.TotalArea.Width + 10};
                    if (FloatWindowStyle != null) floatWindow.Style = FloatWindowStyle;
                    try
                    {
                        if (TemplatedParent == null)
                        {
                            if (multiPanel.Children.Contains(child.Child)) multiPanel.Children.Remove(child.Child);
                        }
                        else
                        {
                            var itemsControl = TemplatedParent as ItemsControl;
                            if (itemsControl != null)
                            {
                                if (itemsControl.Items.Contains(child.Child)) itemsControl.Items.Remove(child.Child);
                            }
                            else
                            {
                                var itemsPresenter = TemplatedParent as ItemsPresenter;
                                if (itemsPresenter != null)
                                {
                                    var itemsControl2 = ElementHelper.FindVisualTreeParent<ItemsControl>(itemsPresenter);
                                    if (itemsControl2 != null)
                                        if (itemsControl2.Items.Contains(child.Child)) itemsControl2.Items.Remove(child.Child);
                                }
                            }
                        }
                        floatWindow.Content = child.Child;

                        var dockResponder = child.Child as IDockResponder;
                        if (dockResponder != null)
                            dockResponder.OnUndocked();

                    }
                    catch
                    {
                    }
                    var absolutePosition = multiPanel.PointToScreen(position);
                    floatWindow.Top = absolutePosition.Y - 10;
                    floatWindow.Left = absolutePosition.X - 20;
                    floatWindow.Show();
                    multiPanel.InvalidateArrange();
                    multiPanel.InvalidateMeasure();
                    multiPanel.InvalidateVisual();
                    e.Handled = true;
                    floatWindow.DragMove();
                    return;
                }

            // We check whether we need to show a special cursor
            if (e.LeftButton == MouseButtonState.Released)
            {
                var overClose = false;
                if (headerRenderAreas != null)
                    foreach (var child in headerRenderAreas.Where(area => area.TotalArea.Contains(position) && SimpleView.GetClosable(area.Child)))
                    {
                        var clientArea = GetClientArea(child.TotalArea);
                        if (Orientation == Orientation.Horizontal)
                        {
                            var headerHeight = clientArea.Top - child.TotalArea.Top;
                            overClose = (position.Y <= clientArea.Top && position.X >= child.TotalArea.Width - headerHeight);
                        }
                        else
                        {
                            var headerWidth = clientArea.Left - child.TotalArea.Left;
                            overClose = (position.X <= clientArea.Left && position.Y <= child.TotalArea.Top + headerWidth);
                        }
                    }
                Mouse.SetCursor(overClose ? Cursors.Hand : Cursors.Arrow);
                if (overClose) e.Handled = true;
            }
        }

        private int GetChildIndexForHeader(MultiPanel.HeaderRenderInformation child, List<MultiPanel.HeaderRenderInformation> headerRenderAreas)
        {
            var index = -1;
            foreach (var area in headerRenderAreas)
            {
                index++;
                if (area == child) break;
            }
            return index;
        }
    }
}