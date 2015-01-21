using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

//This code originated from the following post: 
//http://www.wpfsharp.com/2012/08/24/a-wpf-searchable-textblock-control-with-highlighting/

namespace CODE.Framework.Wpf.Controls
{
    /// <summary>
    /// TextBlock class with extended features
    /// </summary>
    public class TextBlockEx : TextBlock
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TextBlockEx"/> class.
        /// </summary>
        public TextBlockEx() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="T:System.Windows.Controls.TextBlock" /> class, adding a specified <see cref="T:System.Windows.Documents.Inline" /> element as the initial display content.
        /// </summary>
        /// <param name="inline">An object deriving from the abstract <see cref="T:System.Windows.Documents.Inline" /> class, to be added as the initial content.</param>
        public TextBlockEx(Inline inline) : base(inline) { }


        new private string Text
        {
            set
            {
                if (string.IsNullOrWhiteSpace(RegularExpression) || !IsValidRegex(RegularExpression))
                {
                    base.Text = value;
                    return;
                }

                Inlines.Clear();
                if (string.IsNullOrWhiteSpace(value)) return;
                var split = Regex.Split(value, RegularExpression, RegexOptions.IgnoreCase);
                foreach (var str in split)
                {
                    var run = new Run(str);
                    if (Regex.IsMatch(str, RegularExpression, RegexOptions.IgnoreCase))
                    {
                        run.Background = HighlightBackground;
                        run.Foreground = HighlightForeground;
                    }
                    Inlines.Add(run);
                }
            }
        }
        /// <summary>
        /// Gets or sets the regular expression.
        /// </summary>
        /// <value>The regular expression.</value>
        public string RegularExpression
        {
            get { return _regularExpression; }
            set
            {
                _regularExpression = value;
                Text = base.Text;
            }
        } 
        private string _regularExpression;
        /// <summary>
        /// Search words
        /// </summary>
        public List<string> SearchWords
        {
            get
            {
                if (null == GetValue(SearchWordsProperty))
                    SetValue(SearchWordsProperty, new List<string>());
                return (List<string>)GetValue(SearchWordsProperty);
            }
            set
            {
                SetValue(SearchWordsProperty, value);
                UpdateRegex();
            }
        }
        /// <summary>
        /// Search words
        /// </summary>
        public static readonly DependencyProperty SearchWordsProperty = DependencyProperty.Register("SearchWords", typeof(List<string>), typeof(TextBlockEx), new PropertyMetadata(SearchWordsPropertyChanged));
        /// <summary>
        /// Fores when the search words change
        /// </summary>
        /// <param name="o">The object the property changed on</param>
        /// <param name="args">The <see cref="DependencyPropertyChangedEventArgs"/> instance containing the event data.</param>
        public static void SearchWordsPropertyChanged(DependencyObject o, DependencyPropertyChangedEventArgs args)
        {
            var stb = o as TextBlockEx;
            if (stb == null) return;
            stb.UpdateRegex();
        }

