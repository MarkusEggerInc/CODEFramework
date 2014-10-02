using System.Windows;
using System.Windows.Documents;
using System.Windows.Markup;

namespace CODE.Framework.Wpf.Documents
{
    /// <summary>Paragraph object capable of displaying simple HTML fragments as part of Flow Documents</summary>
    [ContentProperty("Html")]
    public class HtmlParagraph : Paragraph
    {
        /// <summary>HTML string (fragment with limited HTML support)</summary>
        public string Html
        {
            get { return (string)GetValue(HtmlProperty); }
            set { SetValue(HtmlProperty, value); }
        }
        /// <summary>HTML string (fragment with limited HTML support)</summary>
        public static readonly DependencyProperty HtmlProperty = DependencyProperty.Register("Html", typeof(string), typeof(HtmlParagraph), new UIPropertyMetadata("", RepopulateInlines));

        /// <summary>Leading HTML string (fragment with limited HTML support)</summary>
        public string LeadingHtml
        {
            get { return (string)GetValue(LeadingHtmlProperty); }
            set { SetValue(LeadingHtmlProperty, value); }
        }
        /// <summary>Leading HTML string (fragment with limited HTML support)</summary>
        public static readonly DependencyProperty LeadingHtmlProperty = DependencyProperty.Register("LeadingHtml", typeof(string), typeof(HtmlParagraph), new UIPropertyMetadata("", RepopulateInlines));

        /// <summary>Trailing HTML string (fragment with limited HTML support)</summary>
        public string TrailingHtml
        {
            get { return (string)GetValue(TrailingHtmlProperty); }
            set { SetValue(TrailingHtmlProperty, value); }
        }
        /// <summary>Trailing HTML string (fragment with limited HTML support)</summary>
        public static readonly DependencyProperty TrailingHtmlProperty = DependencyProperty.Register("TrailingHtml", typeof(string), typeof(HtmlParagraph), new UIPropertyMetadata("", RepopulateInlines));

        /// <summary>Defines whether spaces are trimmed off at the start of the paragraph</summary>
        public bool TrimLeadingSpaces
        {
            get { return (bool)GetValue(TrimLeadingSpacesProperty); }
            set { SetValue(TrimLeadingSpacesProperty, value); }
        }
        /// <summary>Defines whether spaces are trimmed off at the start of the paragraph</summary>
        public static readonly DependencyProperty TrimLeadingSpacesProperty = DependencyProperty.Register("TrimLeadingSpaces", typeof(bool), typeof(HtmlParagraph), new UIPropertyMetadata(true, RepopulateInlines));

        /// <summary>Defines whether spaces are trimmed off at the start of the paragraph</summary>
        public bool TrimLeadingTabs
        {
            get { return (bool)GetValue(TrimLeadingTabsProperty); }
            set { SetValue(TrimLeadingTabsProperty, value); }
        }
        /// <summary>Defines whether spaces are trimmed off at the start of the paragraph</summary>
        public static readonly DependencyProperty TrimLeadingTabsProperty = DependencyProperty.Register("TrimLeadingTabs", typeof(bool), typeof(HtmlParagraph), new UIPropertyMetadata(true, RepopulateInlines));

        /// <summary>Re-creates the actual paragraph inlines based on the HTML as well as leading and trailing inlines</summary>
        /// <param name="source">Special Paragraph object</param>
        /// <param name="e">Event arguments</param>
        private static void RepopulateInlines(DependencyObject source, DependencyPropertyChangedEventArgs e)
        {
            var paragraph = source as HtmlParagraph;
            if (paragraph == null) return;

            paragraph.Inlines.Clear();

            paragraph.Inlines.AddRange(paragraph.LeadingHtml.ToInlines(paragraph.TrimLeadingSpaces, paragraph.TrimLeadingTabs));
            paragraph.Inlines.AddRange(paragraph.Html.ToInlines(paragraph.TrimLeadingSpaces, paragraph.TrimLeadingTabs));
            paragraph.Inlines.AddRange(paragraph.TrailingHtml.ToInlines(paragraph.TrimLeadingSpaces, paragraph.TrimLeadingTabs));
        }
    }

}
