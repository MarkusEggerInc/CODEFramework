using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using CODE.Framework.Core.Utilities;
using CODE.Framework.Wpf.Utilities;

namespace CODE.Framework.Wpf.Layout
{
    /// <summary>
    /// Special layout panel geared towards rendering 1-to-many UIs (such as customers and their orders)
    /// </summary>
    public class OneToManyPanel : Panel
    {
        /// <summary>Object used to render the captions</summary>
        public AutoLayoutHeaderRenderer CaptionRenderer
        {
            get { return (AutoLayoutHeaderRenderer)GetValue(CaptionRendererProperty); }
            set { SetValue(CaptionRendererProperty, value); }
        }
        /// <summary>Object used to render the captions</summary>
        public static readonly DependencyProperty CaptionRendererProperty = DependencyProperty.Register("CaptionRenderer", typeof(AutoLayoutHeaderRenderer), typeof(OneToManyPanel), new UIPropertyMetadata(null, InvalidateEverything));

        /// <summary>Font family used to render group headers</summary>
        public FontFamily CaptionFontFamily
        {
            get { return (FontFamily)GetValue(CaptionFontFamilyProperty); }
            set { SetValue(CaptionFontFamilyProperty, value); }
        }
        /// <summary>Font family used to render group headers</summary>
        public static readonly DependencyProperty CaptionFontFamilyProperty = DependencyProperty.Register("CaptionFontFamily", typeof(FontFamily), typeof(OneToManyPanel), new UIPropertyMetadata(new FontFamily("Segoe UI"), InvalidateEverything));

        /// <summary>Font style used to render group headers</summary>
        public FontStyle CaptionFontStyle
        {
            get { return (FontStyle)GetValue(CaptionFontStyleProperty); }
            set { SetValue(CaptionFontStyleProperty, value); }
        }
        /// <summary>Font style used to render group headers</summary>
        public static readonly DependencyProperty CaptionFontStyleProperty = DependencyProperty.Register("CaptionFontStyle", typeof(FontStyle), typeof(OneToManyPanel), new UIPropertyMetadata(FontStyles.Normal, InvalidateEverything));

        /// <summary>Font weight used to render group headers</summary>
        public FontWeight CaptionFontWeight
        {
            get { return (FontWeight)GetValue(CaptionFontWeightProperty); }
            set { SetValue(CaptionFontWeightProperty, value); }
        }
        /// <summary>Font weight used to render group headers</summary>
        public static readonly DependencyProperty CaptionFontWeightProperty = DependencyProperty.Register("CaptionFontWeight", typeof(FontWeight), typeof(OneToManyPanel), new UIPropertyMetadata(FontWeights.Bold, InvalidateEverything));

        /// <summary>Font size used to render group headers</summary>
        public double CaptionFontSize
        {
            get { return (double)GetValue(CaptionFontSizeProperty); }
            set { SetValue(CaptionFontSizeProperty, value); }
        }
        /// <summary>Font size used to render group headers</summary>
        public static readonly DependencyProperty CaptionFontSizeProperty = DependencyProperty.Register("CaptionFontSize", typeof(double), typeof(OneToManyPanel), new UIPropertyMetadata(12d, InvalidateEverything));

        /// <summary>Foreground brush used to render group headers</summary>
        public Brush CaptionForegroundBrush
        {
            get { return (Brush)GetValue(CaptionForegroundBrushProperty); }
            set { SetValue(CaptionForegroundBrushProperty, value); }
        }
        /// <summary>Foreground brush used to render group headers</summary>
        public static readonly DependencyProperty CaptionForegroundBrushProperty = DependencyProperty.Register("CaptionForegroundBrush", typeof(Brush), typeof(OneToManyPanel), new UIPropertyMetadata(Brushes.Black, InvalidateEverything));

