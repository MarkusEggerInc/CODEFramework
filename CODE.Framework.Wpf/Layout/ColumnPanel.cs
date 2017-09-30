using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using CODE.Framework.Wpf.Utilities;

namespace CODE.Framework.Wpf.Layout
{
    /// <summary>
    /// This automatic layout control can arrange elements in columns
    /// </summary>
    /// <remarks>
    /// This control can be seen as a super-simple Grid control that only supports columns and only 
    /// 0-margin and vertical and horizontal stretch alignments. The advantage of this control is performance.
    /// </remarks>
    public class ColumnPanel : Panel
    {
        /// <summary>
        /// Constructor
        /// </summary>
        public ColumnPanel()
        {
            HorizontalAlignment = HorizontalAlignment.Stretch;
            VerticalAlignment = VerticalAlignment.Stretch;
            ColumnDefinitions = new ColumnPanelColumnsCollection(this);
        }

        /// <summary>Column definitions</summary>
        public ColumnPanelColumnsCollection ColumnDefinitions
        {
            get { return (ColumnPanelColumnsCollection) GetValue(ColumnDefinitionsProperty); }
            set { SetValue(ColumnDefinitionsProperty, value); }
        }

        /// <summary>Column definitions</summary>
        public static readonly DependencyProperty ColumnDefinitionsProperty = DependencyProperty.Register("ColumnDefinitions", typeof (ColumnPanelColumnsCollection), typeof (ColumnPanel), new PropertyMetadata(null, OnColumnDefinitionsChanged));

        private static void OnColumnDefinitionsChanged(DependencyObject d, DependencyPropertyChangedEventArgs args)
        {
            var panel = d as ColumnPanel;
            if (panel == null) return;

            panel.InvalidateArrange();
            panel.InvalidateMeasure();

            panel.ColumnDefinitions.CollectionChanged += (s, e) =>
            {
                panel.InvalidateArrange();
                panel.InvalidateMeasure();

                if (e.NewItems != null)
                    foreach (var newItem in e.NewItems)
                    {
                        var newColumn = newItem as ColumnDefinition;
                        if (newColumn == null) continue;
                        var descriptor = DependencyPropertyDescriptor.FromProperty(ColumnDefinition.WidthProperty, typeof (ColumnDefinition));
                        if (descriptor == null) continue;
                        descriptor.AddValueChanged(newColumn, panel.ColumnDefinitionChangedEventHandler);
                    }

                if (e.Action == NotifyCollectionChangedAction.Remove || e.Action == NotifyCollectionChangedAction.Reset)
                    if (e.OldItems != null)
                        foreach (var oldItem in e.OldItems)
                        {
                            var oldColumn = oldItem as ColumnDefinition;
                            if (oldColumn == null) continue;
                            var descriptor = DependencyPropertyDescriptor.FromProperty(ColumnDefinition.WidthProperty, typeof (ColumnDefinition));
                            if (descriptor == null) continue;
                            descriptor.RemoveValueChanged(oldColumn, panel.ColumnDefinitionChangedEventHandler);
                        }
            };
        }

        private void ColumnDefinitionChangedEventHandler(object source, EventArgs e)
        {
            InvalidateArrange();
            InvalidateMeasure();
        }

        /// <summary>Column assignment</summary>
        public static int GetColumn(DependencyObject obj)
        {
            return (int) obj.GetValue(ColumnProperty);
        }

        /// <summary>Column assignment</summary>
        public static void SetColumn(DependencyObject obj, int value)
        {
            obj.SetValue(ColumnProperty, value);
        }

        /// <summary>Column assignment</summary>
        public static readonly DependencyProperty ColumnProperty = DependencyProperty.RegisterAttached("Column", typeof (int), typeof (ColumnPanel), new PropertyMetadata(0));

        /// <summary>Indicates elements for the detail area</summary>
        public static bool GetIsDetail(DependencyObject obj)
        {
            return (bool)obj.GetValue(IsDetailProperty);
        }

        /// <summary>Indicates elements for the detail area</summary>
        public static void SetIsDetail(DependencyObject obj, bool value)
        {
            obj.SetValue(IsDetailProperty, value);
        }

        /// <summary>Indicates elements for the detail area</summary>
        public static readonly DependencyProperty IsDetailProperty = DependencyProperty.RegisterAttached("IsDetail", typeof(bool), typeof(ColumnPanel), new PropertyMetadata(false));

