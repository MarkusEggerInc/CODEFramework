using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using CODE.Framework.Wpf.Layout;

namespace CODE.Framework.Wpf.Theme.Geek.Controls
{
    /// <summary>
    /// Special version of the primary secondary grid for the geek theme
    /// </summary>
    public class GeekPrimarySecondary : GridPrimarySecondary
    {
        /// <summary>Font used to render the primary title</summary>
        public FontFamily PrimaryTitleFont
        {
            get { return (FontFamily)GetValue(PrimaryTitleFontProperty); }
            set { SetValue(PrimaryTitleFontProperty, value); }
        }
        /// <summary>Font used to render the primary title</summary>
        public static readonly DependencyProperty PrimaryTitleFontProperty = DependencyProperty.Register("PrimaryTitleFont", typeof(FontFamily), typeof(GeekPrimarySecondary), new PropertyMetadata(new FontFamily("Segoe UI")));

        /// <summary>Font used to render the secondary title</summary>
        public FontFamily SecondaryTitleFont
        {
            get { return (FontFamily)GetValue(SecondaryTitleFontProperty); }
            set { SetValue(SecondaryTitleFontProperty, value); }
        }
        /// <summary>Font used to render the secondary title</summary>
        public static readonly DependencyProperty SecondaryTitleFontProperty = DependencyProperty.Register("SecondaryTitleFont", typeof(FontFamily), typeof(GeekPrimarySecondary), new PropertyMetadata(new FontFamily("Segoe UI")));

        /// <summary>Font size used to render the primary title</summary>
        public double PrimaryTitleFontSize
        {
            get { return (double)GetValue(PrimaryTitleFontSizeProperty); }
            set { SetValue(PrimaryTitleFontSizeProperty, value); }
        }
        /// <summary>Font size used to render the primary title</summary>
        public static readonly DependencyProperty PrimaryTitleFontSizeProperty = DependencyProperty.Register("PrimaryTitleFontSize", typeof(double), typeof(GeekPrimarySecondary), new PropertyMetadata(12d));

        /// <summary>Font size used to render the secondary title</summary>
        public double SecondaryTitleFontSize
        {
            get { return (double)GetValue(SecondaryTitleFontSizeProperty); }
            set { SetValue(SecondaryTitleFontSizeProperty, value); }
        }
        /// <summary>Font size used to render the secondary title</summary>
        public static readonly DependencyProperty SecondaryTitleFontSizeProperty = DependencyProperty.Register("SecondaryTitleFontSize", typeof(double), typeof(GeekPrimarySecondary), new PropertyMetadata(12d));

        /// <summary>Brush used to render the foreground of the primary title</summary>
        public Brush PrimaryTitleHeaderBrush
        {
            get { return (Brush)GetValue(PrimaryTitleHeaderBrushProperty); }
            set { SetValue(PrimaryTitleHeaderBrushProperty, value); }
        }
        /// <summary>Brush used to render the foreground of the primary title</summary>
        public static readonly DependencyProperty PrimaryTitleHeaderBrushProperty = DependencyProperty.Register("PrimaryTitleHeaderBrush", typeof(Brush), typeof(GeekPrimarySecondary), new PropertyMetadata(Brushes.White));

        /// <summary>Brush used to render the foreground of the primary footer</summary>
        public Brush PrimaryTitleFooterBrush
        {
            get { return (Brush)GetValue(PrimaryTitleFooterBrushProperty); }
            set { SetValue(PrimaryTitleFooterBrushProperty, value); }
        }
        /// <summary>Brush used to render the foreground of the primary title</summary>
        public static readonly DependencyProperty PrimaryTitleFooterBrushProperty = DependencyProperty.Register("PrimaryTitleFooterBrush", typeof(Brush), typeof(GeekPrimarySecondary), new PropertyMetadata(Brushes.Black));

        /// <summary>Brush used to render the foreground of the secondary title</summary>
        public Brush SecondaryTitleHeaderBrush
        {
            get { return (Brush)GetValue(SecondaryTitleHeaderBrushProperty); }
            set { SetValue(SecondaryTitleHeaderBrushProperty, value); }
        }
        /// <summary>Brush used to render the foreground of the secondary title</summary>
        public static readonly DependencyProperty SecondaryTitleHeaderBrushProperty = DependencyProperty.Register("SecondaryTitleHeaderBrush", typeof(Brush), typeof(GeekPrimarySecondary), new PropertyMetadata(Brushes.White));

