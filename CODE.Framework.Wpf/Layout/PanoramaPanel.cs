using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using CODE.Framework.Wpf.Utilities;

namespace CODE.Framework.Wpf.Layout
{
    /// <summary>
    /// Panorama layout container (similar, but not identical, to the Windows Phone Panorama control)
    /// </summary>
    public class PanoramaPanel : Panel
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PanoramaPanel"/> class.
        /// </summary>
        public PanoramaPanel()
        {
            IsHitTestVisible = true;
            ClipToBounds = true;
        }

        /// <summary>
        /// Selected item within the panorama
        /// </summary>
        /// <value>The index of the selected.</value>
        public int SelectedIndex
        {
            get { return (int)GetValue(SelectedIndexProperty); }
            set { SetValue(SelectedIndexProperty, value); }
        }
        /// <summary>
        /// Selected item within the panorama
        /// </summary>
        public static readonly DependencyProperty SelectedIndexProperty = DependencyProperty.Register("SelectedIndex", typeof(int), typeof(PanoramaPanel), new PropertyMetadata(0, OnSelectedIndexChanged));
        /// <summary>
        /// Fires when the selected panorama item changes
        /// </summary>
        /// <param name="d">The panorama panel</param>
        /// <param name="args">The <see cref="DependencyPropertyChangedEventArgs"/> instance containing the event data.</param>
        private static void OnSelectedIndexChanged(DependencyObject d, DependencyPropertyChangedEventArgs args)
        {
            var panorama = d as PanoramaPanel;
            if (panorama == null) return;
            if (panorama._horizontalHeaderOffsetStoryboard == null)
            {
                var duration = new Duration(new TimeSpan(0, 0, 0, 0, 250));
                panorama._horizontalHeaderOffsetStoryboard = new Storyboard {Duration = duration};
                var animation = new DoubleAnimation(100d, 0d, duration);
                panorama._horizontalHeaderOffsetStoryboard.Children.Add(animation);
                Storyboard.SetTarget(animation, panorama);
                Storyboard.SetTargetProperty(animation, new PropertyPath("HorizontalHeaderRenderingOffset"));
            }
            else
                panorama._horizontalHeaderOffsetStoryboard.Stop();
            panorama._horizontalHeaderOffsetStoryboard.Begin();
        }

        /// <summary>
        /// Font size for header elements
        /// </summary>
        /// <value>The size of the header font.</value>
        public double HeaderFontSize
        {
            get { return (double)GetValue(HeaderFontSizeProperty); }
            set { SetValue(HeaderFontSizeProperty, value); }
        }
        /// <summary>
        /// Font size for header elements
        /// </summary>
        public static readonly DependencyProperty HeaderFontSizeProperty = DependencyProperty.Register("HeaderFontSize", typeof(double), typeof(PanoramaPanel), new PropertyMetadata(28d, OnHeaderFontSizeChanged));
        /// <summary>
        /// Fires when the header font size changes
        /// </summary>
        /// <param name="d">The panorama panel</param>
        /// <param name="args">The <see cref="DependencyPropertyChangedEventArgs"/> instance containing the event data.</param>
        private static void OnHeaderFontSizeChanged(DependencyObject d, DependencyPropertyChangedEventArgs args)
        {
            var panorama = d as PanoramaPanel;
            if (panorama == null) return;
            panorama._headerHeight = -1;
            panorama.InvalidateVisual();
        }