        /// <summary>
        /// Occurs when [on highlightable text changed].
        /// </summary>
        public event EventHandler OnHighlightableTextChanged;
        /// <summary>
        /// Gets or sets the highlightable text.
        /// </summary>
        /// <value>The highlightable text.</value>
        public string HighlightableText
        {
            get { return (string)GetValue(HighlightableTextProperty); }
            set { SetValue(HighlightableTextProperty, value); }
        }
        /// <summary>
        /// The highlightable text property
        /// </summary>
        public static readonly DependencyProperty HighlightableTextProperty = DependencyProperty.Register("HighlightableText", typeof(string), typeof(TextBlockEx), new PropertyMetadata(HighlightableTextChanged));
        /// <summary>
        /// Fires when the highlight text changes
        /// </summary>
        /// <param name="o">The o.</param>
        /// <param name="args">The <see cref="DependencyPropertyChangedEventArgs"/> instance containing the event data.</param>
        public static void HighlightableTextChanged(DependencyObject o, DependencyPropertyChangedEventArgs args)
        {
            var stb = o as TextBlockEx;
            if (stb == null) return;
            stb.Text = stb.HighlightableText;

            // Raise the event by using the () operator.
            if (stb.OnHighlightableTextChanged != null)
                stb.OnHighlightableTextChanged(stb, null);
        }
        /// <summary>
        /// Occurs when the highlight foreground changes
        /// </summary>
        public event EventHandler OnHighlightForegroundChanged;
        /// <summary>
        /// Gets or sets the highlight foreground.
        /// </summary>
        /// <value>The highlight foreground.</value>
        public Brush HighlightForeground
        {
            get
            {
                if (GetValue(HighlightForegroundProperty) == null)
                    SetValue(HighlightForegroundProperty, Brushes.Black);
                return (Brush)GetValue(HighlightForegroundProperty);
            }
            set { SetValue(HighlightForegroundProperty, value); }
        }
        /// <summary>
        /// The highlight foreground property
        /// </summary>
        public static readonly DependencyProperty HighlightForegroundProperty = DependencyProperty.Register("HighlightForeground", typeof(Brush), typeof(TextBlockEx), new PropertyMetadata(HighlightableForegroundChanged));
        /// <summary>
        /// Fires when the highlight foreground changes
        /// </summary>
        /// <param name="o">The o.</param>
        /// <param name="args">The <see cref="DependencyPropertyChangedEventArgs"/> instance containing the event data.</param>
        public static void HighlightableForegroundChanged(DependencyObject o, DependencyPropertyChangedEventArgs args)
        {
            var stb = o as TextBlockEx;
            if (stb == null) return;
            // Raise the event by using the () operator.
            if (stb.OnHighlightForegroundChanged != null)
                stb.OnHighlightForegroundChanged(stb, null);
        }
        /// <summary>
        /// Occurs when the highlight background changes
        /// </summary>
        public event EventHandler OnHighlightBackgroundChanged;
        /// <summary>
        /// Gets or sets the highlight background.
        /// </summary>
        /// <value>The highlight background.</value>
        public Brush HighlightBackground
        {
            get
            {
                if (GetValue(HighlightBackgroundProperty) == null)
                    SetValue(HighlightBackgroundProperty, Brushes.Yellow);
                return (Brush)GetValue(HighlightBackgroundProperty);
            }
            set { SetValue(HighlightBackgroundProperty, value); }
        }
        /// <summary>
        /// The highlight background property
        /// </summary>
        public static readonly DependencyProperty HighlightBackgroundProperty = DependencyProperty.Register("HighlightBackground", typeof(Brush), typeof(TextBlockEx), new PropertyMetadata(HighlightableBackgroundChanged));
        /// <summary>
        /// Fires when the highlight background changes
        /// </summary>
        /// <param name="o">The o.</param>
        /// <param name="args">The <see cref="DependencyPropertyChangedEventArgs"/> instance containing the event data.</param>
        public static void HighlightableBackgroundChanged(DependencyObject o, DependencyPropertyChangedEventArgs args)
        {
            var stb = o as TextBlockEx;
            if (stb == null) return;
            // Raise the event by using the () operator.
            if (stb.OnHighlightBackgroundChanged != null)
                stb.OnHighlightBackgroundChanged(stb, null);
        }

        /// <summary>
        /// Adds the search string.
        /// </summary>
        /// <param name="inString">The in string.</param>
        public void AddSearchString(String inString)
        {
            SearchWords.Add(inString);
            Update();
        }

        /// <summary>
        /// Updates the regular expression
        /// </summary>
        public void Update()
        {
            UpdateRegex();
        }

        /// <summary>
        /// Refreshes the highlighted text.
        /// </summary>
        public void RefreshHighlightedText()
        {
            Text = base.Text;
        }

        /// <summary>
        /// Updates the regex.
        /// </summary>
        private void UpdateRegex()
        {
            var newRegularExpression = string.Empty;
            foreach (var s in SearchWords)
            {
                if (newRegularExpression.Length > 0)
                    newRegularExpression += "|";
                newRegularExpression += RegexWrap(s);
            }

            if (RegularExpression != newRegularExpression)
                RegularExpression = newRegularExpression;
        }

        /// <summary>
        /// Determines whether the provided string is a valid RegEx
        /// </summary>
        /// <param name="inRegex">The in regex.</param>
        /// <returns>True of false</returns>
        public bool IsValidRegex(string inRegex)
        {
            if (string.IsNullOrEmpty(inRegex))
                return false;

            try
            {
                Regex.Match("", inRegex);
            }
            catch (ArgumentException)
            {
                return false;
            }

            return true;
        }

        private static string RegexWrap(string inString)
        {
            // Use positive look ahead and positive look behind tags
            // so the break is before and after each word, so the
            // actual word is not removed by Regex.Split()
            return String.Format("(?={0})|(?<={0})", inString);
        }
    }
}
 