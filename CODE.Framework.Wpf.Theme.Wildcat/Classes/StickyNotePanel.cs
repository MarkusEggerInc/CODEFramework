using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace CODE.Framework.Wpf.Theme.Wildcat.Classes
{
    /// <summary>
    /// Panel specifically designed to lay out sticky notes
    /// </summary>
    public class StickyNotePanel : Panel
    {
        /// <summary>
        /// Overall padding/margin around the sticky notes
        /// </summary>
        /// <value>The padding.</value>
        public Thickness Padding
        {
            get { return (Thickness)GetValue(PaddingProperty); }
            set { SetValue(PaddingProperty, value); }
        }
        /// <summary>
        /// Overall padding/margin around the sticky notes
        /// </summary>
        /// <value>The padding.</value>
        public static readonly DependencyProperty PaddingProperty = DependencyProperty.Register("Padding", typeof(Thickness), typeof(StickyNotePanel), new PropertyMetadata(new Thickness(25d), (o, args) => InvalidateAll(o)));

        /// <summary>
        /// Maximum width for each sticky note
        /// </summary>
        public double MaxNoteWidth
        {
            get { return (double)GetValue(MaxNoteWidthProperty); }
            set { SetValue(MaxNoteWidthProperty, value); }
        }
        /// <summary>
        /// Maximum width for each sticky note
        /// </summary>
        public static readonly DependencyProperty MaxNoteWidthProperty = DependencyProperty.Register("MaxNoteWidth", typeof(double), typeof(StickyNotePanel), new PropertyMetadata(225d, (o, args) => InvalidateAll(o)));

        /// <summary>
        /// Maximum height for each sticky note
        /// </summary>
        public double MaxNoteHeight
        {
            get { return (double)GetValue(MaxNoteHeightProperty); }
            set { SetValue(MaxNoteHeightProperty, value); }
        }
        /// <summary>
        /// Maximum height for each sticky note
        /// </summary>
        public static readonly DependencyProperty MaxNoteHeightProperty = DependencyProperty.Register("MaxNoteHeight", typeof(double), typeof(StickyNotePanel), new PropertyMetadata(225d, (o, args) => InvalidateAll(o)));

        /// <summary>
        /// Triggers a re-render
        /// </summary>
        /// <param name="d">The object to invalidate</param>
        private static void InvalidateAll(DependencyObject d)
        {
            var element = d as UIElement;
            if (element == null) return;
            element.InvalidateVisual();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="StickyNotePanel"/> class.
        /// </summary>
        public StickyNotePanel()
        {
            ClipToBounds = false;
            VerticalAlignment = VerticalAlignment.Stretch;
            HorizontalAlignment = HorizontalAlignment.Stretch;
        }

        /// <summary>
        /// When overridden in a derived class, measures the size in layout required for child elements and determines a size for the <see cref="T:System.Windows.FrameworkElement" />-derived class.
        /// </summary>
        /// <param name="availableSize">The available size that this element can give to child elements. Infinity can be specified as a value to indicate that the element will size to whatever content is available.</param>
        /// <returns>The size that this element determines it needs during layout, based on its calculations of child element sizes.</returns>
        protected override Size MeasureOverride(Size availableSize)
        {
            var availableHeight = availableSize.Height;
            if (double.IsNaN(availableHeight)) availableHeight = MaxNoteHeight;
            availableHeight = availableHeight - Padding.Left - Padding.Right;

            var columnHeights = new List<double> {0d};
            var columnWidths = new List<double> {0d};

            foreach (UIElement child in Children)
            {
                child.Measure(new Size(MaxNoteWidth, MaxNoteHeight));

                if (columnHeights[columnHeights.Count - 1] > 0d && columnHeights[columnHeights.Count - 1] + child.DesiredSize.Height > availableHeight)
                {
                    columnHeights.Add(0d);
                    columnWidths.Add(0d);
                }

                columnHeights[columnHeights.Count - 1] += child.DesiredSize.Height;
                columnWidths[columnWidths.Count - 1] = Math.Max(columnWidths[columnWidths.Count - 1], child.DesiredSize.Width);
            }

            var totalWidth = columnWidths.Sum(w => w);
            var totalHeight = columnHeights.Max(h => h);
            return new Size(totalWidth, totalHeight);
        }

        /// <summary>
        /// When overridden in a derived class, positions child elements and determines a size for a <see cref="T:System.Windows.FrameworkElement" /> derived class.
        /// </summary>
        /// <param name="finalSize">The final area within the parent that this element should use to arrange itself and its children.</param>
        /// <returns>The actual size used.</returns>
        protected override Size ArrangeOverride(Size finalSize)
        {
            var availableHeight = finalSize.Height;
            if (double.IsNaN(availableHeight)) availableHeight = MaxNoteHeight;
            availableHeight = availableHeight - Padding.Left - Padding.Right;

            var columnHeights = new List<double> { 0d };
            var columnWidths = new List<double> { 0d };
            var columns = new List<List<UIElement>> {new List<UIElement>()};
            var rotations = new Dictionary<double, double> {{-3d, 1d}, {-2d, 2d}, {0d, 2d}, {1d, 4d}, {2d, -3d}, {4d, -2d}};
            var lastRotationAngle = 0d;

            foreach (UIElement child in Children)
            {
                if (columnHeights[columnHeights.Count - 1] > 0d && columnHeights[columnHeights.Count - 1] + child.DesiredSize.Height > availableHeight - Padding.Top - Padding.Bottom)
                {
                    columnHeights.Add(0d);
                    columnWidths.Add(0d);
                    columns.Add(new List<UIElement>());
                }

                columnHeights[columnHeights.Count - 1] += child.DesiredSize.Height;
                columnWidths[columnWidths.Count - 1] = Math.Max(columnWidths[columnWidths.Count - 1], child.DesiredSize.Width);
                columns[columns.Count - 1].Add(child);
            }

            var left = finalSize.Width - columnWidths[0] - Padding.Right;
            var columnCounter = 0;
            foreach (var column in columns)
            {
                var top = Padding.Top;
                foreach (var child in column)
                {
                    if (!(child.RenderTransform is RotateTransform))
                    {
                        var nextAngle = 0d;
                        if (rotations.ContainsKey(lastRotationAngle)) nextAngle = rotations[lastRotationAngle];
                        child.RenderTransform = new RotateTransform(nextAngle, .5d, .5d);
                    }
                    lastRotationAngle = ((RotateTransform)child.RenderTransform).Angle;
                    var childHeight = Math.Min(MaxNoteHeight, child.DesiredSize.Height);
                    child.Arrange(new Rect(left, top, Math.Min(MaxNoteWidth, child.DesiredSize.Width), childHeight));
                    top += childHeight;
                }
                left -= columnWidths[columnCounter];
                columnCounter++;
            }

            base.ArrangeOverride(finalSize);
            var totalWidth = columnWidths.Sum(w => w);
            var totalHeight = columnHeights.Sum(h => h);
            return new Size(Math.Max(totalWidth, finalSize.Width), Math.Max(totalHeight, finalSize.Height));
        }
    }
}
