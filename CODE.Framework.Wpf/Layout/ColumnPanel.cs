using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

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
                        descriptor.AddValueChanged(newColumn, (s2, e2) =>
                        {
                            panel.InvalidateArrange();
                            panel.InvalidateMeasure();
                        });
                    }
            };
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

        /// <summary>
        /// When overridden in a derived class, measures the size in layout required for child elements and determines a size for the <see cref="T:System.Windows.FrameworkElement" />-derived class.
        /// </summary>
        /// <param name="availableSize">The available size that this element can give to child elements. Infinity can be specified as a value to indicate that the element will size to whatever content is available.</param>
        /// <returns>The size that this element determines it needs during layout, based on its calculations of child element sizes.</returns>
        protected override Size MeasureOverride(Size availableSize)
        {
            var infinity = new Size(double.PositiveInfinity, double.PositiveInfinity);
            var totalColumns = ColumnDefinitions == null ? 1 : ColumnDefinitions.Count;
            var columnWidths = GetActualColumnWidths(availableSize.Width);
            var maxHeight = 0d;
            if (ColumnDefinitions != null)
                foreach (UIElement child in Children)
                {
                    var columnIndex = Math.Min(GetColumn(child), totalColumns);
                    if (ColumnDefinitions.Count > 0 && ColumnDefinitions[columnIndex].Visible == Visibility.Visible)
                    {
                        child.Measure(infinity);
                        maxHeight = Math.Max(maxHeight, child.DesiredSize.Height);
                        if (!ColumnDefinitions[columnIndex].Width.IsAbsolute)
                            columnWidths[columnIndex] = Math.Max(columnWidths[columnIndex], child.DesiredSize.Width);
                    }
                }

            return new Size(columnWidths.Sum(), maxHeight);
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

            foreach (UIElement child in Children)
            {
                if (child == null) continue;
                var columnIndex = Math.Min(GetColumn(child), totalColumns);
                if (ColumnDefinitions != null && ColumnDefinitions.Count > 0 && ColumnDefinitions[columnIndex].Visible == Visibility.Visible)
                {
                    if (ColumnDefinitions[columnIndex].AutoShowChildElement)
                        child.Visibility = Visibility.Visible;
                    var renderRect = new Rect(columnLefts[columnIndex], 0d, actualColumnWidths[columnIndex], finalSize.Height);
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
            var binding = BindingOperations.GetBinding(column, WidthProperty);
            if (binding == null)
                Width = column.Width;
            else
                SetBinding(WidthProperty, binding);

            var descriptor = DependencyPropertyDescriptor.FromProperty(WidthProperty, typeof(ColumnDefinition));
            descriptor.AddValueChanged(this, (o, e) =>
            {
                if (WidthChanged != null)
                    WidthChanged(this, new EventArgs());
            });
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
            if (column.ReadOnlyTextChanged != null)
                column.ReadOnlyTextChanged(column, new EventArgs());
        }

        /// <summary>
        /// Occurs when read-only text changes
        /// </summary>
        public event EventHandler ReadOnlyTextChanged;

        /// <summary>
        /// Desired text alignment for read-only column content (note: this is only supported by some rederers)
        /// </summary>
        public TextAlignment ReadOnlyTextAlignment
        {
            get { return (TextAlignment)GetValue(ReadOnlyTextAlignmentProperty); }
            set { SetValue(ReadOnlyTextAlignmentProperty, value); }
        }
        /// <summary>
        /// Desired text alignment for read-only column content (note: this is only supported by some rederers)
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
            if (column.ReadOnlyTextAlignmentChanged != null)
                column.ReadOnlyTextAlignmentChanged(column, new EventArgs());
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
