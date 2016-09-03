using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace CODE.Framework.Wpf.Controls
{
    /// <summary>Control used to indicate sort orders</summary>
    public class SortOrderIndicator : ContentControl
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SortOrderIndicator"/> class.
        /// </summary>
        public SortOrderIndicator()
        {
            Content = new Rectangle {VerticalAlignment = VerticalAlignment.Stretch, HorizontalAlignment = HorizontalAlignment.Stretch};
        }

        private void SetBrush()
        {
            var rect = Content as Rectangle;
            if (rect == null) return;

            switch (Order)
            {
                case SortOrder.Unsorted:
                    rect.Fill = UnsortedBrush;
                    Visibility = UnsortedBrush == null ? Visibility.Collapsed : Visibility.Visible;
                    break;
                case SortOrder.Ascending:
                    rect.Fill = AscendingBrush;
                    Visibility = AscendingBrush == null ? Visibility.Collapsed : Visibility.Visible;
                    break;
                case SortOrder.Descending:
                    rect.Fill = DescendingBrush;
                    Visibility = DescendingBrush == null ? Visibility.Collapsed : Visibility.Visible;
                    break;
            }
            if (Parent == null) return;
            var element = Parent as UIElement;
            if (element == null) return;
            element.InvalidateMeasure();
            element.InvalidateArrange();
        }

        /// <summary>Sort order</summary>
        /// <value>The sort order</value>
        public SortOrder Order
        {
            get { return (SortOrder)GetValue(OrderProperty); }
            set { SetValue(OrderProperty, value); }
        }
        /// <summary>Sort order</summary>
        /// <value>The sort order</value>
        public static readonly DependencyProperty OrderProperty = DependencyProperty.Register("Order", typeof(SortOrder), typeof(SortOrderIndicator), new PropertyMetadata(SortOrder.Unsorted, OnOrderChanged));

        /// <summary>Fires when one of the dependency properties changes that causes a brush change</summary>
        /// <param name="d">The source</param>
        /// <param name="args">The <see cref="DependencyPropertyChangedEventArgs"/> instance containing the event data.</param>
        private static void OnOrderChanged(DependencyObject d, DependencyPropertyChangedEventArgs args)
        {
            var control = d as SortOrderIndicator;
            if (control == null) return;
            control.SetBrush();
        }

        /// <summary>Brush used to indicate unsorted order</summary>
        /// <value>The unsorted brush.</value>
        public Brush UnsortedBrush
        {
            get { return (Brush) GetValue(UnsortedBrushProperty); }
            set { SetValue(UnsortedBrushProperty, value); }
        }
        /// <summary>Brush used to indicate unsorted order</summary>
        /// <value>The unsorted brush.</value>
        public static readonly DependencyProperty UnsortedBrushProperty = DependencyProperty.Register("UnsortedBrush", typeof(Brush), typeof(SortOrderIndicator), new PropertyMetadata(null, OnOrderChanged));

        /// <summary>Brush used to indicate ascending order</summary>
        /// <value>The ascending brush.</value>
        public Brush AscendingBrush
        {
            get { return (Brush)GetValue(AscendingBrushProperty); }
            set { SetValue(AscendingBrushProperty, value); }
        }
        /// <summary>Brush used to indicate ascending order</summary>
        /// <value>The ascending brush.</value>
        public static readonly DependencyProperty AscendingBrushProperty = DependencyProperty.Register("AscendingBrush", typeof(Brush), typeof(SortOrderIndicator), new PropertyMetadata(null, OnOrderChanged));

        /// <summary>Brush used to indicate descending order</summary>
        /// <value>The descending brush.</value>
        public Brush DescendingBrush
        {
            get { return (Brush)GetValue(DescendingBrushProperty); }
            set { SetValue(DescendingBrushProperty, value); }
        }
        /// <summary>Brush used to indicate descending order</summary>
        /// <value>The descending brush.</value>
        public static readonly DependencyProperty DescendingBrushProperty = DependencyProperty.Register("DescendingBrush", typeof(Brush), typeof(SortOrderIndicator), new PropertyMetadata(null, OnOrderChanged));
    }

    /// <summary>
    /// Sort order
    /// </summary>
    public enum SortOrder
    {
        /// <summary>
        /// Unsorted
        /// </summary>
        Unsorted,
        /// <summary>
        /// Ascending sort
        /// </summary>
        Ascending,
        /// <summary>
        /// Descending sort
        /// </summary>
        Descending
    }
}