        /// <summary>
        /// Indentation of the detail area
        /// </summary>
        /// <value>The detail area indent.</value>
        public double DetailAreaIndent
        {
            get { return (double)GetValue(DetailAreaIndentProperty); }
            set { SetValue(DetailAreaIndentProperty, value); }
        }
        /// <summary>
        /// Indentation of the detail area
        /// </summary>
        /// <value>The detail area indent.</value>
        public static readonly DependencyProperty DetailAreaIndentProperty = DependencyProperty.Register("DetailAreaIndent", typeof(double), typeof(ColumnPanel), new PropertyMetadata(0d));

        /// <summary>
        /// Width of the expand/collapse icon
        /// </summary>
        /// <value>The width of the expand icon.</value>
        public double ExpandIconWidth
        {
            get { return (double)GetValue(ExpandIconWidthProperty); }
            set { SetValue(ExpandIconWidthProperty, value); }
        }
        /// <summary>
        /// Width of the expand/collapse icon
        /// </summary>
        /// <value>The width of the expand icon.</value>
        public static readonly DependencyProperty ExpandIconWidthProperty = DependencyProperty.Register("ExpandIconWidth", typeof(double), typeof(ColumnPanel), new PropertyMetadata(20d));

        /// <summary>
        /// Indicates whether the detail area is expanded
        /// </summary>
        public bool DetailIsExpanded
        {
            get { return (bool)GetValue(DetailIsExpandedProperty); }
            set { SetValue(DetailIsExpandedProperty, value); }
        }
        /// <summary>
        /// Indicates whether the detail area is expanded
        /// </summary>
        public static readonly DependencyProperty DetailIsExpandedProperty = DependencyProperty.Register("DetailIsExpanded", typeof(bool), typeof(ColumnPanel), new PropertyMetadata(false, OnDetailIsExpandedChanged));

        /// <summary>
        /// Brush resource key used for the expand-icon (shown in collapsed state)
        /// </summary>
        /// <value>The expand icon brush resource key.</value>
        public string ExpandIconBrushResourceKey
        {
            get { return (string)GetValue(ExpandIconBrushResourceKeyProperty); }
            set { SetValue(ExpandIconBrushResourceKeyProperty, value); }
        }
        /// <summary>
        /// Brush resource key used for the expand-icon (shown in collapsed state)
        /// </summary>
        /// <value>The expand icon brush resource key.</value>
        public static readonly DependencyProperty ExpandIconBrushResourceKeyProperty = DependencyProperty.Register("ExpandIconBrushResourceKey", typeof(string), typeof(ColumnPanel), new PropertyMetadata("CODE.Framework-Icon-Collapsed"));

        /// <summary>
        /// Brush resource key used for the collapse-icon (shown in expanded state)
        /// </summary>
        /// <value>The collapse icon brush resource key.</value>
        public string CollapseIconBrushResourceKey
        {
            get { return (string)GetValue(CollapseIconBrushResourceKeyProperty); }
            set { SetValue(CollapseIconBrushResourceKeyProperty, value); }
        }
        /// <summary>
        /// Brush resource key used for the collapse-icon (shown in expanded state)
        /// </summary>
        /// <value>The collapse icon brush resource key.</value>
        public static readonly DependencyProperty CollapseIconBrushResourceKeyProperty = DependencyProperty.Register("CollapseIconBrushResourceKey", typeof(string), typeof(ColumnPanel), new PropertyMetadata("CODE.Framework-Icon-Expanded"));

        /// <summary>
        /// Brush used for the expand-icon (shown in collapsed state)
        /// </summary>
        /// <value>The expand icon.</value>
        public Brush ExpandIcon
        {
            get { return (Brush)GetValue(ExpandIconProperty); }
            set { SetValue(ExpandIconProperty, value); }
        }
        /// <summary>
        /// Brush used for the expand-icon (shown in collapsed state)
        /// </summary>
        /// <value>The expand icon.</value>
        public static readonly DependencyProperty ExpandIconProperty = DependencyProperty.Register("ExpandIcon", typeof(Brush), typeof(ColumnPanel), new PropertyMetadata(null));

