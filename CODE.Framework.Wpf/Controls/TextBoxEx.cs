using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using CODE.Framework.Core.Utilities;
using CODE.Framework.Wpf.Utilities;

namespace CODE.Framework.Wpf.Controls
{
    /// <summary>
    /// This class provides extensions to the text box class
    /// </summary>
    public class TextBoxEx : TextBox
    {
        /// <summary>Watermark text property (can be used to set text for empty textboxes)</summary>
        public static readonly DependencyProperty WatermarkTextProperty = DependencyProperty.RegisterAttached("WatermarkText", typeof(string), typeof(TextBoxEx), new PropertyMetadata(""));
        /// <summary>Watermark text property (can be used to set text for empty textboxes)</summary>
        /// <param name="o">The object to set the value on.</param>
        /// <param name="value">The value.</param>
        public static void SetWatermarkText(DependencyObject o, string value)
        {
            o.SetValue(WatermarkTextProperty, value);
        }
        /// <summary>Watermark text property (can be used to set text for empty textboxes)</summary>
        /// <param name="o">The object to get the value for.</param>
        /// <returns>System.String.</returns>
        public static string GetWatermarkText(DependencyObject o)
        {
            return (string)o.GetValue(WatermarkTextProperty);
        }

        /// <summary>
        /// Attached property can be used to define a RegEx based input mask
        /// </summary>
        public static readonly DependencyProperty RegexInputMaskProperty = DependencyProperty.RegisterAttached("RegexInputMask", typeof(Regex), typeof(TextBoxEx), new PropertyMetadata(null));

        /// <summary>
        /// Attached property can be used to define a RegEx based input mask
        /// </summary>
        public static readonly DependencyProperty InputMaskRegExProperty = DependencyProperty.RegisterAttached("InputMaskRegEx", typeof(string), typeof(TextBoxEx), new PropertyMetadata(null, OnInputMaskRegExChanged));

        /// <summary>
        /// Fires when the input mask regular expression changes
        /// </summary>
        /// <param name="o">The object the mask is set on.</param>
        /// <param name="args">The <see cref="DependencyPropertyChangedEventArgs"/> instance containing the event data.</param>
        private static void OnInputMaskRegExChanged(DependencyObject o, DependencyPropertyChangedEventArgs args)
        {
            var textBox = o as TextBoxBase;
            if (textBox == null) return;

            var stringExpression = args.NewValue as string;
            if (string.IsNullOrEmpty(stringExpression))
            {
                textBox.SetValue(RegexInputMaskProperty, null);
                textBox.PreviewKeyDown -= PreviewKeyDownHandler;
                textBox.PreviewTextInput -= PreviewTextInputHandler;
                DataObject.RemovePastingHandler(textBox, PastingHandler);
            }
            else
            {
                var regex = new Regex(stringExpression, RegexOptions.Compiled | RegexOptions.IgnorePatternWhitespace);
                textBox.SetValue(RegexInputMaskProperty, regex);
                textBox.PreviewKeyDown += PreviewKeyDownHandler;
                textBox.PreviewTextInput += PreviewTextInputHandler;
                DataObject.AddPastingHandler(textBox, PastingHandler);
            }
        }

        /// <summary>
        /// Text input handler
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="TextCompositionEventArgs"/> instance containing the event data.</param>
        private static void PreviewTextInputHandler(object sender, TextCompositionEventArgs e)
        {
            var textBox = sender as TextBox;
            if (textBox == null) return;

            var regex = textBox.GetValue(RegexInputMaskProperty) as Regex;
            if (regex == null) return;

            var proposedText = GetProposedText(textBox, e.Text);

            if (!regex.IsMatch(proposedText)) e.Handled = true; // The input doesn't match the pattern, so we throw it out.
        }

        private static void PreviewKeyDownHandler(object sender, KeyEventArgs e)
        {
            //pressing space doesn't raise PreviewTextInput, reasons here http://social.msdn.microsoft.com/Forums/en-US/wpf/thread/446ec083-04c8-43f2-89dc-1e2521a31f6b?prof=required
            if (e.Key != Key.Space) return;

            var textBox = sender as TextBox;
            if (textBox == null) return;

            var regex = textBox.GetValue(RegexInputMaskProperty) as Regex;
            if (regex == null) return;

            var proposedText = e.Key == Key.Back ? GetProposedTextBackspace(textBox) : GetProposedText(textBox, " ");
            if (!regex.IsMatch(proposedText)) e.Handled = true;
        }