        /// <summary>Indicates whether the general layout is horizontal or vertical</summary>
        public Orientation Orientation
        {
            get { return (Orientation)GetValue(OrientationProperty); }
            set { SetValue(OrientationProperty, value); }
        }
        /// <summary>Indicates whether the general layout is horizontal or vertical</summary>
        public static readonly DependencyProperty OrientationProperty = DependencyProperty.Register("Orientation", typeof(Orientation), typeof(OneToManyPanel), new UIPropertyMetadata(Orientation.Vertical, InvalidateEverything));

        /// <summary>Spacing between the 2 elements</summary>
        public double Spacing
        {
            get { return (double)GetValue(SpacingProperty); }
            set { SetValue(SpacingProperty, value); }
        }
        /// <summary>Spacing between the 2 elements</summary>
        public static readonly DependencyProperty SpacingProperty = DependencyProperty.Register("Spacing", typeof(double), typeof(OneToManyPanel), new UIPropertyMetadata(8d, InvalidateEverything));

        /// <summary>Spacing between the caption and the element</summary>
        public double CaptionSpacing
        {
            get { return (double)GetValue(CaptionSpacingProperty); }
            set { SetValue(CaptionSpacingProperty, value); }
        }
        /// <summary>Spacing between the caption and the element</summary>
        public static readonly DependencyProperty CaptionSpacingProperty = DependencyProperty.Register("CaptionSpacing", typeof(double), typeof(OneToManyPanel), new UIPropertyMetadata(8d, InvalidateEverything));

        /// <summary>Caption for elements within a one-to-many panel</summary>
        public static readonly DependencyProperty CaptionProperty = DependencyProperty.RegisterAttached("Caption", typeof(string), typeof(OneToManyPanel), new UIPropertyMetadata("", InvalidateEverything));
        /// <summary>Caption for elements within a one-to-many panel</summary>
        /// <param name="obj">The dependency object the value is associated with</param>
        public static string GetCaption(DependencyObject obj) { return (string)obj.GetValue(CaptionProperty); }
        /// <summary>Caption for elements within a one-to-many panel</summary>
        /// <param name="obj">The dependency object the value is associated with</param>
        /// <param name="value">Value</param>
        public static void SetCaption(DependencyObject obj, string value) { obj.SetValue(CaptionProperty, value); }        

