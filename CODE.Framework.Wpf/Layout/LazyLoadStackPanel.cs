using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using CODE.Framework.Wpf.Controls;
using CODE.Framework.Wpf.Interfaces;
using CODE.Framework.Wpf.Utilities;

namespace CODE.Framework.Wpf.Layout
{
    /// <summary>
    /// Special stack panel class that can use ILazyLoad members
    /// </summary>
    public class LazyLoadStackPanel : Panel, IScrollInfo
    {
        private double _verticalOffset;
        private double _horizontalOffset;
        private double _extentHeight;
        private double _extentWidth;

        private int _currentFirstDisplayLineIndex = -1;
        private int _currentLastDisplayLineIndex = -1;
        private int _currentPageSize = -1;

        /// <summary>
        /// Initializes a new instance of the <see cref="LazyLoadStackPanel"/> class.
        /// </summary>
        public LazyLoadStackPanel()
        {
            VerticalAlignment = VerticalAlignment.Top;
            HorizontalAlignment = HorizontalAlignment.Left;
            SnapsToDevicePixels = true;

            SizeChanged += (s, e) =>
            {
                InvalidateScrollInfo();
                InvalidateVisual();
            };
        }

        private LazyLoadChildItem[] _childItems;
        private void PopulateInternalChildrenCollection()
        {
            var childCount = Children.Count;
            _childItems = new LazyLoadChildItem[childCount];

            for (var counter = 0; counter < childCount; counter++)
            {
                var currentChild = Children[counter];
                var newItem = new LazyLoadChildItem { Child = currentChild};
                _childItems[counter] = newItem;

                var listItem = currentChild as ListBoxItem;
                if (listItem == null) continue;
                
                // Get the content
                var contentChild = GetContentPresenterChild(listItem);
                if (contentChild == null) continue;
                var contentScroll = contentChild as IScrollInfo;
                if (contentScroll != null) newItem.ScrollInfo = contentScroll;

                // See if we can observe resizing
                var contentElement = contentChild as FrameworkElement;
                if (contentElement != null)
                    contentElement.SizeChanged += (s, e) => InvalidateMeasure();

                var invalidated = contentChild as IInvalidated;
                if (invalidated != null)
                    invalidated.MeasureInvalidated += (s, e) =>
                    {
                        InvalidateMeasure();
                        if (ScrollOwner != null)
                            ScrollOwner.InvalidateScrollInfo();
                    };
            }
        }

        private class LazyLoadChildItem
        {
            public UIElement Child { get; set; }
            public IScrollInfo ScrollInfo { get; set; }
            public Rect ClientArea { get; set; }
        }

