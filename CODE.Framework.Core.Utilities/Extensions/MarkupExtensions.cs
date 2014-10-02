using System;

namespace CODE.Framework.Core.Utilities.Extensions
{
    /// <summary>
    /// Extension methods providing MarkupHelper functionality
    /// </summary>
    public static class MarkupExtensions
    {
        /// <summary>
        /// Strips all markup tags from the provided string
        /// </summary>
        /// <param name="markup">A string with markup</param>
        /// <returns>Text without markup</returns>
        /// <example>
        /// Example markup:
        /// &lt;html&gt;&lt;body style="color: black;"&gt;&lt;p&gt;This is some text&lt;/p&gt;&lt;/body&gt;&lt;/html&gt;
        /// Result after markup-strip:
        /// This is some text
        /// </example>
        /// <remarks>This method can be used as an extension method.</remarks>
        public static string StripMarkup(this string markup)
        {
            return MarkupHelper.StripMarkup(markup);
        }

        /// <summary>
        /// Inspects the provided HTML for start and end body-tags.
        /// If those tags are present, only the content within the body tags (inner HTML)
        /// is included int he return value.
        /// </summary>
        /// <param name="html">The HTML.</param>
        /// <returns>Body portion of the HTML</returns>
        /// <remarks>
        /// This method is very helpful whenever a potentially complete HTML string needs to be
        /// displayed as a portion of a larger HTML output.
        /// </remarks>
        public static string GetBodyOnly(this string html)
        {
            return MarkupHelper.GetBodyOnly(html);
        }

        /// <summary>
        /// Removes non-body elements from HTML and then strips all markup and returns the result.
        /// </summary>
        /// <param name="html">The HTML.</param>
        /// <returns>Body text without markup</returns>
        /// <example>
        /// Example markup:
        /// &lt;html&gt;&lt;head&gt;&lt;title&gt;Document Title&lt;/title&gt;&lt;head&gt;&lt;body style="color: black;"&gt;&lt;p&gt;This is some text&lt;/p&gt;&lt;/body&gt;&lt;/html&gt;
        /// Result after markup-strip:
        /// This is some text
        /// </example>
        /// <remarks>
        /// This method is very helpful whenever a potentially complete HTML string needs to be
        /// displayed as a portion of a larger HTML output.
        /// </remarks>
        public static string GetStrippedBodyOnly(this string html)
        {
            return MarkupHelper.GetStrippedBodyOnly(html);
        }

        /// <summary>
        /// Creates a time string formatted for RSS feeds
        /// </summary>
        /// <param name="date">Date to convert</param>
        /// <returns>Converted date</returns>
        /// <example>
        /// string rssDate = MarkupHelper.GetRssFormattedDateTime(DateTime.Now);
        /// </example>
        public static string GetRssFormattedDateTime(this DateTime date)
        {
            return MarkupHelper.GetRssFormattedDateTime(date);
        }

        /// <summary>
        /// Returns a date formatted according to RSS rules
        /// </summary>
        /// <param name="date">The date.</param>
        /// <returns>RSS formatted date</returns>
        /// <example>
        /// using EPS.Utilities;
        /// 
        /// // ... more code
        /// 
        /// string rssDate = DateTime.Now.ToRssDate();
        /// </example>
        public static string ToRssData(this DateTime date)
        {
            return MarkupHelper.ToRssData(date);
        }

        /// <summary>
        /// Converts plain Text to HTML. Includes handling of line breaks and such.
        /// </summary>
        /// <param name="text">Text to convert</param>
        /// <returns>HTML</returns>
        public static string ToHtml(this string text)
        {
            return MarkupHelper.PlainTextToHtml(text);
        }

        /// <summary>
        /// Encodes strings for use in HTML
        /// </summary>
        /// <param name="text">Text to convert</param>
        /// <returns>HTML-encoded string</returns>
        public static string HtmlEncode(this string text)
        {
            return MarkupHelper.HtmlEncode(text);
        }

        /// <summary>
        /// Encodes strings for use in XML
        /// </summary>
        /// <param name="text">Text to convert</param>
        /// <returns>XML-encoded string</returns>
        /// <remarks>Identical to HtmlEncode()</remarks>
        public static string XmlEncode(this string text)
        {
            return MarkupHelper.XmlEncode(text);
        }
    }
}
