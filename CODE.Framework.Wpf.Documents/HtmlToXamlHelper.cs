using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using CODE.Framework.Core.Utilities;

namespace CODE.Framework.Wpf.Documents
{
    /// <summary>
    /// This helper class provides various extension methods useful in converting HMTL to XAML
    /// </summary>
    public static class HtmlToXamlHelper
    {
        /// <summary>
        /// Converts HTML to a simple text version presented as WPF inlines
        /// </summary>
        /// <param name="html">The HTML.</param>
        /// <param name="leadingHtml">The leading HTML.</param>
        /// <param name="trailingHtml">The trailing HTML.</param>
        /// <param name="trimLeadingSpaces">if set to <c>true</c> [trim leading spaces].</param>
        /// <param name="trimLeadingTabs">if set to <c>true</c> [trim leading tabs].</param>
        /// <returns>IEnumerable{Inline}.</returns>
        public static IEnumerable<Inline> ToSimplifiedInlines(this string html, string leadingHtml = "", string trailingHtml = "", bool trimLeadingSpaces = true, bool trimLeadingTabs = true)
        {
            var blocks = new List<Inline>();

            var hasParagraphTags = html.IndexOf("<p>", StringComparison.Ordinal) > -1 || html.IndexOf("<P>", StringComparison.Ordinal) > -1;

            if (!hasParagraphTags) return html.ToInlines();

            html = html.Replace("<P>", "<p>");
            html = html.Replace("</P>", "</p>");
            if (!html.StartsWith("<p>")) html = "<p>" + html;
            if (!html.EndsWith("</p>")) html += "</p>";

            var paragraphs = new List<string>();
            while (!string.IsNullOrEmpty(html))
            {
                var at = html.IndexOf("</p>", StringComparison.Ordinal);
                if (at > -1)
                {
                    var paragraphHtml = html.Substring(0, at + 4);
                    paragraphs.Add(paragraphHtml);
                    html = html.Length > at + 4 ? html.Substring(at + 4) : string.Empty;
                }
                else
                {
                    paragraphs.Add(html);
                    html = string.Empty;
                }
            }

            foreach (var paragraph in paragraphs)
            {
                if (blocks.Count > 0) blocks.Add(new LineBreak());
                var innerParagraph = paragraph.Replace("<p>", string.Empty).Replace("</p>", string.Empty);
                blocks.AddRange(innerParagraph.ToInlines());
            }

            return blocks;
        }

