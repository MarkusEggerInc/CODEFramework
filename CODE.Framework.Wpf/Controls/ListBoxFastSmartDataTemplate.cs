using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using CODE.Framework.Core.Utilities;
using CODE.Framework.Core.Utilities.Extensions;

namespace CODE.Framework.Wpf.Controls
{
    /// <summary>
    /// Multi-column ListBox data template designed to render fast (although with a limited feature set)
    /// </summary>
    public class ListBoxFastSmartDataTemplate : Panel, IScrollInfo, IScrollMeasure
    {
        private double _horizontalOffset;
        private double _verticalOffset;

        /// <summary>
        /// Initializes a new instance of the <see cref="ListBoxFastSmartDataTemplate"/> class.
        /// </summary>
        public ListBoxFastSmartDataTemplate()
        {
            CanVerticallyScroll = false;
            CanHorizontallyScroll = true;

            Loaded += (s, a) =>
            {
                var item = FindAncestor<ListBoxItem>(this);
                if (item == null)
                {
                    if (!string.IsNullOrEmpty(_defaultColumnsToSet))
                        PopulateColumnsFromDefaults(this, null);
                }

                _listBox = ItemsControl.ItemsControlFromItemContainer(item);
                if (_listBox == null) return;

                var columns = ListEx.GetColumns(_listBox);
                if (columns != null)
                    Columns = columns;
                else
                    PopulateColumnsFromDefaults(this, _listBox);

                _listBox.DataContextChanged += (s2, a2) =>
                {
                    var columns2 = ListEx.GetColumns(_listBox);
                    if (columns2 != null)
                        Columns = columns2;
                };
            };
        }

        private string _defaultColumnsToSet = string.Empty;

        private static void PopulateColumnsFromDefaults(ListBoxFastSmartDataTemplate item, ItemsControl listBox)
        {
            var columns = new ListColumnsCollection();

            var dataSources = item._defaultColumnsToSet.Split(',');
            foreach (var dataSource in dataSources)
            {
                var path = dataSource.Trim();

                var width = 200;
                if (path.StartsWith("Image")) width = 30;
                else if (path.StartsWith("Logo")) width = 30;
                else if (path.StartsWith("Number")) width = 75;

                var header = string.Empty;
                if (!path.StartsWith("Image") && !path.StartsWith("Logo"))
                    header = path.SpaceCamelCase();

                columns.Add(new ListColumn
                {
                    BindingPath = path,
                    Width = new GridLength(width),
                    Header = header,
                    IsResizable = !(path.StartsWith("Image") || path.StartsWith("Logo"))
                });
            }

            if (listBox != null)
                ListEx.SetColumns(listBox, columns);
            else
                item.Columns = columns;

            item._defaultColumnsToSet = string.Empty;
        }

        /// <summary>Walks the visual tree to find the parent of a certain type</summary>
        /// <typeparam name="T">Type to search</typeparam>
        /// <param name="d">Object for which to find the ancestor</param>
        /// <returns>Object or null</returns>
        private static T FindAncestor<T>(DependencyObject d) where T : class
        {
            var parent = VisualTreeHelper.GetParent(d);
            if (parent == null) return null;
            if (parent is T) return parent as T;
            return FindAncestor<T>(parent);
        }

        /// <summary>Generic column definition</summary>
        public ListColumnsCollection Columns
        {
            get { return (ListColumnsCollection)GetValue(ColumnsProperty); }
            set { SetValue(ColumnsProperty, value); }
        }

        /// <summary>Generic column definition</summary>
        public static readonly DependencyProperty ColumnsProperty = DependencyProperty.Register("Columns", typeof(ListColumnsCollection), typeof(ListBoxFastSmartDataTemplate), new UIPropertyMetadata(null, OnColumnsChanged));

        private ItemsControl _listBox;

        /// <summary>Called when columns change.</summary>
        /// <param name="o">The object the columns changed on.</param>
        /// <param name="args">The <see cref="System.Windows.DependencyPropertyChangedEventArgs"/> instance containing the event data.</param>
        private static void OnColumnsChanged(DependencyObject o, DependencyPropertyChangedEventArgs args)
        {
            var item = o as ListBoxFastSmartDataTemplate;
            if (item == null) return;
            var columns = args.NewValue as ListColumnsCollection;
            if (columns == null)
            {
                item.InvalidateVisual();
                return;
            }
            columns.CollectionChanged += (o2, a) =>
            {
                item.InvalidateVisual();
                foreach (var column in columns)
                {
                    column.WidthChanged += (s, e) => item.InvalidateAll();
                    column.VisibilityChanged += (s2, e2) => item.InvalidateAll();
                }
            };
            foreach (var column in columns)
            {
                column.WidthChanged += (s, e) => item.InvalidateAll();
                column.VisibilityChanged += (s2, e2) => item.InvalidateAll();
            }
            item.InvalidateVisual();
        }