        /// <summary>Brush used to render the foreground of the secondary footer</summary>
        public Brush SecondaryTitleFooterBrush
        {
            get { return (Brush)GetValue(SecondaryTitleFooterBrushProperty); }
            set { SetValue(SecondaryTitleFooterBrushProperty, value); }
        }
        /// <summary>Brush used to render the foreground of the secondary footer</summary>
        public static readonly DependencyProperty SecondaryTitleFooterBrushProperty = DependencyProperty.Register("SecondaryTitleFooterBrush", typeof(Brush), typeof(GeekPrimarySecondary), new PropertyMetadata(Brushes.Black));

        /// <summary>Brush used to render the primary area header background</summary>
        public Brush PrimaryAreaHeaderBackgroundBrush
        {
            get { return (Brush)GetValue(PrimaryAreaHeaderBackgroundBrushProperty); }
            set { SetValue(PrimaryAreaHeaderBackgroundBrushProperty, value); }
        }
        /// <summary>Brush used to render the primary area header background</summary>
        public static readonly DependencyProperty PrimaryAreaHeaderBackgroundBrushProperty = DependencyProperty.Register("PrimaryAreaHeaderBackgroundBrush", typeof(Brush), typeof(GeekPrimarySecondary), new PropertyMetadata(Brushes.Gray));

        /// <summary>Brush used to render the secondary area header background</summary>
        public Brush SecondaryAreaHeaderBackgroundBrush
        {
            get { return (Brush)GetValue(SecondaryAreaHeaderBackgroundBrushProperty); }
            set { SetValue(SecondaryAreaHeaderBackgroundBrushProperty, value); }
        }
        /// <summary>Brush used to render the secondary area header background</summary>
        public static readonly DependencyProperty SecondaryAreaHeaderBackgroundBrushProperty = DependencyProperty.Register("SecondaryAreaHeaderBackgroundBrush", typeof(Brush), typeof(GeekPrimarySecondary), new PropertyMetadata(Brushes.Gray));

        /// <summary>Margin around the primary element</summary>
        public Thickness PrimaryElementMargin
        {
            get { return (Thickness)GetValue(PrimaryElementMarginProperty); }
            set { SetValue(PrimaryElementMarginProperty, value); }
        }
        /// <summary>Margin around the primary element</summary>
        public static readonly DependencyProperty PrimaryElementMarginProperty = DependencyProperty.Register("PrimaryElementMargin", typeof(Thickness), typeof(GeekPrimarySecondary), new PropertyMetadata(new Thickness(0)));

        /// <summary>Margin around the secondary element</summary>
        public Thickness SecondaryElementMargin
        {
            get { return (Thickness)GetValue(SecondaryElementMarginProperty); }
            set { SetValue(SecondaryElementMarginProperty, value); }
        }
        /// <summary>Margin around the secondary element</summary>
        public static readonly DependencyProperty SecondaryElementMarginProperty = DependencyProperty.Register("SecondaryElementMargin", typeof(Thickness), typeof(GeekPrimarySecondary), new PropertyMetadata(new Thickness(0)));

        /// <summary>
        /// Assigns the elements.
        /// </summary>
        /// <param name="layout">The layout.</param>
        /// <param name="primary">The primary.</param>
        /// <param name="secondary">The secondary.</param>
        /// <param name="mustInvalidate">if set to <c>true</c> [must invalidate].</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise</returns>
        protected override bool AssignElements(PrimarySecondaryLayout layout, UIElement primary, IEnumerable<UIElement> secondary, bool mustInvalidate)
        {
            var result = base.AssignElements(layout, primary, secondary, mustInvalidate);

            var element1 = primary as FrameworkElement;
            if (element1 != null) element1.Margin = PrimaryElementMargin;

            var element2 = secondary.FirstOrDefault() as FrameworkElement;
            if (element2 != null) element2.Margin = SecondaryElementMargin;

            return result;
        }

