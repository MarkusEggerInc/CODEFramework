using System.Windows;
using System.Windows.Controls;

namespace CODE.Framework.Wpf.Documents
{
    /// <summary>
    /// TextBlock that can be populated from HTML
    /// </summary>
    public class HtmlTextBlock : TextBlock
    {
        /// <summary>HTML string (fragment with limited HTML support)</summary>
        public string Html
        {
            get { return (string) GetValue(HtmlProperty); }
            set { SetValue(HtmlProperty, value); }
        }

        /// <summary>HTML string (fragment with limited HTML support)</summary>
        public static readonly DependencyProperty HtmlProperty = DependencyProperty.Register("Html", typeof (string), typeof (HtmlTextBlock), new UIPropertyMetadata("", RepopulateInlines));

        /// <summary>Re-creates the actual paragraph inlines based on the HTML as well as leading and trailing inlines</summary>
        /// <param name="source">Special Paragraph object</param>
        /// <param name="e">Event arguments</param>
        private static void RepopulateInlines(DependencyObject source, DependencyPropertyChangedEventArgs e)
        {
            var textBlock = source as HtmlTextBlock;
            if (textBlock == null) return;

            textBlock.Inlines.Clear();

            textBlock.Inlines.AddRange(textBlock.Html.ToSimplifiedInlines());
        }
    }
}