        /// <summary>
        /// Triggers invalidation of everything
        /// </summary>
        public void InvalidateAll()
        {
            InvalidateMeasure();
            InvalidateArrange();
            InvalidateVisual();

            if (ScrollOwner != null)
            {
                ScrollOwner.InvalidateScrollInfo();
                ScrollOwner.InvalidateMeasure();
                ScrollOwner.InvalidateArrange();
            }
        }

        /// <summary>
        /// Draws the content of a <see cref="T:System.Windows.Media.DrawingContext" /> object during the render pass of a <see cref="T:System.Windows.Controls.Panel" /> element.
        /// </summary>
        /// <param name="dc">The <see cref="T:System.Windows.Media.DrawingContext" /> object to draw.</param>
        protected override void OnRender(DrawingContext dc)
        {
            if (Columns == null) return;

            var typeface = new Typeface(new FontFamily("Segoe UI"), FontStyles.Normal, FontWeights.Normal, FontStretches.Normal);
            var currentLeft = HorizontalOffset*-1;
            var height = Math.Max(ActualHeight, 30);
            foreach (var column in Columns.Where(c => c.Visibility == Visibility.Visible))
            {
                if (!string.IsNullOrEmpty(column.BindingPath) && DataContext != null)
                {
                    var currentValue = DataContext.GetPropertyValue<object>(column.BindingPath).ToString();
                    var ft = new FormattedText(currentValue, CultureInfo.CurrentUICulture, FlowDirection.LeftToRight, typeface, 12d, Brushes.Black) {MaxTextWidth = column.Width.Value, MaxTextHeight = height};
                    dc.DrawText(ft, new Point(currentLeft, 0d));
                }
                currentLeft += column.Width.Value;
            }
        }

        /// <summary>
        /// Scrolls up within content by one logical unit.
        /// </summary>
        /// <exception cref="System.NotImplementedException"></exception>
        public void LineUp()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Scrolls down within content by one logical unit.
        /// </summary>
        /// <exception cref="System.NotImplementedException"></exception>
        public void LineDown()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Scrolls left within content by one logical unit.
        /// </summary>
        /// <exception cref="System.NotImplementedException"></exception>
        public void LineLeft()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Scrolls right within content by one logical unit.
        /// </summary>
        /// <exception cref="System.NotImplementedException"></exception>
        public void LineRight()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Scrolls up within content by one page.
        /// </summary>
        /// <exception cref="System.NotImplementedException"></exception>
        public void PageUp()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Scrolls down within content by one page.
        /// </summary>
        /// <exception cref="System.NotImplementedException"></exception>
        public void PageDown()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Scrolls left within content by one page.
        /// </summary>
        /// <exception cref="System.NotImplementedException"></exception>
        public void PageLeft()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Scrolls right within content by one page.
        /// </summary>
        /// <exception cref="System.NotImplementedException"></exception>
        public void PageRight()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Scrolls up within content after a user clicks the wheel button on a mouse.
        /// </summary>
        /// <exception cref="System.NotImplementedException"></exception>
        public void MouseWheelUp()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Scrolls down within content after a user clicks the wheel button on a mouse.
        /// </summary>
        /// <exception cref="System.NotImplementedException"></exception>
        public void MouseWheelDown()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Scrolls left within content after a user clicks the wheel button on a mouse.
        /// </summary>
        /// <exception cref="System.NotImplementedException"></exception>
        public void MouseWheelLeft()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Scrolls right within content after a user clicks the wheel button on a mouse.
        /// </summary>
        /// <exception cref="System.NotImplementedException"></exception>
        public void MouseWheelRight()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Sets the amount of horizontal offset.
        /// </summary>
        /// <param name="offset">The degree to which content is horizontally offset from the containing viewport.</param>
        public void SetHorizontalOffset(double offset)
        {
            HorizontalOffset = offset;
        }