        /// <summary>
        /// When overridden in a derived class, measures the size in layout required for child elements and determines a size for the <see cref="T:System.Windows.FrameworkElement" />-derived class.
        /// </summary>
        /// <param name="availableSize">The available size that this element can give to child elements. Infinity can be specified as a value to indicate that the element will size to whatever content is available.</param>
        /// <returns>The size that this element determines it needs during layout, based on its calculations of child element sizes.</returns>
        protected override Size MeasureOverride(Size availableSize)
        {
            PopulateInternalChildrenCollection();

            var maxWidth = double.IsNaN(availableSize.Width) || double.IsInfinity(availableSize.Width) ? 1000d : availableSize.Width;
            var maxHeight = double.IsNaN(availableSize.Height) || double.IsInfinity(availableSize.Height) ? 100d : availableSize.Height;
            var infiniteWidth = new Size(double.PositiveInfinity, maxHeight);

            var widthUsed = 0d;
            var heightUsed = 0d;
            var currentTop = VerticalOffset * -1;
            var currentLeft = HorizontalOffset * -1;

            _currentFirstDisplayLineIndex = -1;
            _currentLastDisplayLineIndex = -1;

            var childElementCount = -1;
            var foundVisibleElement = false;

            foreach (var item in _childItems)
            {
                childElementCount++;
                var specialContentFound = false;
                var child = item.Child;
                var listBoxItem = child as ListBoxItem;
                if (listBoxItem != null && listBoxItem.Content != null && listBoxItem.Content is FrameworkElement)
                {
                    specialContentFound = true;
                    var listContentElement = listBoxItem.Content as UIElement;
                    child.Measure(infiniteWidth);
                    listContentElement.Measure(infiniteWidth);
                    var offsetElement = child as IHorizontalOffset; // Can this element scroll internally?
                    var elementRect = GetNewRect(currentLeft, currentTop, offsetElement == null ? (listContentElement.DesiredSize.Width + listBoxItem.Padding.Left + listBoxItem.Padding.Right) : offsetElement.ExtentWidth, child.DesiredSize.Height);
                    item.ClientArea = elementRect;
                    widthUsed = Math.Max(elementRect.Width, widthUsed);
                    heightUsed += elementRect.Height;
                    currentTop += elementRect.Height;
                }

                if (!specialContentFound)
                {
                    child.Measure(item.ScrollInfo == null ? infiniteWidth : availableSize);

                    if (item.ScrollInfo is IScrollMeasure)
                        (item.ScrollInfo as IScrollMeasure).MeasureForScroll(availableSize);

                    var offsetElement = child as IHorizontalOffset; // Can this element scroll internally?
                    var elementSize = GetNewRect(currentLeft, currentTop, offsetElement == null ? child.DesiredSize.Width : offsetElement.ExtentWidth, child.DesiredSize.Height);
                    item.ClientArea = elementSize;
                    if (item.ScrollInfo == null)
                        widthUsed = Math.Max(elementSize.Width, widthUsed);
                    else
                        widthUsed = Math.Max(item.ScrollInfo.ExtentWidth, widthUsed);
                    heightUsed += elementSize.Height;
                    currentTop += elementSize.Height;
                }

                if (!foundVisibleElement && heightUsed - VerticalOffset - child.DesiredSize.Height >= 0)
                {
                    _currentFirstDisplayLineIndex = childElementCount;
                    foundVisibleElement = true;
                }
                if (_currentLastDisplayLineIndex == -1 && foundVisibleElement && heightUsed - VerticalOffset + child.DesiredSize.Height > maxHeight)
                    _currentLastDisplayLineIndex = childElementCount;
            }

            _currentPageSize = _currentLastDisplayLineIndex - _currentFirstDisplayLineIndex;
            CanHorizontallyScroll = widthUsed > maxWidth;
            CanVerticallyScroll = heightUsed > maxHeight;
            if (!CanHorizontallyScroll && HorizontalOffset > 0) HorizontalOffset = 0;
            if (!CanVerticallyScroll && VerticalOffset > 0) VerticalOffset = 0;
            ViewportHeight = maxHeight;
            ViewportWidth = maxWidth;
            ExtentHeight = heightUsed;
            ExtentWidth = widthUsed;
            InvalidateScrollInfo();

            return GeometryHelper.NewSize(maxWidth, maxHeight);
        }

        private TChildItem FindVisualChild<TChildItem>(DependencyObject obj) where TChildItem : DependencyObject
        {
            for (var i = 0; i < VisualTreeHelper.GetChildrenCount(obj); i++)
            {
                var child = VisualTreeHelper.GetChild(obj, i);
                if (child is TChildItem)
                    return (TChildItem)child;
                var childOfChild = FindVisualChild<TChildItem>(child);
                if (childOfChild != null)
                    return childOfChild;
            }
            return null;
        }

        private Rect GetNewRect(double left, double top, double width, double height)
        {
            if (SnapsToDevicePixels)
            {
                left = Math.Round(left);
                top = Math.Round(top);
                width = Math.Round(width);
                height = Math.Round(height);
            }

            return GeometryHelper.NewRect(left, top, width, height);
        }

