using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace CODE.Framework.Wpf.Controls
{
    /// <summary>
    /// Generic header control usable with Lists to create a data grid-style header. Optimized for lists with a large number of coluns
    /// </summary>
    /// <remarks>This control is very similar in purpose to the ListBoxGridHeader class. However, it trades flexibility and feature-set for performance.
    /// This control can render a large list of columns very quickly, but it doesn't support all the features of the ListBoxGridHeader class, such
    /// as cell templates.</remarks>
    public class ListBoxFastGridHeader : Panel
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ListBoxFastGridHeader"/> class.
        /// </summary>
        public ListBoxFastGridHeader()
        {
            Visibility = Visibility.Visible;
            ClipToBounds = true;
        }

        /// <summary>Generic column definition</summary>
        public ListColumnsCollection Columns
        {
            get { return (ListColumnsCollection)GetValue(ColumnsProperty); }
            set { SetValue(ColumnsProperty, value); }
        }

        /// <summary>Generic column definition</summary>
        public static readonly DependencyProperty ColumnsProperty = DependencyProperty.Register("Columns", typeof(ListColumnsCollection), typeof(ListBoxFastGridHeader), new UIPropertyMetadata(null, OnColumnsChanged));

        /// <summary>Called when columns change.</summary>
        /// <param name="o">The object the columns changed on.</param>
        /// <param name="args">The <see cref="System.Windows.DependencyPropertyChangedEventArgs"/> instance containing the event data.</param>
        private static void OnColumnsChanged(DependencyObject o, DependencyPropertyChangedEventArgs args)
        {
            var header = o as ListBoxFastGridHeader;
            if (header == null) return;
            header.InvalidateAll();
            var columns = args.NewValue as ListColumnsCollection;
            if (columns == null) return;
            columns.CollectionChanged += (o2, a) =>
            {
                header.InvalidateAll();
                var newColumns = new List<ListColumn>();
                foreach (var item in a.NewItems)
                {
                    var newColumn = item as ListColumn;
                    if (newColumn != null) newColumns.Add(newColumn);
                }
                header.HandleRequiredColumnEvents(newColumns);
            };
            header.HandleRequiredColumnEvents(columns);
        }

        private void HandleRequiredColumnEvents(IEnumerable<ListColumn> columns)
        {
            foreach (var column in columns)
            {
                column.VisibilityChanged += (s, e) => InvalidateAll();
                column.WidthChanged += (s, e) => InvalidateAll();
            }
        }

        /// <summary>Horizontal offset of the header</summary>
        public double HorizontalHeaderOffset
        {
            get { return (double)GetValue(HorizontalHeaderOffsetProperty); }
            set { SetValue(HorizontalHeaderOffsetProperty, value); }
        }

        /// <summary>Horizontal offset of the header</summary>
        public static readonly DependencyProperty HorizontalHeaderOffsetProperty = DependencyProperty.Register("HorizontalHeaderOffset", typeof(double), typeof(ListBoxFastGridHeader), new UIPropertyMetadata(0d, HorizontalHeaderOffsetChanged));

        /// <summary>Horizontals the header offset changed.</summary>
        /// <param name="o">The object the property was changed on.</param>
        /// <param name="args">The <see cref="System.Windows.DependencyPropertyChangedEventArgs"/> instance containing the event data.</param>
        private static void HorizontalHeaderOffsetChanged(DependencyObject o, DependencyPropertyChangedEventArgs args)
        {
            var header = o as ListBoxFastGridHeader;
            if (header == null) return;
            header.InvalidateAll();
        }

        /// <summary>Foreground brush (for text rendering)</summary>
        /// <value>The foreground.</value>
        public Brush Foreground
        {
            get { return (Brush)GetValue(ForegroundProperty); }
            set { SetValue(ForegroundProperty, value); }
        }

        /// <summary>Foreground brush (for text rendering)</summary>
        public static readonly DependencyProperty ForegroundProperty = DependencyProperty.Register("Foreground", typeof (Brush), typeof (ListBoxFastGridHeader), new PropertyMetadata(Brushes.Black, TriggerInvalidateAll));

        /// <summary> Font family used to render the header text</summary>
        public FontFamily FontFamily
        {
            get { return (FontFamily)GetValue(FontFamilyProperty); }
            set { SetValue(FontFamilyProperty, value); }
        }

        /// <summary> Font family used to render the header text</summary>
        public static readonly DependencyProperty FontFamilyProperty = DependencyProperty.Register("FontFamily", typeof(FontFamily), typeof(ListBoxFastGridHeader), new PropertyMetadata(new FontFamily("Segoe UI"), TriggerInvalidateAll));

        /// <summary>Font size used to render the header font elements</summary>
        public double FontSize
        {
            get { return (double)GetValue(FontSizeProperty); }
            set { SetValue(FontSizeProperty, value); }
        }
        /// <summary>Font size used to render the header font elements</summary>
        public static readonly DependencyProperty FontSizeProperty = DependencyProperty.Register("FontSize", typeof(double), typeof(ListBoxFastGridHeader), new PropertyMetadata(12d, TriggerInvalidateAll));

        /// <summary>Font style used to render the header font elements</summary>
        public FontStyle FontStyle
        {
            get { return (FontStyle)GetValue(FontStyleProperty); }
            set { SetValue(FontStyleProperty, value); }
        }
        /// <summary>Font style used to render the header font elements</summary>
        public static readonly DependencyProperty FontStyleProperty = DependencyProperty.Register("FontStyle", typeof(FontStyle), typeof(ListBoxFastGridHeader), new PropertyMetadata(FontStyles.Normal, TriggerInvalidateAll));

        /// <summary>Font weight used to render the header font elements</summary>
        public FontWeight FontWeight
        {
            get { return (FontWeight)GetValue(FontWeightProperty); }
            set { SetValue(FontWeightProperty, value); }
        }
        /// <summary>Font weight used to render the header font elements</summary>
        public static readonly DependencyProperty FontWeightProperty = DependencyProperty.Register("FontWeight", typeof(FontWeight), typeof(ListBoxFastGridHeader), new PropertyMetadata(FontWeights.Normal, TriggerInvalidateAll));

        /// <summary>Brush used as the basis for the grid line pen</summary>
        public Brush GridLineColor
        {
            get { return (Brush)GetValue(GridLineColorProperty); }
            set { SetValue(GridLineColorProperty, value); }
        }
        /// <summary>Brush used as the basis for the grid line pen</summary>
        public static readonly DependencyProperty GridLineColorProperty = DependencyProperty.Register("GridLineColor", typeof(Brush), typeof(ListBoxFastGridHeader), new PropertyMetadata(Brushes.LightGray, OnGridLineColorChanged));

        /// <summary>
        /// Fires when the grid line color property changes
        /// </summary>
        /// <param name="d">Object the color was changed on</param>
        /// <param name="args">Event arguments</param>
        private static void OnGridLineColorChanged(DependencyObject d, DependencyPropertyChangedEventArgs args)
        {
            var header = d as ListBoxFastGridHeader;
            if (header == null) return;
            header._gridLinePen = null;
            header.InvalidateAll();
        }

        /// <summary>
        /// Triggers invalidation of all visuals and layouts
        /// </summary>
        public void InvalidateAll()
        {
            InvalidateMeasure();
            InvalidateArrange();
            InvalidateVisual();
        }

        private static void TriggerInvalidateAll(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var header = d as ListBoxFastGridHeader;
            if (header == null) return;
            header.InvalidateAll();
        }

        private readonly List<ResizeAreaWrapper> _resizeAreas = new List<ResizeAreaWrapper>(); 

        /// <summary>
        /// Draws the content of a <see cref="T:System.Windows.Media.DrawingContext" /> object during the render pass of a <see cref="T:System.Windows.Controls.Panel" /> element.
        /// </summary>
        /// <param name="dc">The <see cref="T:System.Windows.Media.DrawingContext" /> object to draw.</param>
        protected override void OnRender(DrawingContext dc)
        {
            _resizeAreas.Clear();
            var currentLeft = 0d; // We always start at position 0 for frozen columns, regardless of the current horizontal scroll offset

            var headerHeight = GetHeaderHeight();

            RenderStandardHeaderElements(dc, headerHeight);

            // First, we render all headers that are considered "frozen"
            foreach (var column in Columns.Where(c => c.IsFrozen && c.Visibility == Visibility.Visible))
            {
                double columnWidth;
                if (column.Width.IsAbsolute)
                    columnWidth = column.Width.Value;
                else
                    // TODO: This should be handled better if we want to support * settings at some point
                    columnWidth = 100d;
                RenderColumnHeader(dc, column, new Rect(currentLeft, 0d, columnWidth, headerHeight));
                currentLeft += columnWidth;
                if (currentLeft > ActualWidth) return; // There is no point in rendering further
            }

            currentLeft += (HorizontalHeaderOffset*-1); // For all other columns, we now also consider the horizontal offset due to scrolling

            // Then, we render all "unfrozen" headers
            foreach (var column in Columns.Where(c => !c.IsFrozen && c.Visibility == Visibility.Visible))
            {
                double columnWidth;
                if (column.Width.IsAbsolute)
                    columnWidth = column.Width.Value;
                else
                    // TODO: This should be handled better if we want to support * settings at some point
                    columnWidth = 100d;
                RenderColumnHeader(dc, column, new Rect(currentLeft, 0d, columnWidth, headerHeight));
                currentLeft += columnWidth;
                if (currentLeft > ActualWidth) return; // There is no point in rendering further
            }
        }

        /// <summary>
        /// Renders the standard header elements such as border and background.
        /// </summary>
        /// <param name="dc">The dc.</param>
        /// <param name="headerHeight">Height of the header.</param>
        protected virtual void RenderStandardHeaderElements(DrawingContext dc, double headerHeight)
        {
            dc.DrawLine(GetGridLinePen(), new Point(0d, headerHeight - .5d), new Point(ActualWidth, headerHeight - .5d));
        }

        /// <summary>
        /// Returns the desired height for the entire header
        /// </summary>
        /// <returns>double</returns>
        protected virtual double GetHeaderHeight()
        {
            var ft = new FormattedText("X", CultureInfo.CurrentUICulture, FlowDirection.LeftToRight, new Typeface(FontFamily, FontStyle, FontWeight, FontStretches.Normal), FontSize, Foreground);
            return ft.Height + 7d;
        }

        /// <summary>
        /// Renders the header for an individual column
        /// </summary>
        /// <param name="dc">Drawing context</param>
        /// <param name="column">Column</param>
        /// <param name="columnClientRect">The suggested area for the header</param>
        protected virtual void RenderColumnHeader(DrawingContext dc, ListColumn column, Rect columnClientRect)
        {
            dc.DrawLine(GetGridLinePen(), new Point(columnClientRect.Right - .5d, 0d), new Point(columnClientRect.Right - .5d, ActualHeight));

            if (column.IsResizable)
                _resizeAreas.Add(new ResizeAreaWrapper {Column = column, HotArea = new Rect(columnClientRect.Right - 2, columnClientRect.Top, 5, columnClientRect.Height)});

            var header = column.Header.ToString();
            if (!string.IsNullOrEmpty(header) && columnClientRect.Width > 8)
            {
                var ft = new FormattedText(header, CultureInfo.CurrentUICulture, FlowDirection.LeftToRight, new Typeface(FontFamily, FontStyle, FontWeight, FontStretches.Normal), FontSize, Foreground) {TextAlignment = column.HeaderTextAlignment, MaxLineCount = 1, MaxTextWidth = columnClientRect.Width - 6, Trimming = TextTrimming.CharacterEllipsis};
                var topOffset = Math.Max((double)(int)(columnClientRect.Height - ft.Height)/2, 0d);
                dc.DrawText(ft, new Point(columnClientRect.X + 3, columnClientRect.Y + topOffset));
            }
        }

        private Pen _gridLinePen;

        /// <summary>
        /// Returns the pen used for grid line rendering in the header
        /// </summary>
        /// <returns>Pen</returns>
        protected virtual Pen GetGridLinePen()
        {
            return _gridLinePen ?? (_gridLinePen = new Pen(GridLineColor, 1d));
        }

        private ListColumn _resizingColumn;
        private double _originalResizeX = -1d;
        private double _originalResizeWidth = -1d;

        /// <summary>
        /// Invoked when an unhandled MouseMove attached event reaches an element in its route that is derived from this class. Implement this method to add class handling for this event.
        /// </summary>
        /// <param name="e">The <see cref="T:System.Windows.Input.MouseEventArgs" /> that contains the event data.</param>
        protected override void OnMouseMove(MouseEventArgs e)
        {
            var position = e.GetPosition(this);

            if (_resizingColumn != null)
            {
                var deltaX = _originalResizeX - position.X;
                var newWidth = Math.Max(_originalResizeWidth - deltaX, 2d);
                _resizingColumn.Width = new GridLength(newWidth);
                e.Handled = true;
                return;
            }

            foreach (var resizeArea in _resizeAreas)
                if (resizeArea.HotArea.Contains(position))
                {
                    Mouse.SetCursor(Cursors.SizeWE);
                    e.Handled = true;
                    return;
                }

            base.OnMouseMove(e);
        }

        /// <summary>
        /// Invoked when an unhandled MouseDown attached event reaches an element in its route that is derived from this class. Implement this method to add class handling for this event.
        /// </summary>
        /// <param name="e">The <see cref="T:System.Windows.Input.MouseButtonEventArgs" /> that contains the event data. This event data reports details about the mouse button that was pressed and the handled state.</param>
        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            var position = e.GetPosition(this);

            foreach (var resizeArea in _resizeAreas)
                if (resizeArea.HotArea.Contains(position))
                {
                    Mouse.Capture(this);
                    _resizingColumn = resizeArea.Column;
                    _originalResizeWidth = _resizingColumn.Width.Value;
                    _originalResizeX = position.X;
                    e.Handled = true;
                    return;
                }

            base.OnMouseDown(e);
        }

        /// <summary>
        /// Invoked when an unhandled MouseUp routed event reaches an element in its route that is derived from this class. Implement this method to add class handling for this event.
        /// </summary>
        /// <param name="e">The <see cref="T:System.Windows.Input.MouseButtonEventArgs" /> that contains the event data. The event data reports that the mouse button was released.</param>
        protected override void OnMouseUp(MouseButtonEventArgs e)
        {
            if (_resizingColumn != null)
            {
                _resizingColumn = null;
                Mouse.Capture(null);
                e.Handled = true;
                return;
            }

            base.OnMouseUp(e);
        }

        /// <summary>
        /// When overridden in a derived class, measures the size in layout required for child elements and determines a size for the <see cref="T:System.Windows.FrameworkElement" />-derived class.
        /// </summary>
        /// <param name="availableSize">The available size that this element can give to child elements. Infinity can be specified as a value to indicate that the element will size to whatever content is available.</param>
        /// <returns>The size that this element determines it needs during layout, based on its calculations of child element sizes.</returns>
        protected override Size MeasureOverride(Size availableSize)
        {
            var width = Columns.Where(c => c.Visibility == Visibility.Visible).Sum(c => c.Width.Value);
            var height = GetHeaderHeight();
            base.MeasureOverride(availableSize);
            return new Size(width, height);
        }
    }

    /// <summary>For internal use only</summary>
    public class ResizeAreaWrapper
    {
        /// <summary>For internal use only</summary>
        public Rect HotArea { get; set; }
        /// <summary>For internal use only</summary>
        public ListColumn Column { get; set; }
    }
}