        /// <summary>
        /// Brush used for the collapse-icon (shown in collapsed state)
        /// </summary>
        /// <value>The collapse icon.</value>
        public Brush CollapseIcon
        {
            get { return (Brush)GetValue(CollapseIconProperty); }
            set { SetValue(CollapseIconProperty, value); }
        }
        /// <summary>
        /// Brush used for the collapse-icon (shown in collapsed state)
        /// </summary>
        /// <value>The collapse icon.</value>
        public static readonly DependencyProperty CollapseIconProperty = DependencyProperty.Register("CollapseIcon", typeof(Brush), typeof(ColumnPanel), new PropertyMetadata(null));

        /// <summary>
        /// Fires when DetailIsExpanded changes
        /// </summary>
        /// <param name="d">The object the event is raised on</param>
        /// <param name="e">The <see cref="DependencyPropertyChangedEventArgs"/> instance containing the event data.</param>
        private static void OnDetailIsExpandedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var panel = d as ColumnPanel;
            if (panel == null) return;
            panel.InvalidateVisual();
        }

        /// <summary>
        /// Defines whether the detail area is auto-assigned the full width available in the list (true),
        /// or whether the controls in the detail template define their own width (false).
        /// </summary>
        public bool DetailSpansFullWidth
        {
            get { return (bool)GetValue(DetailSpansFullWidthProperty); }
            set { SetValue(DetailSpansFullWidthProperty, value); }
        }

        /// <summary>
        /// Defines whether the detail area is auto-assigned the full width available in the list (true),
        /// or whether the controls in the detail template define their own width (false).
        /// </summary>
        public static readonly DependencyProperty DetailSpansFullWidthProperty = DependencyProperty.Register("DetailSpansFullWidth", typeof(bool), typeof(ColumnPanel), new PropertyMetadata(true));

        private double _mainRowHeight;
        private bool _detailAreaFound;

        /// <summary>
        /// When overridden in a derived class, measures the size in layout required for child elements and determines a size for the <see cref="T:System.Windows.FrameworkElement" />-derived class.
        /// </summary>
        /// <param name="availableSize">The available size that this element can give to child elements. Infinity can be specified as a value to indicate that the element will size to whatever content is available.</param>
        /// <returns>The size that this element determines it needs during layout, based on its calculations of child element sizes.</returns>
        protected override Size MeasureOverride(Size availableSize)
        {
            var totalColumns = ColumnDefinitions == null ? 1 : ColumnDefinitions.Count;
            var columnWidths = GetActualColumnWidths(availableSize.Width);
            var maxHeight = MinHeight;
            if (double.IsNaN(maxHeight)) maxHeight = 0d;
            var detailHeight = 0d;
            var detailWidth = 0d;
            var maxWidth = 0d;
            _detailAreaFound = false;
            if (ColumnDefinitions != null)
            {
                foreach (var child in Children.OfType<UIElement>().Where(c => !GetIsDetail(c)))
                {
                    var columnIndex = Math.Min(GetColumn(child), totalColumns);
                    if (ColumnDefinitions.Count > 0 && ColumnDefinitions[columnIndex].Visible == Visibility.Visible)
                    {
                        var currentColumnWidth = columnWidths[columnIndex];
                        child.Measure(new Size(currentColumnWidth, double.PositiveInfinity));
                        maxHeight = Math.Max(maxHeight, child.DesiredSize.Height);
                        if (!ColumnDefinitions[columnIndex].Width.IsAbsolute)
                            columnWidths[columnIndex] = Math.Max(currentColumnWidth, child.DesiredSize.Width);
                    }
                }
                maxWidth = columnWidths.Sum();
                foreach (var child in Children.OfType<UIElement>().Where(GetIsDetail))
                {
                    _detailAreaFound = true;
                    if (DetailIsExpanded)
                    {
                        if (DetailSpansFullWidth)
                            child.Measure(new Size(Math.Max(maxWidth - DetailAreaIndent - ExpandIconWidth, 0d), double.PositiveInfinity));
                        else
                        {
                            child.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                            detailWidth = child.DesiredSize.Width + DetailAreaIndent + ExpandIconWidth;
                        }
                        detailHeight = child.DesiredSize.Height;
                    }
                }
            }

            _mainRowHeight = maxHeight;
            if (_detailAreaFound && DetailIsExpanded)
            {
                if (!DetailSpansFullWidth) maxWidth = Math.Max(maxWidth, detailWidth);
                maxHeight += detailHeight;
            }
            return GeometryHelper.NewSize(maxWidth, maxHeight);
        }

