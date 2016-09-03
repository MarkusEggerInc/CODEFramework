using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace CODE.Framework.Wpf.Controls
{
    /// <summary>
    /// This control can pick up any content and attempts to turn it into text and displays it
    /// </summary>
    public class ContentTextDisplay : Control
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ContentTextDisplay"/> class.
        /// </summary>
        public ContentTextDisplay()
        {
            Focusable = false;
        }

        /// <summary>
        /// Content to be displayed
        /// </summary>
        /// <value>The content.</value>
        public object Content
        {
            get { return GetValue(ContentProperty); }
            set { SetValue(ContentProperty, value); }
        }

        /// <summary>
        /// Content to be displayed
        /// </summary>
        public static readonly DependencyProperty ContentProperty = DependencyProperty.Register("Content", typeof (object), typeof (ContentTextDisplay), new PropertyMetadata(null, InvalidateDisplay));

        /// <summary>
        /// Maximum line count for the display of the content text
        /// </summary>
        /// <value>The maximum line count.</value>
        public int MaxLineCount
        {
            get { return (int) GetValue(MaxLineCountProperty); }
            set { SetValue(MaxLineCountProperty, value); }
        }
        /// <summary>
        /// Maximum line count for the display of the content text
        /// </summary>
        public static readonly DependencyProperty MaxLineCountProperty = DependencyProperty.Register("MaxLineCount", typeof (int), typeof (ContentTextDisplay), new PropertyMetadata(1, InvalidateDisplay));

        /// <summary>
        /// Fires when the content changes
        /// </summary>
        /// <param name="d">The control the content changed on</param>
        /// <param name="args">The <see cref="DependencyPropertyChangedEventArgs"/> instance containing the event data.</param>
        private static void InvalidateDisplay(DependencyObject d, DependencyPropertyChangedEventArgs args)
        {
            var control = d as ContentTextDisplay;
            if (control == null) return;
            control.InvalidateVisual();
        }

        /// <summary>
        /// Called to remeasure a control.
        /// </summary>
        /// <param name="constraint">The maximum size that the method can return.</param>
        /// <returns>The size of the control, up to the maximum specified by <paramref name="constraint" />.</returns>
        protected override Size MeasureOverride(Size constraint)
        {
            base.MeasureOverride(constraint);
            var desiredSize = GetTextDimensions(constraint.Width);
            return desiredSize;
        }

        private Size GetTextDimensions(double maxWidth)
        {
            if (double.IsInfinity(maxWidth)) maxWidth = 1000000d;
            
            var displayText = GetDisplayText();
            var textParts = displayText.Split(' ');
            var textWidths = new double[textParts.Length];

            var ft = GetFormattedText("-", 100000d, 1);
            var spaceWidth = ft.Width;
            var singleLineHeight = ft.Height;

            for (var counter = 0; counter < textParts.Length; counter++)
            {
                ft = GetFormattedText(textParts[counter], 100000d, 1);
                textWidths[counter] = ft.Width;
            }
            var totalTextLength = 0d;
            for (var counter = 0; counter < textParts.Length; counter++)
            {
                if (counter > 0) totalTextLength += spaceWidth;
                totalTextLength += textWidths[counter];
            }

            // If we have one line max, it is what it is
            if (MaxLineCount == 1) return new Size(Math.Min(maxWidth, totalTextLength), singleLineHeight);

            // If we have only a single word, it is trivial
            if (textWidths.Length == 1) return new Size(Math.Min(maxWidth, textWidths[0]), singleLineHeight);

            // We can also do a quick check for two words
            if (textWidths.Length == 2)
            {
                if (totalTextLength <= MinWidth) return new Size(totalTextLength, singleLineHeight);
                if (MaxLineCount < 2) return new Size(maxWidth, singleLineHeight);
                return new Size(Math.Max(textWidths[0], textWidths[1]), singleLineHeight*2);
            }

            // The above scenarios probably handled most likely cases, but apparently we got past that, and now we need to do some math

            // If even a completely even distribution of the text (each line has the exact same length... which would be the ideal case)
            // is too long if we divide by the number of lines, then all scenarios exceed the max width available, so we can simply assume the max.
            if (totalTextLength / MaxLineCount > maxWidth) return new Size(maxWidth, singleLineHeight * MaxLineCount);

            // Still going? We now need to run through all the permutations
            var permutations = new List<LinePermutation>();
            var permutationIndex = 0;
            var maxLineCount = MaxLineCount;
            while (true)
            {
                var permutation = new LinePermutation(maxLineCount, spaceWidth, textWidths, permutationIndex);
                if (!permutation.IsValidPermutation) break;
                permutations.Add(permutation);
                permutationIndex++;
            }

            var shortestPermutationWidth = permutations.Min(l => l.Width);
            return new Size(shortestPermutationWidth, singleLineHeight * MaxLineCount);
        }

        private class LinePermutation
        {
            public LinePermutation(int lineCount, double spaceWidth, double[] widths, int permutationIndex)
            {
                Lines = new List<TextWidthLine>(lineCount);
                for (var counter = 0; counter < lineCount; counter++) Lines.Add(new TextWidthLine(spaceWidth));

                if (permutationIndex == 0) // Original version
                {
                    foreach (var t in widths) Lines[0].Widths.Add(t);
                    IsValidPermutation = true;
                    return;
                }

                if (lineCount == 1 && permutationIndex != 0) // There is only one permutation of a single line
                {
                    IsValidPermutation = false;
                    return;
                }

                if (lineCount == 2)
                {
                    if (widths.Length - permutationIndex < 1)
                    {
                        IsValidPermutation = false;
                        return;
                    }
                    for (var counter = 0; counter < widths.Length - permutationIndex; counter++) Lines[0].Widths.Add(widths[counter]);
                    for (var counter = widths.Length - permutationIndex; counter < widths.Length; counter++) Lines[1].Widths.Add(widths[counter]);
                    IsValidPermutation = true;
                    return;
                }

                IsValidPermutation = false;
            }

            public override string ToString()
            {
                var sb = new StringBuilder();

                foreach (var line in Lines)
                {
                    sb.Append("Width: " + line.Width + " (");
                    foreach (var width in line.Widths) sb.Append(width + " - ");
                    sb.Append(")\r\n");
                }

                return sb.ToString();
            }

            public bool IsValidPermutation { get; private set; }

            private List<TextWidthLine> Lines { get; set; }

            public double Width
            {
                get
                {
                    return Lines.Max(l => l.Width);
                }
            }
        }
        
        private class TextWidthLine
        {
            private readonly double _spaceWidth;

            public TextWidthLine(double spaceWidth)
            {
                _spaceWidth = spaceWidth;
                Widths = new List<double>();
            }

            public double Width
            {
                get
                {
                    var totalWidth = 0d;
                    for (var counter = 0; counter < Widths.Count; counter++)
                    {
                        if (counter > 0) totalWidth += _spaceWidth;
                        totalWidth += Widths[counter];
                    }
                    return totalWidth;
                }
            }

            public List<double> Widths { get; set; }
        }

        /// <summary>
        /// Gets the formatted text.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <param name="maxWidth">The maximum width.</param>
        /// <param name="maxLineCount">The maximum line count.</param>
        /// <returns>FormattedText.</returns>
        private FormattedText GetFormattedText(string text, double maxWidth, int maxLineCount)
        {
            return new FormattedText(text, CultureInfo.CurrentUICulture, FlowDirection.LeftToRight, new Typeface(FontFamily, FontStyle, FontWeight, FontStretch), FontSize, Foreground) {MaxLineCount = maxLineCount, MaxTextWidth = maxWidth};
        }

        /// <summary>
        /// Gets the display text.
        /// </summary>
        /// <returns>System.String.</returns>
        private string GetDisplayText()
        {
            return Content.ToString();
        }

        /// <summary>
        /// Called when the control needs to be rendered.
        /// </summary>
        /// <param name="drawingContext">The drawing context.</param>
        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);

            var ft = GetFormattedText(GetDisplayText(), Math.Min(ActualWidth, MaxWidth), MaxLineCount);
            ft.TextAlignment = TextAlignment.Center;
            ft.Trimming = TextTrimming.CharacterEllipsis;
            var top = (int) ((ActualHeight - ft.Height)/2);
            drawingContext.DrawText(ft, new Point(0, top));
        }
    }
}