        /// <summary>
        /// This method draws additional elements into the main display area
        /// </summary>
        /// <param name="dc">Drawing context</param>
        protected override void CustomRender(DrawingContext dc)
        {
            var primaryTitle = string.Empty;
            var secondaryTitle = string.Empty;

            foreach (UIElement element in Children)
            {
                var elementType = SimpleView.GetUIElementType(element);
                if (elementType == UIElementTypes.Primary)
                {
                    primaryTitle = SimpleView.GetUIElementTitle(element);
                    break;
                }
            }
            foreach (UIElement element in Children)
            {
                var elementType = SimpleView.GetUIElementType(element);
                if (elementType == UIElementTypes.Secondary)
                {
                    secondaryTitle = SimpleView.GetUIElementTitle(element);
                    break;
                }
            }

            var primaryTitleFont = PrimaryTitleFont;
            var primaryTitleFontSize = PrimaryTitleFontSize;
            var primaryTitleBrush1 = PrimaryTitleHeaderBrush;
            var primaryTitleBrush2 = PrimaryTitleFooterBrush;
            var primaryHeaderBackgroundBrush = PrimaryAreaHeaderBackgroundBrush;
            var secondaryTitleFont = SecondaryTitleFont;
            var secondaryTitleFontSize = PrimaryTitleFontSize;
            var secondaryTitleBrush1 = SecondaryTitleHeaderBrush;
            var secondaryTitleBrush2 = SecondaryTitleFooterBrush;
            var secondaryHeaderBackgroundBrush = SecondaryAreaHeaderBackgroundBrush;

            // Calculating the general areas
            var primaryRect = new Rect();
            switch (CalculatedLayout)
            {
                case PrimarySecondaryLayout.PrimaryTopSecondaryBottom:
                    primaryRect.Y = 0d;
                    primaryRect.X = 0d;
                    primaryRect.Height = RowDefinitions[0].ActualHeight + RowDefinitions[1].ActualHeight;
                    primaryRect.Width = ActualWidth;
                    break;
                case PrimarySecondaryLayout.SecondaryTopPrimaryBottom:
                    primaryRect.Y = RowDefinitions[0].ActualHeight;
                    primaryRect.X = 0d;
                    primaryRect.Height = RowDefinitions[1].ActualHeight + RowDefinitions[2].ActualHeight;
                    primaryRect.Width = ActualWidth;
                    break;
                case PrimarySecondaryLayout.PrimaryLeftSecondaryRight:
                    primaryRect.Y = 0d;
                    primaryRect.X = 0d;
                    primaryRect.Height = ActualHeight;
                    primaryRect.Width = ColumnDefinitions[0].ActualWidth + ColumnDefinitions[1].ActualWidth;
                    break;
                case PrimarySecondaryLayout.SecondaryLeftPrimaryRight:
                    primaryRect.Y = 0d;
                    primaryRect.X = ColumnDefinitions[0].ActualWidth;
                    primaryRect.Height = ActualHeight;
                    primaryRect.Width = ColumnDefinitions[1].ActualWidth + ColumnDefinitions[2].ActualWidth;
                    break;
            }

            var secondaryRect = new Rect();
            switch (CalculatedLayout)
            {
                case PrimarySecondaryLayout.PrimaryTopSecondaryBottom:
                    secondaryRect.Y = RowDefinitions[0].ActualHeight + RowDefinitions[1].ActualHeight;
                    secondaryRect.X = 0d;
                    secondaryRect.Height = RowDefinitions[2].ActualHeight;
                    secondaryRect.Width = ActualWidth;
                    break;
                case PrimarySecondaryLayout.SecondaryTopPrimaryBottom:
                    secondaryRect.Y = 0d;
                    secondaryRect.X = 0d;
                    secondaryRect.Height = RowDefinitions[0].ActualHeight;
                    secondaryRect.Width = ActualWidth;
                    break;
                case PrimarySecondaryLayout.PrimaryLeftSecondaryRight:
                    secondaryRect.Y = 0d;
                    secondaryRect.X = ColumnDefinitions[0].ActualWidth + ColumnDefinitions[1].ActualWidth;
                    secondaryRect.Height = ActualHeight;
                    secondaryRect.Width = ColumnDefinitions[2].ActualWidth;
                    break;
                case PrimarySecondaryLayout.SecondaryLeftPrimaryRight:
                    secondaryRect.Y = 0d;
                    secondaryRect.X = 0d;
                    secondaryRect.Height = ActualHeight;
                    secondaryRect.Width = ColumnDefinitions[0].ActualWidth;
                    break;
            }

            if (!string.IsNullOrEmpty(primaryTitle))
            {
                var primaryFormattedText1 = new FormattedText(primaryTitle, CultureInfo.CurrentUICulture, FlowDirection.LeftToRight, new Typeface(primaryTitleFont, FontStyles.Normal, FontWeights.Normal, FontStretches.Normal), primaryTitleFontSize, primaryTitleBrush1) { MaxTextWidth = secondaryRect.Width - 4, Trimming = TextTrimming.CharacterEllipsis };
                var primaryFormattedText2 = new FormattedText(primaryTitle, CultureInfo.CurrentUICulture, FlowDirection.LeftToRight, new Typeface(primaryTitleFont, FontStyles.Normal, FontWeights.Normal, FontStretches.Normal), primaryTitleFontSize, primaryTitleBrush2) { MaxTextWidth = secondaryRect.Width - 4, Trimming = TextTrimming.CharacterEllipsis };

                var headerRect = new Rect(primaryRect.X + 2, primaryRect.Y + 2, primaryRect.Width - 4, primaryFormattedText1.Height + 4);
                dc.DrawRectangle(primaryHeaderBackgroundBrush, null, headerRect);
                dc.DrawText(primaryFormattedText1, new Point(primaryRect.X + 4, primaryRect.Y + 3));

                var mainAreaRect = new Rect(primaryRect.X + 2, primaryRect.Y + headerRect.Height + 4, primaryRect.Width - 4, primaryRect.Height - 2 - headerRect.Height - primaryFormattedText2.Height - 4);
                dc.DrawRectangle(PrimaryAreaBackgroundBrush, null, mainAreaRect);

                var tabRect = new Rect(primaryRect.X + 2, primaryRect.Y + primaryRect.Height - primaryFormattedText2.Height - 4, Math.Min(primaryRect.Width, primaryFormattedText2.Width + 10), primaryFormattedText2.Height + 4);
                dc.DrawRectangle(PrimaryAreaBackgroundBrush, null, tabRect);
                dc.DrawText(primaryFormattedText2, new Point(tabRect.X + 4, tabRect.Y + 2));
            }
            else if (PrimaryAreaBackgroundBrush != null)
                dc.DrawRectangle(PrimaryAreaBackgroundBrush, null, primaryRect);

            if (!string.IsNullOrEmpty(secondaryTitle))
            {
                var secondaryFormattedText1 = new FormattedText(secondaryTitle, CultureInfo.CurrentUICulture, FlowDirection.LeftToRight, new Typeface(secondaryTitleFont, FontStyles.Normal, FontWeights.Normal, FontStretches.Normal), secondaryTitleFontSize, secondaryTitleBrush1) {MaxTextWidth = secondaryRect.Width - 4, Trimming = TextTrimming.CharacterEllipsis};
                var secondaryFormattedText2 = new FormattedText(secondaryTitle, CultureInfo.CurrentUICulture, FlowDirection.LeftToRight, new Typeface(secondaryTitleFont, FontStyles.Normal, FontWeights.Normal, FontStretches.Normal), secondaryTitleFontSize, secondaryTitleBrush2) {MaxTextWidth = secondaryRect.Width - 4, Trimming = TextTrimming.CharacterEllipsis};

                var headerRect = new Rect(secondaryRect.X + 2, secondaryRect.Y + 2, secondaryRect.Width - 4, secondaryFormattedText1.Height + 4);
                dc.DrawRectangle(secondaryHeaderBackgroundBrush, null, headerRect);
                dc.DrawText(secondaryFormattedText1, new Point(secondaryRect.X + 4, secondaryRect.Y + 3));

                var mainAreaRect = new Rect(secondaryRect.X + 2, secondaryRect.Y + headerRect.Height + 4, secondaryRect.Width - 4, secondaryRect.Height - 2 - headerRect.Height - secondaryFormattedText2.Height - 4);
                dc.DrawRectangle(SecondaryAreaBackgroundBrush, null, mainAreaRect);

                var tabRect = new Rect(secondaryRect.X + 2, secondaryRect.Y + secondaryRect.Height - secondaryFormattedText2.Height - 4, Math.Min(secondaryRect.Width, secondaryFormattedText2.Width + 10), secondaryFormattedText2.Height + 4);
                dc.DrawRectangle(SecondaryAreaBackgroundBrush, null, tabRect);
                dc.DrawText(secondaryFormattedText2, new Point(tabRect.X + 4, tabRect.Y + 2));
            }
            else if (SecondaryAreaBackgroundBrush != null)
                dc.DrawRectangle(SecondaryAreaBackgroundBrush, null, secondaryRect);
        }
    }
}