        /// <summary>
        /// When overridden in a derived class, positions child elements and determines a size for a <see cref="T:System.Windows.FrameworkElement" /> derived class.
        /// </summary>
        /// <param name="finalSize">The final area within the parent that this element should use to arrange itself and its children.</param>
        /// <returns>The actual size used.</returns>
        protected override Size ArrangeOverride(Size finalSize)
        {
            var actualColumnWidths = GetActualColumnWidths(finalSize.Width);
            var totalColumns = actualColumnWidths.Length;

            var columnLefts = new double[actualColumnWidths.Length];
            var currentLeft = 0d;
            for (var columnCounter = 0; columnCounter < columnLefts.Length; columnCounter++)
            {
                columnLefts[columnCounter] = currentLeft;
                currentLeft += actualColumnWidths[columnCounter];
            }

            var columnCounter2 = 0;
            foreach (var child in Children.OfType<UIElement>()) // We want the invisible ones too
                if (GetIsDetail(child))
                {
                    if (DetailIsExpanded)
                    {
                        child.Visibility = Visibility.Visible;
                        Rect renderRect;
                        if (!DetailSpansFullWidth)
                            renderRect = GeometryHelper.NewRect(DetailAreaIndent + ExpandIconWidth, _mainRowHeight + 1, child.DesiredSize.Width, finalSize.Height - _mainRowHeight - 1);
                        else
                            renderRect = GeometryHelper.NewRect(DetailAreaIndent + ExpandIconWidth, _mainRowHeight + 1, finalSize.Width - DetailAreaIndent - ExpandIconWidth, finalSize.Height - _mainRowHeight - 1);
                        child.Arrange(renderRect);
                    }
                    else
                        child.Visibility = Visibility.Collapsed;
                }
                else
                {
                    var columnIndex = Math.Min(GetColumn(child), totalColumns);
                    if (ColumnDefinitions != null && ColumnDefinitions.Count > 0 && ColumnDefinitions[columnIndex].Visible == Visibility.Visible)
                    {
                        columnCounter2++;
                        if (ColumnDefinitions[columnIndex].AutoShowChildElement) child.Visibility = Visibility.Visible;
                        Rect renderRect;
                        if (columnCounter2 == 1 && _detailAreaFound)
                            renderRect = GeometryHelper.NewRect(columnLefts[columnIndex] + ExpandIconWidth, 0d, actualColumnWidths[columnIndex] - ExpandIconWidth, _mainRowHeight);
                        else
                            renderRect = GeometryHelper.NewRect(columnLefts[columnIndex], 0d, actualColumnWidths[columnIndex], _mainRowHeight);
                        child.Arrange(renderRect);
                    }
                    else
                        child.Visibility = Visibility.Collapsed;
                }

            return finalSize;
        }

        private double[] GetActualColumnWidths(double totalWidth)
        {
            if (ColumnDefinitions == null || ColumnDefinitions.Count < 1)
                return !double.IsNaN(totalWidth) && !double.IsInfinity(totalWidth) ? new[] {totalWidth} : new[] {0d};

            var columns = ColumnDefinitions;
            var widths = new double[columns.Count];

            var totalPixelWidth = 0d;
            var totalStarWidth = 0d;
            for (var columnCounter = 0; columnCounter < columns.Count; columnCounter++)
                if (columns[columnCounter].Visible == Visibility.Visible)
                    if (columns[columnCounter].Width.GridUnitType == GridUnitType.Pixel)
                    {
                        totalPixelWidth += columns[columnCounter].Width.Value;
                        widths[columnCounter] = columns[columnCounter].Width.Value;
                    }
                    else
                        // We treat autos as if they were stars
                        totalStarWidth += columns[columnCounter].Width.Value;

            if (totalStarWidth > 0d && !double.IsNaN(totalWidth) && !double.IsInfinity(totalWidth))
            {
                var availableToStars = totalWidth - totalPixelWidth;
                if (availableToStars > 0d)
                {
                    var oneStar = availableToStars/totalStarWidth;
                    for (var columnCounter = 0; columnCounter < columns.Count; columnCounter++)
                        if (columns[columnCounter].Visible == Visibility.Visible)
                            if (columns[columnCounter].Width.GridUnitType != GridUnitType.Pixel)
                                widths[columnCounter] = columns[columnCounter].Width.Value*oneStar;
                }
            }

            return widths;
        }