        /// <summary>
        /// Fires when text is pasted into the control
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="DataObjectPastingEventArgs"/> instance containing the event data.</param>
        private static void PastingHandler(object sender, DataObjectPastingEventArgs e)
        {
            var textBox = sender as TextBox;
            if (textBox == null) return;

            var regex = textBox.GetValue(RegexInputMaskProperty) as Regex;
            if (regex == null) return;


            if (e.DataObject.GetDataPresent(typeof (string)))
            {
                var pastedText = e.DataObject.GetData(typeof (string)) as string;
                var proposedText = GetProposedText(textBox, pastedText);
                if (!regex.IsMatch(proposedText)) e.CancelCommand();
            }
            else
                e.CancelCommand();
        }

        /// <summary>
        /// Gets the text a new input event would produce if we let things go forward
        /// </summary>
        /// <param name="textBox">The text box.</param>
        /// <param name="newText">The new text.</param>
        /// <returns>System.String.</returns>
        private static string GetProposedText(TextBox textBox, string newText)
        {
            var text = textBox.Text;
            if (textBox.SelectionStart != -1) text = text.Remove(textBox.SelectionStart, textBox.SelectionLength);
            text = text.Insert(textBox.CaretIndex, newText);
            return text;
        }

        /// <summary>
        /// Gets the proposed text assuming a backspace.
        /// </summary>
        /// <param name="textBox">The text box.</param>
        /// <returns>System.String.</returns>
        private static string GetProposedTextBackspace(TextBox textBox)
        {
            var text = GetTextWithSelectionRemoved(textBox);
            if (textBox.SelectionStart > 0 && textBox.SelectionLength == 0) text = text.Remove(textBox.SelectionStart - 1, 1);
            return text;
        }

        /// <summary>
        /// Returns what the textbox text would be with the current selection removed
        /// </summary>
        /// <param name="textBox">TextBox</param>
        /// <returns>Text</returns>
        private static string GetTextWithSelectionRemoved(TextBox textBox)
        {
            var text = textBox.Text;
            if (textBox.SelectionStart != -1)text = text.Remove(textBox.SelectionStart, textBox.SelectionLength);
            return text;
        }

        /// <summary>
        /// Sets the input mask.
        /// </summary>
        public static void SetInputMaskRegEx(DependencyObject o, string value)
        {
            o.SetValue(InputMaskRegExProperty, value);
        }

        /// <summary>
        /// Gets the input mask.
        /// </summary>
        public static string GetInputMaskRegEx(DependencyObject o)
        {
            return (string)o.GetValue(InputMaskRegExProperty);
        }


        //CUSTOM SPELLCHECK DICTIONARIES
        private static string _customDictionaryFile;
        private static string _ignoreAllDictionaryFile;
        private static List<string> _customDictionaries;
        /// <summary>
        /// Fires when the spell check flag is toggled
        /// </summary>
        public static event EventHandler ToggleSpellCheck;

        ///<summary>When set to True, assumes a custom user dictionary named UserDictionary.lex in the application folder.
        ///Spell check context menu allows user to add words to the UserDictionary.lex custom dictionary.
        ///If UserDictionary.lex does not exist, it will be created.
        ///IgnoreAllDictionary.lex will also be created. Unlike UserDictionary.lex it will be overwritten each time the app is restarted.
        ///Other custom dictionaries found in the same folder will be used, but only UserDictionary.lex will get "Added" words.
        ///Custom dictionary path can be overridden in the app.config by adding a CustomDictionaryPath key and value to appSettings.
        ///Custom dictionary path setting is applicaiton wide.</summary>
        public static readonly DependencyProperty UseCustomDictionariesProperty = DependencyProperty.RegisterAttached("UseCustomDictionaries", typeof(bool), typeof(TextBoxEx), new PropertyMetadata(false, UseCustomDictionariesChanged));
        /// <summary>
        /// Gets UseCustomDictionaries
        /// </summary>
        /// <param name="obj">The object.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        public static bool GetUseCustomDictionaries(DependencyObject obj) { return (bool)obj.GetValue(UseCustomDictionariesProperty); }
        /// <summary>
        /// Sets UseCustomDictionaries
        /// </summary>
        /// <param name="obj">The object.</param>
        /// <param name="value">if set to <c>true</c> [value].</param>
        public static void SetUseCustomDictionaries(DependencyObject obj, bool value) { obj.SetValue(UseCustomDictionariesProperty, value); }