        /// <summary>Invalidates all layout, measurement, and rendering</summary>
        /// <param name="dependencyObject">One-To-Many panel to invalidate</param>
        /// <param name="e">The <see cref="System.Windows.DependencyPropertyChangedEventArgs"/> instance containing the event data.</param>
        private static void InvalidateEverything(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
        {
            var panel = dependencyObject as OneToManyPanel;
            if (panel != null)
            {
                panel.InvalidateArrange();
                panel.InvalidateMeasure();
                panel.InvalidateVisual();
            }
            else
            {
                var element = dependencyObject as FrameworkElement;
                if (element != null && element.Parent != null)
                    InvalidateEverything(element.Parent, e);
            }
        }

        /// <summary>
        /// When overridden in a derived class, measures the size in layout required for child elements and determines a size for the <see cref="T:System.Windows.FrameworkElement"/>-derived class.
        /// </summary>
        /// <param name="availableSize">The available size that this element can give to child elements. Infinity can be specified as a value to indicate that the element will size to whatever content is available.</param>
        /// <returns>
        /// The size that this element determines it needs during layout, based on its calculations of child element sizes.
        /// </returns>
        protected override Size MeasureOverride(Size availableSize)
        {
            if (Children.Count != 2) return base.MeasureOverride(availableSize); // Not much we can do until we have exactly two elements;

            // First, we check whether we have any headers
            _headers.Clear();
            var header1 = GetCaption(Children[0]);
            var header2 = GetCaption(Children[1]);

            if (Orientation == Orientation.Vertical)
            {
                var maxHeaderHeight = 0d;
                if (!string.IsNullOrEmpty(header1) || !string.IsNullOrEmpty(header2))
                {
                    _headers.Add(new AutoHeaderTextRenderInfo { Text = header1, FormattedText = new FormattedText(header1, CultureInfo.CurrentUICulture, FlowDirection.LeftToRight, new Typeface(CaptionFontFamily, CaptionFontStyle, CaptionFontWeight, FontStretches.Normal), CaptionFontSize, CaptionForegroundBrush) });
                    _headers.Add(new AutoHeaderTextRenderInfo { Text = header2, FormattedText = new FormattedText(header2, CultureInfo.CurrentUICulture, FlowDirection.LeftToRight, new Typeface(CaptionFontFamily, CaptionFontStyle, CaptionFontWeight, FontStretches.Normal), CaptionFontSize, CaptionForegroundBrush) });
                    maxHeaderHeight = Math.Max(_headers[0].FormattedText.Height, _headers[1].FormattedText.Height);
                }

                var top = maxHeaderHeight + CaptionSpacing;
                var height = Math.Max(availableSize.Height - top, 0);
                var width = Math.Max((availableSize.Width - Spacing) / 2, 0);

                var measureSize = GeometryHelper.NewSize(width, height);
                Children[0].Measure(measureSize);
                Children[1].Measure(measureSize);
            }
            else
            {
                var top = 0d;
                var height = Math.Max((availableSize.Height - Spacing) / 2, 0);
                var height1 = height;
                var height2 = height;

                if (!string.IsNullOrEmpty(header1))
                {
                    var text1 = new AutoHeaderTextRenderInfo { Text = header1, FormattedText = new FormattedText(header1, CultureInfo.CurrentUICulture, FlowDirection.LeftToRight, new Typeface(CaptionFontFamily, CaptionFontStyle, CaptionFontWeight, FontStretches.Normal), CaptionFontSize, CaptionForegroundBrush) };
                    _headers.Add(text1);
                    text1.RenderRect = GeometryHelper.NewRect(0d, 0d, availableSize.Width, text1.FormattedText.Height);
                    top += text1.FormattedText.Height + CaptionSpacing;
                    height1 -= (text1.FormattedText.Height + CaptionSpacing);
                }

                Children[0].Measure(GeometryHelper.NewSize(availableSize.Width, height1));

                if (!string.IsNullOrEmpty(header2))
                {
                    var text2 = new AutoHeaderTextRenderInfo { Text = header2, FormattedText = new FormattedText(header2, CultureInfo.CurrentUICulture, FlowDirection.LeftToRight, new Typeface(CaptionFontFamily, CaptionFontStyle, CaptionFontWeight, FontStretches.Normal), CaptionFontSize, CaptionForegroundBrush) };
                    _headers.Add(text2);
                    text2.RenderRect = GeometryHelper.NewRect(0d, 0d, availableSize.Width, text2.FormattedText.Height);
                    top += text2.FormattedText.Height + CaptionSpacing;
                    height2 -= (text2.FormattedText.Height + CaptionSpacing);
                }

                Children[1].Measure(GeometryHelper.NewSize(availableSize.Width, height2));
            }

            return base.MeasureOverride(availableSize);
        }

        /// <summary>
        /// When overridden in a derived class, positions child elements and determines a size for a <see cref="T:System.Windows.FrameworkElement"/> derived class.
        /// </summary>
        /// <param name="finalSize">The final area within the parent that this element should use to arrange itself and its children.</param>
        /// <returns>
        /// The actual size used.
        /// </returns>
        protected override Size ArrangeOverride(Size finalSize)
        {
            if (Children.Count != 2) return base.ArrangeOverride(finalSize); // Not much we can do until we have exactly two elements;

            // First, we check whether we have any headers
            _headers.Clear();
            var header1 = GetCaption(Children[0]);
            var header2 = GetCaption(Children[1]);

            if (Orientation == Orientation.Vertical)
            {
                var maxHeaderHeight = 0d;
                if (!string.IsNullOrEmpty(header1) || !string.IsNullOrEmpty(header2))
                {
                    _headers.Add(new AutoHeaderTextRenderInfo { Text = header1, FormattedText = new FormattedText(header1, CultureInfo.CurrentUICulture, FlowDirection.LeftToRight, new Typeface(CaptionFontFamily, CaptionFontStyle, CaptionFontWeight, FontStretches.Normal), CaptionFontSize, CaptionForegroundBrush) });
                    _headers.Add(new AutoHeaderTextRenderInfo { Text = header2, FormattedText = new FormattedText(header2, CultureInfo.CurrentUICulture, FlowDirection.LeftToRight, new Typeface(CaptionFontFamily, CaptionFontStyle, CaptionFontWeight, FontStretches.Normal), CaptionFontSize, CaptionForegroundBrush) });
                    maxHeaderHeight = Math.Max(_headers[0].FormattedText.Height, _headers[1].FormattedText.Height);
                }

                var top = maxHeaderHeight + CaptionSpacing;
                var height = Math.Max(finalSize.Height - top, 0);
                var width = Math.Max((finalSize.Width - Spacing) / 2, 0);

                Children[0].Arrange(GeometryHelper.NewRect(0d, top, width, height));
                Children[1].Arrange(GeometryHelper.NewRect(width + Spacing, top, width, height));

                if (maxHeaderHeight > 0d)
                {
                    _headers[0].RenderRect = GeometryHelper.NewRect(0d, 0d, width, maxHeaderHeight);
                    _headers[1].RenderRect = GeometryHelper.NewRect(width + Spacing, 0d, width, maxHeaderHeight);
                }
            }
            else
            {
                var top = 0d;
                var height = Math.Max((finalSize.Height - Spacing) / 2, 0);
                var height1 = height;
                var height2 = height;

                if (!string.IsNullOrEmpty(header1))
                {
                    var text1 = new AutoHeaderTextRenderInfo { Text = header1, FormattedText = new FormattedText(header1, CultureInfo.CurrentUICulture, FlowDirection.LeftToRight, new Typeface(CaptionFontFamily, CaptionFontStyle, CaptionFontWeight, FontStretches.Normal), CaptionFontSize, CaptionForegroundBrush) };
                    _headers.Add(text1);
                    text1.RenderRect = GeometryHelper.NewRect(0d, 0d, finalSize.Width, text1.FormattedText.Height);
                    top += text1.FormattedText.Height + CaptionSpacing;
                    height1 -= (text1.FormattedText.Height + CaptionSpacing);
                }

                Children[0].Arrange(GeometryHelper.NewRect(0d, top, finalSize.Width, height1));

                if (!string.IsNullOrEmpty(header2))
                {
                    var text2 = new AutoHeaderTextRenderInfo { Text = header2, FormattedText = new FormattedText(header2, CultureInfo.CurrentUICulture, FlowDirection.LeftToRight, new Typeface(CaptionFontFamily, CaptionFontStyle, CaptionFontWeight, FontStretches.Normal), CaptionFontSize, CaptionForegroundBrush) };
                    _headers.Add(text2);
                    text2.RenderRect = GeometryHelper.NewRect(0d, 0d, finalSize.Width, text2.FormattedText.Height);
                    top += text2.FormattedText.Height + CaptionSpacing;
                    height2 -= (text2.FormattedText.Height + CaptionSpacing);
                }

                Children[1].Arrange(GeometryHelper.NewRect(0d, top, finalSize.Width, height2));
            }

            return finalSize;
        }

        private readonly List<AutoHeaderTextRenderInfo> _headers = new List<AutoHeaderTextRenderInfo>(); 

        /// <summary>
        /// Draws the content of a <see cref="T:System.Windows.Media.DrawingContext"/> object during the render pass of a <see cref="T:System.Windows.Controls.Panel"/> element.
        /// </summary>
        /// <param name="dc">The <see cref="T:System.Windows.Media.DrawingContext"/> object to draw.</param>
        protected override void OnRender(DrawingContext dc)
        {
            base.OnRender(dc);
            if (CaptionRenderer == null) CaptionRenderer = new AutoLayoutHeaderRenderer();

            var offset = new Point();

            foreach (var header in _headers)
                CaptionRenderer.RenderHeader(dc, header, 1d, offset);
        }
    }
}