        /// <summary>
        /// Converts HTML to WPF blocks
        /// </summary>
        /// <param name="html">The HTML.</param>
        /// <param name="leadingHtml">The leading HTML.</param>
        /// <param name="trailingHtml">The trailing HTML.</param>
        /// <param name="trimLeadingSpaces">if set to <c>true</c> [trim leading spaces].</param>
        /// <param name="trimLeadingTabs">if set to <c>true</c> [trim leading tabs].</param>
        /// <returns>IEnumerable{Block}.</returns>
        public static IEnumerable<Block> ToBlocks(this string html, string leadingHtml = "", string trailingHtml = "", bool trimLeadingSpaces = true, bool trimLeadingTabs = false)
        {
            var blocks = new List<Block>();

            //if (html == null) html = string.Empty;
            if (string.IsNullOrEmpty(html)) return blocks;

            var hasParagraphTags = html.IndexOf("<p>", StringComparison.Ordinal) > -1 || html.IndexOf("<P>", StringComparison.Ordinal) > -1;

            if (!hasParagraphTags)
            {
                var para = new Paragraph();
                if (!string.IsNullOrEmpty(leadingHtml)) para.Inlines.AddRange(leadingHtml.ToInlines());
                para.Inlines.AddRange(html.ToInlines());
                if (!string.IsNullOrEmpty(trailingHtml)) para.Inlines.AddRange(trailingHtml.ToInlines());
                blocks.Add(para);
            }
            else
            {
                html = html.Replace("<P>", "<p>");
                html = html.Replace("</P>", "</p>");
                if (!html.StartsWith("<p>")) html = "<p>" + html;
                if (!html.EndsWith("</p>")) html += "</p>";

                var paragraphs = new List<string>();
                while (!string.IsNullOrEmpty(html))
                {
                    var at = html.IndexOf("</p>", StringComparison.Ordinal);
                    if (at > -1)
                    {
                        var paragraphHtml = html.Substring(0, at + 4);
                        paragraphs.Add(paragraphHtml);
                        html = html.Length > at + 4 ? html.Substring(at + 4) : string.Empty;
                    }
                    else
                    {
                        paragraphs.Add(html);
                        html = string.Empty;
                    }
                }

                for (var paragraphCounter = 0; paragraphCounter < paragraphs.Count; paragraphCounter++)
                {
                    var paragraph = paragraphs[paragraphCounter];
                    var para = new Paragraph();
                    if (paragraphCounter == 0 && !string.IsNullOrEmpty(leadingHtml)) para.Inlines.AddRange(leadingHtml.ToInlines());
                    var innerParagraph = paragraph.Replace("<p>", string.Empty).Replace("</p>", string.Empty);
                    para.Inlines.AddRange(innerParagraph.ToInlines());
                    if (paragraphCounter == paragraphs.Count - 1 && !string.IsNullOrEmpty(trailingHtml)) para.Inlines.AddRange(trailingHtml.ToInlines());
                    blocks.Add(para);
                }
            }

            return blocks;
        }

        /// <summary>
        /// Converts HTML to WPF inlines
        /// </summary>
        /// <param name="html">The HTML.</param>
        /// <param name="trimLeadingSpaces">if set to <c>true</c> [trim leading spaces].</param>
        /// <param name="trimLeadingTabs">if set to <c>true</c> [trim leading tabs].</param>
        /// <returns>IEnumerable{Inline}.</returns>
        public static IEnumerable<Inline> ToInlines(this string html, bool trimLeadingSpaces = true, bool trimLeadingTabs = false)
        {
            var inlines = new List<Inline>();
            if (string.IsNullOrEmpty(html)) return inlines;

            var htmlBytes = html.ToCharArray();

            var currentRun = new Run();
            inlines.Add(currentRun);

            // We count how many open tags of specific meaning we are currently in.
            var boldCount = 0;
            var italicCount = 0;
            var underliniedCount = 0;
            var inMeaningfulTag = false;

            for (var charCounter = 0; charCounter < htmlBytes.Length; charCounter++)
            {
                var addChar = false;
                if (!inMeaningfulTag)
                    // Checking for open tags
                    if (htmlBytes[charCounter] == '<')
                    {
                        // This may be either an open or close HTML tag.
                        if (IsStartOf("/", htmlBytes, charCounter + 1))
                        {
                            // We are in a closing tag
                            if (IsStartOf("b", htmlBytes, charCounter + 2))
                            {
                                inMeaningfulTag = true;
                                boldCount--;
                            }
                            else if (IsStartOf("strong", htmlBytes, charCounter + 2))
                            {
                                inMeaningfulTag = true;
                                boldCount--;
                            }
                            else if (IsStartOf("i", htmlBytes, charCounter + 2))
                            {
                                inMeaningfulTag = true;
                                italicCount--;
                            }
                            else if (IsStartOf("u", htmlBytes, charCounter + 2))
                            {
                                inMeaningfulTag = true;
                                underliniedCount--;
                            }
                            else if (IsStartOf("br", htmlBytes, charCounter + 2))
                            {
                                inMeaningfulTag = true;
                                inlines.Add(new LineBreak());
                                currentRun = GetNewRunForSettings(boldCount, italicCount, underliniedCount);
                                inlines.Add(currentRun);
                            }
                        }
                        else
                        {
                            // Probably in an open tag
                            if (IsStartOf("b", htmlBytes, charCounter + 1))
                            {
                                inMeaningfulTag = true;
                                boldCount++;
                            }
                            else if (IsStartOf("strong", htmlBytes, charCounter + 1))
                            {
                                inMeaningfulTag = true;
                                boldCount++;
                            }
                            else if (IsStartOf("i", htmlBytes, charCounter + 1))
                            {
                                inMeaningfulTag = true;
                                italicCount++;
                            }
                            else if (IsStartOf("u", htmlBytes, charCounter + 1))
                            {
                                inMeaningfulTag = true;
                                underliniedCount++;
                            }
                        }
                        if (!inMeaningfulTag) addChar = true;
                    }
                    else addChar = true;
                else if (htmlBytes[charCounter] == '>')
                {
                    inMeaningfulTag = false; // We are not in the tag anymore, so we can now process the text as regular text again.

                    // We also start a new run
                    currentRun = GetNewRunForSettings(boldCount, italicCount, underliniedCount);
                    inlines.Add(currentRun);
                }

                if (addChar) currentRun.Text += htmlBytes[charCounter];
            }

            var inlineCounter = -1;
            var inlinesToRemove = new List<int>();
            foreach (var inline in inlines)
            {
                inlineCounter++;
                var inlineRun = inline as Run;
                if (inlineRun == null) continue;
                if (inlineRun.Text.Length == 0)
                {
                    inlinesToRemove.Add(inlineCounter);
                    inlineCounter--; // This is right and it is done because as we go through the removal loop, the prior items will be gone
                }
                else if (inlineCounter == 0 && string.IsNullOrEmpty(inlineRun.Text))
                {
                    inlinesToRemove.Add(inlineCounter);
                    inlineCounter--; // This is right and it is done because as we go through the removal loop, the prior items will be gone
                }
            }
            foreach (var index in inlinesToRemove) inlines.RemoveAt(index);

            if (inlines.Count > 0)
                If.Real<Run>(inlines[0], r =>
                    {
                        while ((trimLeadingSpaces && r.Text.StartsWith(" ")) || (trimLeadingTabs && r.Text.StartsWith("\t")))
                            r.Text = r.Text.Substring(1); // The very first run should never start with spaces
                    });

            return inlines;
        }