        /// <summary>
        /// Fires when UseCustomDictionaries changes
        /// </summary>
        /// <param name="dependencyObject">The dependency object.</param>
        /// <param name="a">The <see cref="DependencyPropertyChangedEventArgs"/> instance containing the event data.</param>
        private static void UseCustomDictionariesChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs a)
        {
            var textBoxBase = dependencyObject as TextBoxBase;
            if (textBoxBase == null) return;
            var isEnabled = (bool)a.NewValue;

            if (isEnabled)
            {
                var handler = new EventHandler((s, e) =>
                {
                    SpellCheck.SetIsEnabled(textBoxBase, false);
                    SpellCheck.SetIsEnabled(textBoxBase, true);
                });

                ToggleSpellCheck += handler;
                textBoxBase.Unloaded += (s, e) => ToggleSpellCheck -= handler;

                SpellCheck.SetIsEnabled(textBoxBase, true);

                lock (textBoxBase)
                {
                    try
                    {
                        if (_customDictionaries == null) //load custom dictionaries
                        {
                            _customDictionaries = new List<string>();
                            var customDictionaryPath = SpellCheckHelper.GetCustomDictionaryPath();
                            if (!Directory.Exists(customDictionaryPath)) Directory.CreateDirectory(customDictionaryPath);
                            _customDictionaryFile = SpellCheckHelper.GetCustomDictionaryFile(customDictionaryPath);
                            _ignoreAllDictionaryFile = SpellCheckHelper.GetIgnoreAllDictionaryFile(customDictionaryPath);
                            var fs = File.Create(_ignoreAllDictionaryFile);
                            fs.Close();
                            if (!File.Exists(_customDictionaryFile))
                            {
                                fs = File.Create(_customDictionaryFile);
                                fs.Close();
                            }

                            var dir = new DirectoryInfo(customDictionaryPath);
                            foreach (var fileInfo in dir.GetFiles("*.lex")) _customDictionaries.Add(fileInfo.FullName);
                        }
                        var dictionaries = SpellCheck.GetCustomDictionaries(textBoxBase);
                        dictionaries.Clear();
                        foreach (var fileName in _customDictionaries) dictionaries.Add(new Uri(fileName));
                    }
                    catch (Exception ex) { LoggingMediator.Log(ex); }
                }

                if (textBoxBase.ContextMenu != null) return;
                textBoxBase.ContextMenu = new ContextMenu();
                textBoxBase.ContextMenuOpening += textBoxEx_ContextMenuOpening;
            }
        }

        private static void textBoxEx_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            var textBoxBase = sender as TextBoxBase;
            if (textBoxBase == null) return;

            textBoxBase.ContextMenu.Items.Clear();
            SpellingError spellingError = null;
            if (textBoxBase is TextBox)
            {
                var textBox = textBoxBase as TextBox;
                spellingError = textBox.GetSpellingError(textBox.CaretIndex);
            }
            else if (textBoxBase is RichTextBox)
            {
                var richTextBox = textBoxBase as RichTextBox;
                spellingError = richTextBox.GetSpellingError(richTextBox.CaretPosition);
            }
            if (spellingError == null)
            {
                e.Handled = true;
                return;
            }

            var menuItemIndex = 0;
            foreach (var str in spellingError.Suggestions)
            {
                var mi = new MenuItem
                {
                    Header = str,
                    FontWeight = FontWeights.Bold,
                    Command = EditingCommands.CorrectSpellingError,
                    CommandParameter = str,
                    CommandTarget = textBoxBase,
                };
                textBoxBase.ContextMenu.Items.Insert(menuItemIndex, mi);
                menuItemIndex++;
            }
            if (menuItemIndex == 0)
            {
                var mi = new MenuItem
                {
                    Header = "(no suggestions)",
                    IsEnabled = false,
                };
                textBoxBase.ContextMenu.Items.Insert(menuItemIndex, mi);
                menuItemIndex++;
            }

