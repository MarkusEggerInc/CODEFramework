using System;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace CODE.Framework.Core.Utilities
{
    /// <summary>
    /// This helper class provides all kinds of features useful for markup (HTML, RSS, XAML, XML,...) processing
    /// </summary>
    public static class MarkupHelper
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
        public static string StripMarkup(string markup)
        {
            return Regex.Replace(markup, @"<(.|\n)*?>", string.Empty);
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
        public static string GetBodyOnly(string html)
        {
            if (html.IndexOf("<BODY>", StringComparison.OrdinalIgnoreCase) > 0)
                html = html.Substring(html.IndexOf("<BODY>", StringComparison.OrdinalIgnoreCase) + 6);
            if (html.IndexOf("</BODY>", StringComparison.OrdinalIgnoreCase) > 0)
                html = html.Substring(0, html.IndexOf("</BODY>", StringComparison.OrdinalIgnoreCase));
            return html;
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
        public static string GetStrippedBodyOnly(string html)
        {
            return StripMarkup(GetBodyOnly(html));
        }

        /// <summary>
        /// Creates a time string formatted for RSS feeds
        /// </summary>
        /// <param name="date">Date to convert</param>
        /// <returns>Converted date</returns>
        /// <example>
        /// string rssDate = MarkupHelper.GetRssFormattedDateTime(DateTime.Now);
        /// </example>
        public static string GetRssFormattedDateTime(DateTime date)
        {
            date = date.ToUniversalTime();
            var sb = new StringBuilder();
            sb.Append(StringHelper.ToString(date.DayOfWeek).Substring(0, 3) + ", ");
            sb.Append(StringHelper.ToString(date.Day) + " ");
            string month = date.ToLongDateString();
            month = month.Substring(month.IndexOf(", ", StringComparison.OrdinalIgnoreCase) + 2, 3);
            sb.Append(month + " ");
            sb.Append(StringHelper.ToString(date.Year) + " ");
            sb.Append(StringHelper.ToString(date.Hour));
            sb.Append(":" + StringHelper.ToString(date.Minute));
            sb.Append(":" + StringHelper.ToString(date.Second) + " GMT");
            return sb.ToString();
        }

        /// <summary>
        /// Returns a date formatted according to RSS rules
        /// </summary>
        /// <param name="date">The date.</param>
        /// <returns>RSS formatted date</returns>
        /// <example>
        /// string rssDate = MarkupHelper.ToRssDate(DateTime.Now);
        /// </example>
        public static string ToRssData(DateTime date)
        {
            return GetRssFormattedDateTime(date);
        }

        /// <summary>XML-encodes a string and returns the encoded string.</summary>
        /// <param name="text">The text string to encode. </param>
        /// <returns>The XML-encoded text.</returns>
        /// <remarks>Identical to HtmlEncode()</remarks>
        public static string XmlEncode(string text)
        {
            return HtmlEncode(text);
        }

        /// <summary>HTML-encodes a string and returns the encoded string.</summary>
        /// <param name="text">The text string to encode. </param>
        /// <returns>The HTML-encoded text.</returns>
        public static string HtmlEncode(string text)
        {
            if (text == null) return null;

            var sb = new StringBuilder(text.Length);

            int len = text.Length;
            for (int i = 0; i < len; i++)
            {
                switch (text[i])
                {
                    case '<':
                        sb.Append("&lt;");
                        break;
                    case '>':
                        sb.Append("&gt;");
                        break;
                    case '"':
                        sb.Append("&quot;");
                        break;
                    case '&':
                        sb.Append("&amp;");
                        break;
                    default:
                        if (text[i] > 159)
                        {
                            // decimal numeric entity
                            sb.Append("&#");
                            sb.Append(((int)text[i]).ToString(CultureInfo.InvariantCulture));
                            sb.Append(";");
                        }
                        else
                            sb.Append(text[i]);
                        break;
                }
            }
            return sb.ToString();
        }

        /// <summary>
        /// Converts plain text to HTML
        /// </summary>
        /// <param name="text">The plain text.</param>
        /// <returns>HTML</returns>
        /// <example>
        /// string myText = "Hello World!\r\n\r\nText > HTML";
        /// string myHtml = MarkupHelper.PlainTextToHtml(myText);
        /// </example>
        public static string PlainTextToHtml(string text)
        {
            var lines = text.Split('\n');
            var sb = new StringBuilder();
            int counter = 0;
            foreach (string line in lines)
            {
                sb.Append(HtmlEncode(line.Trim()));
                if (counter < lines.Length - 1)
                    sb.Append("<br />");
                counter++;
            }
            return sb.ToString();
        }
    }
}