        /// <summary>
        /// Font family used for header rendering
        /// </summary>
        /// <value>The header font family.</value>
        public FontFamily HeaderFontFamily
        {
            get { return (FontFamily)GetValue(HeaderFontFamilyProperty); }
            set { SetValue(HeaderFontFamilyProperty, value); }
        }
        /// <summary>
        /// Font family used for header rendering
        /// </summary>
        public static readonly DependencyProperty HeaderFontFamilyProperty = DependencyProperty.Register("HeaderFontFamily", typeof(FontFamily), typeof(PanoramaPanel), new PropertyMetadata(new FontFamily("Segoe UI"), OnHeaderFontFamilyChanged));
        /// <summary>
        /// Fires when the header font family changes
        /// </summary>
        /// <param name="d">The panorama panel</param>
        /// <param name="args">The <see cref="DependencyPropertyChangedEventArgs"/> instance containing the event data.</param>
        private static void OnHeaderFontFamilyChanged(DependencyObject d, DependencyPropertyChangedEventArgs args)
        {
            var panorama = d as PanoramaPanel;
            if (panorama == null) return;
            panorama.InvalidateVisual();
        }

        /// <summary>
        /// Brush used to render the selected header
        /// </summary>
        /// <value>The selected header foreground.</value>
        public Brush SelectedHeaderForeground
        {
            get { return (Brush)GetValue(SelectedHeaderForegroundProperty); }
            set { SetValue(SelectedHeaderForegroundProperty, value); }
        }
        /// <summary>
        /// Brush used to render the selected header
        /// </summary>
        public static readonly DependencyProperty SelectedHeaderForegroundProperty = DependencyProperty.Register("SelectedHeaderForeground", typeof(Brush), typeof(PanoramaPanel), new PropertyMetadata(Brushes.Black, OnSelectedHeaderForegroundChanged));
        /// <summary>
        /// Fires when the header selected foreground brush changes
        /// </summary>
        /// <param name="d">The panorama panel</param>
        /// <param name="args">The <see cref="DependencyPropertyChangedEventArgs"/> instance containing the event data.</param>
        private static void OnSelectedHeaderForegroundChanged(DependencyObject d, DependencyPropertyChangedEventArgs args)
        {
            var panorama = d as PanoramaPanel;
            if (panorama == null) return;
            panorama.InvalidateVisual();
        }

        /// <summary>
        /// Brush used to render the unselected header
        /// </summary>
        /// <value>The unselected header foreground.</value>
        public Brush UnselectedHeaderForeground
        {
            get { return (Brush)GetValue(UnselectedHeaderForegroundProperty); }
            set { SetValue(UnselectedHeaderForegroundProperty, value); }
        }
        /// <summary>
        /// Brush used to render the unselected header
        /// </summary>
        public static readonly DependencyProperty UnselectedHeaderForegroundProperty = DependencyProperty.Register("UnselectedHeaderForeground", typeof(Brush), typeof(PanoramaPanel), new PropertyMetadata(Brushes.Gray, OnUnselectedHeaderForegroundChanged));
        /// <summary>
        /// Fires when the header unselected foreground brush changes
        /// </summary>
        /// <param name="d">The panorama panel</param>
        /// <param name="args">The <see cref="DependencyPropertyChangedEventArgs"/> instance containing the event data.</param>
        private static void OnUnselectedHeaderForegroundChanged(DependencyObject d, DependencyPropertyChangedEventArgs args)
        {
            var panorama = d as PanoramaPanel;
            if (panorama == null) return;
            panorama.InvalidateVisual();
        }

        /// <summary>
        /// For internal use only
        /// </summary>
        /// <value>The horizontal header rendering offset.</value>
        public double HorizontalHeaderRenderingOffset
        {
            get { return (double)GetValue(HorizontalHeaderRenderingOffsetProperty); }
            set { SetValue(HorizontalHeaderRenderingOffsetProperty, value); }
        }
        /// <summary>
        /// For internal use only
        /// </summary>
        public static readonly DependencyProperty HorizontalHeaderRenderingOffsetProperty = DependencyProperty.Register("HorizontalHeaderRenderingOffset", typeof(double), typeof(PanoramaPanel), new PropertyMetadata(0d, OnHorizontalHeaderRenderingOffsetChanged));
        /// <summary>
        /// For internal use only
        /// </summary>
        /// <param name="d">The panorama panel</param>
        /// <param name="args">The <see cref="DependencyPropertyChangedEventArgs"/> instance containing the event data.</param>
        private static void OnHorizontalHeaderRenderingOffsetChanged(DependencyObject d, DependencyPropertyChangedEventArgs args)
        {
            var panorama = d as PanoramaPanel;
            if (panorama == null) return;
            panorama.InvalidateVisual();
        }