        /// <summary>
        /// Sets the amount of vertical offset.
        /// </summary>
        /// <param name="offset">The degree to which content is vertically offset from the containing viewport.</param>
        public void SetVerticalOffset(double offset)
        {
            VerticalOffset = offset;
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
            throw new NotImplementedException();
        }

        /// <summary>
        /// Gets or sets a value that indicates whether scrolling on the vertical axis is possible.
        /// </summary>
        /// <value><c>true</c> if this instance can vertically scroll; otherwise, <c>false</c>.</value>
        public bool CanVerticallyScroll { get; set; }
        /// <summary>
        /// Gets or sets a value that indicates whether scrolling on the horizontal axis is possible.
        /// </summary>
        /// <value><c>true</c> if this instance can horizontally scroll; otherwise, <c>false</c>.</value>
        public bool CanHorizontallyScroll { get; set; }
        /// <summary>
        /// Gets the horizontal size of the extent.
        /// </summary>
        /// <value>The width of the extent.</value>
        public double ExtentWidth { get; private set; }
        /// <summary>
        /// Gets the vertical size of the extent.
        /// </summary>
        /// <value>The height of the extent.</value>
        public double ExtentHeight { get; private set; }
        /// <summary>
        /// Gets the horizontal size of the viewport for this content.
        /// </summary>
        /// <value>The width of the viewport.</value>
        public double ViewportWidth { get; private set; }
        /// <summary>
        /// Gets the vertical size of the viewport for this content.
        /// </summary>
        /// <value>The height of the viewport.</value>
        public double ViewportHeight { get; private set; }

        /// <summary>
        /// Gets the horizontal offset of the scrolled content.
        /// </summary>
        /// <value>The horizontal offset.</value>
        public double HorizontalOffset
        {
            get { return _horizontalOffset; }
            private set
            {
                _horizontalOffset = value;
                InvalidateVisual();
            }
        }

        /// <summary>
        /// Gets the vertical offset of the scrolled content.
        /// </summary>
        /// <value>The vertical offset.</value>
        public double VerticalOffset
        {
            get { return _verticalOffset; }
            private set
            {
                _verticalOffset = value;
                InvalidateVisual();
            }
        }

        /// <summary>
        /// Gets or sets a <see cref="T:System.Windows.Controls.ScrollViewer" /> element that controls scrolling behavior.
        /// </summary>
        /// <value>The scroll owner.</value>
        public ScrollViewer ScrollOwner { get; set; }

        /// <summary>
        /// When overridden in a derived class, measures the size in layout required for child elements and determines a size for the <see cref="T:System.Windows.FrameworkElement" />-derived class.
        /// </summary>
        /// <param name="availableSize">The available size that this element can give to child elements. Infinity can be specified as a value to indicate that the element will size to whatever content is available.</param>
        /// <returns>The size that this element determines it needs during layout, based on its calculations of child element sizes.</returns>
        protected override Size MeasureOverride(Size availableSize)
        {
            if (Columns == null) return new Size(1,30);

            base.MeasureOverride(availableSize);

            var totalWidth = Columns.Where(c => c.Visibility == Visibility.Visible).Sum(column => column.Width.Value);

            CanHorizontallyScroll = totalWidth > availableSize.Width;
            if (!CanHorizontallyScroll && HorizontalOffset > 0) HorizontalOffset = 0;
            ViewportWidth = availableSize.Width;
            ExtentWidth = totalWidth;
            if (ScrollOwner != null)
                ScrollOwner.InvalidateScrollInfo();

            return new Size(totalWidth, 30);
        }

        /// <summary>
        /// Measures for scroll operations
        /// </summary>
        /// <param name="availableSize">Size of the available.</param>
        public void MeasureForScroll(Size availableSize)
        {
            if (Columns == null) return;

            var totalWidth = Columns.Where(c => c.Visibility == Visibility.Visible).Sum(column => column.Width.Value);

            CanHorizontallyScroll = totalWidth > availableSize.Width;
            if (!CanHorizontallyScroll && HorizontalOffset > 0) HorizontalOffset = 0;
            ViewportWidth = availableSize.Width;
            ExtentWidth = totalWidth;
        }
    }

    /// <summary>
    /// When a class implements this interface, it can force a re-measure operation specific to scroll settings
    /// </summary>
    public interface IScrollMeasure
    {
        /// <summary>
        /// Measures for scroll operations
        /// </summary>
        /// <param name="availableSize"></param>
        void MeasureForScroll(Size availableSize);
    }
}
