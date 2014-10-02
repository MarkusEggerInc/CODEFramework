using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

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
            ColumnDefinitions = new ObservableCollection<ColumnDefinition>();
        }

        /// <summary>Column definitions</summary>
        public ObservableCollection<ColumnDefinition> ColumnDefinitions
        {
            get { return (ObservableCollection<ColumnDefinition>)GetValue(ColumnDefinitionsProperty); }
            set { SetValue(ColumnDefinitionsProperty, value); }
        }
        /// <summary>Column definitions</summary>
        public static readonly DependencyProperty ColumnDefinitionsProperty = DependencyProperty.Register("ColumnDefinitions", typeof(ObservableCollection<ColumnDefinition>), typeof(ColumnPanel), new PropertyMetadata(null, OnColumnDefinitionsChanged));

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
        public static readonly DependencyProperty ColumnProperty = DependencyProperty.RegisterAttached("Column", typeof(int), typeof(ColumnPanel), new PropertyMetadata(0));

        protected override Size MeasureOverride(Size availableSize)
        {
            var infinity = new Size(double.PositiveInfinity, double.PositiveInfinity);
            var totalColumns = ColumnDefinitions == null ? 1 : ColumnDefinitions.Count;
            var columnWidths = GetActualColumnWidths(availableSize.Width);
            var maxHeight = 0d;
            foreach (UIElement child in Children)
            {
                var columnIndex = Math.Min(GetColumn(child), totalColumns);
                child.Measure(infinity);
                maxHeight = Math.Max(maxHeight, child.DesiredSize.Height);
                columnWidths[columnIndex] = Math.Max(columnWidths[columnIndex], child.DesiredSize.Width);
            }

            return new Size(columnWidths.Sum(), maxHeight);
        }

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
                var columnIndex = Math.Min(GetColumn(child), totalColumns);
                var renderRect = new Rect(columnLefts[columnIndex], 0d, actualColumnWidths[columnIndex], finalSize.Height);
                child.Arrange(renderRect);
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
                        if (columns[columnCounter].Width.GridUnitType != GridUnitType.Pixel)
                            widths[columnCounter] = columns[columnCounter].Width.Value*oneStar;
                }
            }

            return widths;
        }
    }
}