        /// <summary>
        /// When overridden in a derived class, positions child elements and determines a size for a <see cref="T:System.Windows.FrameworkElement" /> derived class.
        /// </summary>
        /// <param name="finalSize">The final area within the parent that this element should use to arrange itself and its children.</param>
        /// <returns>The actual size used.</returns>
        protected override Size ArrangeOverride(Size finalSize)
        {
            var displayPortRect = GeometryHelper.NewRect(0d, 0d, finalSize.Width, finalSize.Height);

            foreach (var item in _childItems)
            {
                var child = item.Child;
                var listBoxItem = child as ListBoxItem;
                var offsetElement = child as IHorizontalOffset;
                if (offsetElement != null || listBoxItem != null && listBoxItem.Content is IHorizontalOffset)
                {
                    if (offsetElement == null) offsetElement = listBoxItem.Content as IHorizontalOffset;
                    offsetElement.Offset = item.ClientArea.Left;
                    child.Arrange(GeometryHelper.NewRect(0d, item.ClientArea.Top, item.ClientArea.Width, item.ClientArea.Height));
                }
                else if (item.ScrollInfo != null)
                {
                    child.Arrange(GeometryHelper.NewRect(0d, item.ClientArea.Top, finalSize.Width, item.ClientArea.Height));
                    if (item.ScrollInfo.ScrollOwner == null)
                        item.ScrollInfo.ScrollOwner = ScrollOwner;
                }
                else
                    child.Arrange(item.ClientArea);

                var isCurrentlyVisible = item.ClientArea.IntersectsWith(displayPortRect);

                if (!isCurrentlyVisible) continue;
                var lazyElement = child as ILazyLoad;
                if (lazyElement != null && !lazyElement.HasLoaded) lazyElement.Load();

                if (listBoxItem != null && listBoxItem.Content != null && listBoxItem.Content is ILazyLoad)
                {
                    var lazyElement2 = listBoxItem.Content as ILazyLoad;
                    if (!lazyElement2.HasLoaded) lazyElement2.Load();
                }
            }

            return finalSize;
        }

        /// <summary>
        /// Scrolls up within content by one logical unit.
        /// </summary>
        /// <exception cref="System.NotImplementedException"></exception>
        public void LineUp()
        {
            if (!CanVerticallyScroll) return;
            if (_currentFirstDisplayLineIndex > 0)
                ScrollToItem(_currentFirstDisplayLineIndex - 1);
        }

        /// <summary>
        /// Scrolls down within content by one logical unit.
        /// </summary>
        /// <exception cref="System.NotImplementedException"></exception>
        public void LineDown()
        {
            if (!CanVerticallyScroll) return;
            if (_currentLastDisplayLineIndex < Children.Count - 1)
                ScrollToItem(_currentFirstDisplayLineIndex + 1);
        }

        /// <summary>
        /// Scrolls left within content by one logical unit.
        /// </summary>
        /// <exception cref="System.NotImplementedException"></exception>
        public void LineLeft()
        {
            if (ScrollOwner != null)
            {
                SetHorizontalOffset(Math.Max(ScrollOwner.HorizontalOffset - 20, 0));
                ScrollOwner.InvalidateScrollInfo();
                return;
            }

            if (_childItems == null) PopulateInternalChildrenCollection();
            if (_childItems == null) return;

            foreach (var child in _childItems)
                if (child.ScrollInfo != null)
                {
                    var newOffset = Math.Max(child.ScrollInfo.HorizontalOffset - 20, 0);
                    child.ScrollInfo.SetHorizontalOffset(newOffset);
                    SetHorizontalOffset(newOffset);
                    break;
                }

            if (ScrollOwner != null) ScrollOwner.InvalidateScrollInfo();
        }

        /// <summary>
        /// Scrolls right within content by one logical unit.
        /// </summary>
        /// <exception cref="System.NotImplementedException"></exception>
        public void LineRight()
        {
            if (ScrollOwner != null)
            {
                SetHorizontalOffset(Math.Min(ScrollOwner.HorizontalOffset + 20, ExtentWidth - ViewportWidth));
                ScrollOwner.InvalidateScrollInfo();
                return;
            }

            if (_childItems == null) PopulateInternalChildrenCollection();
            if (_childItems == null) return;

            foreach (var child in _childItems)
                if (child.ScrollInfo != null)
                {
                    var newOffset = Math.Min(child.ScrollInfo.HorizontalOffset + 20, ExtentWidth - ViewportWidth);
                    child.ScrollInfo.SetHorizontalOffset(newOffset);
                    SetHorizontalOffset(newOffset);
                    break;
                }

            if (ScrollOwner != null) ScrollOwner.InvalidateScrollInfo();
        }