        /// <summary>
        /// Draws the content of a <see cref="T:System.Windows.Media.DrawingContext" /> object during the render pass of a <see cref="T:System.Windows.Controls.Panel" /> element.
        /// </summary>
        /// <param name="dc">The <see cref="T:System.Windows.Media.DrawingContext" /> object to draw.</param>
        protected override void OnRender(DrawingContext dc)
        {
            base.OnRender(dc);

            if (_detailAreaFound)
            {
                var iconWidth = ExpandIconWidth - 10;
                if (iconWidth > _mainRowHeight - 10) iconWidth = _mainRowHeight - 10;
                Brush iconBrush = null;
                if (DetailIsExpanded)
                {
                    if (CollapseIcon == null)
                        if (!string.IsNullOrEmpty(CollapseIconBrushResourceKey))
                            CollapseIcon = FindResource(CollapseIconBrushResourceKey) as Brush;
                    iconBrush = CollapseIcon;
                }
                else
                {
                    if (ExpandIcon == null)
                        if (!string.IsNullOrEmpty(ExpandIconBrushResourceKey))
                            ExpandIcon = FindResource(ExpandIconBrushResourceKey) as Brush;
                    iconBrush = ExpandIcon;

                }
                if (iconBrush != null)
                    dc.DrawRectangle(iconBrush, null, GeometryHelper.NewRect(5, (int) ((_mainRowHeight - iconWidth) / 2), iconWidth, iconWidth));
            }
        }

        /// <summary>
        /// Invoked when an unhandled <see cref="E:System.Windows.UIElement.MouseLeftButtonDown" /> routed event is raised on this element. Implement this method to add class handling for this event.
        /// </summary>
        /// <param name="e">The <see cref="T:System.Windows.Input.MouseButtonEventArgs" /> that contains the event data. The event data reports that the left mouse button was pressed.</param>
        protected override void OnPreviewMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            if (!_detailAreaFound) return;

