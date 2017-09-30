using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace CODE.Framework.Wpf.Layout
{
    /// <summary>
    /// Special items presenter class that can natively scroll its own content
    /// </summary>
    public class ScrollAwareItemsPresenter : ItemsPresenter, IScrollInfo
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ScrollAwareItemsPresenter"/> class.
        /// </summary>
        public ScrollAwareItemsPresenter()
        {
            CanHorizontallyScroll = true;
            CanVerticallyScroll = false;

            Loaded += (s, e) =>
            {
                if (ScrollOwner != null)
                {
                    ScrollOwner.InvalidateArrange();
                    ScrollOwner.InvalidateMeasure();
                    ScrollOwner.InvalidateScrollInfo();
                }
            };
        }

        private UIElement _childElement;
        private IScrollInfo _childScrollInfo;

        /// <summary>
        /// Overrides the base class implementation of <see cref="M:System.Windows.FrameworkElement.MeasureOverride(System.Windows.Size)" /> to measure the size of the <see cref="T:System.Windows.Controls.ItemsPresenter" /> object and return proper sizes to the layout engine.
        /// </summary>
        /// <param name="constraint">Constraint size is an "upper limit." The return value should not exceed this size.</param>
        /// <returns>The desired size.</returns>
        protected override Size MeasureOverride(Size constraint)
        {
            if (_childElement == null) _childElement = (VisualTreeHelper.GetChildrenCount(this) > 0) ? VisualTreeHelper.GetChild(this, 0) as UIElement : null;
            if (_childElement == null) return base.MeasureOverride(constraint);
            _childElement.Measure(constraint);

            if (_childScrollInfo == null)
            {
                _childScrollInfo = _childElement as IScrollInfo;
                if (_childScrollInfo != null && _childScrollInfo.ScrollOwner == null && _scrollOwner != null)
                {
                    _childScrollInfo.ScrollOwner = _scrollOwner;
                    _scrollOwner.InvalidateScrollInfo();
                }
            }

            return _childElement.DesiredSize;
        }

        /// <summary>
        /// Scrolls up within content by one logical unit.
        /// </summary>
        public void LineUp()
        {
            if (_childScrollInfo == null) return;
            _childScrollInfo.LineUp();
        }

        /// <summary>
        /// Scrolls down within content by one logical unit.
        /// </summary>
        public void LineDown()
        {
            if (_childScrollInfo == null) return;
            _childScrollInfo.LineDown();
        }

        /// <summary>
        /// Scrolls left within content by one logical unit.
        /// </summary>
        public void LineLeft()
        {
            if (_childScrollInfo == null) return;
            _childScrollInfo.LineLeft();
        }

        /// <summary>
        /// Scrolls right within content by one logical unit.
        /// </summary>
        public void LineRight()
        {
            if (_childScrollInfo == null) return;
            _childScrollInfo.LineRight();
        }

        /// <summary>
        /// Scrolls up within content by one page.
        /// </summary>
        public void PageUp()
        {
            if (_childScrollInfo == null) return;
            _childScrollInfo.PageUp();
        }

        /// <summary>
        /// Scrolls down within content by one page.
        /// </summary>
        public void PageDown()
        {
            if (_childScrollInfo == null) return;
            _childScrollInfo.LineDown();
        }

        /// <summary>
        /// Scrolls left within content by one page.
        /// </summary>
        public void PageLeft()
        {
            if (_childScrollInfo == null) return;
            _childScrollInfo.LineLeft();
        }

        /// <summary>
        /// Scrolls right within content by one page.
        /// </summary>
        public void PageRight()
        {
            if (_childScrollInfo == null) return;
            _childScrollInfo.LineRight();
        }

        /// <summary>
        /// Scrolls up within content after a user clicks the wheel button on a mouse.
        /// </summary>
        public void MouseWheelUp()
        {
            if (_childScrollInfo == null) return;
            _childScrollInfo.MouseWheelUp();
        }

        /// <summary>
        /// Scrolls down within content after a user clicks the wheel button on a mouse.
        /// </summary>
        public void MouseWheelDown()
        {
            if (_childScrollInfo == null) return;
            _childScrollInfo.MouseWheelDown();
        }

        /// <summary>
        /// Scrolls left within content after a user clicks the wheel button on a mouse.
        /// </summary>
        public void MouseWheelLeft()
        {
            if (_childScrollInfo == null) return;
            _childScrollInfo.MouseWheelLeft();
        }

        /// <summary>
        /// Scrolls right within content after a user clicks the wheel button on a mouse.
        /// </summary>
        public void MouseWheelRight()
        {
            if (_childScrollInfo == null) return;
            _childScrollInfo.MouseWheelRight();
        }

        /// <summary>
        /// Sets the amount of horizontal offset.
        /// </summary>
        /// <param name="offset">The degree to which content is horizontally offset from the containing viewport.</param>
        public void SetHorizontalOffset(double offset)
        {
            if (_childScrollInfo == null) return;
            _childScrollInfo.SetHorizontalOffset(offset);
        }

        /// <summary>
        /// Sets the amount of vertical offset.
        /// </summary>
        /// <param name="offset">The degree to which content is vertically offset from the containing viewport.</param>
        public void SetVerticalOffset(double offset)
        {
            if (_childScrollInfo == null) return;
            _childScrollInfo.SetVerticalOffset(offset);
        }

        /// <summary>
        /// Forces content to scroll until the coordinate space of a <see cref="T:System.Windows.Media.Visual" /> object is visible.
        /// </summary>
        /// <param name="visual">A <see cref="T:System.Windows.Media.Visual" /> that becomes visible.</param>
        /// <param name="rectangle">A bounding rectangle that identifies the coordinate space to make visible.</param>
        /// <returns>A <see cref="T:System.Windows.Rect" /> that is visible.</returns>
        public Rect MakeVisible(Visual visual, Rect rectangle)
        {
            if (_childScrollInfo == null) return Rect.Empty;
            return _childScrollInfo.MakeVisible(visual, rectangle);
        }

        /// <summary>
        /// Gets or sets a value that indicates whether scrolling on the vertical axis is possible.
        /// </summary>
        /// <value><c>true</c> if this instance can vertically scroll; otherwise, <c>false</c>.</value>
        public bool CanVerticallyScroll
        {
            get
            {
                if (_childScrollInfo == null) return false;
                return _childScrollInfo.CanVerticallyScroll;
            }
            set
            {
                if (_childScrollInfo == null) return;
                _childScrollInfo.CanVerticallyScroll = value;
            }
        }

        /// <summary>
        /// Gets or sets a value that indicates whether scrolling on the horizontal axis is possible.
        /// </summary>
        /// <value><c>true</c> if this instance can horizontally scroll; otherwise, <c>false</c>.</value>
        public bool CanHorizontallyScroll
        {
            get
            {
                if (_childScrollInfo == null) return false;
                return _childScrollInfo.CanHorizontallyScroll;
            }
            set
            {
                if (_childScrollInfo == null) return;
                _childScrollInfo.CanHorizontallyScroll = value;
            }
        }

        /// <summary>
        /// Gets the horizontal size of the extent.
        /// </summary>
        /// <value>The width of the extent.</value>
        public double ExtentWidth
        {
            get
            {
                if (_childScrollInfo == null) return 0d;
                return _childScrollInfo.ExtentWidth;
            }
        }

        /// <summary>
        /// Gets the vertical size of the extent.
        /// </summary>
        /// <value>The height of the extent.</value>
        public double ExtentHeight
        {
            get
            {
                if (_childScrollInfo == null) return 0d;
                return _childScrollInfo.ExtentHeight;
            }
        }

        /// <summary>
        /// Gets the horizontal size of the viewport for this content.
        /// </summary>
        /// <value>The width of the viewport.</value>
        public double ViewportWidth
        {
            get
            {
                if (_childScrollInfo == null) return 0d;
                return _childScrollInfo.ViewportWidth;
            }
        }

        /// <summary>
        /// Gets the vertical size of the viewport for this content.
        /// </summary>
        /// <value>The height of the viewport.</value>
        public double ViewportHeight
        {
            get
            {
                if (_childScrollInfo == null) return 0d;
                return _childScrollInfo.ViewportHeight;
            }
        }

        /// <summary>
        /// Gets the horizontal offset of the scrolled content.
        /// </summary>
        /// <value>The horizontal offset.</value>
        public double HorizontalOffset
        {
            get
            {
                if (_childScrollInfo == null) return 0d;
                return _childScrollInfo.HorizontalOffset;
            }
        }

        /// <summary>
        /// Gets the vertical offset of the scrolled content.
        /// </summary>
        /// <value>The vertical offset.</value>
        public double VerticalOffset
        {
            get
            {
                if (_childScrollInfo == null) return 0d;
                return _childScrollInfo.VerticalOffset;
            }
        }

        private ScrollViewer _scrollOwner;
        /// <summary>
        /// Gets or sets a <see cref="T:System.Windows.Controls.ScrollViewer" /> element that controls scrolling behavior.
        /// </summary>
        /// <value>The scroll owner.</value>
        public ScrollViewer ScrollOwner
        {
            get
            {
                if (_childScrollInfo == null) return null;
                return _childScrollInfo.ScrollOwner;
            }
            set
            {
                _scrollOwner = value;
                InvalidateArrange();
                InvalidateMeasure();
                value.InvalidateArrange();
                value.InvalidateMeasure();
                value.InvalidateScrollInfo();
                if (_childScrollInfo == null) return;
                _childScrollInfo.ScrollOwner = value;
            }
        }
    }

}