        /// <summary>
        /// Scrolls up within content by one page.
        /// </summary>
        /// <exception cref="System.NotImplementedException"></exception>
        public void PageUp()
        {
            if (!CanVerticallyScroll) return;
            ScrollToItem(_currentFirstDisplayLineIndex - _currentPageSize - 1);
        }

        /// <summary>
        /// Scrolls down within content by one page.
        /// </summary>
        /// <exception cref="System.NotImplementedException"></exception>
        public void PageDown()
        {
            if (!CanVerticallyScroll) return;
            ScrollToItem(_currentFirstDisplayLineIndex + _currentPageSize + 1);
        }

        /// <summary>
        /// Scrolls left within content by one page.
        /// </summary>
        /// <exception cref="System.NotImplementedException"></exception>
        public void PageLeft()
        {
            if (ScrollOwner != null)
            {
                SetHorizontalOffset(Math.Max(ScrollOwner.HorizontalOffset - ViewportWidth, 0));
                ScrollOwner.InvalidateScrollInfo();
                return;
            }

            if (_childItems == null) PopulateInternalChildrenCollection();
            if (_childItems == null) return;

            if (_childItems.Any())
                foreach (var child in _childItems.Where(child => child.ScrollInfo != null))
                {
                    SetHorizontalOffset(Math.Max(child.ScrollInfo.HorizontalOffset - ViewportWidth, 0));
                    break;
                }
            if (ScrollOwner != null) ScrollOwner.InvalidateScrollInfo();
        }

        /// <summary>
        /// Scrolls right within content by one page.
        /// </summary>
        /// <exception cref="System.NotImplementedException"></exception>
        public void PageRight()
        {
            if (ScrollOwner != null)
            {
                SetHorizontalOffset(Math.Min(ScrollOwner.HorizontalOffset + ViewportWidth, ExtentWidth - ViewportWidth));
                ScrollOwner.InvalidateScrollInfo();
                return;
            }

            if (_childItems == null) PopulateInternalChildrenCollection();
            if (_childItems == null) return;

            foreach (var child in _childItems)
                if (child.ScrollInfo != null)
                {
                    var newOffset = Math.Min(child.ScrollInfo.HorizontalOffset + ViewportWidth, ExtentWidth - ViewportWidth);
                    child.ScrollInfo.SetHorizontalOffset(newOffset);
                        SetHorizontalOffset(newOffset);
                    break;
                }

            if (ScrollOwner != null) ScrollOwner.InvalidateScrollInfo();
        }

        /// <summary>
        /// Scrolls up within content after a user clicks the wheel button on a mouse.
        /// </summary>
        /// <exception cref="System.NotImplementedException"></exception>
        public void MouseWheelUp()
        {
            if (!CanVerticallyScroll) return;
            ScrollToItem(_currentFirstDisplayLineIndex - SystemParameters.WheelScrollLines);
        }

        /// <summary>
        /// Scrolls down within content after a user clicks the wheel button on a mouse.
        /// </summary>
        /// <exception cref="System.NotImplementedException"></exception>
        public void MouseWheelDown()
        {
            if (!CanVerticallyScroll) return;
            ScrollToItem(_currentFirstDisplayLineIndex + SystemParameters.WheelScrollLines);
        }

        /// <summary>
        /// Scrolls left within content after a user clicks the wheel button on a mouse.
        /// </summary>
        /// <exception cref="System.NotImplementedException"></exception>
        public void MouseWheelLeft()
        {
            // TODO: Nothing for now, but should probably support it later
        }

        /// <summary>
        /// Scrolls right within content after a user clicks the wheel button on a mouse.
        /// </summary>
        /// <exception cref="System.NotImplementedException"></exception>
        public void MouseWheelRight()
        {
            // TODO: Nothing for now, but should probably support it later
        }