            var separator = new Separator();
            textBoxBase.ContextMenu.Items.Insert(menuItemIndex, separator);
            menuItemIndex++;

            var ignoreAll = new MenuItem
            {
                Header = "Ignore All",
                CommandTarget = textBoxBase,
            };
            ignoreAll.Click += ignoreAll_Click;
            textBoxBase.ContextMenu.Items.Insert(menuItemIndex, ignoreAll);
            menuItemIndex++;

            var addToDictionary = new MenuItem
            {
                Header = "Add to Dictionary",
                CommandTarget = textBoxBase,
            };

            addToDictionary.Click += addToDictionary_Click;
            textBoxBase.ContextMenu.Items.Insert(menuItemIndex, addToDictionary);
        }

        private static void addToDictionary_Click(object sender, RoutedEventArgs e)
        {
            WriteToDictionary(sender, _customDictionaryFile);
        }

        private static void ignoreAll_Click(object sender, RoutedEventArgs e)
        {
            WriteToDictionary(sender, _ignoreAllDictionaryFile);
        }
        
        private static void WriteToDictionary(object sender, string dictionaryFile)
        {
            var item = sender as MenuItem;
            if (item == null) return;
            var textBoxBase = item.CommandTarget as TextBoxBase; ;
            if (textBoxBase == null) return;

            string misspelledWord = string.Empty;
            if (textBoxBase is TextBox)
            {
                var textBox = textBoxBase as TextBox;
                misspelledWord = textBox.Text.Substring(textBox.GetSpellingErrorStart(textBox.CaretIndex), textBox.GetSpellingErrorLength(textBox.CaretIndex));
            }
            else if (textBoxBase is RichTextBox)
            {
                var richTextBox = textBoxBase as RichTextBox;
                var range = richTextBox.GetSpellingErrorRange(richTextBox.CaretPosition);
                if (range != null) misspelledWord = range.Text;
            }
            if (string.IsNullOrWhiteSpace(misspelledWord)) return;

            try
            {
                var writer = File.AppendText(dictionaryFile);
                writer.WriteLine(misspelledWord);
                writer.Close();
                if (ToggleSpellCheck != null) ToggleSpellCheck(textBoxBase, null);
            }
            catch (Exception ex) { LoggingMediator.Log(ex); }
        }

        /// <summary>
        /// If set to true, triggers a source update on textboxes whenever the ENTER key is pressed, even if the 
        /// text element is otherwise only updated on LostFocus or Explicit
        /// </summary>
        public static readonly DependencyProperty UpdateSourceOnEnterKeyProperty = DependencyProperty.RegisterAttached("UpdateSourceOnEnterKey", typeof(bool), typeof(TextBoxEx), new PropertyMetadata(false, OnUpdateSourceOnEnterKeyChanged));

        /// <summary>
        /// Fires when the UpdateSourceOnEnterKey property changes
        /// </summary>
        /// <param name="d">The object the property is set on</param>
        /// <param name="e">Event arguments</param>
        private static void OnUpdateSourceOnEnterKeyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (!(bool) e.NewValue) return;

            var text = d as TextBox;
            if (text == null) return;

            text.PreviewKeyDown += (s2, e2) =>
            {
                if (e2.Key == Key.Enter)
                {
                    var binding = BindingOperations.GetBindingExpression(text, TextProperty);
                    if (binding != null)
                        binding.UpdateSource();
                }
            };
        }

        /// <summary>
        /// If set to true, triggers a source update on textboxes whenever the ENTER key is pressed, even if the 
        /// text element is otherwise only updated on LostFocus or Explicit
        /// </summary>
        /// <param name="d">The object the property is set on</param>
        public static bool GetUpdateSourceOnEnterKey(DependencyObject d)
        {
            return (bool) d.GetValue(UpdateSourceOnEnterKeyProperty);
        }
        /// <summary>
        /// If set to true, triggers a source update on textboxes whenever the ENTER key is pressed, even if the 
        /// text element is otherwise only updated on LostFocus or Explicit
        /// </summary>
        /// <param name="d">The object the property is set on</param>
        /// <param name="value">The new value that is to be set</param>
        public static void SetUpdateSourceOnEnterKey(DependencyObject d, bool value)
        {
            d.SetValue(UpdateSourceOnEnterKeyProperty, value);
        }

    }
}