        /// <summary>
        /// Gets the new run for settings.
        /// </summary>
        /// <param name="boldCount">The bold count.</param>
        /// <param name="italicCount">The italic count.</param>
        /// <param name="underliniedCount">The underlinied count.</param>
        /// <returns>Run.</returns>
        private static Run GetNewRunForSettings(int boldCount, int italicCount, int underliniedCount)
        {
            var run = new Run();
            if (boldCount > 0) run.FontWeight = FontWeights.Bold;
            if (italicCount > 0) run.FontStyle = FontStyles.Italic;
            if (underliniedCount > 0) run.TextDecorations.Add(new TextDecoration(TextDecorationLocation.Underline, new Pen(Brushes.Black, 1), 0d, TextDecorationUnit.Pixel, TextDecorationUnit.Pixel));
            return run;
        }

        /// <summary>
        /// Checks whether the provided textis the start of a specified tag
        /// </summary>
        /// <param name="text">The text.</param>
        /// <param name="chars">The chars.</param>
        /// <param name="startIndex">The start index.</param>
        /// <returns><c>true</c> if [is start of] [the specified text]; otherwise, <c>false</c>.</returns>
        private static bool IsStartOf(string text, char[] chars, int startIndex)
        {
            if (chars.Length < startIndex + text.Length) return false; // Couldn't possibly be a match

            text = text.ToLower();
            var text2 = string.Empty;
            for (var counter = startIndex; counter < text.Length + startIndex; counter++)
                text2 += chars[counter];
            text2 = text2.ToLower();
            return text == text2;
        }
    }
}