        private object GetContentPresenterChild(DependencyObject element)
        {
            var childItemCount = VisualTreeHelper.GetChildrenCount(element);
            for (var counter = 0; counter < childItemCount; counter++)
            {
                var element2 = VisualTreeHelper.GetChild(element, counter);
                var contentPresenter = element2 as ContentPresenter;
                if (contentPresenter != null && VisualTreeHelper.GetChildrenCount(contentPresenter) > 0) 
                    return VisualTreeHelper.GetChild(contentPresenter, 0);
                var result2 = GetContentPresenterChild(element2);
                if (result2 != null) return result2;
            }
            return null;
        }

        /// <summary>
        /// Sets the amount of horizontal offset.
        /// </summary>
        /// <param name="offset">The degree to which content is horizontally offset from the containing viewport.</param>
        /// <exception cref="System.NotImplementedException"></exception>
        public void SetHorizontalOffset(double offset)
        {
            HorizontalOffset = offset;

            if (_childItems == null) PopulateInternalChildrenCollection();
            if (_childItems == null) return;

            foreach (var child in _childItems)
                if (child.ScrollInfo != null)
                    child.ScrollInfo.SetHorizontalOffset(offset);
        }

        /// <summary>
        /// Sets the amount of vertical offset.
        /// </summary>
        /// <param name="offset">The degree to which content is vertically offset from the containing viewport.</param>
        /// <exception cref="System.NotImplementedException"></exception>
        public void SetVerticalOffset(double offset)
        {
            if (double.IsNegativeInfinity(offset)) // Likely TOP key hit
            {
                ScrollToItem(0);
                return;
            }
            if (double.IsPositiveInfinity(offset)) // Likely END key hit
            {
                var childItemCount = _childItems.Count();
                ScrollToItem(childItemCount - 1);
                return;
            }

            VerticalOffset = offset;

            if (_childItems == null) PopulateInternalChildrenCollection();
            if (_childItems == null) return;

            foreach (var child in _childItems)
                if (child.ScrollInfo != null)
                    child.ScrollInfo.SetVerticalOffset(offset);
        }

        /// <summary>
        /// Forces content to scroll until the coordinate space of a <see cref="T:System.Windows.Media.Visual" /> object is visible.
        /// </summary>
        /// <param name="visual">A <see cref="T:System.Windows.Media.Visual" /> that becomes visible.</param>
        /// <param name="rectangle">A bounding rectangle that identifies the coordinate space to make visible.</param>
        /// <returns>A <see cref="T:System.Windows.Rect" /> that is visible.</returns>
        /// <exception cref="System.NotImplementedException"></exception>
        public Rect MakeVisible(Visual visual, Rect rectangle)
        {
            if (!CanVerticallyScroll) return rectangle;

            var index = -1;

            foreach (Visual child in Children)
            {
                index++;
                if (child != visual) continue;
                if (index < _currentFirstDisplayLineIndex)
                    // Must scroll up
                    ScrollToItem(index);
                else if (index > _currentLastDisplayLineIndex)
                {
                    // Must scroll down
                    var scrollBy = index - _currentLastDisplayLineIndex;
                    ScrollToItem(_currentFirstDisplayLineIndex + scrollBy);
                }
                break;
            }

            return rectangle;
        }

        /// <summary>
        /// Gets or sets a value that indicates whether scrolling on the vertical axis is possible.
        /// </summary>
        /// <value><c>true</c> if this instance can vertically scroll; otherwise, <c>false</c>.</value>
        /// <returns>true if scrolling is possible; otherwise, false. This property has no default value.</returns>
        public bool CanVerticallyScroll { get; set; }

        /// <summary>
        /// Gets or sets a value that indicates whether scrolling on the horizontal axis is possible.
        /// </summary>
        /// <value><c>true</c> if this instance can horizontally scroll; otherwise, <c>false</c>.</value>
        /// <returns>true if scrolling is possible; otherwise, false. This property has no default value.</returns>
        public bool CanHorizontallyScroll { get; set; }