            var position = e.GetPosition(this);
            if (position.X <= ExpandIconWidth && position.Y <= _mainRowHeight)
            {
                DetailIsExpanded = !DetailIsExpanded;
                e.Handled = true;
            }
        }
    }

    /// <summary>
    /// Specialized column definition for ColumnPanel control
    /// </summary>
    public class ColumnPanelColumnDefinition : ColumnDefinition
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ColumnPanelColumnDefinition"/> class.
        /// </summary>
        public ColumnPanelColumnDefinition()
        {
            AutoShowChildElement = true;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ColumnPanelColumnDefinition"/> class.
        /// </summary>
        /// <param name="column">The column.</param>
        public ColumnPanelColumnDefinition(ColumnDefinition column)
        {
            var descriptor = DependencyPropertyDescriptor.FromProperty(WidthProperty, typeof(ColumnDefinition));
            descriptor.AddValueChanged(this, (o, e) =>
            {
                var handler = WidthChanged;
                if (handler != null)
                    handler(this, new EventArgs());
            });

            var binding = BindingOperations.GetBinding(column, WidthProperty);
            if (binding == null)
                Width = column.Width;
            else
                SetBinding(WidthProperty, binding);
        }

        /// <summary>
        /// Occurs when the column width changes
        /// </summary>
        public event EventHandler WidthChanged;

        /// <summary>
        /// Indicates whether child elements (cell contents) should be made visible automatically when the cell is shown
        /// </summary>
        public bool AutoShowChildElement { get; set; }

        /// <summary>
        /// Gets or sets the visible of the column.
        /// </summary>
        /// <value>The visible.</value>
        public Visibility Visible
        {
            get { return (Visibility) GetValue(VisibleProperty); }
            set { SetValue(VisibleProperty, value); }
        }

        /// <summary>
        /// Gets or sets the visible of the column.
        /// </summary>
        public static readonly DependencyProperty VisibleProperty = DependencyProperty.Register("Visible", typeof (Visibility), typeof (ColumnPanelColumnDefinition), new PropertyMetadata(Visibility.Visible));

        /// <summary>
        /// Read only text property used as column content by some renderers
        /// </summary>
        public string ReadOnlyText
        {
            get { return (string)GetValue(ReadOnlyTextProperty); }
            set { SetValue(ReadOnlyTextProperty, value); }
        }
        /// <summary>
        /// Read only text property used as column content by some renderers
        /// </summary>
        public static readonly DependencyProperty ReadOnlyTextProperty = DependencyProperty.Register("ReadOnlyText", typeof(string), typeof(ColumnPanelColumnDefinition), new PropertyMetadata("", OnReadOnlyTextChanged));

        /// <summary>
        /// Invoked when read-only text changes
        /// </summary>
        /// <param name="d">The dependency object the text changes on</param>
        /// <param name="args">The <see cref="DependencyPropertyChangedEventArgs"/> instance containing the event data.</param>
        /// <exception cref="System.NotImplementedException"></exception>
        private static void OnReadOnlyTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs args)
        {
            var column = d as ColumnPanelColumnDefinition;
            if (column == null) return;
            var handler = column.ReadOnlyTextChanged;
            if (handler != null)
                handler(column, new EventArgs());
        }

        /// <summary>
        /// Occurs when read-only text changes
        /// </summary>
        public event EventHandler ReadOnlyTextChanged;

        /// <summary>
        /// Desired text alignment for read-only column content (note: this is only supported by some renderers)
        /// </summary>
        public TextAlignment ReadOnlyTextAlignment
        {
            get { return (TextAlignment)GetValue(ReadOnlyTextAlignmentProperty); }
            set { SetValue(ReadOnlyTextAlignmentProperty, value); }
        }
        /// <summary>
        /// Desired text alignment for read-only column content (note: this is only supported by some renderers)
        /// </summary>
        public static readonly DependencyProperty ReadOnlyTextAlignmentProperty = DependencyProperty.Register("ReadOnlyTextAlignment", typeof(TextAlignment), typeof(ColumnPanelColumnDefinition), new PropertyMetadata(TextAlignment.Left, OnReadOnlyTextAlignmentChanged));

        /// <summary>
        /// Occurs when the read-only text alignment changes
        /// </summary>
        /// <param name="d">The d.</param>
        /// <param name="args">The <see cref="DependencyPropertyChangedEventArgs"/> instance containing the event data.</param>
        /// <exception cref="System.NotImplementedException"></exception>
        private static void OnReadOnlyTextAlignmentChanged(DependencyObject d, DependencyPropertyChangedEventArgs args)
        {
            var column = d as ColumnPanelColumnDefinition;
            if (column == null) return;
            var handler = column.ReadOnlyTextAlignmentChanged;
            if (handler != null)
                handler(column, new EventArgs());
        }

        /// <summary>
        /// Occurs when read-only text alignment changes
        /// </summary>
        public event EventHandler ReadOnlyTextAlignmentChanged;
    }

    /// <summary>
    /// Specialized collection class for column definitions on the ColumnPanel class
    /// </summary>
    public class ColumnPanelColumnsCollection : ObservableCollection<ColumnPanelColumnDefinition>
    {
        private readonly ColumnPanel _parent;

        /// <summary>
        /// Initializes a new instance of the <see cref="ColumnPanelColumnsCollection"/> class.
        /// </summary>
        /// <param name="parent">The parent.</param>
        public ColumnPanelColumnsCollection(ColumnPanel parent)
        {
            _parent = parent;
        }

        /// <summary>
        /// Adds the specified column.
        /// </summary>
        /// <param name="column">The column.</param>
        [Obsolete("Please use the overload with the ColumnPanelColumnDefinition parameter instead.")]
        public void Add(ColumnDefinition column)
        {
            Add(new ColumnPanelColumnDefinition(column));
        }

        /// <summary>
        /// Adds the specified column.
        /// </summary>
        /// <param name="column">The column.</param>
        public new void Add(ColumnPanelColumnDefinition column)
        {
            base.Add(column);

            var dpd = DependencyPropertyDescriptor.FromProperty(ColumnPanelColumnDefinition.VisibleProperty, typeof (ColumnPanelColumnDefinition));
            if (dpd != null)
                dpd.AddValueChanged(column, (sender, args) =>
                {
                    _parent.InvalidateMeasure();
                    _parent.InvalidateArrange();
                });
        }
    }
}