        private Storyboard _horizontalHeaderOffsetStoryboard;

        /// <summary>
        /// When overridden in a derived class, measures the size in layout required for child elements and determines a size for the <see cref="T:System.Windows.FrameworkElement" />-derived class.
        /// </summary>
        /// <param name="availableSize">The available size that this element can give to child elements. Infinity can be specified as a value to indicate that the element will size to whatever content is available.</param>
        /// <returns>The size that this element determines it needs during layout, based on its calculations of child element sizes.</returns>
        protected override Size MeasureOverride(Size availableSize)
        {
            foreach (UIElement child in Children)
            {
                child.Measure(availableSize);
                child.IsVisibleChanged += (s, e) => InvalidateVisual();
            }

            return base.MeasureOverride(availableSize);
        }

        /// <summary>
        /// When overridden in a derived class, positions child elements and determines a size for a <see cref="T:System.Windows.FrameworkElement" /> derived class.
        /// </summary>
        /// <param name="finalSize">The final area within the parent that this element should use to arrange itself and its children.</param>
        /// <returns>The actual size used.</returns>
        protected override Size ArrangeOverride(Size finalSize)
        {
            var children = GetCurrentChildren();

            for (var index = 0; index < children.Count; index++)
            {
                var child = children[index].Child;
                if (index == 0)
                {
                    var headerHeight = GetHeaderHeight() + 5;
                    child.Arrange(GeometryHelper.NewRect(0d, headerHeight, finalSize.Width, finalSize.Height - headerHeight));
                    TranslateTransform currentTranslate = null;
                    if (child.RenderTransform != null)
                        if (!(child.RenderTransform is TranslateTransform) && !(child.RenderTransform is TransformGroup))
                        {
                            currentTranslate = new TranslateTransform();
                            child.RenderTransform = currentTranslate;
                        }
                        else if (child.RenderTransform is TransformGroup)
                        {
                            var currentGroup = (TransformGroup) child.RenderTransform;
                            foreach (var transform in currentGroup.Children)
                                if (transform is TranslateTransform)
                                {
                                    currentTranslate = transform as TranslateTransform;
                                    break;
                                }
                            if (currentTranslate == null)
                            {
                                currentTranslate = new TranslateTransform();
                                currentGroup.Children.Add(currentTranslate);
                            }
                        }
                        else
                            currentTranslate = child.RenderTransform as TranslateTransform;

                    if (currentTranslate != null)
                        if (HorizontalHeaderRenderingOffset > 0d)
                            currentTranslate.X = HorizontalHeaderRenderingOffset*3;
                        else
                            currentTranslate.X = 0d;
                }
                else
                    child.Arrange(GeometryHelper.NewRect(-100000d, -100000d, finalSize.Width, finalSize.Height));
            }

            return base.ArrangeOverride(finalSize);
        }

        private List<PanoramaChild> GetCurrentChildren()
        {
            var result = new List<PanoramaChild>();

            var index = SelectedIndex; // Our "first" element is the element that is selected
            PanoramaChild lastChild = null;
            for (var counter = 0; counter < Children.Count; counter++)
            {
                if (index > Children.Count - 1) index = 0; // We wrap back to item 0 when we shoot out the back
                if (Children[index].Visibility == Visibility.Visible)
                {
                    var currentChild = Children[index];
                    if (lastChild != null && SimpleView.GetFlowsWithPrevious(currentChild))
                        lastChild.FloatingElements.Add(currentChild);
                    else
                    {
                        lastChild = new PanoramaChild {Child = currentChild, ActualChildIndex = index};
                        result.Add(lastChild);
                    }
                }
                index++;
            }

            return result;
        }