        /// <summary>
        /// Gets the horizontal size of the extent.
        /// </summary>
        /// <value>The width of the extent.</value>
        /// <returns>A <see cref="T:System.Double" /> that represents, in device independent pixels, the horizontal size of the extent. This property has no default value.</returns>
        public double ExtentWidth
        {
            get { return _extentWidth; }
            private set
            {
                _extentWidth = value;
                InvalidateScrollInfo();
            }
        }

        /// <summary>
        /// Gets the vertical size of the extent.
        /// </summary>
        /// <value>The height of the extent.</value>
        /// <returns>A <see cref="T:System.Double" /> that represents, in device independent pixels, the vertical size of the extent.This property has no default value.</returns>
        public double ExtentHeight
        {
            get { return _extentHeight; }
            private set
            {
                _extentHeight = value;
                InvalidateScrollInfo();
            }
        }

        /// <summary>
        /// Gets the horizontal size of the viewport for this content.
        /// </summary>
        /// <value>The width of the viewport.</value>
        /// <returns>A <see cref="T:System.Double" /> that represents, in device independent pixels, the horizontal size of the viewport for this content. This property has no default value.</returns>
        public double ViewportWidth { get; private set; }

        /// <summary>
        /// Gets the vertical size of the viewport for this content.
        /// </summary>
        /// <value>The height of the viewport.</value>
        /// <returns>A <see cref="T:System.Double" /> that represents, in device independent pixels, the vertical size of the viewport for this content. This property has no default value.</returns>
        public double ViewportHeight { get; private set; }

        /// <summary>
        /// Gets the horizontal offset.
        /// </summary>
        /// <value>The horizontal offset.</value>
        public double HorizontalOffset
        {
            get { return _horizontalOffset; }
            private set
            {
                if (Math.Abs(_horizontalOffset - value) < .9) return;
                _horizontalOffset = value;
                InvalidateMeasure();
                InvalidateArrange();
            }
        }

        /// <summary>
        /// Gets the vertical offset of the scrolled content.
        /// </summary>
        /// <value>The vertical offset.</value>
        /// <returns>A <see cref="T:System.Double" /> that represents, in device independent pixels, the vertical offset of the scrolled content. Valid values are between zero and the <see cref="P:System.Windows.Controls.Primitives.IScrollInfo.ExtentHeight" /> minus the <see cref="P:System.Windows.Controls.Primitives.IScrollInfo.ViewportHeight" />. This property has no default value.</returns>
        public double VerticalOffset
        {
            get { return _verticalOffset; }
            private set
            {
                if (Math.Abs(_verticalOffset - value) < .9) return;
                _verticalOffset = value;
                InvalidateMeasure();
                InvalidateArrange();
            }
        }

        /// <summary>
        /// Gets or sets a <see cref="T:System.Windows.Controls.ScrollViewer" /> element that controls scrolling behavior.
        /// </summary>
        /// <value>The scroll owner.</value>
        /// <returns>A <see cref="T:System.Windows.Controls.ScrollViewer" /> element that controls scrolling behavior. This property has no default value.</returns>
        public ScrollViewer ScrollOwner { get; set; }

        /// <summary>
        /// Invalidates the scroll information for the scroll owner (if a scroll owner is set).
        /// </summary>
        private void InvalidateScrollInfo()
        {
            if (ScrollOwner != null) 
                ScrollOwner.InvalidateScrollInfo();
        }

        /// <summary>
        /// Scrolls the current display to the specified item index
        /// </summary>
        /// <param name="itemIndex">Index of the item.</param>
        private void ScrollToItem(int itemIndex)
        {
            try
            {
                if (itemIndex < 0) itemIndex = 0;
                var maxFirstItem = Children.Count - _currentPageSize - 1;
                if (itemIndex > maxFirstItem) itemIndex = maxFirstItem;

                if (_childItems == null) PopulateInternalChildrenCollection();
                if (_childItems == null) return;

                var verticalOffset = 0d;
                for (var counter = 0; counter < itemIndex; counter++)
                    verticalOffset += _childItems[counter].ClientArea.Height;

                _verticalOffset = verticalOffset;
                InvalidateMeasure();
                InvalidateArrange();
            }
            catch
            {
                // Bummer, but nothing we can do
            }
        }
    }
}