        private class PanoramaChild
        {
            public PanoramaChild()
            {
                FloatingElements = new List<UIElement>();
            }
            public UIElement Child { get; set; }
            public List<UIElement> FloatingElements { get; set; }
            public int ActualChildIndex { get; set; }
        }

        /// <summary>
        /// Draws the content of a <see cref="T:System.Windows.Media.DrawingContext" /> object during the render pass of a <see cref="T:System.Windows.Controls.Panel" /> element.
        /// </summary>
        /// <param name="dc">The <see cref="T:System.Windows.Media.DrawingContext" /> object to draw.</param>
        protected override void OnRender(DrawingContext dc)
        {
            base.OnRender(dc);

            var children = GetCurrentChildren();
            if (children.Count < 1) return;

            _hitZones.Clear();
            var headerHeight = GetHeaderHeight() + 5;
            var currentLeft = HorizontalHeaderRenderingOffset;
            var index = SelectedIndex; // Our "first" element is the element that is selected again
            var isFirst = true;
            foreach (var child in children)
            {
                var title = SimpleView.GetTitle(child.Child);
                if (string.IsNullOrEmpty(title)) title = "Item";
                var ft = isFirst ? new FormattedText(title, CultureInfo.CurrentUICulture, FlowDirection.LeftToRight, new Typeface(HeaderFontFamily, FontStyles.Normal, FontWeights.Normal, FontStretches.Normal), HeaderFontSize, SelectedHeaderForeground) : new FormattedText(title, CultureInfo.CurrentUICulture, FlowDirection.LeftToRight, new Typeface(HeaderFontFamily, FontStyles.Normal, FontWeights.Light, FontStretches.Normal), HeaderFontSize, UnselectedHeaderForeground);
                dc.DrawText(ft, new Point(currentLeft, 0d));

                if (index > Children.Count - 1) index = 0; // We wrap back to item 0 when we shoot out the back
                _hitZones.Add(new PanoramaHeaderHitZone {Index = child.ActualChildIndex, Rect = GeometryHelper.NewRect(currentLeft - 10, 0d, ft.Width + 15, headerHeight)});

                currentLeft += ft.Width + 25;
                index++;
                isFirst = false;
            }
        }

        /// <summary>
        /// Invoked when an unhandled <see cref="E:System.Windows.UIElement.MouseLeftButtonDown" /> routed event is raised on this element. Implement this method to add class handling for this event.
        /// </summary>
        /// <param name="e">The <see cref="T:System.Windows.Input.MouseButtonEventArgs" /> that contains the event data. The event data reports that the left mouse button was pressed.</param>
        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            var headerHeight = GetHeaderHeight() + 5;
            var position = e.GetPosition(this);

            if (position.Y <= headerHeight)
                foreach (var zone in _hitZones.Where(zone => zone.Rect.Contains(position)))
                {
                    SelectedIndex = zone.Index;
                    e.Handled = true;
                    return;
                }

            base.OnMouseLeftButtonDown(e);
        }

        private readonly List<PanoramaHeaderHitZone> _hitZones = new List<PanoramaHeaderHitZone>();

        private class PanoramaHeaderHitZone
        {
            public Rect Rect { get; set; }
            public int Index { get; set; }
        }

        private double _headerHeight = -1d;
        private double GetHeaderHeight()
        {
            if (_headerHeight < 0)
            {
                var ft = new FormattedText("X", CultureInfo.CurrentUICulture, FlowDirection.LeftToRight, new Typeface(HeaderFontFamily, FontStyles.Normal, FontWeights.Normal, FontStretches.Normal), HeaderFontSize, Brushes.Black);
                _headerHeight = ft.Height;
            }
            return _headerHeight;
        }
    }
}
